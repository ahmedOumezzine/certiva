using Certiva.Domain.Exams;
using Certiva.Domain.Attempts;
using Certiva.Domain.Enums;
using Certiva.Services.Exams;
using Xunit;

namespace Certiva.Web.Tests;

public sealed class ExamPublicationValidatorTests
{
    [Fact]
    public void RejectsExamWithoutPublishedQuestions()
    {
        var errors = ExamPublicationValidator.Validate(ValidExam(), "Exam", "Description", "EX", "exam", null, 70);

        Assert.Contains(errors, error => error.Contains("au moins une question", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RejectsUnsupportedQuestionType()
    {
        var exam = ValidExam();
        exam.Questions = [Question(exam, QuestionType.LongAnswer)];

        var errors = ExamPublicationValidator.Validate(exam, exam.Name, exam.Description, exam.Code, exam.Slug, null, 70);

        Assert.Contains(errors, error => error.Contains("non pris en charge", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RejectsQuestionWhoseSkillBelongsToAnotherExam()
    {
        var exam = ValidExam();
        var question = Question(exam, QuestionType.SingleChoice);
        question.SkillId = Guid.NewGuid();
        exam.Questions = [question];

        var errors = ExamPublicationValidator.Validate(exam, exam.Name, exam.Description, exam.Code, exam.Slug, null, 70);

        Assert.Contains(errors, error => error.Contains("n’appartient pas", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AcceptsSupportedQuestionTypesWithValidAnswerKeys()
    {
        var exam = ValidExam();
        var skill = new Skill { Id = Guid.NewGuid(), ExamId = exam.Id };
        exam.Skills = [skill];
        var single = Question(exam, QuestionType.SingleChoice, ("A", true), ("B", false));
        single.SkillId = skill.Id;
        var multi = Question(exam, QuestionType.MultiChoice, ("A", true), ("B", true), ("C", false));
        var boolean = Question(exam, QuestionType.TrueFalse, ("True", true), ("False", false));
        var shortAnswer = Question(exam, QuestionType.ShortAnswer, ("expected", true));
        exam.Questions = [single, multi, boolean, shortAnswer];

        var errors = ExamPublicationValidator.Validate(exam, exam.Name, exam.Description, exam.Code, exam.Slug, 30, 70);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(0, 70)]
    [InlineData(1441, 70)]
    [InlineData(null, -1)]
    [InlineData(null, 101)]
    public void RejectsInvalidDurationOrPassingThreshold(int? duration, int threshold)
    {
        var exam = ValidExam();
        exam.Questions = [Question(exam, QuestionType.SingleChoice, ("A", true), ("B", false))];

        var errors = ExamPublicationValidator.Validate(exam, exam.Name, exam.Description, exam.Code, exam.Slug, duration, threshold);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void DraftQuestionDoesNotSatisfyPublicationRequirement()
    {
        var exam = ValidExam();
        var question = Question(exam, QuestionType.SingleChoice, ("A", true), ("B", false));
        question.Status = Status.Draft;
        exam.Questions = [question];

        var errors = ExamPublicationValidator.Validate(exam, exam.Name, exam.Description, exam.Code, exam.Slug, null, 70);

        Assert.Contains(errors, error => error.Contains("au moins une question", StringComparison.OrdinalIgnoreCase));
    }

    private static Exam ValidExam() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Exam",
        Description = "Description",
        Code = "EX",
        Slug = "exam",
        PassingPercentage = 70,
        Status = Status.Published,
        Skills = [],
        Questions = []
    };

    private static Question Question(Exam exam, QuestionType type, params (string Text, bool Correct)[] choices) => new()
    {
        Id = Guid.NewGuid(),
        ExamId = exam.Id,
        Description = "Question",
        QuestionType = type,
        Status = Status.Published,
        Choices = choices.Select(choice => new Choice
        {
            Id = Guid.NewGuid(),
            QuestionId = Guid.NewGuid(),
            ChoiceText = choice.Text,
            IsCorrect = choice.Correct
        }).ToList()
    };
}
