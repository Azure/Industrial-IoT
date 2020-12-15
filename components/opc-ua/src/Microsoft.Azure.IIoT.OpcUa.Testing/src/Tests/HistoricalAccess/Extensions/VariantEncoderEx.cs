// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using Opc.Ua;
    using System;
    using System.Linq;

    /// <summary>
    /// Variant encoder extensions to encode and decode details and results
    /// </summary>
    internal static class VariantEncoderEx {

        /// <summary>
        /// Convert read raw modified details
        /// </summary>
        /// <param name="codec"></param>
        /// <param name="details"></param>
        /// <returns></returns>
        public static VariantValue Encode(this IVariantEncoder codec, ReadValuesDetailsModel details) {
            if (details == null) {
                throw new ArgumentNullException(nameof(details));
            }
            if (details.EndTime == null && details.StartTime == null) {
                throw new ArgumentException("Start time and end time cannot both be null", nameof(details));
            }
            if ((details.StartTime == null || details.EndTime == null) && ((details.NumValues ?? 0) == 0)) {
                throw new ArgumentException("Value bound must be set", nameof(details.NumValues));
            }
            return codec.Encode(new ExtensionObject(new ReadRawModifiedDetails {
                EndTime = details.EndTime ?? DateTime.MinValue,
                StartTime = details.StartTime ?? DateTime.MinValue,
                IsReadModified = false,
                ReturnBounds = details.ReturnBounds ?? false,
                NumValuesPerNode = details.NumValues ?? 0
            }));
        }


        /// <summary>
        /// Convert to results
        /// </summary>
        /// <param name="codec"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static HistoricValueModel[] DecodeValues(this IVariantEncoder codec, VariantValue result) {
            var extensionObject = codec.DecodeExtensionObject(result);
            if (extensionObject?.Body is HistoryData data) {
                var results = data.DataValues.Select(d => new HistoricValueModel {
                    ServerPicoseconds = d.ServerPicoseconds.ToNullable((ushort)0),
                    SourcePicoseconds = d.SourcePicoseconds.ToNullable((ushort)0),
                    ServerTimestamp = d.ServerTimestamp.ToNullable(DateTime.MinValue),
                    SourceTimestamp = d.SourceTimestamp.ToNullable(DateTime.MinValue),
                    StatusCode = d.StatusCode.ToNullable(StatusCodes.Good)?.CodeBits,
                    Value = d.WrappedValue == Variant.Null ? null : codec.Encode(d.WrappedValue)
                }).ToArray();
                if (extensionObject?.Body is HistoryModifiedData modified) {
                    if (modified.ModificationInfos.Count != data.DataValues.Count) {
                        throw new FormatException("Modification infos and data value count is not the same");
                    }
                    for (var i = 0; i < modified.ModificationInfos.Count; i++) {
                        results[i].ModificationInfo = new ModificationInfoModel {
                            ModificationTime =
                                modified.ModificationInfos[i].ModificationTime.ToNullable(DateTime.MinValue),
                            UserName =
                                modified.ModificationInfos[i].UserName
                        };
                    }
                }
                return results;
            }
            return null;
        }

        /// <summary>
        /// Encode extension object
        /// </summary>
        /// <param name="codec"></param>
        /// <param name="o"></param>
        /// <returns></returns>
        internal static VariantValue Encode(this IVariantEncoder codec, ExtensionObject o) {
            var variant = o == null ? Variant.Null : new Variant(o);
            return codec.Encode(variant);
        }

        /// <summary>
        /// Encode extension object
        /// </summary>
        /// <param name="codec"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        internal static ExtensionObject DecodeExtensionObject(this IVariantEncoder codec,
            VariantValue result) {
            if (result == null) {
                return null;
            }
            var variant = codec.Decode(result, BuiltInType.ExtensionObject);
            return variant.Value as ExtensionObject;
        }
    }
}
