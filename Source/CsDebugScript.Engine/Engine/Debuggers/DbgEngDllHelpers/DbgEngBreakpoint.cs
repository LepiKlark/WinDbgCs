using System;
using DbgEngManaged;
using System.Threading;

namespace CsDebugScript.Engine.Debuggers.DbgEngDllHelpers
{
    /// <summary>
    /// Represents managed breakpoint.
    /// </summary>
    public class DbgEngBreakpoint : IBreakpoint
    {
        /// <summary>
        /// Native breakpoint object.
        /// </summary>
        private IDebugBreakpoint2 breakpoint;

        /// <summary>
        /// Breakpoint action executed when breakpoint is hit.
        /// </summary>
        private Func<BreakpointEventStatus> breakpointAction;

        /// <summary>
        /// Invalidate cache when breakpoint is hit.
        /// </summary>
        private Action invalidateCache;

        /// <summary>
        /// Debug control interface to DbgEng.
        /// </summary>
        private IDebugControl7 debugControlInterface;

        /// <summary>
        /// Constructor for creating new breakpoint.
        /// </summary>
        /// <param name="breakpointExpression"></param>
        /// <param name="breakpointAction"></param>
        /// <param name="invalidateCache"></param>
        /// <param name="debugControlInterface"></param>
        public DbgEngBreakpoint(string breakpointExpression, Func<BreakpointEventStatus> breakpointAction, Action invalidateCache, IDebugControl7 debugControlInterface)
        {
            this.debugControlInterface = debugControlInterface;

            IDebugBreakpoint2 nativeBreakpoint = null;
            unchecked
            {
                nativeBreakpoint = debugControlInterface.AddBreakpoint2((uint)Defines.DebugBreakpointCode, (uint)Defines.DebugAnyId);
            }

            nativeBreakpoint.SetOffsetExpressionWide(breakpointExpression);
            nativeBreakpoint.AddFlags((uint)Defines.DebugBreakpointEnabled);
            this.breakpoint = nativeBreakpoint;
            this.breakpointAction = breakpointAction;
            this.invalidateCache = invalidateCache;
        }

        /// <summary>
        /// Executes action assosiated to this breakpoint.
        /// </summary>
        public BreakpointEventStatus ExecutionAction()
        {
            invalidateCache();
            return breakpointAction();
        }

        /// <summary>
        /// Returns breakpoints id.
        /// </summary>
        /// <returns></returns>
        public uint GetId()
        {
            return breakpoint.GetId();
        }

        /// <summary>
        /// Changes action executed when this breakpoint is hit.
        /// </summary>
        /// <param name="action"></param>
        public void ChangeAction(Func<BreakpointEventStatus> action)
        {
            breakpointAction = action;
        }

        /// <summary>
        /// Disable this breakpoint.
        /// </summary>
        public void Disable()
        {
            breakpoint.RemoveFlags((uint)Defines.DebugBreakpointEnabled);
        }

        /// <summary>
        /// Enable this breakpoint.
        /// Breakpoint is enabled by default on creation.
        /// </summary>
        public void Enable()
        {
            breakpoint.AddFlags((uint)Defines.DebugBreakpointEnabled);
        }

        public void Remove()
        {
            throw new NotImplementedException();
        }

    }
}
