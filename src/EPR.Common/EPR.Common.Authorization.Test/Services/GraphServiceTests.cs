using EPR.Common.Authorization.Config;
using EPR.Common.Authorization.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Abstractions.Store;
using Microsoft.Kiota.Serialization.Json;
using Moq;

namespace EPR.Common.Authorization.Test.Services;

public class GraphServiceTests
{
    private const string ExtensionClientId = "a12-b34";

    private Mock<IOptions<AzureB2CExtensionConfig>> _azureB2CExtensionOptions;
    private Mock<GraphServiceClient> _graphServiceClientMock;
    private Mock<ILogger<GraphService>> _loggerMock;
    private Mock<IRequestAdapter> _requestAdapterMock;
    private Mock<ISerializationWriterFactory> _serializationWriterFactoryMock;
    private GraphService _systemUnderTest;

    [TestInitialize]
    public void SetUp()
    {
        _requestAdapterMock = new Mock<IRequestAdapter>();

        _serializationWriterFactoryMock = new Mock<ISerializationWriterFactory>();
        _serializationWriterFactoryMock.Setup(factory => factory.GetSerializationWriter(It.IsAny<string>()))
            .Returns((string _) => new JsonSerializationWriter());

        _requestAdapterMock.SetupGet(adapter => adapter.BaseUrl).Returns("http://graph.test.internal/mock");
        _requestAdapterMock.SetupSet(adapter => adapter.BaseUrl = It.IsAny<string>());
        _requestAdapterMock.Setup(adapter => adapter.EnableBackingStore(It.IsAny<IBackingStoreFactory>()));
        _requestAdapterMock.SetupGet(adapter => adapter.SerializationWriterFactory).Returns(_serializationWriterFactoryMock.Object);

        _graphServiceClientMock = new Mock<GraphServiceClient>(
            _requestAdapterMock.Object,
            It.IsAny<string>());

        _azureB2CExtensionOptions = new Mock<IOptions<AzureB2CExtensionConfig>>();
        _azureB2CExtensionOptions!
            .Setup(x => x.Value)
            .Returns(new AzureB2CExtensionConfig
            {
                ExtensionsClientId = ExtensionClientId,
            });

        _loggerMock = new Mock<ILogger<GraphService>>();

        _systemUnderTest = new GraphService(
            _graphServiceClientMock.Object,
            _azureB2CExtensionOptions.Object,
            _loggerMock.Object);
    }

    [TestMethod]
    public async Task PatchUserProperty_DoesNotCallGraphServiceClient_WhenExtensionsClientIdIsMissing()
    {
        _azureB2CExtensionOptions
            .Setup(x => x.Value)
            .Returns(new AzureB2CExtensionConfig
            {
                ExtensionsClientId = string.Empty
            });

        await _systemUnderTest.PatchUserProperty(Guid.NewGuid(), "customProperty", "value");

        _requestAdapterMock
            .Verify(ra => ra.SendAsync(
                It.IsAny<RequestInformation>(),
                It.IsAny<ParsableFactory<User>>(),
                It.IsAny<Dictionary<string, ParsableFactory<IParsable>>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task PatchUserProperty_CallsGraphServiceClientWithCorrectParameters()
    {
        var userId = Guid.NewGuid();
        var propertyName = "custom";
        var value = "newValue";

        await _systemUnderTest.PatchUserProperty(userId, propertyName, value);

        _requestAdapterMock
            .Verify(ra => ra.SendAsync(
                It.IsAny<RequestInformation>(),
                It.IsAny<ParsableFactory<User>>(),
                It.IsAny<Dictionary<string, ParsableFactory<IParsable>>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task PatchUserProperty_LogsError_WhenSendAsyncThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var propertyName = "custom";
        var value = "newValue";
        var exception = new Exception("SendAsync failed");

        _requestAdapterMock
            .Setup(ra => ra.SendAsync(
                It.IsAny<RequestInformation>(),
                It.IsAny<ParsableFactory<User>>(),
                It.IsAny<Dictionary<string, ParsableFactory<IParsable>>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var act = async () => await _systemUnderTest.PatchUserProperty(userId, propertyName, value);

        // Assert
        await act.Should().NotThrowAsync();

        _loggerMock.VerifyLog(x => x.LogError("Error while trying to patch {PropertyName} for user {UserId} with Graph API", propertyName, userId));
    }
}
