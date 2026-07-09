// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using System.Text.Json.Nodes;

    /// <summary>
    /// Test helper that produces <see cref="JsonNode"/> instances from
    /// objects and arrays, matching the JSON representation the variant
    /// encoder produces so they can be compared with
    /// <see cref="JsonNode.DeepEquals(JsonNode, JsonNode)"/>.
    /// </summary>
    internal static class TestJson
    {
        private static readonly IJsonSerializer kSerializer = new NewtonsoftJsonSerializer();

        /// <summary>
        /// Create a json node from an object
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static JsonNode? FromObject(object? value)
        {
            if (value is JsonNode node)
            {
                return node.DeepClone();
            }
            return JsonNode.Parse(kSerializer.SerializeToString(value));
        }

        /// <summary>
        /// Create a json array node from the passed values
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static JsonNode? FromArray(params object?[] values)
        {
            return JsonNode.Parse(kSerializer.SerializeToString(values));
        }
    }
}
