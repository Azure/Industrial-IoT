using System;
using System.Text;
using System.Threading.Tasks;

namespace OpcPublisher.Crypto
{
    public static class CryptoProviderExtensions
    {
        public static async Task<string> EncryptAsync(this ICryptoProvider cryptoProvider, string plainValue)
        {
            var plainData = Encoding.UTF8.GetBytes(plainValue);
            var result = await cryptoProvider.EncryptAsync(plainData);
            return Convert.ToBase64String(result);
        }

        public static async Task<string> DecryptAsync(this ICryptoProvider cryptoProvider, string encryptedValue)
        {
            var encryptedData = Convert.FromBase64String(encryptedValue);
            var result = await cryptoProvider.DecryptAsync(encryptedData);
            return Encoding.UTF8.GetString(result);
        }
    }
}
