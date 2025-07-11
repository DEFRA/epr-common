namespace EPR.Common.Authorization.Services.Interfaces;

public interface IGraphService
{
    Task PatchUserProperty(Guid userId, string propertyName, string value,
        CancellationToken cancellationToken = default);
}
