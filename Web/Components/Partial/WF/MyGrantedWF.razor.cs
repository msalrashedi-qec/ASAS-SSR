namespace Web.Components.Partial.WF
{
    public partial class MyGrantedWF
    {
        public IEnumerable<ReportDto> permittedRequests;
        private bool isLoading = false;

        protected async Task GetList()
        {
            try
            {
                isLoading = true;
                //var permittedRequests = await _uow.RoleWorkflows.FindAllAsync(x =>true, new[] { "WorkflowType" }, x => x.WorkflowType.SN);
                //var list = permittedRequests.Select(x => new { x.WorkflowType.Name, x.WorkflowType.NameAr, x.WorkflowType.Url, x.WorkflowType.Icon }).Distinct().ToList();
                //List<ReportDto> requests = new List<ReportDto>();
                //foreach (var request in list)
                //{
                //    requests.Add(new ReportDto
                //    {
                //        Name = request.Name,
                //        Icon = request.Icon,
                //        Url = request.Url,
                //    });
                //}
            }
            catch (Exception ex)
            {
                _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer[ex.Message]);
            }
            finally
            {
                isLoading = false;
            }
        }

        void NavigateTo(string page)
        {
            _navigator.NavigateTo(page);
        }
    }
}