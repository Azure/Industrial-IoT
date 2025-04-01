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
    /// Isa95Jobs fixture
    /// </summary>
    public class Isa95JobsServer : BaseServerFixture
    {
        /// <inheritdoc/>
        public static IEnumerable<INodeManagerFactory> Isa95Jobs(
            ILoggerFactory? factory, TimeService timeservice)
        {
            yield return new Isa95Jobs.Isa95JobControlServer();
        }

        /// <inheritdoc/>
        public Isa95JobsServer()
            : base(Isa95Jobs)
        {
        }

        /// <inheritdoc/>
        private Isa95JobsServer(ILoggerFactory loggerFactory)
            : base(Isa95Jobs, loggerFactory)
        {
        }

        /// <inheritdoc/>
        public static Isa95JobsServer Create(ILoggerFactory loggerFactory)
        {
            return new Isa95JobsServer(loggerFactory);
        }
    }
}
