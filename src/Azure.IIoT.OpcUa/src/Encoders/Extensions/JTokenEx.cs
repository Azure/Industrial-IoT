// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;

    /// <summary>
    /// Json token extensions
    /// </summary>
    public static class JTokenEx
    {
        /// <summary>
        /// Returns dimensions of the multi dimensional array assuming
        /// it is not jagged.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static int[] GetDimensions(this JArray token, out JTokenType type)
        {
            var dimensions = new List<int>();
            type = JTokenType.Undefined;
            var array = token;
            while (array != null && array.Count != 0)
            {
                dimensions.Add(array.Count);
                type = array[0].Type;
                array = array[0] as JArray;
            }
            return dimensions.ToArray();
        }
    }
}
