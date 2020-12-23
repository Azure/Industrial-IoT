// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Exceptions {
    using Microsoft.Azure.IIoT.Exceptions;

    /// <summary>
    /// Monitored item was already added
    /// </summary>
    public class MonitoredItemAlreadyAddedException : ConflictingResourceException {

        /// <inheritdoc/>
        public MonitoredItemAlreadyAddedException() {
        }

        /// <inheritdoc/>
        public MonitoredItemAlreadyAddedException(string monitoredItemId, string subscriptionName) :
            base($"A monitored item with id '{monitoredItemId}' is already part" +
                $" of subscription with name '{subscriptionName}'.") {
        }
    }
}