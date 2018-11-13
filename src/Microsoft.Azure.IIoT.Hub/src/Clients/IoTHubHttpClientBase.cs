// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Clients {
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.Utils;
    using System;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// Messaging service client
    /// </summary>
    public abstract class IoTHubHttpClientBase {

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        protected IoTHubHttpClientBase(IHttpClient httpClient,
            IIoTHubConfig config, ILogger logger) {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (config == null) {
                throw new ArgumentNullException(nameof(config));
            }
            if (string.IsNullOrEmpty(config.IoTHubConnString)) {
                throw new ArgumentException(nameof(config.IoTHubConnString));
            }
            _resourceId = config.IoTHubResourceId;
            _hubConnectionString = ConnectionString.Parse(config.IoTHubConnString);
        }

        /// <summary>
        /// Helper to create new request
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        protected IHttpRequest NewRequest(string path) {
            var request = _httpClient.NewRequest(new UriBuilder {
                Scheme = "https",
                Host = _hubConnectionString.HostName,
                Path = path,
                Query = "api-version=" + kApiVersion
            }.Uri, _resourceId);
            request.Headers.Add(HttpRequestHeader.Authorization.ToString(),
                CreateSasToken(_hubConnectionString, 3600));
            request.Headers.Add(HttpRequestHeader.UserAgent.ToString(), kClientId);
            return request;
        }

        /// <summary>
        /// Helper to create resource path for device and optional module
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <returns></returns>
        protected static string ToResourceId(string deviceId, string moduleId) =>
            string.IsNullOrEmpty(moduleId) ? deviceId : $"{deviceId}/modules/{moduleId}";

        /// <summary>
        /// Create a token for iothub from connection string.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="validityPeriodInSeconds"></param>
        /// <returns></returns>
        protected static string CreateSasToken(ConnectionString connectionString,
            int validityPeriodInSeconds) {
            // http://msdn.microsoft.com/en-us/library/azure/dn170477.aspx
            // signature is computed from joined encoded request Uri string and expiry string
            var expiryTime = DateTime.UtcNow + TimeSpan.FromSeconds(validityPeriodInSeconds);
            var expiry = ((long)(expiryTime -
                new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds).ToString();
            var encodedScope = Uri.EscapeDataString(connectionString.HostName);
            // the connection string signature is base64 encoded
            var key = connectionString.SharedAccessKey.DecodeAsBase64();
            using (var hmac = new HMACSHA256(key)) {
                var sig = hmac.ComputeHash(Encoding.UTF8.GetBytes(encodedScope + "\n" + expiry))
                    .ToBase64String();
                return $"SharedAccessSignature sr={encodedScope}" +
                    $"&sig={Uri.EscapeDataString(sig)}&se={Uri.EscapeDataString(expiry)}" +
                    $"&skn={Uri.EscapeDataString(connectionString.SharedAccessKeyName)}";
            }
        }

        const string kApiVersion = "2018-06-30";
        const string kClientId = "OpcTwin";

        /// <summary>Hub connection string to use</summary>
        protected readonly ConnectionString _hubConnectionString;
        /// <summary>resource id to use in derived class</summary>
        protected readonly string _resourceId;
        /// <summary>Http client to use in derived class</summary>
        protected readonly IHttpClient _httpClient;
        /// <summary>Logger to use in derived class</summary>
        protected readonly ILogger _logger;
    }
}
