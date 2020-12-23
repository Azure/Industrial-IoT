/* ========================================================================
 * Copyright (c) 2005-2013 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

namespace Opc.Ua.Aggregates {
    using Opc.Ua.Server;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// An object that manages aggregate factories supported by the server.
    /// </summary>
    public class AggregateManager : IDisposable {

        /// <summary>
        /// Initilizes the manager.
        /// </summary>
        public AggregateManager(IServerInternal server) {
            _server = server;
            _factories = new Dictionary<NodeId, AggregatorFactory>();
            _minimumProcessingInterval = 1000;
        }

        /// <summary>
        /// The finializer implementation.
        /// </summary>
        ~AggregateManager() {
            Dispose(false);
        }

        /// <summary>
        /// Frees any unmanaged resources.
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// An overrideable version of the Dispose.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_requestTimer")]
        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                // TBD
            }
        }

        /// <summary>
        /// Checks if the aggregate is supported by the server.
        /// </summary>
        /// <param name="aggregateId">The id of the aggregate function.</param>
        /// <returns>True if the aggregate is supported.</returns>
        public bool IsSupported(NodeId aggregateId) {
            if (NodeId.IsNull(aggregateId)) {
                return false;
            }

            lock (_lock) {
                return _factories.ContainsKey(aggregateId);
            }
        }

        /// <summary>
        /// The minimum processing interval for any aggregate calculation.
        /// </summary>
        public double MinimumProcessingInterval {
            get {
                lock (_lock) {
                    return _minimumProcessingInterval;
                }
            }

            set {
                lock (_lock) {
                    _minimumProcessingInterval = value;
                }
            }
        }

        /// <summary>
        /// Returns the default configuration for the specified variable id.
        /// </summary>
        /// <param name="variableId">The id of history data node.</param>
        /// <returns>The configuration.</returns>
        public AggregateConfiguration GetDefaultConfiguration(NodeId variableId) {
            System.Diagnostics.Contracts.Contract.Assume(variableId != null);
            lock (_lock) {
                if (_defaultConfiguration == null) {
                    _defaultConfiguration = new AggregateConfiguration {
                        PercentDataBad = 0,
                        PercentDataGood = 100,
                        TreatUncertainAsBad = false,
                        UseSlopedExtrapolation = false
                    };
                }

                return _defaultConfiguration;
            }
        }

        /// <summary>
        /// Sets the default aggregate configuration.
        /// </summary>
        /// <param name="configuration">The default aggregate configuration..</param>
        public void SetDefaultConfiguration(AggregateConfiguration configuration) {
            lock (_lock) {
                _defaultConfiguration = configuration;
            }
        }

        /// <summary>
        /// Creates a new aggregate calculator.
        /// </summary>
        /// <param name="aggregateId">The id of the aggregate function.</param>
        /// <param name="startTime">When to start processing.</param>
        /// <param name="endTime">When to stop processing.</param>
        /// <param name="processingInterval">The processing interval.</param>
        /// <param name="configuration">The configuaration to use.</param>
        /// <returns></returns>
        public IAggregateCalculator CreateCalculator(
            NodeId aggregateId,
            DateTime startTime,
            DateTime endTime,
            double processingInterval,
            AggregateConfiguration configuration) {
            if (NodeId.IsNull(aggregateId)) {
                return null;
            }

            AggregatorFactory factory = null;

            lock (_lock) {
                if (!_factories.TryGetValue(aggregateId, out factory)) {
                    return null;
                }
            }

            var calculator = factory();

            if (calculator == null) {
                return null;
            }

            calculator.StartTime = startTime;
            calculator.EndTime = endTime;
            calculator.ProcessingInterval = processingInterval;
            calculator.Configuration = configuration;
            calculator.SteppedVariable = configuration.UseSlopedExtrapolation;

            return calculator;
        }

        /// <summary>
        /// Registers an aggregate factory.
        /// </summary>
        /// <param name="aggregateId">The id of the aggregate function.</param>
        /// <param name="aggregateName">The id of the aggregate name.</param>
        /// <param name="factory">The factory used to create calculators.</param>
        public void RegisterFactory(NodeId aggregateId, string aggregateName, AggregatorFactory factory) {
            System.Diagnostics.Contracts.Contract.Assume(aggregateName != null);
            lock (_lock) {
                _factories[aggregateId] = factory;
            }

        }

        /// <summary>
        /// Unregisters an aggregate factory.
        /// </summary>
        /// <param name="aggregateId">The id of the aggregate function.</param>
        public void RegisterFactory(NodeId aggregateId) {
            lock (_lock) {
                _factories.Remove(aggregateId);
            }
        }

        private readonly object _lock = new object();
#pragma warning disable IDE0052 // Remove unread private members
        private readonly IServerInternal _server;
#pragma warning restore IDE0052 // Remove unread private members
        private AggregateConfiguration _defaultConfiguration;
        private readonly Dictionary<NodeId, AggregatorFactory> _factories;
        private double _minimumProcessingInterval;
    }
}
