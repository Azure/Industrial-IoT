using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Runtime {
    public static class LegacyCliEx {
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
