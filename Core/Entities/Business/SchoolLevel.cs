namespace Core.Entities.Business
{
    public class SchoolLevel : EntityBase
    {
        public string Name { get; set; }
        public string NameAr { get; set; }
        public Guid SchoolId { get; set; }
        public int SN { get; set; }

        public virtual School School { get; set; } = null!;

        public virtual ICollection<SchoolClass> Classes { get; set; } = new List<SchoolClass>();
    }
}
