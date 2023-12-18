namespace EPR.Common.Sample.Data.Repositories.Query;

using AutoMapper;
using Common.Data.Tables.Audit;
using Functions.CancellationTokens.Interfaces;
using Interfaces;
using Microsoft.EntityFrameworkCore;
using Tables;

internal class OrganisationQueryRepository : IOrganisationQueryRepository
{
    private readonly EprContext context;
    private readonly IMapper mapper;
    private readonly ICancellationTokenAccessor cancellationTokenAccessor;

    public OrganisationQueryRepository(EprContext context, IMapper mapper, ICancellationTokenAccessor cancellationTokenAccessor)
    {
        this.mapper = mapper;
        this.context = context;
        this.cancellationTokenAccessor = cancellationTokenAccessor;
    }

    public async Task<ICollection<OrgApplication>> GetAsync() => await this.context.OrganisationApplications.OrderByDescending(x => x.Created).ToListAsync(this.cancellationTokenAccessor.CancellationToken);

    public async Task<OrgApplication> GetByIdAsync(Guid id) => await this.context.OrganisationApplications.FirstOrDefaultAsync(x => x.Id == id, this.cancellationTokenAccessor.CancellationToken);

    public async Task<ICollection<OrgApplicationAudit>> GetAuditHistoryAsync(Guid id) => await this.context.OrganisationApplicationsAudits.Where(x => x._Id == id).OrderByDescending(x => x.AuditCreated).ToListAsync(this.cancellationTokenAccessor.CancellationToken);
}