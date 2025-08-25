namespace EPR.Common.Authorization.Test.Handlers;

using Authorization.Handlers;
using AutoFixture;
using Config;
using Constants;
using FluentAssertions;
using Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Models;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TestClasses;

[TestClass]
public abstract class ReExPolicyHandlerTestFixture<TPolicyHandler, TPolicyRequirement, TSession>
	: PolicyHandlerTestsBase<TPolicyHandler, TPolicyRequirement, TSession>
	where TPolicyHandler : PolicyHandlerBase<TPolicyRequirement, TSession>
	where TPolicyRequirement : IAuthorizationRequirement, new()
	where TSession : class, IHasUserData, new()
{
	protected Guid _testOrganisationId;
	protected Mock<IHttpContextAccessor> _httpContextAccessorMock = null!;

	protected new void SetUp() // Use 'new' to hide the base SetUp method
	{
		// IMPORTANT: We do NOT call base.SetUp() here directly,
		// as base.SetUp() would try to instantiate _policyHandler incorrectly.
		// Instead, we perform the necessary common setups ourselves.
		// Common setups copied from PolicyHandlerTestsBase.SetUp()
		var config = new EprAuthorizationConfig
		{
			FacadeBaseUrl = "https://test.api/",
			SignInRedirect = "/Account/SignIn",
			SelectOrganisationRedirect = "/SelectOrganisation"
		};
		_optionsMock.Setup(x => x.Value).Returns(config);

		_httpMessageHandlerMock = new Mock<HttpMessageHandler>();
		_httpClient = new HttpClient(_httpMessageHandlerMock.Object)
		{
			BaseAddress = new Uri(config.FacadeBaseUrl)
		};
		_httpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(_httpClient);

		_httpContextMock.SetupGet(x => x.Session).Returns(Mock.Of<ISession>());
		_httpContextMock.SetupGet(x => x.Response).Returns(HttpResponseMock.Object);
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

		_testOrganisationId = Guid.NewGuid();
		_httpContextAccessorMock = new Mock<IHttpContextAccessor>();

		// Correct _policyHandler instantiation for ReEx handlers (5 arguments)
		_policyHandler = Activator.CreateInstance(
				typeof(TPolicyHandler),
				_sessionManagerMock.Object,
				_httpClientFactory.Object,
				_optionsMock.Object,
				NullLogger<TPolicyHandler>.Instance,
				_httpContextAccessorMock.Object) as TPolicyHandler;
		Assert.IsNotNull(_policyHandler);
	}

	protected async Task HandleRequirementAsync_Succeeds_WhenUserHasAllowedRoleInClaimWithOrganisation(
		string serviceRoleKey, string roleInOrganisation, string enrolmentStatus, Guid organisationId)
	{
		// Arrange
		var userData = _fixture.Build<UserData>()
			.With(x => x.Organisations,
			[
				new Organisation
				{
					Id = organisationId,
					Name = "Test Organisation",
					PersonRoleInOrganisation = roleInOrganisation,
					Enrolments =
					[
						new Enrolment
						{
							ServiceRoleKey = serviceRoleKey,
							EnrolmentStatus = enrolmentStatus
						}
					]
				}
			])
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
		var routeValueDictionary = new RouteValueDictionary
		{
			{ "organisationId", organisationId.ToString() }
		};
		var featureCollection = BuildFeatureCollection(user);
		_httpContextMock.Setup(x => x.Features).Returns(featureCollection);
		_httpContextMock.Setup(x => x.Request.RouteValues).Returns(routeValueDictionary);
		_httpContextAccessorMock.SetupGet(x => x.HttpContext).Returns(_httpContextMock.Object);

		var authorizationHandlerContext = new AuthorizationHandlerContext(
			new List<IAuthorizationRequirement> { new TPolicyRequirement() },
			user,
			_httpContextMock.Object);

		// Act
		await _policyHandler.HandleAsync(authorizationHandlerContext);

		// Assert
		authorizationHandlerContext.HasSucceeded.Should().BeTrue();
	}

	protected async Task HandleRequirementAsync_Fails_WhenUserDataExistsInClaimButUserRoleIsNotAuthorisedWithOrganisation(
		string serviceRoleKey, string roleInOrganisation, string enrolmentStatus, Guid organisationId)
	{
		// Arrange
		var userData = _fixture.Build<UserData>()
			.With(x => x.Organisations,
			[
				new Organisation
				{
					Id = organisationId,
					Name = "Test Organisation",
					PersonRoleInOrganisation = roleInOrganisation,
					Enrolments =
					[
						new Enrolment
						{
							ServiceRoleKey = serviceRoleKey,
							EnrolmentStatus = enrolmentStatus
						}
					]
				}
			])
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
		var routeValueDictionary = new RouteValueDictionary
		{
			{ "organisationId", organisationId.ToString() }
		};
		_httpContextMock.Setup(x => x.Request.RouteValues).Returns(routeValueDictionary);
		_httpContextAccessorMock.SetupGet(x => x.HttpContext).Returns(_httpContextMock.Object);

		var authorizationHandlerContext = new AuthorizationHandlerContext(
			new List<IAuthorizationRequirement> { new TPolicyRequirement() },
			user,
			_httpContextMock.Object);

		// Act
		await _policyHandler.HandleAsync(authorizationHandlerContext);

		// Assert
		authorizationHandlerContext.HasSucceeded.Should().BeFalse();
	}

	protected async Task HandleRequirementAsync_Fails_WhenUserDataClaimIsMissing()
	{
		// Arrange
		var objectId = "12345678-1234-1234-1234-123456789012";
		var claims = new[]
		{
			new Claim(ClaimConstants.ObjectId, objectId)
            // No UserData claim
        };

		var claimsIdentity = new ClaimsIdentity(claims, "CustomAuthenticationType");
		var user = new ClaimsPrincipal(claimsIdentity);

		_httpContextMock.SetupGet(x => x.User).Returns(user);
		_httpContextAccessorMock.SetupGet(x => x.HttpContext).Returns(_httpContextMock.Object);

		var authorizationHandlerContext = new AuthorizationHandlerContext(
			new List<IAuthorizationRequirement> { new TPolicyRequirement() },
			user,
			_httpContextMock.Object);

		// Act
		await _policyHandler.HandleAsync(authorizationHandlerContext);

		// Assert
		authorizationHandlerContext.HasSucceeded.Should().BeFalse();
	}

	protected async Task HandleRequirementAsync_Succeeds_WhenCacheContainRequiredUserDataWithOrganisation(
		string serviceRoleKey, string roleInOrganisation, string enrolmentStatus, Guid organisationId)
	{
		// Arrange
		var userData = _fixture.Build<UserData>()
			.With(x => x.Organisations,
			[
				new Organisation
				{
					Id = organisationId,
					Name = "Test Organisation",
					PersonRoleInOrganisation = roleInOrganisation,
					Enrolments =
					[
						new Enrolment
						{
							ServiceRoleKey = serviceRoleKey,
							EnrolmentStatus = enrolmentStatus
						}
					]
				}
			])
			.Create();

		var objectId = "12345678-1234-1234-1234-123456789012";
		var claims = new[]
		{
			new Claim(ClaimConstants.ObjectId, objectId)
            // No UserData claim, it will be fetched from session/cache
        };

		var claimsIdentity = new ClaimsIdentity(claims, "CustomAuthenticationType");
		var user = new ClaimsPrincipal(claimsIdentity);

		var mySession = new MySession { UserData = userData };
		_sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(mySession);

		_httpContextMock.SetupGet(x => x.User).Returns(user);
		var routeValueDictionary = new RouteValueDictionary
		{
			{ "organisationId", organisationId.ToString() }
		};
		var featureCollection = BuildFeatureCollection(user);
		_httpContextMock.Setup(x => x.Features).Returns(featureCollection);
		_httpContextMock.Setup(x => x.Request.RouteValues).Returns(routeValueDictionary);
		_httpContextAccessorMock.SetupGet(x => x.HttpContext).Returns(_httpContextMock.Object);

		var authorizationHandlerContext = new AuthorizationHandlerContext(
			new List<IAuthorizationRequirement> { new TPolicyRequirement() },
			user,
			_httpContextMock.Object);

		// Act
		await _policyHandler.HandleAsync(authorizationHandlerContext);

		// Assert
		authorizationHandlerContext.HasSucceeded.Should().BeTrue();
	}

	protected async Task HandleRequirementAsync_Fails_WhenUserDataExistsInCacheButUserRoleIsNotAuthorisedWithOrganisation(
		string serviceRoleKey, string roleInOrganisation, string enrolmentStatus, Guid organisationId)
	{
		// Arrange
		var userData = _fixture.Build<UserData>()
			.With(x => x.Organisations,
			[
				new Organisation
				{
					Id = organisationId,
					Name = "Test Organisation",
					PersonRoleInOrganisation = roleInOrganisation,
					Enrolments =
					[
						new Enrolment
						{
							ServiceRoleKey = serviceRoleKey,
							EnrolmentStatus = enrolmentStatus
						}
					]
				}
			])
			.Create();

		var objectId = "12345678-1234-1234-1234-123456789012";
		var claims = new[]
		{
			new Claim(ClaimConstants.ObjectId, objectId)
		};

		var claimsIdentity = new ClaimsIdentity(claims, "CustomAuthenticationType");
		var user = new ClaimsPrincipal(claimsIdentity);

		var mySession = new MySession { UserData = userData };
		_sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(mySession);

		_httpContextMock.SetupGet(x => x.User).Returns(user);
		var routeValueDictionary = new RouteValueDictionary
		{
			{ "organisationId", organisationId.ToString() }
		};
		_httpContextMock.Setup(x => x.Request.RouteValues).Returns(routeValueDictionary);
		_httpContextAccessorMock.SetupGet(x => x.HttpContext).Returns(_httpContextMock.Object);

		var authorizationHandlerContext = new AuthorizationHandlerContext(
			new List<IAuthorizationRequirement> { new TPolicyRequirement() },
			user,
			_httpContextMock.Object);

		// Act
		await _policyHandler.HandleAsync(authorizationHandlerContext);

		// Assert
		authorizationHandlerContext.HasSucceeded.Should().BeFalse();
	}

	protected async Task HandleRequirementAsync_Succeeds_WhenUserDataIsRetrievedFromApiWithOrganisation(
		string serviceRoleKey, string roleInOrganisation, string enrolmentStatus, Guid organisationId)
	{
		// Arrange
		var objectId = "12345678-1234-1234-1234-123456789012";
		var claims = new[]
		{
			new Claim(ClaimConstants.ObjectId, objectId)
		};

		var claimsIdentity = new ClaimsIdentity(claims, "CustomAuthenticationType");
		var user = new ClaimsPrincipal(claimsIdentity);

		_sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync((MySession)null);

		var userData = new UserData
		{
			Organisations =
			[
				new Organisation
				{
					Id = organisationId,
					Name = "Test Organisation",
					PersonRoleInOrganisation = roleInOrganisation,
					Enrolments =
					[
						new Enrolment
						{
							ServiceRoleKey = serviceRoleKey,
							EnrolmentStatus = enrolmentStatus
						}
					]
				}
			]
		};

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

		_httpContextMock.SetupGet(x => x.User).Returns(user);
		var routeValueDictionary = new RouteValueDictionary
		{
			{ "organisationId", organisationId.ToString() }
		};
		var featureCollection = BuildFeatureCollection(user);
		_httpContextMock.Setup(x => x.Features).Returns(featureCollection);
		_httpContextMock.Setup(x => x.Request.RouteValues).Returns(routeValueDictionary);
		_httpContextAccessorMock.SetupGet(x => x.HttpContext).Returns(_httpContextMock.Object);

		var authorizationHandlerContext = new AuthorizationHandlerContext(
			new List<IAuthorizationRequirement> { new TPolicyRequirement() },
			user,
			_httpContextMock.Object);

		// Act
		await _policyHandler.HandleAsync(authorizationHandlerContext);

		// Assert
		authorizationHandlerContext.HasSucceeded.Should().BeTrue();
	}

	protected async Task HandleRequirementAsync_Fails_WhenUserDataIsRetrievedFromApiButUserRoleIsNotAuthorisedWithOrganisation(
		string serviceRoleKey, string roleInOrganisation, string enrolmentStatus, Guid organisationId)
	{
		// Arrange
		var objectId = "12345678-1234-1234-1234-123456789012";
		var claims = new[]
		{
			new Claim(ClaimConstants.ObjectId, objectId)
		};

		var claimsIdentity = new ClaimsIdentity(claims, "CustomAuthenticationType");
		var user = new ClaimsPrincipal(claimsIdentity);

		_sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync((MySession)null);

		var userData = new UserData
		{
			Organisations =
			[
				new Organisation
				{
					Id = organisationId,
					Name = "Test Organisation",
					PersonRoleInOrganisation = roleInOrganisation,
					Enrolments =
					[
						new Enrolment
						{
							ServiceRoleKey = serviceRoleKey,
							EnrolmentStatus = enrolmentStatus
						}
					]
				}
			]
		};

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

		_httpContextMock.SetupGet(x => x.User).Returns(user);
		var routeValueDictionary = new RouteValueDictionary
		{
			{ "organisationId", organisationId.ToString() }
		};
		_httpContextMock.Setup(x => x.Request.RouteValues).Returns(routeValueDictionary);
		_httpContextAccessorMock.SetupGet(x => x.HttpContext).Returns(_httpContextMock.Object);

		var authorizationHandlerContext = new AuthorizationHandlerContext(
			new List<IAuthorizationRequirement> { new TPolicyRequirement() },
			user,
			_httpContextMock.Object);

		// Act
		await _policyHandler.HandleAsync(authorizationHandlerContext);

		// Assert
		authorizationHandlerContext.HasSucceeded.Should().BeFalse();
	}

	protected async Task HandleRequirementAsync_Fails_WhenNoOrganisationIdInSessionOrRoute()
	{
		// Arrange
		var userData = _fixture.Build<UserData>()
			.With(x => x.ServiceRole, ServiceRoleKeys.ReExAdminUser)
			.With(x => x.RoleInOrganisation, "SomeRole")
			.With(x => x.EnrolmentStatus, "Active")
			.With(x => x.Organisations,
			[
				new Organisation
				{
					Id = _testOrganisationId,
					Name = "Test Organisation",
					Enrolments =
					[
						new Enrolment
						{
							ServiceRoleKey = ServiceRoleKeys.ReExAdminUser,
							EnrolmentStatus = "Active"
						}
					]
				}
			])
			.Create();

		var objectId = "12345678-1234-1234-1234-123456789012";
		var claims = new[]
		{
			new Claim(ClaimConstants.ObjectId, objectId),
			new Claim(ClaimTypes.UserData, JsonSerializer.Serialize(userData))
		};

		var claimsIdentity = new ClaimsIdentity(claims, "CustomAuthenticationType");
		var user = new ClaimsPrincipal(claimsIdentity);

		var mySession = new MySession { UserData = userData, SelectedOrganisationId = null };
		_sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(mySession);

		var routeValueDictionary = new RouteValueDictionary();
		_httpContextMock.Setup(x => x.Request.RouteValues).Returns(routeValueDictionary);
		_httpContextMock.SetupGet(x => x.User).Returns(user);
		_httpContextMock.SetupGet(x => x.Response).Returns(HttpResponseMock.Object);

		_httpContextAccessorMock.SetupGet(x => x.HttpContext).Returns(_httpContextMock.Object);

		var authorizationHandlerContext = new AuthorizationHandlerContext(
			new List<IAuthorizationRequirement> { new TPolicyRequirement() },
			user,
			_httpContextMock.Object);

		// Act
		await _policyHandler.HandleAsync(authorizationHandlerContext);

		// Assert
		authorizationHandlerContext.HasSucceeded.Should().BeFalse();
		HttpResponseMock.Verify(x => x.Redirect(It.IsAny<string>()), Times.Once);
	}

	protected async Task HandleRequirementAsync_Succeeds_WhenOrganisationIdFromRoute()
	{
		// Arrange
		var objectId = "12345678-1234-1234-1234-123456789012";
		var claims = new[]
		{
			new Claim(ClaimConstants.ObjectId, objectId)
		};

		var claimsIdentity = new ClaimsIdentity(claims, "CustomAuthenticationType");
		var user = new ClaimsPrincipal(claimsIdentity);

		var routeValueDictionary = new RouteValueDictionary
		{
			{ "organisationId", _testOrganisationId.ToString() }
		};
		var featureCollection = BuildFeatureCollection(user);
		_httpContextMock.Setup(x => x.Features).Returns(featureCollection);
		_httpContextMock.Setup(x => x.Request.RouteValues).Returns(routeValueDictionary);
		_httpContextMock.SetupGet(x => x.User).Returns(user);

		_httpContextAccessorMock.SetupGet(x => x.HttpContext).Returns(_httpContextMock.Object);

		var mySession = new MySession { UserData = new(), SelectedOrganisationId = null };
		_sessionManagerMock.Setup(x => x.GetSessionAsync(It.IsAny<ISession>())).ReturnsAsync(mySession);

		TSession capturedSession = null!;
		_sessionManagerMock.Setup(x => x.SaveSessionAsync(It.IsAny<ISession>(), It.IsAny<MySession>()))
			.Returns(Task.CompletedTask)
			.Callback<ISession, MySession>((s, sess) => { capturedSession = (TSession)(object)sess; });

		var userDataFromApi = new UserData
		{
			Organisations =
			[
				new Organisation
				{
					Id = _testOrganisationId,
					Name = "Test Organisation",
					PersonRoleInOrganisation = RoleInOrganisation.Admin,
					Enrolments =
					[
						new Enrolment
						{
							ServiceRoleKey = ServiceRoleKeys.ReExAdminUser,
							EnrolmentStatus = EnrolmentStatuses.Enrolled
						}
					]
				}
			]
		};
		_httpMessageHandlerMock
			.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.OK,
				Content = new StringContent(JsonSerializer.Serialize(new UserOrganisations { User = userDataFromApi }))
			})
			.Verifiable();

		var authorizationHandlerContext = new AuthorizationHandlerContext(
			new List<IAuthorizationRequirement> { new TPolicyRequirement() },
			user,
			_httpContextMock.Object);

		// Act
		await _policyHandler.HandleAsync(authorizationHandlerContext);

		// Assert
		authorizationHandlerContext.HasSucceeded.Should().BeTrue();
		capturedSession.Should().NotBeNull();
		capturedSession.SelectedOrganisationId.Should().Be(_testOrganisationId);

		_httpMessageHandlerMock.Protected().Verify(
			"SendAsync",
			Times.Once(),
			ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
			ItExpr.IsAny<CancellationToken>());
	}
}