using Core.Entities.General;

namespace Web.Components.Pages.Messages;

public partial class CustomMessages
{
    [Inject] 
    private ISmsSender SmsSender { get; set; } = null!;

    private readonly HashSet<Guid> selectedStudentIds = [];
    private List<StudentMessageRow> rows = [];
    private List<StudentStatus> statuses = [];
    private List<Nationality> nationalities = [];
    private List<Gender> genders = [];
    private List<District> districts = [];
    private List<SchoolClass> classes = [];
    private School? school;
    private string studentName = string.Empty;
    private IList<byte> statusIds = [], genderIds = [];
    private IList<int> nationalityIds = [], districtIds = [], classIds = [];
    private string messageText = string.Empty;
    private bool isLoading;
    private bool isSending;
    private bool IsArabic => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

    private IEnumerable<StudentMessageRow> FilteredRows => rows
        .Where(x => string.IsNullOrWhiteSpace(studentName) || x.Name.Contains(studentName.Trim(), StringComparison.CurrentCultureIgnoreCase))
        .Where(x => statusIds is null || statusIds.Count() == 0 || statusIds.Contains(x.StatusId))
        .Where(x => nationalityIds is null || nationalityIds.Count() == 0 || nationalityIds.Contains(x.NationalityId))
        .Where(x => genderIds is null || genderIds.Count() == 0 || genderIds.Contains(x.GenderId))
        .Where(x => districtIds is null || districtIds.Count() == 0 || districtIds.Contains(x.DistrictId))
        .Where(x => classIds is null || classIds.Count() == 0 || classIds.Contains(x.ClassId))
        .OrderBy(x => x.Name);
    private IEnumerable<StudentMessageRow> SelectableFilteredRows => FilteredRows.Where(x => x.HasMobile);
    private bool AreAllVisibleSelected => SelectableFilteredRows.Any() && SelectableFilteredRows.All(x => selectedStudentIds.Contains(x.Id));
    private int UniqueRecipientCount => rows.Where(x => selectedStudentIds.Contains(x.Id) && x.HasMobile).Select(x => Digits(x.Mobile)).Distinct().Count();
    private bool CanSend => !isSending && selectedStudentIds.Count > 0 && !string.IsNullOrWhiteSpace(messageText);

    protected override async Task OnInitializedAsync()
    {
        await _appStateService.InitializeAsync();
        if (_appStateService.SchoolId == Guid.Empty)
        {
            _navigator.NavigateTo("select-school");
            return;
        }
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        isLoading = true;
        try
        {
            school = await _uow.Schools.FindAsync(x => x.Id == _appStateService.SchoolId);
            statuses = (await _uow.StudentStatuses.GetAllAsync()).OrderBy(x => x.Id).ToList();
            nationalities = (await _uow.Nationalities.GetAllAsync()).OrderBy(x => Localized(x.Name, x.NameAr)).ToList();
            genders = (await _uow.Genders.FindAllAsync(x=> x.Id < 10)).OrderBy(x => x.Id).ToList();
            districts = (await _uow.Districts.FindAllAsync(x => x.CityId == school.CityId)).OrderBy(x => Localized(x.Name, x.NameAr)).ToList();
            classes = (await _uow.SchoolClasses.FindAllAsync(x => x.Level.SchoolId == school.Id, new[] { "Level" })).OrderBy(x => x.Level.SN).ThenBy(x => x.SN).ToList();

            var students = await _uow.Students.FindAllAsync(x => x.SchoolId == school.Id,
                new[] { "School", "Parent", "Gender", "Nationality", "District", "CurrentContract", "CurrentContract.Status", "CurrentContract.Class" });
            rows = students.Select(x => new StudentMessageRow(
                x.Id, $"{x.Name} {x.FatherName}".Trim(), $"{x.School.Code}-{x.SN:D5}",
                x.CurrentContract.StatusId, Localized(x.CurrentContract.Status.Name, x.CurrentContract.Status.NameAr), x.CurrentContract.Status.ColorCode,
                x.NationalityId, Localized(x.Nationality.Name, x.Nationality.NameAr), x.GenderId, Localized(x.Gender.Name, x.Gender.NameAr),
                x.DistrictId, Localized(x.District.Name, x.District.NameAr), x.CurrentContract.ClassId,
                ClassLabel(x.CurrentContract.ClassId), FirstMobile(x.ContactMobile, x.Parent.Mobile))).ToList();
        }
        catch (Exception ex)
        {
            _toastService.Notify(NotificationSeverity.Error, T("Error", "خطأ"), ex.Message);
        }
        finally { isLoading = false; }
    }

