namespace IIoTPlatform_E2E_Tests.Config
{
    /// <summary>
    /// The configuration of the TestEventProcessor
    /// </summary>
    public interface ITestEventProcessorConfig {
        /// <summary>
        /// The base url of the TestEventProcessor service
        /// </summary>
        public string TestEventProcessorBaseUrl { get; }

        /// <summary>
        /// The username to authenticate to the TestEventProcessor (Basic Auth)
        /// </summary>
        public string TestEventProcessorUsername { get; }

        /// <summary>
        /// The password to authenticate to the TestEventProcessor (Basic Auth)
        /// </summary>
        public string TestEventProcessorPassword { get; }
    }
}