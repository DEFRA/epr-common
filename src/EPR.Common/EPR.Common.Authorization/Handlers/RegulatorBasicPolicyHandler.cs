using System.Security.Claims;
using EPR.Common.Authorization.Config;
using EPR.Common.Authorization.Constants;
using EPR.Common.Authorization.Helpers;
using EPR.Common.Authorization.Interfaces;
using EPR.Common.Authorization.Requirements;
using EPR.Common.Authorization.Sessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EPR.Common.Authorization.Handlers;

public sealed class RegulatorBasicPolicyHandler<TSessionType>
    : PolicyHandlerBase<RegulatorBasicPolicyRequirement, TSessionType>
    where TSessionType : class, IHasUserData, new()
{
    public RegulatorBasicPolicyHandler(
        ISessionManager<TSessionType> sessionManager,
        IHttpClientFactory httpClientFactory,
        IOptions<EprAuthorizationConfig> options,
        ILogger<RegulatorBasicPolicyHandler<TSessionType>> logger)
        : base(sessionManager, httpClientFactory, options, logger)
    {
    }

    protected override string PolicyHandlerName => nameof(RegulatorBasicPolicyHandler<TSessionType>);
    protected override string PolicyDescription => ServiceRoles.RegulatorBasic;
    protected override Func<ClaimsPrincipal, bool> IsUserAllowed =>
        ClaimsPrincipleHelper.IsRegulator;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RegulatorBasicPolicyRequirement requirement)
    {
        var httpContext =
            context.Resource as HttpContext ??
            (context.Resource as AuthorizationFilterContext)?.HttpContext;

        if (httpContext is not null)
        {
            var endpoint = httpContext.GetEndpoint();

            // If the endpoint explicitly allows anonymous, bail out early
            if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() != null)
            {
                return;
            }

            // Skip well-known anonymous/system paths that might be hit by probes
            var healthPath = "/admin/health";

            if (!string.IsNullOrWhiteSpace(healthPath) &&
                httpContext.Request.Path.StartsWithSegments(healthPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // This guards in case metadata isn't present during re-exec
            if (httpContext.Request.Path.StartsWithSegments("/error", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        // For everything else, use the base behavior (includes auth checks, cache, DB, and logging)
        await base.HandleRequirementAsync(context, requirement);
    }
}