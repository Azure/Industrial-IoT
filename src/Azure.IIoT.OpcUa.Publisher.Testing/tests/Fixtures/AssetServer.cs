// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Fixtures
{
    using Furly.Extensions.Logging;
    using Microsoft.Extensions.Logging;
    using Opc.Ua.Server;
    using Opc.Ua.Test;
    using System.Collections.Generic;

    /// <summary>
    /// Asset server fixture
    /// </summary>
    public class AssetServer : BaseServerFixture
    {
        /// <summary>
        /// Sample server nodes
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="timeservice"></param>
        public static IEnumerable<INodeManagerFactory> Asset(
            ILoggerFactory? factory, TimeService timeservice)
        {
            var logger = (factory ?? Log.ConsoleFactory())
                .CreateLogger<Asset.AssetServer>();
            yield return new Asset.AssetServer(logger);
        }

        /// <inheritdoc/>
        public AssetServer() : base(Asset)
        {
        }

        /// <inheritdoc/>
        private AssetServer(ILoggerFactory loggerFactory)
            : base(Asset, loggerFactory)
        {
        }

        /// <inheritdoc/>
        public static AssetServer Create(ILoggerFactory loggerFactory)
        {
            return new AssetServer(loggerFactory);
        }
    }
}
