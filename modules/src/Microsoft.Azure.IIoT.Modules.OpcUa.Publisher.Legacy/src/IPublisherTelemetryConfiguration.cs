// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher
{

    public interface IPublisherTelemetryConfiguration
    {
        /// <summary>
        /// Method to get the telemetry configuration for a specific endpoint URL. If the endpoint URL is not found, then the default configuration is returned.
        /// </summary>
        EndpointTelemetryConfigurationModel GetEndpointTelemetryConfiguration(string endpointUrl);

        /// <summary>
        /// Update the default configuration with the settings give in the 'Defaults' object of the configuration file.
        /// </summary>
        bool UpdateDefaultEndpointTelemetryConfiguration();

        /// <summary>
        /// Update the endpoint specific telemetry configuration using settings from the default configuration.
        /// Only those settings are applied, which are not defined by the endpoint specific configuration.
        /// </summary>
        void UpdateEndpointTelemetryConfiguration(EndpointTelemetryConfigurationModel config);
    }
}
