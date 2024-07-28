// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Models
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Opc.Ua.Client;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Subscription identifier
    /// </summary>
    public sealed class SubscriptionIdentifier : IEquatable<SubscriptionIdentifier>
    {
        /// <summary>
        /// Connection configuration
        /// </summary>
        public ConnectionModel Connection => _id.Connection;

        /// <summary>
        /// Subscription configuration
        /// </summary>
        public SubscriptionModel Subscription { get; }

        /// <summary>
        /// Create identifier
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="subscription"></param>
        public SubscriptionIdentifier(ConnectionModel connection,
            SubscriptionModel subscription)
        {
            _id = new ConnectionIdentifier(connection);
            Subscription = subscription;
            _hash = HashCode.Combine(_id.GetHashCode(), subscription);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj is not SubscriptionIdentifier that)
            {
                return false;
            }
            return that.Equals(this);
        }

        /// <inheritdoc/>
        public static bool operator ==(SubscriptionIdentifier r1,
            SubscriptionIdentifier r2) =>
            EqualityComparer<SubscriptionIdentifier>.Default.Equals(r1, r2);
        /// <inheritdoc/>
        public static bool operator !=(SubscriptionIdentifier r1,
            SubscriptionIdentifier r2) =>
            !(r1 == r2);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return _hash;
        }

        /// <inheritdoc/>
        public bool Equals(SubscriptionIdentifier? other)
        {
            return other is not null && 
                _id.Equals(other._id) &&
                Subscription == other.Subscription;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{Connection.CreateConnectionId()}:{_hash}";
        }

        private readonly int _hash;
        private readonly ConnectionIdentifier _id;
    }
}
