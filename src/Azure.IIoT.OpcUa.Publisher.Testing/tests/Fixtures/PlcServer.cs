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
    /// Plc server fixture
    /// </summary>
    public class PlcServer : BaseServerFixture
    {
        /// <inheritdoc/>
        public static IEnumerable<INodeManagerFactory> Plc(
            ILoggerFactory? factory, TimeService timeservice)
        {
            yield return new Plc.PlcServer(timeservice,
                (factory ?? Log.ConsoleFactory()).CreateLogger<Plc.PlcServer>());
        }

        /// <inheritdoc/>
        public PlcServer()
            : base(Plc)
        {
        }

        /// <inheritdoc/>
        private PlcServer(ILoggerFactory loggerFactory)
            : base(Plc, loggerFactory)
        {
        }

        /// <inheritdoc/>
        public static PlcServer Create(ILoggerFactory loggerFactory)
        {
            return new PlcServer(loggerFactory);
        }
    }
}
