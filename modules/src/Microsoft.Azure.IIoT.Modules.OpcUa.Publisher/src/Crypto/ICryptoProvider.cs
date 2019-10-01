using System.Threading.Tasks;

namespace OpcPublisher.Crypto
{
    public interface ICryptoProvider
    {
        Task<byte[]> EncryptAsync(byte[] plainData);

        Task<byte[]> DecryptAsync(byte[] encryptedData);
    }
}
