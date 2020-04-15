// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Infrastructure {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Text;

    using Microsoft.Azure.Management.DeploymentManager;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;

    class AzureDeploymentMgmtClient : IDisposable {

        private readonly AzureDeploymentManagerClient _deploymentClient;

        public AzureDeploymentMgmtClient(
            string subscriptionId,
            RestClient restClient
        ) {
            // We need to initialize new RestClient so that we
            // extract RootHttpHandler and DelegatingHandlers out of it.
            var deploymentClientRestClient = RestClient
                .Configure()
                .WithEnvironment(restClient.Environment)
                .WithCredentials(restClient.Credentials)
                //.WithLogLevel(HttpLoggingDelegatingHandler.Level.BodyAndHeaders)
                .Build();

            _deploymentClient = new AzureDeploymentManagerClient(
                deploymentClientRestClient.Credentials,
                deploymentClientRestClient.RootHttpHandler,
                deploymentClientRestClient.Handlers.ToArray()
            ) {
                SubscriptionId = subscriptionId
            };
        }

        public void Dispose() {
            if (null != _deploymentClient) {
                _deploymentClient.Dispose();
            }
        }
    }
}
