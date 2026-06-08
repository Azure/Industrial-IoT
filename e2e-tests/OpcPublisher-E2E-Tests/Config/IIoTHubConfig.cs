// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisherAEE2ETests.Config
{
    /// <summary>
    /// IoT Hub configuration
    /// </summary>
    public interface IIoTHubConfig
    {
        /// <summary>
        /// IoT Hub connection string. Still used by IoT Hub service operations (registry
        /// and service client) until the v2 IoT Hub SDK GA's and we can migrate to
        /// IotHubServiceClient(hostname, TokenCredential). See plan Phase 1.3.
        /// </summary>
        string IoTHubConnectionString { get; }

        /// <summary>
        /// Fully-qualified namespace of the IoT Hub's built-in Event Hub-compatible endpoint
        /// (e.g. "iothub-ns-myhub-12345-abc123.servicebus.windows.net"). When provided, the
        /// test process authenticates to this endpoint via AAD (TokenCredential) instead of
        /// a shared access key.
        /// </summary>
        string IoTHubEventHubFullyQualifiedNamespace { get; }

        /// <summary>
        /// Event Hub "entity path" for the IoT Hub's built-in endpoint. Equals the IoT Hub
        /// name. When IoTHubEventHubFullyQualifiedNamespace is also provided, the test
        /// process uses these two values + a TokenCredential to construct the Event Hub
        /// consumer client (AAD auth, no shared key).
        /// </summary>
        string IoTHubEventHubName { get; }
    }
}
