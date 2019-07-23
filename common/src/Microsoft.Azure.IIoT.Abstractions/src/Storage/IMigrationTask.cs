// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage {
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a migration task to run
    /// </summary>
    public interface IMigrationTask {

        /// <summary>
        /// Migrate
        /// </summary>
        /// <returns></returns>
        Task MigrateAsync();
    }
}
