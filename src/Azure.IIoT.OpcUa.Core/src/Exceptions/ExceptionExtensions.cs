// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Exceptions
{
    using Microsoft.Extensions.Diagnostics.ExceptionSummarization;
    using System;

    /// <summary>
    /// Exception extensions
    /// </summary>
    public static class ExceptionExtensions
    {
        /// <summary>
        /// Convert to method call status exception
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="status"></param>
        /// <param name="summarizer"></param>
        /// <returns></returns>
        /// <exception cref="MethodCallStatusException"></exception>
        public static MethodCallStatusException AsMethodCallStatusException(
            this Exception ex, int? status = null, IExceptionSummarizer? summarizer = null)
        {
            if (ex is MethodCallStatusException mcs)
            {
                return mcs;
            }
            if (summarizer != null)
            {
                var summary = summarizer.Summarize(ex);
                throw new MethodCallStatusException(status,
                    summary.AdditionalDetails, summary.Description,
                    summary.ExceptionType);
            }
            throw new MethodCallStatusException(status ?? 500,
                ex?.Message ?? ex?.ToString() ?? "Unknown");
        }
    }
}
