using System.Net;
using System.Threading.Tasks;

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Crypto
{
    /// <summary>
    /// Class to provide in-memory access to encrypted credentials. It uses the static CryptoProvider from the
    /// Programm-class to encrypt/decrypt credentials.
    /// </summary>
    public class EncryptedNetworkCredential : NetworkCredential
    {
        public async static Task<EncryptedNetworkCredential> FromPlainCredential(string username, string password)
        {
            return await FromNetworkCredential(new NetworkCredential(username, password));
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

            if (UserName != null)
            {
                result.UserName = await Program.CryptoProvider.DecryptAsync(UserName);
            }

            if (Password != null)
            {
                result.Password = await Program.CryptoProvider.DecryptAsync(Password);
            }

            return result;
        }

        public override bool Equals(object obj)
        {
            var other = obj as EncryptedNetworkCredential;

            if (other != null)
            {
                return UserName.Equals(other.UserName) && Password.Equals(other.Password);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return (UserName + Password).GetHashCode();
        }
    }
}
