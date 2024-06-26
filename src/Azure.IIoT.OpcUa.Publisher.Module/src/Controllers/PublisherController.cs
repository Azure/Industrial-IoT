// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Controllers
{
    using Azure.IIoT.OpcUa.Publisher.Module.Filters;
    using Furly.Tunnel.Router;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher controller
    /// </summary>
    [Version("_V2")]
    [Version("")]
    [RouterExceptionFilter]
    public class PublisherController : IMethodController
    {
        /// <summary>
        /// Support restarting the module
        /// </summary>
        /// <param name="process"></param>
        /// <param name="apikey"></param>
        /// <param name="certificate"></param>
        public PublisherController(IProcessControl process, IApiKeyProvider apikey,
            ISslCertProvider certificate)
        {
            _apikey = apikey;
            _certificate = certificate;
            _process = process;
        }

        /// <summary>
        /// Get ApiKey to use when calling the HTTP API.
        /// </summary>
        /// <returns></returns>
        public Task<string?> GetApiKeyAsync()
        {
            return Task.FromResult(_apikey.ApiKey);
        }

        /// <summary>
        /// Get server certificate as PEM.
        /// </summary>
        /// <returns></returns>
        public Task<string?> GetServerCertificateAsync()
        {
            return Task.FromResult(_certificate.Certificate?.ExportCertificatePem());
        }

        /// <summary>
        /// Shutdown
        /// </summary>
        /// <param name="failFast"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public async Task ShutdownAsync(bool failFast = false)
        {
            if (!_process.Shutdown(failFast))
            {
                // Should be gone now
                await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                throw new NotSupportedException("Failed to invoke shutdown");
            }
        }

        /// <summary>
        /// Legacy shutdown
        /// </summary>
        /// <returns></returns>
        public Task ExitApplicationAsync()
        {
            return ShutdownAsync();
        }

        private readonly IApiKeyProvider _apikey;
        private readonly ISslCertProvider _certificate;
        private readonly IProcessControl _process;
    }
}
