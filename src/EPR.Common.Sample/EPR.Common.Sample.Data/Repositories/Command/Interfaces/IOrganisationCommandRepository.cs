namespace EPR.Common.Sample.Data.Repositories.Command.Interfaces;

using Tables;

public interface IOrganisationCommandRepository
{
    Task CreateOrganisationAsync(OrgApplication org);

    Task AddUserToOrganisationAsync(Guid id, OrgUser user);

    Task SeedDataAsync(IEnumerable<OrgApplication> applications);
}