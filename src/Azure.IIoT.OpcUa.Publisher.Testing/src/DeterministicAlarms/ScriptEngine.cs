/* ========================================================================
 * Copyright (c) 2005-2017 The OPC Foundation, Inc. All rights reserved.
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

namespace DeterministicAlarms
{
    using DeterministicAlarms.Configuration;
    using Opc.Ua.Test;
    using System;
    using System.Collections.Generic;
    using System.Timers;

    public class ScriptEngine
    {
        public delegate void NextScriptStepAvailable(Step step, long numberOfLoops);

        private LinkedList<Step> _steps;
        private LinkedListNode<Step> _currentStep;
        private ITimer _stepsTimer;
        private readonly Script _script;
        private long _numberOfLoops = 1;
        private DateTime _scriptStopTime;
        private readonly TimeService _timeService;

        public NextScriptStepAvailable OnNextScriptStepAvailable { get; set; }

        /// <summary>
        /// Initialize ScriptEngine
        /// </summary>
        /// <param name="script"></param>
        /// <param name="scriptCallback"></param>
        /// <param name="timeService"></param>
        public ScriptEngine(Script script, NextScriptStepAvailable scriptCallback, TimeService timeService)
        {
            OnNextScriptStepAvailable += scriptCallback ?? throw new ScriptException("Script Callback is not defined");

            _script = script;
            _timeService = timeService;

            CreateLinkedList(script.Steps);

            StartScript();
        }

        private void StartScript()
        {
            _stepsTimer = _timeService.NewTimer(OnStepTimedEvent, Convert.ToUInt32(_script.WaitUntilStartInSeconds * 1000));
            _scriptStopTime = _timeService.Now.AddSeconds(_script.RunningForSeconds + _script.WaitUntilStartInSeconds);
        }

        private void StopScript()
        {
            _stepsTimer.Close();
            _stepsTimer = null;
        }

        /// <summary>
        /// Create the Linked List that will be used internally to go through the steps
        /// </summary>
        /// <param name="steps"></param>
        private void CreateLinkedList(IList<Step> steps)
        {
            _steps = new LinkedList<Step>();
            foreach (var step in steps)
            {
                _steps.AddLast(step);
            }
        }

        /// <summary>
        /// Active a new step
        /// </summary>
        /// <param name="step"></param>
        private void ActivateCurrentStep(LinkedListNode<Step> step)
        {
            _currentStep = step;
            OnNextScriptStepAvailable?.Invoke(step?.Value, _numberOfLoops);
            if (_stepsTimer != null)
            {
                _stepsTimer.Interval = Math.Max(1, step.Value.SleepInSeconds * 1000);
            }
        }

        /// <summary>
        /// Get the next step
        /// </summary>
        /// <param name="step"></param>
        /// <returns></returns>
        private LinkedListNode<Step> GetNextValue(LinkedListNode<Step> step)
        {
            // Script should end because it has been executed as long as expected in the parameter
            // RunningForSeconds
            if (_scriptStopTime < _timeService.Now)
            {
                StopScript();
                return null;
            }

            // Is it the first step?
            if (step == null)
            {
                return _steps.First;
            }

            // Do we have a next step?
            if (step.Next != null)
            {
                return step.Next;
            }

            // We don't have a next step, now we should see if we should repeat
            // and start on first step again or terminate.
            if (_script.IsScriptInRepeatingLoop)
            {
                _numberOfLoops++;
                return _steps.First;
            }

            StopScript();
            return null;
        }

        /// <summary>
        /// Trigger when next step should be executed
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnStepTimedEvent(object source, ElapsedEventArgs e)
        {
            ActivateCurrentStep(GetNextValue(_currentStep));
        }
    }
}
