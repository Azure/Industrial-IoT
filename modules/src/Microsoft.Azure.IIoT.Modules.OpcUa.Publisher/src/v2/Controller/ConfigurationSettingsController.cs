// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.v2.Controller {
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Newtonsoft.Json.Linq;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Controller
    /// </summary>
    [Version(1)]
    [Version(2)]
    public class ConfigurationSettingsController : ISettingsController,
        IAgentConfigProvider {

        /// <summary>
        /// Job orchestrator url
        /// </summary>
        public string JobOrchestratorUrl {
            set => _config.JobOrchestratorUrl =
                string.IsNullOrEmpty(value) ? null : value;
            get => _config.JobOrchestratorUrl ?? "";
        }

        /// <summary>
        /// Job check interval
        /// </summary>
        public JToken JobCheckInterval {
            set => _config.JobCheckInterval =
                value?.ToObject<TimeSpan>();
            get => _config.JobCheckInterval;
        }

        /// <summary>
        /// Heartbeat interval
        /// </summary>
        public JToken HeartbeatInterval {
            set => _config.HeartbeatInterval =
                value?.ToObject<TimeSpan>();
            get => _config.HeartbeatInterval;
        }

        /// <summary>
        /// Max number of workers
        /// </summary>
        public JToken MaxWorkers {
            set => _config.MaxWorkers =
                value?.ToObject<int>();
            get => _config.MaxWorkers;
        }

        /// <summary>
        /// Max number of workers
        /// </summary>
        public JToken Capabilities {
            set => _config.Capabilities =
                value?.ToObject<Dictionary<string, string>>();
            get => _config.Capabilities == null ? null :
                JToken.FromObject(_config.Capabilities);
        }

        /// <inheritdoc/>
        [Ignore]
        public AgentConfigModel Config {
            get {
                var config = _config.Clone();
                config.AgentId = _identity.DeviceId + "_" + _identity.ModuleId;
                if (config.Capabilities == null) {
                    config.Capabilities = new Dictionary<string, string>();
                }
                config.Capabilities.AddOrUpdate("Type", IdentityType.Publisher);
                config.Capabilities.AddOrUpdate(nameof(_identity.SiteId),
                    _identity.SiteId ?? _identity.DeviceId);
                config.Capabilities.AddOrUpdate(nameof(_identity.DeviceId),
                    _identity.DeviceId);
                config.Capabilities.AddOrUpdate(nameof(_identity.ModuleId),
                    _identity.ModuleId);
                return config;
            }
        }

        /// <summary>
        /// Called to update discovery configuration
        /// </summary>
        /// <returns></returns>
        public Task ApplyAsync() {
            _logger.Debug("Updating agent configuration...");
            OnConfigUpdated?.Invoke(this, new EventArgs());
            _logger.Information("Agent configuration updated.");
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public event ConfigUpdatedEventHandler OnConfigUpdated;

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="identity"></param>
        public ConfigurationSettingsController(ILogger logger, IIdentity identity) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _identity = identity ?? throw new ArgumentNullException(nameof(identity));
            _config = new AgentConfigModel();
        }

        private readonly AgentConfigModel _config;
        private readonly ILogger _logger;
        private readonly IIdentity _identity;
    }
}