// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.AzureSdk
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.Serialization;
    using System.Text;

    /// <summary>
    /// Minimal connection string helper used by the owned Azure transports.
    /// </summary>
    public sealed class ConnectionString
    {
        /// <summary>
        /// Connection string keys.
        /// </summary>
        public enum Id
        {
            /// <summary>Host name.</summary>
            HostName,
            /// <summary>Device id.</summary>
            DeviceId,
            /// <summary>Module id.</summary>
            ModuleId,
            /// <summary>Shared access key name.</summary>
            SharedAccessKeyName,
            /// <summary>Shared access key.</summary>
            SharedAccessKey,
            /// <summary>Shared access token.</summary>
            SharedAccessToken,
            /// <summary>Endpoint.</summary>
            Endpoint,
            /// <summary>Account endpoint.</summary>
            AccountEndpoint,
            /// <summary>Account name.</summary>
            AccountName,
            /// <summary>Account key.</summary>
            AccountKey,
            /// <summary>Access key.</summary>
            AccessKey,
            /// <summary>Expires.</summary>
            Expires,
            /// <summary>Endpoint suffix.</summary>
            EndpointSuffix,
            /// <summary>Default endpoints protocol.</summary>
            DefaultEndpointsProtocol,
            /// <summary>Entity path.</summary>
            EntityPath,
            /// <summary>Gateway host name.</summary>
            GatewayHostName
        }

        /// <summary>
        /// Entity path.
        /// </summary>
        public string? EntityPath => this[Id.EntityPath];

        /// <summary>
        /// Device id.
        /// </summary>
        public string? DeviceId => this[Id.DeviceId];

        /// <summary>
        /// Module id.
        /// </summary>
        public string? ModuleId => this[Id.ModuleId];

        /// <summary>
        /// Gateway host name.
        /// </summary>
        public string? GatewayHostName => this[Id.GatewayHostName];

        /// <summary>
        /// Host name.
        /// </summary>
        public string? HostName => this[Id.HostName];

        /// <summary>
        /// Endpoint.
        /// </summary>
        public string? Endpoint => this[Id.AccountName] ??
            this[Id.AccountEndpoint] ?? this[Id.Endpoint];

        /// <summary>
        /// Hub name.
        /// </summary>
        public string? HubName
        {
            get
            {
                if (EntityPath != null)
                {
                    return EntityPath;
                }
                var idx = HostName?.IndexOf('.', StringComparison.Ordinal) ?? -1;
                return idx == -1 ? null : HostName![..idx];
            }
        }

        /// <summary>
        /// Parse a connection string.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static ConnectionString Parse(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString));
            }
            var cs = new ConnectionString();
            foreach (var elem in connectionString.Split(kSemicolon,
                StringSplitOptions.RemoveEmptyEntries))
            {
                var index = elem.IndexOf('=', StringComparison.Ordinal);
                if (index < 0)
                {
                    throw new InvalidDataContractException("Bad key value pair.");
                }
                cs._items.Add(Enum.Parse<Id>(elem[..index], true),
                    elem[(index + 1)..]);
            }
            return cs;
        }

        /// <summary>
        /// Try parse a connection string.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="cs"></param>
        /// <returns></returns>
        public static bool TryParse(string connectionString,
            [NotNullWhen(true)] out ConnectionString? cs)
        {
            try
            {
                cs = Parse(connectionString);
                return true;
            }
            catch
            {
                cs = null;
                return false;
            }
        }

        /// <summary>
        /// Create service connection string.
        /// </summary>
        /// <param name="hostName"></param>
        /// <param name="keyName"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static ConnectionString CreateServiceConnectionString(string hostName,
            string keyName, string key)
        {
            var connectionString = new ConnectionString();
            connectionString._items[Id.HostName] = hostName;
            connectionString._items[Id.SharedAccessKeyName] = keyName;
            connectionString._items[Id.SharedAccessKey] = key;
            return connectionString;
        }

        /// <summary>
        /// Create module connection string.
        /// </summary>
        /// <param name="hostName"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static ConnectionString CreateModuleConnectionString(string hostName,
            string deviceId, string moduleId, string key)
        {
            var connectionString = new ConnectionString();
            connectionString._items[Id.HostName] = hostName;
            connectionString._items[Id.DeviceId] = deviceId;
            connectionString._items[Id.ModuleId] = moduleId;
            connectionString._items[Id.SharedAccessKey] = key;
            return connectionString;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var builder = new StringBuilder();
            foreach (var kv in _items)
            {
                builder.Append(kv.Key).Append('=').Append(kv.Value).Append(';');
            }
            return builder.ToString().TrimEnd(';');
        }

        private ConnectionString()
        {
        }

        private string? this[Id id] =>
            !_items.TryGetValue(id, out var value) ? null : value;

        private static readonly char[] kSemicolon = [';'];
        private readonly Dictionary<Id, string> _items = [];
    }
}
