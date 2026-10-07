namespace Shared.Dtos.Workflow
{
    public class WfRequestDto<T> where T : class
    {
        public T RequestDetails { get; set; }
        public IEnumerable<WorkflowHistoryDto> RequestHistory { get; set; }
    }
}
