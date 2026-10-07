using Core.Entities.General;

namespace Web.Components.Pages.Messages;

public partial class FullDebitClaims
{
    [Inject] private ISmsSender SmsSender { get; set; } = null!;
    private readonly HashSet<Guid> selectedStudentIds = [];
    private List<DebitClaimRow> rows = [];
    private List<StudentStatus> statuses = [];
    private List<Nationality> nationalities = [];
    private List<Gender> genders = [];
    private List<SchoolClass> classes = [];
    private List<Semester> semesters = [];
    private List<Student> students = [];
    private List<StudentAccount> accounts = [];
    private School? school;
    private string parentName = string.Empty;
    private double? minimumDebit;
    private IList<byte> statusIds = [], genderIds = [], semesterIds = [];
    private IList<int> nationalityIds = [], classIds = [];
    private bool isLoading, isSending;
    private bool IsArabic => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

    private IEnumerable<DebitClaimRow> FilteredRows => rows
        .Where(x => string.IsNullOrWhiteSpace(parentName) || x.ParentName.Contains(parentName.Trim(), StringComparison.CurrentCultureIgnoreCase))
        .Where(x => !minimumDebit.HasValue || x.ClaimedAmount > minimumDebit.Value)
        .Where(x => statusIds.Count == 0 || statusIds.Contains(x.StatusId)).Where(x => nationalityIds.Count == 0 || nationalityIds.Contains(x.NationalityId))
        .Where(x => genderIds.Count == 0 || genderIds.Contains(x.GenderId)).Where(x => classIds.Count == 0 || classIds.Contains(x.ClassId))
        .OrderByDescending(x => x.ClaimedAmount).ThenBy(x => x.StudentName);
    private IEnumerable<DebitClaimRow> SelectableFilteredRows => FilteredRows.Where(x => x.HasMobile);
    private List<DebitClaimRow> SelectedRows => rows.Where(x => selectedStudentIds.Contains(x.Id) && x.HasMobile).ToList();
    private bool AreAllVisibleSelected => SelectableFilteredRows.Any() && SelectableFilteredRows.All(x => selectedStudentIds.Contains(x.Id));
    private double SelectedTotal => SelectedRows.Sum(x => x.ClaimedAmount);
    private bool CanSend => !isSending && SelectedRows.Count > 0;

    protected override async Task OnInitializedAsync()
    {
        await _appStateService.InitializeAsync();
        if (_appStateService.SchoolId == Guid.Empty) { _navigator.NavigateTo("select-school"); return; }
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        isLoading = true;
        try
        {
            school = await _uow.Schools.FindAsync(x => x.Id == _appStateService.SchoolId);
            if (school is null) return;
            statuses = (await _uow.StudentStatuses.GetAllAsync()).OrderBy(x => x.Id).ToList();
            nationalities = (await _uow.Nationalities.GetAllAsync()).OrderBy(x => Localized(x.Name, x.NameAr)).ToList();
            genders = (await _uow.Genders.FindAllAsync(x => x.Id < 10)).OrderBy(x => x.Id).ToList();
            semesters = (await _uow.Semesters.GetAllAsync()).OrderBy(x => x.Id).ToList();
            classes = (await _uow.SchoolClasses.FindAllAsync(x => x.Level.SchoolId == school.Id, ["Level"])).OrderBy(x => x.Level.SN).ThenBy(x => x.SN).ToList();
            students = (await _uow.Students.FindAllAsync(x => x.SchoolId == school.Id, ["School", "Parent", "Gender", "Nationality", "CurrentContract", "CurrentContract.Status", "CurrentContract.Class"])).ToList();
            var studentIds = students.Select(x => x.Id).ToArray();
            accounts = studentIds.Length == 0 ? [] : (await _uow.StudentAccounts.FindAllAsync(x => studentIds.Contains(x.Contract.StudentId), ["Contract"])).ToList();
            BuildRows();
        }
        catch (Exception ex) { _toastService.Notify(NotificationSeverity.Error, T("Error", "خطأ"), ex.Message); }
        finally { isLoading = false; }
    }

    private void BuildRows()
    {
        var amounts = accounts.Where(x => semesterIds.Count == 0 || semesterIds.Contains(x.SemesterId)).GroupBy(x => x.Contract.StudentId).ToDictionary(x => x.Key, x => Math.Round(x.Sum(a => a.Debit - a.Credit), 2));
        rows = students.Select(x => new DebitClaimRow(x.Id, $"{x.Name} {x.FatherName}".Trim(), $"{x.School.Code}-{x.SN:D5}", x.Parent.Name,
            x.CurrentContract.StatusId, Localized(x.CurrentContract.Status.Name, x.CurrentContract.Status.NameAr), x.CurrentContract.Status.ColorCode,
            x.NationalityId, Localized(x.Nationality.Name, x.Nationality.NameAr), x.GenderId, Localized(x.Gender.Name, x.Gender.NameAr),
            x.CurrentContract.ClassId, ClassLabel(x.CurrentContract.ClassId), x.Parent.Mobile?.Trim() ?? string.Empty, amounts.GetValueOrDefault(x.Id)))
            .Where(x => x.ClaimedAmount > 0).ToList();
        selectedStudentIds.RemoveWhere(id => rows.All(x => x.Id != id));
    }

