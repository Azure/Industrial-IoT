// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi.SignalR
{
    using System;

    /// <summary>
    /// Metadata for hub
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class MapToAttribute : Attribute
    {
        /// <summary>
        /// Create attribute
        /// </summary>
        /// <param name="route"></param>
        public MapToAttribute(string route)
        {
            Route = route;
        }

        /// <summary>
        /// Mapping
        /// </summary>
        public string Route { get; }
    }
}
