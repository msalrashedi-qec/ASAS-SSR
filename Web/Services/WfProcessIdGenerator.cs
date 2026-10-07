namespace Web.Services
{
    public class WfProcessIdGenerator
    {
        public static async Task<string> GetProcessId(IUnitOfWork _uow)
        {
            DateTime firstDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            return $"{DateTime.UtcNow.GetKsaDateTime().ToString("yy")}{DateTime.UtcNow.GetKsaDateTime().ToString("MM")}-{await _uow.WorkflowProcesses.CountAsync(x => x.CreatedOn >= firstDate) + 1}";
        }
    }
}
