namespace EPR.Common.Authorization.Test.Services;

using AutoFixture;
using AutoFixture.AutoMoq;
using System.Security.Cryptography;

public class EncryptionKeyGenerator
{
    private readonly IFixture _fixture;

    public EncryptionKeyGenerator()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());
        CustomizeStringGeneration();
    }

    private void CustomizeStringGeneration()
    {
        _fixture.Customizations.Add(new StringGenerator(() =>
        {
            var randomBytes = GenerateRandomBytes(32);
            return Convert.ToBase64String(randomBytes);
        }));
    }

    public string GenerateBase64EncodedKey()
    {
        return _fixture.Create<string>();
    }

    private byte[] GenerateRandomBytes(int length)
    {
        var randomBytes = new byte[length];
        using var rng = new RNGCryptoServiceProvider();
        rng.GetBytes(randomBytes);
        return randomBytes;
    }
}