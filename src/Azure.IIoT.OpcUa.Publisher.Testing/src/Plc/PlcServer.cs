// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Plc
{
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using Opc.Ua.Server;
    using Opc.Ua.Test;

    /// <inheritdoc/>
    public class PlcServer : INodeManagerFactory
    {
        /// <inheritdoc/>
        public StringCollection NamespacesUris
        {
            get
            {
                return [
                    Namespaces.PlcApplications,
                    Namespaces.PlcSimulation,
                    Namespaces.PlcInstance
                ];
            }
        }

        /// <inheritdoc/>
        public PlcServer(TimeService timeservice, ILogger logger, uint scaleunits)
        {
            _timeservice = timeservice;
            _logger = logger;
            _scaleunits = scaleunits;
        }

        /// <inheritdoc/>
        public INodeManager Create(IServerInternal server,
            ApplicationConfiguration configuration)
        {
            return new PlcNodeManager(server, configuration, _timeservice, _logger, _scaleunits);
        }

        private readonly TimeService _timeservice;
        private readonly ILogger _logger;
        private readonly uint _scaleunits;
    }
}
