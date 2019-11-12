// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Publisher.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Publisher processing engine configuration
    /// </summary>
    public class PublisherJobConfigApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public PublisherJobConfigApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public PublisherJobConfigApiModel(EngineConfigurationModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            BatchSize = model.BatchSize;
            DiagnosticsInterval = model.DiagnosticsInterval;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public EngineConfigurationModel ToServiceModel() {
            return new EngineConfigurationModel {
                BatchSize = BatchSize,
                DiagnosticsInterval = DiagnosticsInterval
            };
        }

        /// <summary>
        /// Buffer size
        /// </summary>
        [JsonProperty(PropertyName = "batchSize",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? BatchSize { get; set; }

        /// <summary>
        /// Interval for diagnostic messages
        /// </summary>
        [JsonProperty(PropertyName = "diagnosticsInterval",
            NullValueHandling = NullValueHandling.Ignore)]
        public TimeSpan? DiagnosticsInterval { get; set; }
    }
}