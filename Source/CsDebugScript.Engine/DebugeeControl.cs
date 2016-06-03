using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsDebugScript.Engine;

namespace CsDebugScript
{
        /// <summary>
    /// Flow controls for the process being debugged.
    /// </summary>
    public class DebugeeControl
    {
        /// <summary>
        /// Continues debugee.
        /// </summary>
        public static void Continue()
        {
            Context.Debugger.Continue();
        }

        /// <summary>
        /// Breaks debugee.
        /// </summary>
        public static void Break()
        {
            Context.Debugger.Break();
        }
    }
}

