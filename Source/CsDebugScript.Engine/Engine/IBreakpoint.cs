using System;

namespace CsDebugScript.Engine
{
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
        void ChangeAction(Action action);
    }
}
