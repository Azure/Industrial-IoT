﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Log level
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TraceLogLevel {

        /// <summary>
        /// Error only
        /// </summary>
        Error = 0,

        /// <summary>
        /// Default
        /// </summary>
        Information = 1,

        /// <summary>
        /// Debug log
        /// </summary>
        Debug = 2,

        /// <summary>
        /// Verbose
        /// </summary>
        Verbose = 3
    }
}
