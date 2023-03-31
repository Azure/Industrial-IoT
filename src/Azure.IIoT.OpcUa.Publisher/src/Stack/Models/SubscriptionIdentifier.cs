// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Models
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Subscription identifier
    /// </summary>
    public sealed class SubscriptionIdentifier : IEquatable<SubscriptionIdentifier>
    {
        /// <summary>
        /// Id of the subscription
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Connection configuration
        /// </summary>
        public ConnectionModel Connection => _id.Connection;

        /// <summary>
        /// Create identifier
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="id"></param>
        public SubscriptionIdentifier(ConnectionModel connection, string id)
        {
            Id = id;
            _id = new ConnectionIdentifier(connection);
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
            var hashCode = 2082053542;
            hashCode = (hashCode * -1521134295) +
                Connection.CreateConsistentHash();
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(Id);
            return hashCode;
        }

        /// <inheritdoc/>
        public bool Equals(SubscriptionIdentifier? other)
        {
            return Connection.IsSameAs(other?.Connection) &&
                Id == other?.Id;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{Connection.CreateConnectionId()}:{Id}";
        }

        private readonly ConnectionIdentifier _id;
    }
}
