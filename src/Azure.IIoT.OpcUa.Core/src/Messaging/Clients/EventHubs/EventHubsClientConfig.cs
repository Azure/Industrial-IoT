// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.EventHubs
{
    using Azure.IIoT.OpcUa.Core.Configuration;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Event Hubs client configuration.
    /// </summary>
    public sealed class EventHubsClientConfig : PostConfigureOptionBase<EventHubsClientOptions>
    {
        /// <summary>
        /// Create configuration.
        /// </summary>
        /// <param name="configuration"></param>
        public EventHubsClientConfig(IConfiguration configuration) :
            base(configuration)
        {
        }

        /// <inheritdoc/>
        public override void PostConfigure(string? name, EventHubsClientOptions options)
        {
            if (string.IsNullOrEmpty(options.ConnectionString))
            {
                options.ConnectionString = GetStringOrDefault(kConnectionString,
                    GetStringOrDefault(kConnectionStringShort, string.Empty));
            }
        }

        private const string kConnectionString = "PCS_EVENTHUB_CONNECTIONSTRING";
        private const string kConnectionStringShort = "_EH_CS";
    }
}
