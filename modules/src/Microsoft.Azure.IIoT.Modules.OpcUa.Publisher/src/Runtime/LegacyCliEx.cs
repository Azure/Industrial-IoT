using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Runtime {
    /// <summary>
    /// Extensions for the Legacy Command Line 
    /// </summary>
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
