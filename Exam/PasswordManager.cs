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
            // Combine salt + password into one string
            string salted = SALT + plainText;

            // Convert the string to bytes
            byte[] inputBytes = Encoding.UTF8.GetBytes(salted);

            // SHA256.Create())
            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(inputBytes);

                //  Convert the hash to a hexadecimal string.
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
    }
}
