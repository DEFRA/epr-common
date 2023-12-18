namespace EPR.Common.Sample.Data.Repositories;

using System.Runtime.CompilerServices;
using EPR.Common.Sample.Data.Repositories.Command;
using EPR.Common.Sample.Data.Repositories.Command.Interfaces;
using EPR.Common.Sample.Data.Repositories.Query;
using EPR.Common.Sample.Data.Repositories.Query.Interfaces;
using Functions.Database.Context.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public static class RepositoryDependencyInjectionExtensions
{
    public static IServiceCollection AddSampleRepositories(this IServiceCollection services) =>
        services
            .AddTransient<IOrganisationCommandRepository, OrganisationCommandRepository>()
            .AddTransient<IOrganisationQueryRepository, OrganisationQueryRepository>();
}