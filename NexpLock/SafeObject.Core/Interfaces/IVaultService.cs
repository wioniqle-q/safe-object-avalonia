namespace SafeObject.Core.Interfaces;

public interface IVaultService
{
    Task<byte[]> EncryptKeyAsync(byte[] contentKey, string filePublicMasterKey);
    Task<byte[]> DecryptKeyAsync(byte[] finalEncryptedKey, string filePublicMasterKey);

    void Dispose();
}