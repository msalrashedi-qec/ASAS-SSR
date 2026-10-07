using Core.Entities.Account;

namespace Core.Entities.General
{
    public class ErrorLog
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public int PageId { get; set; }
        public string IpAddress { get; set; }
        public string ErrorDetails { get; set; }
        public DateTime LoggedOn { get; set; }

        public virtual ApplicationUser? User { get; set; }
        public virtual SystemPage? Page { get; set; } = null!;
    }
}
