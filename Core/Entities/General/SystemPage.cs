using Core.Entities.Account;

namespace Core.Entities.General
{
    public class SystemPage
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string NameAr { get; set; } = null!;

        ////////////// Tables Relationship /////////////////
        public virtual ICollection<UserAudit> UserAudits { get; set; } = new List<UserAudit>();
        public virtual ICollection<ErrorLog> ErrorLogs { get; set; } = new List<ErrorLog>();

    }
}
