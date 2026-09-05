// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisherAEE2ETests.TestExtensions
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    internal static class OpcUaNodeId
    {
        public static string Normalize(string nodeId, IReadOnlyList<string> namespaceUris)
        {
            ArgumentException.ThrowIfNullOrEmpty(nodeId);
            ArgumentNullException.ThrowIfNull(namespaceUris);
            string namespaceUri;
            string identifier;
            if (nodeId.StartsWith("ns=", StringComparison.Ordinal))
            {
                var separator = nodeId.IndexOf(';');
                if (separator < 0 || !ushort.TryParse(nodeId.AsSpan(3, separator - 3),
                    NumberStyles.None, CultureInfo.InvariantCulture, out var index) ||
                    index >= namespaceUris.Count)
                {
                    throw new FormatException($"NodeId '{nodeId}' has an unknown namespace index.");
                }
                namespaceUri = namespaceUris[index];
                identifier = nodeId[(separator + 1)..];
            }
            else if (nodeId.StartsWith("nsu=", StringComparison.Ordinal))
            {
                var separator = nodeId.IndexOf(';');
                if (separator < 0)
                {
                    throw new FormatException($"NodeId '{nodeId}' has no identifier.");
                }
                namespaceUri = Uri.UnescapeDataString(nodeId[4..separator]);
                identifier = nodeId[(separator + 1)..];
            }
            else if (nodeId.Length >= 2 && nodeId[1] == '=' && namespaceUris.Count > 0)
            {
                namespaceUri = namespaceUris[0];
                identifier = nodeId;
            }
            else
            {
                // Match the namespace prefix, not the last '#': a string
                // identifier may itself contain '#' or ';'.
                namespaceUri = namespaceUris
                    .Where(uri => !string.IsNullOrEmpty(uri) &&
                        nodeId.StartsWith(uri + "#", StringComparison.Ordinal))
                    .OrderByDescending(uri => uri.Length)
                    .FirstOrDefault();
                if (namespaceUri is null)
                {
                    throw new FormatException($"NodeId '{nodeId}' has an unknown namespace URI.");
                }
                identifier = nodeId[(namespaceUri.Length + 1)..];
            }
            if (!namespaceUris.Contains(namespaceUri, StringComparer.Ordinal) ||
                !Uri.TryCreate(namespaceUri, UriKind.Absolute, out _) ||
                identifier.Length < 2 || identifier[1] != '=')
            {
                throw new FormatException($"NodeId '{nodeId}' is invalid for the server namespace table.");
            }
            var value = identifier[2..];
            identifier = identifier[0] switch
            {
                'i' when uint.TryParse(value, NumberStyles.None,
                    CultureInfo.InvariantCulture, out var numeric)
                    => "i=" + numeric.ToString(CultureInfo.InvariantCulture),
                's' => identifier,
                'g' when Guid.TryParse(value, out var guid) => "g=" + guid.ToString("D"),
                'b' => "b=" + Convert.ToBase64String(Convert.FromBase64String(value)),
                _ => throw new FormatException($"NodeId '{nodeId}' has an invalid identifier.")
            };
            return namespaceUri + "#" + identifier;
        }
    }
}
