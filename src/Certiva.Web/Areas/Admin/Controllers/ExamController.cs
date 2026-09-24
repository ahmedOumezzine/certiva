using Certiva.Areas.Admin.Services;
using Certiva.Areas.Admin.ViewModels;
using Certiva.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Certiva.Localization;
using Microsoft.Extensions.Localization;

namespace Certiva.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class ExamController : Controller
{
    private readonly IExamAdminService _service;
    private readonly IStringLocalizer<SharedResource> _text;

    public ExamController(IExamAdminService service, IStringLocalizer<SharedResource> text)
    {
        _service = service;
        _text = text;
    }

    public async Task<IActionResult> Index(string? search, Status? status, int page = 1, int pageSize = AdminPagination.DefaultPageSize)
    {
        return View(await _service.GetExamsAsync(search, status, page, pageSize));
    }

    public IActionResult Create()
    {
        return View("Edit", new ExamEditViewModel());
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var model = await _service.GetExamEditAsync(id);
        return model == null ? NotFound() : View(model);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var model = await _service.GetExamDetailsAsync(id);
        return model == null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(ExamEditViewModel model)
    {
        if (!ModelState.IsValid)
            return View("Edit", model);

        var proxy = new ModelStateDictionaryProxy(ModelState.AddModelError);
        var saved = await _service.SaveExamAsync(model, proxy);
        if (!saved && !proxy.HasErrors)
            ModelState.AddModelError(string.Empty, _text["Exam.SaveFailed"]);

        if (!saved || !ModelState.IsValid)
            return View("Edit", model);

        TempData["Success"] = _text["Exam.Saved"].Value;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deletedOrArchived = await _service.DeleteExamAsync(id);
        TempData[deletedOrArchived ? "Success" : "Error"] = deletedOrArchived
            ? _text["Exam.DeletedOrArchived"].Value
            : _text["Exam.NotFound"].Value;
        return RedirectToAction(nameof(Index));
    }
}
