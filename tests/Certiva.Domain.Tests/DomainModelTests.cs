using System.ComponentModel.DataAnnotations;
using Certiva.Domain.Attempts;
using Certiva.Domain.Entities;
using Certiva.Domain.Enums;
using Certiva.Domain.Exams;
using Xunit;

namespace Certiva.Domain.Tests;

public sealed class DomainModelTests
{
    [Fact]
    public void BaseEntityStartsWithExpectedDefaults()
    {
        var entity = new Exam();

        Assert.Equal(Guid.Empty, entity.Id);
        Assert.False(entity.IsDeleted);
        Assert.Null(entity.LastModifiedOnUtc);
        Assert.Null(entity.DeletedOnUtc);
    }

    [Fact]
    public void ExamHasExpectedDefaultsAndCollections()
    {
        var exam = new Exam();

        Assert.Equal(70, exam.PassingPercentage);
        Assert.NotNull(exam.Translations);
        Assert.Null(exam.Skills);
        Assert.Null(exam.Questions);
    }

    [Fact]
    public void AttemptDefaultsToFrenchInProgressAndEmptyRowVersion()
    {
        var attempt = new ExamAttempt();

        Assert.Equal("fr", attempt.Culture);
        Assert.Equal(70, attempt.PassingPercentageSnapshot);
        Assert.Equal(ExamAttemptStatus.InProgress, attempt.Status);
        Assert.Empty(attempt.RowVersion);
        Assert.Empty(attempt.Questions);
    }

    [Fact]
    public void AttemptQuestionDefaultsToOnePointAndEmptyChoices()
    {
        var question = new ExamAttemptQuestion();

        Assert.Equal(1m, question.Points);
        Assert.Empty(question.Choices);
        Assert.Null(question.Answer);
    }

    [Fact]
    public void ExamAttemptAnswerStoresAnswerAndScoringData()
    {
        var answer = new ExamAnswer
        {
            SelectedChoiceIdsJson = "[\"choice-1\"]",
            TextAnswer = "Answer",
            SavedAtUtc = DateTime.UtcNow,
            IsCorrect = true,
            ScoreAwarded = 2.5m,
        };

        Assert.Equal("[\"choice-1\"]", answer.SelectedChoiceIdsJson);
        Assert.Equal("Answer", answer.TextAnswer);
        Assert.True(answer.IsCorrect);
        Assert.Equal(2.5m, answer.ScoreAwarded);
    }

    [Fact]
    public void ExamAttemptChoicePreservesSourceSnapshot()
    {
        var choice = new ExamAttemptChoice
        {
            SourceChoiceId = Guid.NewGuid(),
            Order = 2,
            ChoiceTextSnapshot = "Choice",
            GroupBySnapshot = "Group",
            IsCorrectSnapshot = true,
        };

        Assert.NotEqual(Guid.Empty, choice.SourceChoiceId);
        Assert.Equal(2, choice.Order);
        Assert.Equal("Choice", choice.ChoiceTextSnapshot);
        Assert.Equal("Group", choice.GroupBySnapshot);
        Assert.True(choice.IsCorrectSnapshot);
    }

    [Fact]
    public void ExamQuestionAndChoiceHaveExpectedDefaults()
    {
        var question = new Question();
        var choice = new Choice();

        Assert.Null(question.Choices);
        Assert.Empty(question.Translations);
        Assert.Equal(QuestionType.SingleChoice, question.QuestionType);
        Assert.Equal(Status.Draft, question.Status);
        Assert.Null(choice.ChoiceText);
        Assert.False(choice.IsCorrect);
        Assert.Empty(choice.Translations);
    }

