// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client {

    /// <summary>
    /// Module configuration
    /// </summary>
    public interface IModuleConfig {

        /// <summary>
        /// IoTEdgeHub connection string
        /// </summary>
        string EdgeHubConnectionString { get; }

        /// <summary>
        /// MqttClient connection string
        /// </summary>
        string MqttClientConnectionString { get; }

        /// <summary>
        /// Gets or sets the optional telemetry topic template. 
        /// If no template is provided default IoT Hub topics are used.
        /// </summary>
        string TelemetryTopicTemplate { get; }

        /// <summary>
        /// Bypass cert validation with hub
        /// </summary>
        bool BypassCertVerification { get; }

        /// <summary>
        /// Enable metrics collection
        /// </summary>
        bool EnableMetrics { get; }

        /// <summary>
        /// Transports to use
        /// </summary>
        TransportOption Transport { get; }
    }
}
