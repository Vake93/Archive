using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace Archive;

class Program
{
    private const int iterations = 1000;
    private const int keyLength = 32;
    private const int ivLength = 16;

    public enum Action
    {
        Encrypt,
        Decrypt,
    }

    /// <summary>
    /// Archive.NET - Secure File Storage 
    /// </summary>
    /// <param name="action">Action to perform on the input</param>
    /// <param name="input">File to archive</param>
    /// <param name="output">Location to store the archived file</param>
    /// <param name="password">Password used to generate encryption key</param>
    public static int Main(Action? action, FileInfo? input, FileInfo? output, string? password)
    {
        if (!Validate(action, input, output, password))
        {
            return -1;
        }

        switch (action)
        {
            case Action.Encrypt:
                EncryptFile(input, output, password);
                break;

            case Action.Decrypt:
                DecryptFile(input, output, password);
                break;
        }

        return 0;
    }

    private static bool Validate(
        [NotNullWhen(true)] Action? action,
        [NotNullWhen(true)] FileInfo? input,
        [NotNullWhen(true)] FileInfo? output,
        [NotNullWhen(true)] string? password)
    {
        var valid = true;

        if (action is null)
        {
            Console.Error.WriteLine("action is required.");
            valid = false;
        }

        if (input is null)
        {
            Console.Error.WriteLine("input file is required.");
            valid = false;
        }

        if (input is not null && !input.Exists)
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

    private static void EncryptFile(FileInfo input, FileInfo output, string password)
    {
        var salt = GenerateSalt();
        var key = GenerateKey(salt, password);

        using var inputStream = input.OpenRead();
        using var outputStream = output.OpenWrite();

        using var aes = Aes.Create();
        aes.Key = key;

        outputStream.Write(salt);
        outputStream.Write(aes.IV);

        using var cryptoStream = new CryptoStream(outputStream, aes.CreateEncryptor(), CryptoStreamMode.Write);

        inputStream.CopyTo(cryptoStream);

        cryptoStream.Flush();
    }

    private static void DecryptFile(FileInfo input, FileInfo output, string password)
    {
        using var inputStream = input.OpenRead();
        using var outputStream = output.OpenWrite();

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

    private static byte[] GenerateSalt() => RandomNumberGenerator.GetBytes(keyLength);

    private static byte[] GenerateKey(byte[] salt, string password) => Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, keyLength);
}