    private void ToggleStudent(StudentMessageRow row)
    {
        if (!row.HasMobile || isSending) return;
        if (!selectedStudentIds.Add(row.Id)) selectedStudentIds.Remove(row.Id);
    }

    private void ToggleAllVisible(ChangeEventArgs args)
    {
        var select = args.Value is bool value && value;
        foreach (var row in SelectableFilteredRows.ToList())
            if (select) selectedStudentIds.Add(row.Id); else selectedStudentIds.Remove(row.Id);
    }

    private void ClearFilters()
    {
        studentName = string.Empty;
        statusIds.Clear();
        nationalityIds.Clear();
        genderIds.Clear();
        districtIds.Clear();
        classIds.Clear();
    }

    private async Task SendAsync()
    {
        if (!CanSend || school is null) return;
        var recipients = rows.Where(x => selectedStudentIds.Contains(x.Id) && x.HasMobile).Select(x => x.Mobile).ToArray();

        var confirmed = await _dialogService.Confirm(
            string.Format(T("Send this message to {0} unique mobile numbers?", "هل تريد بالتأكيد إرسال هذه الرسالة؟"), UniqueRecipientCount),
            T("Confirm sending", "تأكيد الإرسال"),
            new ConfirmOptions
            {
                OkButtonText = T("Send", "إرسال"),
                CancelButtonText = T("Cancel", "إلغاء")
            });
        if (confirmed != true) return;

        isSending = true;
        try
        {
            var result = await SmsSender.SendAsync(new SmsSendRequest(school.SmsSender, school.SmsUserName, school.SmsPassword, recipients, messageText));
            if (!result.IsSuccess)
            {
                _toastService.Notify(NotificationSeverity.Error, T("Message not sent", "لم يتم إرسال الرسالة"), result.ErrorMessage ?? T("An unknown SMS provider error occurred.", "حدث خطأ غير معروف لدى مزود الرسائل."));
                return;
            }

            _toastService.Notify(NotificationSeverity.Success, T("Message sent", "تم إرسال الرسالة"), string.Format(T("The message was sent to {0} mobile numbers.", "تم إرسال الرسالة إلى {0} أرقام جوال."), result.RecipientCount));
            selectedStudentIds.Clear();
            messageText = string.Empty;
        }
        catch (Exception)
        {
            _toastService.Notify(NotificationSeverity.Error, T("Message not sent", "لم يتم إرسال الرسالة"), T("An unexpected error occurred while sending the message.", "حدث خطأ غير متوقع أثناء إرسال الرسالة."));
        }
        finally { isSending = false; }
    }

    private string Localized(string english, string arabic) => IsArabic ? arabic : english;
    private string ClassLabel(int id) => classes.FirstOrDefault(x => x.Id == id) is { } item ? $"{Localized(item.Level.Name, item.Level.NameAr)} - {Localized(item.Name, item.NameAr)}" : string.Empty;
    private string T(string english, string arabic) => IsArabic ? arabic : english;
    private IEnumerable<FilterOption<byte>> StatusOptions => statuses.Select(x => new FilterOption<byte>(x.Id, Localized(x.Name, x.NameAr)));
    private IEnumerable<FilterOption<int>> NationalityOptions => nationalities.Select(x => new FilterOption<int>(x.Id, Localized(x.Name, x.NameAr)));
    private IEnumerable<FilterOption<byte>> GenderOptions => genders.Select(x => new FilterOption<byte>(x.Id, Localized(x.Name, x.NameAr)));
    private IEnumerable<FilterOption<int>> DistrictOptions => districts.Select(x => new FilterOption<int>(x.Id, Localized(x.Name, x.NameAr)));
    private IEnumerable<FilterOption<int>> ClassOptions => classes.Select(x => new FilterOption<int>(x.Id, ClassLabel(x.Id)));
    private sealed record FilterOption<T>(T Id, string Name);
    private static string FirstMobile(string? contactMobile, string? parentMobile) => !string.IsNullOrWhiteSpace(contactMobile) ? contactMobile.Trim() : parentMobile?.Trim() ?? string.Empty;
    private static string Digits(string value) => new(value.Where(char.IsDigit).ToArray());

    private sealed record StudentMessageRow(Guid Id, string Name, string Number, byte StatusId, string Status, string StatusColor,
        int NationalityId, string Nationality, byte GenderId, string Gender, int DistrictId, string District, int ClassId, string Class, string Mobile)
    {
        public bool HasMobile => !string.IsNullOrWhiteSpace(Mobile);
    }
}
