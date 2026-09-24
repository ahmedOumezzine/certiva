using Certiva.Areas.Admin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Certiva.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class DashboardController : Controller
{
    private readonly IExamAdminService _exams;

    public DashboardController(IExamAdminService exams)
    {
        _exams = exams;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _exams.GetDashboardAsync());
    }
}
