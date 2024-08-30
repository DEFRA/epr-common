namespace EPR.Common.Authorization.Interfaces;

public interface ICryptoServices
{
    string EncryptText(string textToEncrypt, string encryptionKey);
    string DecryptText(string encryptedText, string encryptionKey);
}