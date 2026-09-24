using Certiva.Application.Attempts;
using Certiva.Application.Exams;
using Certiva.Domain.Enums;
using Certiva.Models.Exams;
using Xunit;

namespace Certiva.Web.Tests;

public sealed class ViewModelMapperTests
{
    [Fact]
    public void CatalogMapperCopiesPagingCardsAndSkills()
    {
        var examId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        var dto = new ExamCatalogPageDto("en", "cert", skillId, 2, 9, 1,
            [new ExamCatalogItemDto(examId, "Exam", "Description", "CERT", "exam", "source", 3,
                Status.Published, 30, 70, ["Security"], true)],
            [new ExamSkillFilterDto(skillId, "Security", false)]);

        var model = dto.ToViewModel();

        Assert.Equal("en", model.Culture);
        Assert.Equal(2, model.Page);
        Assert.Equal("Exam", Assert.Single(model.Exams).Name);
        Assert.Equal(examId, Assert.Single(model.Exams).Id);
        Assert.Equal("Security", Assert.Single(model.Skills).Name);
    }

    [Fact]
    public void AttemptMapperCopiesNestedQuestionsChoicesAndResults()
    {
        var questionId = Guid.NewGuid();
        var attemptQuestionId = Guid.NewGuid();
        var session = new AttemptSessionDto(Guid.NewGuid(), Guid.NewGuid(), "fr", "Exam", "Description", "CERT", "exam", 70, 30,
            DateTime.UtcNow, null, 120,
            [new AttemptQuestionDto(questionId, attemptQuestionId, 1, "Question", QuestionType.SingleChoice, "Security", [], null,
                [new AttemptChoiceDto(Guid.NewGuid(), Guid.NewGuid(), 1, "Choice", null)])]);

        var take = session.ToViewModel();

        Assert.Equal("Exam", take.ExamName);
        var mappedQuestion = Assert.Single(take.Questions);
        Assert.Equal(questionId, mappedQuestion.Id);
        Assert.Equal("Choice", Assert.Single(mappedQuestion.Choices).ChoiceText);

        var result = new AttemptResultDto("fr", "Exam", "exam", 1, 1, 1, 1, 100, true, 70, 30,
            [new SkillResultDto("Security", 1, 1)],
            [new AttemptQuestionResultDto(questionId, attemptQuestionId, "Question", "Security", QuestionType.SingleChoice, true, 1, 1, "Explanation", ["Choice"], ["Choice"])]) ;

        var resultModel = result.ToViewModel();

        Assert.True(resultModel.Passed);
        Assert.Equal("Security", Assert.Single(resultModel.SkillResults).SkillName);
        Assert.Equal("Explanation", Assert.Single(resultModel.Questions).Explication);
    }
}
