
namespace Web.Components.Pages.LegalAffairs
{
    public partial class StudentCases
    {
        [Inject]
        public IConfiguration Configuration { get; set; }


        [Parameter]
        public string? Id { get; set; }

        private string url = string.Empty;
        private string submitUrl = string.Empty;

        private Student student = new();
        private List<StudentAccount> studentAccounts = [];
        private bool IsArabic => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";
        private string StudentStatus => student?.CurrentStatus is null
        ? "-"
        : IsArabic ? student.CurrentStatus.NameAr : student.CurrentStatus.Name;
        private string StudentLevelAndClass => student?.CurrentContract?.Class is null
            ? "-"
            : IsArabic
                ? $"{student.CurrentContract.Class.Level.NameAr} - {student.CurrentContract.Class.NameAr}"
                : $"{student.CurrentContract.Class.Level.Name} - {student.CurrentContract.Class.Name}";
        private decimal RemainingBalance => decimal.Round(studentAccounts.Sum(x => (decimal)x.Debit - (decimal)x.Credit), 2);
        private bool isLoading = false;
        
        private async Task LoadStudentAsync()
        {
            try
            {
                studentAccounts = [];

                student = await _uow.Students.FindAsync(x => x.Id == Guid.Parse(Id),
                    ["CurrentContract.Class", "CurrentContract.Class.Level", "CurrentStatus"]);

                if (student is null)
                    return;

                studentAccounts = (await _uow.StudentAccounts.FindAllAsync(x => x.ContractId == student.CurrentContractId)).ToList();
            }
            catch (Exception ex)
            {
                _toastService.Notify(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = _localizer["Error"], Detail = ex.Message });
            }
            finally
            {
                isLoading = false;
            }
        }


        override protected async Task OnInitializedAsync()
        {
            await LoadStudentAsync();
            var userId = await _userService.GetUserIdAsync();

            url = $"{Configuration["LegalAffairs:BaseUrl"]}/student-cases/{Id}/{userId}";
            submitUrl = $"{Configuration["LegalAffairs:BaseUrl"]}/create-student-case/{Id}/{userId}";
        }

    }
}