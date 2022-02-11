// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Runtime {
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Extensions for the standalone command line
    /// </summary>
    public static class StandaloneCliEx {
        /// <summary>
        /// Add options
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="args"></param>
        public static IConfigurationBuilder AddStandalonePublisherCommandLine(this IConfigurationBuilder builder, string[] args) {
            return builder.AddInMemoryCollection(new StandaloneCliOptions(args));
        }
    }
}
