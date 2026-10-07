
using Core.Entities.General;

namespace Core.Entities.Account
{
    public class UserAudit
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public int PageId { get; set; }
        public int ActionId { get; set; }
        public string? IpAddress { get; set; } = null!;
        public string? OldValue { get; set; } = null!;
        public string? NewValue { get; set; } = null!;
        public string? FeildName { get; set; }
        public string? Comments { get; set; }
        public DateTime CreatedOn { get; set; }

        public virtual SystemAction Action { get; set; } = null!;
        public virtual SystemPage Page { get; set; } = null!;
        public virtual ApplicationUser User { get; set; } = null!;

    }
}
