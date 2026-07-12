// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures
{
    using System.Text.Json;
    using Xunit;

    [Trait("Compatibility", "ProvisionalFixture")]
    public sealed class CompatibilityGoldenNormalizerTests
    {
        [Fact]
        public void NormalizesOnlyFixtureVaryingFields()
        {
            using var document = JsonDocument.Parse("""
                {
                  "EndpointUrl": "opc.tcp://127.0.0.1:51000/path",
                  "Timestamp": "2026-07-12T20:27:40.779+02:00",
                  "DataSetClassId": "d2719a6e-10ad-4e2b-a76b-0973382d0bd3",
                  "SequenceNumber": 12,
                  "Payload": {
                    "Output": 42,
                    "Description": "unchanged",
                    "Items": [
                      { "Timestamp": "2026-07-12T20:27:40.779+02:00" },
                      { "Description": "still unchanged" }
                    ]
                  }
                }
                """);

            var normalized = CompatibilityGoldenNormalizer.Normalize(document.RootElement);

            Assert.Equal("opc.tcp://<host>:<port>/path",
                normalized["EndpointUrl"]!.GetValue<string>());
            Assert.Equal("<timestamp>", normalized["Timestamp"]!.GetValue<string>());
            Assert.Equal("<guid>", normalized["DataSetClassId"]!.GetValue<string>());
            Assert.Equal("<sequence>", normalized["SequenceNumber"]!.GetValue<string>());
            Assert.Equal(42, normalized["Payload"]!["Output"]!.GetValue<int>());
            Assert.Equal("unchanged", normalized["Payload"]!["Description"]!.GetValue<string>());
            Assert.Equal("<timestamp>",
                normalized["Payload"]!["Items"]![0]!["Timestamp"]!.GetValue<string>());
            Assert.Equal("still unchanged",
                normalized["Payload"]!["Items"]![1]!["Description"]!.GetValue<string>());
        }

        [Fact]
        public void LeavesStableValuesAndUnrecognizedFieldsUntouched()
        {
            using var document = JsonDocument.Parse("""
                {
                  "EndpointUrl": "not a uri",
                  "Description": "2026-07-12T20:27:40.779+02:00",
                  "Counter": 12,
                  "Payload": { "Id": "business-id" }
                }
                """);

            var normalized = CompatibilityGoldenNormalizer.Normalize(document.RootElement);

            Assert.Equal("not a uri", normalized["EndpointUrl"]!.GetValue<string>());
            Assert.Equal("2026-07-12T20:27:40.779+02:00",
                normalized["Description"]!.GetValue<string>());
            Assert.Equal(12, normalized["Counter"]!.GetValue<int>());
            Assert.Equal("business-id", normalized["Payload"]!["Id"]!.GetValue<string>());
        }
    }
}
