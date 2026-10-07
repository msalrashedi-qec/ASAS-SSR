using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Core.Entities.Business;
using Core.Entities.Discounts;
using Core.Interfaces;
using Shared.Dtos.Business;
using Web.Components.Pages.Discounts;
using Web.Services;

var selectedSchool = Guid.NewGuid();
var student = new Student { Id = Guid.NewGuid(), SchoolId = Guid.NewGuid() };
var request = new DiscountRequest { Id = Guid.NewGuid(), WfProcessId = Guid.NewGuid(), StudentId = student.Id };
var uow = Stub<IUnitOfWork>.Create((method, _) => method.Name switch
{
    "get_Students" => Repository(new List<Student> { student }),
    "get_DiscountRequests" => Repository(new List<DiscountRequest> { request }),
    "get_StudentAccounts" => Repository(new List<StudentAccount>()),
    _ => throw new NotSupportedException(method.Name)
});
var component = new DiscountRequestDetails();
Set("_uow", uow);
Set("_appStateService", new AppStateService(null!, null!, uow) { SchoolId = selectedSchool });
Set("_model", new DiscountRequestDto { StudentId = student.Id });

component.ProcessId = request.WfProcessId;
await Load();
Check(Get("student") == student, "Existing request resolves its student across selected schools");
component.ProcessId = null;
await Load();
Check(Get("student") == null, "New request cannot select a student outside the selected school");
student.SchoolId = selectedSchool;
await Load();
Check(Get("student") == student, "New request resolves a student in the selected school");
component.ProcessId = request.WfProcessId;
Set("_model", new DiscountRequestDto { StudentId = Guid.NewGuid() });
await Load();
Check(Get("student") == null, "Existing request cannot resolve a different student");

