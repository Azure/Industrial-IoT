// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.Common.Utils {
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Text;

    /// <summary>
    /// Connection string helper class
    /// </summary>
    public class ConnectionString {

        public enum Id {
            HostName,
            DeviceId,
            SharedAccessKeyName,
            SharedAccessKey,
            EndpointName,
            SharedAccessToken
        }

        /// <summary>
        /// Get hub name from connection string
        /// </summary>
        public string HubName {
            get {
                var idx = HostName.IndexOf('.');
                if (idx == -1) {
                    throw new InvalidDataContractException("No hub name");
                }
                return HostName.Substring(idx);
            }
        }

        /// <summary>
        /// Get host name from connection string
        /// </summary>
        public string HostName => this[Id.HostName];

        /// <summary>
        /// Get device id
        /// </summary>
        public string DeviceId => this[Id.DeviceId];

        /// <summary>
        /// Get shared access key name
        /// </summary>
        public string SharedAccessKeyName => this[Id.SharedAccessKeyName];

        /// <summary>
        /// Get shared access key
        /// </summary>
        public string SharedAccessKey => this[Id.SharedAccessKey];

        /// <summary>
        /// Get shared access key
        /// </summary>
        public string SharedAccessToken => this[Id.SharedAccessToken];

        /// <summary>
        /// Parse connection string
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static ConnectionString Parse(string connectionString) {
            if (connectionString == null) {
                throw new ArgumentException("Connection string must be non null");
            }
            var cs = new ConnectionString();
            foreach (var elem in connectionString.Split(';')) {
                var i = elem.IndexOf("=", StringComparison.Ordinal);
                if (i < 0) {
                    throw new InvalidDataContractException("Bad key value pair.");
                }
                // Throws argument if already exists or parse fails...
                cs._items.Add((Id)Enum.Parse(typeof(Id), elem.Substring(0, i), true),
                    elem.Substring(i + 1));
            }
            return cs;
        }

        /// <summary>
        /// Converts to string
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            var b = new StringBuilder();
            foreach (var kv in _items) {
                b.Append(kv.Key.ToString());
                b.Append("=");
                b.Append(kv.Value.ToString());
                b.Append(";");
            }
            return b.ToString().TrimEnd(';');
        }

        /// <summary>
        /// Create connection string
        /// </summary>
        private ConnectionString() {
            _items = new Dictionary<Id, string>();
        }

        /// <summary>
        /// Indexer
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private string this[Id id] {
            get {
                if (!_items.TryGetValue(id, out var value)) {
                    return null;
                }
                return value;
            }
        }

        private readonly Dictionary<Id, string> _items;
    }
}
