// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Plc {
    using System;
    using System.Threading;
    using Opc.Ua;

    public partial class PlcNodeManager {
        /// <summary>
        /// Flags for anomaly generation.
        /// </summary>
        public static bool GenerateSpikes { get; set; } = true;
        public static bool GenerateDips { get; set; } = true;
        public static bool GeneratePosTrend { get; set; } = true;
        public static bool GenerateNegTrend { get; set; } = true;
        public static bool GenerateData { get; set; } = true;

        /// <summary>
        /// Simulation data.
        /// </summary>
        public static int SimulationCycleCount { get; set; } =
            kSIMULATION_CYCLECOUNT_DEFAULT;
        public static int SimulationCycleLength { get; set; } =
            kSIMULATION_CYCLELENGTH_DEFAULT;
        public static double SimulationMaxAmplitude { get; set; } =
            kSIMULATION_MAXAMPLITUDE_DEFAULT;

        /// <summary>
        /// Ctor for simulation server.
        /// </summary>
        private void Initialize() {
            _random = new Random();
            _cyclesInPhase = SimulationCycleCount;
            _spikeCycleInPhase = SimulationCycleCount;
            _spikeAnomalyCycle = _random.Next(SimulationCycleCount);
            Utils.TraceDebug($"first spike anomaly cycle: {_spikeAnomalyCycle}");
            _dipCycleInPhase = SimulationCycleCount;
            _dipAnomalyCycle = _random.Next(SimulationCycleCount);
            Utils.TraceDebug($"first dip anomaly cycle: {_dipAnomalyCycle}");
            _posTrendAnomalyPhase = _random.Next(10);
            _posTrendCycleInPhase = SimulationCycleCount;
            Utils.TraceDebug($"first pos trend anomaly phase: {_posTrendAnomalyPhase}");
            _negTrendAnomalyPhase = _random.Next(10);
            _negTrendCycleInPhase = SimulationCycleCount;
            Utils.TraceDebug($"first neg trend anomaly phase: {_negTrendAnomalyPhase}");
            _stepUp = 0;
            _stepUpStarted = true;
        }

        /// <summary>
        /// Start the simulation.
        /// </summary>
        public void Start() {
            _spikeGenerator = new Timer(SpikeGenerator, null, 0, SimulationCycleLength);
            _dipGenerator = new Timer(DipGenerator, null, 0, SimulationCycleLength);
            _posTrendGenerator = new Timer(PosTrendGenerator, null, 0, SimulationCycleLength);
            _negTrendGenerator = new Timer(NegTrendGenerator, null, 0, SimulationCycleLength);

            if (GenerateData) {
                _dataGenerator = new Timer(ValueGenerator, null, 0, SimulationCycleLength);
            }
        }

        /// <summary>
        /// Stop the simulation.
        /// </summary>
        public void Stop() {
            _spikeGenerator?.Change(Timeout.Infinite, Timeout.Infinite);
            _dipGenerator?.Change(Timeout.Infinite, Timeout.Infinite);
            _posTrendGenerator?.Change(Timeout.Infinite, Timeout.Infinite);
            _negTrendGenerator?.Change(Timeout.Infinite, Timeout.Infinite);
            _dataGenerator?.Change(Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Generates a sine wave with spikes at a random cycle in the phase.
        /// Called each SimulationCycleLength msec.
        /// </summary>
        private void SpikeGenerator(object state) {
            // calculate next value
            double nextValue = 0;
            if (GenerateSpikes && _spikeCycleInPhase == _spikeAnomalyCycle) {
                // TODO: calculate
                nextValue = SimulationMaxAmplitude * 10;
                Utils.Trace($"generate spike anomaly");
            }
            else {
                nextValue = SimulationMaxAmplitude * Math.Sin(2 * Math.PI / SimulationCycleCount * _spikeCycleInPhase);
            }
            Utils.Trace($"spike cycle: {_spikeCycleInPhase} data: {nextValue}");
            SpikeData = nextValue;

            // end of cycle: reset cycle count and calc next anomaly cycle
            if (--_spikeCycleInPhase == 0) {
                _spikeCycleInPhase = SimulationCycleCount;
                _spikeAnomalyCycle = _random.Next(SimulationCycleCount);
                Utils.Trace($"next spike anomaly cycle: {_spikeAnomalyCycle}");
            }
        }

        /// <summary>
        /// Generates a sine wave with dips at a random cycle in the phase.
        /// Called each SimulationCycleLength msec.
        /// </summary>
        private void DipGenerator(object state) {
            // calculate next value
            double nextValue = 0;
            if (GenerateDips && _dipCycleInPhase == _dipAnomalyCycle) {
                nextValue = SimulationMaxAmplitude * -10;
                Utils.Trace($"generate dip anomaly");
            }
            else {
                nextValue = SimulationMaxAmplitude * Math.Sin(2 * Math.PI / SimulationCycleCount * _dipCycleInPhase);
            }
            Utils.Trace($"spike cycle: {_dipCycleInPhase} data: {nextValue}");
            DipData = nextValue;

            // end of cycle: reset cycle count and calc next anomaly cycle
            if (--_dipCycleInPhase == 0) {
                _dipCycleInPhase = SimulationCycleCount;
                _dipAnomalyCycle = _random.Next(SimulationCycleCount);
                Utils.Trace($"next dip anomaly cycle: {_dipAnomalyCycle}");
            }
        }

        /// <summary>
        /// Generates a sine wave with spikes at a configurable cycle in the phase.
        /// Called each SimulationCycleLength msec.
        /// </summary>
        private void PosTrendGenerator(object state) {
            // calculate next value
            var nextValue = kTREND_BASEVALUE;
            if (GeneratePosTrend && _posTrendPhase >= _posTrendAnomalyPhase) {
                nextValue = kTREND_BASEVALUE + ((_posTrendPhase - _posTrendAnomalyPhase) / 10);
                Utils.Trace($"generate postrend anomaly");
            }
            PosTrendData = nextValue;

            // end of cycle: reset cycle count and calc next anomaly cycle
            if (--_posTrendCycleInPhase == 0) {
                _posTrendCycleInPhase = SimulationCycleCount;
                _posTrendPhase++;
                Utils.Trace($"pos trend phase: {_posTrendPhase}, data: {nextValue}");
            }
        }

        /// <summary>
        /// Generates a sine wave with spikes at a configurable cycle in the phase.
        /// Called each SimulationCycleLength msec.
        /// </summary>
        private void NegTrendGenerator(object state) {
            // calculate next value
            var nextValue = kTREND_BASEVALUE;
            if (GenerateNegTrend && _negTrendPhase >= _negTrendAnomalyPhase) {
                nextValue = kTREND_BASEVALUE - ((_negTrendPhase - _negTrendAnomalyPhase) / 10);
                Utils.Trace($"generate negtrend anomaly");
            }
            NegTrendData = nextValue;

            // end of cycle: reset cycle count and calc next anomaly cycle
            if (--_negTrendCycleInPhase == 0) {
                _negTrendCycleInPhase = SimulationCycleCount;
                _negTrendPhase++;
                Utils.Trace($"neg trend phase: {_negTrendPhase}, data: {nextValue}");
            }
        }

        /// <summary>
        /// Updates simulation values. Called each SimulationCycleLength msec.
        /// Using SimulationCycleCount cycles per simulation phase.
        /// </summary>
        private void ValueGenerator(object state) {
            // calculate next boolean value
            var nextAlternatingBoolean = (_cyclesInPhase % (SimulationCycleCount / 2)) == 0 ? !_currentAlternatingBoolean : _currentAlternatingBoolean;
            if (_currentAlternatingBoolean != nextAlternatingBoolean) {
                Utils.Trace($"data change to: {nextAlternatingBoolean}");
                _currentAlternatingBoolean = nextAlternatingBoolean;
            }
            AlternatingBoolean = nextAlternatingBoolean;

            // calculate next Int values
            RandomSignedInt32 = _random.Next(int.MinValue, int.MaxValue);
            RandomUnsignedInt32 = (uint)_random.Next();

            // increase step up value
            if (_stepUpStarted && (_cyclesInPhase % (SimulationCycleCount / 50) == 0)) {
                StepUp = _stepUp++;
            }

            // end of cycle: reset cycle count
            if (--_cyclesInPhase == 0) {
                _cyclesInPhase = SimulationCycleCount;
            }
        }

        /// <summary>
        /// Method implementation to reset the trend data.
        /// </summary>
        public void ResetTrendData() {
            _posTrendAnomalyPhase = _random.Next(10);
            _posTrendCycleInPhase = SimulationCycleCount;
            _posTrendPhase = 0;
            _negTrendAnomalyPhase = _random.Next(10);
            _negTrendCycleInPhase = SimulationCycleCount;
            _negTrendPhase = 0;
        }

        /// <summary>
        /// Method implementation to reset the StepUp data.
        /// </summary>
        public void ResetStepUpData() {
            StepUp = _stepUp = 0;
        }

        /// <summary>
        /// Method implementation to start the StepUp.
        /// </summary>
        public void StartStepUp() {
            _stepUpStarted = true;
        }

        /// <summary>
        /// Method implementation to stop the StepUp.
        /// </summary>
        public void StopStepUp() {
            _stepUpStarted = false;
        }

        private const int kSIMULATION_CYCLECOUNT_DEFAULT = 50;           // in cycles
        private const int kSIMULATION_CYCLELENGTH_DEFAULT = 100;        // in msec
        private const double kSIMULATION_MAXAMPLITUDE_DEFAULT = 100.0;
        private const double kTREND_BASEVALUE = 100.0;

        private Random _random;
        private int _cyclesInPhase;
        private Timer _dataGenerator;
        private bool _currentAlternatingBoolean;
        private Timer _spikeGenerator;
        private int _spikeAnomalyCycle;
        private int _spikeCycleInPhase;
        private Timer _dipGenerator;
        private int _dipAnomalyCycle;
        private int _dipCycleInPhase;
        private Timer _posTrendGenerator;
        private int _posTrendAnomalyPhase;
        private int _posTrendCycleInPhase;
        private int _posTrendPhase;
        private Timer _negTrendGenerator;
        private int _negTrendAnomalyPhase;
        private int _negTrendCycleInPhase;
        private int _negTrendPhase;
        private uint _stepUp;
        private bool _stepUpStarted;
    }
}