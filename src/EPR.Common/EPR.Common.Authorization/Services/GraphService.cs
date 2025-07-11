using EPR.Common.Authorization.Config;
using EPR.Common.Authorization.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace EPR.Common.Authorization.Services;

public class GraphService(
    GraphServiceClient graphServiceClient,
    IOptions<AzureB2CExtensionConfig> azureB2CExtensionOptions,
    ILogger<GraphService> logger) : IGraphService
{
    private readonly GraphServiceClient _graphServiceClient = graphServiceClient;
    private readonly ILogger<GraphService> _logger = logger;

    private readonly string _extensionsClientId = azureB2CExtensionOptions.Value
        .ExtensionsClientId
        .Replace("-", string.Empty);

    public async Task PatchUserProperty(Guid userId, string propertyName, string value, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_extensionsClientId))
        {
            return;
        }

        try
        {
            var requestBody = new User
            {
                AdditionalData = new Dictionary<string, object>
                {
                    { $"extension_{_extensionsClientId}_{propertyName}", value }
                }
            };

            await _graphServiceClient.Users[$"{userId}"].PatchAsync(requestBody, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while trying to patch {PropertyName} for user {UserId} with Graph API", propertyName, userId);
        }
    }
}
