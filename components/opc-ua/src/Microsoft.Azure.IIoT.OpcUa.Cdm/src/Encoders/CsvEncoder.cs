// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Cdm.Storage {
    using Microsoft.Azure.IIoT.OpcUa.Subscriber.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Writes data tables into files on file storage
    /// </summary>
    public class CsvEncoder : IRecordEncoder {

        /// <inheritdoc/>
        public byte[] Encode<T>(List<T> data, string separator, bool addHeader = false) {
            var sb = new StringBuilder();
            var info = typeof(T).GetProperties();
            if (addHeader) {
                foreach (var prop in info) {
                    if (prop.Name != nameof(DataSetMessageModel.Payload)) {
                        AddValueToCsvStringBuilder(prop.Name, separator, sb);
                    }
                    else {
                        var payload = prop.GetValue(data[0]) as Dictionary<string, DataValueModel>;
                        foreach (var node in payload.OrderBy(i => i.Key)) {
                            AddValueToCsvStringBuilder($"{node.Key}_value", separator, sb);
                            AddValueToCsvStringBuilder($"{node.Key}_status", separator, sb);
                            AddValueToCsvStringBuilder($"{node.Key}_sourceTimestamp", separator, sb);
                            AddValueToCsvStringBuilder($"{node.Key}_serverTimestamp", separator, sb);
                        }
                    }
                }
                sb.Remove(sb.Length - 1, 1);
            }
            foreach (var obj in data) {
                sb.AppendLine();
                foreach (var prop in info) {
                    if (prop.Name != nameof(DataSetMessageModel.Payload)) {
                        AddValueToCsvStringBuilder(prop.GetValue(obj), separator, sb);
                    }
                    else {
                        var payload = prop.GetValue(obj) as Dictionary<string, DataValueModel>;
                        foreach (var node in payload.OrderBy(i => i.Key)) {
                            AddValueToCsvStringBuilder(node.Value.Value?.Value, separator, sb);
                            AddValueToCsvStringBuilder(node.Value.Status, separator, sb);
                            AddValueToCsvStringBuilder(node.Value.SourceTimestamp, separator, sb);
                            AddValueToCsvStringBuilder(node.Value.ServerTimestamp, separator, sb);
                        }
                    }
                }
                sb.Remove(sb.Length - 1, 1);
            }
            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        /// <summary>
        /// Add value
        /// </summary>
        /// <param name="value"></param>
        /// <param name="separator"></param>
        /// <param name="sb"></param>
        private void AddValueToCsvStringBuilder(object value,
            string separator, StringBuilder sb) {

            if (value != null) {
                var str = FormatValue(value);
                if (str.Contains(separator) ||
                    str.Contains("\"") || str.Contains("\r") ||
                    str.Contains("\n")) {
                    sb.Append('\"');
                    foreach (var nextChar in str) {
                        sb.Append(nextChar);
                        if (nextChar == '"') {
                            sb.Append('\"');
                        }
                    }
                    sb.Append('\"');
                }
                else {
                    sb.Append(str);
                }
            }
            sb.Append(separator);
        }

        /// <summary>
        /// Format value as string
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        private string FormatValue(object o) {
            switch (o) {
                case null:
                    return "";
                case VariantValue vv:
                    return vv.ToJson().TrimQuotes();
                case IFormattable f:
                    return f.ToString(null, CultureInfo.InvariantCulture);
                default:
                    return o.ToString();
            }
        }

        internal const string Type = "adls";
    }
}
