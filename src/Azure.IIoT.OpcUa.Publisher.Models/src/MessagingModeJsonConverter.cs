// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Reads and writes <see cref="MessagingMode"/> by name.
    /// </summary>
    /// <remarks>
    /// The proprietary sample modes were removed in OPC Publisher 3.0. A
    /// configuration file written by 2.x can still name them, so they are
    /// reported with a migration message that names the replacement instead of
    /// failing as an unrecognized enum value.
    /// </remarks>
    internal sealed class MessagingModeJsonConverter : JsonConverter<MessagingMode>
    {
        /// <inheritdoc/>
        public override MessagingMode Read(ref Utf8JsonReader reader,
            Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number &&
                reader.TryGetInt32(out var numeric) &&
                Enum.IsDefined(typeof(MessagingMode), numeric))
            {
                return (MessagingMode)numeric;
            }
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException(
                    "A messaging mode must be written as a string.");
            }
            var value = reader.GetString();
            if (TryGetRemovedModeReplacement(value, out var replacement))
            {
                throw new JsonException(
                    $"The messaging mode '{value}' was removed in OPC Publisher 3.0. " +
                    $"It emitted a proprietary message format that the OPC UA PubSub " +
                    $"runtime cannot produce. Use '{replacement}' instead.");
            }
            if (Enum.TryParse<MessagingMode>(value, ignoreCase: true, out var mode) &&
                Enum.IsDefined(typeof(MessagingMode), mode))
            {
                return mode;
            }
            throw new JsonException($"'{value}' is not a known messaging mode.");
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, MessagingMode value,
            JsonSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(writer);
            writer.WriteStringValue(value.ToString());
        }

        /// <summary>
        /// Get the replacement for a mode removed in 3.0.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="replacement"></param>
        internal static bool TryGetRemovedModeReplacement(string? value,
            out string replacement)
        {
            if (StringComparer.OrdinalIgnoreCase.Equals(value, "Samples"))
            {
                replacement = nameof(MessagingMode.PubSub);
                return true;
            }
            if (StringComparer.OrdinalIgnoreCase.Equals(value, "FullSamples"))
            {
                replacement = nameof(MessagingMode.FullNetworkMessages);
                return true;
            }
            replacement = string.Empty;
            return false;
        }
    }
}
