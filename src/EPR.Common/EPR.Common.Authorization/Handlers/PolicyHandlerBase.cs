namespace EPR.Common.Authorization.Handlers;

using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Config;
using Constants;
using Extensions;
using Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;
using Sessions;

public abstract class PolicyHandlerBase<TPolicyRequirement, TSessionType>
    : AuthorizationHandler<TPolicyRequirement>
    where TPolicyRequirement : IAuthorizationRequirement
    where TSessionType : class, IHasUserData, new()
{
    private readonly ISessionManager<TSessionType> _sessionManager;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly EprAuthorizationConfig _config;
    private readonly ILogger<PolicyHandlerBase<TPolicyRequirement, TSessionType>> _logger;
    private readonly string _serviceKey;

	protected PolicyHandlerBase(
        ISessionManager<TSessionType> sessionManager,
        IHttpClientFactory httpClientFactory,
        IOptions<EprAuthorizationConfig> options,
        ILogger<PolicyHandlerBase<TPolicyRequirement, TSessionType>> logger,
        string serviceKey = null)
    {
        _sessionManager = sessionManager;
        _httpClientFactory = httpClientFactory;
        _config = options.Value;
        _logger = logger;
        _serviceKey = serviceKey ?? _config.ServiceKey;
	}

    protected abstract string PolicyHandlerName { get; }
    protected abstract string PolicyDescription { get; }
    protected abstract Func<ClaimsPrincipal, bool> IsUserAllowed { get; }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TPolicyRequirement requirement)
    {
        try
        {
            if (context.User.Identity?.IsAuthenticated != true)
            {
                _logger.LogWarning("User is unauthenticated");
                return;
            }

            // check context
            if (CheckContext(context, requirement))
                return;

            // check cache
            if (context.Resource is not HttpContext httpContext)
            {
                _logger.LogError("Error getting HttpContext in {PolicyHandler} for user {UserId}",
                    PolicyHandlerName, context.User.UserId());
                return;
            }

            var cacheResult = await CheckCache(context, requirement, httpContext);
            if (cacheResult.InCache)
                return;

            // check db via facade
            if (await CheckDatabase(context, requirement, cacheResult.Session, httpContext))
                return;

            _logger.LogWarning("User {UserId} does not have permission to {PolicyDescription}",
                context.User.UserId(), PolicyDescription);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in {PolicyHandler} for user {UserId}",
                PolicyHandlerName, context.User.UserId());
        }
	}

	private async Task<bool> CheckDatabase(
	AuthorizationHandlerContext context,
	TPolicyRequirement requirement,
	TSessionType session,
	HttpContext httpContext)
	{
		using var httpClient = _httpClientFactory.CreateClient(FacadeConstants.FacadeAPIClient);

		if (_serviceKey == ServiceKeys.ReprocessorExporter)
		{
			return await HandleReprocessorExporterFlow(context, session, httpContext, requirement, httpClient);
		}

		// Default flow for other service keys
		var dbResponse = await FetchUserOrganisationsAsync(httpClient, _config.FacadeUserAccountEndpoint);
		if (dbResponse == null) return false;

		_logger.LogInformation("User {UserId} data fetched from standard flow", context.User.UserId());

		await UpdateUserSessionAndClaimsAsync(context, session, httpContext, dbResponse.User);
		return FinalizeAuthorization(context, requirement);
	}
	private async Task<bool> HandleReprocessorExporterFlow(
		AuthorizationHandlerContext context,
		TSessionType session,
		HttpContext httpContext,
		TPolicyRequirement requirement,
		HttpClient httpClient)
	{
		var endpoint = string.Format(_config.FacadeUserAccountV1Endpoint, _serviceKey);
		var dbResponse = await FetchUserOrganisationsAsync(httpClient, endpoint);
		if (dbResponse == null) return false;

		var organisations = dbResponse.User.Organisations;
		if (organisations == null || !organisations.Any())
		{
			_logger.LogWarning("User {UserId} has no organisations assigned", context.User.UserId());
			return false;
		}

		if (!TryGetSelectedOrganisation(organisations, session, out var selectedOrg, out var warning))
		{
			_logger.LogWarning("User {UserId}: {WarningMessage}", context.User.UserId(), warning);
			return RedirectToSelectOrganisation(context, httpContext);
		}

		dbResponse.User.Organisations = new List<Organisation> { selectedOrg! };

		_logger.LogInformation("User {UserId} organisation resolved to {OrgId}", context.User.UserId(), selectedOrg!.Id);

		await UpdateUserSessionAndClaimsAsync(context, session, httpContext, dbResponse.User);
		return FinalizeAuthorization(context, requirement);
	}

	private async Task<UserOrganisations?> FetchUserOrganisationsAsync(HttpClient httpClient, string endpoint)
	{
		try
		{
			var response = await httpClient.GetAsync(endpoint);
			if (!response.IsSuccessStatusCode)
			{
				_logger.LogError("Failed to fetch user data. Endpoint: {Endpoint}, Status: {Status}", endpoint, response.StatusCode);
				return null;
			}

			return await response.Content.ReadFromJsonAsync<UserOrganisations>();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Exception occurred while fetching user organisations from {Endpoint}", endpoint);
			return null;
		}
	}

	private static bool TryGetSelectedOrganisation(
		List<Organisation> organisations,
		TSessionType session,
		out Organisation? selectedOrganisation,
		out string? warningMessage)
	{
		selectedOrganisation = null;
		warningMessage = null;

		if (organisations.Count == 1)
		{
			selectedOrganisation = organisations[0];
			return true;
		}

		var sessionOrganisations = session.UserData?.Organisations;
		if (sessionOrganisations == null || sessionOrganisations.Count != 1)
		{
			warningMessage = "Multiple organisations but no selected organisation in session";
			return false;
		}

		var selectedOrganisationId = sessionOrganisations[0].Id;
		if (!selectedOrganisationId.HasValue)
		{
			warningMessage = "Selected organisation ID is null or empty in session";
			return false;
		}

		selectedOrganisation = organisations.FirstOrDefault(org => org.Id == selectedOrganisationId);
		if (selectedOrganisation == null)
		{
			warningMessage = "Selected organisation not found in available organisations from response";
			return false;
		}

		return true;
	}

	private bool RedirectToSelectOrganisation(AuthorizationHandlerContext context, HttpContext httpContext)
	{
		if (!string.IsNullOrEmpty(_config.SelectOrganisationRedirect))
		{
			httpContext.Response.Redirect(_config.SelectOrganisationRedirect);
			return false;
		}

		_logger.LogWarning("User {UserId} has multiple organisations assigned, but no redirect configured",
				context.User.UserId());
		return false;
	}

	private async Task UpdateUserSessionAndClaimsAsync(
	AuthorizationHandlerContext context,
	TSessionType session,
	HttpContext httpContext,
	UserData user)
	{
		context.User.AddOrUpdateUserData(user);
		session.UserData = user;

		await _sessionManager.SaveSessionAsync(httpContext.Session, session);
		await UpdateClaimsAndSignInAsync(httpContext, user);
	}

	private bool FinalizeAuthorization(AuthorizationHandlerContext context, TPolicyRequirement requirement)
	{
		if (!IsUserAllowed(context.User))
			return false;

		context.Succeed(requirement);
		_logger.LogInformation("User {UserId} has permission to {PolicyDescription}", context.User.UserId(), PolicyDescription);
		return true;
	}

	private async Task<(bool InCache, TSessionType Session)> CheckCache(
        AuthorizationHandlerContext context,
        TPolicyRequirement requirement,
        HttpContext httpContext)
    {
        TSessionType consumerSession = await _sessionManager.GetSessionAsync(httpContext.Session) ?? new TSessionType();

        if (consumerSession is not { UserData: { } })
        {
            return (InCache: false, Session: consumerSession);
        }

        context.User.AddOrUpdateUserData(consumerSession.UserData);

        await UpdateClaimsAndSignInAsync(httpContext, consumerSession.UserData);

        if (IsUserAllowed(context.User))
        {
            context.Succeed(requirement);
            _logger.LogInformation("User {UserId} has permission to {PolicyDescription}",
                context.User.UserId(), PolicyDescription);
            return (true, consumerSession);
        }

        return (false, consumerSession);
    }

    private bool CheckContext(AuthorizationHandlerContext context, TPolicyRequirement requirement)
    {
        if (!IsUserAllowed(context.User))
        {
            return false;
        }

        context.Succeed(requirement);
        _logger.LogInformation("User {UserId} has permission to {PolicyDescription}",
            context.User.UserId(), PolicyDescription);
        return true;
    }

    private async Task UpdateClaimsAndSignInAsync(HttpContext httpContext, UserData userData)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.UserData, JsonSerializer.Serialize(userData)),
        };
        var claimsIdentity = new ClaimsIdentity(httpContext.User.Identity, claims);
        var principal = new ClaimsPrincipal(claimsIdentity);
        var properties = httpContext.Features.Get<IAuthenticateResultFeature>()?.AuthenticateResult?.Properties;

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);

        if (!string.IsNullOrEmpty(_config.SignInRedirect))
        {
            httpContext.Response.Redirect(_config.SignInRedirect);
        }
    }
}