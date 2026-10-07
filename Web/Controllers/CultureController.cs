using Microsoft.AspNetCore.Localization;
using MvcRoute = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace Web.Controllers
{
    [MvcRoute("culture/[action]")]
    public class CultureController : ControllerBase
    {
        [HttpGet]
        public IActionResult SetCulture(string culture, string returnUrl)
        {
            if (!string.IsNullOrEmpty(culture))
            {
                var requestCulture = new RequestCulture(culture, culture);
                var cookieName = CookieRequestCultureProvider.DefaultCookieName;
                var cookieValue = CookieRequestCultureProvider.MakeCookieValue(requestCulture);
                var cookieOptions = new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    Path = "/",
                    HttpOnly = false,
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax
                };
                HttpContext.Response.Cookies.Append(cookieName, cookieValue, cookieOptions);
            }
            return LocalRedirect(returnUrl);
        }
    }
}
