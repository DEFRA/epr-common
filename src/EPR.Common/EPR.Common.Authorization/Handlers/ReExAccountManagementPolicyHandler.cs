namespace EPR.Common.Authorization.Handlers;

using System.Security.Claims;
using Config;
using Helpers;
using Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Requirements;
using Sessions;

public sealed class ReExAccountManagementPolicyHandler<TSessionType>
    : PolicyHandlerBase<AccountManagementPolicyRequirement, TSessionType>
    where TSessionType : class, IHasUserData, new()
{
    public ReExAccountManagementPolicyHandler(
        ISessionManager<TSessionType> sessionManager,
        IHttpClientFactory httpClientFactory,
        IOptions<EprAuthorizationConfig> options,
        ILogger<ReExAccountManagementPolicyHandler<TSessionType>> logger,
        string serviceKey)
        : base(sessionManager, httpClientFactory, options, logger, serviceKey)
    {
    }

    protected override string PolicyHandlerName => nameof(ReExAccountManagementPolicyHandler<TSessionType>);
    protected override string PolicyDescription => "manage users";
    protected override Func<ClaimsPrincipal, bool> IsUserAllowed =>
        ClaimsPrincipleHelper.IsReExAdminOrApprovedPerson;
}