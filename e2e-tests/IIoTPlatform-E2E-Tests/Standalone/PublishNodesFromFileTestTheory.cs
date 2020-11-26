using System;
using System.Collections.Generic;
using System.Text;

namespace IIoTPlatform_E2E_Tests.Standalone {
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;
    using Newtonsoft.Json;
    using RestSharp;
    using TestExtensions;
    using Xunit.Abstractions;
    using System.Threading;
    using System.IO;

    /// <summary>
    /// The test theory using different (ordered) test cases to go thru all required steps of publishing OPC UA node
    /// </summary>
    [TestCaseOrderer("IIoTPlatform_E2E_Tests.TestExtensions.TestOrderer", TestConstants.TestAssemblyName)]
    [Collection("IIoT Platform Test Collection")]
    public class PublishNodesFromFileTestTheory {
        private readonly ITestOutputHelper _output;
        private readonly IIoTPlatformTestContext _context;

        public PublishNodesFromFileTestTheory(IIoTPlatformTestContext context, ITestOutputHelper output) {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.OutputHelper = _output;
        }

        [Fact, PriorityOrder(1)]
        public async Task Test_ReadSimulatedOpcUaNodes() {
            var simulatedOpcServer = await TestHelper.GetSimulatedOpcUaNodesAsync(_context);
            Assert.NotNull(simulatedOpcServer);
            Assert.NotEmpty(simulatedOpcServer.Keys);
            Assert.NotEmpty(simulatedOpcServer.Values);
        }

        [Fact, PriorityOrder(2)]
        public async Task SwitchToStandaloneMode() {
            var simulatedOpcServer = await TestHelper.GetSimulatedOpcUaNodesAsync(_context);
            TestHelper.SavePublishedNodesFile(simulatedOpcServer, simulatedOpcServer.Keys.First());
            var dir = Directory.GetCurrentDirectory() + "/" + TestConstants.PublisherPublishedNodesFile;
            TestHelper.SwitchToStandaloneMode(_context);
            TestHelper.LoadPublishedNodesFile(dir, "/mount/published_nodes.json", _context);
        }

        [Fact, PriorityOrder(3)]
        public async Task PublishFromPublishedNodesFile() {
            var deploy = new IoTHubPublisherDeployment(_context);
            Assert.NotNull(deploy);

            var result = await deploy.CreateOrUpdateLayeredDeploymentAsync();
            Assert.True(result);
        }
    }
}
