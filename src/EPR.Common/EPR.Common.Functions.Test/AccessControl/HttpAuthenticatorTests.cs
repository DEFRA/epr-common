namespace EPR.Common.Functions.Test.AccessControl;

using FluentAssertions;
using Functions.AccessControl;
using Functions.CancellationTokens.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

[TestClass]
public class HttpAuthenticatorTests
{
    private HttpAuthenticator httpAuthenticator;
    private ILogger<HttpAuthenticator> logger;
    private ICancellationTokenAccessor cancellationTokenAccessor;

    [TestInitialize]
    public void Setup()
    {
        this.logger = Substitute.For<ILogger<HttpAuthenticator>>();
        this.cancellationTokenAccessor = Substitute.For<ICancellationTokenAccessor>();
        this.httpAuthenticator = new HttpAuthenticator(this.logger, this.cancellationTokenAccessor);
    }

    [TestMethod]
    public async Task AuthenticateAsync_ReturnsFalse_WhenBearerTokenIsNull()
    {
        // Arrange
        string bearerToken = null;

        // Act
        var result = await this.httpAuthenticator.AuthenticateAsync(bearerToken);

        // Assert
        result.Should().BeFalse();
    }
}