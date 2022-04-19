// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Infrastructure {

    using Microsoft.Azure.Management.Compute.Fluent;
    using Microsoft.Azure.Management.Compute.Fluent.Models;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    class ComputeMgmtClient : IDisposable {

        private readonly ComputeManagementClient _computeManagementClient;

        public ComputeMgmtClient(
            string subscriptionId,
            RestClient restClient
        ) {
            if (string.IsNullOrEmpty(subscriptionId)) {
                throw new ArgumentNullException(nameof(subscriptionId));
            }
            if (restClient is null) {
                throw new ArgumentNullException(nameof(restClient));
            }

            _computeManagementClient = new ComputeManagementClient(restClient) {
                SubscriptionId = subscriptionId
            };
        }

        /// <summary>
        /// Retrieves information about a VM.
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="vmName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<VirtualMachineInner> GetVMAsync(
            IResourceGroup resourceGroup,
            string vmName,
            CancellationToken cancellationToken = default
        ) {
            var vm = await _computeManagementClient
                .VirtualMachines
                .GetAsync(
                    resourceGroup.Name,
                    vmName,
                    cancellationToken: cancellationToken
                );

            return vm;
        }

        /// <summary>
        /// Shuts down the virtual machine and releases the compute resources.
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="vm"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task DeallocateVMAsync(
            IResourceGroup resourceGroup,
            VirtualMachineInner vm,
            CancellationToken cancellationToken = default
        ) {
            await _computeManagementClient
                .VirtualMachines
                .DeallocateAsync(
                    resourceGroup.Name,
                    vm.Name,
                    cancellationToken: cancellationToken
                );
        }

        /// <summary>
        /// Shuts down the virtual machine and releases the compute resources.
        /// This method will not wait for the deallocation to happen.
        /// </summary>
        /// <param name="resourceGroup"></param>
        /// <param name="vm"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task BeginDeallocateVMAsync(
            IResourceGroup resourceGroup,
            VirtualMachineInner vm,
            CancellationToken cancellationToken = default
        ) {
            await _computeManagementClient
                .VirtualMachines
                .BeginDeallocateAsync(
                    resourceGroup.Name,
                    vm.Name,
                    cancellationToken: cancellationToken
                );
        }

        public void Dispose() {
            if (!(_computeManagementClient is null)) {
                _computeManagementClient.Dispose();
            };
        }
    }
}
