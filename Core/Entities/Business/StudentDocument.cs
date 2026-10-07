using Core.Entities.General;

namespace Core.Entities.Business
{
    public class StudentDocument : GuidEntityBase
    {
        public Guid StudentId { get; set; }
        public int YearId { get; set; }
        public int TypeId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public byte StatusId { get; set; }

        public virtual Student Student { get; set; } = null!; 
        public virtual Year Year { get; set; } = null!;
        public virtual DocumentType Type { get; set; } = null!;
        public virtual DocumentStatus Status { get; set; } = null!;

    }
}
