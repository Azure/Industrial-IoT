// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#nullable enable

namespace Asset
{
    using Opc.Ua;
    using System.Reflection;

    public static class SimulatedFormExtension
    {
        /// <summary>
        /// Get datatype id
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        public static NodeId GetDataTypeId(this SimulatedForm form)
        {
            var fields = typeof(DataTypeIds).GetFields(
                BindingFlags.Public | BindingFlags.Static);
            foreach (var field in fields)
            {
                try
                {
                    var value = field.GetValue(typeof(DataTypeIds)) as NodeId;
                    if (value != null && field.Name == form.PayloadType)
                    {
                        return value;
                    }
                }
                catch
                {
                    continue;
                }
            }
            return NodeId.Null;
        }
    }
}
