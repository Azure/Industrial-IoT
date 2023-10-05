// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Opc.Ua;

    /// <summary>
    /// Provides application configuration
    /// </summary>
    public interface IOpcUaConfiguration
    {
        /// <summary>
        /// Validation events
        /// </summary>
        event CertificateValidationEventHandler Validate;

        /// <summary>
        /// Gets the configuration for the clients
        /// </summary>
        ApplicationConfiguration Value { get; }
    }
}
