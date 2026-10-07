namespace Shared.Dtos
{
    public class PagedListResponse<T>
    {
        public IEnumerable<T> DataList { get; set; }
        public int RowsCount { get; set; }
    }
}
