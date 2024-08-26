// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Fixtures
{
    using Microsoft.Extensions.Logging;
    using Opc.Ua.Server;
    using Opc.Ua.Test;
    using System.Collections.Generic;

    /// <summary>
    /// Sample server fixture
    /// </summary>
    public class FileSystemServer : BaseServerFixture
    {
        /// <summary>
        /// Sample server nodes
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="timeservice"></param>
        public static IEnumerable<INodeManagerFactory> FileSystem(
            ILoggerFactory? factory, TimeService timeservice)
        {
            yield return new FileSystem.FileSystemServer();
        }

        /// <inheritdoc/>
        public FileSystemServer() : base(FileSystem)
        {
        }

        /// <inheritdoc/>
        private FileSystemServer(ILoggerFactory loggerFactory)
            : base(FileSystem, loggerFactory)
        {
        }

        /// <inheritdoc/>
        public static FileSystemServer Create(ILoggerFactory loggerFactory)
        {
            return new FileSystemServer(loggerFactory);
        }
    }
}
