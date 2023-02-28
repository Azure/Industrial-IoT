// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Client
{
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Abstractions.Serializers.Extensions;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Microsoft.Extensions.Logging;
    using Furly.Extensions.Serializers;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implementation of twin and job services, talking to iot hub
    /// directly. Alternatively, there is a sdk based implementation
    /// in the Hub.Client nuget package that can also be used.
    /// </summary>
    public sealed class IoTHubServiceHttpClient : IoTHubHttpClientBase,
        IIoTHubTwinServices, IHealthCheck
    {
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
            base(httpClient, config, serializer, logger)
        {
        }

        /// <inheritdoc/>
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, CancellationToken cancellationToken)
        {
            try
            {
                await QueryAsync("SELECT * FROM devices", null, 1, cancellationToken).ConfigureAwait(false);
                return HealthCheckResult.Healthy();
            }
            catch (Exception ex)
            {
                return new HealthCheckResult(context.Registration.FailureStatus,
                    exception: ex);
            }
        }

        /// <inheritdoc/>
        public Task<DeviceTwinModel> CreateOrUpdateAsync(DeviceTwinModel device, bool force,
            CancellationToken ct)
        {
            if (device == null)
            {
                throw new ArgumentNullException(nameof(device));
            }
            if (string.IsNullOrEmpty(device.Id))
            {
                throw new ArgumentException("Id missing.", nameof(device));
            }
            // Retry transient errors
            return Retry2.WithExponentialBackoffAsync(_logger, async () =>
            {
                // First try create device
                try
                {
                    var request = NewRequest($"/devices/{device.Id}");
                    if (string.IsNullOrEmpty(device.ModuleId) && !string.IsNullOrEmpty(device.DeviceScope))
                    {
                        _serializer.SerializeToRequest(request, new
                        {
                            deviceId = device.Id,
                            capabilities = device.Capabilities,
                            deviceScope = device.DeviceScope
                        });
                    }
                    else
                    {
                        _serializer.SerializeToRequest(request, new
                        {
                            deviceId = device.Id,
                            capabilities = device.Capabilities
                        });
                    }
                    var response = await _httpClient.PutAsync(request, ct).ConfigureAwait(false);
                    response.Validate();
                }
                catch (ConflictingResourceException)
                    when (!string.IsNullOrEmpty(device.ModuleId) || force)
                {
                    // Continue onward
                    // Update the deviceScope if the twin provided is for leaf iot device
                    //  (not iotedge device or iotedge module)
                    if (!((device.Capabilities?.IotEdge) ?? false) &&
                        string.IsNullOrEmpty(device.ModuleId) &&
                        !string.IsNullOrEmpty(device.DeviceScope))
                    {
                        try
                        {
                            var update = NewRequest($"/devices/{device.Id}");
                            update.Headers.Add("If-Match",
                                $"\"{(string.IsNullOrEmpty(device.Etag) || force ? "*" : device.Etag)}\"");
                            _serializer.SerializeToRequest(update, new
                            {
                                deviceId = device.Id,
                                deviceScope = device.DeviceScope
                            });
                            var response = await _httpClient.PutAsync(update, ct).ConfigureAwait(false);
                            // just throw if the update fails
                            response.Validate();
                        }
                        catch (ConflictingResourceException)
                            when (force)
                        {
                            // Continue onward
                        }
                    }
                }
                if (!string.IsNullOrEmpty(device.ModuleId))
                {
                    // Try create module
                    try
                    {
                        var module = NewRequest(
                            $"/devices/{device.Id}/modules/{device.ModuleId}");
                        _serializer.SerializeToRequest(module, new
                        {
                            deviceId = device.Id,
                            moduleId = device.ModuleId
                        });
                        var response = await _httpClient.PutAsync(module, ct).ConfigureAwait(false);
                        response.Validate();
                    }
                    catch (ConflictingResourceException)
                        when (force)
                    {
                    }
                }
                return await PatchAsync(device, true, ct).ConfigureAwait(false);  // Force update of twin
            }, ct, kMaxRetryCount);
        }

        /// <inheritdoc/>
        public Task<DeviceTwinModel> PatchAsync(DeviceTwinModel device, bool force, CancellationToken ct)
        {
            if (device == null)
            {
                throw new ArgumentNullException(nameof(device));
            }
            if (string.IsNullOrEmpty(device.Id))
            {
                throw new ArgumentException("Id missing", nameof(device));
            }
            return Retry2.WithExponentialBackoffAsync(_logger, async () =>
            {
                // Then update twin assuming it now exists. If fails, retry...
                var patch = NewRequest(
                    $"/twins/{ToResourceId(device.Id, device.ModuleId)}");
                patch.Headers.Add("If-Match",
                    $"\"{(string.IsNullOrEmpty(device.Etag) || force ? "*" : device.Etag)}\"");
                if (!string.IsNullOrEmpty(device.ModuleId))
                {
                    // Patch module
                    _serializer.SerializeToRequest(patch, new
                    {
                        deviceId = device.Id,
                        moduleId = device.ModuleId,
                        tags = device.Tags ?? new Dictionary<string, VariantValue>(),
                        properties = new
                        {
                            desired = device.Properties?.Desired ?? new Dictionary<string, VariantValue>()
                        }
                    });
                }
                else
                {
                    // Patch device
                    _serializer.SerializeToRequest(patch, new
                    {
                        deviceId = device.Id,
                        tags = device.Tags ?? new Dictionary<string, VariantValue>(),
                        properties = new
                        {
                            desired = device.Properties?.Desired ?? new Dictionary<string, VariantValue>()
                        }
                    });
                }
                {
                    var response = await _httpClient.PatchAsync(patch, ct).ConfigureAwait(false);
                    response.Validate();
                    var result = _serializer.DeserializeResponse<DeviceTwinModel>(response);
                    _logger.LogInformation(
                        "{Id} ({ModuleId}) created or updated ({TwinEtag} -> {ResultEtag})",
                        device.Id, device.ModuleId ?? string.Empty, device.Etag ?? "*", result.Etag);
                    return result;
                }
            }, ct, kMaxRetryCount);
        }

        /// <inheritdoc/>
        public async Task<MethodResultModel> CallMethodAsync(string deviceId, string moduleId,
            MethodParameterModel parameters, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(deviceId))
            {
                throw new ArgumentNullException(nameof(deviceId));
            }
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }
            if (string.IsNullOrEmpty(parameters.Name))
            {
                throw new ArgumentException("Name missing.", nameof(parameters));
            }
            var request = NewRequest(
                $"/twins/{ToResourceId(deviceId, moduleId)}/methods");

            _serializer.SerializeToRequest(request, new
            {
                methodName = parameters.Name,
                // TODO: Add timeouts...
                // responseTimeoutInSeconds = ...
                payload = _serializer.Parse(parameters.JsonPayload)
            });
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            var result = _serializer.ParseResponse(response);
            return new MethodResultModel
            {
                JsonPayload = _serializer.SerializeToString(result["payload"]),
                Status = (int)result["status"]
            };
        }

        /// <inheritdoc/>
        public Task UpdatePropertiesAsync(string deviceId, string moduleId,
            Dictionary<string, VariantValue> properties, string etag, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(deviceId))
            {
                throw new ArgumentNullException(nameof(deviceId));
            }
            return Retry2.WithExponentialBackoffAsync(_logger, async () =>
            {
                var request = NewRequest(
                    $"/twins/{ToResourceId(deviceId, moduleId)}");
                _serializer.SerializeToRequest(request, new
                {
                    deviceId,
                    properties = new
                    {
                        desired = properties ?? new Dictionary<string, VariantValue>()
                    }
                });
                request.Headers.Add("If-Match",
                    $"\"{(string.IsNullOrEmpty(etag) ? "*" : etag)}\"");
                var response = await _httpClient.PatchAsync(request, ct).ConfigureAwait(false);
                response.Validate();
            }, ct, kMaxRetryCount);
        }

        /// <inheritdoc/>
        public Task<DeviceTwinModel> GetAsync(string deviceId, string moduleId,
            CancellationToken ct)
        {
            if (string.IsNullOrEmpty(deviceId))
            {
                throw new ArgumentNullException(nameof(deviceId));
            }
            return Retry2.WithExponentialBackoffAsync(_logger, async () =>
            {
                var request = NewRequest(
                    $"/twins/{ToResourceId(deviceId, moduleId)}");
                var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
                response.Validate();
                return _serializer.DeserializeResponse<DeviceTwinModel>(response);
            }, ct, kMaxRetryCount);
        }

        /// <inheritdoc/>
        public Task<DeviceModel> GetRegistrationAsync(string deviceId, string moduleId,
            CancellationToken ct)
        {
            if (string.IsNullOrEmpty(deviceId))
            {
                throw new ArgumentNullException(nameof(deviceId));
            }
            return Retry2.WithExponentialBackoffAsync(_logger, async () =>
            {
                var request = NewRequest(
                    $"/devices/{ToResourceId(deviceId, moduleId)}");
                var response = await _httpClient.GetAsync(request, ct).ConfigureAwait(false);
                response.Validate();
                return ToDeviceRegistrationModel(_serializer.ParseResponse(response));
            }, ct, kMaxRetryCount);
        }

        /// <inheritdoc/>
        public async Task<QueryResultModel> QueryAsync(string query, string continuation,
            int? pageSize, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(query))
            {
                throw new ArgumentNullException(nameof(query));
            }
            var request = NewRequest("/devices/query");
            if (continuation != null)
            {
                _serializer.DeserializeContinuationToken(continuation,
                    out query, out continuation, out pageSize);
                request.Headers.Add(HttpHeader.ContinuationToken, continuation);
            }
            if (pageSize != null)
            {
                request.Headers.Add(HttpHeader.MaxItemCount, pageSize.ToString());
            }
            _serializer.SerializeToRequest(request, new
            {
                query
            });
            var response = await _httpClient.PostAsync(request, ct).ConfigureAwait(false);
            response.Validate();
            if (response.Headers.TryGetValues(HttpHeader.ContinuationToken, out var values))
            {
                continuation = _serializer.SerializeContinuationToken(
                    query, values.First(), pageSize);
            }
            else
            {
                continuation = null;
            }
            var results = _serializer.ParseResponse(response);
            return new QueryResultModel
            {
                ContinuationToken = continuation,
                Result = results.Values
            };
        }

        /// <inheritdoc/>
        public Task DeleteAsync(string deviceId, string moduleId, string etag,
            CancellationToken ct)
        {
            if (string.IsNullOrEmpty(deviceId))
            {
                throw new ArgumentNullException(nameof(deviceId));
            }
            etag = null; // TODO : Fix - Currently prevents internal server error
            return Retry2.WithExponentialBackoffAsync(_logger, async () =>
            {
                var request = NewRequest(
                    $"/devices/{ToResourceId(deviceId, moduleId)}");
                request.Headers.Add("If-Match",
                    $"\"{(string.IsNullOrEmpty(etag) ? "*" : etag)}\"");
                var response = await _httpClient.DeleteAsync(request, ct).ConfigureAwait(false);
                response.Validate();
            }, ct, kMaxRetryCount);
        }

        /// <summary>
        /// Convert json to registration
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        private static DeviceModel ToDeviceRegistrationModel(VariantValue result)
        {
            return new DeviceModel
            {
                Etag = (string)result["etag"],
                Id = (string)result["deviceId"],
                ModuleId = (string)result["moduleId"],
                ConnectionState = (string)result["connectionState"],
                Authentication = new DeviceAuthenticationModel
                {
                    PrimaryKey = (string)result["authentication"]["symmetricKey"]["primaryKey"],
                    SecondaryKey = (string)result["authentication"]["symmetricKey"]["secondaryKey"]
                }
            };
        }
    }
}
