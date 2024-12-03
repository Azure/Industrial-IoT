// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using BitFaster.Caching;
    using System;

    /// <summary>
    /// A pooled session wraps a session from the session pool.
    /// Disposing the client returns the session to the pool.
    /// </summary>
    public sealed class PooledSession : IDisposable
    {
        /// <summary>
        /// Session reference valid until disposed.
        /// </summary>
        public ISession Session => _lifetime.Value;

        /// <inheritdoc/>
        internal PooledSession(Lifetime<ISession> lifetime)
        {
            _lifetime = lifetime;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _lifetime.Dispose();
        }

        private readonly Lifetime<ISession> _lifetime;
    }
}
