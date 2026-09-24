using Certiva.Domain.Exams;
using Certiva.Domain.Attempts;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Certiva.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<ExamTranslation> ExamTranslations => Set<ExamTranslation>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<SkillTranslation> SkillTranslations => Set<SkillTranslation>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionTranslation> QuestionTranslations => Set<QuestionTranslation>();
    public DbSet<Choice> Choices => Set<Choice>();
    public DbSet<ChoiceTranslation> ChoiceTranslations => Set<ChoiceTranslation>();
    public DbSet<ExamAttempt> ExamAttempts => Set<ExamAttempt>();
    public DbSet<ExamAttemptQuestion> ExamAttemptQuestions => Set<ExamAttemptQuestion>();
    public DbSet<ExamAttemptChoice> ExamAttemptChoices => Set<ExamAttemptChoice>();
    public DbSet<ExamAnswer> ExamAnswers => Set<ExamAnswer>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Exam>().ToTable("Exam");
        builder.Entity<Skill>().ToTable("Skill");
        builder.Entity<Question>().ToTable("Question");
        builder.Entity<Choice>().ToTable("Choice");
        ConfigureTranslations(builder);
        builder.Entity<ExamAttempt>().ToTable("ExamAttempts");
        builder.Entity<ExamAttemptQuestion>().ToTable("ExamAttemptQuestions");
        builder.Entity<ExamAttemptChoice>().ToTable("ExamAttemptChoices");
        builder.Entity<ExamAnswer>().ToTable("ExamAnswers");

        builder.Entity<Exam>()
            .HasMany(exam => exam.Skills)
            .WithOne(skill => skill.Exam)
            .HasForeignKey(skill => skill.ExamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Exam>()
            .HasMany(exam => exam.Questions)
            .WithOne(question => question.Exam)
            .HasForeignKey(question => question.ExamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Question>()
            .HasMany(question => question.Choices)
            .WithOne(choice => choice.Question)
            .HasForeignKey(choice => choice.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ExamAttempt>()
            .HasOne(attempt => attempt.Exam)
            .WithMany()
            .HasForeignKey(attempt => attempt.ExamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ExamAttempt>().Property(attempt => attempt.RowVersion).IsRowVersion();
        builder.Entity<ExamAttempt>().HasIndex(attempt => new { attempt.ExamId, attempt.Status });
        builder.Entity<ExamAttempt>().HasIndex(attempt => new { attempt.UserId, attempt.Status });
        builder.Entity<ExamAttempt>().HasIndex(attempt => attempt.AnonymousTokenHash);
        builder.Entity<ExamAttempt>().Property(attempt => attempt.AnonymousTokenHash).HasMaxLength(128);
        builder.Entity<ExamAttempt>().Property(attempt => attempt.GuestEmail).HasMaxLength(256);
        builder.Entity<ExamAttempt>().Property(attempt => attempt.ExamNameSnapshot).HasMaxLength(500);
        builder.Entity<ExamAttempt>().Property(attempt => attempt.ExamSlugSnapshot).HasMaxLength(500);
        builder.Entity<ExamAttempt>().Property(attempt => attempt.Culture).HasMaxLength(2).HasDefaultValue("fr");
        builder.Entity<Exam>().Property(exam => exam.PassingPercentage).HasDefaultValue(70);

        builder.Entity<ExamAttemptQuestion>()
            .HasOne(question => question.ExamAttempt)
            .WithMany(attempt => attempt.Questions)
            .HasForeignKey(question => question.ExamAttemptId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<ExamAttemptQuestion>()
            .HasIndex(question => new { question.ExamAttemptId, question.Order })
            .IsUnique();
        builder.Entity<ExamAttemptQuestion>().Property(question => question.QuestionTextSnapshot).HasMaxLength(4000);
        builder.Entity<ExamAttemptQuestion>().Property(question => question.ExplanationSnapshot).HasMaxLength(4000);
        builder.Entity<ExamAttemptQuestion>().Property(question => question.SkillNameSnapshot).HasMaxLength(256);

        builder.Entity<ExamAttemptChoice>()
            .HasOne(choice => choice.ExamAttemptQuestion)
            .WithMany(question => question.Choices)
            .HasForeignKey(choice => choice.ExamAttemptQuestionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<ExamAttemptChoice>()
            .HasIndex(choice => new { choice.ExamAttemptQuestionId, choice.Order })
            .IsUnique();
        builder.Entity<ExamAttemptChoice>().Property(choice => choice.ChoiceTextSnapshot).HasMaxLength(2000);
        builder.Entity<ExamAttemptChoice>().Property(choice => choice.GroupBySnapshot).HasMaxLength(500);

        builder.Entity<ExamAnswer>()
            .HasOne(answer => answer.ExamAttemptQuestion)
            .WithOne(question => question.Answer)
            .HasForeignKey<ExamAnswer>(answer => answer.ExamAttemptQuestionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<ExamAnswer>().HasIndex(answer => answer.ExamAttemptQuestionId).IsUnique();
        builder.Entity<ExamAnswer>().Property(answer => answer.TextAnswer).HasMaxLength(4000);
        builder.Entity<ExamAnswer>().Property(answer => answer.ScoreAwarded).HasPrecision(18, 2);
    }

    internal static void ConfigureTranslations(ModelBuilder builder)
    {
        builder.Entity<ExamTranslation>(entity =>
        {
            entity.ToTable("ExamTranslations");
            entity.HasIndex(item => new { item.ExamId, item.Culture }).IsUnique();
            entity.HasIndex(item => new { item.Culture, item.Slug }).IsUnique().HasFilter("[Slug] IS NOT NULL");
            entity.Property(item => item.Culture).HasMaxLength(2).IsRequired();
            entity.Property(item => item.Name).HasMaxLength(160);
            entity.Property(item => item.Description).HasMaxLength(2000);
            entity.Property(item => item.Slug).HasMaxLength(180);
            entity.HasOne(item => item.Exam).WithMany(item => item.Translations)
                .HasForeignKey(item => item.ExamId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SkillTranslation>(entity =>
        {
            entity.ToTable("SkillTranslations");
            entity.HasIndex(item => new { item.SkillId, item.Culture }).IsUnique();
            entity.Property(item => item.Culture).HasMaxLength(2).IsRequired();
            entity.Property(item => item.Name).HasMaxLength(140);
            entity.Property(item => item.Description).HasMaxLength(1000);
            entity.HasOne(item => item.Skill).WithMany(item => item.Translations)
                .HasForeignKey(item => item.SkillId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<QuestionTranslation>(entity =>
        {
            entity.ToTable("QuestionTranslations");
            entity.HasIndex(item => new { item.QuestionId, item.Culture }).IsUnique();
            entity.Property(item => item.Culture).HasMaxLength(2).IsRequired();
            entity.Property(item => item.Description).HasMaxLength(4000);
            entity.Property(item => item.Explanation).HasMaxLength(4000);
            entity.Property(item => item.FillInBlank).HasMaxLength(1000);
            entity.HasOne(item => item.Question).WithMany(item => item.Translations)
                .HasForeignKey(item => item.QuestionId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ChoiceTranslation>(entity =>
        {
            entity.ToTable("ChoiceTranslations");
            entity.HasIndex(item => new { item.ChoiceId, item.Culture }).IsUnique();
            entity.Property(item => item.Culture).HasMaxLength(2).IsRequired();
            entity.Property(item => item.ChoiceText).HasMaxLength(1000);
            entity.Property(item => item.GroupBy).HasMaxLength(200);
            entity.HasOne(item => item.Choice).WithMany(item => item.Translations)
                .HasForeignKey(item => item.ChoiceId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
