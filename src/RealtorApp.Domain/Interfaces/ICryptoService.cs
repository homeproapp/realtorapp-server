namespace RealtorApp.Domain.Interfaces;

public interface ICryptoService
{
    string Encrypt(string plainText);
    string Decrypt(string encryptedText);
}