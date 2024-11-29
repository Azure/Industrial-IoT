// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using Opc.Ua.Client.ComplexTypes;
    using Opc.Ua;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Session interface
    /// </summary>
    public interface ISession : ISessionServiceSets, IDisposable
    {
        /// <summary>
        /// Connected?
        /// </summary>
        bool Connected { get; }

        /// <summary>
        /// Endpoint the session is connected to
        /// </summary>
        EndpointDescription Endpoint { get; }

        /// <summary>
        /// Type factory
        /// </summary>
        IEncodeableFactory Factory { get; }

        /// <summary>
        /// User identity of the session
        /// </summary>
        IUserIdentity Identity { get; }

        /// <summary>
        /// Message context
        /// </summary>
        IServiceMessageContext MessageContext { get; }

        /// <summary>
        /// Node cache
        /// </summary>
        INodeCache NodeCache { get; }

        /// <summary>
        /// Subscriptions in the session
        /// </summary>
        ISubscriptionManager Subscriptions { get; }

        /// <summary>
        /// System context (legacy)
        /// </summary>
        ISystemContext SystemContext { get; }

        /// <summary>
        /// Session timeout
        /// </summary>
        TimeSpan SessionTimeout { get; }

        /// <summary>
        /// Operation limits for the session
        /// </summary>
        Limits OperationLimits { get; }

        /// <summary>
        /// Get complex type system
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<ComplexTypeSystem?> GetComplexTypeSystemAsync(
            CancellationToken ct = default);

        /// <summary>
        /// Close session
        /// </summary>
        /// <param name="closeChannel"></param>
        /// <param name="deleteSubscriptions"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<ServiceResult> CloseAsync(bool closeChannel = true,
            bool deleteSubscriptions = true,
            CancellationToken ct = default);
    }
}
