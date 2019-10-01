// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models;
    using Microsoft.Azure.IIoT.Module;
    using System;
    using System.Threading.Tasks;
    using System.Linq;
    using System.Threading;
    using Newtonsoft.Json;

    /// <summary>
    /// Implementation of supervisor module api.
    /// </summary>
    public sealed class TwinModuleClient : ITwinModuleApi {

        /// <summary>
        /// Create module client
        /// </summary>
        /// <param name="methodClient"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        public TwinModuleClient(IMethodClient methodClient, string deviceId, string moduleId) {
            _methodClient = methodClient ?? throw new ArgumentNullException(nameof(methodClient));
            _moduleId = moduleId ?? throw new ArgumentNullException(nameof(moduleId));
            _deviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
        }

        /// <summary>
        /// Create module client
        /// </summary>
        /// <param name="methodClient"></param>
        /// <param name="config"></param>
        public TwinModuleClient(IMethodClient methodClient, ITwinModuleConfig config) :
            this(methodClient, config?.DeviceId, config?.ModuleId) {
        }

        /// <inheritdoc/>
        public async Task<BrowseResponseApiModel> NodeBrowseFirstAsync(EndpointApiModel endpoint,
            BrowseRequestApiModel request, CancellationToken ct) {
            if (endpoint == null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            if (string.IsNullOrEmpty(endpoint.Url)) {
                throw new ArgumentNullException(nameof(endpoint.Url));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "Browse_V2", JsonConvertEx.SerializeObject(new {
                    endpoint,
                    request
                }), null, ct);
            return JsonConvertEx.DeserializeObject<BrowseResponseApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<BrowseNextResponseApiModel> NodeBrowseNextAsync(EndpointApiModel endpoint,
            BrowseNextRequestApiModel request, CancellationToken ct) {
            if (endpoint == null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            if (string.IsNullOrEmpty(endpoint.Url)) {
                throw new ArgumentNullException(nameof(endpoint.Url));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.ContinuationToken == null) {
                throw new ArgumentNullException(nameof(request.ContinuationToken));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "BrowseNext_V2", JsonConvertEx.SerializeObject(new {
                    endpoint,
                    request
                }), null, ct);
            return JsonConvertEx.DeserializeObject<BrowseNextResponseApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<BrowsePathResponseApiModel> NodeBrowsePathAsync(EndpointApiModel endpoint,
            BrowsePathRequestApiModel request, CancellationToken ct) {
            if (endpoint == null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            if (string.IsNullOrEmpty(endpoint.Url)) {
                throw new ArgumentNullException(nameof(endpoint.Url));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.BrowsePaths == null || request.BrowsePaths.Count == 0 ||
                request.BrowsePaths.Any(p => p == null || p.Length == 0)) {
                throw new ArgumentNullException(nameof(request.BrowsePaths));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "BrowsePath_V2", JsonConvertEx.SerializeObject(new {
                    endpoint,
                    request
                }), null, ct);
            return JsonConvertEx.DeserializeObject<BrowsePathResponseApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<ReadResponseApiModel> NodeReadAsync(EndpointApiModel endpoint,
            ReadRequestApiModel request, CancellationToken ct) {
            if (endpoint == null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            if (string.IsNullOrEmpty(endpoint.Url)) {
                throw new ArgumentNullException(nameof(endpoint.Url));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Attributes == null || request.Attributes.Count == 0) {
                throw new ArgumentException(nameof(request.Attributes));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "NodeRead_V2", JsonConvertEx.SerializeObject(new {
                    endpoint,
                    request
                }), null, ct);
            return JsonConvertEx.DeserializeObject<ReadResponseApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<WriteResponseApiModel> NodeWriteAsync(EndpointApiModel endpoint,
            WriteRequestApiModel request, CancellationToken ct) {
            if (endpoint == null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            if (string.IsNullOrEmpty(endpoint.Url)) {
                throw new ArgumentNullException(nameof(endpoint.Url));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Attributes == null || request.Attributes.Count == 0) {
                throw new ArgumentException(nameof(request.Attributes));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "NodeWrite_V2", JsonConvertEx.SerializeObject(new {
                    endpoint,
                    request
                }), null, ct);
            return JsonConvertEx.DeserializeObject<WriteResponseApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<ValueReadResponseApiModel> NodeValueReadAsync(EndpointApiModel endpoint,
            ValueReadRequestApiModel request, CancellationToken ct) {
            if (endpoint == null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            if (string.IsNullOrEmpty(endpoint.Url)) {
                throw new ArgumentNullException(nameof(endpoint.Url));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "ValueRead_V2", JsonConvertEx.SerializeObject(new {
                    endpoint,
                    request
                }), null, ct);
            return JsonConvertEx.DeserializeObject<ValueReadResponseApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<ValueWriteResponseApiModel> NodeValueWriteAsync(EndpointApiModel endpoint,
            ValueWriteRequestApiModel request, CancellationToken ct) {
            if (endpoint == null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            if (string.IsNullOrEmpty(endpoint.Url)) {
                throw new ArgumentNullException(nameof(endpoint.Url));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Value == null) {
                throw new ArgumentNullException(nameof(request.Value));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "ValueWrite_V2", JsonConvertEx.SerializeObject(new {
                    endpoint,
                    request
                }), null, ct);
            return JsonConvertEx.DeserializeObject<ValueWriteResponseApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<MethodMetadataResponseApiModel> NodeMethodGetMetadataAsync(
            EndpointApiModel endpoint, MethodMetadataRequestApiModel request, CancellationToken ct) {
            if (endpoint == null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            if (string.IsNullOrEmpty(endpoint.Url)) {
                throw new ArgumentNullException(nameof(endpoint.Url));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "MethodMetadata_V2", JsonConvertEx.SerializeObject(new {
                    endpoint,
                    request
                }), null, ct);
            return JsonConvertEx.DeserializeObject<MethodMetadataResponseApiModel>(response);
        }

        /// <inheritdoc/>
        public async Task<MethodCallResponseApiModel> NodeMethodCallAsync(
            EndpointApiModel endpoint, MethodCallRequestApiModel request, CancellationToken ct) {
            if (endpoint == null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            if (string.IsNullOrEmpty(endpoint.Url)) {
                throw new ArgumentNullException(nameof(endpoint.Url));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var response = await _methodClient.CallMethodAsync(_deviceId, _moduleId,
                "MethodCall_V2", JsonConvertEx.SerializeObject(new {
                    endpoint,
                    request
                }), null, ct);
            return JsonConvertEx.DeserializeObject<MethodCallResponseApiModel>(response);
        }

        private readonly IMethodClient _methodClient;
        private readonly string _moduleId;
        private readonly string _deviceId;
    }
}
