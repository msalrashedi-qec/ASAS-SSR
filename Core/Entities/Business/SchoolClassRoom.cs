using Core.Entities.General;

using Core.Entities.Account;

namespace Core.Entities.Business
{
    public class SchoolClassRoom : GuidEntityBase
    {
        public string Name { get; set; }
        public string NameAr { get; set; }
        public int ClassId { get; set; }
        public byte GenderId { get; set; }
        public int Capacity { get; set; }
        public string? ResponsibleTeacherId { get; set; }

        public virtual SchoolClass Class { get; set; } = null!;
        public virtual Gender Gender { get; set; } = null!;
        public virtual ApplicationUser? ResponsibleTeacher { get; set; }
        public virtual ICollection<StudentClassRoomAssignment> StudentAssignments { get; set; } = new List<StudentClassRoomAssignment>();

    }
}
