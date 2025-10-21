using System.Security.Cryptography;
using System.Text;

namespace GoNomads.Shared.Security;

/// <summary>
/// 密码哈希工具类
/// </summary>
public static class PasswordHasher
{
    private const int SaltSize = 16; // 128 bit
    private const int KeySize = 32; // 256 bit
    private const int Iterations = 10000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    /// <summary>
    /// 哈希密码
    /// </summary>
    public static string HashPassword(string password)
    {
        // 生成随机盐值
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        
        // 使用 PBKDF2 派生密钥
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            Algorithm,
            KeySize);

        // 组合盐值和哈希值: salt + hash
        byte[] result = new byte[SaltSize + KeySize];
        Buffer.BlockCopy(salt, 0, result, 0, SaltSize);
        Buffer.BlockCopy(hash, 0, result, SaltSize, KeySize);

        return Convert.ToBase64String(result);
    }

    /// <summary>
    /// 验证密码
    /// </summary>
    public static bool VerifyPassword(string password, string passwordHash)
    {
        try
        {
            byte[] hashBytes = Convert.FromBase64String(passwordHash);

            // 提取盐值
            byte[] salt = new byte[SaltSize];
            Buffer.BlockCopy(hashBytes, 0, salt, 0, SaltSize);

            // 提取已存储的哈希值
            byte[] storedHash = new byte[KeySize];
            Buffer.BlockCopy(hashBytes, SaltSize, storedHash, 0, KeySize);

            // 使用相同的盐值计算新密码的哈希
            byte[] testHash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                Iterations,
                Algorithm,
                KeySize);

            // 比较哈希值
            return CryptographicOperations.FixedTimeEquals(storedHash, testHash);
        }
        catch
        {
            return false;
        }
    }
}
