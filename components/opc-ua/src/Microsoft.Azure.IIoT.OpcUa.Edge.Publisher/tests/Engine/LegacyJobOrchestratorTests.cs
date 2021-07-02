// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Tests.Engine {
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Agent.Framework;
    using Agent.Framework.Models;
    using Diagnostics;
    using Models;
    using Module;
    using Moq;
    using Publisher.Engine;
    using Serializers.NewtonSoft;
    using Xunit;
    using static Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Agent.PublisherJobsConfiguration;

    /// <summary>
    /// Tests the LegacyJobOrchestrator class
    /// </summary>
    public class LegacyJobOrchestratorTests {

        [Fact]
        public async Task GetAvailableJobAsyncMulithreading() {
            var legacyCliModelProviderMock = new Mock<ILegacyCliModelProvider>();
            var agentConfigProviderMock = new Mock<IAgentConfigProvider>();
            var identityMock = new Mock<IIdentity>();
            var newtonSoftJsonSerializer = new NewtonSoftJsonSerializer();
            var jobSerializer = new PublisherJobSerializer(newtonSoftJsonSerializer);
            var publishedNodesJobConverter = new PublishedNodesJobConverter(TraceLogger.Create(), newtonSoftJsonSerializer);

            var legacyCliModel = new LegacyCliModel { PublishedNodesFile = "Engine/publishednodes.json", PublishedNodesSchemaFile = "Storage/publishednodesschema.json" };
            legacyCliModelProviderMock.Setup(p => p.LegacyCliModel).Returns(legacyCliModel);
            agentConfigProviderMock.Setup(p => p.Config).Returns(new AgentConfigModel());

            var converter = new LegacyJobOrchestrator(publishedNodesJobConverter, legacyCliModelProviderMock.Object, agentConfigProviderMock.Object, jobSerializer, TraceLogger.Create(), identityMock.Object);

            var tasks = new List<Task<JobProcessingInstructionModel>>();
            for (var i = 0; i < 10; i++) {
                tasks.Add(converter.GetAvailableJobAsync(i.ToString(), new JobRequestModel()));
            }

            await Task.WhenAll(tasks);

            Assert.Equal(2, tasks.Count(t => t.Result != null));
            var distinctConfigurations = tasks
                .Where(t => t.Result != null)
                .Select(t => t.Result.Job.JobConfiguration)
                .Distinct();
            Assert.Equal(2, distinctConfigurations.Count());
        }

        [Fact]
        public void Test_PnJson_With_Multiple_Jobs_Expect_DifferentJobIds() {
            var legacyCliModelProviderMock = new Mock<ILegacyCliModelProvider>();
            var agentConfigProviderMock = new Mock<IAgentConfigProvider>();
            var identityMock = new Mock<IIdentity>();
            var newtonSoftJsonSerializer = new NewtonSoftJsonSerializer();
            var jobSerializer = new PublisherJobSerializer(newtonSoftJsonSerializer);
            var publishedNodesJobConverter = new PublishedNodesJobConverter(TraceLogger.Create(), newtonSoftJsonSerializer);

            var legacyCliModel = new LegacyCliModel { PublishedNodesFile = "Engine/pn_assets.json", PublishedNodesSchemaFile = "Storage/publishednodesschema.json" };
            legacyCliModelProviderMock.Setup(p => p.LegacyCliModel).Returns(legacyCliModel);
            agentConfigProviderMock.Setup(p => p.Config).Returns(new AgentConfigModel());

            var converter = new LegacyJobOrchestrator(publishedNodesJobConverter, legacyCliModelProviderMock.Object, agentConfigProviderMock.Object, jobSerializer, TraceLogger.Create(), identityMock.Object);

            var job1 = converter.GetAvailableJobAsync(1.ToString(), new JobRequestModel()).GetAwaiter().GetResult();
            Assert.NotNull(job1);
            var job2 = converter.GetAvailableJobAsync(2.ToString(), new JobRequestModel()).GetAwaiter().GetResult();
            Assert.NotNull(job2);
            var job3 = converter.GetAvailableJobAsync(3.ToString(), new JobRequestModel()).GetAwaiter().GetResult();
            Assert.Null(job3);

            Assert.NotEqual(job1.Job.Id, job2.Job.Id);
        }
    }
}
