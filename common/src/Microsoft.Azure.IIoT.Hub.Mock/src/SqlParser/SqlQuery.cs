// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Mock.SqlParser {
    using Microsoft.Azure.IIoT.Hub.Models;
    using Antlr4.Runtime;
    using Antlr4.Runtime.Tree;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Linq.Expressions;

    /// <summary>
    /// Mock device registry query processor
    /// </summary>
    internal sealed class SqlQuery {

        /// <summary>
        /// Create Registry
        /// </summary>
        public SqlQuery(IIoTHub hub) {
            _hub = hub;
        }

        /// <summary>
        /// Parse and retrieve results
        /// </summary>
        /// <param name="sqlSelectString"></param>
        /// <returns></returns>
        public IEnumerable<JToken> Query(string sqlSelectString) {

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
                    _hub.Modules.Select(m => m.Twin), context)
                        .Select(JToken.FromObject), context);
            }
            if (context.collection()?.DEVICES() != null) {
                return Project(Select(
                    _hub.Devices.Select(d => d.Twin), context)
                        .Select(JToken.FromObject), context);
            }
            if (context.collection()?.DEVICES_JOBS() != null) {
                return Project(Select(
                    _hub.Jobs, context)
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

            // TODO: Implement projection

            return records;
        }


        /// <summary>
        /// Parse string function
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private Expression<Func<JsonToken, JsonToken>> ParseScalarLambda(
            SqlSelectParser.ScalarFunctionContext context) {

            if (context.STARTS_WITH() != null) {
                return s => JToken.FromObject(
                    ((string)(JToken)s).StartsWith(ParseStringValue(context.STRING_LITERAL()),
                        StringComparison.Ordinal));
            }

            if (context.ENDS_WITH() != null) {
                return s => JToken.FromObject(
                    ((string)(JToken)s).EndsWith(ParseStringValue(context.STRING_LITERAL()),
                        StringComparison.Ordinal));
            }
            throw new ArgumentException("Bad function");
        }

        /// <summary>
        /// Parse test function
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private Expression<Func<JsonToken, JsonToken>> ParseScalarLambda(
            SqlSelectParser.ScalarTypeFunctionContext context) {

            if (context.IS_DEFINED() != null) {
                return s => JToken.FromObject(s != null);
            }
            if (context.IS_NULL() != null) {
                return s => JToken.FromObject(s != null &&
                    (((JToken)s).Type == JTokenType.Null));
            }
            if (context.IS_BOOL() != null) {
                return s => JToken.FromObject(s != null &&
                    (((JToken)s).Type == JTokenType.Boolean));
            }
            if (context.IS_NUMBER() != null) {
                return s => JToken.FromObject(s != null &&
                    (((JToken)s).Type == JTokenType.Float || ((JToken)s).Type == JTokenType.Integer));
            }
            if (context.IS_STRING() != null) {
                return s => JToken.FromObject(s != null &&
                    (((JToken)s).Type == JTokenType.String));
            }
            if (context.IS_OBJECT() != null) {
                return s => JToken.FromObject(s != null &&
                    (((JToken)s).Type == JTokenType.Object));
            }
            return s => JToken.FromObject(true);
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

            return CreateBinaryExpression(context.COMPARISON_OPERATOR()?.GetText() ?? "=",
                lhs, rhs);
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
                    return (t, s) => SelectTargetToken(t.Capabilities, s);
                case "configurations":
                    // TODO
                    return (t, s) => null;
                case "connectionstate":
                    return (t, s) => SelectTargetToken(t.ConnectionState, s);
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
        private static JsonToken SelectTargetToken<T>(T target, string path) where T : class {
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
        /// <param name="parameter"></param>
        /// <param name="aggregateResultColumnNames"></param>
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
        private string ParseStringValue(ITerminalNode stringLiteralContext) {
            return stringLiteralContext.GetText().TrimQuotes();
        }

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
            public override int GetHashCode() {
                return JToken.EqualityComparer.GetHashCode(_jtoken);
            }

            /// <inheritdoc/>
            public override string ToString() {
                return _jtoken.ToString();
            }

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

        private readonly IIoTHub _hub;
    }
}
