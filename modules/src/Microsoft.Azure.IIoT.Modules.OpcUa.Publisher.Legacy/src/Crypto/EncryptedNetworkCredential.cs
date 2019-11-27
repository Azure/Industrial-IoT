using System.Net;
using System.Threading.Tasks;

namespace OpcPublisher.Crypto
{
    /// <summary>
    /// Class to provide in-memory access to encrypted credentials. It uses the static CryptoProvider from the
    /// Programm-class to encrypt/decrypt credentials.
    /// </summary>
    public class EncryptedNetworkCredential : NetworkCredential
    {
        public async static Task<EncryptedNetworkCredential> FromPlainCredential(string username, string password)
        {
            return await EncryptedNetworkCredential.FromNetworkCredential(new NetworkCredential(username, password));
        }

        public async static Task<EncryptedNetworkCredential> FromNetworkCredential(NetworkCredential networkCredential)
        {
            EncryptedNetworkCredential encryptedNetworkCredential = new EncryptedNetworkCredential();

            if (networkCredential.UserName != null)
            {
                encryptedNetworkCredential.UserName = await Program.CryptoProvider.EncryptAsync(networkCredential.UserName);
            }

            if (networkCredential.Password != null)
            {
                encryptedNetworkCredential.Password = await Program.CryptoProvider.EncryptAsync(networkCredential.Password);
            }

            return encryptedNetworkCredential;
        }

        public async Task<NetworkCredential> Decrypt()
        {
            var result = new NetworkCredential();

            if (this.UserName != null)
            {
                result.UserName = await Program.CryptoProvider.DecryptAsync(this.UserName);
            }

            if (this.Password != null)
            {
                result.Password = await Program.CryptoProvider.DecryptAsync(this.Password);
            }

            return result;
        }

        public override bool Equals(object obj)
        {
            var other = obj as EncryptedNetworkCredential;

            if (other != null)
            {
                return this.UserName.Equals(other.UserName) && this.Password.Equals(other.Password);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return (this.UserName + this.Password).GetHashCode();
        }
    }
}
