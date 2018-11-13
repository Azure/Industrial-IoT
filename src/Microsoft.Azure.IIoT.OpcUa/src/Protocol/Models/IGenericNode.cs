// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {

    /// <summary>
    /// Represents a generic node
    /// </summary>
    public interface IGenericNode {

        /// <summary>
        /// Indexed access to values
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        object this[uint attribute] { get; set; }

        /// <summary>
        /// Retrieve attribute from node or return a
        /// default as per node class
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="attribute"></param>
        /// <returns></returns>
        T GetAttribute<T>(uint attribute);

        /// <summary>
        /// Set attribute
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        void SetAttribute<T>(uint attribute, T value);

        /// <summary>
        /// Try get attribute or return false if attribute
        /// value does not exist.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        bool TryGetAttribute<T>(uint attribute, out T value);
    }
}
