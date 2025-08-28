namespace EPR.Common.Authorization.Test.Handlers;

using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Authorization.Handlers;
using Authorization.Sessions;
using AutoFixture;
using AutoFixture.AutoMoq;
using Config;
using Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Models;
using Moq;
using Moq.Protected;
using TestClasses;

[TestClass]
public abstract class PolicyHandlerTestsBase<TPolicyHandler, TPolicyRequirement, TSession>
    where TPolicyHandler : PolicyHandlerBase<TPolicyRequirement, TSession>
    where TPolicyRequirement : IAuthorizationRequirement, new()
    where TSession : class, IHasUserData, new()
{
    private readonly IFixture _fixture = new Fixture().Customize(new AutoMoqCustomization());
    private readonly Mock<ISessionManager<MySession>> _sessionManagerMock = new();
    private readonly Mock<IOptions<EprAuthorizationConfig>> _optionsMock = new();
    private readonly Mock<IAuthenticationService> _authenticationServiceMock = new();
    private readonly Mock<HttpContext> _httpContextMock = new();
    private Mock<HttpMessageHandler> _httpMessageHandlerMock = new();
    private HttpClient _httpClient;
    private readonly Mock<IHttpClientFactory> _httpClientFactory = new();
    private TPolicyHandler _policyHandler;

    protected Mock<HttpResponse> HttpResponseMock { get; } = new();

    protected void SetUp()
    {
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        _httpMessageHandlerMock = new();

        var config = new EprAuthorizationConfig
        {
            FacadeUserAccountEndpoint = "endpoint",
            FacadeBaseUrl = "http://localhost"
        };
        _optionsMock.Setup(x => x.Value).Returns(config);

        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClient.BaseAddress = new Uri(config.FacadeBaseUrl);
        _httpClientFactory.Setup(x => x.CreateClient("FacadeAPIClient"))
            .Returns(_httpClient);

        _authenticationServiceMock
            .Setup(x => x.SignInAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask);

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(x => x.GetService(typeof(IAuthenticationService)))
            .Returns(_authenticationServiceMock.Object);

        _httpContextMock.SetupGet(x => x.RequestServices).Returns(serviceProviderMock.Object);

        _policyHandler = Activator.CreateInstance(
            typeof(TPolicyHandler),
            _sessionManagerMock.Object,
            _httpClientFactory.Object,
            _optionsMock.Object,
            NullLogger<TPolicyHandler>.Instance) as TPolicyHandler;

        Assert.IsNotNull(_policyHandler);
    }

    protected void SetupSignInRedirect(string signInRedirectValue)
    {
        var config = new EprAuthorizationConfig
        {
            FacadeUserAccountEndpoint = "endpoint",
            FacadeBaseUrl = "http://localhost",
            SignInRedirect = signInRedirectValue
        };

        _optionsMock.Setup(options => options.Value).Returns(config);
        _httpContextMock.SetupGet(context => context.Response).Returns(HttpResponseMock.Object);

        _policyHandler = Activator.CreateInstance(
            typeof(TPolicyHandler),
            _sessionManagerMock.Object,
            _httpClientFactory.Object,
            _optionsMock.Object,
            NullLogger<TPolicyHandler>.Instance) as TPolicyHandler;
    }

    protected async Task HandleRequirementAsync_Succeeds_WhenUserHasRolesInClaim(
        string serviceRole, string roleInOrganisation, string enrolmentStatus)
    {
        // Arrange
        var userData = _fixture.Build<UserData>()
            .With(x => x.ServiceRole, serviceRole)
            .With(x => x.RoleInOrganisation, roleInOrganisation)
            .With(x => x.EnrolmentStatus, enrolmentStatus)
            .Create();

        var objectId = "12345678-1234-1234-1234-123456789012";
        var claims = new[]
        {
            new Claim(ClaimConstants.ObjectId, objectId),
            new Claim(ClaimTypes.UserData, JsonSerializer.Serialize(userData))
        };

        var claimsIdentity = new ClaimsIdentity(claims, "CustomAuthenticationType");
        var user = new ClaimsPrincipal(claimsIdentity);

        _httpContextMock.SetupGet(x => x.User).Returns(user);

        var authorizationHandlerContext = new AuthorizationHandlerContext(
            new List<IAuthorizationRequirement> { new TPolicyRequirement() },
            user,
            _httpContextMock.Object);

        // Act
        await _policyHandler.HandleAsync(authorizationHandlerContext);

        // Assert
        Assert.IsTrue(authorizationHandlerContext.HasSucceeded);
        _sessionManagerMock.Verify(x => x.GetSessionAsync(It.IsAny<ISession>()), Times.Never);
        _sessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<MySession>()), Times.Never);
    }

    protected async Task HandleRequirementAsync_Fails_WhenUserIsNotAuthenticated(
        string serviceRole, string roleInOrganisation, string enrolmentStatus)
    {
        // Arrange
        var userData = _fixture.Build<UserData>()
            .With(x => x.ServiceRole, serviceRole)
            .With(x => x.RoleInOrganisation, roleInOrganisation)
            .With(x => x.EnrolmentStatus, enrolmentStatus)
            .Create();

        var objectId = "12345678-1234-1234-1234-123456789012";
        var claims = new[]
        {
            new Claim(ClaimConstants.ObjectId, objectId),
            new Claim(ClaimTypes.UserData, JsonSerializer.Serialize(userData))
        };

        var claimsIdentity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(claimsIdentity);

        _httpContextMock.SetupGet(x => x.User).Returns(user);

        var authorizationHandlerContext = new AuthorizationHandlerContext(
            new List<IAuthorizationRequirement> { new TPolicyRequirement() },
            user,
            _httpContextMock.Object);

        // Act
        await _policyHandler.HandleAsync(authorizationHandlerContext);

        // Assert
        Assert.IsFalse(authorizationHandlerContext.HasSucceeded);
    }

    protected async Task HandleRequirementAsync_Fails_WhenUserDataExistsInClaimButUserRoleIsNotAuthorised(
        string serviceRole, string roleInOrganisation, string enrolmentStatus)
    {
        // Arrange
        var userData = _fixture.Build<UserData>()
            .With(x => x.ServiceRole, serviceRole)
            .With(x => x.RoleInOrganisation, roleInOrganisation)
            .With(x => x.EnrolmentStatus, enrolmentStatus)
            .Create();

        var objectId = "12345678-1234-1234-1234-123456789012";
        var claims = new[]
        {
            new Claim(ClaimConstants.ObjectId, objectId),
            new Claim(ClaimTypes.UserData, JsonSerializer.Serialize(userData))
        };

        var claimsIdentity = new ClaimsIdentity(claims, "CustomAuthenticationType");
        var user = new ClaimsPrincipal(claimsIdentity);

        _httpContextMock.SetupGet(x => x.User).Returns(user);

        var authorizationHandlerContext = new AuthorizationHandlerContext(
            new List<IAuthorizationRequirement> { new TPolicyRequirement() },
            user,
            _httpContextMock.Object);

        // Act
        await _policyHandler.HandleAsync(authorizationHandlerContext);

        // Assert
        Assert.IsFalse(authorizationHandlerContext.HasSucceeded);
    }

    protected async Task HandleRequirementAsync_Fails_WhenAuthorizationHandlerContextResourceDoesNotExist()
    {
        // Arrange
        var objectId = "12345678-1234-1234-1234-123456789012";
        var claims = new[]
        {
            new Claim(ClaimConstants.ObjectId, objectId)
        };

        var claimsIdentity = new ClaimsIdentity(claims, "CustomAuthenticationType");
        var user = new ClaimsPrincipal(claimsIdentity);

        var authorizationHandlerContext = new AuthorizationHandlerContext(
            new List<IAuthorizationRequirement> { new TPolicyRequirement() },
            user,
            null);

        // Act
        await _policyHandler.HandleAsync(authorizationHandlerContext);

        // Assert
        Assert.IsFalse(authorizationHandlerContext.HasSucceeded);
    }

    protected async Task HandleRequirementAsync_Succeeds_WhenCacheContainRequiredUserData(
        string serviceRole, string roleInOrganisation, string enrolmentStatus)
    {
        // Arrange
        var objectId = "12345678-1234-1234-1234-123456789012";
        var claims = new[]
        {
            new Claim(ClaimConstants.ObjectId, objectId)
        };

        var claimsIdentity = new ClaimsIdentity(claims, "CustomAuthenticationType");
        var user = new ClaimsPrincipal(claimsIdentity);

        var featureCollection = BuildFeatureCollection(user);
        _httpContextMock.Setup(x => x.Features).Returns(featureCollection);
        _httpContextMock.Setup(x => x.User).Returns(user);

        var authorizationHandlerContext = new AuthorizationHandlerContext(
            new List<IAuthorizationRequirement> { new TPolicyRequirement() },
            user,
            _httpContextMock.Object);

        var mySession = new MySession
        { UserData = new UserData { ServiceRole = serviceRole, RoleInOrganisation = roleInOrganisation, EnrolmentStatus = enrolmentStatus } };
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(mySession);

        // Act
        await _policyHandler.HandleAsync(authorizationHandlerContext);

        // Assert
        Assert.IsTrue(authorizationHandlerContext.HasSucceeded);
        _sessionManagerMock.Verify(x => x.GetSessionAsync(It.IsAny<ISession>()), Times.Once);
    }

    protected async Task HandleRequirementAsync_Fails_WhenUserDataExistsInCacheButUserRoleIsNotAuthorised(
        string serviceRole, string roleInOrganisation, string enrolmentStatus)
    {
        // Arrange
        var objectId = "12345678-1234-1234-1234-123456789012";
        var claims = new[]
        {
            new Claim(ClaimConstants.ObjectId, objectId)
        };

        var claimsIdentity = new ClaimsIdentity(claims, "CustomAuthenticationType");
        var user = new ClaimsPrincipal(claimsIdentity);

        var featureCollection = BuildFeatureCollection(user);
        _httpContextMock.Setup(x => x.Features).Returns(featureCollection);
        _httpContextMock.Setup(x => x.User).Returns(user);

        var authorizationHandlerContext = new AuthorizationHandlerContext(
            new List<IAuthorizationRequirement> { new TPolicyRequirement() },
            user,
            _httpContextMock.Object);

        var mySession = new MySession
        { UserData = new UserData { ServiceRole = serviceRole, RoleInOrganisation = roleInOrganisation, EnrolmentStatus = enrolmentStatus } };
        _sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(mySession);

        // Act
        await _policyHandler.HandleAsync(authorizationHandlerContext);

        // Assert
        Assert.IsFalse(authorizationHandlerContext.HasSucceeded);
    }

    protected async Task HandleRequirementAsync_Succeeds_WhenUserDataIsRetrievedFromApi(
        string serviceRole, string roleInOrganisation, string enrolmentStatus)
    {
        // Arrange
        var objectId = "12345678-1234-1234-1234-123456789012";
        var claims = new[]
        {
            new Claim(ClaimConstants.ObjectId, objectId)
        };

        var claimsIdentity = new ClaimsIdentity(claims, "CustomAuthenticationType");
        var user = new ClaimsPrincipal(claimsIdentity);

        var featureCollection = BuildFeatureCollection(user);
        _httpContextMock.Setup(x => x.Features).Returns(featureCollection);
        _httpContextMock.Setup(x => x.User).Returns(user);

        var authorizationHandlerContext = new AuthorizationHandlerContext(
            new List<IAuthorizationRequirement> { new TPolicyRequirement() },
            user,
            _httpContextMock.Object);

        _sessionManagerMock
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync((MySession)null!);

        var userData = new UserData
        { ServiceRole = serviceRole, RoleInOrganisation = roleInOrganisation, EnrolmentStatus = enrolmentStatus };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new UserOrganisations { User = userData }))
            })
            .Verifiable();

        // Act
        await _policyHandler.HandleAsync(authorizationHandlerContext);

        // Assert
        Assert.IsTrue(authorizationHandlerContext.HasSucceeded);
        _sessionManagerMock.Verify(x => x.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<MySession>()), Times.Once);
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
            ItExpr.IsAny<CancellationToken>());
    }

    protected async Task HandleRequirementAsync_Fails_WhenUserDataIsRetrievedFromApiButUserRoleIsNotAuthorised(
        string serviceRole, string roleInOrganisation, string enrolmentStatus)
    {
        // Arrange
        var objectId = "12345678-1234-1234-1234-123456789012";
        var claims = new[]
        {
            new Claim(ClaimConstants.ObjectId, objectId)
        };

        var claimsIdentity = new ClaimsIdentity(claims, "CustomAuthenticationType");
        var user = new ClaimsPrincipal(claimsIdentity);

        var featureCollection = BuildFeatureCollection(user);
        _httpContextMock.Setup(x => x.Features).Returns(featureCollection);
        _httpContextMock.Setup(x => x.User).Returns(user);

        var authorizationHandlerContext = new AuthorizationHandlerContext(
            new List<IAuthorizationRequirement> { new TPolicyRequirement() },
            user,
            _httpContextMock.Object);

        _sessionManagerMock
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync((MySession)null!);

        var userData = new UserData
        { ServiceRole = serviceRole, RoleInOrganisation = roleInOrganisation, EnrolmentStatus = enrolmentStatus };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new UserOrganisations { User = userData }))
            })
            .Verifiable();

        // Act
        await _policyHandler.HandleAsync(authorizationHandlerContext);

        // Assert
        Assert.IsFalse(authorizationHandlerContext.HasSucceeded);
    }

    protected async Task HandleRequirementAsync_Fails_WhenApiCallFails()
    {
        // Arrange
        var objectId = "12345678-1234-1234-1234-123456789012";
        var claims = new[]
        {
            new Claim(ClaimConstants.ObjectId, objectId)
        };
        var claimsIdentity = new ClaimsIdentity(claims, "CustomAuthenticationType");
        var user = new ClaimsPrincipal(claimsIdentity);

        var featureCollection = BuildFeatureCollection(user);
        _httpContextMock.Setup(x => x.Features).Returns(featureCollection);
        _httpContextMock.Setup(x => x.User).Returns(user);

        var authorizationHandlerContext = new AuthorizationHandlerContext(
            new List<IAuthorizationRequirement> { new TPolicyRequirement() },
            user,
            _httpContextMock.Object);

        _sessionManagerMock
            .Setup(x => x.GetSessionAsync(It.IsAny<ISession>()))
            .ReturnsAsync((MySession)null!);

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
            })
            .Verifiable();

        // Act
        await _policyHandler.HandleAsync(authorizationHandlerContext);

        // Assert
        Assert.IsFalse(authorizationHandlerContext.HasSucceeded);
    }

    private FeatureCollection BuildFeatureCollection(ClaimsPrincipal user)
    {
        var authenticationProperties = new AuthenticationProperties();

        var authenticateResultFeatureMock = new Mock<IAuthenticateResultFeature>();
        authenticateResultFeatureMock.SetupGet(x => x.AuthenticateResult)
            .Returns(AuthenticateResult.Success(new AuthenticationTicket(user, authenticationProperties, "CustomAuthenticationType")));

        var featureCollection = new FeatureCollection();
        featureCollection.Set(authenticateResultFeatureMock.Object);

        return featureCollection;
    }
}