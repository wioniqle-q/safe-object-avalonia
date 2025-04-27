using System.Security.Cryptography;

namespace NexpLock;

public static class Constants
{
    public static class FileDialog
    {
        public const string OpenFileTitle = "Open File";
    }

    public static class Operation
    {
        public const string Ready = "Ready";
        public const string Canceled = "Canceled";
        public const string Failed = "Failed";
        public const string Completed = "Completed";
    }

    public static class Crypto
    {
        public const string SuggestedStartLocation = Vault.EncryptedFolder;
    }

    public static class Vault
    {
        public const string EncryptedFolder = "seal";
        public const string DecryptedFolder = "bare";
    }

    public static class Exception
    {
        public const string NoStorageProvider = "No storage provider is available for the window.";
        public const string NoWindowInstance = "Window instance cannot be null.";
        public const string NoScreensAvailable = "The Screens property on the window is not available.";
        public const string NoServiceProvider = "Service provider is not initialized.";

        public const string FileNotFound = "The specified file does not exist.";
        public const string FileIdNotFound = "File ID not found in the file name.";
    }

    public static class Encryption
    {
        public const int Iterations = 600 * 1000;
        public const int KeySize = 256;
        public const int SaltSize = 32;
        public const int BufferSize = 81920;

        public static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA256;
    }
}