// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace FileSystem
{
    using Opc.Ua;
    using Opc.Ua.Server;

    /// <inheritdoc/>
    public class FileSystemServer : INodeManagerFactory
    {
        /// <inheritdoc/>
        public StringCollection NamespacesUris
        {
            get
            {
                return new StringCollection {
                    Namespaces.FileSystem
                };
            }
        }

        /// <inheritdoc/>
        public INodeManager Create(IServerInternal server,
            ApplicationConfiguration configuration)
        {
            return new FileSystemNodeManager(server, configuration);
        }
    }
}
