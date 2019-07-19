// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Deployment {
    using Microsoft.Azure.IIoT.Module.Deployment.Models;
    using Microsoft.Azure.IIoT.Hub.Models;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    /// <summary>
    /// Edge deployment base implementation
    /// </summary>
    public class EdgeDeploymentBase : IEdgeDeployment {

        /// <summary>
        /// Create new deployment
        /// </summary>
        internal EdgeDeploymentBase() :
            this(null) {
        }

        /// <summary>
        /// Create new deployment
        /// </summary>
        /// <param name="configuration"></param>
        internal EdgeDeploymentBase(ConfigurationContentModel configuration) {
            if (configuration == null) {
                // Create default configuration
                _configuration = new ConfigurationContentModel();
                _edgeAgent = new EdgeAgentConfigurationModel {
                    SchemaVersion = kDefaultSchemaVersion,
                    Modules = new Dictionary<string, EdgeAgentModuleModel>(),
                    Runtime = new EdgeAgentRuntimeModel {
                        Settings = new EdgeAgentRuntimeSettingsModel()
                    }
                };
                _edgeHub = new EdgeHubConfigurationModel {
                    SchemaVersion = kDefaultSchemaVersion,
                    Routes = new Dictionary<string, string> {
                        [kDefaultRouteName] = kDefaultRoute
                    },
                    StorageConfig = new EdgeHubStoreAndForwardModel {
                        TimeToLiveSecs = 7200
                    }
                };
                _configuration.AddModulesContent(_edgeAgent);
                _configuration.AddModulesContent(_edgeHub);
            }
            else {
                // Use provided configuration
                _configuration = configuration;
                _edgeAgent = _configuration.GetEdgeAgentConfiguration();
                _edgeHub = _configuration.GetEdgeHubConfiguration();
            }
        }

        /// <inheritdoc/>
        public IEdgeDeployment WithModule(EdgeDeploymentModuleModel module) {
            if (module == null) {
                throw new ArgumentNullException(nameof(module));
            }
            if (string.IsNullOrEmpty(module.Name)) {
                throw new ArgumentNullException(nameof(module.Name));
            }
            if (string.IsNullOrEmpty(module.ImageName)) {
                throw new ArgumentNullException(nameof(module.ImageName));
            }
            if (module.Name.EqualsIgnoreCase("$edgeAgent") ||
                module.Name.EqualsIgnoreCase("$edgeHub")) {
                throw new ArgumentException($"{module.Name} cannot be system module name.",
                    nameof(module.Name));
            }
            _edgeAgent.Modules.Add(module.Name, new EdgeAgentModuleModel {
                DesiredStatus = (module.Stopped ?? false) ?
                    ModuleDesiredStatus.Stopped : ModuleDesiredStatus.Running,
                RestartPolicy = module.RestartPolicy ?? ModuleRestartPolicy.Always,
                Version = module.Version,
                Settings = EdgeAgentModuleSettingsModelEx.Create(module.ImageName,
                    module.Version, module.CreateOptions)
            });
            _configuration.AddModulesContent(_edgeAgent);
            if (module.Properties == null) {
                module.Properties = new Dictionary<string, dynamic>();
            }
            _configuration.AddModulesContent(module.Name, module.Properties);
            return this;
        }

        /// <inheritdoc/>
        public IEdgeDeployment WithRoute(EdgeDeploymentRouteModel route) {
            if (route == null) {
                throw new ArgumentNullException(nameof(route));
            }
            if (string.IsNullOrEmpty(route.Name) && string.IsNullOrEmpty(route.From)) {
                // Default route covers this one
                return this;
            }
            if (string.IsNullOrEmpty(route.From) || route.To.EqualsIgnoreCase("messages/*")) {
                route.From = "messages/*";
            }
            else if (!_configuration.ModulesContent.ContainsKey(route.To)) {
                throw new ArgumentException($"{route.From} does not exist yet",
                    nameof(route.From));
            }
            if (string.IsNullOrEmpty(route.To) || route.To.EqualsIgnoreCase("$upstream")) {
                if (string.IsNullOrEmpty(route.Condition)) {
                    return this;
                }
                route.From = "$upstream";
            }
            else if (!_configuration.ModulesContent.ContainsKey(route.To)) {
                throw new ArgumentException($"{route.To} does not exist yet",
                    nameof(route.To));
            }
            var statement = !string.IsNullOrEmpty(route.Condition) ?
                $"FROM {route.From} INTO {route.To} WHERE {route.Condition}" :
                $"FROM {route.From} INTO {route.To}";
            _edgeHub.Routes.AddOrUpdate(route.Name, statement);
            return this;
        }

        /// <inheritdoc/>
        public virtual Task ApplyAsync() {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override string ToString() {
            return JsonConvertEx.SerializeObjectPretty(_configuration);
        }

        private const string kDefaultSchemaVersion = "1.0";
        private const string kDefaultRouteName = "default";
        private const string kDefaultRoute = "FROM messages/* INTO $upstream";

        /// <summary>Configuration content to be used by derived classes</summary>
        protected readonly ConfigurationContentModel _configuration;
        private readonly EdgeAgentConfigurationModel _edgeAgent;
        private readonly EdgeHubConfigurationModel _edgeHub;
    }
}
