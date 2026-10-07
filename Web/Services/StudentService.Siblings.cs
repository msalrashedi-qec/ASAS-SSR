using Core.Entities.Discounts;
using Shared.Enums;

namespace Web.Services;

public partial class StudentService
{
    public async Task StageSiblingDiscountChangesAsync(IUnitOfWork uow,
        IEnumerable<StudentAccount> changes, StudentContract? pendingContract = null)
    {
        var entries = changes.ToList();
        var visible = (await uow.Discounts.FindAllAsync(x => x.TransactionTypeId == TransactionTypeIds.SiblingDiscount
            && x.IsShownInContract)).SelectMany(x => new[]
            { StudentDiscountWithdrawal.DiscountReference(x.Id), $"discount-withdrawal:{x.Id}" }).ToHashSet();
        foreach (var entry in entries)
        {
            StudentContractDetail? detail = entry.Categoryid != StudentSubscriptionTax.TaxCategoryId && visible.Contains(entry.RefNo ?? "") ? new()
            {
                ContractId = entry.ContractId, ServiceId = entry.ServiceId, SemesterId = entry.SemesterId,
                Amount = entry.Debit - entry.Credit, Comments = entry.Comments
            } : null;
            if (pendingContract is not null && entry.ContractId == pendingContract.Id)
            {
                pendingContract.StudentAccounts.Add(entry);
                if (detail is not null) pendingContract.Details.Add(detail);
            }
            else
            {
                await uow.StudentAccounts.AddAsync(entry);
                if (detail is not null) await uow.StudentContractDetails.AddAsync(detail);
            }
        }
    }

    private async Task<bool> IsSiblingDiscountEligibleAsync(IUnitOfWork uow, StudentDto model)
    {
        var family = (await uow.Students.FindAllAsync(x => x.Id != model.Id
            && x.ParentId == model.ParentId && x.SchoolId == model.SchoolId
            && x.CurrentContract.RegisteredForYearId == model.RegisteredForYearId
            && x.CurrentContract.StatusId >= 10 && x.CurrentContract.StatusId < 30,
            ["CurrentContract.Class.Level"])).ToList();
        var existing = model.Id == Guid.Empty ? null : await uow.Students.FindAsync(x => x.Id == model.Id, ["CurrentContract"]);
        var candidate = new Student
        {
            Id = model.Id, SN = existing?.SN ?? int.MaxValue,
            CreatedOn = existing?.CreatedOn ?? DateTime.UtcNow.GetKsaDateTime(),
            CurrentContract = new StudentContract
            {
                StatusId = 10,
                CreatedOn = existing?.CurrentContract is { StatusId: >= 10 and < 30 } current
                    ? current.CreatedOn : DateTime.UtcNow.GetKsaDateTime(),
                Class = await uow.SchoolClasses.FindAsync(x => x.Id == model.ClassId, ["Level"])
            }
        };
        family.Add(candidate);
        return SiblingDiscountPolicy.Rank(family).First() != candidate;
    }

    // Build the complete plan before staging writes. Include the pending contract
    // explicitly because repository reads use a separate context from staged writes.
    public async Task<IReadOnlyList<StudentAccount>> BuildSiblingDiscountChangesAsync(
        IUnitOfWork uow, Student student, StudentContract contract, DateTime date, string userId,
        bool withdrawing = false, IEnumerable<StudentAccount>? pendingAccounts = null)
    {
        var family = (await uow.Students.FindAllAsync(x => x.Id != student.Id
            && x.ParentId == student.ParentId && x.SchoolId == student.SchoolId
            && x.CurrentContract.RegisteredForYearId == contract.RegisteredForYearId
            && x.CurrentContract.StatusId >= 10 && x.CurrentContract.StatusId < 30,
            ["CurrentContract.Class.Level"])).ToList();
        if (!withdrawing && contract.StatusId >= 10 && contract.StatusId < 30)
        {
            family.Add(new Student
            {
                Id = student.Id, SN = student.SN, Name = student.Name, CreatedOn = student.CreatedOn,
                CurrentContract = new StudentContract
                {
                    Id = contract.Id, StatusId = contract.StatusId, StartDate = contract.StartDate,
                    CreatedOn = contract.CreatedOn,
                    Class = await uow.SchoolClasses.FindAsync(x => x.Id == contract.ClassId, ["Level"])
                }
            });
        }
        var contractIds = family.Select(x => x.CurrentContract.Id).ToArray();
        var ledger = contractIds.Length == 0 ? new List<StudentAccount>()
            : (await uow.StudentAccounts.FindAllAsync(x => contractIds.Contains(x.ContractId))).ToList();
        if (pendingAccounts is not null)
        {
            ledger.RemoveAll(x => x.ContractId == contract.Id);
            ledger.AddRange(pendingAccounts);
        }
        var definitions = (await uow.Discounts.FindAllAsync(x => x.TransactionTypeId == TransactionTypeIds.SiblingDiscount)).ToList();
        var details = (await uow.DiscountDetails.FindAllAsync(x => x.SchoolId == student.SchoolId
            && x.YearId == contract.RegisteredForYearId
            && x.Discount.TransactionTypeId == TransactionTypeIds.SiblingDiscount
            && x.Discount.ApplicationTiming == DiscountApplicationTiming.OnSubscription, ["Discount"])).ToList();
        var semesters = (await uow.SchoolSemesters.FindAllAsync(x => x.SchoolId == student.SchoolId
            && x.YearId == contract.RegisteredForYearId)).ToList();
        return SiblingDiscountPolicy.Reconcile(family, ledger, definitions, (child, order, semesterId, categoryId) =>
            details.Where(x => x.Discount.ServiceCategoryId == categoryId && (x.SemesterId == semesterId || x.SemesterId == 0))
                .GroupBy(x => x.DiscountId)
                .Select(group => group.OrderByDescending(x => x.SemesterId == semesterId)
                    .FirstOrDefault(x => IsRegistrationStateInRange(x,
                        semesters.FirstOrDefault(s => s.SemesterId == semesterId), child.CurrentContract.StartDate,
                        family.Count, child.CurrentContract.Class.Id)))
                .Where(x => x is not null).Select(x => x!),
            date, userId, DateTime.UtcNow.GetKsaDateTime());
    }
}
