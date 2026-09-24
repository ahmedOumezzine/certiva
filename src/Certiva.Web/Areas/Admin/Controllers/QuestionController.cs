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
public sealed class QuestionController : Controller
{
    private readonly IQuestionAdminService _service;
    private readonly IStringLocalizer<SharedResource> _text;

    public QuestionController(IQuestionAdminService service, IStringLocalizer<SharedResource> text)
    {
        _service = service;
        _text = text;
    }

    public async Task<IActionResult> Index(string? search, Guid? examId, Guid? skillId, QuestionType? questionType, Status? status, int page = 1, int pageSize = AdminPagination.DefaultPageSize)
    {
        return View(await _service.GetQuestionsAsync(search, examId, skillId, questionType, status, page, pageSize));
    }

    public async Task<IActionResult> Create(Guid? examId)
    {
        return View("Edit", await _service.NewQuestionAsync(examId));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var model = await _service.GetQuestionAsync(id);
        return model == null ? NotFound() : View(model);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var model = await _service.GetQuestionDetailsAsync(id);
        return model == null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(QuestionEditViewModel model)
    {
        if (!ModelState.IsValid)
            return View("Edit", await RehydrateAsync(model));

        var proxy = new ModelStateDictionaryProxy(ModelState.AddModelError);
        var saved = await _service.SaveQuestionAsync(model, proxy);
        if (!saved && !proxy.HasErrors)
            ModelState.AddModelError(string.Empty, _text["Question.SaveFailed"]);

        if (!saved || !ModelState.IsValid)
            return View("Edit", await RehydrateAsync(model));

        TempData["Success"] = _text["Question.Saved"].Value;
        return RedirectToAction(nameof(Index), new { examId = model.ExamId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteQuestionAsync(id);
        TempData[deleted ? "Success" : "Error"] = deleted ? _text["Question.Deleted"].Value : _text["Question.NotFound"].Value;
        return RedirectToAction(nameof(Index));
    }

    private async Task<QuestionEditViewModel> RehydrateAsync(QuestionEditViewModel model)
    {
        var fresh = await _service.NewQuestionAsync(model.ExamId);
        model.Exams = fresh.Exams;
        model.Skills = fresh.Skills;
        return model;
    }
}
