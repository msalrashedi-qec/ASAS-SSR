
using Core.Entities.General;

namespace Core.Entities.Account
{
    public class RoleReport
    {
        public Guid Id { get; set; }
        public string RoleId { get; set; }
        public int ReportId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }

        public virtual Report Report { get; set; } = null!;
    }
}
