namespace EPR.Common.Authorization.Handlers;

using Config;
using Constants;
using Interfaces;
using Models;
using Requirements;
using Sessions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Json;

public sealed class ReExAccountManagementPolicyHandler<TSessionType>(
	ISessionManager<TSessionType> sessionManager,
	IHttpClientFactory httpClientFactory,
	IOptions<EprAuthorizationConfig> options,
	ILogger<ReExAccountManagementPolicyHandler<TSessionType>> logger,
	IHttpContextAccessor httpContextAccessor)
	: PolicyHandlerBase<ReExAccountManagementRequirement, TSessionType>(sessionManager, httpClientFactory, options, logger)
	where TSessionType : class, IHasUserData, new()
{
	private readonly ISessionManager<TSessionType> _sessionManager = sessionManager;
	private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
	private readonly EprAuthorizationConfig _config = options.Value;

	protected override string PolicyHandlerName => nameof(ReExAccountManagementPolicyHandler<TSessionType>);
	protected override string PolicyDescription => "manage users";

	protected override Func<ClaimsPrincipal, bool> IsUserAllowed => claimsPrincipal =>
	{
		var context = _httpContextAccessor.HttpContext;
		if (context == null)
			return false;

		var userDataClaim = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.UserData)?.Value;
		if (string.IsNullOrWhiteSpace(userDataClaim))
			return false;

		var session = context.Session;
		TSessionType consumerSession;

		try
		{
			// forced to use .Result because of sync constraint
			consumerSession = _sessionManager.GetSessionAsync(session).Result ?? new TSessionType();
		}
		catch
		{
			return false;
		}

		var organisationId = GetOrSetSelectedOrganisationId(context, consumerSession);
		if (!organisationId.HasValue || organisationId.Value == Guid.Empty)
		{
			context.Response.Redirect(_config.SelectOrganisationRedirect);
			return false;
		}

		try
		{
			var userData = JsonSerializer.Deserialize<UserData>(userDataClaim);
			var enrolments = userData?.Organisations?
				.FirstOrDefault(o => o.Id == organisationId)?
				.Enrolments;

			if (enrolments == null || enrolments.Count == 0)
				return false;

			return enrolments.Any(e => AllowedRoles.Contains(e.ServiceRoleKey));
		}
		catch (JsonException)
		{
			return false;
		}
	};

	private Guid? GetOrSetSelectedOrganisationId(HttpContext context, TSessionType session)
	{
		if (session.SelectedOrganisationId != null && session.SelectedOrganisationId != Guid.Empty)
			return session.SelectedOrganisationId;

		var routeValues = context.Request.RouteValues;
		if (routeValues.TryGetValue("organisationId", out var organisationIdObject) &&
			Guid.TryParse(organisationIdObject?.ToString(), out var parsedOrganisationId))
		{
			session.SelectedOrganisationId = parsedOrganisationId;
			try
			{
				// We are in a sync context so forced to block
				_sessionManager.SaveSessionAsync(context.Session, session).Wait();
			}
			catch
			{
				return null;
			}

			return parsedOrganisationId;
		}

		return null;
	}

	private static readonly HashSet<string> AllowedRoles = new()
	{
		ServiceRoleKeys.ReExAdminUser,
		ServiceRoleKeys.ReExApprovedPerson,
		ServiceRoleKeys.ReExStandardUser,
		ServiceRoleKeys.ReExBasicUser
	};
}
