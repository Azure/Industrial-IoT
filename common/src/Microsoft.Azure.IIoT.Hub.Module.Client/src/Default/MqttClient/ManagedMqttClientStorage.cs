// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client.MqttClient {
    using Microsoft.Extensions.Logging;
    using MQTTnet.Extensions.ManagedClient;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    /// <inheritdoc />
    public class ManagedMqttClientStorage : IManagedMqttClientStorage {
        private readonly string _stateFile;
        private readonly ILogger _logger;

        /// <summary>
        /// Create client storage.
        /// </summary>
        public ManagedMqttClientStorage(string stateFile, ILogger logger) {
            if (string.IsNullOrWhiteSpace(stateFile)) {
                throw new ArgumentException($"'{nameof(stateFile)}' cannot be null or whitespace.", nameof(stateFile));
            }

            _stateFile = stateFile;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<IList<ManagedMqttApplicationMessage>> LoadQueuedMessagesAsync() {
            if (File.Exists(_stateFile)) {
                try {
                    var content = await File.ReadAllTextAsync(_stateFile, Encoding.UTF8).ConfigureAwait(false);
                    var messages = JsonConvert.DeserializeObject<List<ManagedMqttApplicationMessage>>(content);
                    _logger.LogInformation("Loaded MQTT state from: {StateFile}", _stateFile);
                    return messages;
                }
                catch (IOException ex) {
                    _logger.LogError(ex, "Failed to load MQTT state.");
                }
            }
            else {
                _logger.LogDebug("MQTT state file {StateFile} not found, starting empty.", _stateFile);
            }
            return new List<ManagedMqttApplicationMessage>();
        }

        /// <inheritdoc />
        public async Task SaveQueuedMessagesAsync(IList<ManagedMqttApplicationMessage> messages) {
            if (messages != null) {
                try {
                    var content = JsonConvert.SerializeObject(messages);
                    await File.WriteAllTextAsync(_stateFile, content, Encoding.UTF8).ConfigureAwait(false);
                }
                catch (IOException ex) {
                    _logger.LogError(ex, "Failed to save MQTT state.");
                }
            }
        }
    }
}
