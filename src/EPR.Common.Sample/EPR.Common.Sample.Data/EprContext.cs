namespace EPR.Common.Sample.Data;

using Common.Data.Tables.Audit;
using EPR.Common.Sample.Data.Tables;
using Functions.AccessControl.Interfaces;
using Functions.Database.Context;
using Functions.Database.Decorators.Interfaces;
using Functions.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

public class EprContext : EprCommonContext
{
    public EprContext(
        DbContextOptions contextOptions,
        IUserContextProvider userContextProvider,
        IRequestTimeService requestTimeService,
        IEnumerable<IEntityDecorator> entityDecorators)
        : base(contextOptions, userContextProvider, requestTimeService, entityDecorators)
    {
    }

    public DbSet<OrgApplication> OrganisationApplications { get; set; }

    public DbSet<OrgApplicationAudit> OrganisationApplicationsAudits { get; set; }

    protected override void ConfigureApplicationKeys(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrgApplication>()
            .ToContainer("OrganisationApplications")
            .HasPartitionKey(x => x.Id)
            .HasKey(x => x.Id);

        modelBuilder.Entity<OrgApplicationAudit>()
            .ToContainer("OrganisationApplications")
            .HasPartitionKey(x => x.Id)
            .HasKey(x => x.Id);

        modelBuilder.Entity<OrgApplication>().OwnsMany<OrgUser>(p => p.Users);
    }
}