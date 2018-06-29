// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AspNetCore.Authorization {
    using System;
    using System.Collections.Generic;

    public static class AuthorizationPolicyBuilderEx {

        /// <summary>
        /// Require assertion
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="handler"></param>
        public static AuthorizationPolicyBuilder Require(
            this AuthorizationPolicyBuilder builder,
            Func<AuthorizationHandlerContext, bool> handler) =>
            builder.RequireAssertion(ctx => handler(ctx));

        /// <summary>
        /// Require nothing
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static AuthorizationPolicyBuilder RequireNothing(
            this AuthorizationPolicyBuilder builder) =>
            builder.RequireAssertion(ctx => true);

        /// <summary>
        /// Add no op policies
        /// </summary>
        /// <param name="options"></param>
        /// <param name="policies"></param>
        public static void AddNoOpPolicies(this AuthorizationOptions options, 
            IEnumerable<string> policies) {
            foreach(var policy in policies) {
                options.AddPolicy(policy, builder => builder.RequireNothing());
            }
        }
    }
}
