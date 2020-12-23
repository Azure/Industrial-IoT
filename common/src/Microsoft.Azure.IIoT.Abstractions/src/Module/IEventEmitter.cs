// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module {
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.IIoT.Serializers;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Send telemetry events and property update events as
    /// module or device identity
    /// </summary>
    public interface IEventEmitter : IIdentity, IEventClient {

        /// <summary>
        /// Send property changed notification
        /// </summary>
        /// <param name="propertyId">property id</param>
        /// <param name="value">property value</param>
        /// <returns></returns>
        Task ReportAsync(string propertyId, VariantValue value);

        /// <summary>
        /// Send property changed notifications
        /// </summary>
        /// <param name="properties">property id</param>
        /// <returns></returns>
        Task ReportAsync(IEnumerable<KeyValuePair<string,
            VariantValue>> properties);
    }
}
