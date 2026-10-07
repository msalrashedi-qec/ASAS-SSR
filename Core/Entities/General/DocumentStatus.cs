using Core.Entities.Business;

namespace Core.Entities.General
{
    public class DocumentStatus
    {
        public byte Id { get; set; }
        public string Name { get; set; }
        public string NameAr { get; set; }
        public string ColorCode { get; set; }

        public virtual ICollection<StudentDocument> Documents { get; set; } = new List<StudentDocument>();
    }
}
