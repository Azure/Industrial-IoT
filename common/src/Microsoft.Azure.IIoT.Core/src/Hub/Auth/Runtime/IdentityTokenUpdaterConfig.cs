// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Identity token update configuration
    /// </summary>
    public class IdentityTokenUpdaterConfig : ConfigBase, IIdentityTokenUpdaterConfig {
        /// <summary>
        /// Configuration keys
        /// </summary>
        private const string kTokenLengthKey = "TokenLength";
        private const string kTokenLifetimeKey = "TokenLifetime";
        private const string kUpdateIntervalKey = "UpdateInterval";

        /// <inheritdoc/>
        public int TokenLength => GetIntOrDefault(kTokenLengthKey, () => 64);

        /// <inheritdoc/>
        public TimeSpan TokenLifetime =>
            GetDurationOrDefault(kTokenLifetimeKey, () => TimeSpan.FromMinutes(15));

        /// <inheritdoc/>
        public TimeSpan UpdateInterval =>
            GetDurationOrDefault(kUpdateIntervalKey, () => TimeSpan.FromMinutes(5));

        /// <inheritdoc/>
        public TimeSpan TokenStaleInterval => TokenLifetime - UpdateInterval;

        /// <summary>
        /// Create config
        /// </summary>
        /// <param name="configuration"></param>
        public IdentityTokenUpdaterConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}