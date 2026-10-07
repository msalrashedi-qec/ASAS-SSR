
using Core.Entities.Business;

namespace Core.Entities.Account
{
    public class UserSchool
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public Guid SchoolId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }

        public virtual ApplicationUser User { get; set; } = null!;
        public virtual School School { get; set; } = null!;
    }
}
