// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests {
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using Xunit;

    internal static partial class TestHelper {

        /// <summary>
        /// Publisher related helper methods
        /// </summary>
        public static class Publisher {
            /// <summary>
            /// Compare PublishNodesEndpointApiModel with PublishedNodeApiModel
            /// </summary>
            public static void AssertEndpointModel(PublishNodesEndpointApiModel expected, PublishNodesEndpointApiModel actual) {
                Assert.Equal(expected.DataSetPublishingInterval, actual.DataSetPublishingInterval);
                Assert.Equal(expected.DataSetWriterGroup, actual.DataSetWriterGroup);
                Assert.Equal(expected.DataSetWriterId, actual.DataSetWriterId);
                Assert.Equal(expected.EndpointUrl.TrimEnd('/'), actual.EndpointUrl.TrimEnd('/'));
                Assert.Equal(expected.OpcAuthenticationMode, actual.OpcAuthenticationMode);
                Assert.Equal(expected.UserName, actual.UserName);
                Assert.Equal(expected.UseSecurity, actual.UseSecurity);
            }

            /// <summary>
            /// Compare PublishNodesEndpointApiModel of requst with DiagnosticInfoApiModel returned
            /// from GetDiagnosticInfo direct method call.
            /// </summary>
            public static void AssertEndpointDiagnosticInfoModel(
                PublishNodesEndpointApiModel expected,
                DiagnosticInfoApiModel diagnosticInfo) {

                var actual = diagnosticInfo.EndpointInfo;

                Assert.Equal(expected.DataSetWriterGroup, actual.DataSetWriterGroup);
                Assert.Equal(expected.EndpointUrl.TrimEnd('/'), actual.EndpointUrl.TrimEnd('/'));
                Assert.Equal(expected.OpcAuthenticationMode, actual.OpcAuthenticationMode);
                Assert.Equal(expected.UserName, actual.UserName);
                Assert.Equal(expected.UseSecurity, actual.UseSecurity);

                // Check validity of diagnosticInfo
                Assert.True(diagnosticInfo.IngressValueChanges > 0);
                Assert.True(diagnosticInfo.IngressDataChanges > 0);
                Assert.Equal(0, diagnosticInfo.MonitoredOpcNodesFailedCount);
                Assert.Equal(expected.OpcNodes.Count, diagnosticInfo.MonitoredOpcNodesSucceededCount);
                Assert.True(diagnosticInfo.OpcEndpointConnected);
                Assert.True(diagnosticInfo.OutgressIoTMessageCount > 0);

                // Check that we are not dropping anything.
                Assert.Equal((uint)0, diagnosticInfo.EncoderNotificationsDropped);
                Assert.Equal((ulong)0, diagnosticInfo.OutgressInputBufferDropped);
            }
        }
    }
}
