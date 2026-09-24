using Certiva.Domain.Enums;
using Certiva.Areas.Admin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Certiva.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class AttemptsController : Controller
{
    private readonly IAdminAttemptService _service;

    public AttemptsController(IAdminAttemptService service) => _service = service;

    public async Task<IActionResult> Index(int page = 1, ExamAttemptStatus? status = null, Guid? examId = null, CancellationToken cancellationToken = default)
    {
        return View(await _service.GetAttemptsAsync(page, status, examId, cancellationToken));
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken = default)
    {
        var model = await _service.GetAttemptAsync(id, cancellationToken);
        return model == null ? NotFound() : View(model);
    }
}
