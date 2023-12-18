namespace EPR.Common.Sample.API.Services.Interfaces;

using Common.Data.Tables.Audit;
using Data.Tables;

public interface IOrganisationService
{
    Task<ICollection<OrgApplication>> GetAsync();

    Task<OrgApplication> GetByIdAsync(Guid id);

    Task<ICollection<OrgApplicationAudit>> GetAuditHistoryAsync(Guid id);

    Task CreateOrganisation();

    Task AddUserToOrganisation(Guid id);

    Task SeedData();
}