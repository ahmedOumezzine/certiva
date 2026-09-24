using Certiva.Domain.Exams;
using Certiva.Domain.Attempts;
using Certiva.Domain.Enums;
using Certiva.Areas.Admin.ViewModels;
using Certiva.Infrastructure.Data;
using Certiva.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.EntityFrameworkCore;

namespace Certiva.Areas.Admin.Services;

public interface IAdminAttemptService
{
    Task<AttemptIndexViewModel> GetAttemptsAsync(int page, ExamAttemptStatus? status, Guid? examId, CancellationToken cancellationToken = default);
    Task<AttemptDetailsViewModel?> GetAttemptAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed class AdminAttemptService : IAdminAttemptService
{
    private readonly ApplicationDbContext _db;
    private readonly IStringLocalizer<SharedResource> _text;

    public AdminAttemptService(ApplicationDbContext db, IStringLocalizer<SharedResource> text)
    {
        _db = db;
        _text = text;
    }

    public async Task<AttemptIndexViewModel> GetAttemptsAsync(int page, ExamAttemptStatus? status, Guid? examId, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        const int pageSize = 25;
        var query = _db.Set<ExamAttempt>().AsNoTracking().AsQueryable();
        if (status.HasValue)
            query = query.Where(attempt => attempt.Status == status.Value);
        if (examId.HasValue && examId.Value != Guid.Empty)
            query = query.Where(attempt => attempt.ExamId == examId.Value);

        var total = await query.CountAsync(cancellationToken);
        var attempts = await (
            from attempt in query
            join user in _db.Users.AsNoTracking()
                on attempt.UserId equals user.Id into matchingUsers
            from user in matchingUsers.DefaultIfEmpty()
            orderby attempt.StartedAtUtc descending
            select new AttemptRowViewModel
            {
                Id = attempt.Id,
                ExamName = attempt.ExamNameSnapshot,
                Candidate = attempt.UserId == null
                    ? (attempt.GuestEmail == null ? string.Empty : "Guest")
                    : user != null && user.FullName != string.Empty
                        ? user.FullName
                        : user == null
                            ? attempt.UserId
                            : user.Email ?? user.Id,
                CandidateEmail = attempt.UserId == null ? attempt.GuestEmail : user == null ? null : user.Email,
                IsGuest = attempt.UserId == null && attempt.GuestEmail != null,
                StartedAtUtc = attempt.StartedAtUtc,
                CompletedAtUtc = attempt.CompletedAtUtc,
                Status = attempt.Status,
                Score = attempt.Score,
                MaxScore = attempt.MaxScore,
                Percentage = attempt.Percentage,
                Passed = attempt.Passed
            })
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        foreach (var attempt in attempts)
        {
            if (attempt.IsGuest)
                attempt.Candidate = _text["Guest"].Value;
            else if (string.IsNullOrEmpty(attempt.Candidate))
                attempt.Candidate = _text["Anonymous user"].Value;
        }

        return new AttemptIndexViewModel
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            Status = status,
            ExamId = examId,
            Attempts = attempts
        };
    }

