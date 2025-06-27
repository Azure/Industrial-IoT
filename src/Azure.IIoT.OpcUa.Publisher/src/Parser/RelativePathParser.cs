// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Parser
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Encoders.Utils;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Relative path parser according to
    /// https://reference.opcfoundation.org/v104/Core/docs/Part4/A.2/
    /// </summary>
    internal static class RelativePathParser
    {
        /// <summary>
        /// Convert to path object
        /// </summary>
        /// <param name="path"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public static IEnumerable<RelativePathElementModel> ToRelativePath(
            this string path, out string prefix)
        {
            if (path.Length == 0)
            {
                prefix = string.Empty;
                return [];
            }

            var index = 0;
            prefix = ExtractTargetName(path, ref index);
            if (index == path.Length)
            {
                return [];
            }
            return Parse(path[index..]);
        }

        /// <summary>
        /// Format relative path element information
        /// </summary>
        /// <param name="path"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public static IReadOnlyList<string> AsString(this IEnumerable<RelativePathElementModel> path,
            string? prefix = null)
        {
            var result = new List<string>();
            var value = new StringBuilder(prefix ?? string.Empty);
            foreach (var element in path)
            {
                var writeReference = false;
                if (element.IsAggregatesReference())
                {
                    value.Append('.');
                }
                else if (element.IsHierarchicalReference())
                {
                    value.Append('/');
                }
                else
                {
                    value.Append('<');
                    writeReference = true;
                }
                if (element.IsInverse ?? false)
                {
                    value.Append('!');
                }
                if (element.NoSubtypes ?? false)
                {
                    value.Append('#');
                }
                if (writeReference)
                {
                    if (!element.ReferenceTypeId.StartsWith("i=",
                            StringComparison.InvariantCulture) ||
                        !uint.TryParse(element.ReferenceTypeId.AsSpan(2), out var id) ||
                        !TypeMaps.ReferenceTypes.Value.TryGetBrowseName(id,
                            out var reference))
                    {
                        reference = element.ReferenceTypeId;
                    }
                    value.Append(reference)
                        .Append('>');
                }
                var escape = element.TargetName.AsSpan().IndexOfAny(kAllowedChars) != -1;
                if (escape)
                {
                    value.Append('[');
                }
                value.Append(element.TargetName);
                if (escape)
                {
                    value.Append(']');
                }
                result.Add(value.ToString());
                value.Clear();
            }
            return result;
        }

        /// <summary>
        /// Returns true if this is a aggregates reference.
        /// A aggregate is either HasComponent or HasProperty
        /// reference. Used to get components of an object
        /// or specifically properties of a variable.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static bool IsAggregatesReference(this RelativePathElementModel element)
        {
            return element.ReferenceTypeId ==
               nameof(Opc.Ua.ReferenceTypes.Aggregates) ||
                   element.ReferenceTypeId ==
                    Opc.Ua.ReferenceTypeIds.Aggregates.ToString();
        }

        /// <summary>
        /// Returns true if this is a hierachical reference.
        /// A hierarchical reference includes Aggregates but
        /// also Organizes (folder) or HasEventSource/HasNotifier
        /// and HasSubtype references
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static bool IsHierarchicalReference(this RelativePathElementModel element)
        {
            return element.ReferenceTypeId ==
               nameof(Opc.Ua.ReferenceTypes.HierarchicalReferences) ||
                   element.ReferenceTypeId ==
                    Opc.Ua.ReferenceTypeIds.HierarchicalReferences.ToString();
        }

        /// <summary>
        /// Convert to path object
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        private static IEnumerable<RelativePathElementModel> Parse(string path)
        {
            var index = 0;
            while (index < path.Length)
            {
                //
                // Parse relative path reference information
                // This should allow
                // - "/targeturi"
                // - ".targeturi"
                // - "!.parenturi"
                // - "!/parenturi"
                // - "<!#uri>parenturi"
                //
                var parseReference = false;
                string? referenceTypeId = null;
                var inverse = false;
                var includeSubtypes = true;
                var exit = false;
                while (index < path.Length && !exit)
                {
                    switch (path[index])
                    {
                        case '<':
                            if (referenceTypeId == null && !parseReference)
                            {
                                parseReference = true;
                                break;
                            }
                            throw new FormatException("Reference type set.");
                        case '!':
                            inverse = true;
                            break;
                        case '#':
                            includeSubtypes = false;
                            break;
                        case '/':
                            if (referenceTypeId == null && !parseReference)
                            {
                                referenceTypeId =
                                    Opc.Ua.ReferenceTypeIds.HierarchicalReferences.ToString();
                                break;
                            }
                            throw new FormatException("Reference type set.");
                        case '.':
                            if (referenceTypeId == null && !parseReference)
                            {
                                referenceTypeId =
                                    Opc.Ua.ReferenceTypeIds.Aggregates.ToString();
                                break;
                            }
                            throw new FormatException("Reference type set.");
                        default:
                            if (referenceTypeId == null && !parseReference)
                            {
                                throw new FormatException(
                                    "No reference type specified.");
                            }
                            exit = true;
                            break;
                    }
                    index++;
                }
                index--;

                // Parse the reference type
                if (parseReference)
                {
                    var builder = new StringBuilder();
                    while (index < path.Length)
                    {
                        if (path[index] == '<' && path[index - 1] != '&')
                        {
                            throw new FormatException(
                                "Reference contains a < which is not allowed.");
                        }
                        if (path[index] == '>' && path[index - 1] != '&')
                        {
                            if (index + 1 < path.Length && path[index + 1] == '>')
                            {
                                throw new FormatException(
                                    "Reference path ends in > followed by >.");
                            }
                            break;
                        }
                        if (path[index] != '&' || path[index - 1] == '&')
                        {
                            builder.Append(path[index]);
                        }
                        index++;
                        if (index == path.Length)
                        {
                            throw new FormatException(
                                "Reference path starts in < but does not end in >");
                        }
                    }
                    index++; // Skip >

                    var reference = builder.ToString();
                    if (string.IsNullOrEmpty(reference))
                    {
                        throw new FormatException(
                            "Missed to provide a reference name between < and >.");
                    }
                    if (TypeMaps.ReferenceTypes.Value.TryGetIdentifier(reference,
                        out var id))
                    {
                        referenceTypeId = new Opc.Ua.NodeId(id).ToString();
                    }
                    else
                    {
                        referenceTypeId = reference;
                    }
                }

                // Parse target
                var target = ExtractTargetName(path, ref index);

                yield return new RelativePathElementModel
                {
                    IsInverse = inverse ? true : null,
                    NoSubtypes = includeSubtypes ? null : true,
                    ReferenceTypeId = referenceTypeId
                        ?? throw new FormatException("No reference type found"),
                    TargetName = target
                };
            }
        }

        /// <summary>
        /// Extracts a target name which can be escaped with []
        /// </summary>
        /// <param name="path"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        private static string ExtractTargetName(string path, ref int index)
        {
            if (index >= path.Length)
            {
                return string.Empty;
            }
            var firstChar = path[index];
            var lastChar = firstChar;
            var builder = new StringBuilder();
            if (firstChar == '[')
            {
                index++;
            }
            while (index < path.Length)
            {
                switch (path[index])
                {
                    case '/':
                    case '.':
                    case '<':
                    case '#':
                    case '!':
                        if (lastChar == '&')
                        {
                            builder.Append(path[index]);
                            break;
                        }
                        // Check whether we are still escaping
                        if (firstChar == '[' && lastChar != ']')
                        {
                            builder.Append(path[index]);
                            break;
                        }
                        // No, we are done.
                        return builder.ToString();
                    case ']':
                    case '&':
                        break;
                    default:
                        if (lastChar == ']')
                        {
                            builder.Append(lastChar);
                        }
                        builder.Append(path[index]);
                        break;
                }
                lastChar = path[index];
                index++;
            }
            if (firstChar == '[' && lastChar != ']')
            {
                throw new FormatException(
                    "Sequence escaped with [ not closed with ].");
            }
            // Not escaping and reaching the end
            return builder.ToString();
        }

        private static readonly SearchValues<char> kAllowedChars
            = SearchValues.Create("/#.<>!");
    }
}