// Item selection must use the complete ledger, including credits and prior contracts.
student.CurrentContractId = Guid.NewGuid();
student.CurrentContract = new StudentContract { Id = student.CurrentContractId };
var oldContract = Guid.NewGuid();
var entries = new List<StudentAccount>();
void Entry(Guid contract, int service, int category, double debit, double credit = 0) => entries.Add(new StudentAccount
{
    ContractId = contract, ServiceId = service, Categoryid = category, SemesterId = 1,
    Debit = debit, Credit = credit, TransactionTypeId = Core.Enums.TransactionTypeIds.AddDues
});
Entry(student.CurrentContractId, 1, 20, 1000);
Entry(student.CurrentContractId, 1, 20, 0, 400);
entries[^1].TransactionTypeId = Core.Enums.TransactionTypeIds.DuesPayment;
Entry(student.CurrentContractId, 1, 90, 150);
Entry(student.CurrentContractId, 2, 30, 500);
Entry(student.CurrentContractId, 2, 90, 75);
Entry(student.CurrentContractId, 3, 30, 200, 200);
Entry(student.CurrentContractId, 4, 30, 100, 110);
Entry(oldContract, 1, 20, 300);
var configurations = new List<DiscountDetail>();
Set("_uow", Stub<IUnitOfWork>.Create((method, _) => method.Name switch
{
    "get_DiscountDetails" => Repository(configurations),
    _ => throw new NotSupportedException(method.Name)
}));
Set("student", student);
Set("studentAccounts", entries);
Set("discountTypes", new List<Discount>
{
    new() { Id = 1, TransactionTypeId = Core.Enums.TransactionTypeIds.FinancialSettlement },
    new() { Id = 2, TransactionTypeId = Core.Enums.TransactionTypeIds.SpecialDiscount }
});
Set("_model", new DiscountRequestDto());
async Task ChangeType(int id) => await (Task)typeof(DiscountRequestDetails)
    .GetMethod("OnDiscountChangedAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(component, new object[] { id })!;
await ChangeType(1);
var model = (DiscountRequestDto)Get("_model")!;
Check(model.Details.Count == 5, "Settlement includes all and only positive balances, including previous contracts");
Check(model.Details.Single(x => x.ContractId == student.CurrentContractId && x.CategoryId == 20).RemainingAmount == 600,
    "Partially paid item exposes only its remaining balance");
Check(model.Details.All(x => x.IsSelected && !x.HasConfiguredPercentage && x.Amount == 0), "Settlement amounts are independently editable");
model.Details[0].Amount = 50;
await ChangeType(2);
Check(model.Details.Count == 2 && model.Details.All(x => x.ServiceId == 1 && x.ContractId == student.CurrentContractId),
    "Other requests include tuition and its tax only");
Check(model.Details.All(x => x.Amount == 0), "Changing request type clears earlier amounts");
void SelectItem(Shared.Dtos.Discounts.DiscountRequestDetailDto item) => typeof(DiscountRequestDetails)
    .GetMethod("OnSemesterSelectionChanged", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(component, new object[] { item });
void SyncTax() => typeof(DiscountRequestDetails)
    .GetMethod("SynchronizeTaxDiscounts", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(component, null);
var fee = model.Details.Single(x => x.CategoryId == 20);
var tax = model.Details.Single(x => x.CategoryId == 90);
fee.IsSelected = true;
fee.Amount = 100;
SelectItem(fee);
Check(tax.IsSelected && tax.Amount == 15, "Fixed discount selects proportional tax, excluding payments from the calculation basis");
fee.Amount = 33.33;
SyncTax();
Check(tax.Amount == 5, "Editing the fee recalculates and rounds the tax discount");
tax.IsSelected = false;
tax.Amount = 999;
SyncTax();
Check(tax.IsSelected && tax.Amount == 5, "Synchronization restores automatic tax selection and amount");
fee.IsSelected = false;
SelectItem(fee);
Check(!tax.IsSelected && tax.Amount == 0, "Deselecting the fee clears its tax discount");
await ChangeType(1);
tax = model.Details.First(x => x.CategoryId == 90);
tax.Amount = 12;
SyncTax();
Check(tax.IsSelected && tax.Amount == 12, "Settlement tax amounts remain independent");
configurations.Add(new DiscountDetail { DiscountId = 2, SchoolId = student.SchoolId, SemesterId = 0,
    Year = new Core.Entities.General.Year { IsCurrent = true }, Amount = 10 });
await ChangeType(2);
Check(model.Details.Single(x => x.CategoryId == 20).Amount == 100
    && model.Details.Single(x => x.CategoryId == 90).Amount == 15, "Configured percentages apply separately to tuition and its tax");
fee = model.Details.Single(x => x.CategoryId == 20);
fee.IsSelected = true;
SelectItem(fee);
Check(model.Details.Single(x => x.CategoryId == 90).IsSelected
    && model.Details.Single(x => x.CategoryId == 90).Amount == 15, "Percentage discount automatically selects and calculates tax");

entries.RemoveAll(x => x.Categoryid == 90 && x.ServiceId == 1);
await ChangeType(2);
Check(model.Details.Count == 1 && model.Details[0].CategoryId == 20, "Tuition without tax has no synthetic tax row");
await ChangeType(1);
Check(model.Details.All(x => !x.HasConfiguredPercentage), "Settlement stays editable after switching from a configured discount");
using var modelServices = new ServiceCollection()
    .Configure<IdentityOptions>(options => options.Stores.SchemaVersion = IdentitySchemaVersions.Version3)
    .BuildServiceProvider();
using (var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseApplicationServiceProvider(modelServices)
    .UseSqlServer("Server=localhost;Database=SchemaValidationOnly;Integrated Security=True;TrustServerCertificate=True").Options))
{
    Check(!db.Database.HasPendingModelChanges(), "Migration snapshot matches the current model");
    // Exercise the detached repository update used by the request editor. No DB
    // connection is needed to verify the DELETE/INSERT states sent by SaveAsync.
    var detailRepository = new Infrastructure.Repositories.BaseRepository<DiscountRequestDetail>(null!, () => db, new SemaphoreSlim(1, 1));
    var requestRepository = new Infrastructure.Repositories.BaseRepository<DiscountRequest>(null!, () => db, new SemaphoreSlim(1, 1));
    var savedRows = new List<DiscountRequestDetail>
    {
        new() { Id = Guid.NewGuid(), RequestId = request.Id, SemesterId = 1, Amount = -200 },
        new() { Id = Guid.NewGuid(), RequestId = request.Id, SemesterId = 2, Amount = -100 }
    };
    for (var edit = 0; edit < 3; edit++)
    {
        db.ChangeTracker.Clear();
        detailRepository.DeleteRange(savedRows.Select(x => new DiscountRequestDetail { Id = x.Id, RequestId = request.Id }));
        var edited = new DiscountRequest
        {
            Id = request.Id,
            Details = new List<DiscountRequestDetail>
            {
                new() { SemesterId = 1, Amount = -250 - edit }
            }
        };
        await requestRepository.Update(edited);
        var states = db.ChangeTracker.Entries<DiscountRequestDetail>().ToList();
        Check(states.Count(x => x.State == EntityState.Deleted) == savedRows.Count,
            $"Edit {edit + 1} deletes every previous detail, including deselected rows");
        Check(states.Count(x => x.State == EntityState.Added) == 1
            && states.Where(x => x.State == EntityState.Added).Single().Entity.Amount == -250 - edit,
            $"Edit {edit + 1} inserts only the selected detail with its updated amount");
        Check(states.All(x => x.State is EntityState.Deleted or EntityState.Added),
            $"Edit {edit + 1} retains no stale details");
        // Simulate the permanent IDs returned by the database after saving.
        savedRows = states.Where(x => x.State == EntityState.Added)
            .Select(x => new DiscountRequestDetail
            {
                Id = Guid.NewGuid(), RequestId = request.Id,
                SemesterId = x.Entity.SemesterId, Amount = x.Entity.Amount
            }).ToList();
    }
    db.ChangeTracker.Clear();
    var script = db.GetService<IMigrator>().GenerateScript("20260929063424_AddBankAccountInfoInSchoolsTable", "20260930120000_AddRequestDetailItems");
    Check(script.Contains("[ContractId]") && script.Contains("[ServiceId]") && script.Contains("[CategoryId]")
        && script.Contains("[ItemName]") && script.Contains("[ItemNameAr]"), "Migration generates SQL for every item field without connecting to a database");
}

object? Get(string name) => typeof(DiscountRequestDetails).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(component);
void Set(string name, object value)
{
    var type = typeof(DiscountRequestDetails);
    var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
    if (field != null) field.SetValue(component, value);
    else type.GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.SetValue(component, value);
}
Task Load() => (Task)typeof(DiscountRequestDetails).GetMethod("LoadStudentAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(component, null)!;
void Check(bool success, string name)
{
    if (!success) throw new Exception(name);
    Console.WriteLine($"PASS: {name}");
}
IBaseRepository<T> Repository<T>(List<T> data) where T : class => Stub<IBaseRepository<T>>.Create((method, args) =>
{
    var matches = data.Where(((Expression<Func<T, bool>>)args![0]!).Compile()).ToList();
    return method.Name switch
    {
        "FindAsync" => Task.FromResult(matches.FirstOrDefault()),
        "FindAllAsync" => Task.FromResult<IEnumerable<T>>(matches),
        _ => throw new NotSupportedException(method.Name)
    };
});
public class Stub<T> : DispatchProxy where T : class
{
    public Func<MethodInfo, object?[]?, object?> Handler = null!;
    public static T Create(Func<MethodInfo, object?[]?, object?> handler)
    {
        var proxy = Create<T, Stub<T>>();
        ((Stub<T>)(object)proxy).Handler = handler;
        return proxy;
    }
    protected override object? Invoke(MethodInfo? method, object?[]? args) => Handler(method!, args);
}
