// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.Diagnostics.Metrics;

    /// <summary>
    /// Context for services in the sdk
    /// </summary>
    internal interface IServiceContext
    {
        /// <summary>
        /// Logger factory
        /// </summary>
        ILoggerFactory LoggerFactory { get; }

        /// <summary>
        /// Logger factory
        /// </summary>
        IMeterFactory MeterFactory { get; }

        /// <summary>
        /// Time provider
        /// </summary>
        TimeProvider TimeProvider { get; }
    }
}
