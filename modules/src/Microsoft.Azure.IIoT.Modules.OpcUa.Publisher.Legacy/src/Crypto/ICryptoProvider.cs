using System.Threading.Tasks;

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Crypto
{
    public interface ICryptoProvider
    {
        Task<byte[]> EncryptAsync(byte[] plainData);

        Task<byte[]> DecryptAsync(byte[] encryptedData);
    }
}
