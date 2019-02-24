// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.History.Models {
    using Newtonsoft.Json.Linq;
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
        public static HistoryReadResultModel<T> ToSpecificModel<T>(
            this HistoryReadResultModel<JToken> model, Func<JToken, T> convert) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            return new HistoryReadResultModel<T> {
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
        public static HistoryReadNextResultModel<T> ToSpecificModel<T>(
            this HistoryReadNextResultModel<JToken> model, Func<JToken, T> convert) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            return new HistoryReadNextResultModel<T> {
                History = convert(model.History),
                ContinuationToken = model.ContinuationToken,
                ErrorInfo = model.ErrorInfo
            };
        }
    }
}
