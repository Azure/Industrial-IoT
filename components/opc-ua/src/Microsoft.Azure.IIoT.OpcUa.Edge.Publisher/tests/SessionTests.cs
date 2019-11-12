// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Tests {
    using Xunit;

    public class SessionTests {


        [Fact]
        public void SessionTest() {
            //var amc = new ApplicationConfigurationFactory();
            //var sessionManager = new DefaultSessionManager(amc);

            //Assert.Equal(0, sessionManager.SessionCount);

            //var endpointUrl = $"opc.tcp://{Environment.MachineName}:{testServerFixture.Port}/UA/SampleServer";
            //endpointUrl = "opc.tcp://localhost:4842";
            //var opcConfig = SampleData.GetOPCConfig();
            //var sm = SampleData.GetSubscriptionModels(endpointUrl).First();

            //var session = sessionManager.GetSessionForSubscription(sm, opcConfig, true).Result;
            //Assert.True(session.Connected);
            //Assert.Equal(1, sessionManager.SessionCount);

            //sessionManager.RemoveSession(session.SessionId).Wait();

            //Assert.Equal(0, sessionManager.SessionCount);
        }

        [Fact]
        public void SubscriptionTest() {
            //var amc = new ApplicationConfigurationFactory();
            //var sessionManager = new DefaultSessionManager(amc);
            //var subscriptionManager = new DefaultSubscriptionManager(sessionManager);

            //var endpointUrl = $"opc.tcp://{Environment.MachineName}:{testServerFixture.Port}/UA/SampleServer";
            //var opcConfig = SampleData.GetOPCConfig();
            //var sm = SampleData.GetSubscriptionModels(endpointUrl).First();

            //Assert.Equal(0, subscriptionManager.TotalSubscriptionCount);
            //Assert.Equal(0, sessionManager.SessionCount);

            //var subscription = subscriptionManager.GetOrCreateSubscription(sm, opcConfig, (s, notification, table) => { }).Result;

            //Assert.Equal(1, subscriptionManager.TotalSubscriptionCount);
            //Assert.Equal(1, sessionManager.SessionCount);

            //subscriptionManager.RemoveSubscription(subscription).Wait();

            //Assert.Equal(0, subscriptionManager.TotalSubscriptionCount);
            //Assert.Equal(0, sessionManager.SessionCount);
        }
    }
}
