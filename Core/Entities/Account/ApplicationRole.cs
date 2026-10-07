
using Core.Entities.Workflow;
using Microsoft.AspNetCore.Identity;

namespace Core.Entities.Account
{
    public class ApplicationRole : IdentityRole
    {

        public virtual ICollection<WorkflowTask> WorkflowTasks { get; set; } = new List<WorkflowTask>();
    }
}
