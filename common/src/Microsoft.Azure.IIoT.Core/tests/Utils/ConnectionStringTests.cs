namespace Microsoft.Azure.IIoT.Core.Tests.Utils {
    using IIoT.Utils;
    using Xunit;

    public class ConnectionStringTests {
        private const string _connectionStringWithoutGateway =
            "HostName=iothub-012345.azure-devices.net;DeviceId=0123456789012345678901234567890123456789012345;SharedAccessKey=aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa=;";
        private const string _connectionStringInvalidGateway =
            "HostName=iothub-012345.azure-devices.net;DeviceId=0123456789012345678901234567890123456789012345;SharedAccessKey=aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa;GatewayHostName=";
        private const string _connectionStringWithGateway =
            "HostName=iothub-012345.azure-devices.net;DeviceId=0123456789012345678901234567890123456789012345;SharedAccessKey=aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa=;GatewayHostName=myGatewayHost";
        
        [Fact]
        public void Test_ConnectionStringWithGatewayHostName_ExpectCaParse() {
            Assert.True(ConnectionString.TryParse(_connectionStringWithGateway, out var cs));
            Assert.NotNull(cs);
        }

        [Fact]
        public void Test_ConnectionStringWithGatewayHostName_ExpectGatewaySet() {
            Assert.True(ConnectionString.TryParse(_connectionStringWithGateway, out var cs));
            Assert.NotNull(cs.GatewayHostName);
            Assert.Equal("myGatewayHost", cs.GatewayHostName);
        }

        [Fact]
        public void Test_ConnectionStringWithoutGatewayHostName_ExpectNull() {
            Assert.True(ConnectionString.TryParse(_connectionStringWithoutGateway, out var cs));
            Assert.Null(cs.GatewayHostName);
        }

        [Fact]
        public void Test_ConnectionStringWithInvalidGatewayHostName_ExpectCantParse() {
            Assert.False(ConnectionString.TryParse(_connectionStringInvalidGateway, out var cs));
        }

        [Fact]
        public void Test_ConnectionStringWithGatewayHostName_ExpectToStringGenerateOriginal() {
            Assert.True(ConnectionString.TryParse(_connectionStringWithGateway, out var cs));
            Assert.Equal(_connectionStringWithGateway, cs.ToString());
        }
    }
}
