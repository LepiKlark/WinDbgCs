﻿using System;
using System.Runtime.InteropServices;

namespace DbgEngManaged
{
    /// <summary>
    /// Static class with functions that provide creation of interfaces pointers to debug client objects.
    /// </summary>
    public static class DebugClient
    {
        /// <summary>
        /// Opens the specified dump file.
        /// </summary>
        /// <param name="dumpFile">The dump file.</param>
        /// <param name="symbolPath">The symbol path.</param>
        public static IDebugClient OpenDumpFile(string dumpFile, string symbolPath)
        {
            IDebugClient client = DebugCreate();

            ((IDebugSymbols5)client).SetSymbolPathWide(symbolPath);
            client.OpenDumpFile(dumpFile);
            ((IDebugControl7)client).WaitForEvent(0, uint.MaxValue);
            ((IDebugSymbols5)client).SetSymbolPathWide(symbolPath);
            ((IDebugControl7)client).Execute(0, ".reload -f", 0);
            return client;
        }

        /// <summary>
        /// Starts the process from specified path under debugger.
        /// </summary>
        /// <param name="processPath">Path to executable.</param>
        /// <param name="symbolPath">The symbol path.</param>
        /// <returns></returns>
        public static IDebugClient OpenExecutable(string processPath, string symbolPath)
        {
            IDebugClient client = DebugCreateEx(0x60);

            ((IDebugSymbols5)client).SetSymbolPathWide(symbolPath);
            ((IDebugClient7)client).CreateProcessAndAttach(0, processPath, 2);

            ((IDebugControl7)client).WaitForEvent(0, uint.MaxValue);
            ((IDebugSymbols5)client).SetSymbolPathWide(symbolPath);
            ((IDebugControl7)client).Execute(0, ".reload -f", 0);

            return client;
        }

        /// <summary>
        /// The DebugCreate function creates a new client object and returns an interface pointer to it.
        /// </summary>
        public static IDebugClient DebugCreate()
        {
            IDebugClient client;
            int hresult = DebugCreate(Marshal.GenerateGuidForType(typeof(IDebugClient)), out client);

            if (hresult < 0)
            {
                Marshal.ThrowExceptionForHR(hresult);
            }

            return client;
        }

        /// <summary>
        /// The DebugCreateEx function creates a new client object and returns an interface pointer to it.
        /// </summary>
        /// <param name="dbgEngOptions">Supplies debugger option flags.</param>
        public static IDebugClient DebugCreateEx(uint dbgEngOptions)
        {
            IDebugClient client;
            int hresult = DebugCreateEx(Marshal.GenerateGuidForType(typeof(IDebugClient)), dbgEngOptions, out client);

            if (hresult < 0)
            {
                Marshal.ThrowExceptionForHR(hresult);
            }

            return client;
        }

        #region Native functions
        /// <summary>
        /// The DebugCreate function creates a new client object and returns an interface pointer to it.
        /// </summary>
        /// <param name="InterfaceId">Specifies the interface identifier (IID) of the desired debugger engine client interface. This is the type of the interface that will be returned in Interface.</param>
        /// <param name="client">Receives an interface pointer for the new client. The type of this interface is specified by InterfaceId.</param>
        [DllImport("dbgeng.dll", EntryPoint = "DebugCreate", SetLastError = false, CallingConvention = CallingConvention.StdCall)]
        private static extern int DebugCreate([In][MarshalAs(UnmanagedType.LPStruct)]Guid InterfaceId, out IDebugClient Interface);

        /// <summary>
        /// The DebugCreateEx function creates a new client object and returns an interface pointer to it.
        /// </summary>
        /// <param name="InterfaceId">Specifies the interface identifier (IID) of the desired debugger engine client interface. This is the type of the interface that will be returned in Interface.</param>
        /// <param name="DbgEngOptions">Supplies debugger option flags.</param>
        /// <param name="Interface">Receives an interface pointer for the new client. The type of this interface is specified by InterfaceId.</param>
        [DllImport("dbgeng.dll", EntryPoint = "DebugCreateEx", SetLastError = false, CallingConvention = CallingConvention.StdCall)]
        private static extern int DebugCreateEx([In][MarshalAs(UnmanagedType.LPStruct)]Guid InterfaceId, uint DbgEngOptions, out IDebugClient Interface);
        #endregion
    }
}
