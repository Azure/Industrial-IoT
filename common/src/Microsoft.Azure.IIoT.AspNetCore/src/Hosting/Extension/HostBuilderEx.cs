// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Autofac.Extensions.Hosting {
    using Autofac.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    /// <summary>
    /// Use autofac
    /// </summary>
    public static class HostBuilderEx {

        /// <summary>
        /// Add autofac
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IHostBuilder UseAutofac(this IHostBuilder builder) {
            return builder.UseServiceProviderFactory(new AutofacServiceProviderFactory());
        }
    }
}