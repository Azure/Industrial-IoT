// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.History.Models {
    using Microsoft.Azure.IIoT.Serializers;
    using System;

    /// <summary>
    /// Request node history read extensions
    /// </summary>
    public static class HistoryRequestModelEx {

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="request"></param>
        /// <param name="convert"></param>
        /// <returns></returns>
        public static HistoryUpdateRequestModel<VariantValue> ToRawModel<T>(
            this HistoryUpdateRequestModel<T> request, Func<T, VariantValue> convert) {
            return new HistoryUpdateRequestModel<VariantValue> {
                Details = convert(request.Details),
                BrowsePath = request.BrowsePath,
                NodeId = request.NodeId,
                Header = request.Header
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="request"></param>
        /// <param name="convert"></param>
        /// <returns></returns>
        public static HistoryReadRequestModel<VariantValue> ToRawModel<T>(
            this HistoryReadRequestModel<T> request, Func<T, VariantValue> convert) {
            return new HistoryReadRequestModel<VariantValue> {
                NodeId = request.NodeId,
                BrowsePath = request.BrowsePath,
                IndexRange = request.IndexRange,
                Details = convert(request.Details),
                Header = request.Header
            };
        }
    }
}
