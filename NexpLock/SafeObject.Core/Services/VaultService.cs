using System.Buffers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using SafeObject.Core.Interfaces;
using SafeObject.Core.Services.Factory;
using static SafeObject.Core.Helpers.Constants;

namespace SafeObject.Core.Services;

public sealed class VaultService : IVaultService, IDisposable
{
    private static readonly ArrayPool<byte> SecurePool = ArrayPool<byte>.Create();
    private readonly string _keyFilePath;

    private readonly Task<byte[]> _systemSecurityKey;

    private volatile bool _disposed;

    public VaultService()
    {
        var lockedBoxDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Security.SystemVault.StoragePath);

        Directory.CreateDirectory(lockedBoxDirectory);

        _keyFilePath = Path.Combine(lockedBoxDirectory, Security.SystemVault.SpBin);
        _systemSecurityKey = Task.Run(LoadOrGenerateSystemSecurityKey);
    }

    public async Task<byte[]> EncryptKeyAsync(byte[] contentKey, string filePublicMasterKey)
    {
        ThrowIfDisposed();

        var masterKeyBytes = Convert.FromBase64String(filePublicMasterKey);

        var encryptedContentKey = await EncryptAsync(contentKey, masterKeyBytes);
        var finalEncryptedKey = await EncryptAsync(encryptedContentKey, await _systemSecurityKey);

        return finalEncryptedKey;
    }

    public async Task<byte[]> DecryptKeyAsync(byte[] finalEncryptedKey, string filePublicMasterKey)
    {
        ThrowIfDisposed();

        var masterKeyBytes = Convert.FromBase64String(filePublicMasterKey);

        var decryptedLayerOne = await DecryptAsync(finalEncryptedKey, await _systemSecurityKey);
        var decryptedLayerTwo = await DecryptAsync(decryptedLayerOne, masterKeyBytes);

        return decryptedLayerTwo;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
            if (_systemSecurityKey.IsCompletedSuccessfully)
            {
                var key = _systemSecurityKey.Result;
                Array.Clear(key, 0, key.Length);
            }

        _disposed = true;
    }

    ~VaultService()
    {
        Dispose(false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
        if (_disposed is not true) return;
        throw new ObjectDisposedException(nameof(VaultService));
    }

    private async Task<byte[]> LoadOrGenerateSystemSecurityKey()
    {
        if (File.Exists(_keyFilePath))
        {
            var keyBytes = await File.ReadAllBytesAsync(_keyFilePath);
            if (keyBytes.Length != Security.SystemVault.SystemSecurityKeySize / 8)
                throw new InvalidOperationException("Stored system security key has an invalid length.");
            return keyBytes;
        }

        var newKey = GenerateSystemSecurityKey(Security.SystemVault.SystemSecurityKeySize);

        await using var stream = DirectStreamFactory.Create(
            _keyFilePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            Storage.BufferSize,
            FileOptions.Asynchronous | FileOptions.WriteThrough,
            null);

        await stream.WriteAsync(newKey);
        await stream.FlushAsync();

        return newKey;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte[] GenerateSystemSecurityKey(int keySize)
    {
        if (keySize is not (128 or 192 or 256))
            throw new ArgumentOutOfRangeException(nameof(keySize), "Key size must be 128, 192, or 256 bits.");

        var initialKey = SecurePool.Rent(keySize / 8);
        var salt = SecurePool.Rent(32);
        var finalKey = SecurePool.Rent(keySize / 8);

        try
        {
            RandomNumberGenerator.Fill(initialKey.AsSpan(0, keySize / 8));
            RandomNumberGenerator.Fill(salt.AsSpan(0, 32));

            var hashAlgorithm = HashAlgorithmProviderFactory.Instance.GetHashAlgorithmName();

            using var keyDerivation = new Rfc2898DeriveBytes(
                initialKey.AsSpan(0, keySize / 8).ToArray(),
                salt.AsSpan(0, 32).ToArray(),
                Security.SystemVault.Iterations,
                hashAlgorithm);

            keyDerivation.GetBytes(keySize / 8).CopyTo(finalKey.AsSpan(0, keySize / 8));

            var result = new byte[keySize / 8];
            finalKey.AsSpan(0, keySize / 8).CopyTo(result.AsSpan());

            return result;
        }
        finally
        {
            SecurePool.Return(initialKey, true);
            SecurePool.Return(salt, true);
            SecurePool.Return(finalKey, true);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Task<byte[]> EncryptAsync(byte[] data, byte[] key)
    {
        var nonce = SecurePool.Rent(Security.KeyVault.NonceSize);
        var ciphertext = SecurePool.Rent(data.Length);
        var tag = SecurePool.Rent(Security.KeyVault.TagSize);
        var result = SecurePool.Rent(Security.KeyVault.NonceSize + data.Length + Security.KeyVault.TagSize);

        try
        {
            RandomNumberGenerator.Fill(nonce.AsSpan(0, Security.KeyVault.NonceSize));

            using var aesGcm = new AesGcm(key, Security.KeyVault.TagSize);
            aesGcm.Encrypt(
                nonce.AsSpan(0, Security.KeyVault.NonceSize),
                data.AsSpan(),
                ciphertext.AsSpan(0, data.Length),
                tag.AsSpan(0, Security.KeyVault.TagSize));

            Buffer.BlockCopy(nonce, 0, result, 0, Security.KeyVault.NonceSize);
            Buffer.BlockCopy(ciphertext, 0, result, Security.KeyVault.NonceSize, data.Length);
            Buffer.BlockCopy(tag, 0, result, Security.KeyVault.NonceSize + data.Length, Security.KeyVault.TagSize);

            var output = new byte[Security.KeyVault.NonceSize + data.Length + Security.KeyVault.TagSize];
            result.AsSpan(0, output.Length).CopyTo(output.AsSpan());

            return Task.FromResult(output);
        }
        finally
        {
            SecurePool.Return(nonce, true);
            SecurePool.Return(ciphertext, true);
            SecurePool.Return(tag, true);
            SecurePool.Return(result, true);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Task<byte[]> DecryptAsync(byte[] encryptedData, byte[] key)
    {
        if (encryptedData.Length < Security.KeyVault.NonceSize + Security.KeyVault.TagSize)
            throw new ArgumentException("Encrypted data is invalid or corrupted", nameof(encryptedData));

        var expectedLength = encryptedData.Length - Security.KeyVault.NonceSize - Security.KeyVault.TagSize;
        if (expectedLength <= 0)
            throw new ArgumentException("Encrypted data does not contain valid ciphertext", nameof(encryptedData));

        var nonce = SecurePool.Rent(Security.KeyVault.NonceSize);
        var ciphertext = SecurePool.Rent(expectedLength);
        var tag = SecurePool.Rent(Security.KeyVault.TagSize);
        var plaintext = SecurePool.Rent(expectedLength);

        try
        {
            Buffer.BlockCopy(encryptedData, 0, nonce, 0, Security.KeyVault.NonceSize);
            Buffer.BlockCopy(encryptedData, Security.KeyVault.NonceSize, ciphertext, 0, expectedLength);
            Buffer.BlockCopy(encryptedData, Security.KeyVault.NonceSize + expectedLength, tag, 0,
                Security.KeyVault.TagSize);

            using var aesGcm = new AesGcm(key, Security.KeyVault.TagSize);
            aesGcm.Decrypt(
                nonce.AsSpan(0, Security.KeyVault.NonceSize),
                ciphertext.AsSpan(0, expectedLength),
                tag.AsSpan(0, Security.KeyVault.TagSize),
                plaintext.AsSpan(0, expectedLength));

            var output = new byte[expectedLength];
            plaintext.AsSpan(0, output.Length).CopyTo(output.AsSpan());

            return Task.FromResult(output);
        }
        finally
        {
            SecurePool.Return(nonce, true);
            SecurePool.Return(ciphertext, true);
            SecurePool.Return(tag, true);
            SecurePool.Return(plaintext, true);
        }
    }
}