namespace EPR.Common.Authorization.Test.Services;

using EPR.Common.Authorization.Services;

[TestClass]
public class CryptoServicesTests
{
    private string _encryptionKey;

    [TestInitialize]
    public void Setup()
    {
        var encryptionKeyGenerator = new EncryptionKeyGenerator();
        _encryptionKey = encryptionKeyGenerator.GenerateBase64EncodedKey();
         
    }

    [TestMethod]
    public void EncryptText_ValidInput_ReturnsEncryptedText()
    {
        // Arrange
        var cryptoService = new CryptoServices();
        var textToEncrypt = "Hello, world!";

        // Act
        var encryptedText = cryptoService.EncryptText(textToEncrypt, _encryptionKey);

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(encryptedText));
        Assert.AreNotEqual(textToEncrypt, encryptedText);
    }

    [TestMethod]
    public void EncryptText_ComprehensiveTests()
    {
        // Arrange
        var cryptoService = new CryptoServices();
        var textToEncrypt = "Hello, world!";

        // Act
        var encryptedText = cryptoService.EncryptText(textToEncrypt, _encryptionKey);

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(encryptedText), "Encrypted text should not be null or empty.");
        Assert.AreNotEqual(textToEncrypt, encryptedText, "Encrypted text should not be the same as the original text.");

        // Try decrypting the encrypted text to ensure it's reversible
        var decryptedText = cryptoService.DecryptText(encryptedText, _encryptionKey);
        Assert.AreEqual(textToEncrypt, decryptedText, "Decrypted text should match the original text.");

        // Test with empty string as input
        var encryptedEmpty = cryptoService.EncryptText("", _encryptionKey);
        Assert.IsNotNull(encryptedEmpty, "Encrypted text for empty input should not be null.");
        Assert.AreNotEqual("", encryptedEmpty, "Encrypted text for empty input should not be empty.");

        // Test with long text
        var longText = new string('A', 1000); // Creating a long string
        var encryptedLongText = cryptoService.EncryptText(longText, _encryptionKey);
        Assert.IsNotNull(encryptedLongText, "Encrypted text for long input should not be null.");
        Assert.AreNotEqual(longText, encryptedLongText, "Encrypted text for long input should not be the same as the original text.");

        // Test with null input
        Assert.ThrowsException<ArgumentNullException>(() => cryptoService.EncryptText(null, _encryptionKey), "Encrypting null input should throw ArgumentNullException.");

        // Test with null encryption key
        Assert.ThrowsException<ArgumentNullException>(() => cryptoService.EncryptText("SomeText", null), "Encrypting with null encryption key should throw ArgumentNullException.");

        // Test with invalid encryption key format
        var invalidKey = "InvalidKey";
        Assert.ThrowsException<FormatException>(() => cryptoService.EncryptText("SomeText", invalidKey), "Encrypting with invalid encryption key format should throw FormatException.");
    }


    [TestMethod]
    public void DecryptText_ValidEncryptedText_ReturnsOriginalText()
    {
        // Arrange
        var cryptoService = new CryptoServices();
        var originalText = "Hello, world!";
        var encryptedText = cryptoService.EncryptText(originalText, _encryptionKey);

        // Act
        var decryptedText = cryptoService.DecryptText(encryptedText, _encryptionKey);

        // Assert
        Assert.AreEqual(originalText, decryptedText);
    }

    [TestMethod]
    [ExpectedException(typeof(FormatException))]
    public void DecryptText_InvalidKey_ThrowsArgumentException()
    {
        // Arrange
        var cryptoService = new CryptoServices();
        var encryptedText = "EncryptedText";
        var invalidKey = "InvalidKey";

        // Act
       cryptoService.DecryptText(encryptedText, invalidKey);

       //Expected exception is formatException
    }
}