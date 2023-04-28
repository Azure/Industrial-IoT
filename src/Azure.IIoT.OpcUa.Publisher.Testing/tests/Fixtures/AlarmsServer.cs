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
    /// Alarms server fixture
    /// </summary>
    public class AlarmsServer : BaseServerFixture
    {
        /// <summary>
        /// Alarm server nodes
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="timeservice"></param>
        public static IEnumerable<INodeManagerFactory> Alarms(
            ILoggerFactory? factory, TimeService timeservice)
        {
            yield return new Alarms.AlarmConditionServer(timeservice);
        }

        /// <inheritdoc/>
        public AlarmsServer()
            : base(Alarms)
        {
        }

        /// <inheritdoc/>
        private AlarmsServer(ILoggerFactory loggerFactory)
            : base(Alarms, loggerFactory)
        {
        }

        /// <inheritdoc/>
        public static AlarmsServer Create(ILoggerFactory loggerFactory)
        {
            return new AlarmsServer(loggerFactory);
        }
    }
}
