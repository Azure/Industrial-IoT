// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AspNetCore.Http
{
    using global::Azure.IIoT.OpcUa.Core.Exceptions;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Convert the in-repo <c>Azure.IIoT.OpcUa.Core</c> error envelope
    /// (<see cref="ErrorDetails"/>) to an ASP.NET <see cref="ProblemDetails"/>.
    /// Replaces the former <c>Legacy.Extensions.AspNetCore</c> helper of the same
    /// shape. The <see cref="ErrorDetails.Extensions"/> values are kept as
    /// <see cref="System.Text.Json.JsonElement"/> and are boxed into the
    /// <see cref="ProblemDetails.Extensions"/> dictionary.
    /// </summary>
    public static class CoreProblemDetailsEx
    {
        /// <summary>
        /// Convert to problem details
        /// </summary>
        /// <param name="problem"></param>
        /// <returns></returns>
        public static ProblemDetails ToProblemDetails(this ErrorDetails problem)
        {
            ArgumentNullException.ThrowIfNull(problem);
            var result = new ProblemDetails
            {
                Title = problem.Title,
                Status = problem.Status,
                Detail = problem.Detail,
                Instance = problem.Instance,
                Type = problem.Type
            };
            foreach (var kv in problem.Extensions)
            {
                result.Extensions[kv.Key] = kv.Value;
            }
            return result;
        }

        /// <summary>
        /// Convert to problem details
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static ProblemDetails ToProblemDetails(this MethodCallStatusException ex)
        {
            ArgumentNullException.ThrowIfNull(ex);
            return ex.Details.ToProblemDetails();
        }
    }
}
