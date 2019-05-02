using Newtonsoft.Json;
using OpcPublisher.Http;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace OpcPublisher.Crypto
{

    public class IotEdgeCryptoProvider : ICryptoProvider
    {
        private class EncryptRequestBody
        {
            [JsonProperty("plaintext")]
            public string PlainText { get; set; }

            [JsonProperty("initializationVector")]
            public string InitializationVector { get; set; }
        }

        private class DecryptRequestBody
        {
            [JsonProperty("ciphertext")]
            public string CipherText { get; set; }

            [JsonProperty("initializationVector")]
            public string InitializationVector { get; set; }
        }

        private class EncryptResponse
        {
            [JsonProperty("ciphertext")]
            public string CipherText { get; set; }
        }

        private class DecryptResponse
        {
            [JsonProperty("plaintext")]
            public string PlainText { get; set; }
        }

        public IotEdgeCryptoProvider()
        {
            var baseUrl = Environment.GetEnvironmentVariable("IOTEDGE_WORKLOADURI").TrimEnd('/');
            var moduleName = Environment.GetEnvironmentVariable("IOTEDGE_MODULEID");
            var moduleGenId = Environment.GetEnvironmentVariable("IOTEDGE_MODULEGENERATIONID");
            var apiVersion = "2018-06-28";

            workloadHttpClient = HttpClientHelper.GetHttpClient(new Uri(baseUrl));

            workloadBaseUriPattern = $"{baseUrl}/modules/{moduleName}/genid/{moduleGenId}/{{0}}?api-version={apiVersion}";
        }

        private readonly HttpClient workloadHttpClient = null;
        private readonly string workloadBaseUriPattern = null;
        private const string InitializationVector = "alKGJdfsgidfasdO";

        private async Task<TOut> CallWorkloadApi<TIn, TOut>(string endpoint, TIn payload)
        {
            var url = string.Format(workloadBaseUriPattern, endpoint);

            var jsonPayload = JsonConvert.SerializeObject(payload);
            var payloadContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var requestResult = await workloadHttpClient.PostAsync(url, payloadContent);
            var jsonResult = await requestResult.Content.ReadAsStringAsync();
            TOut result = JsonConvert.DeserializeObject<TOut>(jsonResult);
            return result;
        }

        public async Task<byte[]> EncryptAsync(byte[] plainData)
        {
            string plainDataBase64 = Convert.ToBase64String(plainData);
            var encryptRequestBody = new EncryptRequestBody() { InitializationVector = InitializationVector, PlainText = plainDataBase64 };
            var result = await CallWorkloadApi<EncryptRequestBody, EncryptResponse>("encrypt", encryptRequestBody);
            return Convert.FromBase64String(result.CipherText);
        }

        public async Task<byte[]> DecryptAsync(byte[] encryptedData)
        {
            string encryptedDataBase64 = Convert.ToBase64String(encryptedData);
            var decryptRequestBody = new DecryptRequestBody() { InitializationVector = InitializationVector, CipherText = encryptedDataBase64 };
            var result = await CallWorkloadApi<DecryptRequestBody, DecryptResponse>("decrypt", decryptRequestBody);
            return Convert.FromBase64String(result.PlainText);
        }
    }
}
