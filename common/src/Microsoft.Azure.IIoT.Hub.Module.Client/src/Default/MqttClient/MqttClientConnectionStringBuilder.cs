// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client.MqttClient {
    using System;
    using System.IO;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Connection string builder for MQTT client connections
    /// </summary>
    public sealed class MqttClientConnectionStringBuilder {
        private const char kValuePairDelimiter = ';';
        private const char kValuePairSeparator = '=';
        private const string kHostNamePropertyName = nameof(HostName);
        private const string kPortPropertyName = nameof(Port);
        private const string kDeviceIdPropertyName = nameof(DeviceId);
        private const string kModuleIdPropertyName = nameof(ModuleId);
        private const string kUsernamePropertyName = nameof(Username);
        private const string kAuthPropertyName = nameof(Password);
        private const string kSharedAccessSignaturePropertyName = nameof(SharedAccessSignature);
        private const string kUsingIoTHubPropertyName = nameof(UsingIoTHub);
        private const string kStateFilePropertyName = nameof(StateFile);
        private const string kMessageExpiryIntervalPropertyName = nameof(MessageExpiryInterval);

        private const int kDefaultMqttPort = 1883;
        private const int kDefaultIoTHubMqttPort = 8883;

        private const RegexOptions kCommonRegexOptions = RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
        private static readonly TimeSpan kRegexTimeoutMilliseconds = TimeSpan.FromMilliseconds(500);
        private static readonly Regex kIdNameRegex = new Regex(@"^[A-Za-z0-9\-:.+%_#*?!(),=@;$']{1,128}$", kCommonRegexOptions, kRegexTimeoutMilliseconds);
        private static readonly Regex kHostNameRegex = new Regex(@"^[a-zA-Z0-9_\-\.]+.(azure-devices\.net)$", kCommonRegexOptions, kRegexTimeoutMilliseconds);
        private static readonly Regex kSharedAccessSignatureRegex = new Regex(@"^(SharedAccessSignature)(( |&)((sr)|(sig)|(se))=.+){3}$", kCommonRegexOptions, kRegexTimeoutMilliseconds);
        private static readonly Regex kSasResourceUriRegex = new Regex(@"^SharedAccessSignature.+(sr=).+(&|$)", kCommonRegexOptions, kRegexTimeoutMilliseconds);
        private static readonly Regex kSasSignatureRegex = new Regex(@"^SharedAccessSignature.+(sig=).+(&|$)", kCommonRegexOptions, kRegexTimeoutMilliseconds);
        private static readonly Regex kSasExpiryRegex = new Regex(@"^SharedAccessSignature.+(se=)\d+(&|$)", kCommonRegexOptions, kRegexTimeoutMilliseconds);

        /// <summary>
        /// Gets or sets the value of the fully-qualified DNS hostname of the MQTT server.
        /// </summary>
        public string HostName { get; private set; }

        /// <summary>
        /// Gets or sets the port number of the MQTT server.
        /// </summary>
        public int Port { get; private set; }

        /// <summary>
        /// Gets the device identifier of the device connecting to the service.
        /// </summary>
        public string DeviceId { get; private set; }

        /// <summary>
        /// Gets the module identifier of the module connecting to the service.
        /// </summary>
        public string ModuleId { get; private set; }

        /// <summary>
        /// Gets the username to connect to an MQTT server.
        /// </summary>
        public string Username { get; private set; }

        /// <summary>
        /// Gets the password to connect to an MQTT server.
        /// </summary>
        public string Password { get; private set; }

        /// <summary>
        /// Gets or sets whether we are connecting to an Azure IoT Hub.
        /// </summary>
        public bool UsingIoTHub { get; private set; }

        /// <summary>
        /// Gets or sets the shared access signature used to connect to an Azure IoT Hub.
        /// </summary>
        public string SharedAccessSignature { get; private set; }

        /// <summary>
        /// Gets or sets the certificate used to connect to an Azure IoT Hub.
        /// </summary>
        public X509Certificate X509Cert { get; private set; }

        /// <summary>
        /// Gets whether a certificate has been loaded for an Azure IoT Hub.
        /// </summary>
        public bool UsingX509Cert => X509Cert != null;

        /// <summary>
        /// Gets or sets the state file path to be used to persist the MQTT client state.
        /// </summary>
        public string StateFile { get; private set; }

        /// <summary>
        /// Gets whether a state file should be used to persist the MQTT client state.
        /// </summary>
        public bool UsingStateFile => !string.IsNullOrWhiteSpace(StateFile);

        /// <summary>
        /// Gets or sets the period of time (seconds) for the broker to store the message for any subscribers that are not yet connected.
        /// </summary>
        public uint? MessageExpiryInterval { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MqttClientConnectionStringBuilder"/> class.
        /// </summary>
        private MqttClientConnectionStringBuilder() {
        }

        /// <summary>
        /// Creates a connection string based on the supplied connection string.
        /// </summary>
        /// <param name="mqttClientConnectionString">The connection string.</param>
        /// <returns>A new instance of the <see cref="MqttClientConnectionStringBuilder"/> class with a populated connection string.</returns>
        public static MqttClientConnectionStringBuilder Create(string mqttClientConnectionString) {
            if (string.IsNullOrWhiteSpace(mqttClientConnectionString)) {
                throw new ArgumentNullException(nameof(mqttClientConnectionString));
            }

            var properties = mqttClientConnectionString.ToDictionary(kValuePairDelimiter, kValuePairSeparator);
            var mqttClientConnectionStringBuilder = new MqttClientConnectionStringBuilder {
                HostName = properties.GetRequired<string>(kHostNamePropertyName),
                DeviceId = properties.GetOptional<string>(kDeviceIdPropertyName),
                ModuleId = properties.GetOptional<string>(kModuleIdPropertyName),
                StateFile = properties.GetOptional<string>(kStateFilePropertyName),
            };

            if (properties.ContainsKey(kUsingIoTHubPropertyName)) {
                mqttClientConnectionStringBuilder.UsingIoTHub = properties.GetRequired<bool>(kUsingIoTHubPropertyName);
            }
            else {
                // If the connection string does not contain the property to indicate if the target is an Azure IoT Hub
                // or not, we can make a guess using the hostname.
                mqttClientConnectionStringBuilder.UsingIoTHub = kHostNameRegex.Match(mqttClientConnectionStringBuilder.HostName).Success;
            }

            // Permit the port to be set if provided, otherwise use defaults.
            if (properties.ContainsKey(kPortPropertyName)) {
                mqttClientConnectionStringBuilder.Port = properties.GetRequired<int>(kPortPropertyName);
            }
            else {
                if (mqttClientConnectionStringBuilder.UsingIoTHub) {
                    mqttClientConnectionStringBuilder.Port = kDefaultIoTHubMqttPort;
                }
                else {
                    mqttClientConnectionStringBuilder.Port = kDefaultMqttPort;
                }
            }

            if (mqttClientConnectionStringBuilder.UsingIoTHub) {
                mqttClientConnectionStringBuilder.SharedAccessSignature = properties.GetRequired<string>(kSharedAccessSignaturePropertyName);
                mqttClientConnectionStringBuilder.Username = $"{mqttClientConnectionStringBuilder.HostName}/{mqttClientConnectionStringBuilder.DeviceId}/?api-version=2018-06-30";
                mqttClientConnectionStringBuilder.Password = mqttClientConnectionStringBuilder.SharedAccessSignature;

                // Get the DigiCert Baltimore root certificate to establish a TLS connection for the Azure IoT Hub.
                // You can find the certificate in the Azure IoT C SDK at https://github.com/Azure/azure-iot-sdk-c/blob/master/certs/certs.c.
                var certificatePath = Environment.GetEnvironmentVariable("IoTHubRootCertificateFile");
                if (!string.IsNullOrWhiteSpace(certificatePath) && File.Exists(certificatePath)) {
                    mqttClientConnectionStringBuilder.X509Cert = X509Certificate.CreateFromCertFile(certificatePath);
                }
            }
            else {
                mqttClientConnectionStringBuilder.Username = properties.GetOptional<string>(kUsernamePropertyName);
                mqttClientConnectionStringBuilder.Password = properties.GetOptional<string>(kAuthPropertyName);
            }

            if (properties.ContainsKey(kMessageExpiryIntervalPropertyName)) {
                mqttClientConnectionStringBuilder.MessageExpiryInterval = properties.GetRequired<uint>(kMessageExpiryIntervalPropertyName);
            }

            mqttClientConnectionStringBuilder.Validate();
            return mqttClientConnectionStringBuilder;
        }

        /// <summary>
        /// Produces the connection string based on the values of the <see cref="MqttClientConnectionStringBuilder"/> instance properties.
        /// </summary>
        /// <returns>A connection string of type <see cref="MqttClientConnectionString"/>.</returns>
        public MqttClientConnectionString Build() {
            Validate();
            return new MqttClientConnectionString(this);
        }

        /// <summary>
        /// Validate the properties of the connection string.
        /// </summary>
        private void Validate() {
            if (string.IsNullOrWhiteSpace(HostName)) {
                throw new ArgumentException($"{kHostNamePropertyName} must be specified in the connection string");
            }

            if (string.IsNullOrWhiteSpace(DeviceId)) {
                throw new ArgumentException($"{kDeviceIdPropertyName} must be specified in the connection string");
            }

            ValidateFormat(DeviceId, kDeviceIdPropertyName, kIdNameRegex);
            ValidateFormatIfSpecified(ModuleId, kModuleIdPropertyName, kIdNameRegex);

            if (UsingIoTHub) {
                if (string.IsNullOrWhiteSpace(SharedAccessSignature)) {
                    throw new ArgumentException($"{kSharedAccessSignaturePropertyName} must be specified in the connection string");
                }

                ValidateFormat(SharedAccessSignature, nameof(SharedAccessSignature), kSharedAccessSignatureRegex);
                ValidateFormat(SharedAccessSignature, nameof(SharedAccessSignature), kSasResourceUriRegex);
                ValidateFormat(SharedAccessSignature, nameof(SharedAccessSignature), kSasSignatureRegex);
                ValidateFormat(SharedAccessSignature, nameof(SharedAccessSignature), kSasExpiryRegex);

                if (string.IsNullOrWhiteSpace(Username)) {
                    throw new ArgumentException($"{kUsernamePropertyName} was not configured and is required for the Azure IoT Hub");
                }

                if (string.IsNullOrWhiteSpace(Password)) {
                    throw new ArgumentException($"{kAuthPropertyName} was not configured and is required for the Azure IoT Hub");
                }
            }
        }

        /// <summary>
        /// Validate the format of a property value.
        /// </summary>
        /// <param name="value">The property value to validate.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="regex">The regular expression for a valid value.</param>
        private static void ValidateFormat(string value, string propertyName, Regex regex) {
            if (!regex.IsMatch(value)) {
                throw new ArgumentException($"The connection string has an invalid value for property: {propertyName}.");
            }
        }

        /// <summary>
        /// Validate the format of a property value if it's not empty.
        /// </summary>
        /// <param name="value">The property value to validate.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="regex">The regular expression for a valid value.</param>
        private static void ValidateFormatIfSpecified(string value, string propertyName, Regex regex) {
            if (!string.IsNullOrEmpty(value)) {
                ValidateFormat(value, propertyName, regex);
            }
        }

        /// <summary>
        /// Produces the connection string based on the values of the <see cref="MqttClientConnectionStringBuilder"/> instance properties.
        /// </summary>
        /// <returns>A properly formatted connection string.</returns>
        public override sealed string ToString() {
            Validate();

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendKeyValuePairIfNotEmpty(kHostNamePropertyName, HostName);
            stringBuilder.AppendKeyValuePairIfNotEmpty(kPortPropertyName, Port);
            stringBuilder.AppendKeyValuePairIfNotEmpty(kDeviceIdPropertyName, DeviceId);
            stringBuilder.AppendKeyValuePairIfNotEmpty(kModuleIdPropertyName, ModuleId);
            stringBuilder.AppendKeyValuePairIfNotEmpty(kUsernamePropertyName, Username);
            stringBuilder.AppendKeyValuePairIfNotEmpty(kAuthPropertyName, Password);
            stringBuilder.AppendKeyValuePairIfNotEmpty(kUsingIoTHubPropertyName, UsingIoTHub);
            if (stringBuilder.Length > 0) {
                stringBuilder.Remove(stringBuilder.Length - 1, 1);
            }

            return stringBuilder.ToString();
        }
    }
}
