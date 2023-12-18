namespace EPR.Common.Sample.Data.Tables;

using Functions.Database;
using Functions.Database.Entities;
using Functions.Database.Entities.Interfaces;

public class OrgApplication : EntityBase, ICreated, IUpdated
{
    public Guid CustomerOrganisationId { get; set; }

    public Guid CustomerId { get; set; }

    public ICollection<OrgUser> Users { get; set; } = new List<OrgUser>();

    public DateTime Created { get; set; }

    public DateTime LastUpdated { get; set; }
}