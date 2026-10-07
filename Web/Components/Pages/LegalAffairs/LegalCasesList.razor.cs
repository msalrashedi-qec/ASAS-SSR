namespace Web.Components.Pages.LegalAffairs
{
    public partial class LegalCasesList
    {
        [Inject]
        public IConfiguration Configuration { get; set; }


        private string url = string.Empty;

        protected override async Task OnInitializedAsync()
        {
            var userId = await _userService.GetUserIdAsync();

            url = $"{Configuration["LegalAffairs:BaseUrl"]}/school-cases/{userId}";
        }

    }
}