    private void SemesterChanged(object? _) => BuildRows();
    private void ToggleStudent(DebitClaimRow row) { if (!row.HasMobile || isSending) return; if (!selectedStudentIds.Add(row.Id)) selectedStudentIds.Remove(row.Id); }
    private void ToggleAllVisible(ChangeEventArgs args) { var select = args.Value is bool value && value; foreach (var row in SelectableFilteredRows.ToList()) if (select) selectedStudentIds.Add(row.Id); else selectedStudentIds.Remove(row.Id); }
    private void ClearFilters() { parentName = string.Empty; minimumDebit = null; statusIds.Clear(); nationalityIds.Clear(); genderIds.Clear(); classIds.Clear(); semesterIds.Clear(); BuildRows(); }

    private async Task SendClaimsAsync()
    {
        if (!CanSend || school is null) return;
        var claims = SelectedRows;
        var confirmed = await _dialogService.Confirm(string.Format(T("Send {0} debit claims?", "هل تريد إرسال {0} مطالبة؟"), claims.Count, FormatAmount(SelectedTotal)), T("Confirm sending", "تأكيد الإرسال"), new ConfirmOptions { OkButtonText = T("Send", "إرسال"), CancelButtonText = T("Cancel", "إلغاء") });
        if (confirmed != true) return;
        isSending = true;
        var sent = 0;
        try
        {
            foreach (var claim in claims)
            {
                var result = await SmsSender.SendAsync(new SmsSendRequest(school.SmsSender, school.SmsUserName, school.SmsPassword, [claim.Mobile], ClaimMessage(claim)));
                if (result.IsSuccess) sent++;
            }
            if (sent == claims.Count) { _toastService.Notify(NotificationSeverity.Success, T("Claims sent", "تم إرسال المطالبات"), string.Format(T("{0} debit claims were sent successfully.", "تم إرسال {0} مطالبة مديونية بنجاح."), sent)); selectedStudentIds.Clear(); }
            else _toastService.Notify(NotificationSeverity.Warning, T("Partially sent", "تم الإرسال جزئيًا"), string.Format(T("{0} of {1} claims were sent.", "تم إرسال {0} من أصل {1} مطالبة."), sent, claims.Count));
        }
        catch { _toastService.Notify(NotificationSeverity.Error, T("Claims not sent", "لم يتم إرسال المطالبات"), T("An unexpected error occurred while sending the claims.", "حدث خطأ غير متوقع أثناء إرسال المطالبات.")); }
        finally { isSending = false; }
    }

    private string ClaimMessage(DebitClaimRow row) => $"ولي الأمر {row.ParentName}، نود تذكيركم بأن المبلغ المستحق على الطالب {row.StudentName} هو {FormatAmount(row.ClaimedAmount)}. شكرًا لكم.";
    private string FormatAmount(double value) => ReportFormatter.Amount(value);
    private string Localized(string english, string arabic) => IsArabic ? arabic : english;
    private string ClassLabel(int id) => classes.FirstOrDefault(x => x.Id == id) is { } item ? $"{Localized(item.Level.Name, item.Level.NameAr)} - {Localized(item.Name, item.NameAr)}" : string.Empty;
    private string T(string english, string arabic) => IsArabic ? arabic : english;
    private IEnumerable<FilterOption<byte>> StatusOptions => statuses.Select(x => new FilterOption<byte>(x.Id, Localized(x.Name, x.NameAr)));
    private IEnumerable<FilterOption<int>> NationalityOptions => nationalities.Select(x => new FilterOption<int>(x.Id, Localized(x.Name, x.NameAr)));
    private IEnumerable<FilterOption<byte>> GenderOptions => genders.Select(x => new FilterOption<byte>(x.Id, Localized(x.Name, x.NameAr)));
    private IEnumerable<FilterOption<int>> ClassOptions => classes.Select(x => new FilterOption<int>(x.Id, ClassLabel(x.Id)));
    private IEnumerable<FilterOption<byte>> SemesterOptions => semesters.Select(x => new FilterOption<byte>(x.Id, Localized(x.Name, x.NameAr)));
    private sealed record FilterOption<T>(T Id, string Name);
    private sealed record DebitClaimRow(Guid Id, string StudentName, string StudentNumber, string ParentName, byte StatusId, string Status, string StatusColor, int NationalityId, string Nationality, byte GenderId, string Gender, int ClassId, string Class, string Mobile, double ClaimedAmount) { public bool HasMobile => !string.IsNullOrWhiteSpace(Mobile); }
}
