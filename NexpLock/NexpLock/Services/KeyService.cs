using System;
using System.Security.Cryptography;
using NexpLock.Interfaces;

namespace NexpLock.Services;

public sealed class KeyService : IKeyService
{
    public string GenerateKey()
    {
        var keyBytes = new byte[Constants.Encryption.KeySize / 8];
        RandomNumberGenerator.Fill(keyBytes);

        using var keyDerivation = new Rfc2898DeriveBytes(
            keyBytes,
            RandomNumberGenerator.GetBytes(Constants.Encryption.SaltSize),
            Constants.Encryption.Iterations,
            Constants.Encryption.HashAlgorithm
        );

        var derivedKey = keyDerivation.GetBytes(Constants.Encryption.KeySize / 8);
        return Convert.ToBase64String(derivedKey);
    }
}