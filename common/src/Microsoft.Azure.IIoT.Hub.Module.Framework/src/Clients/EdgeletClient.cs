// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Hosting {
    using Microsoft.Azure.IIoT.Module.Models;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.Utils;
    using Newtonsoft.Json;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Security.Cryptography.X509Certificates;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Edgelet client providing discovery and in the future other services
    /// </summary>
    public sealed class EdgeletClient : IModuleDiscovery, ISecureElement {

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="client"></param>
        /// <param name="logger"></param>
        public EdgeletClient(IHttpClient client, ILogger logger) : this(client,
            Environment.GetEnvironmentVariable("IOTEDGE_WORKLOADURI")?.TrimEnd('/'),
            Environment.GetEnvironmentVariable("IOTEDGE_MODULEGENERATIONID"),
            Environment.GetEnvironmentVariable("IOTEDGE_MODULEID"),
            Environment.GetEnvironmentVariable("IOTEDGE_APIVERSION"),
            logger) {
        }

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="client"></param>
        /// <param name="workloaduri"></param>
        /// <param name="genId"></param>
        /// <param name="moduleId"></param>
        /// <param name="apiVersion"></param>
        /// <param name="logger"></param>
        public EdgeletClient(IHttpClient client, string workloaduri,
            string genId, string moduleId, string apiVersion,
            ILogger logger) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _workloaduri = workloaduri;
            _moduleGenerationId = genId;
            _moduleId = moduleId;
            _apiVersion = apiVersion ?? "2019-01-30";
        }

        /// <inheritdoc/>
        public async Task<List<DiscoveredModuleModel>> GetModulesAsync(
            string deviceId, CancellationToken ct) {

            if (!string.IsNullOrEmpty(_workloaduri)) {
                try {
                    var uri = _workloaduri + "/modules?api-version=" + _apiVersion;
                    _logger.Debug("Calling GET on {uri} uri...", uri);

                    var request = _client.NewRequest(uri);
                    var result = await Retry.WithExponentialBackoff(_logger, ct, async () => {
                        var response = await _client.GetAsync(request, ct);
                        var payload = response.GetContentAsString();
                        if (response.StatusCode == System.Net.HttpStatusCode.OK) {
                            _logger.Debug("... returned {statusCode}.", response.StatusCode);
                            _logger.Verbose("payload: {payload}.", payload);
                        }
                        else {
                            _logger.Warning("... resulted in {statusCode} with error: {payload}.",
                                response.StatusCode, payload);
                        }
                        response.Validate();
                        return JsonConvertEx.DeserializeObject<EdgeletModules>(payload);
                    }, kMaxRetryCount);
                    return result.Modules?.Select(m => new DiscoveredModuleModel {
                        Id = m.Name,
                        ImageName = m.Config?.Settings?.Image,
                        ImageHash = m.Config?.Settings?.ImageHash,
                        Version = GetVersionFromImageName(m.Config?.Settings?.Image),
                        Status = m.Status?.RuntimeStatus?.Status
                    }).ToList();
                }
                catch (Exception ex) {
                    _logger.Error(ex, "Error during GetModulesAsync");
                }
                return new List<DiscoveredModuleModel>();
            }
            _logger.Warning("Not running in iotedge context - no modules in scope.");
            return new List<DiscoveredModuleModel>();
        }

        /// <inheritdoc/>
        public async Task<X509Certificate2Collection> CreateServerCertificateAsync(
            string commonName, DateTime expiration, CancellationToken ct) {
            var request = _client.NewRequest(
                $"{_workloaduri}/modules/{_moduleId}/genid/{_moduleGenerationId}/" +
                $"certificate/server?api-version={_apiVersion}");
            request.SetContent(new { commonName, expiration });
            return await Retry.WithExponentialBackoff(_logger, ct, async () => {
                var response = await _client.PostAsync(request, ct);
                response.Validate();
                var result = JsonConvertEx.DeserializeObject<EdgeletCertificateResponse>(
                   response.GetContentAsString());
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
            request.SetContent(new { initializationVector, plaintext });
            return await Retry.WithExponentialBackoff(_logger, ct, async () => {
                var response = await _client.PostAsync(request, ct);
                response.Validate();
                return JObject.Parse(response.GetContentAsString())?
                    .GetValueOrDefault<byte[]>("ciphertext");
            }, kMaxRetryCount);
        }

        /// <inheritdoc/>
        public async Task<byte[]> DecryptAsync(
            string initializationVector, byte[] ciphertext, CancellationToken ct) {
            var request = _client.NewRequest(
                $"{_workloaduri}/modules/{_moduleId}/genid/{_moduleGenerationId}/" +
                $"decrypt?api-version={_apiVersion}");
            request.SetContent(new { initializationVector, ciphertext });
            return await Retry.WithExponentialBackoff(_logger, ct, async () => {
                var response = await _client.PostAsync(request, ct);
                response.Validate();
                return JObject.Parse(response.GetContentAsString())?
                    .GetValueOrDefault<byte[]>("plaintext");
            }, kMaxRetryCount);
        }

        /// <summary>
        /// Parse version out of image name
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private static string GetVersionFromImageName(string image) {
            var index = image?.LastIndexOf(':') ?? -1;
            return index == -1 ? null : image.Substring(index + 1);
        }

        /// <summary>
        /// Edgelet create certificate response
        /// </summary>
        public class EdgeletCertificateResponse {

            /// <summary>
            /// Base64 encoded PEM formatted byte array
            /// containing the certificate and its chain.
            /// </summary>
            [JsonProperty(PropertyName = "certificate")]
            public string Certificate { get; set; }

            /// <summary>Private key.</summary>
            [JsonProperty(PropertyName = "privateKey")]
            public EdgeletPrivateKey PrivateKey { get; set; }
        }

        /// <summary>
        /// Edgelet private key
        /// </summary>
        public class EdgeletPrivateKey {

            /// <summary>Type of private key.</summary>
            [JsonProperty(PropertyName = "type")]
            public string Type { get; set; }

            /// <summary>Base64 encoded PEM formatted byte array</summary>
            [JsonProperty(PropertyName = "bytes")]
            public string Bytes { get; set; }
        }

        /// <summary>
        /// Edgelet modules
        /// </summary>
        public class EdgeletModules {

            /// <summary> Modules </summary>
            [JsonProperty(PropertyName = "modules")]
            public List<EdgeletModuleDetails> Modules { get; set; }
        }

        /// <summary>
        /// Edgelet module details model
        /// </summary>
        public class EdgeletModuleDetails {

            /// <summary> Module id </summary>
            [JsonProperty(PropertyName = "id")]
            public string Id { get; set; }

            /// <summary> Module name </summary>
            [JsonProperty(PropertyName = "name")]
            public string Name { get; set; }

            /// <summary> Module type </summary>
            [JsonProperty(PropertyName = "type")]
            public string Type { get; set; }

            /// <summary> Module config</summary>
            [JsonProperty(PropertyName = "config")]
            public EdgeletModuleConfig Config { get; set; }

            /// <summary> Module status </summary>
            [JsonProperty(PropertyName = "status")]
            public EdgeletModuleStatus Status { get; set; }
        }

        /// <summary>
        /// Module runtime status
        /// </summary>
        public class EdgeletModuleStatus {

            /// <summary> Runtime status </summary>
            [JsonProperty(PropertyName = "runtimeStatus")]
            public EdgeletModuleRuntimeStatus RuntimeStatus { get; set; }

            /// <summary> Exit status </summary>
            [JsonProperty(PropertyName = "exitStatus")]
            public EdgeletModuleExitStatus ExitStatus { get; set; }
        }

        /// <summary>
        /// Module exit status
        /// </summary>
        public class EdgeletModuleExitStatus {

            /// <summary> Exit status code </summary>
            [JsonProperty(PropertyName = "statusCode")]
            public string StatusCode { get; set; }

            /// <summary> Exit time </summary>
            [JsonProperty(PropertyName = "exitTime")]
            public string ExitTime { get; set; }
        }

        /// <summary>
        /// Module runtime status
        /// </summary>
        public class EdgeletModuleRuntimeStatus {

            /// <summary> Module status </summary>
            [JsonProperty(PropertyName = "status")]
            public string Status { get; set; }

            /// <summary> Module status description </summary>
            [JsonProperty(PropertyName = "description")]
            public string Description { get; set; }
        }

        /// <summary>
        /// Module config
        /// </summary>
        public class EdgeletModuleConfig {

            /// <summary> Module settings</summary>
            [JsonProperty(PropertyName = "settings")]
            public EdgeletModuleSettings Settings { get; set; }
        }

        /// <summary>
        /// Edge agent managed settings
        /// </summary>
        public class EdgeletModuleSettings {

            /// <summary> Module image </summary>
            [JsonProperty(PropertyName = "image")]
            public string Image { get; set; }

            /// <summary> Image hash </summary>
            [JsonProperty(PropertyName = "imageHash")]
            public string ImageHash { get; set; }

            /// <summary> Create Options </summary>
            [JsonProperty(PropertyName = "createOptions")]
            public JToken CreateOptions { get; set; }
        }

        private readonly IHttpClient _client;
        private readonly ILogger _logger;
        private readonly string _workloaduri;
        private readonly string _moduleGenerationId;
        private readonly string _moduleId;
        private readonly string _apiVersion;
        private const int kMaxRetryCount = 3;
    }
}
