
using Shared.Dtos.Account;
using Shared.Dtos.General;

namespace Shared.Dtos.Workflow
{
    public class WfPermissionsDto
    {
        public BasicDto WfTask { get; set; } = new();
        public List<PermissionDto> Permissions { get; set; } = new();
    }
}
