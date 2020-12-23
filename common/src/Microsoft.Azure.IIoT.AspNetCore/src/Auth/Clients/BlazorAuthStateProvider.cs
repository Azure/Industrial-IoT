// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Auth.Clients {
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.AspNetCore.Components.Server;
    using Microsoft.AspNetCore.Components.Authorization;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Threading;
    using System.Security.Claims;

    /// <summary>
    /// Revalidate all user token providers
    /// </summary>
    public class BlazorAuthStateProvider : ServerAuthenticationStateProvider, ITokenClient,
        IDisposable {

        /// <summary>
        /// Create auth state provider
        /// </summary>
        /// <param name="clients"></param>
        /// <param name="logger"></param>
        /// <param name="config"></param>
        public BlazorAuthStateProvider(IEnumerable<IUserTokenClient> clients, ILogger logger,
            IAuthStateProviderConfig config = null) : base() {
            _clients = clients?.ToList() ?? throw new ArgumentNullException(nameof(clients));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config;
            // Whenever we receive notification of a new authentication state, cancel any
            // existing revalidation loop and start a new one
            AuthenticationStateChanged += authenticationStateTask => {
                _cts?.Cancel();
                _cts = new CancellationTokenSource();
                _ = RevalidationLoopAsync(authenticationStateTask, _cts.Token);
            };
        }

        /// <inheritdoc/>
        public void Dispose() {
            _cts?.Cancel();
            _logger.Debug("Token revalidation loop exit.");
        }

        /// <inheritdoc/>
        public bool Supports(string resource) {
            foreach (var client in _clients) {
                if (client is ITokenProvider p && p.Supports(resource)) {
                    return true;
                }
            }
            return false;
        }

        /// <inheritdoc/>
        public async Task<TokenResultModel> GetTokenForAsync(string resource, IEnumerable<string> scopes) {
            var authenticationState = await GetAuthenticationStateAsync();
            if (authenticationState?.User == null || !authenticationState.User.Identity.IsAuthenticated) {
                return null;
            }
            var exceptions = new List<Exception>();
            foreach (var client in _clients) {
                try {
                    // TODO: Compare provider name and identity and only check those that match.
                    var result = await client.GetUserTokenAsync(authenticationState.User, scopes);
                    if (result?.RawToken != null) {
                        return result;
                    }
                }
                catch (Exception ex) {
                    exceptions.Add(ex);
                }
            }
            if (exceptions.Count != 0) {
                throw new AggregateException(exceptions);
            }
            return null;
        }

        /// <inheritdoc/>
        public async Task InvalidateAsync(string resource) {
            try {
                var authenticationState = await GetAuthenticationStateAsync();
                if (authenticationState?.User == null) {
                    return;
                }
                foreach (var client in _clients) {
                    try {
                        // TODO: Compare provider name and identity and only check those that match.
                        await client.SignOutUserAsync(authenticationState.User);
                    }
                    catch {
                        continue;
                    }
                }
            }
            catch {
            }
            finally {
                ForceSignOut();
            }
        }

        /// <summary>
        /// Validate
        /// </summary>
        /// <param name="authenticationStateTask"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task RevalidationLoopAsync(Task<AuthenticationState> authenticationStateTask,
            CancellationToken ct) {
            try {
                var authenticationState = await authenticationStateTask;
                _logger.Debug("Starting token revalidation loop");
                if (authenticationState.User.Identity.IsAuthenticated) {
                    while (!ct.IsCancellationRequested) {
                        bool isValid;
                        try {
                            await Task.Delay(_config?.RevalidateInterval ?? TimeSpan.FromSeconds(10), ct);
                            _logger.Debug("Testing token still valid...");
                            isValid = await ValidateAuthenticationStateAsync(authenticationState, ct);
                        }
                        catch (TaskCanceledException tce) {
                            // If it was our cancellation token, then this revalidation loop gracefully completes
                            // Otherwise, treat it like any other failure
                            if (tce.CancellationToken == ct) {
                                break;
                            }
                            throw;
                        }
                        if (!isValid) {
                            _logger.Information("Token invalid - signing out...");
                            ForceSignOut();
                            break;
                        }
                    }
                }
            }
            catch  {
                ForceSignOut();
            }
        }

        /// <summary>
        /// Force sign out
        /// </summary>
        private void ForceSignOut() {
            var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
            var anonymousState = new AuthenticationState(anonymousUser);
            SetAuthenticationState(Task.FromResult(anonymousState));
        }

        /// <summary>
        /// Validate state
        /// </summary>
        /// <param name="authenticationState"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<bool> ValidateAuthenticationStateAsync(
            AuthenticationState authenticationState, CancellationToken cancellationToken) {
            foreach (var client in _clients) {
                try {
                    // TODO: Compare provider name and identity and only check those that match.

                    var result = await client.GetUserTokenAsync(
                        authenticationState.User, string.Empty.YieldReturn());
                    if (result?.RawToken != null) {
                        return true;
                    }
                }
                catch {
                    continue;
                }
            }
            return false;
        }

        private readonly ILogger _logger;
        private readonly IAuthStateProviderConfig _config;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly IEnumerable<IUserTokenClient> _clients;
    }
}
