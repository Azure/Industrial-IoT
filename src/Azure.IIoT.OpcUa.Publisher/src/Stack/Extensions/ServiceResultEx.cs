// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Furly.Exceptions;
    using Opc.Ua;
    using System;

    /// <summary>
    /// Service result model extensions
    /// </summary>
    internal static class ServiceResultEx
    {
        /// <summary>
        /// Convert exception to service result model
        /// </summary>
        /// <param name="sr"></param>
        /// <returns></returns>
        public static ServiceResultModel ToServiceResultModel(this ServiceResult sr)
        {
            return new ServiceResultModel
            {
                StatusCode = sr.Code,
                ErrorMessage = sr.LocalizedText?.Text,
                Locale = sr.LocalizedText?.Locale,
                AdditionalInfo = sr.AdditionalInfo,
                NamespaceUri = sr.NamespaceUri,
                SymbolicId = sr.SymbolicId ??
                    StatusCode.LookupSymbolicId(sr.Code),
                Inner = sr.InnerResult == null ||
                    sr.InnerResult.StatusCode == StatusCodes.Good ?
                        null : sr.InnerResult.ToServiceResultModel()
            };
        }
        /// <summary>
        /// Convert exception to service result model
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static ServiceResultModel ToServiceResultModel(this Exception e)
        {
            switch (e)
            {
                case ServiceResultException sre:
                    return sre.Result.ToServiceResultModel();
                case TimeoutException:
                    return Create(StatusCodes.BadTimeout, e.Message);
                case OperationCanceledException:
                    return Create(StatusCodes.BadRequestCancelledByClient, e.Message);
                case ResourceInvalidStateException:
                    return Create(StatusCodes.BadInvalidState, e.Message);
                case ResourceNotFoundException:
                    return Create(StatusCodes.BadNotFound, e.Message);
                case ResourceConflictException:
                    return Create(StatusCodes.BadDuplicateReferenceNotAllowed, e.Message);
                default:
                    return Create(StatusCodes.Bad, e.Message);
            }
            static ServiceResultModel Create(uint code, string message) =>
                new ServiceResultModel
                {
                    ErrorMessage = message,
                    SymbolicId = StatusCode.LookupSymbolicId(code),
                    StatusCode = code
                };
        }

        /// <summary>
        /// Create result recursively from diagnostics
        /// </summary>
        /// <param name="statusCode"></param>
        /// <param name="diagnostics"></param>
        /// <param name="stringTable"></param>
        /// <returns></returns>
        public static ServiceResultModel CreateResultModel(this StatusCode statusCode,
            DiagnosticInfo? diagnostics = null, StringCollection? stringTable = null)
        {
            return new ServiceResultModel
            {
                // The last operation result is the one that caused the service to fail.
                StatusCode = statusCode.Code,
                SymbolicId = stringTable?.GetStringFromTable(diagnostics?.SymbolicId) ??
                    StatusCode.LookupSymbolicId(statusCode.Code),
                ErrorMessage = stringTable?.GetStringFromTable(diagnostics?.LocalizedText),
                NamespaceUri = stringTable?.GetStringFromTable(diagnostics?.NamespaceUri),
                Locale = stringTable?.GetStringFromTable(diagnostics?.Locale),
                AdditionalInfo = diagnostics?.AdditionalInfo,
                Inner = diagnostics?.InnerStatusCode == null ||
                    diagnostics.InnerStatusCode == StatusCodes.Good ? null :
                    diagnostics.InnerStatusCode.CreateResultModel(
                        diagnostics?.InnerDiagnosticInfo, stringTable)
            };
        }

        /// <summary>
        /// Get string from string table
        /// </summary>
        /// <param name="stringTable"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private static string? GetStringFromTable(
            this StringCollection stringTable, int? index)
        {
            if (index == null || stringTable == null ||
                index.Value >= stringTable.Count ||
                index.Value < 0)
            {
                return null;
            }
            var str = stringTable[index.Value];
            if (string.IsNullOrWhiteSpace(str))
            {
                return null;
            }
            return str;
        }
    }
}
