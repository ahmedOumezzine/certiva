using Certiva.Areas.Admin.Services;
using Certiva.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Certiva.Localization;
using Microsoft.Extensions.Localization;

namespace Certiva.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class SkillController : Controller
{
    private readonly ISkillAdminService _service;
    private readonly IStringLocalizer<SharedResource> _text;

    public SkillController(ISkillAdminService service, IStringLocalizer<SharedResource> text)
    {
        _service = service;
        _text = text;
    }

    public async Task<IActionResult> Index(Guid? examId, string? search, int page = 1, int pageSize = AdminPagination.DefaultPageSize)
    {
        return View(await _service.GetSkillsAsync(examId, search, page, pageSize));
    }

    public async Task<IActionResult> Create(Guid? examId)
    {
        return View("Edit", await _service.NewSkillAsync(examId));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var model = await _service.GetSkillAsync(id);
        return model == null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(SkillEditViewModel model)
    {
        if (!ModelState.IsValid)
            return View("Edit", await RehydrateAsync(model));

        var proxy = new ModelStateDictionaryProxy(ModelState.AddModelError);
        var saved = await _service.SaveSkillAsync(model, proxy);
        if (!saved && !proxy.HasErrors)
            ModelState.AddModelError(string.Empty, _text["Skill.SaveFailed"]);

        if (!saved || !ModelState.IsValid)
            return View("Edit", await RehydrateAsync(model));

        TempData["Success"] = _text["Skill.Saved"].Value;
        return RedirectToAction(nameof(Index), new { examId = model.ExamId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteSkillAsync(id);
        TempData[deleted ? "Success" : "Error"] = deleted
            ? _text["Skill.Deleted"].Value
            : _text["Skill.DeleteBlocked"].Value;
        return RedirectToAction(nameof(Index));
    }

    private async Task<SkillEditViewModel> RehydrateAsync(SkillEditViewModel model)
    {
        model.Exams = (await _service.NewSkillAsync(model.ExamId)).Exams;
        return model;
    }
}
