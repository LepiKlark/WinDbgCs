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
        /// Signal that debugger is released.
        /// </summary>
        private AutoResetEvent controllerSyncReleaseDebuggerEvent;

        /// <summary>
        /// Signal that break signall has been processed.
        /// </summary>
        private AutoResetEvent controllerSyncBreakSignalledEvent;

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
            this.client = client;

            lock (eventCallbacksReady)
            {
                debuggerStateLoop =
                    new System.Threading.Thread(() => DebuggerStateLoop()) { IsBackground = true };
                debuggerStateLoop.SetApartmentState(System.Threading.ApartmentState.MTA);
                debuggerStateLoop.Start();

                // Wait for loop thread to become ready.
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
            controllerSyncReleaseDebuggerEvent = new AutoResetEvent(false);
            controllerSyncBreakSignalledEvent = new AutoResetEvent(false);

            lock (eventCallbacksReady)
            {
                Monitor.Pulse(eventCallbacksReady);
            }

            // Default is to start in break mode, wait for release.
            // TODO: Needs to be changed when we allow non intrusive attach/start for example.
            controllerSyncReleaseDebuggerEvent.WaitOne();

            while (!hasClientExited)
            {
                loopControl.WaitForEvent(0, UInt32.MaxValue);
                uint executionStatus = loopControl.GetExecutionStatus();

                if (executionStatus == (uint)Defines.DebugStatusBreak)
                {
                    // Loop until debugger state break has been signaled.
                    controllerSyncBreakSignalledEvent.Set();

                    // Wait for debugger to continue.
                    controllerSyncReleaseDebuggerEvent.WaitOne();

                    executionStatus = loopControl.GetExecutionStatus();

                    if (executionStatus == (uint)Defines.DebugStatusBreak)
                    {
                        throw new InvalidOperationException("Can't be in break state after go event is signaled");
                    }
                }

                hasClientExited = executionStatus == (uint)Defines.DebugStatusNoDebuggee;
            }
        }

        /// <summary>
        /// Breaks the debugger.
        /// </summary>
        public void BreakExecution()
        {
            IDebugControl7 control = (IDebugControl7)client;
            control.SetInterrupt(0);

            // Wait for break event to be processed.
            controllerSyncBreakSignalledEvent.WaitOne();
        }

        /// <summary>
        /// Continues the execution.
        /// </summary>
        public void ContinueExecution()
        {
            IDebugControl7 control = (IDebugControl7)client;
            control.Execute(0, "g", 0);

            controllerSyncReleaseDebuggerEvent.Set();
        }

        /// <summary>
        /// Terminates execution of current process.
        /// </summary>
        public void TerminateExecution()
        {
            controllerSyncBreakSignalledEvent.Set();
            controllerSyncReleaseDebuggerEvent.Set();

            // Wait for debug loop to exit.
            debuggerStateLoop.Join();
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
