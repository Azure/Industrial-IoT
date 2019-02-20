// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Utils {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Text;

    /// <summary>
    /// Connection string helper class
    /// </summary>
    public sealed class ConnectionString {

        /// <summary>
        /// Identifies a part of the connection string
        /// </summary>
        public enum Id {
            /// <summary>Host name</summary>
            HostName,
            /// <summary>Device Id</summary>
            DeviceId,
            /// <summary>Module Id</summary>
            ModuleId,
            /// <summary>Key Name</summary>
            SharedAccessKeyName,
            /// <summary>Shared access key</summary>
            SharedAccessKey,
            /// <summary>Shared access token</summary>
            SharedAccessToken,
            /// <summary>Endpoint</summary>
            Endpoint,
            /// <summary>Account endpoint</summary>
            AccountEndpoint,
            /// <summary>Account endpoint</summary>
            AccountKey,
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
                return HostName.Substring(0, idx);
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
        /// Get module id
        /// </summary>
        public string ModuleId => this[Id.ModuleId];

        /// <summary>
        /// Get shared access key name
        /// </summary>
        public string SharedAccessKeyName => this[Id.SharedAccessKeyName];

        /// <summary>
        /// Get shared access key
        /// </summary>
        public string SharedAccessKey => this[Id.SharedAccessKey] ?? this[Id.AccountKey];

        /// <summary>
        /// Get shared access key
        /// </summary>
        public string SharedAccessToken => this[Id.SharedAccessToken];

        /// <summary>
        /// Get account endpoint
        /// </summary>
        public string Endpoint => this[Id.AccountEndpoint] ?? this[Id.Endpoint];

        /// <summary>
        /// Parse connection string
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static ConnectionString Parse(string connectionString) {
            if (string.IsNullOrEmpty(connectionString)) {
                throw new ArgumentException("Connection string must not be null",
                    nameof(ConnectionString));
            }
            var cs = new ConnectionString();
            foreach (var elem in connectionString.Split(new char[] { ';' },
                StringSplitOptions.RemoveEmptyEntries)) {
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
        /// Try parse connection string
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="cs"></param>
        /// <returns></returns>
        public static bool TryParse(string connectionString, out ConnectionString cs) {
            try {
                cs = Parse(connectionString);
                return true;
            }
            catch {
                cs = null;
                return false;
            }
        }

        /// <summary>
        /// Create service bus or event hub connection string
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="keyName"></param>
        /// <param name="token"></param>
        public static ConnectionString CreateWithEndpointAndToken(Uri endpoint,
            string keyName, string token) {
            var connectionString = new ConnectionString();
            connectionString._items[Id.Endpoint] = endpoint.ToString();
            connectionString._items[Id.SharedAccessKeyName] = keyName;
            connectionString._items[Id.SharedAccessToken] = token;
            return connectionString;
        }

        /// <summary>
        /// Create event hub connection string
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="keyName"></param>
        /// <param name="key"></param>
        public static ConnectionString CreateEventHubConnectionString(string endpoint,
            string keyName, string key) {
            var connectionString = new ConnectionString();
            connectionString._items[Id.Endpoint] = endpoint;
            connectionString._items[Id.SharedAccessKeyName] = keyName;
            connectionString._items[Id.SharedAccessKey] = key;
            return connectionString;
        }

        /// <summary>
        /// Create service connection string
        /// </summary>
        /// <param name="hostName"></param>
        /// <param name="keyName"></param>
        /// <param name="key"></param>
        public static ConnectionString CreateServiceConnectionString(string hostName,
            string keyName, string key) {
            var connectionString = new ConnectionString();
            connectionString._items[Id.HostName] = hostName;
            connectionString._items[Id.SharedAccessKeyName] = keyName;
            connectionString._items[Id.SharedAccessKey] = key;
            return connectionString;
        }

        /// <summary>
        /// Create device connection string
        /// </summary>
        /// <param name="hostName"></param>
        /// <param name="deviceId"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static ConnectionString CreateDeviceConnectionString(string hostName,
            string deviceId, string key) {
            var connectionString = new ConnectionString();
            connectionString._items[Id.HostName] = hostName;
            connectionString._items[Id.DeviceId] = deviceId;
            connectionString._items[Id.SharedAccessKey] = key;
            return connectionString;
        }

        /// <summary>
        /// Create module connection string
        /// </summary>
        /// <param name="hostName"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static ConnectionString CreateModuleConnectionString(string hostName,
            string deviceId, string moduleId, string key) {
            var connectionString = new ConnectionString();
            connectionString._items[Id.HostName] = hostName;
            connectionString._items[Id.DeviceId] = deviceId;
            connectionString._items[Id.ModuleId] = moduleId;
            connectionString._items[Id.SharedAccessKey] = key;
            return connectionString;
        }

        /// <summary>
        /// Converts to string
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            var b = new StringBuilder();
            foreach (var kv in _items.Where(kv => kv.Value != null)) {
                b.Append(kv.Key.ToString());
                b.Append("=");
                b.Append(kv.Value);
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
