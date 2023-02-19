// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Shared.Models {
    using Microsoft.Azure.IIoT.Serializers;
    using System;

    /// <summary>
    /// History read results extensions
    /// </summary>
    public static class HistoryResultModelEx {

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="convert"></param>
        public static HistoryReadResponseModel<T> ToSpecificModel<T>(
            this HistoryReadResponseModel<VariantValue> model, Func<VariantValue, T> convert) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            return new HistoryReadResponseModel<T> {
                History = convert(model.History),
                ContinuationToken = model.ContinuationToken,
                ErrorInfo = model.ErrorInfo
            };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="convert"></param>
        public static HistoryReadNextResponseModel<T> ToSpecificModel<T>(
            this HistoryReadNextResponseModel<VariantValue> model, Func<VariantValue, T> convert) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            return new HistoryReadNextResponseModel<T> {
                History = convert(model.History),
                ContinuationToken = model.ContinuationToken,
                ErrorInfo = model.ErrorInfo
            };
        }
    }
}
