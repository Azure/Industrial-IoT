// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
#nullable enable
namespace Azure.IIoT.OpcUa.Publisher.Parser.Tests
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Json;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    public sealed class EventFilterParserTests
    {
        private readonly IJsonSerializer _serializer = new DefaultJsonSerializer();
        private readonly ITestOutputHelper _output;

        public EventFilterParserTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData("SELECT *")]
        [InlineData("SELECT * FROM BaseEventType")]
        [InlineData("SELECT * FROM ns0:BaseEventType")]
        public async Task SimpleStatementParsingNoPrefixNoWhereTestsAsync(string query)
        {
            var parser = new FilterQueryParser(_serializer);
            var context = new TestParserContext
            {
                new IdentifierMetaData(Opc.Ua.ObjectTypeIds.BaseEventType.ToString(),
                    new[] { "/Severity" }, "Severity"),
                new IdentifierMetaData(Opc.Ua.ObjectTypeIds.BaseEventType.ToString(),
                    new[] { "/SourceNode" }, "SourceNode"),
                new IdentifierMetaData(Opc.Ua.ObjectTypeIds.BaseEventType.ToString(),
                    new[] { "/SourceName" }, "SourceName"),
                new IdentifierMetaData(Opc.Ua.ObjectTypeIds.BaseEventType.ToString(),
                    new[] { "/LocalTime" }, "LocalTime")
            };
            var eventFilter = await parser.ParseEventFilterAsync(query, context, default).ConfigureAwait(false);

            Assert.NotNull(eventFilter);
            Assert.NotNull(eventFilter.SelectClauses);
            Assert.NotEmpty(eventFilter.SelectClauses);
            Assert.Null(eventFilter.WhereClause);
            _output.WriteLine(_serializer.SerializeToString(eventFilter, SerializeOption.Indented));
        }

        [Theory]
        [InlineData("SELECT * FROM BaseEventType " +
            "WHERE /Severity > 5 AND /SourceName = 'SouthMotor'")]
        [InlineData("SELECT * " +
            "WHERE /Severity > 5 AND /SourceName = 'SouthMotor'")]
        [InlineData("SELECT /Severity, /SourceNode FROM BaseEventType " +
            "WHERE !(/Severity <= 5 OR /SourceName == 'SouthMotor')")]
        [InlineData("SELECT /LocalTime FROM BaseEventType " +
            "WHERE NOT ISNULL /LocalTime " +
            "AND (/Severity <> 5 OR /SourceName != 'SouthMotor')")]
        [InlineData("SELECT E/Severity, E/SourceNode FROM BaseEventType E " +
            "WHERE !(E/Severity <= 5 OR E/SourceName == 'SouthMotor')")]
        [InlineData("SELECT E/0:Severity, E/SourceNode FROM ns0:BaseEventType E " +
            "WHERE NOT (E/0:Severity <= 5 OR E/0:SourceName == 'SouthMotor')")]
        [InlineData("SELECT /0:LocalTime FROM ns0:BaseEventType " +
            "WHERE NOT ISNULL /0:LocalTime " +
            "AND (/0:Severity <> 5 OR /0:SourceName != 'SouthMotor')")]
        [InlineData("SELECT E/Severity, /SourceNode FROM BaseEventType E " +
            "WHERE !(E/Severity <= 5 OR E/SourceName == 'SouthMotor')")]
        [InlineData("SELECT E/0:Severity, /SourceNode FROM ns0:BaseEventType E " +
            "WHERE NOT (E/0:Severity <= 5 OR E/0:SourceName == 'SouthMotor')")]
        public async Task SimpleStatementParsingNoPrefixTestsAsync(string query)
        {
            var parser = new FilterQueryParser(_serializer);
            var context = new TestParserContext
            {
                new IdentifierMetaData(Opc.Ua.ObjectTypeIds.BaseEventType.ToString(),
                    new[] { "/Severity" }, "Severity"),
                new IdentifierMetaData(Opc.Ua.ObjectTypeIds.BaseEventType.ToString(),
                    new[] { "/SourceNode" }, "SourceNode"),
                new IdentifierMetaData(Opc.Ua.ObjectTypeIds.BaseEventType.ToString(),
                    new[] { "/SourceName" }, "SourceName"),
                new IdentifierMetaData(Opc.Ua.ObjectTypeIds.BaseEventType.ToString(),
                    new[] { "/LocalTime" }, "LocalTime")
            };
            var eventFilter = await parser.ParseEventFilterAsync(query, context, default).ConfigureAwait(false);

            Assert.NotNull(eventFilter);
            Assert.NotNull(eventFilter.SelectClauses);
            Assert.NotEmpty(eventFilter.SelectClauses);
            Assert.NotNull(eventFilter.WhereClause);
            var where = eventFilter.WhereClause!.Elements;
            Assert.NotNull(where);
            Assert.NotEmpty(where);
            _output.WriteLine(_serializer.SerializeToString(eventFilter, SerializeOption.Indented));
        }

        [Theory]
        [InlineData("PREFIX ua <http://furly/ua> SELECT * FROM ua:BaseEventType " +
            "WHERE /ua:Severity > 5 AND /ua:SourceName = 'SouthMotor'")]
        [InlineData("PREFIX ua <http://furly/ua> " +
            "SELECT /ua:Severity, /ua:SourceNode FROM ua:BaseEventType " +
            "WHERE !(/ua:Severity <= 5 OR /ua:SourceName == 'SouthMotor')")]
        [InlineData("PREFIX ua <http://furly/ua> PREFIX f <http://furly/f> " +
            "SELECT /f:LocalTime FROM ua:BaseEventType " +
            "WHERE NOT ISNULL /f:LocalTime " +
            "AND (/ua:Severity <> 5 OR /ua:SourceName != 'SouthMotor')")]
        [InlineData("PREFIX ua <http://furly/ua> PREFIX f <http://furly/f> " +
            "SELECT E/ua:Severity, E/ua:SourceNode FROM ua:BaseEventType E " +
            "WHERE !(E/ua:Severity <= 5 OR E/ua:SourceName == 'SouthMotor')")]
        public async Task SimpleStatementParsingTestsAsync(string query)
        {
            var parser = new FilterQueryParser(_serializer);
            var context = new TestParserContext
            {
                new IdentifierMetaData("http://furly/ua#BaseEventType",
                    new[] { "/[http://furly/ua#Severity]" }, "Severity"),
                new IdentifierMetaData("http://furly/ua#BaseEventType",
                    new[] { "/[http://furly/ua#SourceNode]" }, "SourceNode"),
                new IdentifierMetaData("http://furly/ua#BaseEventType",
                    new[] { "/[http://furly/ua#SourceName]" }, "SourceName"),
                new IdentifierMetaData("http://furly/ua#BaseEventType",
                    new[] { "/[http://furly/f#LocalTime]" }, "LocalTime")
            };
            var eventFilter = await parser.ParseEventFilterAsync(query, context, default).ConfigureAwait(false);

            Assert.NotNull(eventFilter);
            Assert.NotNull(eventFilter.SelectClauses);
            Assert.NotEmpty(eventFilter.SelectClauses);
            Assert.NotNull(eventFilter.WhereClause);
            var where = eventFilter.WhereClause!.Elements;
            Assert.NotNull(where);
            Assert.NotEmpty(where);
            _output.WriteLine(_serializer.SerializeToString(eventFilter, SerializeOption.Indented));
        }

        [Theory]
        [InlineData("SELECT * FROM ´http://furly/ua#BaseEventType´ " +
            "WHERE ´/[http://furly/ua#Severity]´ > 5 " +
            "AND ´/[http://furly/ua#SourceName]´ = 'SouthMotor'")]
        [InlineData("SELECT ´E/[http://furly/ua#Severity]´, ´E/[http://furly/ua#SourceNode]´ " +
            "FROM ´http://furly/ua#BaseEventType´ E " +
            "WHERE !(´E/[http://furly/ua#Severity]´ <= 5 " +
            "   OR ´E/[http://furly/ua#SourceName]´ == 'SouthMotor')")]
        public async Task SimpleStatementParsingTestsInlineNamespacesAsync(string query)
        {
            var parser = new FilterQueryParser(_serializer);
            var context = new TestParserContext
            {
                new IdentifierMetaData("http://furly/ua#BaseEventType",
                    new[] { "/[http://furly/ua#Severity]" }, "Severity"),
                new IdentifierMetaData("http://furly/ua#BaseEventType",
                    new[] { "/[http://furly/ua#SourceNode]" }, "SourceNode"),
                new IdentifierMetaData("http://furly/ua#BaseEventType",
                    new[] { "/[http://furly/ua#SourceName]" }, "SourceName"),
                new IdentifierMetaData("http://furly/ua#BaseEventType",
                    new[] { "/[http://furly/f#LocalTime]" }, "LocalTime")
            };
            var eventFilter = await parser.ParseEventFilterAsync(query, context, default).ConfigureAwait(false);

            Assert.NotNull(eventFilter);
            Assert.NotNull(eventFilter.SelectClauses);
            Assert.NotEmpty(eventFilter.SelectClauses);
            Assert.NotNull(eventFilter.WhereClause);
            var where = eventFilter.WhereClause!.Elements;
            Assert.NotNull(where);
            Assert.NotEmpty(where);
            _output.WriteLine(_serializer.SerializeToString(eventFilter, SerializeOption.Indented));
        }

        [Theory]
        [InlineData("SELECT")]
        [InlineData("SELECT " +
            "WHERE /Severity > 5 AND /SourceName = 'SouthMotor'")]
        [InlineData("FROM BaseEventType")]
        [InlineData("SELECT * FROM 0:BaseEventType")]
        [InlineData("SELECT * FROM BaseEventType " +
            "WHERE /ua:Severity > 5 AND /SourceName = 'SouthMotor'")]
        [InlineData("PREFIX ua f SELECT * FROM BaseEventType " +
            "WHERE /ua:Severity > 5 AND /ub:SourceName = 'SouthMotor'")]
        [InlineData("SELECT * " +
            "WHERE /Severity > 5 NAND /SourceName = 'SouthMotor'")]
        public async Task SimpleStatementParsingFailsTestsAsync(string query)
        {
            var parser = new FilterQueryParser(_serializer);
            var context = new TestParserContext
            {
                new IdentifierMetaData(Opc.Ua.ObjectTypeIds.BaseEventType.ToString(),
                    new[] { "/Severity" }, "Severity"),
                new IdentifierMetaData(Opc.Ua.ObjectTypeIds.BaseEventType.ToString(),
                    new[] { "/SourceNode" }, "SourceNode"),
                new IdentifierMetaData(Opc.Ua.ObjectTypeIds.BaseEventType.ToString(),
                    new[] { "/SourceName" }, "SourceName"),
                new IdentifierMetaData(Opc.Ua.ObjectTypeIds.BaseEventType.ToString(),
                    new[] { "/LocalTime" }, "LocalTime")
            };
            await Assert.ThrowsAsync<ParserException>(
                () => parser.ParseEventFilterAsync(query, context, default)).ConfigureAwait(false);
        }

        [Fact]
        public async Task SimpleStatementTest2Async()
        {
            const string query = "SELECT * FROM BaseEventType WHERE /Severity > 5 AND /SourceName = 'SouthMotor'";

            var parser = new FilterQueryParser(_serializer);
            var context = new TestParserContext
            {
                new IdentifierMetaData(Opc.Ua.ObjectTypeIds.BaseEventType.ToString(),
                    new[] { "/Severity" }, "Severity"),
                new IdentifierMetaData(Opc.Ua.ObjectTypeIds.BaseEventType.ToString(),
                    new[] { "/SourceNode" }, "SourceNode"),
                new IdentifierMetaData(Opc.Ua.ObjectTypeIds.BaseEventType.ToString(),
                    new[] { "/SourceName" }, "SourceName"),
                new IdentifierMetaData(Opc.Ua.ObjectTypeIds.BaseEventType.ToString(),
                    new[] { "/LocalTime" }, "LocalTime")
            };
            var eventFilter = await parser.ParseEventFilterAsync(query, context, default).ConfigureAwait(false);

            Assert.NotNull(eventFilter);
            Assert.NotNull(eventFilter.SelectClauses);
            Assert.NotEmpty(eventFilter.SelectClauses);
            Assert.NotNull(eventFilter.WhereClause);
            var where = eventFilter.WhereClause!.Elements;
            Assert.NotNull(where);
            Assert.NotEmpty(where);
            _output.WriteLine(_serializer.SerializeToString(eventFilter, SerializeOption.Indented));
        }

        [Fact]
        public async Task SimpleStatementTest3Async()
        {
            const string query = "SELECT * FROM BaseEventType WHERE /Severity > 5 AND /SourceName = 'SouthMotor'";

            var parser = new FilterQueryParser(_serializer);
            var context = new TestParserContext
            {
                new IdentifierMetaData(Opc.Ua.ObjectTypeIds.BaseEventType.ToString(),
                    new[] { "/Severity" }, "Severity"),
                new IdentifierMetaData(Opc.Ua.ObjectTypeIds.BaseEventType.ToString(),
                    new[] { "/SourceNode" }, "SourceNode"),
                new IdentifierMetaData(Opc.Ua.ObjectTypeIds.BaseEventType.ToString(),
                    new[] { "/SourceName" }, "SourceName"),
                new IdentifierMetaData(Opc.Ua.ObjectTypeIds.BaseEventType.ToString(),
                    new[] { "/LocalTime" }, "LocalTime")
            };
            var eventFilter = await parser.ParseEventFilterAsync(query, context, default).ConfigureAwait(false);

            Assert.NotNull(eventFilter);
            Assert.NotNull(eventFilter.SelectClauses);
            Assert.NotEmpty(eventFilter.SelectClauses);
            Assert.NotNull(eventFilter.WhereClause);
            var where = eventFilter.WhereClause!.Elements;
            Assert.NotNull(where);
            Assert.NotEmpty(where);
            _output.WriteLine(_serializer.SerializeToString(eventFilter, SerializeOption.Indented));
        }

        [Fact]
        public async Task SimpleStatementTest4Async()
        {
            const string query =
                @"
PREFIX t http://test/#
SELECT *
FROM BaseEventType
WHERE
    (/Severity > 5 AND /Severity < 10)
  OR /SourceNode IN('t:i=1544'^^NodeId, 't:i=1545'^^NodeId)";

            var parser = new FilterQueryParser(_serializer);
            var context = new TestParserContext
            {
                new IdentifierMetaData(Opc.Ua.ObjectTypeIds.BaseEventType.ToString(),
                    new[] { "/Severity" }, "Severity"),
                new IdentifierMetaData(Opc.Ua.ObjectTypeIds.BaseEventType.ToString(),
                    new[] { "/SourceNode" }, "SourceNode"),
                new IdentifierMetaData(Opc.Ua.ObjectTypeIds.BaseEventType.ToString(),
                    new[] { "/SourceName" }, "SourceName"),
                new IdentifierMetaData(Opc.Ua.ObjectTypeIds.BaseEventType.ToString(),
                    new[] { "/LocalTime" }, "LocalTime")
            };
            var eventFilter = await parser.ParseEventFilterAsync(query, context, default).ConfigureAwait(false);

            Assert.NotNull(eventFilter);
            Assert.NotNull(eventFilter.SelectClauses);
            Assert.NotEmpty(eventFilter.SelectClauses);
            Assert.NotNull(eventFilter.WhereClause);
            var where = eventFilter.WhereClause!.Elements;
            Assert.NotNull(where);
            Assert.NotEmpty(where);
            _output.WriteLine(_serializer.SerializeToString(eventFilter, SerializeOption.Indented));
        }

        [Fact]
        public async Task AlarmsTestFilterTestAsync()
        {
            var parser = new FilterQueryParser(_serializer);
            var context = new TestParserContext
            {
                new IdentifierMetaData("i=2041",
                    new[] { "/Severity" }, "Severity"),
                new IdentifierMetaData("i=2041",
                    new[] { "/SourceNode" }, "SourceNode"),
                new IdentifierMetaData("i=10751",
                    new[] { "/Additional" }, "Additional")
            };

            const string query1 = @"
PREFIX ac <http://opcfoundation.org/AlarmCondition>
SELECT * FROM i=10751, i=2041
WHERE
    OFTYPE i=10751 AND
    /SourceNode IN ('ac:s=1%3aMetals%2fSouthMotor'^^NodeId)
";
            var filter1 = await parser.ParseEventFilterAsync(query1, context, default).ConfigureAwait(false);

            // Test with default aliasing
            const string query2 = @"
PREFIX ac <http://opcfoundation.org/AlarmCondition>
SELECT /Additional, /Severity, /SourceNode FROM TripAlarmType, BaseEventType
WHERE
    OFTYPE TripAlarmType AND
    /SourceNode IN ('ac:s=1%3aMetals%2fSouthMotor'^^NodeId)
";
            var filter2 = await parser.ParseEventFilterAsync(query2, context, default).ConfigureAwait(false);

            var whereExpected = new ContentFilterModel
            {
                Elements = new[]
                {
                    new ContentFilterElementModel // Index 0
                    {
                        FilterOperator = FilterOperatorType.And,
                        FilterOperands = new [] {
                            // Filter element indexes
                            new FilterOperandModel
                            {
                                Index = 1
                            },
                            new FilterOperandModel
                            {
                                Index = 2
                            }
                        }
                    },
                    new ContentFilterElementModel // Index 1
                    {
                        FilterOperator = FilterOperatorType.InList,
                        FilterOperands = new [] {
                            // Source node property of base event type should be ...
                            new FilterOperandModel
                            {
                                // Simple attribute
                                AttributeId = NodeAttribute.Value,
                                // Note: Default namespace which we omit
                                NodeId = Opc.Ua.ObjectTypeIds.BaseEventType.ToString(),
                                BrowsePath = new[] { "/" + Opc.Ua.BrowseNames.SourceNode }
                            },
                            // ...In the following list
                            new FilterOperandModel
                            {
                                // Literal
                                Value = "http://opcfoundation.org/AlarmCondition#s=1%3aMetals%2fSouthMotor",
                                DataType = "NodeId"
                            }
                        }
                    },
                    new ContentFilterElementModel // Index 2
                    {
                        FilterOperator = FilterOperatorType.OfType,
                        FilterOperands = new[] {
                            new FilterOperandModel
                            {
                                // Literal
                                Value = Opc.Ua.ObjectTypeIds.TripAlarmType.ToString(),
                                DataType = "NodeId"
                            }
                        }
                    }
                }
            };

            Assert.NotNull(filter1);
            Assert.NotNull(filter1.SelectClauses);
            Assert.Equal(3, filter1.SelectClauses!.Count);
            Assert.NotNull(filter1.WhereClause);

            var f1 = _serializer.SerializeToString(filter1);
            var f2 = _serializer.SerializeToString(filter2);
            Assert.Equal(f1, f2);

            var evtFilter = _serializer.SerializeToString(filter1.WhereClause);
            var expFilter = _serializer.SerializeToString(whereExpected);
            Assert.Equal(expFilter, evtFilter);
        }
    }
}
