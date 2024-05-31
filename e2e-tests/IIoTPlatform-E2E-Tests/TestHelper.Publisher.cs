// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatformE2ETests
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    internal static partial class TestHelper
    {
        /// <summary>
        /// Publisher related helper methods
        /// </summary>
        public static class Publisher
        {
            /// <summary>
            /// Compare PublishNodesEndpointApiModel with PublishedNodeApiModel
            /// </summary>
            /// <param name="expected"></param>
            /// <param name="actual"></param>
            public static void AssertEndpointModel(PublishedNodesEntryModel expected, PublishedNodesEntryModel actual)
            {
                Assert.Equal(expected.DataSetPublishingInterval, actual.DataSetPublishingInterval);
                Assert.Equal(expected.DataSetPublishingIntervalTimespan, actual.DataSetPublishingIntervalTimespan);
                Assert.Equal(expected.DataSetWriterGroup, actual.DataSetWriterGroup);
                Assert.Equal(expected.DataSetWriterId, actual.DataSetWriterId);
                Assert.Equal(expected.EndpointUrl.TrimEnd('/'), actual.EndpointUrl.TrimEnd('/'));
                Assert.Equal(expected.OpcAuthenticationMode, actual.OpcAuthenticationMode);
                Assert.Equal(expected.OpcAuthenticationUsername, actual.OpcAuthenticationUsername);
                Assert.Equal(expected.UseSecurity, actual.UseSecurity);
            }

            /// <summary>
            /// Compare PublishNodesEndpointApiModel of requst with DiagnosticInfoApiModel returned
            /// from GetDiagnosticInfo direct method call.
            /// </summary>
            /// <param name="configuredEndpoints"></param>
            /// <param name="diagnosticInfo"></param>
            public static void AssertEndpointDiagnosticInfoModel(
                IEnumerable<PublishedNodesEntryModel> configuredEndpoints,
                PublishDiagnosticInfoModel diagnosticInfo)
            {
                var actual = diagnosticInfo.Endpoints ?? diagnosticInfo.Endpoint.YieldReturn().ToList();
                var expected = configuredEndpoints.ToList();

                Assert.Equal(expected, actual, EqualityComparer<PublishedNodesEntryModel>.Create(
                    (x, y) =>
                    {
                        return x.EndpointUrl.TrimEnd('/') == y.EndpointUrl.TrimEnd('/')
                            && x.DataSetWriterGroup == y.DataSetWriterGroup
                            && x.OpcAuthenticationMode == y.OpcAuthenticationMode
                            && x.OpcAuthenticationUsername == y.OpcAuthenticationUsername
                            && x.UseSecurity == y.UseSecurity
                            ;
                    },
                    _ => 1));

                // Check validity of diagnosticInfo
                Assert.Equal(0, diagnosticInfo.MonitoredOpcNodesFailedCount);
                Assert.Equal(expected.Sum(m => m.OpcNodes.Count), diagnosticInfo.MonitoredOpcNodesSucceededCount);
                Assert.True(diagnosticInfo.OpcEndpointConnected, "Endpoint not connected");
                Assert.True(diagnosticInfo.IngressValueChanges > 0, "No ingress value changes");
                Assert.True(diagnosticInfo.IngressDataChanges > 0, "No ingress data changes");
                Assert.True(diagnosticInfo.OutgressIoTMessageCount > 0, "No outgress messages sent");

                // Check that we are not dropping anything.
                Assert.Equal(0U, diagnosticInfo.EncoderNotificationsDropped);
                Assert.Equal(0L, diagnosticInfo.OutgressInputBufferDropped);
            }
        }
    }
}
