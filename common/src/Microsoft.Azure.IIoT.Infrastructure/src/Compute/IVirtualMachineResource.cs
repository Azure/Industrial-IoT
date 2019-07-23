// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.Compute {
    using Microsoft.Azure.IIoT.Net;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A managed virtual machine resource
    /// </summary>
    public interface IVirtualMachineResource : IResource {

        /// <summary>
        /// Root user
        /// </summary>
        string User { get; }

        /// <summary>
        /// Root password
        /// </summary>
        string Password { get; }

        /// <summary>
        /// Public ip address when added.
        /// </summary>
        string PublicIPAddress { get; }

        /// <summary>
        /// Add a public ip to the vm.
        /// </summary>
        /// <returns></returns>
        Task AddPublicIPAddressAsync();

        /// <summary>
        /// Opens a shell to the vm.  Will add
        /// a public IP if none has been added.
        /// </summary>
        /// <param name="port">Port to connect
        /// to.</param>
        /// <param name="ct">Cancel opening or
        /// else it will run forever</param>
        /// <returns></returns>
        Task<ISecureShell> OpenShellAsync(int port,
            CancellationToken ct);

        /// <summary>
        /// Remove public IP address from the vm.
        /// </summary>
        /// <returns></returns>
        Task RemovePublicIPAddressAsync();

        /// <summary>
        /// Restart machine
        /// </summary>
        /// <returns></returns>
        Task RestartAsync();
    }
}
