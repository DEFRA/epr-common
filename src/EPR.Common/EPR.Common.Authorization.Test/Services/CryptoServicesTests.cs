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
    public void EncryptText_When_TextIsNull_ThenItThrowsException()
    {
        // Arrange
        var cryptoService = new CryptoServices();

        // Act
        // Assert
        Assert.ThrowsException<ArgumentNullException>(() => cryptoService.EncryptText(null, _encryptionKey), "Encrypting null input should throw ArgumentNullException.");
    }

    [TestMethod]
    public void EncryptText_When_EncryptionKeyIsNull_ThenItThrowsException()
    {
        // Arrange
        var cryptoService = new CryptoServices();

        // Act
        // Assert
        Assert.ThrowsException<ArgumentNullException>(() => cryptoService.EncryptText("SomeText", null), "Encrypting with null encryption key should throw ArgumentNullException.");
    }

    [TestMethod]
    public void EncryptText_When_EncryptionKeyIsNotValidFormt_ThenItThrowsException()
    {
        // Arrange
        var cryptoService = new CryptoServices();
        var textToEncrypt = "Hello, world!";

        // Act
        // Assert
        var invalidKey = "InvalidKey";
        Assert.ThrowsException<FormatException>(() => cryptoService.EncryptText(textToEncrypt, invalidKey), "Encrypting with invalid encryption key format should throw FormatException.");
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
    public void EncryptText_When_InputsNotMatching_Then_decryptedValuesAreNotEqual()
    {
        // Arrange
        var cryptoService = new CryptoServices();
        var textToEncrypt = "Hello, world!";

        // Act
        var encryptedText = cryptoService.EncryptText(textToEncrypt, _encryptionKey);

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(encryptedText), "Encrypted text should not be null or empty.");
        Assert.AreNotEqual(textToEncrypt, encryptedText, "Encrypted text should not be the same as the original text.");
    }


    [TestMethod]
    public void EncryptText_When_DecryptTheSameInput_ThenItIsEqualOriginalValue()
    {
        // Arrange
        var cryptoService = new CryptoServices();
        var textToEncrypt = "Hello, world!";

        // Act
        var encryptedText = cryptoService.EncryptText(textToEncrypt, _encryptionKey);

        // Assert
        var decryptedText = cryptoService.DecryptText(encryptedText, _encryptionKey);
        Assert.AreEqual(textToEncrypt, decryptedText, "Decrypted text should match the original text.");
    }

    [TestMethod]
    public void EncryptEmptyText_When_EncryptTheEmptyString_ThenTheEncryptedValueIsNotEmpty()
    {
        // Arrange
        var cryptoService = new CryptoServices();

        // Act
        var encryptedEmpty = cryptoService.EncryptText("", _encryptionKey);

        // Assert
        Assert.IsNotNull(encryptedEmpty, "Encrypted text for empty input should not be null.");
        Assert.AreNotEqual("", encryptedEmpty, "Encrypted text for empty input should not be empty.");
    }

    [TestMethod]
    public void EncryptLongText_When_Encrypt_ThenItGetsEncryptedCorrectly()
    {
        // Arrange
        var cryptoService = new CryptoServices();

        // Act
        var longText = new string('A', 1000); // Creating a long string
        var encryptedLongText = cryptoService.EncryptText(longText, _encryptionKey);
        var decryptedLongText = cryptoService.DecryptText(encryptedLongText, _encryptionKey);

        // Assert
        Assert.IsNotNull(encryptedLongText, "Encrypted text for long input should not be null.");
        Assert.AreNotEqual(longText, encryptedLongText, "Encrypted text for long input should not be the same as the original text.");
        Assert.AreEqual(decryptedLongText, longText);
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
}