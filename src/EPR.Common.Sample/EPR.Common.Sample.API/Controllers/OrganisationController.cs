namespace EPR.Common.Sample.API.Controllers;

using Common.Data.Tables.Audit;
using Data.Tables;
using Functions.AccessControl;
using Functions.Http.Interfaces;
using Functions.Services;
using Services.Interfaces;

[ApiController]
[Route("[controller]")]
[Microsoft.AspNetCore.Authorization.Authorize]
public class OrganisationController : ControllerBase
{
    private readonly IHttpRequestWrapper<CommonPermission> httpRequestWrapper;
    private readonly IOrganisationService organisationService;
    private readonly ContextAdminOverride contextAdminOverride;
    private readonly ILogger<OrganisationController> logger;

    public OrganisationController(IHttpRequestWrapper<CommonPermission> httpRequestWrapper, IOrganisationService organisationService, ContextAdminOverride contextAdminOverride, ILogger<OrganisationController> logger)
    {
        this.httpRequestWrapper = httpRequestWrapper;
        this.organisationService = organisationService;
        this.contextAdminOverride = contextAdminOverride;
        this.logger = logger;
    }

    [HttpGet("~/GetDocuments")]
    public async Task<ActionResult<IEnumerable<OrgApplication>>> GetDocuments(CancellationToken cancellationToken) =>
        await this.httpRequestWrapper.Execute(
            new List<CommonPermission> { CommonPermission.AllowAll },
            async () => new OkObjectResult(await this.organisationService.GetAsync()),
            cancellationToken);

    [HttpGet("~/GetDocument")]
    public async Task<ActionResult<OrgApplication>> GetDocument(Guid organisationId, CancellationToken cancellationToken) =>
        await this.httpRequestWrapper.Execute(
            new List<CommonPermission> { CommonPermission.AllowWrite,  },
            async () => new OkObjectResult(await this.organisationService.GetByIdAsync(organisationId)),
            cancellationToken);

    [HttpGet("~/GetAuditHistory")]
    public async Task<ActionResult<IEnumerable<OrgApplicationAudit>>> GetAuditHistory(Guid organisationId, CancellationToken cancellationToken) =>
        await this.httpRequestWrapper.Execute(
            new List<CommonPermission> { CommonPermission.AllowAll },
            async () => new OkObjectResult(await this.organisationService.GetAuditHistoryAsync(organisationId)),
            cancellationToken);

    [HttpPost("~/CreateDocument")]
    public async Task<ActionResult> CreateDocument(CancellationToken cancellationToken) => await this.httpRequestWrapper.Execute(
        new List<CommonPermission> { CommonPermission.AllowAll },
        async () =>
        {
            await this.organisationService.CreateOrganisation();
            return new OkResult();
        }, cancellationToken);

    [HttpPost("~/CreateUser")]
    public async Task<ActionResult> CreateUser(Guid organisationId, CancellationToken cancellationToken) => await this.httpRequestWrapper.Execute(
        new List<CommonPermission> { CommonPermission.AllowAll },
        async () =>
        {
            await this.organisationService.AddUserToOrganisation(organisationId);
            return new OkResult();
        }, cancellationToken);

    [HttpPost("~/SeedData")]
    public async Task<ActionResult> SeedData(CancellationToken cancellationToken) => await this.httpRequestWrapper.Execute(
        new List<CommonPermission> { CommonPermission.AllowAll },
        async () =>
        {
            using (this.contextAdminOverride.OverrideContext(Guid.Empty, "system@here.com"))
            {
                await this.organisationService.SeedData();
                return new OkResult();
            }
        }, cancellationToken);
}