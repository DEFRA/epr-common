namespace EPR.Common.Sample.Data.Repositories.Query.Interfaces;

using Common.Data.Tables.Audit;
using Tables;

public interface IOrganisationQueryRepository
{
    Task<ICollection<OrgApplication>> GetAsync();

    Task<OrgApplication> GetByIdAsync(Guid id);

    Task<ICollection<OrgApplicationAudit>> GetAuditHistoryAsync(Guid id);
}