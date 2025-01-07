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
    /// <remarks>
    /// Create attribute
    /// </remarks>
    /// <param name="route"></param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class MapToAttribute(string route) : Attribute
    {
        /// <summary>
        /// Mapping
        /// </summary>
        public string Route { get; } = route;
    }
}
