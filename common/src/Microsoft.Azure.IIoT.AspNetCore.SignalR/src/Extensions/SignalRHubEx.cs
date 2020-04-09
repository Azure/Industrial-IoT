﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Extensions.DependencyInjection {
    using Microsoft.Azure.IIoT.Messaging.SignalR;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.AspNetCore.SignalR;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// SignalR hub extensions
    /// </summary>
    public static class SignalRHubEx {

        /// <summary>
        /// Map all hubs
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static void MapHubs(this IEndpointRouteBuilder endpoints, Assembly assembly = null) {
            foreach (var hub in (assembly ?? Assembly.GetCallingAssembly()).GetExportedTypes()
                .Where(t => typeof(Hub).IsAssignableFrom(t))) {
                var result = typeof(SignalRHubEx).GetMethod(nameof(MapHub)).MakeGenericMethod(hub)
                    .Invoke(null, new object[] { endpoints });
            }
        }

        /// <summary>
        /// Map hub
        /// </summary>
        /// <typeparam name="THub"></typeparam>
        /// <param name="endpoints"></param>
        /// <returns></returns>
        public static void MapHub<THub>(this IEndpointRouteBuilder endpoints)
            where THub : Hub {
            var type = typeof(THub);
            var results = type.GetCustomAttributes<RouteAttribute>(false)
                .Select(m => m.MapTo.TrimStart('/'))
                .ToList();
            if (!results.Any()) {
                results.Add(NameAttribute.GetName(type));
            }
            foreach (var map in results) {
                var builder = endpoints.MapHub<THub>("/" + map, options => {
                    // ?
                });
            }
        }
    }
}
