using Certiva.Domain.Exams;
using Certiva.Domain.Attempts;
using Certiva.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Certiva.Infrastructure.Tests;

public sealed class CertivaModelOnlyTests
{
    [Fact]
    public void AttemptModelKeepsAnswersUniqueAndVersioned()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=CertivaModelOnly;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        using var db = new ApplicationDbContext(options);

        var attempt = db.Model.FindEntityType(typeof(ExamAttempt))!;
        var answer = db.Model.FindEntityType(typeof(ExamAnswer))!;
        var question = db.Model.FindEntityType(typeof(ExamAttemptQuestion))!;
        var rowVersion = attempt.FindProperty(nameof(ExamAttempt.RowVersion));

        Assert.NotNull(rowVersion);
        Assert.True(rowVersion!.IsConcurrencyToken);
        Assert.Equal(Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate, rowVersion!.ValueGenerated);
        Assert.Equal("rowversion", rowVersion.GetColumnType());
        Assert.Contains(answer.GetIndexes(), index => index.IsUnique && index.Properties.Single().Name == nameof(ExamAnswer.ExamAttemptQuestionId));
        Assert.Contains(question.GetIndexes(), index => index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual([nameof(ExamAttemptQuestion.ExamAttemptId), nameof(ExamAttemptQuestion.Order)]));
        Assert.Equal(70, db.Model.FindEntityType(typeof(Exam))!.FindProperty(nameof(Exam.PassingPercentage))!.GetDefaultValue());
    }
}
