using Certiva.Application.Attempts;
using Certiva.Application.Exams;
using Certiva.Infrastructure.Data;
using Certiva.Infrastructure.Data.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Certiva.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCertivaInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IExamCatalogReader, EfExamCatalogReader>();
        services.AddScoped<IExamAttemptReader, EfExamAttemptReader>();
        services.AddScoped<IExamAttemptAnswerStore, EfExamAttemptAnswerStore>();
        services.AddScoped<IExamAttemptStartStore, EfExamAttemptStartStore>();
        services.AddScoped<IExamAttemptSubmitStore, EfExamAttemptSubmitStore>();
        return services;
    }
}
