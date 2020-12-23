// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Services {
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using Serilog;
    using System.Collections.Generic;

    /// <summary>
    /// Twin registry change events
    /// </summary>
    public sealed class IoTHubTwinChangeEventHandler : IoTHubDeviceTwinChangeHandlerBase {

        /// <inheritdoc/>
        public override string MessageSchema => MessageSchemaTypes.TwinChangeNotification;

        /// <summary>
        /// Create handler
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="handlers"></param>
        /// <param name="logger"></param>
        public IoTHubTwinChangeEventHandler(IJsonSerializer serializer,
            IEnumerable<IIoTHubDeviceTwinEventHandler> handlers, ILogger logger)
            : base (serializer, handlers, logger) {
        }

        /// <summary>
        /// Get operation
        /// </summary>
        /// <param name="opType"></param>
        /// <returns></returns>
        protected override DeviceTwinEventType? GetOperation(string opType) {
            switch (opType) {
                case "replaceTwin":
                    return DeviceTwinEventType.Create;
                case "updateTwin":
                    return DeviceTwinEventType.Update;
            }
            return null;
        }
    }
}
