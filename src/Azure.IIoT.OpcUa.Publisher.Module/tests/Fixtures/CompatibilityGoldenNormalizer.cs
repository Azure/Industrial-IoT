// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#nullable enable

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Nodes;

    /// <summary>
    /// Normalizes only run-specific fields from fixture-backed PubSub observations.
    /// The resulting semantic goldens are intentionally not byte-identity goldens.
    /// </summary>
    internal static class CompatibilityGoldenNormalizer
    {
        public static JsonNode Normalize(JsonElement element)
        {
            return Normalize(JsonNode.Parse(element.GetRawText()), null)
                ?? throw new InvalidOperationException("The PubSub message cannot be null.");
        }

        private static JsonNode? Normalize(JsonNode? node, string? propertyName)
        {
            if (node is JsonObject jsonObject)
            {
                foreach (var property in jsonObject.ToArray())
                {
                    var childPropertyName = string.Equals(propertyName, "EndpointUrl",
                        StringComparison.Ordinal) ? propertyName : property.Key;
                    var normalized = Normalize(property.Value, childPropertyName);
                    if (!ReferenceEquals(normalized, property.Value))
                    {
                        jsonObject[property.Key] = normalized;
                    }
                }
                return jsonObject;
            }

            if (node is JsonArray jsonArray)
            {
                for (var i = 0; i < jsonArray.Count; i++)
                {
                    var item = jsonArray[i];
                    var normalizedItem = Normalize(item, propertyName);
                    if (!ReferenceEquals(normalizedItem, item))
                    {
                        jsonArray[i] = normalizedItem;
                    }
                }
                return jsonArray;
            }

            if (node is not JsonValue jsonValue || propertyName is null)
            {
                return node;
            }

            if (IsSequence(propertyName))
            {
                return JsonValue.Create("<sequence>");
            }

            if (!jsonValue.TryGetValue<string>(out var text) || text is null)
            {
                return node;
            }

            if (string.Equals(propertyName, "EndpointUrl", StringComparison.Ordinal) &&
                Uri.TryCreate(text, UriKind.Absolute, out var endpoint))
            {
                return JsonValue.Create($"{endpoint.Scheme}://<host>:<port>{endpoint.AbsolutePath}");
            }
            if (Guid.TryParse(text, out _))
            {
                return JsonValue.Create("<guid>");
            }
            if (IsTimestamp(propertyName) && DateTimeOffset.TryParse(text,
                CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _))
            {
                return JsonValue.Create("<timestamp>");
            }
            return node;
        }

        private static bool IsSequence(string propertyName)
        {
            return propertyName is "SequenceNumber" or "NetworkMessageNumber";
        }

        private static bool IsTimestamp(string propertyName)
        {
            return propertyName is "Time" or "CurrentTime" or "PublishTime" ||
                propertyName.EndsWith("Timestamp", StringComparison.Ordinal);
        }
    }
}
