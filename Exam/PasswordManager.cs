using System;
using System.Security.Cryptography;
using System.Text;

namespace C_FinalTask
{
    // Generates passwords and creates SHA256 hashes
    public class PasswordManager
    {
        // Salt added before hashing
        public const string SALT = "MySuperSecretSalt123";

        // Characters allowed in passwords
        private const string CHARSET = "abcdefghijklmnopqrstuvwxyz0123456789";

        private readonly Random _random = new Random();


        [ThreadStatic]
        private static SHA256 _sha256;

        private static SHA256 Sha256 => _sha256 ?? (_sha256 = SHA256.Create());

        // Generates a random password with length 4 or 5
        public string GeneratePassword()
        {
            // 6 is also included
            int length = _random.Next(4, 6);

            var password = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                password.Append(CHARSET[_random.Next(CHARSET.Length)]);
            }

            return password.ToString();
        }

        // Creates a SHA256 hash from the password and salt
        public static string HashPassword(string plainText)
        {
            string salted = SALT + plainText;
            byte[] inputBytes = Encoding.UTF8.GetBytes(salted);
            byte[] hashBytes = Sha256.ComputeHash(inputBytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }

        public static byte[] HashPasswordBytes(string plainText)
        {
            string salted = SALT + plainText;
            byte[] inputBytes = Encoding.UTF8.GetBytes(salted);
            return Sha256.ComputeHash(inputBytes);
        }
    }
}
