// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisher_AE_E2E_Tests {
    using Newtonsoft.Json.Linq;

    internal static partial class TestConstants {
        internal static class PublishedNodesConfigurations {
            public static string SimpleEvents(string host, uint port, string writerId) {
                return @$"
                [
                    {{
                        ""EndpointUrl"": ""opc.tcp://{host}:{port}"",
                        ""UseSecurity"": false,
                        ""DataSetWriterId"":""{writerId}"",
                        ""OpcNodes"": [
                            {{
                                ""Id"": ""ns=0;i=2253"",
                                ""QueueSize"": 10,
                                ""DisplayName"": ""SimpleEvents"",
                                ""EventFilter"": {{
                                    ""SelectClauses"": [
                                        {{
                                            ""TypeDefinitionId"": ""i=2041"",
                                            ""BrowsePath"": [
                                                ""EventId""
                                            ]
                                        }},
                                        {{
                                            ""TypeDefinitionId"": ""i=2041"",
                                            ""BrowsePath"": [
                                                ""Message""
                                            ]
                                        }},
                                        {{
                                            ""TypeDefinitionId"": ""nsu=http://microsoft.com/Opc/OpcPlc/SimpleEvents;i=2"",
                                            ""BrowsePath"": [
                                                ""http://microsoft.com/Opc/OpcPlc/SimpleEvents#CycleId""
                                            ]
                                        }},
                                        {{
                                            ""TypeDefinitionId"": ""nsu=http://microsoft.com/Opc/OpcPlc/SimpleEvents;i=2"",
                                            ""BrowsePath"": [
                                                ""http://microsoft.com/Opc/OpcPlc/SimpleEvents#CurrentStep""
                                            ]
                                        }}
                                    ],
                                    ""WhereClause"": {{
                                        ""Elements"": [
                                            {{
                                                ""FilterOperator"": ""OfType"",
                                                ""FilterOperands"": [
                                                    {{
                                                        ""Value"": ""nsu=http://microsoft.com/Opc/OpcPlc/SimpleEvents;i=2""
                                                    }}
                                                ]
                                            }}
                                        ]
                                    }}
                                }}
                            }}
                        ]
                    }}
                ]";
            }

            public static JArray SimpleEventFilter(
                string typeDefinitionId = "i=2782") {
                return new JArray(
                    new JObject(
                        new JProperty("Id", "i=2253"),
                        new JProperty("QueueSize", 1000),
                        new JProperty("EventFilter", new JObject(
                            new JProperty("TypeDefinitionId", typeDefinitionId)))));
            }

            public static JArray PendingConditionForAlarmsView() {
                return new JArray(
                    new JObject(
                        new JProperty("Id", "i=2253"),
                        new JProperty("QueueSize", 10),
                        new JProperty("EventFilter", new JObject(
                            new JProperty("TypeDefinitionId", "i=2915"))),
                        new JProperty("ConditionHandling", new JObject(
                            new JProperty("UpdateInterval", 10),
                            new JProperty("SnapshotInterval", 20)
                        ))));
            }
        }
    }
}
