using Core.Entities.General;

namespace Web.Components.Pages.Reports
{
    public partial class ReportsIndex
    {
        private IEnumerable<Report> reports = [];
        private int index = 1;
        override protected async Task OnInitializedAsync()
        {
            index = 1;
            string userId = await _userService.GetUserIdAsync();
            var roleIds = (await _uow.UserRoles.FindAllAsync(x => x.UserId == userId))
                .Select(x => x.RoleId)
                .ToList();

            if (roleIds.Count == 0)
                return;

            reports = (await _uow.RoleReports.FindAllAsync(
                    x => roleIds.Contains(x.RoleId),
                    ["Report"]))
                .Select(x => x.Report)
                .GroupBy(x => x.Id)
                .Select(x => x.First())
                .OrderBy(x => x.SN)
                .ToList();
            if (reports.Any(x => x.Url == "students") && !reports.Any(x => x.Url == "monthly-students"))
                reports = reports.Append(new Report { Url = "monthly-students", NameAr = "إحصائية أعداد الطلاب في كل شهر", Description = "أعداد الطلاب الشهرية والتسجيل للعام القادم حسب المدرسة والفترة" });
        }

    }
}
