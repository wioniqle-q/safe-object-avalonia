using System.Buffers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using SafeObject.Core.Helpers;
using SafeObject.Core.Interfaces;
using SafeObject.Core.Models;
using SafeObject.Core.Services.Factory;

namespace SafeObject.Core.Services;

public sealed class StorageService(ILogger<StorageService>? logger, IVaultService? keyVaultService)
    : IStorageService, IDisposable
{
    private static readonly ArrayPool<byte> SecurePool = ArrayPool<byte>.Create();

    private readonly IVaultService _keyVaultService =
        keyVaultService ?? throw new ArgumentNullException(nameof(keyVaultService));

    private readonly ILogger<StorageService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private volatile bool _disposed;

    public async Task EncryptFileAsync(FileProcessingRequest request, string filePublicMasterKey,
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        var contentKey = GenerateRandomKey();
        var finalEncryptedKey = await _keyVaultService.EncryptKeyAsync(contentKey, filePublicMasterKey);
        var nonce = SecurePool.Rent(Constants.Security.KeyVault.NonceSize);

        try
        {
            RandomNumberGenerator.Fill(nonce.AsSpan(0, Constants.Security.KeyVault.NonceSize));

            await using var sourceStream =
                CreateFileStream(request.SourcePath, FileMode.Open, FileAccess.Read, _logger);
            await using var destinationStream =
                CreateFileStream(request.DestinationPath, FileMode.Create, FileAccess.Write, _logger);

            await destinationStream.WriteAsync(
                finalEncryptedKey.AsMemory(0, Constants.Security.KeyVault.FinalEncryptedKeySize), cancellationToken);
            await destinationStream.WriteAsync(nonce.AsMemory(0, Constants.Security.KeyVault.NonceSize),
                cancellationToken);
            await destinationStream.FlushAsync(cancellationToken);

            await ProcessEncryptionStreamAsync(sourceStream, destinationStream, contentKey, nonce, cancellationToken);
        }
        finally
        {
            SecurePool.Return(nonce, true);
            CryptographicOperations.ZeroMemory(contentKey);
        }
    }

    public async Task DecryptFileAsync(FileProcessingRequest request, string filePublicMasterKey,
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        await using var sourceStream = CreateFileStream(request.SourcePath, FileMode.Open, FileAccess.Read, _logger);
        await using var destinationStream =
            CreateFileStream(request.DestinationPath, FileMode.Create, FileAccess.Write, _logger);

        var finalEncryptedKey = SecurePool.Rent(Constants.Security.KeyVault.FinalEncryptedKeySize);
        var nonce = SecurePool.Rent(Constants.Security.KeyVault.NonceSize);

        try
        {
            await sourceStream.ReadExactlyAsync(
                finalEncryptedKey.AsMemory(0, Constants.Security.KeyVault.FinalEncryptedKeySize), cancellationToken);
            await sourceStream.ReadExactlyAsync(nonce.AsMemory(0, Constants.Security.KeyVault.NonceSize),
                cancellationToken);

            var contentKey = await _keyVaultService.DecryptKeyAsync(
                finalEncryptedKey.AsSpan(0, Constants.Security.KeyVault.FinalEncryptedKeySize).ToArray(),
                filePublicMasterKey);

            try
            {
                await ProcessDecryptionStreamAsync(sourceStream, destinationStream, contentKey,
                    nonce.AsSpan(0, Constants.Security.KeyVault.NonceSize).ToArray(), cancellationToken);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(contentKey);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(finalEncryptedKey.AsSpan(0,
                Constants.Security.KeyVault.FinalEncryptedKeySize));
            CryptographicOperations.ZeroMemory(nonce.AsSpan(0, Constants.Security.KeyVault.NonceSize));
            SecurePool.Return(finalEncryptedKey, true);
            SecurePool.Return(nonce, true);
        }
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

        if (disposing && _keyVaultService is IDisposable vaultService)
            vaultService.Dispose();

        _disposed = true;
    }

    ~StorageService()
    {
        Dispose(false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
        if (_disposed is not true)
            return;

        throw new ObjectDisposedException(nameof(StorageService));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static FileStream CreateFileStream(
        string path,
        FileMode mode,
        FileAccess access,
        ILogger<StorageService>? logger)
    {
        var share = access is FileAccess.Read ? FileShare.Read : FileShare.None;
        var options = FileOptions.Asynchronous
                      | FileOptions.SequentialScan
                      | (access is FileAccess.Write ? FileOptions.WriteThrough : FileOptions.None);

        return DirectStreamFactory.Create(
            path,
            mode,
            access,
            share,
            Constants.Storage.BufferSize,
            options,
            logger);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte[] GenerateRandomKey(int keySize = Constants.Security.KeyVault.DefaultKeySize)
    {
        var key = new byte[keySize];
        RandomNumberGenerator.Fill(key);
        return key;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DeriveNonce(byte[] salt, long blockIndex, byte[] outputNonce)
    {
        var hashProvider = HashAlgorithmProviderFactory.Instance;
        var hmacKeySize = hashProvider.GetHmacKeySize();

        Span<byte> blockIndexBytes = stackalloc byte[sizeof(long)];
        Span<byte> prk = stackalloc byte[hmacKeySize];
        Span<byte> info = stackalloc byte[sizeof(long) + Constants.Security.KeyVault.NonceContext.Length];
        Span<byte> okm = stackalloc byte[Constants.Security.KeyVault.NonceSize];

        try
        {
            BitConverter.TryWriteBytes(blockIndexBytes, blockIndex);
            using (var hmac = hashProvider.CreateHmac(salt))
            {
                hmac.TryComputeHash(blockIndexBytes, prk, out _);
            }

            blockIndexBytes.CopyTo(info);
            Constants.Security.KeyVault.NonceContext.CopyTo(info[sizeof(long)..]);

            HKDF.Expand(hashProvider.GetHashAlgorithmName(), prk, okm, info);
            okm.CopyTo(outputNonce.AsSpan(0, Constants.Security.KeyVault.NonceSize));
        }
        finally
        {
            prk.Clear();
            okm.Clear();
            info.Clear();
            blockIndexBytes.Clear();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PrecomputeSalt(byte[] originalNonce, byte[] salt)
    {
        var hashProvider = HashAlgorithmProviderFactory.Instance;

        var saltSize = hashProvider.GetSaltSize();
        if (salt.Length != saltSize)
            throw new ArgumentException($"Salt buffer size must be {saltSize} bytes.", nameof(salt));

        Span<byte> input = stackalloc byte[8];
        
        try
        {
            BitConverter.TryWriteBytes(input, 0L);

            using var hmac =
                hashProvider.CreateHmac(originalNonce.AsSpan(0, Constants.Security.KeyVault.NonceSize).ToArray());
            if (!hmac.TryComputeHash(input, salt, out var bytesWritten) || bytesWritten != saltSize)
                throw new CryptographicException(
                    $"Failed to derive salt: Expected {saltSize} bytes, got {bytesWritten}");
        }
        finally
        {
            input.Clear();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task ProcessEncryptionStreamAsync(Stream sourceStream, Stream destinationStream, byte[] key,
        byte[] nonce, CancellationToken cancellationToken)
    {
        using var aesGcm = new AesGcm(key, Constants.Security.KeyVault.TagSize);

        var saltSize = HashAlgorithmProviderFactory.Instance.GetSaltSize();

        var buffer = SecurePool.Rent(Constants.Storage.BufferSize);
        var ciphertext = SecurePool.Rent(Constants.Storage.BufferSize);
        var tag = SecurePool.Rent(Constants.Security.KeyVault.TagSize);
        var chunkNonce = SecurePool.Rent(Constants.Security.KeyVault.NonceSize);
        var salt = SecurePool.Rent(saltSize);

        try
        {
            PrecomputeSalt(nonce, salt);

            long blockIndex = 0;
            while (true)
            {
                var bytesRead = await sourceStream.ReadAsync(buffer.AsMemory(0, Constants.Storage.BufferSize),
                    cancellationToken);
                if (bytesRead is 0) break;

                DeriveNonce(salt, blockIndex, chunkNonce);

                aesGcm.Encrypt(
                    chunkNonce.AsSpan(0, Constants.Security.KeyVault.NonceSize),
                    buffer.AsSpan(0, bytesRead),
                    ciphertext.AsSpan(0, bytesRead),
                    tag.AsSpan(0, Constants.Security.KeyVault.TagSize));

                await destinationStream.WriteAsync(tag.AsMemory(0, Constants.Security.KeyVault.TagSize),
                    cancellationToken);
                await destinationStream.WriteAsync(ciphertext.AsMemory(0, bytesRead), cancellationToken);
                await destinationStream.FlushAsync(cancellationToken);

                blockIndex++;
            }
        }
        finally
        {
            SecurePool.Return(buffer, true);
            SecurePool.Return(ciphertext, true);
            SecurePool.Return(tag, true);
            SecurePool.Return(chunkNonce, true);
            SecurePool.Return(salt, true);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task ProcessDecryptionStreamAsync(Stream sourceStream, Stream destinationStream, byte[] key,
        byte[] nonce, CancellationToken cancellationToken)
    {
        using var aesGcm = new AesGcm(key, Constants.Security.KeyVault.TagSize);

        var saltSize = HashAlgorithmProviderFactory.Instance.GetSaltSize();

        var tag = SecurePool.Rent(Constants.Security.KeyVault.TagSize);
        var buffer = SecurePool.Rent(Constants.Storage.BufferSize);
        var plaintext = SecurePool.Rent(Constants.Storage.BufferSize);
        var chunkNonce = SecurePool.Rent(Constants.Security.KeyVault.NonceSize);
        var salt = SecurePool.Rent(saltSize);

        try
        {
            PrecomputeSalt(nonce, salt);

            long blockIndex = 0;
            while (sourceStream.Position < sourceStream.Length)
            {
                var tagRead = await sourceStream.ReadAsync(tag.AsMemory(0, Constants.Security.KeyVault.TagSize),
                    cancellationToken);
                if (tagRead < Constants.Security.KeyVault.TagSize)
                    break;

                var bytesRead = await sourceStream.ReadAsync(buffer.AsMemory(0, Constants.Storage.BufferSize),
                    cancellationToken);
                if (bytesRead is 0)
                    break;

                DeriveNonce(salt, blockIndex, chunkNonce);

                aesGcm.Decrypt(
                    chunkNonce.AsSpan(0, Constants.Security.KeyVault.NonceSize),
                    buffer.AsSpan(0, bytesRead),
                    tag.AsSpan(0, Constants.Security.KeyVault.TagSize),
                    plaintext.AsSpan(0, bytesRead));

                await destinationStream.WriteAsync(plaintext.AsMemory(0, bytesRead), cancellationToken);
                await destinationStream.FlushAsync(cancellationToken);

                blockIndex++;
            }
        }
        finally
        {
            SecurePool.Return(tag, true);
            SecurePool.Return(buffer, true);
            SecurePool.Return(plaintext, true);
            SecurePool.Return(chunkNonce, true);
            SecurePool.Return(salt, true);
        }
    }
}