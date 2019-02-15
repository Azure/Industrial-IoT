namespace OpcPublisher
{
    using Xunit;

    /// <summary>
    /// Collection of tests which require the PLC container and OPC Publisher configuration.
    /// </summary>
    [CollectionDefinition("Need PLC and publisher config")]
    public class PlcAndAppConfigCollection : ICollectionFixture<TestDirectoriesFixture>, ICollectionFixture<PlcOpcUaServerFixture>, ICollectionFixture<OpcPublisherFixture>
    {
    }
}
