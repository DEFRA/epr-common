namespace EPR.Common.Sample.Data.Repositories.Command;

using AutoMapper;
using Functions.CancellationTokens.Interfaces;
using Interfaces;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Tables;

internal class OrganisationCommandRepository : IOrganisationCommandRepository
{
    private readonly EprContext context;
    private readonly IMapper mapper;
    private readonly ICancellationTokenAccessor cancellationTokenAccessor;

    public OrganisationCommandRepository(EprContext context, IMapper mapper, ICancellationTokenAccessor cancellationTokenAccessor)
    {
        this.mapper = mapper;
        this.context = context;
        this.cancellationTokenAccessor = cancellationTokenAccessor;
    }

    public async Task CreateOrganisationAsync(OrgApplication org) => await this.context.OrganisationApplications.AddAsync(org, this.cancellationTokenAccessor.CancellationToken);

    public async Task AddUserToOrganisationAsync(Guid id, OrgUser user)
    {
        var org = await this.context.OrganisationApplications.FirstOrDefaultAsync(x => x.Id == id, this.cancellationTokenAccessor.CancellationToken);
        org.Users.Add(user);

        this.context.Update(org);
    }

    public async Task SeedDataAsync(IEnumerable<OrgApplication> applications)
    {
        await this.context.Database.EnsureDeletedAsync();
        await this.context.Database.EnsureCreatedAsync();
        if (await this.context.OrganisationApplications.CountAsync() == 0)
        {
            this.context.OrganisationApplications.AddRangeAsync(applications, this.cancellationTokenAccessor.CancellationToken);
        }
    }
}