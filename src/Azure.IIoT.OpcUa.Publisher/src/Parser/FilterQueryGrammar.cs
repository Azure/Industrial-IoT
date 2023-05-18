// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Parser
{
    using Irony.Parsing;

    /// <summary>
    /// Grammar to parse event filter sql into event filter models.
    /// Based on Irony sample which is loosely based on SQL89 grammar
    /// from Gold parser
    /// </summary>
    [Language("EventFilterGrammar", "1",
        "Sql grammar for OPC UA event filter construction.")]
    public class FilterQueryGrammar : Grammar
    {
        /// <summary>
        /// Create grammar
        /// </summary>
        public FilterQueryGrammar() : base(false) // SQL is case insensitive
        {
            //
            // Simple alpha numeric identifiers, e.g, prefixes or
            // namespace indexes.
            //
            var prefixId = new IdentifierTerminal("prefixId");

            // Covers most node id and qualified name identifiers
            var id = new IdentifierTerminal("id", "=:/.;<>[]", "[/<.");
            // Allow quoting to support any character in the id
            var quoted = new StringLiteral("uaDefaultIdQuoted");
            quoted.AddStartEnd("´", StringOptions.AllowsAllEscapes);
            quoted.SetOutputTerminal(this, id);

            // Filter statement
            var semiOpt = new NonTerminal("semiOpt")
            {
                Rule = Empty | ";"
            };
            var stmt = new NonTerminal("stmt");
            var stmtLine = new NonTerminal("stmtLine")
            {
                Rule = stmt + semiOpt
            };
            var stmtList = new NonTerminal("stmtList");
            stmtList.Rule = MakePlusRule(stmtList, stmtLine);
            Root = stmtList; // Set as root of parse tree
            var namespaceUri = new StringLiteral("namespaceUri");
            namespaceUri.AddStartEnd("<", ">", StringOptions.NoEscapes);
            namespaceUri.AddStartEnd("http://", "#",
              StringOptions.NoEscapes);
            namespaceUri.AddStartEnd("https://", "#",
              StringOptions.NoEscapes);
            var PREFIX = ToTerm("PREFIX");
            var prefixDecl = new NonTerminal("prefixDecl")
            {
                Rule = PREFIX + prefixId + namespaceUri
            };
            var prefixList = new NonTerminal("prefixList");
            prefixList.Rule = MakePlusRule(prefixList, prefixDecl);
            var prefixListOpt = new NonTerminal("prefixListOpt")
            {
                Rule = Empty | prefixList
            };

            //
            // From selector which selects a type using the provided type
            // identifier and optionally assigns it an alias, e.g. from
            // BaseEventType E. It is optional, the default is assumed
            // to be "FROM BaseEventType".
            //
            var typeAliasOpt = new NonTerminal("typeAliasOpt")
            {
                Rule = Empty | prefixId
            };
            var type = new NonTerminal("type")
            {
                Rule = id + typeAliasOpt
            };
            var comma = ToTerm(",");
            var typeList = new NonTerminal("typeList");
            typeList.Rule = MakePlusRule(typeList, comma, type);

            var FROM = ToTerm("FROM");
            var fromClauseOpt = new NonTerminal("fromClauseOpt")
            {
                Rule = Empty | (FROM + typeList)
            };

            //
            // Projection selector which selects fields by browse path
            // from the FROM selected type identifier. Field source is
            // either a simple browse path (default / first from statement)
            // Or qualified, e.g., E.f:path1/f:path2/x:path3
            //
            var AS = ToTerm("AS");
            var asOpt = new NonTerminal("asOpt")
            {
                Rule = Empty | AS
            };
            var aliasOpt = new NonTerminal("aliasOpt")
            {
                Rule = Empty | (asOpt + prefixId)
            };
            var fieldSource = new NonTerminal("fieldSource")
            {
                Rule = id
            };
            var fieldItem = new NonTerminal("fieldItem")
            {
                Rule = fieldSource + aliasOpt
            };
            var fieldItemList = new NonTerminal("fieldItemList");
            fieldItemList.Rule = MakePlusRule(fieldItemList, comma,
              fieldItem);
            var selList = new NonTerminal("selList")
            {
                Rule = fieldItemList | "*"
            };

            //
            // Where expression filter based on boolean logic. An expression
            // can be a unary, binary, or complex expression (related_to).
            // The expressions correspond to the defined filter operands
            //
            var WHERE = ToTerm("WHERE");
            var expression = new NonTerminal("expression");
            var whereClauseOpt = new NonTerminal("whereClauseOpt")
            {
                Rule = Empty | (WHERE + expression)
            };

            //
            // Full select stmt which can be used inside filter
            // as expression (TODO)
            //
            var SELECT = ToTerm("SELECT");
            var selectStmt = new NonTerminal("selectStmt")
            {
                Rule = prefixListOpt + SELECT + selList +
                fromClauseOpt + whereClauseOpt
            };
            stmt.Rule = selectStmt;

            //
            // Unary expression
            //
            var NOT = ToTerm("NOT") | "!";
            var unOp = new NonTerminal("unOp")
            {
                Rule =
              NOT                       // Not_7
              | "ISNULL"                // IsNull_1
              | "INVIEW"                // InView_13
              | "OFTYPE"                // OfType_14
            };
            var unExpr = new NonTerminal("unExpr")
            {
                Rule = unOp + expression
            };

            RegisterOperators(10,
                NOT);
            RegisterOperators(9,
                "ISNULL", "INVIEW", "OFTYPE");

            //
            // Binary expressions
            //
            var binOp = new NonTerminal("binOp")
            {
                Rule =
              ToTerm("=") | "=="        // Equals_0
              | ">"                     // GreaterThan_2
              | "<"                     // LessThan_3
              | ">="                    // GreaterThanOrEqual_4
              | "<="                    // LessThanOrEqual_5
              | "LIKE"                  // Like_6
              | "<>" | "!="             // Not_7 + Equals_0
              | "!<"                    // Not_7 + LessThan_3
              | "!>"                    // Not_7 + GreaterThan_2
              | "AND"                   // And_10
              | "OR"                    // Or_11
              | "&"                     // BitwiseAnd_16
              | "|"                     // BitwiseOr_17
            };
            var binExpr = new NonTerminal("binExpr")
            {
                Rule = expression + binOp + expression
            };

            // Operators
            RegisterOperators(9,
              "=", "==", ">", "<", ">=", "<=", "<>",
              "!=", "!<", "!>", "LIKE");
            RegisterOperators(8,
              "&", "|");
            RegisterOperators(5,
              "AND");
            RegisterOperators(4,
              "OR");

            //
            // Now cover any special functions including IN(...),
            // BETWEEN(...), RELATEDTO(...)
            //
            var fnOp = new NonTerminal("fnOp")
            {
                Rule =
                ToTerm("IN")            // InList_9
              | "CAST"                  // Cast_12
              | "BETWEEN"               // Between_8
              | "RELATEDTO"             // RelatedTo_15
            };
            var exprList = new NonTerminal("exprList");
            exprList.Rule = MakePlusRule(exprList, comma, expression);
            var fnArgsList = new NonTerminal("fnArgsList")
            {
                Rule = "(" + exprList + ")"
            };
            var fnExpr = new NonTerminal("fnExpr")
            {
                Rule = expression + fnOp + fnArgsList
            };
            RegisterOperators(9,
                "IN", "BETWEEN", "RELATEDTO", "CAST");

            //
            // Literal expression. A literal expression is essentially
            // a constant which can be a number, string, or a string
            // qualified by a type using the double hat operator.
            //
            var anyNumber = new NumberLiteral("number");
            var anyString = new StringLiteral("string", "'",
              StringOptions.AllowsDoubledQuote);
            var anyBoolean = new NonTerminal("boolean")
            {
                Rule = ToTerm("true") | "false"
            };
            var typeOpt = new NonTerminal("typeOpt")
            {
                Rule = Empty | ("^^" + id)
            };
            var value = new NonTerminal("value")
            {
                Rule = anyNumber | anyString | anyBoolean
            };
            var literal = new NonTerminal("literal")
            {
                Rule = value + typeOpt
            };

            //
            // A selector selects a value either using a browse
            // path construct or a more complex select statement
            //
            var parSelectStmt = new NonTerminal("parSelectStmt")
            {
                Rule = "(" + selectStmt + ")"
            };

            var parExpression = new NonTerminal("parExpression")
            {
                Rule = "(" + expression + ")"
            };

            //
            // Expression is func, unary, binary, literal, inner
            // select or an expression in paranthesis
            //
            expression.Rule =
                unExpr | binExpr | fnExpr
              | literal | id
              | parSelectStmt
              | parExpression
              ;

            //
            // Support for sql comments
            //
            var comment = new CommentTerminal("comment", "/*", "*/");
            var lineComment = new CommentTerminal("line_comment",
              "--", "\n", "\r\n");
            NonGrammarTerminals.Add(comment);
            NonGrammarTerminals.Add(lineComment);

            MarkPunctuation(",", "(", ")", "*", "^^");
            MarkPunctuation(semiOpt, asOpt, PREFIX, SELECT, FROM, WHERE);

            MarkTransient(stmt, parExpression, stmtLine, expression,
              typeAliasOpt, asOpt, typeOpt, prefixListOpt, fnArgsList);
            //
            // Set flag InheritPrecedence so that it inherits precedence
            // value from it's children, and this precedence is used
            // in conflict resolution when binOp node is sitting on the
            // stack
            //
            binOp.SetFlag(TermFlags.InheritPrecedence);
            unOp.SetFlag(TermFlags.InheritPrecedence);
            fnOp.SetFlag(TermFlags.InheritPrecedence);
        }
    }
}
