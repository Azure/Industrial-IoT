// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Runtime {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Service auth configuration
    /// </summary>
    public class ServiceAuthAggregateConfig : ConfigBase, IServerAuthConfig {

        /// <summary>
        /// Auth configuration
        /// </summary>
        private const string kAuth_RequiredKey = "Auth:Required";

        /// <inheritdoc/>
        public bool AllowAnonymousAccess => !GetBoolOrDefault(kAuth_RequiredKey,
            () => GetBoolOrDefault(PcsVariable.PCS_AUTH_REQUIRED,
                () => true));

        /// <inheritdoc/>
        public IEnumerable<IOAuthServerConfig> JwtBearerProviders { get; }

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="providers"></param>
        public ServiceAuthAggregateConfig(IConfiguration configuration,
            IEnumerable<IOAuthServerConfig> providers) :
            base(configuration) {
            JwtBearerProviders = providers?.Where(s => s.IsValid).ToList()
                ?? throw new ArgumentNullException(nameof(providers));
        }
    }
}
