namespace Shared.Dtos.Business
{
    public class StudentListDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid ParentId { get; set; }
    }
}
