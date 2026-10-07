namespace Shared.Dtos.Account
{
    public class ApplicationUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string DefaultLanguage { get; set; } = "ar-EG";
        public DateTime? AuthFactorStartDate { get; set; }
        public DateTime? SuspenseDate { get; set; }
        public bool IsActive { get; set; } = true;
        public bool EmailConfirmed { get; set; } = true;
        public bool PhoneNumberConfirmed { get; set; } = true;
        public bool TwoFactorEnabled { get; set; }
        public DateTime CreatedOn { get; set; }

        public IList<string> GrantedRoles { get; set; } = new List<string>();
        public IList<Guid> GrantedSchools { get; set; } = new List<Guid>();
    }
}
