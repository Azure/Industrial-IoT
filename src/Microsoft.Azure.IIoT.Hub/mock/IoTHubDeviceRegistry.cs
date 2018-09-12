// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Mock {
    using Microsoft.Azure.IIoT.Hub.Mock.SqlParser;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Antlr4.Runtime;
    using Antlr4.Runtime.Tree;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;

    /// <summary>
    /// Mock registry
    /// </summary>
    public class IoTHubDeviceRegistry : IIoTHubTwinServices {

        /// <summary>
        /// The host name the client is talking to
        /// </summary>
        public string HostName => "mock.azure-devices.net";

        /// <summary>
        /// List of devices for devices queries
        /// </summary>
        public List<IoTHubDeviceModel> Devices { get; set; }

        /// <summary>
        /// List of modules for module queries
        /// </summary>
        public List<IoTHubDeviceModel> Modules { get; set; }

        /// <summary>
        /// List of jobs for job queries
        /// </summary>
        public List<DeviceJobModel> Jobs { get; set; }

        /// <summary>
        /// Create Registry
        /// </summary>
        public IoTHubDeviceRegistry() {
            Devices = new List<IoTHubDeviceModel>();
            Modules = new List<IoTHubDeviceModel>();
            Jobs = new List<DeviceJobModel>();
        }

        /// <inheritdoc/>
        public Task<DeviceTwinModel> CreateOrUpdateAsync(DeviceTwinModel twin,
            bool forceUpdate) {

            var existing = Devices
                .Select(d => d.Twin)
                .FirstOrDefault(t => t.Id == twin.Id);
            if (existing == null) {
                // Create
                Devices.Add(new IoTHubDeviceModel {
                    Device = new DeviceModel {
                        Id = twin.Id
                    },
                    Twin = new DeviceTwinModel {
                        Id = twin.Id,
                        Etag = twin.Etag,
                        Properties = twin.Properties,
                        Tags = twin.Tags
                    }
                });
            }

            if (twin.ModuleId != null) {
                existing = Modules
                    .Select(m => m.Twin)
                    .FirstOrDefault(t => t.Id == twin.Id && t.ModuleId == twin.ModuleId);
                if (existing == null) {
                    // Create
                    Modules.Add(new IoTHubDeviceModel {
                        Device = new DeviceModel {
                            Id = twin.Id,
                            ModuleId = twin.ModuleId
                        },
                        Twin = new DeviceTwinModel {
                            Id = twin.Id,
                            ModuleId = twin.ModuleId,
                            Etag = twin.Etag,
                            Properties = twin.Properties,
                            Tags = twin.Tags
                        }
                    });
                }
            }

            if (existing != null) {
                existing.Tags = Merge(
                    existing.Tags, twin.Tags);
                if (existing.Properties == null) {
                    existing.Properties = new TwinPropertiesModel();
                }
                existing.Properties.Desired = Merge(
                    existing.Properties.Desired, twin.Properties?.Desired);
                existing.Properties.Reported = Merge(
                    existing.Properties.Reported, twin.Properties?.Reported);
            }
            else {
                existing = twin;
            }
            return Task.FromResult(twin);
        }

        /// <inheritdoc/>
        public Task<MethodResultModel> CallMethodAsync(string deviceId, string moduleId,
            MethodParameterModel parameters) {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public Task UpdatePropertiesAsync(string deviceId, string moduleId,
            Dictionary<string, JToken> properties, string etag) {

            var item = GetAsync(deviceId, moduleId).Result;

            if (item.Properties == null) {
                item.Properties = new TwinPropertiesModel();
            }
            item.Properties.Desired = Merge(item.Properties.Desired, properties);

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<DeviceTwinModel> GetAsync(string deviceId, string moduleId) {
            var result = moduleId == null ?
                Devices
                    .Select(d => d.Twin)
                    .FirstOrDefault(t => t.Id == deviceId) :
                Modules
                    .Select(m => m.Twin)
                    .FirstOrDefault(t => t.Id == deviceId && t.ModuleId == moduleId);
            if (result == null) {
                throw new ResourceNotFoundException();
            }
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task<DeviceModel> GetRegistrationAsync(string deviceId, string moduleId) {
            var result = moduleId == null ?
                Devices
                    .Select(d => d.Device)
                    .FirstOrDefault(d => d.Id == deviceId) :
                Modules
                    .Select(m => m.Device)
                    .FirstOrDefault(d => d.Id == deviceId && d.ModuleId == moduleId);
            if (result == null) {
                throw new ResourceNotFoundException();
            }
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task DeleteAsync(string deviceId, string moduleId, string etag) {
            if (moduleId == null) {
                Devices.RemoveAll(d => d.Device.Id == deviceId);
            }
            else {
                Modules.RemoveAll(d => d.Device.Id == deviceId && d.Device.ModuleId == moduleId);
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<QueryResultModel> QueryAsync(string query, string continuation,
            int? pageSize) {
            var result = Query(query).ToList();
            if (pageSize == null) {
                pageSize = int.MaxValue;
            }

            int.TryParse(continuation, out var index);
            var count = Math.Max(0, Math.Min(pageSize.Value, result.Count - index));

            return Task.FromResult(new QueryResultModel {
                ContinuationToken = count >= result.Count ? null : count.ToString(),
                Result = JArray.FromObject(result.Skip(index).Take(count).ToList())
            });
        }

        /// <summary>
        /// Merge properties
        /// </summary>
        /// <param name="target"></param>
        /// <param name="source"></param>
        private Dictionary<string, JToken> Merge(
            Dictionary<string, JToken> target,
            Dictionary<string, JToken> source) {

            if (source == null) {
                return target;
            }

            if (target == null) {
                return source;
            }

            foreach (var item in source) {
                if (target.ContainsKey(item.Key)) {
                    if (item.Value == null) {
                        target.Remove(item.Key);
                    }
                    else {
                        target[item.Key] = item.Value;
                    }
                }
                else {
                    target.Add(item.Key, item.Value);
                }
            }
            return target;
        }

        /// <summary>
        /// Parse and retrieve results
        /// </summary>
        /// <param name="sqlSelectString"></param>
        /// <returns></returns>
        private IEnumerable<JToken> Query(string sqlSelectString) {

            // Parse
            var lexer = new SqlSelectLexer(new AntlrInputStream(sqlSelectString));
            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(new RaiseException<int>());
            var parser = new SqlSelectParser(new CommonTokenStream(lexer));
            parser.RemoveErrorListeners();
            parser.AddErrorListener(new RaiseException<IToken>());
            var context = parser.parse();

            // Select
            if (context.collection()?.DEVICES_MODULES() != null) {
                return Project(Select(
                    Modules.Select(m => m.Twin), context)
                        .Select(d => d.ToJson()), context);
            }
            if (context.collection()?.DEVICES() != null) {
                return Project(Select(
                    Devices.Select(d => d.Twin), context)
                        .Select(d => d.ToJson()), context);
            }
            if (context.collection()?.DEVICES_JOBS() != null) {
                return Project(Select(
                    Jobs, context) // TODO: ToJson
                        .Select(JToken.FromObject), context);
            }
            throw new FormatException("Bad format");
        }

        /// <summary>
        /// Where
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="records"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private IEnumerable<T> Select<T>(IEnumerable<T> records,
            SqlSelectParser.ParseContext context) {
            var pe = Expression.Parameter(typeof(T));

            // Top
            if (context.selectList().topExpr() != null) {
                var maxCount = double.Parse(context.selectList().topExpr().maxCount().GetText(),
                    CultureInfo.InvariantCulture);
                records = records.Take((int)maxCount);
            }

            // Where
            if (context.expr() != null) {
                var where = (Expression<Func<T, bool>>)Expression.Lambda(
                    ParseWhereExpression(pe, context.expr()), pe);
                var compiled = where.Compile();
                records = records.Where(compiled);
            }

            return records;
        }

        /// <summary>
        /// Project
        /// </summary>
        /// <param name="records"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private IEnumerable<JToken> Project(IEnumerable<JToken> records,
            SqlSelectParser.ParseContext context) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            //
            return records;
            //   if (context.selectList().selectExpr().Any(e => e.aggregateExpr() != null)) {
            //       queryExp.Aggregate = ParseGroupByClause(context.selectList(), context.groupByClause());
            //   }
            //
            //   else if (context.selectList().selectExpr().Any()) {
            //       queryExp.Projection = ParseSelectList(context.selectList());
            //   }
            //
            //   if (context.orderByClause() != null) {
            //       queryExp.Sort = ParseOrderByClause(context.orderByClause(),
            //           queryExp.Aggregate?.AggregatedProperties?.Select(p => p.ResultColumnName).ToList());
            //   }
            //   return queryExp;
        }


        /// <summary>
        /// Parse string function
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private Expression<Func<JsonToken, bool>> ParseScalarLambda(
            SqlSelectParser.ScalarFunctionContext context) {

            if (context.STARTS_WITH() != null) {
                return s =>
                    ((string)(JToken)s).StartsWith(ParseStringValue(context.STRING_LITERAL()),
                        StringComparison.Ordinal);
            }

            if (context.ENDS_WITH() != null) {
                return s =>
                    ((string)(JToken)s).EndsWith(ParseStringValue(context.STRING_LITERAL()),
                        StringComparison.Ordinal);
            }
            throw new ArgumentException("Bad function");
        }

        /// <summary>
        /// Parse test function
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private Expression<Func<JsonToken, bool>> ParseScalarLambda(
            SqlSelectParser.ScalarTypeFunctionContext context) {

            if (context.IS_DEFINED() != null) {
                return s => s != null;
            }
            if (context.IS_NULL() != null) {
                return s => s != null && (((JToken)s).Type == JTokenType.Null);
            }
            if (context.IS_BOOL() != null) {
                return s => s != null && (((JToken)s).Type == JTokenType.Boolean);
            }
            if (context.IS_NUMBER() != null) {
                return s => s != null && (((JToken)s).Type == JTokenType.Float || ((JToken)s).Type == JTokenType.Integer);
            }
            if (context.IS_STRING() != null) {
                return s => s != null && (((JToken)s).Type == JTokenType.String);
            }
            if (context.IS_OBJECT() != null) {
                return s => s != null && (((JToken)s).Type == JTokenType.Object);
            }
            return s => true;
        }

        /// <summary>
        /// Parse expression
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private Expression ParseWhereExpression(ParameterExpression parameter,
            SqlSelectParser.ExprContext context) {
            if (context.NOT() != null) {
                return Expression.Not(
                    ParseWhereExpression(parameter, context.expr(0)));
            }
            if (context.AND() != null) {
                return Expression.And(
                    ParseWhereExpression(parameter, context.expr(0)),
                    ParseWhereExpression(parameter, context.expr(1)));
            }
            if (context.OR() != null) {
                return Expression.Or(
                    ParseWhereExpression(parameter, context.expr(0)),
                    ParseWhereExpression(parameter, context.expr(1)));
            }

            if (context.scalarFunction() != null) {
                return ParseScalarFunction(parameter, context);
            }

            if (context.BR_OPEN() != null) {
                return ParseWhereExpression(parameter, context.expr(0));
            }
            return ParseComparisonExpression(parameter, context);
        }

        /// <summary>
        /// Parse scalar function
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private Expression ParseScalarFunction(ParameterExpression parameter,
            SqlSelectParser.ExprContext context) {
            var scalarFunctionContext = context.scalarFunction();

            var expr = scalarFunctionContext.scalarTypeFunction() != null ?
                ParseScalarLambda(scalarFunctionContext.scalarTypeFunction()) :
                ParseScalarLambda(scalarFunctionContext);

            var lhs = Expression.Invoke(expr,
                ParseParameterBinding(parameter, scalarFunctionContext.columnName()));
            var rhs = Expression.Constant(context.literal_value() != null ?
                ParseLiteralValue(context.literal_value()) : (JsonToken)JToken.FromObject(true));

            return CreateBinaryExpression(context.COMPARISON_OPERATOR().GetText(), lhs, rhs);
        }

        /// <summary>
        /// Parse comparison expression
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private Expression ParseComparisonExpression(ParameterExpression parameter,
            SqlSelectParser.ExprContext context) {

            var lhs = ParseParameterBinding(parameter, context.columnName(0));
            Expression rhs;
            if (context.columnName().Length > 1) {
                rhs = ParseParameterBinding(parameter, context.columnName(1));
            }
            else if (context.array_literal() != null) {
                rhs = Expression.Constant(context.array_literal().literal_value()
                    .Select(ParseLiteralValue)
                    .ToArray());
            }
            else {
                rhs = Expression.Constant(ParseLiteralValue(context.literal_value()));
            }
            return CreateBinaryExpression(context.COMPARISON_OPERATOR().GetText(), lhs, rhs);
        }

        /// <summary>
        /// Parse target
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        private Expression<Func<DeviceTwinModel, string, JsonToken>> CreateBindingLambda(
            string identifier) {
            switch (identifier.ToLowerInvariant()) {
                case "tags":
                    return (t, s) => SelectTargetToken(t.Tags, s);
                case "reported":
                    return (t, s) => SelectTargetToken(t.Properties.Reported, s);
                case "desired":
                    return (t, s) => SelectTargetToken(t.Properties.Desired, s);
                case "properties":
                    return (t, s) => SelectTargetToken(t.Properties, s);
                case "capabilities":
                case "configurations":
                default:
                    return (t, s) => null;
            }
        }

        /// <summary>
        /// Select targeted token value based on path
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        private static JsonToken SelectTargetToken<T>(T target, string path) where T : class{
            if (target == null) {
                return null;
            }
            var root = JToken.FromObject(target);
            var selected = root.SelectToken(path, false);

            return selected;
        }

        /// <summary>
        /// Parse target
        /// </summary>
        /// <param name="context"></param>
        /// <param name="aggregateResultColumnNames"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        private Expression ParseParameterBinding(ParameterExpression parameter,
            SqlSelectParser.ColumnNameContext context, List<string> aggregateResultColumnNames = null) {
            if (aggregateResultColumnNames != null) {
                // ...
            }
            //    var property = new QueryProperty {
            //        PropertyName = context.propertyName().GetText()
            //    };
            //
            //    if (context.propertyName().IDENTIFIER().Length == 1) {
            //        if (aggregateResultColumnNames?.Contains(property.PropertyName) ?? false) {
            //            property.PropertyType = PropertyType.AggregatedProperty;
            //        }
            //        return property;
            //    }

            var identifiers = context.propertyName().IDENTIFIER().AsEnumerable();
            var root = ParseIdentifier(identifiers.First());
            if (root.Equals("properties", StringComparison.OrdinalIgnoreCase) && identifiers.Count() > 2) {
                identifiers = identifiers.Skip(1);
                root = ParseIdentifier(identifiers.First());
            }
            var path = string.Join(".", identifiers.Skip(1).Select(ParseIdentifier));
            return Expression.Invoke(CreateBindingLambda(root), parameter, Expression.Constant(path));
        }

        /// <summary>
        /// Build binary expression
        /// </summary>
        /// <param name="op"></param>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        private Expression CreateBinaryExpression(string op, Expression lhs, Expression rhs) {
            switch (op.ToLowerInvariant()) {
                case "=":
                    return Expression.Equal(lhs, rhs);
                case "!=":
                case "<>":
                    return Expression.NotEqual(lhs, rhs);
                case "<":
                    return Expression.LessThan(lhs, rhs);
                case "<=":
                    return Expression.LessThanOrEqual(lhs, rhs);
                case ">":
                    return Expression.GreaterThan(lhs, rhs);
                case ">=":
                    return Expression.GreaterThanOrEqual(lhs, rhs);
                // TODO   case "in":
                // TODO       return Expression.In;
                // TODO   case "nin":
                // TODO       return Expression.NotIn;
                default:
                    return lhs;
            }
        }

        /// <summary>
        /// Parse identifier including wrapped between [[ and ]]
        /// </summary>
        /// <param name="identifierNode"></param>
        /// <returns></returns>
        private string ParseIdentifier(ITerminalNode identifierNode) {
            if (identifierNode == null) {
                return null;
            }
            var identifier = identifierNode.GetText();
            if (identifier.StartsWith("[[", StringComparison.OrdinalIgnoreCase) &&
                identifier.EndsWith("]]", StringComparison.OrdinalIgnoreCase)) {
                return identifier.Substring(2, identifier.Length - 4);
            }
            return identifier;
        }

        /// <summary>
        /// Parse literal
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private JsonToken ParseLiteralValue(SqlSelectParser.Literal_valueContext context) {
            return context.object_literal() != null ?
                ParseObjectLiteralValue(context.object_literal()) :
                ParseScalarLiteralValue(context.scalar_literal());
        }

        /// <summary>
        /// Parse object literal
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private JsonToken ParseObjectLiteralValue(
            SqlSelectParser.Object_literalContext context) {
            var result = new Dictionary<string, JToken>();
            foreach (var kvpContext in context.keyValuePair()) {
                var key = ParseIdentifier(kvpContext.IDENTIFIER());
                var value = kvpContext.scalar_literal() != null
                    ? ParseScalarLiteralValue(kvpContext.scalar_literal())
                    : ParseObjectLiteralValue(kvpContext.object_literal());

                result.Add(key, value);
            }
            return JToken.FromObject(result);
        }

        /// <summary>
        /// Parse scalar
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private JsonToken ParseScalarLiteralValue(SqlSelectParser.Scalar_literalContext context) {
            if (context.BOOLEAN() != null) {
                return JToken.FromObject(bool.Parse(context.BOOLEAN().GetText()));
            }
            if (context.NUMERIC_LITERAL() != null) {
                return JToken.FromObject(double.Parse(context.NUMERIC_LITERAL().GetText(),
                    CultureInfo.InvariantCulture));
            }
            if (context.STRING_LITERAL() != null) {
                return JToken.FromObject(ParseStringValue(context.STRING_LITERAL()));
            }
            return null;
        }

        /// <summary>
        /// Parse literal strings by trimming quotes
        /// </summary>
        /// <param name="stringLiteralContext"></param>
        /// <returns></returns>
        private string ParseStringValue(ITerminalNode stringLiteralContext) =>
            stringLiteralContext.GetText().TrimQuotes();

        /// <summary>
        /// Token helper
        /// </summary>
        public class JsonToken {

            /// <summary>
            /// Create token
            /// </summary>
            /// <param name="jtoken"></param>
            public JsonToken(JToken jtoken) {
                _jtoken = jtoken;
            }

            /// <summary>
            /// Implicit conversion to <see cref="JToken"/>
            /// </summary>
            /// <param name="t"></param>
            public static implicit operator JToken(JsonToken t) => t._jtoken;

            /// <summary>
            /// Implicit conversion from <see cref="JToken"/>
            /// </summary>
            /// <param name="t"></param>
            public static implicit operator JsonToken(JToken t) => new JsonToken(t);

            /// <inheritdoc/>
            public override int GetHashCode() => JToken.EqualityComparer.GetHashCode(_jtoken);

            /// <inheritdoc/>
            public override string ToString() => _jtoken.ToString();

            /// <inheritdoc/>
            public static bool operator ==(JsonToken helper1, JsonToken helper2) {
                if (helper1?._jtoken == null || helper2?._jtoken == null) {
                    return helper1?._jtoken == helper2?._jtoken;
                }
                return JToken.DeepEquals(helper1._jtoken, helper2._jtoken);
            }

            /// <inheritdoc/>
            public static bool operator !=(JsonToken helper1, JsonToken helper2) =>
                !(helper1 == helper2);

            /// <inheritdoc/>
            public override bool Equals(object obj) {
                var helper = obj as JsonToken;
                if (helper?._jtoken == null || _jtoken == null) {
                    return helper?._jtoken == _jtoken;
                }
                return helper != null && JToken.DeepEquals(_jtoken, helper._jtoken);
            }

            private readonly JToken _jtoken;
        }


#if FALSE
        /// <summary>
        /// Parse select
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private ProjectionExpression ParseSelectList(SqlSelectParser.SelectListContext context) {
            // STAR = *
            // If present, project all properties. In this case, a null ProjectionExpression implies the same.
            if (context.STAR() != null) {
                return null;
            }

            var projectionExpression = new ProjectionExpression {
                ProjectedProperties = new List<ProjectionProperty>()
            };
            foreach (var selectExprContext in context.selectExpr()) {
                projectionExpression.ProjectedProperties.Add(ParseProjectionProperty(selectExprContext));
            }

            return projectionExpression;
        }

        /// <summary>
        /// Parse group by
        /// </summary>
        /// <param name="selectListContext"></param>
        /// <param name="groupByContext"></param>
        /// <returns></returns>
        private AggregationExpression ParseGroupByClause(SqlSelectParser.SelectListContext selectListContext,
            SqlSelectParser.GroupByClauseContext groupByContext) {
            // First ensure that all non-aggregate columns in the SELECT expression are part of the GROUP BY clause
            if (!selectListContext.selectExpr()
                .Where(s => s.aggregateExpr() == null)
                .All(s => groupByContext.columnName()
                .Any(g => g.GetText() == s.columnName().GetText()))) {
                throw new Exception("SqlQueryParserInvalidAggregateQuery");
            }

            var aggregate = new AggregationExpression {
                AggregatedProperties = new List<AggregationProperty>(),
                Keys = new List<ProjectionProperty>()
            };

            if (groupByContext != null) {
                foreach (var columnNameContext in groupByContext.columnName()) {
                    aggregate.Keys.Add(new ProjectionProperty {
                        Property = ParsePropertyTarget(columnNameContext),
                        Alias = ParseIdentifier(selectListContext.selectExpr()
                            .FirstOrDefault(s => s.aggregateExpr() == null &&
                                s.columnName().GetText() == columnNameContext.GetText())?.IDENTIFIER())
                    });
                }
            }

            var columnAlias = 0;
            foreach (var selectExprContext in selectListContext.selectExpr()) {
                if (selectExprContext.aggregateExpr() != null) {
                    aggregate.AggregatedProperties.Add(new AggregationProperty {
                        AggregationOperator = ParseAggregationOperator(
                            selectExprContext.aggregateExpr()),
                        Property = selectExprContext.aggregateExpr().columnName() != null ?
                            ParsePropertyTarget(selectExprContext.aggregateExpr().columnName()) : null,
                        ResultColumnName = selectExprContext.AS() != null ?
                            ParseIdentifier(selectExprContext.IDENTIFIER()) : $"#{columnAlias++}"
                    });
                }
            }

            return aggregate;
        }

        /// <summary>
        /// Parse order by
        /// </summary>
        /// <param name="context"></param>
        /// <param name="aggregateResultColumnNames"></param>
        /// <returns></returns>
        private List<SortExpression> ParseOrderByClause(
            SqlSelectParser.OrderByClauseContext context,
            List<string> aggregateResultColumnNames) {
            var sortExpressions = new List<SortExpression>();
            foreach (var sortColumnContext in context.sortColumn()) {
                var sortExpression = new SortExpression {
                    Order = sortColumnContext.DESC() != null ?
                        SortOrder.Descending : SortOrder.Ascending,
                    Property = ParsePropertyTarget(sortColumnContext.columnName(),
                    aggregateResultColumnNames)
                };
                sortExpressions.Add(sortExpression);
            }
            return sortExpressions;
        }

        /// <summary>
        /// Parse projection
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private ProjectionProperty ParseProjectionProperty(ParameterExpression parameter,
            SqlSelectParser.SelectExprContext context) {
            var projectionProperty = new ProjectionProperty {
                Property = context.columnName() != null ? ParseBindingLambda(parameter, context.columnName()) : null
            };
            if (context.AS() != null) {
                projectionProperty.Alias = ParseIdentifier(context.IDENTIFIER());
            }
            return projectionProperty;
        }

        /// <summary>
        /// Parse aggregation
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private AggregationOperatorType ParseAggregationOperator(
            SqlSelectParser.AggregateExprContext context) {
            if (context.COUNT() != null) {
                return AggregationOperatorType.Count;
            }
            if (context.SUM() != null) {
                return AggregationOperatorType.Sum;
            }
            if (context.AVG() != null) {
                return AggregationOperatorType.Average;
            }
            if (context.MIN() != null) {
                return AggregationOperatorType.Min;
            }
            return AggregationOperatorType.Max;
        }
#endif

        /// <summary>
        /// Error callback
        /// </summary>
        private class RaiseException<T> : IAntlrErrorListener<T> {
            public void SyntaxError(IRecognizer recognizer, T offendingSymbol,
                int line, int charPositionInLine, string msg, RecognitionException e) {
                throw new FormatException(
                    $"{offendingSymbol} at #{line}:{charPositionInLine} : {msg} ", e);
            }
        }
    }
}
