// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Opc.Ua;
    using Opc.Ua.Security.Certificates;
    using System;

    /// <summary>
    /// Provides application configuration
    /// </summary>
    public interface IOpcUaConfiguration
    {
        /// <summary>
        /// Decides whether to accept a peer certificate that failed
        /// validation. Returning true accepts it for this validation.
        /// </summary>
        /// <remarks>
        /// This is the stack's own <c>ICertificateManager.AcceptError</c>
        /// shape. It was previously a multicast event with an args object
        /// carrying <c>Accept</c> and <c>AcceptAll</c> in and out - but there
        /// has only ever been one subscriber, and the two flags were folded
        /// back into a single boolean before the stack saw them, so neither
        /// the multicast nor the distinction ever meant anything.
        /// </remarks>
        Func<Certificate, ServiceResult, bool>? AcceptError { get; set; }

        /// <summary>
        /// Gets the configuration for the clients
        /// </summary>
        ApplicationConfiguration Value { get; }
    }
}
