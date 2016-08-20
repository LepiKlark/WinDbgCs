using DbgEngManaged;
using System;
using CsDebugScript.Engine.Utility;
using System.Threading;

namespace CsDebugScript.Engine.Debuggers.DbgEngDllHelpers
{
    /// <summary>
    /// Controler for Debugee actions during live debugging.
    /// </summary>
    class DebuggeeFlowController
    {
        /// <summary>
        /// Enum defining whether this control flow is
        /// currently in break or go state.
        /// </summary>
        public enum DebuggerState
        {
            DebuggerStateGo,
            DebuggerStateBreak,
        }

        /// <summary>
        /// Current debugger state.
        /// </summary>
        private DebuggerState debuggerState;

        /// <summary>
        /// Object used for syncronization when changing debugger state.
        /// </summary>
        private object stateChangeSyncronization = new object();

        private object eventStateSignalled = new object();

        /// <summary>
        /// Loop responsible for catching debug events and signaling debugee state.
        /// </summary>
        private System.Threading.Thread debuggerStateLoop;

        /// <summary>
        /// Reference to debug client taken from DbgEng.
        /// </summary>
        private IDebugClient client;

        /// <summary>
        /// Reference to debug callbacks responsible to trigger actions
        /// on debug events.
        /// </summary>
        private DebugCallbacks debugCallbacks;

        /// <summary>
        /// Syncronization signaling that debug callbacks are installed.
        /// </summary>
        private static readonly object eventCallbacksReady = new Object();

        /// <summary>
        /// Initializes a new instance of the <see cref="DebuggeeFlowController"/> class.
        /// </summary>
        /// <param name="client">The client.</param>
        public DebuggeeFlowController(IDebugClient client)
        {
            // Default is that we start in break mode.
            // TODO: Needs to be changed when we allow non intrusive attach/start for example.
            //
            debuggerState = DebuggerState.DebuggerStateBreak;

            this.client = client;

            lock (eventCallbacksReady)
            {
                debuggerStateLoop =
                    new System.Threading.Thread(() => DebuggerStateLoop()) { IsBackground = true };
                debuggerStateLoop.SetApartmentState(System.Threading.ApartmentState.MTA);
                debuggerStateLoop.Start();

                // Wait for loop thread to become ready.
                //
                Monitor.Wait(eventCallbacksReady);
            }
        }

        /// <summary>
        /// Loop responsible to wait for debug events.
        /// Needs to be run in separate thread.
        /// </summary>
        private void DebuggerStateLoop()
        {
            bool hasClientExited = false;
            IDebugControl7 loopControl = (IDebugControl7)client;
            debugCallbacks = new DebugCallbacks(client, this);

            lock (eventCallbacksReady)
            {
                Monitor.Pulse(eventCallbacksReady);
            }

            // Default is to start in break mode, wait for release.
            //
            lock (eventStateSignalled)
            {
                Monitor.Wait(eventStateSignalled);
            }

            while (!hasClientExited)
            {
                loopControl.WaitForEvent(0, UInt32.MaxValue);
                uint executionStatus = loopControl.GetExecutionStatus();

                while (executionStatus == (uint)Defines.DebugStatusBreak)
                {
                    lock (eventStateSignalled)
                    {
                        Monitor.Pulse(eventStateSignalled);
                    }

                    executionStatus = loopControl.GetExecutionStatus();
                }

                hasClientExited = executionStatus == (uint)Defines.DebugStatusNoDebuggee;
            }
        }

        /// <summary>
        /// Breaks the debugger.
        /// </summary>
        public void BreakExecution()
        {
            lock(eventStateSignalled)
            {
                if (debuggerState == DebuggerState.DebuggerStateBreak)
                {
                    // Can't break two times.
                    throw new InvalidOperationException("Break can be executed only once.");
                }

                IDebugControl7 control = (IDebugControl7)client;
                control.SetInterrupt(0);
                Monitor.Wait(eventStateSignalled);
                debuggerState = DebuggerState.DebuggerStateBreak;
            }
        }

        /// <summary>
        /// Continues the execution.
        /// </summary>
        public void ContinueExecution()
        {
            lock(eventStateSignalled)
            {
                IDebugControl7 control = (IDebugControl7)client;
                control.Execute(0, "g", 0);
                debuggerState = DebuggerState.DebuggerStateGo;
                Monitor.Pulse(eventStateSignalled);
            }
        }

        /// <summary>
        /// Terminates execution of current process.
        /// </summary>
        public void TerminateExecution()
        {
            // Release any threads that are waiting.
            lock (eventStateSignalled)
            {
                Monitor.Pulse(eventStateSignalled);
            }

            // Wait for debug loop to exit.
            debuggerStateLoop.Join();
        }

        public void DebugeeStateChanged(DebuggerState debugeeState)
        {
            debuggerState = debugeeState;
            // TODO....
        }

        /// <summary>
        /// Adds new breakpoint to debug callbacks.
        /// </summary>
        /// <param name="breakpoint"></param>
        public void AddBreakpoint(DbgEngBreakpoint breakpoint)
        {
            debugCallbacks.AddBreakpoint(breakpoint);
        }
    }
}
