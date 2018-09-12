// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure {
    using System.Threading.Tasks;

    /// <summary>
    /// Azure resource
    /// </summary>
    public interface IResource {

        /// <summary>
        /// Name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Delete resource
        /// </summary>
        /// <returns></returns>
        Task DeleteAsync();
    }
}
