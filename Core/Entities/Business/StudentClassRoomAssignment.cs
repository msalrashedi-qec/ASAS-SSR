namespace Core.Entities.Business
{
    public class StudentClassRoomAssignment : GuidEntityBase
    {
        public Guid ContractId { get; set; }
        public Guid ClassRoomId { get; set; }

        public virtual StudentContract Contract { get; set; } = null!;
        public virtual SchoolClassRoom ClassRoom { get; set; } = null!;
    }
}
