// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Runtime
{
    using Furly.Extensions.Configuration;
    using Furly.Extensions.Mqtt;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Configure mqtt broker
    /// </summary>
    public sealed class MqttBrokerConfig : ConfigureOptionBase<MqttOptions>
    {
        /// <summary>
        /// Configuration
        /// </summary>
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public const string MqttClientConnectionStringKey = "MqttClientConnectionString";
        public const string ClientIdKey = "MqttClientId";
        public const string UserNameKey = "MqttBrokerUserName";
        public const string PasswordKey = "MqttBrokerPasswordKey";
        public const string HostNameKey = "MqttBrokerHostName";
        public const string HostPortKey = "MqttBrokerPort";
        public const string ProtocolKey = "MqttProtocolVersion";
        public const string UseTlsKey  = "MqttBrokerUsesTls";
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <inheritdoc/>
        public override void Configure(string name, MqttOptions options)
        {
            var mqttClientConnectionString = GetStringOrDefault(MqttClientConnectionStringKey);
            if (mqttClientConnectionString != null)
            {
                var properties = ToDictionary(mqttClientConnectionString);
                options.HostName = properties[nameof(options.HostName)];

                // Permit the port to be set if provided, otherwise use defaults.
                if (properties.TryGetValue(nameof(options.Port), out var value) &&
                    int.TryParse(value, CultureInfo.InvariantCulture, out var port))
                {
                    options.Port = port;
                }
                if (properties.ContainsKey(nameof(options.UserName)))
                {
                    options.UserName = properties[nameof(options.UserName)];
                }
                if (properties.ContainsKey(nameof(options.Password)))
                {
                    options.Password = properties[nameof(options.Password)];
                }
                if (properties.TryGetValue(nameof(options.Version), out value) &&
                    Enum.TryParse<MqttVersion>(value, true, out var version))
                {
                    options.Version = version;
                }
                if (properties.TryGetValue(nameof(options.UseTls), out value) &&
                    bool.TryParse(value, out var useTls))
                {
                    options.UseTls = useTls;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(options.HostName))
                {
                    options.ClientId = GetStringOrDefault(HostNameKey);
                }
                if (string.IsNullOrEmpty(options.UserName))
                {
                    options.UserName = GetStringOrDefault(UserNameKey);
                }
                if (string.IsNullOrEmpty(options.Password))
                {
                    options.Password = GetStringOrDefault(PasswordKey);
                }
                if (options.Port == null)
                {
                    options.Port = GetIntOrNull(HostPortKey);
                }
                if (Enum.TryParse<MqttVersion>(GetStringOrDefault(ProtocolKey),
                    true, out var version))
                {
                    options.Version = version;
                }
                if (options.UseTls == null)
                {
                    options.UseTls = GetBoolOrNull(UseTlsKey);
                }
            }

            if (string.IsNullOrEmpty(options.ClientId))
            {
                options.ClientId = GetStringOrDefault(ClientIdKey);
            }
        }

        /// <summary>
        /// Transport configuration
        /// </summary>
        /// <param name="configuration"></param>
        public MqttBrokerConfig(IConfiguration configuration)
            : base(configuration)
        {
        }

        /// <summary>
        /// Parse connection string as dictionary
        /// </summary>
        /// <param name="valuePairString"></param>
        /// <param name="kvpDelimiter"></param>
        /// <param name="kvpSeparator"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="FormatException"></exception>
        private static IDictionary<string, string> ToDictionary(string valuePairString,
            char kvpDelimiter = ';', char kvpSeparator = '=')
        {
            if (string.IsNullOrWhiteSpace(valuePairString))
            {
                throw new ArgumentException("Malformed Token");
            }

            // This regex allows semi-colons to be part of the allowed characters
            // for device names. Although new devices are not
            // allowed to have semi-colons in the name, some legacy devices still
            // have them and so this name validation cannot be changed.
            var parts = new Regex($"(?:^|{kvpDelimiter})([^{kvpDelimiter}{kvpSeparator}]*){kvpSeparator}")
                .Matches(valuePairString)
                .Cast<Match>()
                .Select(m => new string[] {
                    m.Result("$1"),
                    valuePairString.Substring(
                        m.Index + m.Value.Length,
                        (m.NextMatch().Success ? m.NextMatch().Index : valuePairString.Length)
                            - (m.Index + m.Value.Length))
                });

            if (!parts.Any() || parts.Any(p => p.Length != 2))
            {
                throw new FormatException("Malformed Token");
            }
            return parts.ToDictionary(kvp => kvp[0], (kvp) => kvp[1], StringComparer.OrdinalIgnoreCase);
        }
    }
}
