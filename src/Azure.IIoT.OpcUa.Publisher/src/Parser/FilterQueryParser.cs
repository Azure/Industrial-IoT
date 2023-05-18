// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Parser
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Extensions.Serializers;
    using Irony.Parsing;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Parses strings into an event filter for simpler event
    /// subscription.
    /// </summary>
    public sealed class FilterQueryParser : IFilterParser
    {
        /// <summary>
        /// Create parser
        /// </summary>
        /// <param name="serializer"></param>
        public FilterQueryParser(IJsonSerializer serializer)
        {
            _serializer = serializer;
        }

        /// <inheritdoc/>
        public async Task<EventFilterModel> ParseEventFilterAsync(string query,
            IFilterParserContext context, CancellationToken ct)
        {
            var parser = new Parser(_grammar);
            var syntaxTree = parser.Parse(query);
            if (syntaxTree.HasErrors())
            {
                throw ParserException.Create("Parsing query failed.",
                    syntaxTree);
            }

            // Build event filter from syntax tree
            return await FilterModelBuilder.BuildEventFilterAsync(
                syntaxTree, context, _serializer, ct).ConfigureAwait(false);
        }

        private readonly IJsonSerializer _serializer;
        private readonly FilterQueryGrammar _grammar = new();
    }
}
