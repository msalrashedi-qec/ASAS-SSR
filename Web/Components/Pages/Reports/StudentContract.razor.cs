using Microsoft.AspNetCore.Components.Rendering;

namespace Web.Components.Pages.Reports;

public partial class StudentContract
{
    private static readonly CultureInfo ArabicCulture = CultureInfo.GetCultureInfo("ar-SA");
    private Student? student;
    private Core.Entities.Business.StudentContract? contract;
    private string registeredForYearName = string.Empty;
    private bool isLoading;

    [Parameter] public Guid StudentId { get; set; }

    private string StudentFullName => student is null ? string.Empty : $"{student.Name} {student.FatherName}".Trim();
    private string ParentFullName => student?.Parent?.Name ?? string.Empty;
    private string ParentNationalId => student?.Parent?.NationalID ?? string.Empty;
    private string ParentMobile => student?.Parent?.Mobile ?? string.Empty;
    private string ParentNationality => student?.Parent?.Nationality?.NameAr ?? string.Empty;
    private string StudentNationality => student?.Nationality?.NameAr ?? string.Empty;
    private string SchoolName => student?.School?.NameAr ?? string.Empty;
    private string CityName => student?.School?.City?.NameAr ?? string.Empty;
    private string CompanyName => student?.School?.Company?.NameAr ?? string.Empty;
    private string CompanyCr => student?.School?.Company?.CR ?? string.Empty;
    private string CompanyAddress => ValueOrBlank(student?.School?.Company?.Address);
    private string CompanyPhone => ValueOrBlank(student?.School?.Company?.PhoneNumber);
    private string LevelName => contract?.Class?.Level?.NameAr ?? string.Empty;
    private string ClassName => contract?.Class?.NameAr ?? string.Empty;
    private string AcademicYear => string.IsNullOrWhiteSpace(registeredForYearName) ? contract?.Year?.Name ?? string.Empty : registeredForYearName;
    private DateTime EffectiveDate => contract?.StartDate ?? DateTime.Today;
    private string ArabicDayName => EffectiveDate.ToString("dddd", ArabicCulture);
    private string ContractDate => ReportFormatter.Date(EffectiveDate);
    private double ContractTotal => contract?.Details.Sum(x => x.Amount) ?? 0;

    protected override async Task OnParametersSetAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        isLoading = true;
        student = null;
        contract = null;
        registeredForYearName = string.Empty;
        try
        {
            await _appStateService.InitializeAsync();
            if (_appStateService.SchoolId == Guid.Empty) return;

            student = await _uow.Students.FindAsync(
                x => x.Id == StudentId && x.SchoolId == _appStateService.SchoolId,
                ["School", "School.Company", "School.City", "Parent", "Parent.Nationality", "Nationality", "CurrentContract", "CurrentContract.Year", "CurrentContract.Class", "CurrentContract.Class.Level", "CurrentContract.Details", "CurrentContract.Details.Service", "CurrentContract.Details.Semester"]);

            contract = student?.CurrentContract;
            if (contract is not null)
            {
                var registeredYear = await _uow.Years.FindAsync(x => x.Id == contract.RegisteredForYearId);
                registeredForYearName = registeredYear?.Name ?? contract.Year?.Name ?? string.Empty;
            }
        }
        catch (Exception ex)
        {
            student = null;
            contract = null;
            _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], ex.Message);
        }
        finally
        {
            isLoading = false;
        }
    }

    private string Localized(string? english, string? arabic) => !string.IsNullOrWhiteSpace(arabic) ? arabic : english ?? string.Empty;
    private static string ValueOrBlank(string? value) => string.IsNullOrWhiteSpace(value) ? "........................" : value;

    private RenderFragment ContractHeader() => builder =>
    {
        var sequence = 0;
        builder.OpenElement(sequence++, "header");
        builder.AddAttribute(sequence++, "class", "contract-letterhead");
        AddLogo(builder, ref sequence, "uploads/schools", student?.School?.Logo, SchoolName, showCaption: false);
        builder.OpenElement(sequence++, "div");
        builder.AddAttribute(sequence++, "class", "contract-ministry");
        builder.OpenElement(sequence++, "strong");
        builder.AddContent(sequence++, "وزارة التعليم");
        builder.CloseElement();
        builder.OpenElement(sequence++, "span");
        builder.AddContent(sequence++, $"الإدارة العامة للتعليم بمدينة {CityName}");
        builder.CloseElement();
        builder.OpenElement(sequence++, "span");
        builder.AddContent(sequence++, SchoolName);
        builder.CloseElement();
        builder.CloseElement();
        AddLogo(builder, ref sequence, "uploads/companies", student?.School?.Company?.Logo, CompanyName, showCaption: false);
        builder.CloseElement();
    };

    private RenderFragment ContractFooter(string PageNumber) => builder =>
    {
        var sequence = 0;
        builder.OpenElement(sequence++, "footer");
        builder.AddAttribute(sequence++, "class", "contract-page-footer");
        builder.OpenElement(sequence++, "span");
        builder.AddContent(sequence++, "توقيع ولي الأمر: .................................");
        builder.CloseElement();
        builder.OpenElement(sequence++, "span");
        builder.AddAttribute(sequence++, "dir", "ltr");
        builder.AddContent(sequence++, $"{PageNumber}/3");
        builder.CloseElement();
        builder.CloseElement();
    };

    private void AddLogo(RenderTreeBuilder builder, ref int sequence, string folder, string? fileName, string alt, bool showCaption = true)
    {
        builder.OpenElement(sequence++, "div");
        builder.AddAttribute(sequence++, "class", "contract-logo");
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            builder.OpenElement(sequence++, "img");
            builder.AddAttribute(sequence++, "src", Assets[$"{folder}/{fileName}"]);
            builder.AddAttribute(sequence++, "alt", alt);
            builder.CloseElement();
        }
        if (showCaption)
        {
            builder.OpenElement(sequence++, "span");
            builder.AddContent(sequence++, alt);
            builder.CloseElement();
        }
        builder.CloseElement();
    }

    private async Task PrintAsync() => await _js.InvokeVoidAsync("APP.printReport", string.Empty, "portrait");
    private async Task ExportPdfAsync() => await _js.InvokeVoidAsync("APP.printReport", $"Student-contract-{student?.School?.Code}-{student?.SN:D5}", "portrait");
}
