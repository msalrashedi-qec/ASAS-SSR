namespace Shared.Dtos.Account
{
    public class RoleDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; }
        public string NormalizedName { get; set; }
        public string ConcurrencyStamp { get; set; }
        public string Description { get; set; } = string.Empty;
        public int UserCount { get; set; }
        public List<string> Claims { get; set; } = new();
        public List<PermissionDto> Permissions { get; set; } = new();
        public List<UserDto> UsersInRole { get; set; } = new();
    }
}
