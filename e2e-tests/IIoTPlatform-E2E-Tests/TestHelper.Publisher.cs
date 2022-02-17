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
            /// Compare PublishNodesEndpointApiModel with PublishedNodeApiModel returned from diagnosticInfo
            /// </summary>
            public static void AssertEndpointInfoModel(PublishNodesEndpointApiModel expected, PublishNodesEndpointApiModel actual) {
                Assert.Equal(expected.DataSetWriterGroup, actual.DataSetWriterGroup);
                Assert.Equal(expected.EndpointUrl.TrimEnd('/'), actual.EndpointUrl.TrimEnd('/'));
                Assert.Equal(expected.OpcAuthenticationMode, actual.OpcAuthenticationMode);
                Assert.Equal(expected.UserName, actual.UserName);
                Assert.Equal(expected.UseSecurity, actual.UseSecurity);
            }
        }
    }
}
