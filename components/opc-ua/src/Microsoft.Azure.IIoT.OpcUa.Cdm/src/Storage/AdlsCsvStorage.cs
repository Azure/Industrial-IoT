// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Cdm.Storage {
    using Microsoft.Azure.IIoT.Cdm;
    using Microsoft.Azure.IIoT.Http;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <inheritdoc/>
    public class AdlsCsvStorage : IAdlsStorage {
        /// <summary>
        /// CDM Azure Data lake storage handler
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="httpClient"></param>
        public AdlsCsvStorage(ILogger logger, IHttpClient httpClient) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>
        /// prepare a csv formated block
        /// </summary>
        /// <param name="data"></param>
        /// <param name="separator"></param>
        /// <param name="addHeader"></param>
        /// <returns></returns>
        private string BuildCsvData<T>(List<T> data,
            string separator, bool addHeader = false) {
            var sb = new StringBuilder();
            var info = typeof(T).GetProperties();
            if (addHeader) {
                foreach (var prop in info) {
                    sb.Append(prop.Name);
                    sb.Append(separator);
                }
                sb.Remove(sb.Length - 1, 1);
            }
            foreach (var obj in data) {
                sb.AppendLine();
                foreach (var prop in info) {
                    var str = prop.GetValue(obj, null)?.ToString();
                    if (str != null &&
                        (str.Contains(separator) ||
                        str.Contains("\"") || str.Contains("\r") ||
                        str.Contains("\n"))) {
                        sb.Append('\"');
                        foreach (var nextChar in str) {
                            sb.Append(nextChar);
                            if (nextChar == '"') {
                                sb.Append('\"');
                            }
                        }
                        sb.Append('\"');
                    }
                    else {
                        sb.Append(str);
                    }
                    sb.Append(separator);
                }
                sb.Remove(sb.Length - 1, 1);
            }
            return sb.ToString();
        }

        /// <summary>
        /// performs
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="partitionUrl"></param>
        /// <param name="data"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public async Task WriteInCsvPartition<T>(string partitionUrl,
            List<T> data, string separator) {
            try {
                await _lock.WaitAsync();
                // check if partition exists
                long contentPosition = 0;
                var content = string.Empty;
                var request = _httpClient.NewRequest($"{partitionUrl}", kResource);
                var response = await _httpClient.HeadAsync(request);
                if (response.IsError() ||
                    0 == (contentPosition = response.ContentHeaders.ContentLength.Value)) {
                    // create a new file
                    request = _httpClient.NewRequest($"{partitionUrl}?resource=file",
                        kResource);
                    response = await _httpClient.PutAsync(request);
                    content = BuildCsvData(data, separator, true);
                }
                else {
                    content = BuildCsvData(data, separator, false);
                }
                // append the the content to the partition
                if (content != string.Empty) {
                    request = _httpClient.NewRequest(
                        $"{partitionUrl}?action=append&position={contentPosition}",
                        kResource);
                    request.SetContent(content);
                    response = await _httpClient.PatchAsync(request);
                    contentPosition += content.Length;
                    request = _httpClient.NewRequest
                        ($"{partitionUrl}?action=flush&position={contentPosition}",
                        kResource);
                    response = await _httpClient.PatchAsync(request);
                }
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to write data in the csv partition");
                throw ex;
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="hostName"></param>
        /// <param name="blobName"></param>
        /// <param name="rootFolder"></param>
        /// <returns></returns>
        public async Task CreateBlobRoot(string hostName, string blobName, string rootFolder) {
            var url = $"https://{hostName}";
            var request = _httpClient.NewRequest($"{url}/{blobName}?resource=filesystem", kResource);
            var response = await _httpClient.HeadAsync(request);
            if (response.IsError()) {
                request = _httpClient.NewRequest($"{url}/{blobName}?resource=filesystem", kResource);
                response = await _httpClient.PutAsync(request);
                if (response.IsError()) {
                    // failed to create or get the filesystem, give up
                    return;
                }
                request = _httpClient.NewRequest($"{url}/{blobName}/{rootFolder}?resource=directory", kResource);
                response = await _httpClient.PutAsync(request);
                if (response.IsError()) {
                    // failed to create or get the root folder, give up
                    return;
                }
            }
            else {
                request = _httpClient.NewRequest($"{url}/{blobName}/{rootFolder}", kResource);
                response = await _httpClient.HeadAsync(request);
                if (response.IsError()) {
                    request = _httpClient.NewRequest($"{url}/{blobName}/{rootFolder}?resource=directory", kResource);
                    response = await _httpClient.PutAsync(request);
                    if (response.IsError()) {
                        // failed to create or get the root folder, give up
                        return;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose() {
            _lock.Dispose();
        }

        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly string kResource = "https://storage.azure.com";
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
    }
}
