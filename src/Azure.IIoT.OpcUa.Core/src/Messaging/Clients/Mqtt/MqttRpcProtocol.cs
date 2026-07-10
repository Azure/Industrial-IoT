// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.Mqtt
{
    using System;
    using System.Buffers;

    /// <summary>
    /// Pure (broker independent) helpers implementing the mqtt request/response
    /// wire protocol used for direct method invocation. The exact topic strings,
    /// correlation scheme and status envelope match the former
    /// Furly.Extensions.Mqtt implementation for backwards compatibility.
    /// </summary>
    internal static class MqttRpcProtocol
    {
        /// <summary>Empty payload sentinel (works around empty payload bugs).</summary>
        public static readonly ReadOnlySequence<byte> EmptyPayload = new([0]);

        /// <summary>Response path segment.</summary>
        public const string ResPath = "res";

        /// <summary>Request id query parameter marker.</summary>
        public const string RequestIdKey = "?$rid=";

        /// <summary>Status code user property name.</summary>
        public const string StatusCodeKey = "StatusCode";

        /// <summary>
        /// The response topic a v5 client subscribes to and advertises.
        /// </summary>
        /// <param name="clientId"></param>
        public static string V5ResponseTopic(string clientId)
        {
            return $"{clientId}/responses";
        }

        /// <summary>
        /// The request topic used to invoke a method (v5).
        /// </summary>
        /// <param name="target"></param>
        /// <param name="method"></param>
        public static string V5RequestTopic(string target, string method)
        {
            return $"{target}/{method}";
        }

        /// <summary>
        /// The request topic used to invoke a method (v3.11). The request id is
        /// carried through the topic path.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="method"></param>
        /// <param name="requestId"></param>
        public static string V311RequestTopic(string target, string method, Guid requestId)
        {
            return $"{target}/{method}/{RequestIdKey}{requestId}";
        }

        /// <summary>
        /// The response subscription filter for a v3.11 client.
        /// </summary>
        /// <param name="target"></param>
        public static string V311ResponseFilter(string target)
        {
            return $"{target}/{ResPath}/+/+";
        }

        /// <summary>
        /// The response topic a v3.11 server publishes on.
        /// </summary>
        /// <param name="topicRoot"></param>
        /// <param name="statusCode"></param>
        /// <param name="requestId"></param>
        public static string V311ResponseTopic(string topicRoot, int statusCode, Guid requestId)
        {
            return $"{topicRoot}/{ResPath}/{statusCode}/{RequestIdKey}{requestId}";
        }

        /// <summary>
        /// Parse a v3.11 response topic and extract the status code and request
        /// id. Returns false if the topic does not have the expected shape.
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="target"></param>
        /// <param name="status"></param>
        /// <param name="requestId"></param>
        public static bool TryParseV311Response(string topic, string target,
            out int status, out Guid requestId)
        {
            status = 0;
            requestId = default;
            var components = topic.Replace($"{target}/{ResPath}/", string.Empty,
                StringComparison.Ordinal).Split('/');
            if (components.Length < 2)
            {
                return false;
            }
            if (!int.TryParse(components[^2],
                System.Globalization.CultureInfo.InvariantCulture, out status))
            {
                return false;
            }
            var last = components[^1];
            if (!last.StartsWith(RequestIdKey, StringComparison.Ordinal))
            {
                return false;
            }
            return Guid.TryParse(last.AsSpan(RequestIdKey.Length), out requestId);
        }

        /// <summary>
        /// Classify an inbound message into a request or response and extract the
        /// request id, method name and topic root. Mirror of the former Furly
        /// <c>ParseMessage</c> logic (v5 by correlation data / response topic,
        /// v3.11 by the request id topic query parameter).
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="correlationData"></param>
        /// <param name="responseTopic"></param>
        /// <param name="isRequest"></param>
        /// <param name="requestId"></param>
        /// <param name="methodName"></param>
        /// <param name="topicRoot"></param>
        public static bool ParseMessage(string topic, byte[]? correlationData,
            string? responseTopic, out bool isRequest, out Guid requestId,
            out string? methodName, out string? topicRoot)
        {
            var components = topic.Split('/');
            var last = components[^1];
            if (correlationData != null || responseTopic != null)
            {
                //
                // Mqtt5 mode. The message is a request if it contains a response
                // topic, and a response otherwise (mapped to a pending request).
                //
                methodName = last;
                requestId = correlationData?.Length == 16 ?
                    new Guid(correlationData) : Guid.NewGuid();
                if (components.Length < 2)
                {
                    isRequest = false;
                    topicRoot = default;
                    return false;
                }
                topicRoot = topic[..(last.Length + 1)];
                isRequest = responseTopic != null;
                return true;
            }

            if (!last.StartsWith(RequestIdKey, StringComparison.Ordinal) ||
                components.Length < 2)
            {
                methodName = default;
                requestId = default;
                isRequest = false;
                topicRoot = default;
                return false;
            }

            //
            // Mqtt3 mode. The topic must have the request query param and at
            // least 2 components (name or res + status code and the request id).
            //
            if (components.Length >= 3 && components[^3] == ResPath)
            {
                methodName = default;
                topicRoot = topic.Split(ResPath)[0].TrimEnd('/');
                isRequest = false;
            }
            else
            {
                methodName = components[^2];
                topicRoot = topic.Split(methodName)[0].TrimEnd('/');
                isRequest = true;
            }
            return Guid.TryParse(last.AsSpan(RequestIdKey.Length), out requestId);
        }
    }
}
