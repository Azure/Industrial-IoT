// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Runtime {
    /// <summary>
    ///     Cli handling
    /// </summary>
    public static class LegacyCommandLineEx {
        /// <summary>
        ///     Add options
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="args"></param>
        public static IConfigurationBuilder AddLegacyPublisherCommandLine(this IConfigurationBuilder builder, string[] args) {
            return builder.AddInMemoryCollection(new LegacyCliOptions(args));
        }
    }
}