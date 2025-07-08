namespace EPR.Common.Authorization.Handlers;

using Config;
using Constants;
using EPR.Common.Authorization.Extensions;
using Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;
using Requirements;
using Sessions;
using System.Security.Claims;
using System.Text.Json;

public sealed class ReExAddTeamMemberPolicyHandler<TSessionType> : PolicyHandlerBase<ReExAddTeamMemberRequirement, TSessionType>
	where TSessionType : class, IHasUserData, new()
{
	private readonly ISessionManager<TSessionType> _sessionManager;
	private readonly IHttpContextAccessor _httpContextAccessor;
	private readonly EprAuthorizationConfig _config;
	private readonly ILogger<ReExAddTeamMemberPolicyHandler<TSessionType>> _logger;

	public ReExAddTeamMemberPolicyHandler(
		ISessionManager<TSessionType> sessionManager,
		IHttpClientFactory httpClientFactory,
		IOptions<EprAuthorizationConfig> options,
		ILogger<ReExAddTeamMemberPolicyHandler<TSessionType>> logger,
		IHttpContextAccessor httpContextAccessor) : base(sessionManager, httpClientFactory, options, logger)
	{
		_sessionManager = sessionManager;
		_httpContextAccessor = httpContextAccessor;
		_config = options.Value;
		_logger = logger;
	}

	protected override string PolicyHandlerName => nameof(ReExAddTeamMemberPolicyHandler<TSessionType>);
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

		var organisationId = ResolveSelectedOrganisationId(context, consumerSession);
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



	private Guid? ResolveSelectedOrganisationId(HttpContext context, TSessionType session)
	{
		if (session.SelectedOrganisationId.HasValue && session.SelectedOrganisationId.Value != Guid.Empty)
			return session.SelectedOrganisationId;

		if (context.Request.RouteValues.TryGetValue("organisationId", out var organisationIdObject) &&
			Guid.TryParse(organisationIdObject?.ToString(), out var parsedOrganisationId))
		{
			session.SelectedOrganisationId = parsedOrganisationId;

			try
			{
				// We are in a sync context so forced to block
				_sessionManager.SaveSessionAsync(context.Session, session).Wait();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error saving session in {PolicyHandler} for user {UserId}",
					PolicyHandlerName, context.User.UserId());
				return null;
			}

			return parsedOrganisationId;
		}

		_logger.LogWarning("Failed to resolve organisationId from route values for user {UserId}",
			context.User.UserId());

		return null;
	}

	private static readonly HashSet<string> AllowedRoles = new()
	{
		ServiceRoleKeys.ReExAdminUser,
		ServiceRoleKeys.ReExApprovedPerson
	};
}