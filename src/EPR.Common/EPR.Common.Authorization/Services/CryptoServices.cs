namespace EPR.Common.Authorization.Services;

using Interfaces;
using System.Security.Cryptography;
using System.Text;

public class CryptoServices : ICryptoServices
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public string EncryptText(string textToEncrypt, string encryptionKey)
    {
        var plainTextBytes = Encoding.UTF8.GetBytes(textToEncrypt);

        using var aesGcm = new AesGcm(Convert.FromBase64String(encryptionKey));
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        var encryptedData = new byte[plainTextBytes.Length];
        var tag = new byte[TagSize];

        aesGcm.Encrypt(nonce, plainTextBytes, encryptedData, tag);

        var encryptedResult = new byte[nonce.Length + encryptedData.Length + tag.Length];
        Buffer.BlockCopy(nonce, 0, encryptedResult, 0, nonce.Length);
        Buffer.BlockCopy(encryptedData, 0, encryptedResult, nonce.Length, encryptedData.Length);
        Buffer.BlockCopy(tag, 0, encryptedResult, nonce.Length + encryptedData.Length, tag.Length);

        return Convert.ToBase64String(encryptedResult);
    }

    public string DecryptText(string encryptedText, string encryptionKey)
    {
        var encryptedData = Convert.FromBase64String(encryptedText);
        using var aesGcm = new AesGcm(Convert.FromBase64String(encryptionKey));

        var nonce = new byte[NonceSize];
        var cipherText = new byte[encryptedData.Length - nonce.Length - TagSize];
        var tag = new byte[TagSize];

        Buffer.BlockCopy(encryptedData, 0, nonce, 0, nonce.Length);
        Buffer.BlockCopy(encryptedData, nonce.Length, cipherText, 0, cipherText.Length);
        Buffer.BlockCopy(encryptedData, nonce.Length + cipherText.Length, tag, 0, tag.Length);

        var decryptedData = new byte[cipherText.Length];
        aesGcm.Decrypt(nonce, cipherText, tag, decryptedData);

        return Encoding.UTF8.GetString(decryptedData);
    }
}