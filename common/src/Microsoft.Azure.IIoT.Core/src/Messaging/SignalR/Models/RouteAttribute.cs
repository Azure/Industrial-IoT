// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging.SignalR {
    using System;

    /// <summary>
    /// Metadata for hub
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class RouteAttribute : Attribute {

        /// <summary>
        /// Create attribute
        /// </summary>
        /// <param name="mapTo"></param>
        public RouteAttribute(string mapTo) {
            MapTo = mapTo;
        }

        /// <summary>
        /// Mapping
        /// </summary>
        public string MapTo { get; set; }
    }
}
