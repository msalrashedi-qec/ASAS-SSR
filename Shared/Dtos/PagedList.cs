namespace Shared.Dtos
{
    public class PagedList<T>
    {
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public T? Filter { get; set; }
    }
}
