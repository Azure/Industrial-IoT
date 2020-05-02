// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Storage {
    using Microsoft.Azure.IIoT.Storage;
    using Microsoft.AspNetCore.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System;
    using Serilog;

    /// <summary>
    /// Uses session cache as <see cref="ICache"/> implementation.
    /// </summary>
    /// <remarks>
    /// For this session cache to work effectively the aspnetcore session has
    /// to be configured properly. The latest guidance is provided at
    /// https://docs.microsoft.com/aspnet/core/fundamentals/app-state
    ///
    /// // In ConfigureServices(IServiceCollection services) add
    /// services.AddSession(option =>
    /// {
    ///	    option.Cookie.IsEssential = true;
    /// });
    ///
    /// // In Configure(IApplicationBuilder app, IHostingEnvironment env) add
    /// app.UseSession(); // Before UseMvc()
    /// </remarks>
    public class HttpContextSessionCache : ICache {

        /// <summary>
        /// Create cache
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="logger"></param>
        public HttpContextSessionCache(IHttpContextAccessor ctx, ILogger logger) {
            _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        /// <inheritdoc/>
        public async Task SetAsync(string key, byte[] value,
            DateTimeOffset expiration, CancellationToken ct) {
            kSessionLock.EnterWriteLock();
            try {
                // Reflect changes in the persistent store
                _ctx.HttpContext.Session.Set(key, value);

                // TODO: Handle expiration

                await _ctx.HttpContext.Session.CommitAsync(ct);
            }
            finally {
                kSessionLock.ExitWriteLock();
            }
        }

        /// <inheritdoc/>
        public async Task<byte[]> GetAsync(string key, CancellationToken ct) {
            await _ctx.HttpContext.Session.LoadAsync(ct);
            kSessionLock.EnterReadLock();
            try {
                if (!_ctx.HttpContext.Session.TryGetValue(key, out var blob)) {
                    _logger.Information("CacheId {key} not found in session {sessionId}",
                        key, _ctx.HttpContext.Session.Id);
                }
                return blob;
            }
            finally {
                kSessionLock.ExitReadLock();
            }
        }

        /// <inheritdoc/>
        public async Task RemoveAsync(string key, CancellationToken ct) {
            kSessionLock.EnterWriteLock();
            try {
                // Reflect changes in the persistent store
                _ctx.HttpContext.Session.Remove(key);
                await _ctx.HttpContext.Session.CommitAsync(ct);
            }
            finally {
                kSessionLock.ExitWriteLock();
            }
        }

        private static readonly ReaderWriterLockSlim kSessionLock =
            new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private readonly IHttpContextAccessor _ctx;
        private readonly ILogger _logger;
    }
}
