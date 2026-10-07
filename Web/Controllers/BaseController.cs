using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Web.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BaseController : ControllerBase
    {
        protected Guid GetUserId()
        {
            if (User.FindFirstValue(ClaimTypes.NameIdentifier) != null)
                return Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            else return Guid.Empty;
        }
        protected bool ValidateGuid(string guid)
        {
            return Guid.TryParse(guid, out var _);
        }
    }
}
