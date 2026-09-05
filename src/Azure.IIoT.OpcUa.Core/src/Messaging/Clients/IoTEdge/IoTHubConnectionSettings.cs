// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.IoTEdge
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Validates the connection fields supported by IoTHubby and applies its
    /// whitespace normalization before an identity is used for client ownership.
    /// </summary>
    internal sealed class IoTHubConnectionSettings
    {
        private IoTHubConnectionSettings(Dictionary<string, string> values)
        {
            HostName = Required("HostName");
            DeviceId = Required("DeviceId");
            ModuleId = values.ContainsKey("ModuleId") ? Required("ModuleId") : null;
            GatewayHostName = values.GetValueOrDefault("GatewayHostName");
            SharedAccessKey = values.GetValueOrDefault("SharedAccessKey");
            SharedAccessKeyName = values.GetValueOrDefault("SharedAccessKeyName");
            SharedAccessSignature = values.GetValueOrDefault("SharedAccessSignature");
            if (values.TryGetValue("X509", out var x509))
            {
                if (!bool.TryParse(x509, out var usesX509))
                {
                    throw new FormatException();
                }
                UsesX509 = usesX509;
            }
            if (Uri.CheckHostName(HostName) == UriHostNameType.Unknown
                || (!string.IsNullOrEmpty(GatewayHostName)
                    && Uri.CheckHostName(GatewayHostName) == UriHostNameType.Unknown)
                || (SharedAccessKey is null ? 0 : 1)
                    + (SharedAccessSignature is null ? 0 : 1)
                    + (UsesX509 ? 1 : 0) != 1
                || SharedAccessKey is { Length: 0 }
                || SharedAccessSignature is { Length: 0 })
            {
                throw new FormatException();
            }

            string Required(string key)
            {
                if (!values.TryGetValue(key, out var value) || string.IsNullOrEmpty(value))
                {
                    throw new FormatException();
                }
                return value;
            }
        }

        public string HostName { get; }
        public string DeviceId { get; }
        public string? ModuleId { get; }
        public string? GatewayHostName { get; }
        public string? SharedAccessKey { get; }
        public string? SharedAccessKeyName { get; }
        public string? SharedAccessSignature { get; }
        public bool UsesX509 { get; }

        public static IoTHubConnectionSettings Parse(string connectionString)
        {
            ArgumentException.ThrowIfNullOrEmpty(connectionString);
            try
            {
                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var segment in connectionString.Split(';',
                    StringSplitOptions.RemoveEmptyEntries))
                {
                    var separator = segment.IndexOf('=', StringComparison.Ordinal);
                    if (separator <= 0
                        || !kConnectionKeys.Contains(segment[..separator].Trim())
                        || !values.TryAdd(segment[..separator].Trim(),
                            segment[(separator + 1)..].Trim()))
                    {
                        throw new FormatException();
                    }
                }
                return new IoTHubConnectionSettings(values);
            }
            catch (Exception exception) when (exception is ArgumentException or FormatException)
            {
                // IoTHubby's parser is internal. Reject ambiguous input here,
                // without retaining parser messages that can contain credentials.
                throw new ArgumentException("The IoT Hub connection string is invalid.",
                    nameof(connectionString));
            }
        }

        private static readonly HashSet<string> kConnectionKeys = new(
            ["HostName", "DeviceId", "ModuleId", "SharedAccessKey", "SharedAccessKeyName",
                "SharedAccessSignature", "GatewayHostName", "X509"],
            StringComparer.OrdinalIgnoreCase);
    }
}
