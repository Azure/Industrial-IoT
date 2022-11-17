// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

// #define USE_TASK_RUN

namespace Opc.Ua.Client {
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Session Client async extensions
    /// </summary>
    public static class SessionClientEx {
        /// <summary>
        /// Async browse service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="requestHeader"></param>
        /// <param name="view"></param>
        /// <param name="nodeToBrowse"></param>
        /// <param name="maxResultsToReturn"></param>
        /// <param name="browseDirection"></param>
        /// <param name="referenceTypeId"></param>
        /// <param name="includeSubtypes"></param>
        /// <param name="nodeClassMask"></param>
        /// <param name="resultMask"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static Task<BrowseResponse> BrowseAsync(this SessionClient client,
            RequestHeader requestHeader, ViewDescription view, NodeId nodeToBrowse,
            uint maxResultsToReturn, BrowseDirection browseDirection,
            NodeId referenceTypeId, bool includeSubtypes, uint nodeClassMask,
            BrowseResultMask resultMask = BrowseResultMask.All, CancellationToken ct = default) {
            return client.BrowseAsync(requestHeader, view, maxResultsToReturn,
                new BrowseDescriptionCollection {
                    new BrowseDescription {
                        BrowseDirection = browseDirection,
                        IncludeSubtypes = includeSubtypes,
                        NodeClassMask = nodeClassMask,
                        NodeId = nodeToBrowse,
                        ReferenceTypeId = referenceTypeId,
                        ResultMask = (uint)resultMask
                    }
                }, ct);
        }
        /// <summary>
        /// Validates responses
        /// </summary>
        /// <param name="results"></param>
        /// <param name="diagnostics"></param>
        public static void Validate<T>(IEnumerable<T> results,
            DiagnosticInfoCollection diagnostics) {
            Validate<T, object>(results, diagnostics, null);
        }

        /// <summary>
        /// Validates responses against requests
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="results"></param>
        /// <param name="diagnostics"></param>
        /// <param name="requested"></param>
        public static void Validate<T, R>(IEnumerable<T> results,
            DiagnosticInfoCollection diagnostics, IEnumerable<R> requested) {
            var resultsWithStatus = results?.ToList();
            if (resultsWithStatus == null || (resultsWithStatus.Count == 0 &&
                diagnostics.Count == 0)) {
                throw new ServiceResultException(StatusCodes.BadUnexpectedError,
                    "The server returned no results or diagnostics information.");
            }
            // Throw on bad responses.
            var expected = requested?.Count() ?? 1;
            if (resultsWithStatus.Count != expected) {
                throw new ServiceResultException(StatusCodes.BadUnexpectedError,
                    "The server returned a list without the expected number of elements.");
            }
            if (diagnostics != null && diagnostics.Count != 0 && diagnostics.Count != expected) {
                throw new ServiceResultException(StatusCodes.BadUnexpectedError,
                    "The server forgot to fill in the DiagnosticInfos array correctly.");
            }
        }
    }
}
