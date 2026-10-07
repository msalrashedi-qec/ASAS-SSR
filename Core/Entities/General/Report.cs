
using Core.Entities.Account;

namespace Core.Entities.General
{
    public class Report
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string NameAr { get; set; }
        public string Description { get; set; }
        public string DescriptionAr { get; set; }
        public string Url { get; set; }
        public string Icon { get; set; }
        public int SN { get; set; }

        public virtual ICollection<RoleReport> RoleReports { get; set; } = new HashSet<RoleReport>();

    }
}
