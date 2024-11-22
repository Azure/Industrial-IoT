// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using FluentAssertions;
    using Microsoft.Extensions.Options;
    using Moq;
    using Neovolve.Logging.Xunit;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    public class SubscriptionManagerTests
    {
        private readonly ITestOutputHelper _output;

        public SubscriptionManagerTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task AddAndRemoveOfSubscription1Async()
        {
            var loggerFactory = LogFactory.Create(_output);
            var session = new Mock<ISubscriptionManagerContext>();
            var so1 = OptionMonitor.Create<SubscriptionOptions>();
            var so2 = OptionMonitor.Create<SubscriptionOptions>();

            var ms1 = new Mock<IManagedSubscription>();
            ms1.SetupGet(s => s.Id).Returns(1);
            var ms2 = new Mock<IManagedSubscription>();
            ms2.SetupGet(s => s.Id).Returns(2);

            var sut = new SubscriptionManager(session.Object,
                loggerFactory, DiagnosticsMasks.None);

            session
                .Setup(s => s.CreateSubscription(
                    It.Is<IOptionsMonitor<SubscriptionOptions>>(o => o == so1),
                    It.Is<IMessageAckQueue>(q => q == sut)))
                .Returns(() => ms1.Object)
                .Verifiable(Times.Once);
            session
                .Setup(s => s.CreateSubscription(
                    It.Is<IOptionsMonitor<SubscriptionOptions>>(o => o == so2),
                    It.Is<IMessageAckQueue>(q => q == sut)))
                .Returns(() => ms2.Object);

            sut.PublishWorkerCount.Should().Be(0);

            // Test adding and removing a subscription from
            var s1 = sut.Add(so1);
            var s2 = sut.Add(so2);
            sut.Count.Should().Be(2);

            sut.Invoking(s => s.Add(so2)).Should().Throw<ServiceResultException>()
                .Which.StatusCode.Should().Be(StatusCodes.BadAlreadyExists);

            sut.PublishWorkerCount.Should().Be(0);
            await sut.CompleteAsync(1, default);
            sut.Count.Should().Be(1);
            sut.Items.Should().Contain(s2);

            sut.PublishControlCycles.Should().BeGreaterThan(0);
            await sut.DisposeAsync();
            sut.Count.Should().Be(0);
            sut.PublishWorkerCount.Should().Be(0);
            session.Verify();
        }

        [Fact]
        public async Task AddAndRemoveOfSubscription2Async()
        {
            var loggerFactory = LogFactory.Create(_output);
            var session = new Mock<ISubscriptionManagerContext>();
            var so1 = OptionMonitor.Create<SubscriptionOptions>();
            var so2 = OptionMonitor.Create<SubscriptionOptions>();

            var ms1 = new Mock<IManagedSubscription>();
            ms1.SetupGet(s => s.Id).Returns(1);
            var ms2 = new Mock<IManagedSubscription>();
            ms2.SetupGet(s => s.Id).Returns(2);

            var sut = new SubscriptionManager(session.Object,
                loggerFactory, DiagnosticsMasks.None);

            session
                .Setup(s => s.CreateSubscription(
                    It.Is<IOptionsMonitor<SubscriptionOptions>>(o => o == so1),
                    It.Is<IMessageAckQueue>(q => q == sut)))
                .Returns(() => ms1.Object);
            session
                .Setup(s => s.CreateSubscription(
                    It.Is<IOptionsMonitor<SubscriptionOptions>>(o => o == so2),
                    It.Is<IMessageAckQueue>(q => q == sut)))
                .Returns(() => ms2.Object);

            sut.PublishWorkerCount.Should().Be(0);

            // Test adding and removing a subscription from
            var s1 = sut.Add(so1);
            var s2 = sut.Add(so2);
            sut.Count.Should().Be(2);

            ms1.SetupGet(s => s.Created).Returns(true);
            sut.Update();
            await Task.Delay(100);

            await sut.CompleteAsync(2, default); // Remove s2
            sut.Count.Should().Be(1);
            sut.Items.Should().NotContain(s2);

            await Task.Delay(1000); // Give time to workers to start

            sut.PublishWorkerCount.Should().Be(2);
            sut.PublishControlCycles.Should().BeGreaterThan(0);

            await sut.CompleteAsync(1, default); // Remove s1
            sut.Count.Should().Be(0);
            sut.Items.Should().NotContain(s1);

            await Task.Delay(100);
            sut.PublishWorkerCount.Should().Be(0);

            await sut.DisposeAsync();
            sut.Count.Should().Be(0);
            sut.PublishWorkerCount.Should().Be(0);
            session.Verify();
        }

        [Fact]
        public async Task ScaleOutAndInOfPublishWorkersAsync()
        {
            var loggerFactory = LogFactory.Create(_output);
            var session = new Mock<ISubscriptionManagerContext>();
            var so1 = OptionMonitor.Create<SubscriptionOptions>();
            var so2 = OptionMonitor.Create<SubscriptionOptions>();

            var ms1 = new Mock<IManagedSubscription>();
            ms1.SetupGet(s => s.Id).Returns(1);
            var ms2 = new Mock<IManagedSubscription>();
            ms2.SetupGet(s => s.Id).Returns(2);

            var sut = new SubscriptionManager(session.Object,
                loggerFactory, DiagnosticsMasks.None);

            session
                .Setup(s => s.CreateSubscription(
                    It.Is<IOptionsMonitor<SubscriptionOptions>>(o => o == so1),
                    It.Is<IMessageAckQueue>(q => q == sut)))
                .Returns(() => ms1.Object)
                .Verifiable(Times.Once);
            session
                .Setup(s => s.CreateSubscription(
                    It.Is<IOptionsMonitor<SubscriptionOptions>>(o => o == so2),
                    It.Is<IMessageAckQueue>(q => q == sut)))
                .Returns(() => ms2.Object)
                .Verifiable(Times.Once);

            sut.PublishWorkerCount.Should().Be(0);

            // Test adding and removing a subscription from
            var s1 = sut.Add(so1);
            var s2 = sut.Add(so2);
            sut.Count.Should().Be(2);

            sut.MinPublishWorkerCount = 0;
            ms1.SetupGet(s => s.Created).Returns(true);
            sut.Update();
            await Task.Delay(100);
            sut.PublishWorkerCount.Should().Be(1);

            sut.MinPublishWorkerCount = 8;
            ms2.SetupGet(s => s.Created).Returns(true);
            sut.Update();
            await Task.Delay(100);
            sut.PublishWorkerCount.Should().Be(8);

            sut.MinPublishWorkerCount = 4;
            sut.Update();
            await Task.Delay(100);
            sut.PublishWorkerCount.Should().Be(4);

            sut.MinPublishWorkerCount = 0;
            sut.Update();
            await Task.Delay(100);
            sut.PublishWorkerCount.Should().Be(2);

            sut.MinPublishWorkerCount = 0;
            sut.MaxPublishWorkerCount = 1;
            sut.Update();
            await Task.Delay(100);
            sut.PublishWorkerCount.Should().Be(1);

            await sut.DisposeAsync();
            sut.Count.Should().Be(0);
            sut.PublishWorkerCount.Should().Be(0);

            session.Verify();
        }

        [Fact]
        public async Task SendPublishRequestsWithSuccessAsync()
        {
            var loggerFactory = LogFactory.Create(_output);
            var session = new Mock<ISubscriptionManagerContext>();
            var so1 = OptionMonitor.Create<SubscriptionOptions>();
            var ms1 = new Mock<IManagedSubscription>();
            ms1.SetupGet(s => s.Id).Returns(1);
            ms1.SetupGet(s => s.Created).Returns(true);
            var sut = new SubscriptionManager(session.Object,
                loggerFactory, DiagnosticsMasks.None);
            session
                .Setup(s => s.CreateSubscription(
                    It.Is<IOptionsMonitor<SubscriptionOptions>>(o => o == so1),
                    It.Is<IMessageAckQueue>(q => q == sut)))
                .Returns(() => ms1.Object)
                .Verifiable(Times.Once);
            // Test adding subscription
            var s1 = sut.Add(so1);
            sut.Count.Should().Be(1);
            sut.MaxPublishWorkerCount = 1;

            // Ack received immediately
            ms1.Setup(subscription => subscription.OnPublishReceivedAsync(
                It.IsAny<NotificationMessage>(),
                It.IsAny<IReadOnlyList<uint>>(),
                It.IsAny<IReadOnlyList<string>>()))
                .Returns((NotificationMessage n, IReadOnlyList<uint> v, IReadOnlyList<string> s)
                    => sut.QueueAsync(new SubscriptionAcknowledgement
                    {
                        SubscriptionId = 1,
                        SequenceNumber = n.SequenceNumber
                    }, default));

            session.Setup(session => session.PublishAsync(
                It.IsAny<RequestHeader>(),
                It.IsAny<SubscriptionAcknowledgementCollection>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((RequestHeader h, SubscriptionAcknowledgementCollection s, CancellationToken ct)
                    => new PublishResponse
                    {
                        AvailableSequenceNumbers = Array.Empty<uint>(),
                        NotificationMessage = new NotificationMessage { SequenceNumber = h.RequestHandle },
                        Results = new StatusCodeCollection(s.Select(_ => (StatusCode)StatusCodes.Good)),
                        SubscriptionId = 1,
                        MoreNotifications = false,
                        ResponseHeader = new ResponseHeader
                        {
                            ServiceResult = StatusCodes.Good,
                            StringTable = new StringCollection()
                        }
                    });

            sut.Resume();
            await Task.Delay(1000);

            await sut.DisposeAsync();
            sut.Count.Should().Be(0);
            sut.PublishWorkerCount.Should().Be(0);
            session.Verify();
        }
    }
}
