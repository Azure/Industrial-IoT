// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Tests.Worker {
    using Microsoft.Azure.IIoT.Agent.Framework.Agent;
    using System;
    using Xunit;
    using Moq;
    using Autofac;
    using Serilog;
    using System.Threading.Tasks;
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Microsoft.Azure.IIoT.Http.HealthChecks;

    public class WorkerSupervisorTests {

        private class TestWorker : IWorker {

            public TestWorker(int workerInstance) {
                WorkerId = $"Agent_{workerInstance}";
                Status = WorkerStatus.Stopped;
            }

            public string WorkerId { get; private set;}

            public WorkerStatus Status { get; private set; }

            public event JobFinishedEventHandler OnJobCompleted;
            public event JobCanceledEventHandler OnJobCanceled;
            public event JobStartedEventHandler OnJobStarted;

            public Task StartAsync() {
                Status = WorkerStatus.WaitingForJob;
                var args = new JobInfoEventArgs(new JobInfoModel());
                OnJobStarted?.Invoke(this, args);
                Status = WorkerStatus.ProcessingJob;
                Task.Delay(10);
                OnJobCompleted?.Invoke(this, args);
                return Task.Delay(1);
            }

            public Task StopAsync() {
                Status = WorkerStatus.Stopping;
                var args = new JobInfoEventArgs(new JobInfoModel());
                OnJobCanceled?.Invoke(this, args);

                Status = WorkerStatus.Stopped;
                return Task.Delay(1);
            }
        }

        private class TestAgentConfigProvider : IAgentConfigProvider {
            public TestAgentConfigProvider() {
                Config = new AgentConfigModel();
            }
            public AgentConfigModel Config { get; set; }

            public event ConfigUpdatedEventHandler OnConfigUpdated;

            /// <inheritdoc/>
            public void TriggerConfigUpdate(object sender, EventArgs eventArgs) {
                OnConfigUpdated?.Invoke(sender, eventArgs);
            }

            public void SetMaxWorker(int numberOfWorker) {

                Config.MaxWorkers = numberOfWorker;
                OnConfigUpdated?.Invoke(this, EventArgs.Empty);
            }
        }

        [Fact]
        public async Task Test_001_DefaultConfiguration_Expect_FiveWorker() {

            using var container = GetAutofacTestConfiguration();
            var agentConfigMock = new Mock<IAgentConfigProvider>();
            var loggerMock = new Mock<ILogger>();
            var healthCheckManagerMock = new Mock<IHealthCheckManager>();

            var workerSupervisor = new WorkerSupervisor(container.BeginLifetimeScope(), agentConfigMock.Object, loggerMock.Object, healthCheckManagerMock.Object, kSupervisorDelay);

            // start host process
            var sut = workerSupervisor as IWorkerSupervisor;
            Assert.NotNull(sut);
            await sut.StartAsync();

            // Test
            Assert.Equal(kDefaultMaxWorker, sut.NumberOfWorkers);

            // clean up
            await sut.StopAsync();
        }

        [Fact]
        public async Task Test_002_IncreaseMaxWorker_Expect_WorkerSpawned() {

            using var container = GetAutofacTestConfiguration();
            var agentConfig = new TestAgentConfigProvider();
            var loggerMock = new Mock<ILogger>();
            var healthCheckManagerMock = new Mock<IHealthCheckManager>();

            var workerSupervisor = new WorkerSupervisor(container.BeginLifetimeScope(), agentConfig, loggerMock.Object, healthCheckManagerMock.Object, kSupervisorDelay);

            // start host process
            var sut = workerSupervisor as IWorkerSupervisor;
            Assert.NotNull(sut);
            await sut.StartAsync();

            // Test
            const int numberOfWorker = 9;
            agentConfig.SetMaxWorker(numberOfWorker);
            await Task.Delay(kDefaultDelay); //Internal timer which creates the Worker run every 10 seconds
            Assert.Equal(numberOfWorker, sut.NumberOfWorkers);

            // clean up
            await sut.StopAsync();
        }

        [Fact]
        public async Task Test_003_DecreaseMaxWorker_Expect_WorkerTermination() {
            using var container = GetAutofacTestConfiguration();
            var agentConfig = new TestAgentConfigProvider();
            var loggerMock = new Mock<ILogger>();
            var healthCheckManagerMock = new Mock<IHealthCheckManager>();

            var workerSupervisor = new WorkerSupervisor(container.BeginLifetimeScope(), agentConfig, loggerMock.Object, healthCheckManagerMock.Object, kSupervisorDelay);

            // start host process
            var sut = workerSupervisor as IWorkerSupervisor;
            Assert.NotNull(sut);
            await sut.StartAsync();

            // Test
            const int numberOfWorker = 2;
            agentConfig.SetMaxWorker(numberOfWorker);
            await Task.Delay(kDefaultDelay); //Internal timer which creates the Worker run every 10 seconds
            Assert.Equal(numberOfWorker, sut.NumberOfWorkers);

            // clean up
            await sut.StopAsync();
        }

        [Fact]
        public async Task Test_004_NegativeMaxWorker_Expect_UseDefaultValue() {

            using var container = GetAutofacTestConfiguration();
            var agentConfig = new TestAgentConfigProvider();
            var loggerMock = new Mock<ILogger>();
            var healthCheckManagerMock = new Mock<IHealthCheckManager>();
            loggerMock.Setup(l => l.Error(
                It.Is<string>(s => s.Contains("MaxWorker")),
                It.Is<int>(i => i == kDefaultMaxWorker)))
                .Verifiable();

            var workerSupervisor = new WorkerSupervisor(container.BeginLifetimeScope(), agentConfig, loggerMock.Object, healthCheckManagerMock.Object, kSupervisorDelay);

            // start host process
            var sut = workerSupervisor as IWorkerSupervisor;
            Assert.NotNull(sut);
            await sut.StartAsync();

            // Test
            const int numberOfWorker = -4;
            agentConfig.SetMaxWorker(numberOfWorker);

            await Task.Delay(kDefaultDelay);
            Assert.Equal(kDefaultMaxWorker, sut.NumberOfWorkers);
            loggerMock.Verify();

            // clean up
            await sut.StopAsync();
        }

        [Fact]
        public async Task Test_005_IncreaseAndDecreaseMaxWorker_Expect_AdoptionOfWorker() {

            using var container = GetAutofacTestConfiguration();
            var agentConfig = new TestAgentConfigProvider();
            var loggerMock = new Mock<ILogger>();
            var healthCheckManagerMock = new Mock<IHealthCheckManager>();

            var workerSupervisor = new WorkerSupervisor(container.BeginLifetimeScope(), agentConfig, loggerMock.Object, healthCheckManagerMock.Object, kSupervisorDelay);

            // start host process
            var sut = workerSupervisor as IWorkerSupervisor;
            Assert.NotNull(sut);
            await sut.StartAsync();

            // Test
            int numberOfWorker = 8; //Increase
            agentConfig.SetMaxWorker(numberOfWorker);
            await Task.Delay(kDefaultDelay);
            Assert.Equal(numberOfWorker, sut.NumberOfWorkers);

            numberOfWorker = 6; //Decrease
            agentConfig.SetMaxWorker(numberOfWorker);
            await Task.Delay(kDefaultDelay);
            Assert.Equal(numberOfWorker, sut.NumberOfWorkers);

            // clean up
            await sut.StopAsync();
        }

        private IContainer GetAutofacTestConfiguration() {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterType<TestWorker>().As<IWorker>();
            var container = containerBuilder.Build();

            return container;
        }

        private const int kDefaultDelay = 2000;
        private const int kDefaultMaxWorker = 5;
        private const int kSupervisorDelay = 1;
    }
}
