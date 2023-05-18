// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Parser
{
    using Irony.Parsing;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Parser exception contains more information regarding source
    /// of the parser error than format exception provides
    /// </summary>
    public class ParserException : FormatException
    {
        /// <inheritdoc/>
        public ParserException()
        {
        }

        /// <inheritdoc/>
        public ParserException(string message)
            : base(message)
        {
        }

        /// <inheritdoc/>
        public ParserException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <inheritdoc/>
        internal static Exception Create(string message,
            ParseTree syntaxTree, params ParseTreeNode?[] nodes)
        {
            return new ParserException(BuildMessage(message, syntaxTree, nodes));
        }

        /// <inheritdoc/>
        internal static Exception Create(string message, Exception innerException,
            ParseTree syntaxTree, params ParseTreeNode[] nodes)
        {
            return new ParserException(BuildMessage(message, syntaxTree, nodes),
                innerException);
        }

        /// <summary>
        /// Create message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="syntaxTree"></param>
        /// <param name="nodes"></param>
        /// <returns></returns>
        private static string BuildMessage(string message, ParseTree syntaxTree,
            ParseTreeNode?[] nodes)
        {
            var lines = syntaxTree.SourceText.Split("\n")
                .Select(line => line.Trim('\r'))
                .ToArray();

            var errors = lines
                .Select(line =>
                {
                    var pointer = new char[line.Length + 10];
                    Array.Fill(pointer, ' ');
                    return (pointer, new List<string>());
                })
                .ToArray();

            foreach (var msg in syntaxTree.ParserMessages)
            {
                var entry = errors[msg.Location.Line];
                entry.pointer[msg.Location.Column] = '^';
                var m =
                    $"<--- (col:{msg.Location.Column}) {msg.Message} ----";
                entry.Item2.Add(m.PadLeft(msg.Location.Column + m.Length));
            }

            foreach (var node in nodes)
            {
                if (node == null)
                {
                    continue;
                }
                var entry = errors[node.Span.Location.Line];
                var len = node.Span.EndPosition - node.Span.Location.Position;
                for (var start = node.Span.Location.Column;
                    start < node.Span.Location.Column + len &&
                    start < entry.pointer.Length; start++)
                {
                    entry.pointer[start] = '~';
                }
                var m =
                    $"<--- (col:{node.Span.Location.Column}|len:{len}) ----";
                entry.Item2.Add(m.PadLeft(node.Span.Location.Column + m.Length));
            }

            var sb = new StringBuilder(message);
            sb.AppendLine();
            for (var line = 0; line < lines.Length; line++)
            {
                sb.AppendLine(lines[line]);
                if (errors[line].Item2.Count > 0)
                {
                    sb.AppendLine(
                        new string(errors[line].pointer));
                    errors[line].Item2
                        .ForEach(e => sb.AppendLine(e));
                }
            }
            return sb.ToString();
        }
    }
}
