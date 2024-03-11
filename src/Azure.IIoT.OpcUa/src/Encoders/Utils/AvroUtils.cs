// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.Utils
{
    using Avro;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Helper functions for avro
    /// </summary>
    internal static partial class AvroUtils
    {
        /// <summary>
        /// Namespace zero
        /// </summary>
        public const string kNamespaceZeroName = "org.opcfoundation.ua";

        /// <summary>
        /// Null schema
        /// </summary>
        public static Schema Null { get; } = PrimitiveSchema.NewInstance("null");

        /// <summary>
        /// Get a avro compliant name string
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string Escape(string name)
        {
            return Escape(name, false);
        }

        /// <summary>
        /// Get a avro compliant name string
        /// </summary>
        /// <param name="name"></param>
        /// <param name="remove"></param>
        /// <returns></returns>
        public static string Escape(string name, bool remove)
        {
            return EscapeAvroRegex().Replace(name.Replace('/', '_'),
                match => remove ? string.Empty : $"__{(int)match.Value[0]}");
        }


        [GeneratedRegex("[^a-zA-Z0-9_]")]
        private static partial Regex EscapeAvroRegex();
    }
}
