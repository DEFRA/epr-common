namespace EPR.Common.Sample.API.Services
{
    using EPR.Common.Sample.API.Services.Interfaces;

    public static class ServiceDependencyInjectionExtensions
    {
        public static IServiceCollection AddSampleServices(this IServiceCollection services) =>
            services.AddTransient<IOrganisationService, OrganisationService>();
    }
}