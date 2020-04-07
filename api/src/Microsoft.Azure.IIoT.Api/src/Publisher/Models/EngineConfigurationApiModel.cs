// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System.Runtime.Serialization;
    using System;

    /// <summary>
    /// Publisher processing engine configuration
    /// </summary>
    [DataContract]
    public class EngineConfigurationApiModel {

        /// <summary>
        /// Buffer size
        /// </summary>
        [DataMember(Name = "batchSize", Order = 0,
            EmitDefaultValue = false)]
        public int? BatchSize { get; set; }

        /// <summary>
        /// Interval for diagnostic messages
        /// </summary>
        [DataMember(Name = "diagnosticsInterval", Order = 1,
            EmitDefaultValue = false)]
        public TimeSpan? DiagnosticsInterval { get; set; }
    }
}