    [Fact]
    public void SkillValidationRejectsMissingRequiredFields()
    {
        var skill = new Skill();
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(skill, new ValidationContext(skill), results, true);

        Assert.False(valid);
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(Skill.Name)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(Skill.Description)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(Skill.Pourcentage)));
    }

    [Fact]
    public void ExamValidationRejectsPassingPercentageOutsideRange()
    {
        var exam = new Exam { PassingPercentage = 101 };
        var results = new List<ValidationResult>();

        Validator.TryValidateProperty(exam.PassingPercentage,
            new ValidationContext(exam) { MemberName = nameof(Exam.PassingPercentage) }, results);

        Assert.NotEmpty(results);
    }

    [Fact]
    public void ExamCodeValidationRejectsMoreThanSixCharacters()
    {
        var exam = new Exam { Code = "TOO-LONG" };
        var results = new List<ValidationResult>();

        Validator.TryValidateProperty(exam.Code,
            new ValidationContext(exam) { MemberName = nameof(Exam.Code) }, results);

        Assert.NotEmpty(results);
    }

    [Fact]
    public void TranslationDefaultsToFrenchCulture()
    {
        Assert.Equal("fr", new ExamTranslation().Culture);
        Assert.Equal("fr", new SkillTranslation().Culture);
        Assert.Equal("fr", new QuestionTranslation().Culture);
        Assert.Equal("fr", new ChoiceTranslation().Culture);
    }

    [Fact]
    public void ExamRelationshipsCanBuildAnAttemptSnapshotGraph()
    {
        var exam = new Exam { Id = Guid.NewGuid(), Name = "Certiva" };
        var question = new ExamAttemptQuestion { Id = Guid.NewGuid(), QuestionTextSnapshot = "Question" };
        var choice = new ExamAttemptChoice { Id = Guid.NewGuid(), ChoiceTextSnapshot = "Answer" };
        question.Choices.Add(choice);
        var attempt = new ExamAttempt { Exam = exam, Questions = [question] };
        question.ExamAttempt = attempt;
        choice.ExamAttemptQuestion = question;

        Assert.Same(exam, attempt.Exam);
        Assert.Same(attempt, question.ExamAttempt);
        Assert.Same(question, choice.ExamAttemptQuestion);
        Assert.Single(attempt.Questions);
    }

    [Fact]
    public void ExamValidationAttributesRejectMissingRequiredFields()
    {
        var exam = new Exam();
        var context = new ValidationContext(exam);
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(exam, context, results, validateAllProperties: true);

        Assert.False(valid);
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(Exam.Name)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(Exam.Description)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(Exam.Code)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(Exam.Slug)));
    }

    [Fact]
    public void ExamValidationAttributesAcceptValidRequiredFields()
    {
        var exam = new Exam { Name = "Exam", Description = "Description", Code = "CERT", Slug = "exam" };
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(exam, new ValidationContext(exam), results, true);

        Assert.True(valid);
        Assert.Empty(results);
    }

    [Fact]
    public void ExamDurationRangeRejectsInvalidValues()
    {
        var exam = new Exam { DurationMinutes = 0 };
        var results = new List<ValidationResult>();

        Validator.TryValidateProperty(exam.DurationMinutes, new ValidationContext(exam) { MemberName = nameof(Exam.DurationMinutes) }, results);

        Assert.NotEmpty(results);
    }

    [Fact]
    public void TranslationCultureRequiresTwoCharacters()
    {
        var translation = new ExamTranslation { Culture = "f" };
        var results = new List<ValidationResult>();

        Validator.TryValidateProperty(translation.Culture, new ValidationContext(translation) { MemberName = nameof(ExamTranslation.Culture) }, results);

        Assert.NotEmpty(results);
    }

    [Fact]
    public void DomainEnumsExposeExpectedWorkflowStates()
    {
        Assert.Equal(ExamAttemptStatus.InProgress, (ExamAttemptStatus)0);
        Assert.Equal(ExamAttemptStatus.Completed, (ExamAttemptStatus)1);
        Assert.Equal(ExamAttemptStatus.Expired, (ExamAttemptStatus)2);
        Assert.Equal(Status.Draft, (Status)0);
        Assert.Equal(Status.Published, (Status)1);
        Assert.Equal(Status.Archived, (Status)2);
    }
}
