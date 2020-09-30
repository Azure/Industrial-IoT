// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Client {
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Threading;

    /// <summary>
    /// Implementation of twin and job services, talking to iot hub
    /// directly. Alternatively, there is a sdk based implementation
    /// in the Hub.Client nuget package that can also be used.
    /// </summary>
    public sealed class IoTHubServiceHttpClient : IoTHubHttpClientBase,
        IIoTHubTwinServices, IHealthCheck {

        /// <summary>
        /// The host name the client is talking to
        /// </summary>
        public string HostName => HubConnectionString.HostName;

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="config"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        public IoTHubServiceHttpClient(IHttpClient httpClient,
            IIoTHubConfig config, IJsonSerializer serializer, ILogger logger) :
            base(httpClient, config, serializer, logger) {
        }

        /// <inheritdoc/>
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, CancellationToken ct) {
            try {
                await QueryAsync("SELECT * FROM devices", null, 1, ct);
                return HealthCheckResult.Healthy();
            }
            catch (Exception ex) {
                return new HealthCheckResult(context.Registration.FailureStatus,
                    exception: ex);
            }
        }

        /// <inheritdoc/>
        public Task<DeviceTwinModel> CreateOrUpdateAsync(DeviceTwinModel twin, bool force,
            CancellationToken ct) {
            if (twin == null) {
                throw new ArgumentNullException(nameof(twin));
            }
            if (string.IsNullOrEmpty(twin.Id)) {
                throw new ArgumentNullException(nameof(twin.Id));
            }
            // Retry transient errors
            return Retry.WithExponentialBackoff(_logger, ct, async () => {
                // First try create device
                try {
                    var device = NewRequest($"/devices/{twin.Id}");
                    if (string.IsNullOrEmpty(twin.ModuleId) && !string.IsNullOrEmpty(twin.DeviceScope)) {
                        _serializer.SerializeToRequest(device, new {
                            deviceId = twin.Id,
                            capabilities = twin.Capabilities,
                            deviceScope = twin.DeviceScope
                        });
                    }
                    else {
                        _serializer.SerializeToRequest(device, new {
                            deviceId = twin.Id,
                            capabilities = twin.Capabilities
                        });
                    }
                    var response = await _httpClient.PutAsync(device, ct);
                    response.Validate();
                }
                catch (ConflictingResourceException)
                    when (!string.IsNullOrEmpty(twin.ModuleId) || force) {
                    // Continue onward
                    // Update the deviceScope if the twin provided is for leaf iot device
                    //  (not iotedge device or iotedge module)
                    if (!(twin.Capabilities?.IotEdge).GetValueOrDefault(false) && 
                        string.IsNullOrEmpty(twin.ModuleId) && 
                        !string.IsNullOrEmpty(twin.DeviceScope)) {
                        try {
                            var update = NewRequest($"/devices/{twin.Id}");
                            update.Headers.Add("If-Match",
                                $"\"{(string.IsNullOrEmpty(twin.Etag) || force ? "*" : twin.Etag)}\"");
                            _serializer.SerializeToRequest(update, new {
                                deviceId = twin.Id,
                                deviceScope = twin.DeviceScope
                            });
                            var response = await _httpClient.PutAsync(update, ct);
                            // just throw if the update fails
                            response.Validate();
                        }
                        catch (ConflictingResourceException)
                            when (force) {
                            // Continue onward
                        }
                    }
                }
                if (!string.IsNullOrEmpty(twin.ModuleId)) {
                    // Try create module
                    try {
                        var module = NewRequest(
                            $"/devices/{twin.Id}/modules/{twin.ModuleId}");
                        _serializer.SerializeToRequest(module, new {
                            deviceId = twin.Id,
                            moduleId = twin.ModuleId
                        });
                        var response = await _httpClient.PutAsync(module, ct);
                        response.Validate();
                    }
                    catch (ConflictingResourceException)
                        when (force) {
                    }
                }
                return await PatchAsync(twin, true, ct);  // Force update of twin
            }, kMaxRetryCount);
        }

        /// <inheritdoc/>
        public Task<DeviceTwinModel> PatchAsync(DeviceTwinModel twin, bool force, CancellationToken ct) {
            if (twin == null) {
                throw new ArgumentNullException(nameof(twin));
            }
            if (string.IsNullOrEmpty(twin.Id)) {
                throw new ArgumentNullException(nameof(twin.Id));
            }
            return Retry.WithExponentialBackoff(_logger, ct, async () => {

                // Then update twin assuming it now exists. If fails, retry...
                var patch = NewRequest(
                    $"/twins/{ToResourceId(twin.Id, twin.ModuleId)}");
                patch.Headers.Add("If-Match",
                    $"\"{(string.IsNullOrEmpty(twin.Etag) || force ? "*" : twin.Etag)}\"");
                if (!string.IsNullOrEmpty(twin.ModuleId)) {

                    // Patch module
                    _serializer.SerializeToRequest(patch, new {
                        deviceId = twin.Id,
                        moduleId = twin.ModuleId,
                        tags = twin.Tags ?? new Dictionary<string, VariantValue>(),
                        properties = new {
                            desired = twin.Properties?.Desired ?? new Dictionary<string, VariantValue>()
                        }
                    });
                }
                else {
                    // Patch device
                    _serializer.SerializeToRequest(patch, new {
                        deviceId = twin.Id,
                        tags = twin.Tags ?? new Dictionary<string, VariantValue>(),
                        properties = new {
                            desired = twin.Properties?.Desired ?? new Dictionary<string, VariantValue>()
                        }
                    });
                }
                {
                    var response = await _httpClient.PatchAsync(patch, ct);
                    response.Validate();
                    var result = _serializer.DeserializeResponse<DeviceTwinModel>(response);
                    _logger.Information(
                        "{id} ({moduleId}) created or updated ({twinEtag} -> {resultEtag})",
                        twin.Id, twin.ModuleId ?? string.Empty, twin.Etag ?? "*", result.Etag);
                    return result;
                }
            }, kMaxRetryCount);
        }

        /// <inheritdoc/>
        public async Task<MethodResultModel> CallMethodAsync(string deviceId, string moduleId,
            MethodParameterModel parameters, CancellationToken ct) {
            if (string.IsNullOrEmpty(deviceId)) {
                throw new ArgumentNullException(nameof(deviceId));
            }
            if (parameters == null) {
                throw new ArgumentNullException(nameof(parameters));
            }
            if (string.IsNullOrEmpty(parameters.Name)) {
                throw new ArgumentNullException(nameof(parameters.Name));
            }
            var request = NewRequest(
                $"/twins/{ToResourceId(deviceId, moduleId)}/methods");

            _serializer.SerializeToRequest(request, new {
                methodName = parameters.Name,
                // TODO: Add timeouts...
                // responseTimeoutInSeconds = ...
                payload = _serializer.Parse(parameters.JsonPayload)
            });
            var response = await _httpClient.PostAsync(request, ct);
            response.Validate();
            var result = _serializer.ParseResponse(response);
            return new MethodResultModel {
                JsonPayload = _serializer.SerializeToString(result["payload"]),
                Status = (int)result["status"]
            };
        }

        /// <inheritdoc/>
        public Task UpdatePropertiesAsync(string deviceId, string moduleId,
            Dictionary<string, VariantValue> properties, string etag, CancellationToken ct) {
            if (string.IsNullOrEmpty(deviceId)) {
                throw new ArgumentNullException(nameof(deviceId));
            }
            return Retry.WithExponentialBackoff(_logger, ct, async () => {
                var request = NewRequest(
                    $"/twins/{ToResourceId(deviceId, moduleId)}");
                _serializer.SerializeToRequest(request, new {
                    deviceId,
                    properties = new {
                        desired = properties ?? new Dictionary<string, VariantValue>()
                    }
                });
                request.Headers.Add("If-Match",
                    $"\"{(string.IsNullOrEmpty(etag) ? "*" : etag)}\"");
                var response = await _httpClient.PatchAsync(request, ct);
                response.Validate();
            }, kMaxRetryCount);
        }

        /// <inheritdoc/>
        public Task ApplyConfigurationAsync(string deviceId,
            ConfigurationContentModel configuration, CancellationToken ct) {
            if (configuration == null) {
                throw new ArgumentNullException(nameof(configuration));
            }
            if (string.IsNullOrEmpty(deviceId)) {
                throw new ArgumentNullException(nameof(deviceId));
            }
            return Retry.WithExponentialBackoff(_logger, ct, async () => {
                var request = NewRequest(
                    $"/devices/{ToResourceId(deviceId, null)}/applyConfigurationContent");
                _serializer.SerializeToRequest(request, configuration);
                var response = await _httpClient.PostAsync(request, ct);
                response.Validate();
            }, kMaxRetryCount);
        }

        /// <inheritdoc/>
        public Task<DeviceTwinModel> GetAsync(string deviceId, string moduleId,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(deviceId)) {
                throw new ArgumentNullException(nameof(deviceId));
            }
            return Retry.WithExponentialBackoff(_logger, ct, async () => {
                var request = NewRequest(
                    $"/twins/{ToResourceId(deviceId, moduleId)}");
                var response = await _httpClient.GetAsync(request, ct);
                response.Validate();
                return _serializer.DeserializeResponse<DeviceTwinModel>(response);
            }, kMaxRetryCount);
        }

        /// <inheritdoc/>
        public Task<DeviceModel> GetRegistrationAsync(string deviceId, string moduleId,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(deviceId)) {
                throw new ArgumentNullException(nameof(deviceId));
            }
            return Retry.WithExponentialBackoff(_logger, ct, async () => {
                var request = NewRequest(
                    $"/devices/{ToResourceId(deviceId, moduleId)}");
                var response = await _httpClient.GetAsync(request, ct);
                response.Validate();
                return ToDeviceRegistrationModel(_serializer.ParseResponse(response));
            }, kMaxRetryCount);
        }

        /// <inheritdoc/>
        public async Task<QueryResultModel> QueryAsync(string query, string continuation,
            int? pageSize, CancellationToken ct) {
            if (string.IsNullOrEmpty(query)) {
                throw new ArgumentNullException(nameof(query));
            }
            var request = NewRequest("/devices/query");
            if (continuation != null) {
                _serializer.DeserializeContinuationToken(continuation,
                    out query, out continuation, out pageSize);
                request.Headers.Add(HttpHeader.ContinuationToken, continuation);
            }
            if (pageSize != null) {
                request.Headers.Add(HttpHeader.MaxItemCount, pageSize.ToString());
            }
            _serializer.SerializeToRequest(request, new {
                query
            });
            var response = await _httpClient.PostAsync(request, ct);
            response.Validate();
            if (response.Headers.TryGetValues(HttpHeader.ContinuationToken, out var values)) {
                continuation = _serializer.SerializeContinuationToken(
                    query, values.First(), pageSize);
            }
            else {
                continuation = null;
            }
            var results = _serializer.ParseResponse(response);
            return new QueryResultModel {
                ContinuationToken = continuation,
                Result = results.Values
            };
        }

        /// <inheritdoc/>
        public Task DeleteAsync(string deviceId, string moduleId, string etag,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(deviceId)) {
                throw new ArgumentNullException(nameof(deviceId));
            }
            etag = null; // TODO : Fix - Currently prevents internal server error
            return Retry.WithExponentialBackoff(_logger, ct, async () => {
                var request = NewRequest(
                    $"/devices/{ToResourceId(deviceId, moduleId)}");
                request.Headers.Add("If-Match",
                    $"\"{(string.IsNullOrEmpty(etag) ? "*" : etag)}\"");
                var response = await _httpClient.DeleteAsync(request, ct);
                response.Validate();
            }, kMaxRetryCount);
        }

        /// <summary>
        /// Convert json to registration
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        private static DeviceModel ToDeviceRegistrationModel(VariantValue result) {
            return new DeviceModel {
                Etag = (string)result["etag"],
                Id = (string)result["deviceId"],
                ModuleId = (string)result["moduleId"],
                ConnectionState = (string)result["connectionState"],
                Authentication = new DeviceAuthenticationModel {
                    PrimaryKey = (string)result["authentication"]["symmetricKey"]["primaryKey"],
                    SecondaryKey = (string)result["authentication"]["symmetricKey"]["secondaryKey"]
                }
            };
        }
    }
}
