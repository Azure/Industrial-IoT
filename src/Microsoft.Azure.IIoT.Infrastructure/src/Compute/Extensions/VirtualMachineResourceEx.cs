// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.Compute {
    using Microsoft.Azure.IIoT.Net;
    using System.Threading;
    using System.Threading.Tasks;

    public static class VirtualMachineResourceEx {

        /// <summary>
        /// Opens a shell to the vm (default ssh port 22)
        /// </summary>
        /// <param name="ct">Cancel opening or
        /// else it will run forever</param>
        /// <returns></returns>
        public static Task<ISecureShell> OpenShellAsync(
            this IVirtualMachineResource resource, CancellationToken ct) =>
            resource.OpenShellAsync(22, ct);
    }
}
