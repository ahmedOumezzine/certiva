using Certiva.Application.Attempts;

namespace Certiva.Models.Exams;

public static class AttemptViewModelMapper
{
    public static TakeExamViewModel ToViewModel(this AttemptSessionDto dto) => new()
    {
        Culture = dto.Culture, AttemptId = dto.AttemptId, ExamId = dto.ExamId, ExamName = dto.ExamName, ExamDescription = dto.ExamDescription, ExamCode = dto.ExamCode, PassingPercentage = dto.PassingPercentage, ExamSlug = dto.ExamSlug, DurationMinutes = dto.DurationMinutes, StartedAtUtc = dto.StartedAtUtc, ExpiresAtUtc = dto.ExpiresAtUtc, RemainingSeconds = dto.RemainingSeconds,
        Questions = dto.Questions.Select(q => new TakeQuestionViewModel { Id = q.QuestionId, AttemptQuestionId = q.AttemptQuestionId, Description = q.Description, QuestionType = q.QuestionType, SkillName = q.SkillName, SelectedChoiceIds = q.SelectedChoiceIds.ToList(), TextAnswer = q.TextAnswer, Choices = q.Choices.Select(c => new TakeChoiceViewModel { Id = c.ChoiceId, AttemptChoiceId = c.AttemptChoiceId, ChoiceText = c.ChoiceText, GroupBy = c.GroupBy }).ToList() }).ToList()
    };

    public static ExamResultViewModel ToViewModel(this AttemptResultDto dto) => new()
    {
        Culture = dto.Culture, ExamName = dto.ExamName, ExamSlug = dto.ExamSlug, TotalQuestions = dto.TotalQuestions, CorrectAnswers = dto.CorrectAnswers, Score = dto.Score, MaxScore = dto.MaxScore, Percentage = dto.Percentage, Passed = dto.Passed, PassingScore = dto.PassingScore, DurationSeconds = dto.DurationSeconds,
        SkillResults = dto.SkillResults.Select(s => new SkillResultViewModel { SkillName = s.SkillName, Total = s.Total, Correct = s.Correct }).ToList(),
        Questions = dto.Questions.Select(q => new QuestionReviewViewModel { QuestionId = q.QuestionId, AttemptQuestionId = q.AttemptQuestionId, Description = q.Description, SkillName = q.SkillName, QuestionType = q.QuestionType, IsCorrect = q.IsCorrect, Points = q.Points, ScoreAwarded = q.ScoreAwarded, Explication = q.Explanation, UserAnswers = q.UserAnswers.ToList(), CorrectAnswers = q.CorrectAnswers.ToList() }).ToList()
    };
}
