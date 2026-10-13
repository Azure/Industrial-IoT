// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

extern alias Quickstarts;

namespace Azure.IIoT.OpcUa.Publisher.Testing.Tests
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Alarms = Quickstarts::Alarms;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Alarms server node tests
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AlarmServerTests<T>
    {
        public AlarmServerTests(Func<INodeServices<T>> services, T connection)
        {
            _services = services;
            _connection = connection;
        }

        public async Task BrowseAreaPathTestAsync(CancellationToken ct = default)
        {
            var services = _services();
            var results = await services.BrowsePathAsync(_connection, new BrowsePathRequestModel
            {
                NodeId = Opc.Ua.ObjectIds.ObjectsFolder.ToString(),
                BrowsePaths = new[]
                {
                    new[]
                    {
                        Alarms.Namespaces.Alarms + "#Alarms"
                    }
                }
            }, ct).ConfigureAwait(false);

            Assert.Null(results.ErrorInfo);
            var target = Assert.Single(results.Targets!);
            Assert.NotNull(target.BrowsePath);
            Assert.NotNull(target.Target);
            Assert.Equal(Alarms.Namespaces.Alarms + "#s=Alarms", target.Target.NodeId);
        }

        public async Task BrowseMetalsSouthMotorTestAsync(CancellationToken ct = default)
        {
            var services = _services();

            var results = await services.BrowsePathAsync(_connection, new BrowsePathRequestModel
            {
                NodeId = Opc.Ua.ObjectIds.ObjectsFolder.ToString(),
                BrowsePaths = new[]
                {
                    new[]
                    {
                        Alarms.Namespaces.Alarms + "#Alarms",
                        Alarms.Namespaces.Alarms + "#Alarms.Start"
                    }
                }
            }, ct).ConfigureAwait(false);

            Assert.Null(results.ErrorInfo);
            var target = Assert.Single(results.Targets!);
            Assert.NotNull(target.BrowsePath);
            Assert.NotNull(target.Target);
            Assert.Equal(Alarms.Namespaces.Alarms + "#s=Alarms.Start", target.Target.NodeId);
        }

        public async Task BrowseColoursEastTankTestAsync(CancellationToken ct = default)
        {
            var services = _services();
            var results = await services.BrowsePathAsync(_connection, new BrowsePathRequestModel
            {
                NodeId = Opc.Ua.ObjectIds.ObjectsFolder.ToString(),
                BrowsePaths = new[]
                {
                    new[]
                    {
                        Alarms.Namespaces.Alarms + "#Alarms",
                        Alarms.Namespaces.Alarms + "#Alarms.StartBranch"
                    }
                }
            }, ct).ConfigureAwait(false);

            Assert.Null(results.ErrorInfo);
            var target = Assert.Single(results.Targets!);
            Assert.NotNull(target.BrowsePath);
            Assert.NotNull(target.Target);
            Assert.Equal(Alarms.Namespaces.Alarms + "#s=Alarms.StartBranch", target.Target.NodeId);
        }

        public async Task CompileSimpleBaseEventQueryTestAsync(CancellationToken ct = default)
        {
            var services = _services();

            var result = await services.CompileQueryAsync(_connection, new QueryCompilationRequestModel
            {
                Query = "select * from BaseEventType",
                QueryType = QueryType.Event
            }, ct).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Null(result.ErrorInfo);
            var expected = new EventFilterModel
            {
                SelectClauses = kBaseEventSelectClauses.Select(CreateSelectClause).ToList()
            };
            AssertEventFilter(result.EventFilter, expected);
        }

        public async Task CompileSimpleTripAlarmQueryTestAsync(CancellationToken ct = default)
        {
            var services = _services();

            var result = await services.CompileQueryAsync(_connection, new QueryCompilationRequestModel
            {
                Query = "select * from OffNormalAlarmType",
                QueryType = QueryType.Event
            }, ct).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Null(result.ErrorInfo);
            var expected = CreateOffNormalEventFilter(includeLocalTime: true);
            AssertEventFilter(result.EventFilter, expected);
        }

        public async Task CompileAlarmQueryTest1Async(CancellationToken ct = default)
        {
            var services = _services();

            var result = await services.CompileQueryAsync(_connection, new QueryCompilationRequestModel
            {
                Query = $@"
                    PREFIX alarms <{Alarms.Namespaces.Alarms}>
                    SELECT /Comment, /Severity, /SourceNode FROM OffNormalAlarmType, BaseEventType
                    WHERE
                        OFTYPE OffNormalAlarmType AND
                        /SourceNode IN ('alarms:s=Alarms.BooleanSource'^^NodeId)
                ",
                QueryType = QueryType.Event
            }, ct).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Null(result.ErrorInfo);

            var expected = CreateOffNormalEventFilter(
            [
                "i=10637|/Comment|/Comment.Value",
                "i=10637|/Severity|/Severity.Value",
                "i=10637|/SourceNode|/SourceNode.Value"
            ], includeWhere: true, sourceNodeTypeId: "i=10637");
            AssertEventFilter(result.EventFilter, expected);
        }

        public async Task CompileAlarmQueryTest2Async(CancellationToken ct = default)
        {
            var services = _services();

            var result = await services.CompileQueryAsync(_connection, new QueryCompilationRequestModel
            {
                Query = $@"
                    PREFIX alarms <{Alarms.Namespaces.Alarms}>
                    SELECT * FROM BaseEventType, OffNormalAlarmType
                    WHERE
                        OFTYPE OffNormalAlarmType AND
                        /SourceNode IN ('alarms:s=Alarms.BooleanSource'^^NodeId)
                ",
                QueryType = QueryType.Event
            }, ct).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Null(result.ErrorInfo);
            var expected = CreateOffNormalEventFilter(includeBaseEventFields: true,
                includeWhere: true, sourceNodeTypeId: "i=2041");
            AssertEventFilter(result.EventFilter, expected);
        }

        private readonly T _connection;
        private readonly Func<INodeServices<T>> _services;

        private static EventFilterModel CreateOffNormalEventFilter(
            IReadOnlyList<string>? selectClauses = null,
            bool includeBaseEventFields = false,
            bool includeLocalTime = false,
            bool includeWhere = false,
            string? sourceNodeTypeId = null)
        {
            selectClauses ??= kOffNormalSelectClauses;
            if (includeLocalTime)
            {
                selectClauses = selectClauses
                    .Take(6)
                    .Append(kBaseEventSelectClauses[6])
                    .Concat(selectClauses.Skip(6))
                    .ToArray();
            }
            var clauses = includeBaseEventFields
                ? kBaseEventSelectClauses.Concat(selectClauses)
                : selectClauses;
            return new EventFilterModel
            {
                SelectClauses = clauses.Select(CreateSelectClause).ToList(),
                WhereClause = includeWhere ? new ContentFilterModel
                {
                    Elements =
                    [
                        new()
                        {
                            FilterOperator = FilterOperatorType.And,
                            FilterOperands =
                            [
                                new FilterOperandModel { Index = 1u },
                                new FilterOperandModel { Index = 2u }
                            ]
                        },
                        new()
                        {
                            FilterOperator = FilterOperatorType.InList,
                            FilterOperands =
                            [
                                new FilterOperandModel
                                {
                                    NodeId = sourceNodeTypeId,
                                    BrowsePath = ["/SourceNode"],
                                    AttributeId = NodeAttribute.Value
                                },
                                new FilterOperandModel
                                {
                                    Value = Alarms.Namespaces.Alarms +
                                        "#s=Alarms.BooleanSource",
                                    DataType = "NodeId"
                                }
                            ]
                        },
                        new()
                        {
                            FilterOperator = FilterOperatorType.OfType,
                            FilterOperands =
                            [
                                new FilterOperandModel
                                {
                                    Value = "i=10637",
                                    DataType = "NodeId"
                                }
                            ]
                        }
                    ]
                } : null
            };
        }

        private static SimpleAttributeOperandModel CreateSelectClause(string clause)
        {
            var parts = clause.Split('|');
            return new SimpleAttributeOperandModel
            {
                TypeDefinitionId = parts[0],
                BrowsePath = parts[1].Split(',', StringSplitOptions.RemoveEmptyEntries),
                AttributeId = NodeAttribute.Value,
                DisplayName = parts[2]
            };
        }

        private static void AssertEventFilter(EventFilterModel? actual,
            EventFilterModel expected)
        {
            var actualFilter = Assert.IsType<EventFilterModel>(actual);
            Assert.Equal(expected.SelectClauses!.Count, actualFilter.SelectClauses!.Count);
            for (var index = 0; index < expected.SelectClauses.Count; index++)
            {
                var expectedClause = expected.SelectClauses[index];
                var actualClause = actualFilter.SelectClauses[index];
                Assert.True(expectedClause.TypeDefinitionId == actualClause.TypeDefinitionId,
                    $"Select clause {index}: {expectedClause.TypeDefinitionId} != " +
                    $"{actualClause.TypeDefinitionId}");
                Assert.Equal(expectedClause.BrowsePath, actualClause.BrowsePath);
                Assert.Equal(expectedClause.AttributeId, actualClause.AttributeId);
                Assert.Equal(expectedClause.DisplayName, actualClause.DisplayName);
            }

            if (expected.WhereClause is null)
            {
                Assert.Null(actualFilter.WhereClause);
                return;
            }
            var expectedElements = expected.WhereClause.Elements!;
            var actualElements = Assert.IsType<ContentFilterModel>(
                actualFilter.WhereClause).Elements!;
            Assert.Equal(expectedElements.Count, actualElements.Count);
            for (var index = 0; index < expectedElements.Count; index++)
            {
                var expectedElement = expectedElements[index];
                var actualElement = actualElements[index];
                Assert.Equal(expectedElement.FilterOperator, actualElement.FilterOperator);
                Assert.Equal(expectedElement.FilterOperands!.Count,
                    actualElement.FilterOperands!.Count);
                for (var operandIndex = 0; operandIndex < expectedElement.FilterOperands.Count;
                    operandIndex++)
                {
                    var expectedOperand = expectedElement.FilterOperands[operandIndex];
                    var actualOperand = actualElement.FilterOperands[operandIndex];
                    Assert.Equal(expectedOperand.Index, actualOperand.Index);
                    Assert.True(expectedOperand.NodeId == actualOperand.NodeId,
                        $"Where element {index}, operand {operandIndex}: " +
                        $"{expectedOperand.NodeId} != {actualOperand.NodeId}");
                    Assert.Equal(expectedOperand.BrowsePath, actualOperand.BrowsePath);
                    Assert.Equal(expectedOperand.AttributeId, actualOperand.AttributeId);
                    Assert.Equal(expectedOperand.DataType, actualOperand.DataType);
                    Assert.True(JsonNode.DeepEquals(expectedOperand.Value, actualOperand.Value),
                        $"{expectedOperand.Value} != {actualOperand.Value}");
                }
            }
        }

        private static readonly string[] kBaseEventSelectClauses =
        [
            "i=2041|/EventId|/EventId.Value",
            "i=2041|/EventType|/EventType.Value",
            "i=2041|/SourceNode|/SourceNode.Value",
            "i=2041|/SourceName|/SourceName.Value",
            "i=2041|/Time|/Time.Value",
            "i=2041|/ReceiveTime|/ReceiveTime.Value",
            "i=2041|/LocalTime|/LocalTime.Value",
            "i=2041|/Message|/Message.Value",
            "i=2041|/Severity|/Severity.Value",
            "i=2041|/ConditionClassId|/ConditionClassId.Value",
            "i=2041|/ConditionClassName|/ConditionClassName.Value",
            "i=2041|/ConditionSubClassId|/ConditionSubClassId.Value",
            "i=2041|/ConditionSubClassName|/ConditionSubClassName.Value"
        ];

        private static readonly string[] kOffNormalSelectClauses =
        [
            "i=10637|/EventId|/EventId.Value",
            "i=10637|/EventType|/EventType.Value",
            "i=10637|/SourceNode|/SourceNode.Value",
            "i=10637|/SourceName|/SourceName.Value",
            "i=10637|/Time|/Time.Value",
            "i=10637|/ReceiveTime|/ReceiveTime.Value",
            "i=10637|/Message|/Message.Value",
            "i=10637|/Severity|/Severity.Value",
            "i=10637|/ConditionClassId|/ConditionClassId.Value",
            "i=10637|/ConditionClassName|/ConditionClassName.Value",
            "i=2782|/ConditionSubClassId|/ConditionSubClassId.Value",
            "i=2782|/ConditionSubClassName|/ConditionSubClassName.Value",
            "i=10637|/ConditionName|/ConditionName.Value",
            "i=10637|/BranchId|/BranchId.Value",
            "i=10637|/Retain|/Retain.Value",
            "i=2782|/SupportsFilteredRetain|/SupportsFilteredRetain.Value",
            "i=10637|/EnabledState|/EnabledState.Value",
            "i=2782|/EnabledState,/TransitionTime|/EnabledState/TransitionTime.Value",
            "i=2782|/EnabledState,/EffectiveTransitionTime|/EnabledState/EffectiveTransitionTime.Value",
            "i=10637|/EnabledState,/Id|/EnabledState/Id.Value",
            "i=2782|/EnabledState,/EffectiveDisplayName|/EnabledState/EffectiveDisplayName.Value",
            "i=10637|/Quality|/Quality.Value",
            "i=10637|/Quality,/SourceTimestamp|/Quality/SourceTimestamp.Value",
            "i=10637|/LastSeverity|/LastSeverity.Value",
            "i=10637|/LastSeverity,/SourceTimestamp|/LastSeverity/SourceTimestamp.Value",
            "i=10637|/Comment|/Comment.Value",
            "i=10637|/Comment,/SourceTimestamp|/Comment/SourceTimestamp.Value",
            "i=10637|/ClientUserId|/ClientUserId.Value",
            "i=10637|/AckedState|/AckedState.Value",
            "i=2881|/AckedState,/TransitionTime|/AckedState/TransitionTime.Value",
            "i=10637|/AckedState,/Id|/AckedState/Id.Value",
            "i=2881|/ConfirmedState|/ConfirmedState.Value",
            "i=2881|/ConfirmedState,/TransitionTime|/ConfirmedState/TransitionTime.Value",
            "i=2881|/ConfirmedState,/Id|/ConfirmedState/Id.Value",
            "i=10637|/ActiveState|/ActiveState.Value",
            "i=2915|/ActiveState,/TransitionTime|/ActiveState/TransitionTime.Value",
            "i=2915|/ActiveState,/EffectiveTransitionTime|/ActiveState/EffectiveTransitionTime.Value",
            "i=10637|/ActiveState,/Id|/ActiveState/Id.Value",
            "i=2915|/ActiveState,/EffectiveDisplayName|/ActiveState/EffectiveDisplayName.Value",
            "i=10637|/InputNode|/InputNode.Value",
            "i=2915|/SuppressedState|/SuppressedState.Value",
            "i=2915|/SuppressedState,/TransitionTime|/SuppressedState/TransitionTime.Value",
            "i=2915|/SuppressedState,/Id|/SuppressedState/Id.Value",
            "i=2915|/OutOfServiceState|/OutOfServiceState.Value",
            "i=2915|/OutOfServiceState,/TransitionTime|/OutOfServiceState/TransitionTime.Value",
            "i=2915|/OutOfServiceState,/Id|/OutOfServiceState/Id.Value",
            "i=2915|/ShelvingState|/ShelvingState.Value",
            "i=2915|/ShelvingState,/UnshelveTime|/ShelvingState/UnshelveTime.Value",
            "i=2915|/ShelvingState,/TimedShelve|/ShelvingState/TimedShelve.Value",
            "i=2915|/ShelvingState,/TimedShelve,/InputArguments|/ShelvingState/TimedShelve/InputArguments.Value",
            "i=2915|/ShelvingState,/Unshelve|/ShelvingState/Unshelve.Value",
            "i=2915|/ShelvingState,/OneShotShelve|/ShelvingState/OneShotShelve.Value",
            "i=2915|/ShelvingState,/CurrentState|/ShelvingState/CurrentState.Value",
            "i=2915|/ShelvingState,/CurrentState,/Id|/ShelvingState/CurrentState/Id.Value",
            "i=2915|/ShelvingState,/LastTransition|/ShelvingState/LastTransition.Value",
            "i=2915|/ShelvingState,/LastTransition,/Id|/ShelvingState/LastTransition/Id.Value",
            "i=2915|/ShelvingState,/LastTransition,/TransitionTime|/ShelvingState/LastTransition/TransitionTime.Value",
            "i=10637|/SuppressedOrShelved|/SuppressedOrShelved.Value",
            "i=2915|/MaxTimeShelved|/MaxTimeShelved.Value",
            "i=2915|/AudibleEnabled|/AudibleEnabled.Value",
            "i=2915|/AudibleSound|/AudibleSound.Value",
            "i=2915|/SilenceState|/SilenceState.Value",
            "i=2915|/SilenceState,/TransitionTime|/SilenceState/TransitionTime.Value",
            "i=2915|/SilenceState,/Id|/SilenceState/Id.Value",
            "i=2915|/OnDelay|/OnDelay.Value",
            "i=2915|/OffDelay|/OffDelay.Value",
            "i=2915|/FirstInGroupFlag|/FirstInGroupFlag.Value",
            "i=2915|/FirstInGroup|/FirstInGroup.Value",
            "i=2915|/LatchedState|/LatchedState.Value",
            "i=2915|/LatchedState,/TransitionTime|/LatchedState/TransitionTime.Value",
            "i=2915|/LatchedState,/Id|/LatchedState/Id.Value",
            "i=2915|/%3CAlarmGroup%3E|/<AlarmGroup>.Value",
            "i=2915|/ReAlarmTime|/ReAlarmTime.Value",
            "i=2915|/ReAlarmRepeatCount|/ReAlarmRepeatCount.Value",
            "i=10637|/NormalState|/NormalState.Value"
        ];
    }
}
