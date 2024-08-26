// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Controllers
{
    using Azure.IIoT.OpcUa.Publisher.Module.Filters;
    using Furly.Tunnel.Router;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Legacy controller
    /// </summary>
    [Version("_V2")]
    [Version("_V1")]
    [Version("")]
    [ControllerExceptionFilter]
    public class LegacyController : IMethodController
    {
        /// <summary>
        /// Handler for GetInfo direct method
        /// </summary>
        /// <exception cref="NotSupportedException"></exception>
#pragma warning disable CA1822 // Mark members as static
        public Task GetInfoAsync()
#pragma warning restore CA1822 // Mark members as static
        {
            return Task.FromException(new NotSupportedException(
                "GetInfo not supported"));
        }

        /// <summary>
        /// Handler for GetDiagnosticLog direct method - not supported
        /// </summary>
        /// <exception cref="NotSupportedException"></exception>
#pragma warning disable CA1822 // Mark members as static
        public Task GetDiagnosticLogAsync()
#pragma warning restore CA1822 // Mark members as static
        {
            return Task.FromException(new NotSupportedException(
                "GetDiagnosticLog not supported"));
        }

        /// <summary>
        /// Handler for GetDiagnosticStartupLog direct method - not supported
        /// </summary>
        /// <exception cref="NotSupportedException"></exception>
#pragma warning disable CA1822 // Mark members as static
        public Task GetDiagnosticStartupLogAsync()
#pragma warning restore CA1822 // Mark members as static
        {
            return Task.FromException(new NotSupportedException(
                "GetDiagnosticStartupLog not supported"));
        }
    }
}
