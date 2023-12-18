namespace EPR.Common.Sample.Data
{
    using EPR.Common.Functions.Database.Context.Interfaces;
    using Microsoft.Azure.Cosmos;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;

    public static class DbDependencyInjectionExtensions
    {
        public static IServiceCollection AddEprDataContext(this IServiceCollection services, string connectionString, string key, string databaseName) =>
            services.AddEntityFrameworkCosmos()
                .AddDbContext<IEprCommonContext, EprContext>(
                    options =>
                    {
                        options.UseCosmos(connectionString, key, databaseName, c => c.ConnectionMode(ConnectionMode.Gateway));
                    });
    }
}