using System;
using DbgEngManaged;
using System.Threading;

namespace CsDebugScript.Engine.Utility
{
    /// <summary>
    /// Represents managed breakpoint.
    /// </summary>
    public class ManagedBreakpoint
    {
        // TODO: Figure out how are you going to represent this for Visaul studio/other debuggers.

        /// <summary>
        /// Native breakpoint object.
        /// </summary>
        private IDebugBreakpoint2 breakpoint;

        /// <summary>
        /// Breakpoint action executed when breakpoint is hit.
        /// </summary>
        private Action breakpointAction;

        /// <summary>
        /// Field specifying whether this breakpoint holds the execution,
        /// or just executes given action.
        /// </summary>
        private bool canBeReleased = false;

        /// <summary>
        /// Reset event used when user doesn't specify the action
        /// but expects that hitting breakpoint breaks the execution.
        /// </summary>
        private ManualResetEvent releaseExecution = null;

        private Process process;

        /// <summary>
        /// Constructor for creating new breakpoint.
        /// </summary>
        /// <param name="breakpoint"></param>
        /// <param name="breakpointAction"></param>
        public ManagedBreakpoint(IDebugBreakpoint2 breakpoint, Action breakpointAction, Process process)
        {
            this.breakpoint = breakpoint;
            this.breakpointAction = breakpointAction;
            this.process = process;
            canBeReleased = false;
            releaseExecution = null;
        }

        /// <summary>
        /// Constructor for creating new breakpoint which will freeze the execution when hit.
        /// </summary>
        /// <param name="breakpoint"></param>
        public ManagedBreakpoint(IDebugBreakpoint2 breakpoint, Process process)
        {
            this.breakpoint = breakpoint;
            this.canBeReleased = true;
            releaseExecution = new ManualResetEvent(false);
            this.breakpointAction = () => { releaseExecution.WaitOne(); };
            this.process = process;

            // TODO: Actually break user execution here.
            //       and wait until user issues go.
        }

        /// <summary>
        /// Executes action assosiated to this breakpoint.
        /// </summary>
        public void ExecutionAction()
        {
            // First clear all the caches.
            //

            process.InvalidateProcessCache();
            breakpointAction();
        }

        /// <summary>
        /// Returns breakpoints id.
        /// </summary>
        /// <returns></returns>
        public uint GetId()
        {
            return breakpoint.GetId();
        }
    }
}
