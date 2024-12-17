// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client;

using Opc.Ua;
using System;
using System.Security.Cryptography.X509Certificates;

/// <summary>
/// Creates channels and provides ability to monitor them
/// </summary>
public interface IChannelFactory
{
    /// <summary>
    /// Callback to register and receive channel diagnostics
    /// </summary>
    event Action<ITransportChannel, ChannelDiagnostic>? OnDiagnostics;

    /// <summary>
    /// Create new channel
    /// </summary>
    /// <param name="endpoint"></param>
    /// <param name="context"></param>
    /// <param name="clientCertificate"></param>
    /// <param name="clientCertificateChain"></param>
    /// <param name="connection"></param>
    /// <returns></returns>
    ITransportChannel CreateChannel(ConfiguredEndpoint endpoint,
        IServiceMessageContext context, X509Certificate2? clientCertificate,
        X509Certificate2Collection? clientCertificateChain,
        ITransportWaitingConnection? connection = null);

    /// <summary>
    /// Close channel
    /// </summary>
    /// <param name="channel"></param>
    void CloseChannel(ITransportChannel channel);
}
