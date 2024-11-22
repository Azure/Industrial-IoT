﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Result set
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="Results"></param>
    /// <param name="Errors"></param>
    public record struct ResultSet<T>(IReadOnlyList<T> Results,
        IReadOnlyList<ServiceResult> Errors);

    /// <summary>
    /// Result set helpers
    /// </summary>
    internal static class ResultSet
    {
        /// <summary>
        /// Empty result set
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static ResultSet<T> Empty<T>()
        {
            return new ResultSet<T>(Array.Empty<T>(), Array.Empty<ServiceResult>());
        }
    }
}
