using Azure.Storage.Blobs.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace Archive;

class Program
{
    private const string configFilename = "archive.conf";
    private const int iterations = 1000;
    private const int keyLength = 32;
    private const int ivLength = 16;

    public enum Action
    {
        Setup,
        Upload,
        Download,
    }

    /// <summary>
    /// Archive.NET - Secure File Storage 
    /// </summary>
    /// <param name="action">Action to perform on the input</param>
    /// <param name="input">File to archive</param>
    /// <param name="output">Location to store the archived file</param>
    /// <param name="password">Password used to generate encryption key</param>
    public static int Main(Action? action, string? input, string? output, string? password)
    {
        if (!Validate(action, input, output, password))
        {
            return -1;
        }

        switch (action)
        {
            case Action.Setup:
                SetupConnectionString();
                break;

            case Action.Upload:
                UploadFile(input, output, password);
                break;

            case Action.Download:
                DownloadFile(input, output, password);
                break;
        }

        return 0;
    }

    private static bool Validate(
        [NotNullWhen(true)] Action? action,
        [NotNullWhen(true)] string? input,
        [NotNullWhen(true)] string? output,
        [NotNullWhen(true)] string? password)
    {
        var valid = true;

        if (action is null)
        {
            Console.Error.WriteLine("action is required.");
            valid = false;
        }

        if (action == Action.Setup)
        {
            return true;
        }

        if (input is null)
        {
            Console.Error.WriteLine("input file is required.");
            valid = false;
        }

        if (input is not null && action == Action.Upload && !File.Exists(input))
        {
            Console.Error.WriteLine("input file does not exists.");
            valid = false;
        }

        if (output is null)
        {
            Console.Error.WriteLine("output file is required.");
            valid = false;
        }

        if (string.IsNullOrEmpty(password))
        {
            Console.Error.WriteLine("password is required.");
            valid = false;
        }

        return valid;
    }

    private static bool Validate(string connectionString, string containerName)
    {
        if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(containerName))
        {
            Console.Error.WriteLine("Setup required.");
            return false;
        }

        return true;
    }

    private static void SetupConnectionString()
    {
        Console.WriteLine("Enter the connection string for Azure Blob Storage:");
        var connectionString = Console.ReadLine() ?? string.Empty;

        Console.WriteLine("Enter the container name for Azure Blob Storage:");
        var containerName = Console.ReadLine() ?? string.Empty;

        WriteConfig(connectionString, containerName);

        Console.WriteLine("Setup completed.");
    }

    private static void UploadFile(string input, string output, string password)
    {
        var (connectionString, containerName) = LoadConfig();

        if (!Validate(connectionString, containerName))
        {
            return;
        }

        var salt = GenerateSalt();
        var key = GenerateKey(salt, password);

        var blockClient = new BlockBlobClient(connectionString, containerName, output);

        using var inputStream = new FileStream(input, FileMode.Open, FileAccess.Read);
        using var outputStream = blockClient.OpenWrite(overwrite: true);

        using var aes = Aes.Create();
        aes.Key = key;

        outputStream.Write(salt);
        outputStream.Write(aes.IV);

        using var cryptoStream = new CryptoStream(outputStream, aes.CreateEncryptor(), CryptoStreamMode.Write);

        inputStream.CopyTo(cryptoStream);

        cryptoStream.Flush();
    }

    private static void DownloadFile(string input, string output, string password)
    {
        var (connectionString, containerName) = LoadConfig();

        if (!Validate(connectionString, containerName))
        {
            return;
        }

        var blockClient = new BlockBlobClient(connectionString, containerName, input);

        using var inputStream = blockClient.OpenRead();
        using var outputStream = new FileStream(output, FileMode.OpenOrCreate, FileAccess.Write);

        var salt = new byte[keyLength];
        var iv = new byte[ivLength];

        inputStream.Read(salt);
        inputStream.Read(iv);

        using var aes = Aes.Create();
        aes.Key = GenerateKey(salt, password);
        aes.IV = iv;

        using var cryptoStream = new CryptoStream(inputStream, aes.CreateDecryptor(), CryptoStreamMode.Read);

        cryptoStream.CopyTo(outputStream);

        outputStream.Flush();
    }

    private static FileInfo GetConfigFile() => new (Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), configFilename));

    private static (string connectionString, string containerName) LoadConfig()
    {
        using var configFile = GetConfigFile().OpenText();

        var connectionString = configFile.ReadLine() ?? string.Empty;
        var containerName = configFile.ReadLine() ?? string.Empty;

        return (connectionString, containerName);
    }

    private static void WriteConfig(string connectionString, string containerName)
    {
        using var configFile = GetConfigFile().CreateText();

        configFile.WriteLine(connectionString);
        configFile.WriteLine(containerName);

        configFile.Flush();
    }

    private static byte[] GenerateSalt() => RandomNumberGenerator.GetBytes(keyLength);

    private static byte[] GenerateKey(byte[] salt, string password) => Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, keyLength);
}