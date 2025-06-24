namespace EPR.Common.Authorization.Handlers;

using System.Security.Claims;
using Config;
using EPR.Common.Authorization.Constants;
using Helpers;
using Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Requirements;
using Sessions;

public sealed class ReExAccountManagementPolicyHandler<TSessionType>
    : PolicyHandlerBase<ReExAccountManagementPolicyRequirement, TSessionType>
    where TSessionType : class, IHasUserData, new()
{
    private const string _serviceKey = ServiceKeys.ReprocessorExporter; // Re-Ex policies must pass service key to policy handler base

    public ReExAccountManagementPolicyHandler(
        ISessionManager<TSessionType> sessionManager,
        IHttpClientFactory httpClientFactory,
        IOptions<EprAuthorizationConfig> options,
        ILogger<ReExAccountManagementPolicyHandler<TSessionType>> logger)
        : base(sessionManager, httpClientFactory, options, logger, _serviceKey)
    {
    }

    protected override string PolicyHandlerName => nameof(ReExAccountManagementPolicyHandler<TSessionType>);
    protected override string PolicyDescription => "manage users";
    protected override Func<ClaimsPrincipal, bool> IsUserAllowed =>
        ClaimsPrincipleHelper.IsReExAdminOrApprovedPerson;
}