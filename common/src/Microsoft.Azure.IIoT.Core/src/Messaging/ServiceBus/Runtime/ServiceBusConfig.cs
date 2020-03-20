// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging.ServiceBus.Runtime {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// ServiceBus configuration
    /// </summary>
    public class ServiceBusConfig : ConfigBase, IServiceBusConfig {

        private const string kServiceBusConnectionString = "ServiceBus:ConnectionString";

        /// <inheritdoc/>
        public string ServiceBusConnString => GetStringOrDefault(kServiceBusConnectionString,
            () => GetStringOrDefault(PcsVariable.PCS_SERVICEBUS_CONNSTRING,
                () => GetStringOrDefault("_SB_CS", () => null)));

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public ServiceBusConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
