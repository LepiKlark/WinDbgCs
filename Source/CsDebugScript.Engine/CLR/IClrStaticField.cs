﻿namespace CsDebugScript.CLR
{
    /// <summary>
    /// CLR code static field interface. This is valid only if there is CLR loaded into debugging process.
    /// </summary>
    public interface IClrStaticField
    {
        /// <summary>
        /// Gets the address of the static field's value in memory.
        /// </summary>
        /// <param name="appDomain">The application domain.</param>
        ulong GetAddress(IClrAppDomain appDomain);
    }
}
