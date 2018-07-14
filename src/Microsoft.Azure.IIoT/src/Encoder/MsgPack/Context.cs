// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MsgPack {
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Context object to carry type serializers during serialization
    /// </summary>
    public class SerializerContext {
        /// <summary>
        /// Get serializer for type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public Serializer<T> Get<T>() {
            if (!_cache.TryGetValue(typeof(T), out var serializer)) {
                serializer = new ReflectionSerializer<T>();
                _cache.Add(typeof(T), serializer);
            }
            return (Serializer<T>)serializer;
        }

        /// <summary>
        /// Add a custom serializer to the context
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serializer"></param>
        protected void Add<T>(Serializer<T> serializer) {
            _cache.Add(typeof(T), serializer);
        }

        private Dictionary<Type, object> _cache = new Dictionary<Type, object>();
    }
}