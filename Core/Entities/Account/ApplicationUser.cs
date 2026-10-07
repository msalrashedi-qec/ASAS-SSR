using Core.Entities.Business;
using Core.Entities.Workflow;
using Microsoft.AspNetCore.Identity;

namespace Core.Entities.Account
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; }
        public string DefaultLanguage { get; set; } = "ar-EG";
        public string Avatar { get; set; }
        public Guid CurrentSchoolId { get; set; }
        public DateTime? AuthFactorStartDate { get; set; }
        public DateTime? SuspenseDate { get; set; }
        public string OTP { get; set; } = string.Empty;
        public DateTime? OTPSentAt { get; set; }
        public bool IsActive { get; set; } = true;
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }

        public virtual ICollection<UserSchool> UserSchools { get; set; } = new List<UserSchool>();
        public virtual ICollection<Receipt> Receipts { get; set; } = new List<Receipt>();
        public virtual ICollection<RefundReceipt> RefundReceipts { get; set; } = new List<RefundReceipt>();
        public virtual ICollection<WorkflowFutureSharing> WfFutureSharingsFromUsers { get; set; } = new List<WorkflowFutureSharing>();
        public virtual ICollection<WorkflowFutureSharing> WfFutureSharingsWithUsers { get; set; } = new List<WorkflowFutureSharing>();
        
    }

}
