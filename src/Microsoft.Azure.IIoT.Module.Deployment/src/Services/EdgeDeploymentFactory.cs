// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Deployment {
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Factory creating deployments for iotedge.
    /// </summary>
    public class EdgeDeploymentFactory : IEdgeDeploymentFactory {

        /// <summary>
        /// Create deployment manager
        /// </summary>
        /// <param name="service"></param>
        public EdgeDeploymentFactory(IIoTHubConfigurationServices service) {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <inheritdoc/>
        public IEdgeDeployment Create(string deviceId,
            ConfigurationContentModel configuration) {
            return new EdgeDeviceDeployment(this, deviceId, configuration);
        }

        /// <inheritdoc/>
        public IEdgeDeployment Create(string name, string condition,
            int priority, ConfigurationContentModel configuration) {
            return new EdgeFleetDeployment(this, name, condition, priority, configuration);
        }

        /// <summary>
        /// Single Deployment
        /// </summary>
        private class EdgeDeviceDeployment : EdgeDeploymentBase {

            /// <summary>
            /// Create deployment
            /// </summary>
            /// <param name="factory"></param>
            /// <param name="deviceId"></param>
            /// <param name="configuration"></param>
            public EdgeDeviceDeployment(EdgeDeploymentFactory factory,
                string deviceId, ConfigurationContentModel configuration) :
                base(configuration) {
                _deviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
                _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            }

            /// <inheritdoc/>
            public override Task ApplyAsync() {
                return _factory._service.ApplyConfigurationAsync(_deviceId, _configuration);
            }

            private readonly string _deviceId;
            private readonly EdgeDeploymentFactory _factory;
        }

        /// <summary>
        /// Fleet deployment
        /// </summary>
        private class EdgeFleetDeployment : EdgeDeploymentBase {

            /// <summary>
            /// Create deployment
            /// </summary>
            /// <param name="factory"></param>
            /// <param name="name"></param>
            /// <param name="condition"></param>
            /// <param name="priority"></param>
            /// <param name="configuration"></param>
            public EdgeFleetDeployment(EdgeDeploymentFactory factory, string name,
                string condition, int priority, ConfigurationContentModel configuration) :
                base(configuration) {
                if (string.IsNullOrEmpty(condition)) {
                    throw new ArgumentNullException(nameof(condition));
                }
                if (string.IsNullOrEmpty(name)) {
                    throw new ArgumentNullException(nameof(name));
                }
                _factory = factory ?? throw new ArgumentNullException(nameof(factory));
                _model = new ConfigurationModel {
                    SchemaVersion = kDefaultSchemaVersion,
                    Id = name,
                    Content = _configuration,
                    TargetCondition = condition,
                    Priority = priority
                };
            }

            /// <inheritdoc/>
            public override Task ApplyAsync() {
                return _factory._service.CreateOrUpdateConfigurationAsync(_model, false);
            }

            private const string kDefaultSchemaVersion = "1.0";
            private readonly EdgeDeploymentFactory _factory;
            private readonly ConfigurationModel _model;
        }

        private readonly IIoTHubConfigurationServices _service;
    }
}
