// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace FileSystem
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Stores the configuration the file system node manager.
    /// </summary>
    [DataContract(Namespace = Namespaces.FileSystem)]
    public class FileSystemServerConfiguration
    {
        /// <summary>
        /// The default constructor.
        /// </summary>
        public FileSystemServerConfiguration()
        {
        }
    }
}
