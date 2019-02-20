// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage {
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a location where an archive is stored.
    /// </summary>
    public interface IArchiveStorage {

        /// <summary>
        /// Opens an archive
        /// </summary>
        /// <param name="path"></param>
        /// <param name="mode"></param>
        /// <param name="access"></param>
        /// <returns></returns>
        Task<IArchive> OpenAsync(string path,
            FileMode mode = FileMode.CreateNew,
            FileAccess access = FileAccess.ReadWrite);
    }
}
