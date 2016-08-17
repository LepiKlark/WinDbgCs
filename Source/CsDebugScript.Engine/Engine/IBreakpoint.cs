using System;

namespace CsDebugScript.Engine
{
    /// <summary>
    /// Indicates state of the debugger after this breakpoint is hit.
    /// </summary>
    public enum BreakpointEventStatus
    {
        /// <summary>
        /// Continues execution after executing action assosiated to this breakpoint.
        /// </summary>
        ReleaseDebugger,
        /// <summary>
        /// Breaks execution until user again issues continue debugger action.
        /// </summary>
        BreakDebugger,
    }

    /// <summary>
    /// Methods to ease the life when dealing with breakpoints.
    /// </summary>
    public static class BreakpointDefaults
    {
        /// <summary>
        /// Action for breaking when breakpoint is hit.
        /// </summary>
        public static Func<BreakpointEventStatus> BreakDebuggerAction
        {
            get { return () => BreakpointEventStatus.BreakDebugger; }
        }
    }


    /// <summary>
    /// Interface for breakpoints.
    /// </summary>
    public interface IBreakpoint
    {
        /// <summary>
        /// Removes this breakpoint.
        /// </summary>
        void Remove();

        /// <summary>
        /// Disables this breakpoint.
        /// </summary>
        void Disable();

        /// <summary>
        /// Enables this breakpoint.
        /// </summary>
        void Enable();

        /// <summary>
        /// Changes action assosiated to this breakpoint.
        /// </summary>
        void ChangeAction(Func<BreakpointEventStatus> action);
    }
}
