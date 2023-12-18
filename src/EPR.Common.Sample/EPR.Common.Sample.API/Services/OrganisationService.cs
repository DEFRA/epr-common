namespace EPR.Common.Sample.API.Services;

using Common.Data.Tables.Audit;
using Data.Repositories.Command.Interfaces;
using Data.Repositories.Query.Interfaces;
using Data.Tables;
using Functions.AccessControl.Interfaces;
using Functions.Database.UnitOfWork.Interfaces;
using Functions.Services;
using Interfaces;

public class OrganisationService : IOrganisationService
{
    private readonly IUnitOfWork unitOfWork;
    private readonly IOrganisationQueryRepository orgQueryRepository;
    private readonly IOrganisationCommandRepository orgCommandRepository;
    private readonly IUserContextProvider userContextProvider;

    public OrganisationService(
        IUnitOfWork unitOfWork,
        IOrganisationQueryRepository orgQueryRepository,
        IOrganisationCommandRepository orgCommandRepository,
        IUserContextProvider userContextProvider)
    {
        this.unitOfWork = unitOfWork;
        this.orgQueryRepository = orgQueryRepository;
        this.orgCommandRepository = orgCommandRepository;
        this.userContextProvider = userContextProvider;
    }

    public async Task<ICollection<OrgApplication>> GetAsync()
    {
        return await this.orgQueryRepository.GetAsync();
    }

    public async Task<OrgApplication> GetByIdAsync(Guid id)
    {
        return await this.orgQueryRepository.GetByIdAsync(id);
    }

    public async Task<ICollection<OrgApplicationAudit>> GetAuditHistoryAsync(Guid id)
    {
        return await this.orgQueryRepository.GetAuditHistoryAsync(id);
    }

    public async Task CreateOrganisation()
    {
        await this.unitOfWork.ExecuteAsync(async () =>
        {
            var org = new OrgApplication
            {
                Id = Guid.NewGuid(),
                CustomerOrganisationId = Guid.NewGuid(),
                Users = new List<OrgUser>()
                {
                    new OrgUser
                    {
                        CustomerId = Guid.NewGuid(),
                        DeclarationAccepted = true,
                        DeclarationAcceptedDateTime = DateTime.Now,
                        PrivacyPolicyAccepted = true,
                        PrivacyPolicyAcceptedDateTime = DateTime.Now,
                    },
                },
            };

            await this.orgCommandRepository.CreateOrganisationAsync(org);
        });
    }

    public async Task AddUserToOrganisation(Guid id)
    {
        await this.unitOfWork.ExecuteAsync(async () =>
        {
            var user = new OrgUser
            {
                CustomerId = Guid.NewGuid(),
                DeclarationAccepted = false,
                PrivacyPolicyAccepted = true,
                PrivacyPolicyAcceptedDateTime = DateTime.Now,
            };

            await this.orgCommandRepository.AddUserToOrganisationAsync(id, user);
        });
    }

    public async Task SeedData()
    {
        await this.unitOfWork.ExecuteAsync(
            async () =>
            {
                var orgApplications = new List<OrgApplication>();
                for (var i = 0; i < 10; i++)
                {
                    var application = new OrgApplication
                    {
                        CustomerOrganisationId = Guid.NewGuid(),
                        Id = Guid.NewGuid(),
                    };
                    orgApplications.Add(application);
                }

                await this.orgCommandRepository.SeedDataAsync(orgApplications);
            });
    }
}