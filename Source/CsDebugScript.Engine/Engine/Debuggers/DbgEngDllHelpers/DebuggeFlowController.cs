using DbgEngManaged;
using System;
using CsDebugScript.Engine.Utility;

namespace CsDebugScript.Engine.Debuggers.DbgEngDllHelpers
{
    /// <summary>
    /// Controler for Debugee actions during live debugging.
    /// </summary>
    class DebuggeeFlowController
    {
        /// <summary>
        /// Signal fired during interactive process debugging when debugee is released.
        /// </summary>
        private System.Threading.AutoResetEvent debugStatusGo;

        /// <summary>
        /// Signal fired during interactive process debugging when debugee is interrupted.
        /// </summary>
        private System.Threading.AutoResetEvent debugStatusBreak;

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
            debugStatusGo = new System.Threading.AutoResetEvent(false);
            debugStatusBreak = new System.Threading.AutoResetEvent(true);

            this.client = client;

            lock (eventCallbacksReady)
            {
                debuggerStateLoop =
                    new System.Threading.Thread(() => DebuggerStateLoop()) { IsBackground = true };
                debuggerStateLoop.SetApartmentState(System.Threading.ApartmentState.MTA);
                debuggerStateLoop.Start();

                // Wait for loop thread to become ready.
                //
                System.Threading.Monitor.Wait(eventCallbacksReady);
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
            debugCallbacks = new DebugCallbacks(client, debugStatusGo, debugStatusBreak);

            lock (eventCallbacksReady)
            {
                System.Threading.Monitor.Pulse(eventCallbacksReady);
            }

            // Default is to start in break mode, wait for release.
            //
            debugStatusGo.WaitOne();

            while (!hasClientExited)
            {
                loopControl.WaitForEvent(0, UInt32.MaxValue);
                uint executionStatus = loopControl.GetExecutionStatus();

                while (executionStatus == (uint)Defines.DebugStatusBreak)
                {
                    debugStatusBreak.Set();
                    debugStatusGo.WaitOne();

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
            IDebugControl7 control = (IDebugControl7)client;

            control.SetInterrupt(0);

            debugStatusBreak.WaitOne();
        }

        /// <summary>
        /// Continues the execution.
        /// </summary>
        public void ContinueExecution()
        {
            IDebugControl7 control = (IDebugControl7)client;

            debugStatusBreak.WaitOne();
            control.Execute(0, "g", 0);
        }

        /// <summary>
        /// Terminates execution of current process.
        /// </summary>
        public void TerminateExecution()
        {
            // Release any threads that are waiting.
            //
            debugStatusGo.Set();
            debugStatusBreak.Set();

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
