// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

// #define USE_TASK_RUN

namespace Opc.Ua.Client {
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Discovery Client async extensions
    /// </summary>
    public static class DiscoveryClientEx {

        /// <summary>
        /// Async find servers service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="requestHeader"></param>
        /// <param name="endpointUrl"></param>
        /// <param name="localeIds"></param>
        /// <param name="serverUris"></param>
        /// <returns></returns>
        public static Task<FindServersResponse> FindServersAsync(this DiscoveryClient client,
            RequestHeader requestHeader, string endpointUrl, StringCollection localeIds,
            StringCollection serverUris) {
#if !USE_TASK_RUN
            return Task.Factory.FromAsync(
                (callback, state) => client.BeginFindServers(requestHeader, endpointUrl,
                    localeIds, serverUris, callback, state),
                result => {
                    var response = client.EndFindServers(result, out var results);
                    return NewFindServersResponse(response, results);
                }, TaskCreationOptions.DenyChildAttach);
#else
            return Task.Run(() => {
                var response = client.FindServers(requestHeader, endpointUrl,
                    localeIds, serverUris, out var results);
                return NewFindServersResponse(response, results);
            });
#endif
        }

        /// <summary>
        /// Async find servers on network service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="requestHeader"></param>
        /// <param name="startingRecordId"></param>
        /// <param name="maxRecordsToReturn"></param>
        /// <param name="serverCapabilityFilter"></param>
        /// <returns></returns>
        public static Task<FindServersOnNetworkResponse> FindServersOnNetworkAsync(
            this DiscoveryClient client, RequestHeader requestHeader,
            uint startingRecordId, uint maxRecordsToReturn,
            StringCollection serverCapabilityFilter) {
#if !USE_TASK_RUN
            return Task.Factory.FromAsync(
                (callback, state) => client.BeginFindServersOnNetwork(requestHeader,
                    startingRecordId, maxRecordsToReturn, serverCapabilityFilter,
                    callback, state),
                result => {
                    var response = client.EndFindServersOnNetwork(result,
                        out var lastCounterResetTime, out var servers);
                    return NewFindServersOnNetworkResponse(response,
                        lastCounterResetTime, servers);
                }, TaskCreationOptions.DenyChildAttach);
#else
            return Task.Run(() => {
                var response = client.FindServersOnNetwork(requestHeader, startingRecordId,
                    maxRecordsToReturn, serverCapabilityFilter,
                    out var lastCounterResetTime, out var servers);
                return NewFindServersOnNetworkResponse(response,
                    lastCounterResetTime, servers);
            });
#endif
        }

        /// <summary>
        /// Async get endpoints service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="requestHeader"></param>
        /// <param name="endpointUrl"></param>
        /// <param name="localeIds"></param>
        /// <param name="profileUris"></param>
        /// <returns></returns>
        public static Task<GetEndpointsResponse> GetEndpointsAsync(
            this DiscoveryClient client, RequestHeader requestHeader,
            string endpointUrl, StringCollection localeIds, StringCollection profileUris) {
#if !USE_TASK_RUN
            return Task.Factory.FromAsync(
                (callback, state) => client.BeginGetEndpoints(requestHeader,
                    endpointUrl, localeIds, profileUris, callback, state),
                result => {
                    var response = client.EndGetEndpoints(result, out var endpoints);
                    return NewGetEndpointsResponse(response, endpoints);
                }, TaskCreationOptions.DenyChildAttach);
#else
            return Task.Run(() => {
                var response = client.GetEndpoints(requestHeader, endpointUrl, localeIds,
                    profileUris, out var endpoints);
                return NewGetEndpointsResponse(response, endpoints);
            });
#endif
        }

        /// <summary>
        /// Get endpoints response constructor
        /// </summary>
        /// <param name="response"></param>
        /// <param name="endpoints"></param>
        /// <returns></returns>
        private static GetEndpointsResponse NewGetEndpointsResponse(ResponseHeader response,
            EndpointDescriptionCollection endpoints) {
            return new GetEndpointsResponse {
                Endpoints = endpoints,
                ResponseHeader = response
            };
        }

        /// <summary>
        /// Find servers on network response constructor
        /// </summary>
        /// <param name="response"></param>
        /// <param name="lastCounterResetTime"></param>
        /// <param name="servers"></param>
        /// <returns></returns>
        private static FindServersOnNetworkResponse NewFindServersOnNetworkResponse(
            ResponseHeader response, DateTime lastCounterResetTime,
            ServerOnNetworkCollection servers) {
            return new FindServersOnNetworkResponse {
                ResponseHeader = response,
                Servers = servers,
                LastCounterResetTime = lastCounterResetTime
            };
        }

        /// <summary>
        /// Find servers response constructor
        /// </summary>
        /// <param name="response"></param>
        /// <param name="results"></param>
        /// <returns></returns>
        private static FindServersResponse NewFindServersResponse(ResponseHeader response,
            ApplicationDescriptionCollection results) {
            return new FindServersResponse {
                ResponseHeader = response,
                Servers = results
            };
        }
    }
}
