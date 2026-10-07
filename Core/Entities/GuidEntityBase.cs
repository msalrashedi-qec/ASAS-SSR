
namespace Core.Entities
{
    public class GuidEntityBase
    {
        public Guid Id { get; set; }
        public string? Comments { get; set; }
        public bool IsDeleted { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }

    }
}
