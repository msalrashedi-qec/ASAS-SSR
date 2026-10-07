namespace Web.Components.Pages.Business;

public partial class ParentReceipts
{
    [Parameter] public Guid ParentId { get; set; }

    private Parent? parent;
    private bool isLoading;
    private string searchText = string.Empty;
    private List<ReceiptRow> receipts = [];

    private List<ReceiptRow> FilteredReceipts => string.IsNullOrWhiteSpace(searchText)
        ? receipts
        : receipts.Where(x => x.Number.ToString(CultureInfo.InvariantCulture).Contains(searchText.Trim(), StringComparison.OrdinalIgnoreCase)
            || x.StudentName.Contains(searchText.Trim(), StringComparison.OrdinalIgnoreCase)
            || x.StudentCode.Contains(searchText.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

    protected override async Task OnParametersSetAsync()
    {
        isLoading = true;
        try
        {
            await _appStateService.InitializeAsync();
            if (_appStateService.SchoolId == Guid.Empty)
            {
                _navigator.NavigateTo("select-school");
                return;
            }

            parent = await _uow.Parents.FindAsync(x => x.Id == ParentId);
            if (parent is null) return;

            var entities = await _uow.Receipts.FindAllAsync(x => x.Contract.Student.ParentId == ParentId && x.Contract.Student.SchoolId == _appStateService.SchoolId
            && x.Contract.Year.IsCurrent
            , ["Contract.Student.School", "PaymentMethod", "ReceiptDetails"]);

            receipts = entities.OrderByDescending(x => x.Date).ThenByDescending(x => x.SN)
                .Select(x => new ReceiptRow(x.Id, x.SN, x.Date,
                    $"{x.Contract.Student.Name} {x.Contract.Student.FatherName}".Trim(),
                    $"{x.Contract.Student.School.Code}-{x.Contract.Student.SN:D5}",
                    Localized(x.PaymentMethod.Name, x.PaymentMethod.NameAr),
                    Math.Round(x.ReceiptDetails.Sum(d => d.Amount), 2))).ToList();
        }
        catch (Exception ex)
        {
            parent = null;
            _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], ex.Message);
        }
        finally
        {
            isLoading = false;
        }
    }

    private string Localized(string? english, string? arabic) =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar" && !string.IsNullOrWhiteSpace(arabic)
            ? arabic : english ?? arabic ?? string.Empty;

    private sealed record ReceiptRow(Guid Id, int Number, DateTime Date, string StudentName, string StudentCode, string PaymentMethod, double Total);
}
