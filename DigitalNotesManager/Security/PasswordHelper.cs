using System.Security.Cryptography;

namespace DigitalNotesManager.Security
{
    public static class PasswordHelper
    {
        private const int Iterations = 100_000;
        private const int SaltSize = 16; // 128-bit
        private const int KeySize = 32; // 256-bit

        // format: v1$<iterations>$<saltBase64>$<hashBase64>
        public static string Hash(string password)
        {
            using var rng = RandomNumberGenerator.Create();
            var salt = new byte[SaltSize];
            rng.GetBytes(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            var key = pbkdf2.GetBytes(KeySize);

            return $"v1${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
        }

        public static bool Verify(string password, string stored)
        {
            try
            {
                var parts = stored.Split('$');
                if (parts.Length != 4 || parts[0] != "v1") return false;

                int iterations = int.Parse(parts[1]);
                var salt = Convert.FromBase64String(parts[2]);
                var hash = Convert.FromBase64String(parts[3]);

                using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
                var test = pbkdf2.GetBytes(hash.Length);

                return CryptographicOperations.FixedTimeEquals(test, hash);
            }
            catch { return false; }
        }
    }
}
