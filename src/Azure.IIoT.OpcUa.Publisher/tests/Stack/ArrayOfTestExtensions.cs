// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Test-only helpers bridging the UA-.NETStandard 2.0 <see cref="ArrayOf{T}"/>
    /// value type (which does not implement <see cref="IEnumerable{T}"/>) to the
    /// LINQ/xunit surface the existing test bodies expect.
    /// </summary>
    internal static class ArrayOfTestExtensions
    {
        public static IEnumerable<T> AsEnumerable<T>(this ArrayOf<T> array)
        {
            return array.ToArray() ?? Array.Empty<T>();
        }

        public static IEnumerable<TResult> Select<T, TResult>(this ArrayOf<T> array,
            Func<T, TResult> selector)
        {
            return array.AsEnumerable().Select(selector);
        }

        public static T? ElementAtOrDefault<T>(this ArrayOf<T> array, int index)
        {
            return array.AsEnumerable().ElementAtOrDefault(index);
        }
    }
}
