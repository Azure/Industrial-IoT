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
            // The reference server in OPC Foundation 1.5.378.x no longer
            // auto-populates Locale on default LocalizedText translations of
            // status codes. Preserve the historical "en-US" default to keep
            // the publisher's external contract stable.
            var localizedText = sr.LocalizedText;
            var locale = localizedText?.Locale;
            if (locale == null && !string.IsNullOrEmpty(localizedText?.Text))
            {
                locale = "en-US";
            }
            return new ServiceResultModel
            {
                StatusCode = sr.Code,
                ErrorMessage = localizedText?.Text,
                Locale = locale,
                AdditionalInfo = sr.AdditionalInfo,
                NamespaceUri = sr.NamespaceUri,
                SymbolicId = sr.SymbolicId ?? sr.StatusCode.AsString(),
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
                    return Create(StatusCodes.BadEntryExists, e.Message);
                case ArgumentNullException:
                    return Create(StatusCodes.BadArgumentsMissing, e.Message);
                case ArgumentException:
                    return Create(StatusCodes.BadInvalidArgument, e.Message);
                default:
                    return Create(StatusCodes.Bad, e.Message);
            }
            static ServiceResultModel Create(StatusCode code, string message) => new()
            {
                ErrorMessage = message,
                SymbolicId = code.AsString(),
                StatusCode = code.Code
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
            // The reference server in OPC Foundation 1.5.378.x no longer
            // populates LocalizedText / Locale entries in the per-operation
            // diagnostics for default status-code translations. When the
            // client asked for diagnostics (i.e. diagnostics != null) but the
            // server omitted the localized text, fall back to the symbolic
            // status name and the historical "en-US" default to preserve the
            // publisher's external contract.
            var errorMessage = stringTable?.GetStringFromTable(diagnostics?.LocalizedText);
            var locale = stringTable?.GetStringFromTable(diagnostics?.Locale);
            if (diagnostics != null && errorMessage == null && StatusCode.IsBad(statusCode.Code))
            {
                errorMessage = statusCode.AsString();
                locale ??= "en-US";
            }
            else if (locale == null && !string.IsNullOrEmpty(errorMessage))
            {
                locale = "en-US";
            }
            return new ServiceResultModel
            {
                // The last operation result is the one that caused the service to fail.
                StatusCode = statusCode.Code,
                SymbolicId = stringTable?.GetStringFromTable(diagnostics?.SymbolicId) ??
                    statusCode.AsString(),
                ErrorMessage = errorMessage,
                NamespaceUri = stringTable?.GetStringFromTable(diagnostics?.NamespaceUri),
                Locale = locale,
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
