// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace TestEventProcessor.BusinessLogic.Checkers
{
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// Class that will perform counting of values per nodeId.
    /// </summary>
    sealed class ValueChangeCounterPerNodeId : IDisposable
    {

        private readonly IDictionary<string, int> _valueChangesPerNodeId;
        private readonly SemaphoreSlim _lock;

        /// <summary>
        /// Constructor for counter class.
        /// </summary>
        /// <param name="logger"></param>
        public ValueChangeCounterPerNodeId()
        {
            _valueChangesPerNodeId = new Dictionary<string, int>();
            _lock = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Method that should be called for processing of events.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="_0"></param>
        /// <param name="_1"></param>
        public void ProcessEvent(
            string nodeId,
            DateTime _0,
            JToken _1
        )
        {
            _lock.Wait();
            try
            {
                if (!_valueChangesPerNodeId.TryGetValue(nodeId, out var value))
                {
                    _valueChangesPerNodeId.Add(nodeId, 1);
                }
                else
                {
                    _valueChangesPerNodeId[nodeId] = ++value;
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Stop and get value counters per nodeId.
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, int> Stop()
        {
            _lock.Wait();
            try
            {
                var copy = _valueChangesPerNodeId
                    .ToDictionary(entry => entry.Key, entry => entry.Value);
                return copy;
            }
            finally
            {
                _lock.Release();
            }
        }

        public void Dispose()
        {
            _lock.Dispose();
        }
    }
}
