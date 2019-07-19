// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage {
    using System;
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents an archive
    /// </summary>
    public interface IArchive : IDisposable {

        /// <summary>
        /// Opens a stream to read or write a part in the
        /// archive.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="access"></param>
        /// <returns></returns>
        Stream GetStream(string name, FileMode access);

        /// <summary>
        /// Close the archive
        /// </summary>
        /// <returns></returns>
        Task CloseAsync();
    }
}
