// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Utils {
    using Microsoft.Azure.IIoT.Hub.Client;

    /// <summary>
    /// Connection string extensions
    /// </summary>
    public static class ConnectionStringEx {

        /// <summary>
        /// IoTHubOwner connection string to configuration
        /// </summary>
        /// <param name="cs"></param>
        /// <param name="resourceId"></param>
        /// <returns></returns>
        public static IIoTHubConfig ToIoTHubConfig(this ConnectionString cs,
            string resourceId = null) {
            return ToIoTHubConfig(cs.ToString(), resourceId);
        }

        /// <summary>
        /// IoTHubOwner connection string to configuration
        /// </summary>
        /// <param name="cs"></param>
        /// <param name="resourceId"></param>
        /// <returns></returns>
        public static IIoTHubConfig ToIoTHubConfig(this string cs,
            string resourceId = null) {
            return new IoTHubConfig {
                IoTHubConnString = cs,
                IoTHubResourceId = resourceId
            };
        }

        /// <summary>
        /// Helper class to wrap connection string
        /// </summary>
        private class IoTHubConfig : IIoTHubConfig {

            /// <inheritdoc/>
            public string IoTHubConnString { get; set; }
            /// <inheritdoc/>
            public string IoTHubResourceId { get; set; }
        }
    }
}
