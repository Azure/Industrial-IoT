// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Hosting {
    using Microsoft.Azure.IIoT.Abstractions;
    using Microsoft.Azure.IIoT.Crypto;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Serializers;
    using Serilog;
    using System;
    using System.Threading;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Security.Cryptography.X509Certificates;
    using System.Runtime.Serialization;

    /// <summary>
    /// Edgelet client providing discovery and in the future other services
    /// </summary>
    public sealed class EdgeletClient : ISecureElement {

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="client"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        public EdgeletClient(IHttpClient client, IJsonSerializer serializer,
            ILogger logger) : this(client, serializer,
            Environment.GetEnvironmentVariable(IoTEdgeVariables.IOTEDGE_WORKLOADURI),
            Environment.GetEnvironmentVariable(IoTEdgeVariables.IOTEDGE_MODULEGENERATIONID),
            Environment.GetEnvironmentVariable(IoTEdgeVariables.IOTEDGE_MODULEID),
            Environment.GetEnvironmentVariable(IoTEdgeVariables.IOTEDGE_APIVERSION),
            logger) {
        }

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="client"></param>
        /// <param name="serializer"></param>
        /// <param name="workloaduri"></param>
        /// <param name="genId"></param>
        /// <param name="moduleId"></param>
        /// <param name="apiVersion"></param>
        /// <param name="logger"></param>
        public EdgeletClient(IHttpClient client, IJsonSerializer serializer,
            string workloaduri, string genId, string moduleId, string apiVersion,
            ILogger logger) {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _workloaduri = workloaduri?.TrimEnd('/');
            _moduleGenerationId = genId;
            _moduleId = moduleId;
            _apiVersion = apiVersion ?? "2019-01-30";
        }

        /// <inheritdoc/>
        public async Task<X509Certificate2Collection> CreateServerCertificateAsync(
            string commonName, DateTime expiration, CancellationToken ct) {
            var request = _client.NewRequest(
                $"{_workloaduri}/modules/{_moduleId}/genid/{_moduleGenerationId}/" +
                $"certificate/server?api-version={_apiVersion}");
            _serializer.SerializeToRequest(request, new { commonName, expiration });
            return await Retry.WithExponentialBackoff(_logger, ct, async () => {
                var response = await _client.PostAsync(request, ct);
                response.Validate();
                var result = _serializer.DeserializeResponse<EdgeletCertificateResponse>(response);
                // TODO add private key
                return new X509Certificate2Collection(
                    X509Certificate2Ex.ParsePemCerts(result.Certificate).ToArray());
            }, kMaxRetryCount);
        }

        /// <inheritdoc/>
        public async Task<byte[]> EncryptAsync(
            string initializationVector, byte[] plaintext, CancellationToken ct) {
            var request = _client.NewRequest(
                $"{_workloaduri}/modules/{_moduleId}/genid/{_moduleGenerationId}/" +
                $"encrypt?api-version={_apiVersion}");
            _serializer.SerializeToRequest(request, new { initializationVector, plaintext });
            return await Retry.WithExponentialBackoff(_logger, ct, async () => {
                var response = await _client.PostAsync(request, ct);
                response.Validate();
                return _serializer.DeserializeResponse<EncryptResponse>(response).CipherText;
            }, kMaxRetryCount);
        }

        /// <inheritdoc/>
        public async Task<byte[]> DecryptAsync(
            string initializationVector, byte[] ciphertext, CancellationToken ct) {
            var request = _client.NewRequest(
                $"{_workloaduri}/modules/{_moduleId}/genid/{_moduleGenerationId}/" +
                $"decrypt?api-version={_apiVersion}");
            _serializer.SerializeToRequest(request, new { initializationVector, ciphertext });
            return await Retry.WithExponentialBackoff(_logger, ct, async () => {
                var response = await _client.PostAsync(request, ct);
                response.Validate();
                return _serializer.DeserializeResponse<DecryptResponse>(response).Plaintext;
            }, kMaxRetryCount);
        }

        /// <summary>
        /// Encrypt response
        /// </summary>
        [DataContract]
        public class EncryptResponse {

            /// <summary>Cypher.</summary>
            [DataMember(Name = "cipherText")]
            public byte[] CipherText { get; set; }
        }

        /// <summary>
        /// Decrypt response
        /// </summary>
        [DataContract]
        public class DecryptResponse {

            /// <summary>Cypher.</summary>
            [DataMember(Name = "plaintext")]
            public byte[] Plaintext { get; set; }
        }

        /// <summary>
        /// Edgelet create certificate response
        /// </summary>
        [DataContract]
        public class EdgeletCertificateResponse {

            /// <summary>
            /// Base64 encoded PEM formatted byte array
            /// containing the certificate and its chain.
            /// </summary>
            [DataMember(Name = "certificate")]
            public string Certificate { get; set; }

            /// <summary>Private key.</summary>
            [DataMember(Name = "privateKey")]
            public EdgeletPrivateKey PrivateKey { get; set; }
        }

        /// <summary>
        /// Edgelet private key
        /// </summary>
        [DataContract]
        public class EdgeletPrivateKey {

            /// <summary>Type of private key.</summary>
            [DataMember(Name = "type")]
            public string Type { get; set; }

            /// <summary>Base64 encoded PEM formatted byte array</summary>
            [DataMember(Name = "bytes")]
            public string Bytes { get; set; }
        }

        private readonly IHttpClient _client;
        private readonly IJsonSerializer _serializer;
        private readonly ILogger _logger;
        private readonly string _workloaduri;
        private readonly string _moduleGenerationId;
        private readonly string _moduleId;
        private readonly string _apiVersion;
        private const int kMaxRetryCount = 3;
    }
}
