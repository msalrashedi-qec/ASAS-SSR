
using Core.Entities.Business;

namespace Core.Entities.General
{
    public class DocumentType : EntityBase
    {
        public string Name { get; set; }
        public string NameAr { get; set; }

        public virtual ICollection<StudentDocument> Documents { get; set; } = new List<StudentDocument>();
    }
}
