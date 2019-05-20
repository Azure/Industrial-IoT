using System;
using System.Text;
using System.Threading.Tasks;

namespace OpcPublisher.Crypto
{
    /// <summary>
    /// This class does not encrypt anything. It just converts strings to base64.
    /// In Standalone-Mode, we do not have a key that we can use to encrypt/decrypt data.
    /// </summary>
    public class StandaloneCryptoProvider : ICryptoProvider
    {
        public async Task<byte[]> DecryptAsync(byte[] encryptedData)
        {
            var encString = Encoding.UTF8.GetString(encryptedData);
            return Convert.FromBase64String(encString);
        }

        public async Task<byte[]> EncryptAsync(byte[] plainData)
        {
            var result = Convert.ToBase64String(plainData);
            return Encoding.UTF8.GetBytes(result);
        }
    }
}
