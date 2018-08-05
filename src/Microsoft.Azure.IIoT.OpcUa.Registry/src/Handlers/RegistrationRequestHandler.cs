// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Handlers {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Services;
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Microsoft.Azure.IIoT.OpcUa;
    using Microsoft.Azure.IIoT.Tasks;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Hub;
    using Newtonsoft.Json;
    using System;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Registration request handler
    /// </summary>
    public class RegistrationRequestHandler : IEventHandler {

        /// <inheritdoc/>
        public string ContentType => "application/x-registration-v1-json";

        /// <summary>
        /// Create handler
        /// </summary>
        /// <param name="registry"></param>
        /// <param name="logger"></param>
        public RegistrationRequestHandler(IOpcUaRegistryMaintenance registry,
            ITaskProcessor processor, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
        }

        /// <inheritdoc/>
        public Task HandleAsync(string deviceId, string moduleId, byte[] payload,
            Func<Task> checkpoint) {
            if (OpcUaOnboarderHelper.kId == deviceId.ToString()) {
                var json = Encoding.UTF8.GetString(payload);
                ServerRegistrationRequestModel request;
                try {
                    request = JsonConvertEx.DeserializeObject<ServerRegistrationRequestModel>(json);
                    _processor.TrySchedule(() => _registry.ProcessRegisterAsync(request), checkpoint);
                }
                catch (Exception ex) {
                    _logger.Error("Failed to convert registration json",
                        () => new { json, ex });
                }
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task OnBatchCompleteAsync() => Task.CompletedTask;

        private readonly ILogger _logger;
        private readonly IOpcUaRegistryMaintenance _registry;
        private readonly ITaskProcessor _processor;
    }
}
