// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Parser {
    using Antlr4.Runtime;
    using System;
    using Opc.Ua;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System.Linq;
    using System.Collections.Generic;
    using Antlr4.Runtime.Tree;

    /// <summary>
    /// OPC UA Query Filter declaration parser
    /// </summary>
    public sealed class FilterExpression {

        /// <summary>
        /// Returns the select clause defined by the filter declaration.
        /// </summary>
        public SimpleAttributeOperandCollection SelectClause { get; }

        /// <summary>
        /// Returns the where clause defined by the filter declaration.
        /// </summary>
        public ContentFilter WhereClause { get; }

        /// <summary>
        /// Create expression from string
        /// </summary>
        public FilterExpression(string filterStatement) {

            // Parse
            var lexer = new FilterLexer(new AntlrInputStream(filterStatement));
            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(new RaiseException<int>());
            var parser = new FilterParser(new CommonTokenStream(lexer));
            parser.RemoveErrorListeners();
            parser.AddErrorListener(new RaiseException<IToken>());
            var context = parser.parse();

            // Fill in select and where clause
            SelectClause = new SimpleAttributeOperandCollection();
            WhereClause = new ContentFilter();

            if (context.selectList().STAR() != null) {
                // Select all / default
            }
            else {
                foreach (var expr in context.selectList().selectexpr()) {
                    expr.attr_op().GetText();

                    var nodeId = expr.attr_op().nodeId().STRING_LITERAL().GetText();
                    var browsePathElems = expr.attr_op().nodeId().browsePathElement();
                    var attributeId = Enum.Parse<NodeAttribute>(expr.attr_op().attributeId().GetText(), true);

                    var operand = new SimpleAttributeOperand {
                       // TypeDefinitionId = expr.attr_op()
                       // AttributeId = (field.InstanceDeclaration.NodeClass == NodeClass.Object) ? Attributes.NodeId : Attributes.Value,
                       // BrowsePath = field.InstanceDeclaration.BrowsePath
                    };
                    SelectClause.Add(operand);
                }
            }

            Evaluate(context.elem_op());
        }

        private ElementOperand Evaluate(FilterParser.Elem_opContext elem_opContext) {

            if (elem_opContext.BR_OPEN() != null ||
                elem_opContext.BR_CLOSE() != null) {
                elem_opContext = elem_opContext.elem_op()[0];
            }

            var comparison = elem_opContext.COMPARISON_OPERATOR();
            var or = elem_opContext.OR();
            var and = elem_opContext.AND();
            if (comparison != null || or != null || and != null) {
                // First is elem or attribute[0], second is lit or attribute[1]
                var lhs = GetLhs(elem_opContext);
                var rhs = GetRhs(elem_opContext);
                WhereClause.Push(and != null ? FilterOperator.And : or != null ? FilterOperator.Or :
                    ToOperator(comparison), lhs, rhs);
                return new ElementOperand { Index = (uint)WhereClause.Elements.Count - 1 };
            }

            var inlist = elem_opContext.IN();
            if (inlist != null) {
                var lhs = GetLhs(elem_opContext);
                var list = Evaluate(elem_opContext.list);
                WhereClause.Push(FilterOperator.InList, lhs.YieldReturn().Concat(list).ToArray());
                return new ElementOperand { Index = (uint)WhereClause.Elements.Count - 1 };
            }

            throw new Exception();
        }

        private FilterOperator ToOperator(ITerminalNode comparison) {
            switch (comparison.Symbol.TokenIndex) {
                case FilterLexer.GREATER_THAN:
                    return FilterOperator.GreaterThan;
                case FilterLexer.GREATER_THAN_EQUALS:
                    return FilterOperator.GreaterThanOrEqual;
                case FilterLexer.LESS_THAN:
                    return FilterOperator.LessThan;
                case FilterLexer.LESS_THAN_EQUALS:
                    return FilterOperator.LessThanOrEqual;
                case FilterLexer.EQUALS:
                    return FilterOperator.Equals;
                case FilterLexer.LIKE:
                    return FilterOperator.Like;
                case FilterLexer.BITWISE_AND:
                    return FilterOperator.BitwiseAnd;
                case FilterLexer.BITWISE_OR:
                    return FilterOperator.BitwiseOr;
            }
            throw new Exception();
        }

        private IEnumerable<FilterOperand> Evaluate(Func<FilterParser.ListContext> list) {
            throw new NotImplementedException();
        }

        private FilterOperand GetLhs(FilterParser.Elem_opContext elem_opContext) {
            FilterOperand lhs = Evaluate(elem_opContext.elem_op()?[0]);
            if (lhs == null) {
                lhs = Evaluate(elem_opContext.attr_op().First());
            }
            return lhs;
        }

        private FilterOperand GetRhs(FilterParser.Elem_opContext elem_opContext) {
            FilterOperand rhs = Evaluate(elem_opContext.lit_op());
            if (rhs == null) {
                rhs = Evaluate(elem_opContext.attr_op().Last());
            }
            if (rhs == null) {
                var elem = elem_opContext.elem_op();
                if (elem?.Length == 2) {
                    rhs = Evaluate(elem_opContext.elem_op()?[1]);
                }
            }
            return rhs;
        }

        private LiteralOperand Evaluate(FilterParser.Lit_opContext lit_opContext) {
            return new LiteralOperand(lit_opContext.GetText());
        }

        private SimpleAttributeOperand Evaluate(FilterParser.Attr_opContext attr_opContext) {
            var nodeId = attr_opContext.nodeId().STRING_LITERAL().GetText();
            var browsePathElems = attr_opContext.nodeId().browsePathElement();
            var attributeId = Enum.Parse<NodeAttribute>(attr_opContext.attributeId().GetText(), true);

            return new SimpleAttributeOperand {
                // TypeDefinitionId = expr.attr_op()
                // AttributeId = (field.InstanceDeclaration.NodeClass == NodeClass.Object) ? Attributes.NodeId : Attributes.Value,
                // BrowsePath = field.InstanceDeclaration.BrowsePath
            };
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


#if FALSE
        /// <summary>
        /// Where
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="records"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private IEnumerable<T> Select<T>(IEnumerable<T> records,
            FilterParser.ParseContext context) {
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
            FilterParser.ParseContext context) {
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
            FilterParser.ScalarFunctionContext context) {

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
            FilterParser.ScalarTypeFunctionContext context) {

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
        /// Parse scalar function
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private Expression ParseScalarFunction(ParameterExpression parameter,
            FilterParser.ExprContext context) {
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
            FilterParser.ExprContext context) {

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
        /// <param name="aggregateResultColumnNames"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        private Expression ParseParameterBinding(ParameterExpression parameter,
            FilterParser.ColumnNameContext context, List<string> aggregateResultColumnNames = null) {
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
        private JsonToken ParseLiteralValue(FilterParser.Literal_valueContext context) {
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
            FilterParser.Object_literalContext context) {
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
        private JsonToken ParseScalarLiteralValue(FilterParser.Scalar_literalContext context) {
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
#endif
    }
}
