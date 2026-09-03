// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisherAEE2ETests.TestExtensions
{
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Correlates OPC UA PubSub messages with the configured writer identity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The identity moved between the 2.9 writer and the 3.0 native stack:
    /// 2.9 compatibility messages wrote the configured name into the string
    /// <c>DataSetWriterId</c> and used <c>DataSetWriterGroup</c> on the network
    /// message. The native stack writes an allocated numeric
    /// <c>DataSetWriterId</c> and calls the network member
    /// <c>WriterGroupName</c>; strict encoding additionally carries the
    /// configured name as <c>DataSetWriterName</c>.
    /// </para>
    /// <para>
    /// The E2E configuration gives every writer group a name beginning with
    /// the logical writer identity. That makes the network-level name the
    /// stable correlation key in 3.0 while retaining the legacy inner-message
    /// fallbacks for older images.
    /// </para>
    /// </remarks>
    internal static class PubSubMessageMatcher
    {
        /// <summary>
        /// Enumerate network messages from a single message or a batched array.
        /// </summary>
        public static IEnumerable<JObject> EnumerateNetworkMessages(JToken root)
        {
            if (root is JArray array)
            {
                foreach (var item in array)
                {
                    if (item is JObject message)
                    {
                        yield return message;
                    }
                }
                yield break;
            }
            if (root is JObject single)
            {
                yield return single;
            }
        }

        /// <summary>
        /// Enumerate data set messages belonging to a configured writer.
        /// </summary>
        public static IEnumerable<PubSubDataSetMatch> Match(
            JObject networkMessage, string writerId)
        {
            ArgumentException.ThrowIfNullOrEmpty(writerId);
            if (!string.Equals((string?)networkMessage["MessageType"], "ua-data",
                StringComparison.Ordinal))
            {
                yield break;
            }

            var writerGroupName = ReadString(networkMessage, "WriterGroupName")
                ?? ReadString(networkMessage, "DataSetWriterGroup");
            var groupMatches = StartsWith(writerGroupName, writerId);
            if (networkMessage["Messages"] is not JArray dataSetMessages)
            {
                yield break;
            }

            foreach (var token in dataSetMessages)
            {
                if (token is not JObject dataSetMessage)
                {
                    continue;
                }
                var dataSetWriterName = ReadString(dataSetMessage,
                    "DataSetWriterName");
                //
                // DataSetWriterId is only a logical name on 2.9-shaped
                // compatibility messages. A JSON number is the 3.0 stack's
                // allocated ushort and must never be compared with the
                // configured string identifier.
                //
                var legacyWriterId = dataSetMessage["DataSetWriterId"]?.Type ==
                    JTokenType.String
                        ? (string?)dataSetMessage["DataSetWriterId"]
                        : null;
                if (!groupMatches &&
                    !StartsWith(dataSetWriterName, writerId) &&
                    !StartsWith(legacyWriterId, writerId))
                {
                    continue;
                }
                if (dataSetMessage["Payload"] is not JObject payload)
                {
                    continue;
                }
                yield return new PubSubDataSetMatch(
                    writerGroupName ?? string.Empty,
                    dataSetWriterName ?? legacyWriterId,
                    ReadString(dataSetMessage, "MessageType"),
                    payload);
            }
        }

        /// <summary>
        /// Describe the identity-bearing fields for timeout diagnostics.
        /// </summary>
        public static string Describe(JObject networkMessage)
        {
            var group = ReadString(networkMessage, "WriterGroupName")
                ?? ReadString(networkMessage, "DataSetWriterGroup")
                ?? "<none>";
            if (networkMessage["Messages"] is not JArray messages)
            {
                return $"group={group}, messages=<none>";
            }
            var writers = new List<string>();
            foreach (var token in messages)
            {
                if (token is not JObject message)
                {
                    continue;
                }
                writers.Add(
                    $"id={message["DataSetWriterId"]?.ToString() ?? "<none>"}," +
                    $"name={ReadString(message, "DataSetWriterName") ?? "<none>"}," +
                    $"type={ReadString(message, "MessageType") ?? "<none>"}");
            }
            return $"group={group}, writers=[{string.Join("; ", writers)}]";
        }

        private static string ReadString(JObject value, string property)
            => value[property]?.Type == JTokenType.String
                ? (string)value[property]
                : null;

        private static bool StartsWith(string value, string expected)
            => value?.StartsWith(expected, StringComparison.Ordinal) == true;
    }

    /// <summary>
    /// A data set message correlated with its logical writer.
    /// </summary>
    internal sealed record class PubSubDataSetMatch(
        string WriterGroupName,
        string DataSetWriterName,
        string MessageType,
        JObject Payload);
}