    public async Task<AttemptDetailsViewModel?> GetAttemptAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var attempt = await _db.Set<ExamAttempt>().AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new AttemptDetailsData
            {
                Id = item.Id,
                ExamId = item.ExamId,
                ExamName = item.ExamNameSnapshot,
                ExamCode = item.Exam == null ? string.Empty : item.Exam.Code ?? string.Empty,
                ExamSlug = item.Exam == null
                    ? item.ExamSlugSnapshot
                    : item.Exam.Slug ?? item.ExamSlugSnapshot,
                ExamIsPublic = item.Exam != null && item.Exam.Status == Status.Published,
                UserId = item.UserId,
                GuestEmail = item.GuestEmail,
                StartedAtUtc = item.StartedAtUtc,
                ExpiresAtUtc = item.ExpiresAtUtc,
                CompletedAtUtc = item.CompletedAtUtc,
                CreatedOnUtc = item.CreatedOnUtc,
                LastModifiedOnUtc = item.LastModifiedOnUtc,
                Status = item.Status,
                Score = item.Score,
                MaxScore = item.MaxScore,
                Percentage = item.Percentage,
                Passed = item.Passed,
                PassingPercentage = item.PassingPercentageSnapshot,
                Questions = item.Questions.OrderBy(question => question.Order)
                    .Select(question => new AttemptQuestionData
                    {
                        Order = question.Order,
                        QuestionText = question.QuestionTextSnapshot,
                        QuestionType = question.QuestionTypeSnapshot,
                        SkillName = question.SkillNameSnapshot,
                        Explanation = question.ExplanationSnapshot,
                        Points = question.Points,
                        SelectedChoiceIdsJson = question.Answer == null ? null : question.Answer.SelectedChoiceIdsJson,
                        ScoreAwarded = question.Answer == null ? 0m : question.Answer.ScoreAwarded,
                        IsCorrect = question.Answer == null ? null : question.Answer.IsCorrect,
                        TextAnswer = question.Answer == null ? null : question.Answer.TextAnswer,
                        Choices = question.Choices.OrderBy(choice => choice.Order)
                            .Select(choice => new AttemptChoiceData
                            {
                                Id = choice.Id,
                                Text = choice.ChoiceTextSnapshot,
                                IsCorrect = choice.IsCorrectSnapshot
                            })
                            .ToList()
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (attempt is null)
            return null;

        var candidate = string.IsNullOrWhiteSpace(attempt.GuestEmail) ? _text["Anonymous user"].Value : _text["Guest"].Value;
        string? candidateEmail = attempt.GuestEmail;
        if (attempt.UserId != null)
        {
            var user = await _db.Users.AsNoTracking().Where(user => user.Id == attempt.UserId)
                .Select(item => new { item.FullName, item.Email })
                .FirstOrDefaultAsync(cancellationToken);
            candidateEmail = user?.Email;
            candidate = !string.IsNullOrWhiteSpace(user?.FullName)
                ? user.FullName
                : candidateEmail ?? _text["Identified user"].Value;
        }

        var questions = attempt.Questions.Select(question =>
        {
            var selectedIds = DeserializeSelected(question.SelectedChoiceIdsJson);
            return new AttemptQuestionAdminViewModel
            {
                Order = question.Order,
                QuestionText = question.QuestionText,
                QuestionType = question.QuestionType,
                SkillName = question.SkillName,
                Explanation = question.Explanation,
                Points = question.Points,
                ScoreAwarded = question.ScoreAwarded,
                IsCorrect = question.IsCorrect,
                TextAnswer = question.TextAnswer,
                HasAnswer = selectedIds.Count > 0 || !string.IsNullOrWhiteSpace(question.TextAnswer),
                Choices = question.Choices.Select(choice => new AttemptChoiceAdminViewModel
                {
                    Text = choice.Text,
                    IsCorrect = choice.IsCorrect,
                    WasSelected = selectedIds.Contains(choice.Id)
                }).ToList()
            };
        }).ToList();

        return new AttemptDetailsViewModel
        {
            Id = attempt.Id,
            ExamId = attempt.ExamId,
            ExamName = attempt.ExamName,
            ExamCode = attempt.ExamCode,
            ExamSlug = attempt.ExamSlug,
            ExamIsPublic = attempt.ExamIsPublic,
            Candidate = candidate,
            CandidateEmail = candidateEmail,
            StartedAtUtc = attempt.StartedAtUtc,
            ExpiresAtUtc = attempt.ExpiresAtUtc,
            CompletedAtUtc = attempt.CompletedAtUtc,
            CreatedOnUtc = attempt.CreatedOnUtc,
            LastModifiedOnUtc = attempt.LastModifiedOnUtc,
            Status = attempt.Status,
            Score = attempt.Score,
            MaxScore = attempt.MaxScore,
            Percentage = attempt.Percentage,
            Passed = attempt.Passed,
            PassingPercentage = attempt.PassingPercentage,
            Questions = questions,
            SkillScores = questions.GroupBy(question => string.IsNullOrWhiteSpace(question.SkillName) ? _text["Général"].Value : question.SkillName!)
                .Select(group => new AttemptSkillScoreViewModel
                {
                    SkillName = group.Key,
                    Total = group.Count(),
                    Correct = group.Count(question => question.IsCorrect == true)
                })
                .OrderBy(result => result.SkillName)
                .ToList()
        };
    }

    private static HashSet<Guid> DeserializeSelected(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];
        try
        {
            return (System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(json) ?? []).ToHashSet();
        }
        catch (System.Text.Json.JsonException)
        {
            return [];
        }
    }

    private sealed class AttemptDetailsData
    {
        public Guid Id { get; set; }
        public Guid ExamId { get; set; }
        public string ExamName { get; set; } = string.Empty;
        public string ExamCode { get; set; } = string.Empty;
        public string ExamSlug { get; set; } = string.Empty;
        public bool ExamIsPublic { get; set; }
        public string? UserId { get; set; }
        public string? GuestEmail { get; set; }
        public DateTime StartedAtUtc { get; set; }
        public DateTime? ExpiresAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public DateTime? LastModifiedOnUtc { get; set; }
        public ExamAttemptStatus Status { get; set; }
        public decimal? Score { get; set; }
        public decimal? MaxScore { get; set; }
        public decimal? Percentage { get; set; }
        public bool? Passed { get; set; }
        public int PassingPercentage { get; set; }
        public List<AttemptQuestionData> Questions { get; set; } = [];
    }

    private sealed class AttemptQuestionData
    {
        public int Order { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public QuestionType QuestionType { get; set; }
        public string? SkillName { get; set; }
        public string? Explanation { get; set; }
        public decimal Points { get; set; }
        public string? SelectedChoiceIdsJson { get; set; }
        public decimal ScoreAwarded { get; set; }
        public bool? IsCorrect { get; set; }
        public string? TextAnswer { get; set; }
        public List<AttemptChoiceData> Choices { get; set; } = [];
    }

    private sealed class AttemptChoiceData
    {
        public Guid Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }
}
