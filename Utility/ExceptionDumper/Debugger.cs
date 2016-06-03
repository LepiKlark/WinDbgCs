using DbgEngManaged;
using System;

namespace ExceptionDumper
{
    public class Dumper : IDebugEventCallbacks, IDisposable
    {
        private IDebugClient7 client;
        private IDebugControl7 control;
        private string dumpPath;
        private bool miniDump;
        private volatile bool dumpTaken = false;

        private Dumper(string applicationPath, string dumpPath, bool miniDump)
        {
            this.dumpPath = dumpPath;
            this.miniDump = miniDump;

            // Create debugging client
            IDebugClient clientBase = DebugClient.DebugCreate();

            // Cast to upper clients
            client = (IDebugClient7)clientBase;
            control = (IDebugControl7)client;
            client.SetEventCallbacks(this);
            client.CreateProcessAndAttach(0, applicationPath, 0x00000002);
        }

        public static void RunAndDumpOnException(string applicationPath, string dumpPath, bool miniDump)
        {
            using (Dumper dumper = new Dumper(applicationPath, dumpPath, miniDump))
            {
                while (!dumper.dumpTaken)
                {
                    dumper.control.WaitForEvent(0, uint.MaxValue);
                }

                dumper.client.EndSession((uint)Defines.DebugEndActiveTerminate);
            }
        }

        #region IDebugEventCallbacks
        public uint GetInterestMask()
        {
            return (uint)(Defines.DebugEventBreakpoint | Defines.DebugEventCreateProcess
                | Defines.DebugEventException | Defines.DebugEventExitProcess
                | Defines.DebugEventCreateThread | Defines.DebugEventExitThread
                | Defines.DebugEventLoadModule | Defines.DebugEventUnloadModule);
        }

        public int Breakpoint(IDebugBreakpoint Bp)
        {
            // Do nothing
            return 0;
        }

        public int Exception(ref _EXCEPTION_RECORD64 Exception, uint FirstChance)
        {
            if (FirstChance != 1)
            {
                // Save the dump
                client.WriteDumpFile(dumpPath, (uint)(miniDump ? Defines.DebugDumpSmall : Defines.DebugDumpDefault));
                dumpTaken = true;
            }

            return 0;
        }

        public int CreateThread(ulong Handle, ulong DataOffset, ulong StartOffset)
        {
            // Do nothing
            return 0;
        }

        public int ExitThread(uint ExitCode)
        {
            return 0;
            // Do nothing
        }

        public int CreateProcess(ulong ImageFileHandle, ulong Handle, ulong BaseOffset, uint ModuleSize, string ModuleName, string ImageName, uint CheckSum, uint TimeDateStamp, ulong InitialThreadHandle, ulong ThreadDataOffset, ulong StartOffset)
        {
            return 0;
            // Do nothing
        }

        public int ExitProcess(uint ExitCode)
        {
            return 0;
            // Do nothing
        }

        public int LoadModule(ulong ImageFileHandle, ulong BaseOffset, uint ModuleSize, string ModuleName, string ImageName, uint CheckSum, uint TimeDateStamp)
        {
            return 0;
            // Do nothing
        }

        public int UnloadModule(string ImageBaseName, ulong BaseOffset)
        {
            return 0;
            // Do nothing
        }

        public int SystemError(uint Error, uint Level)
        {
            return 0;
            // Do nothing
        }

        public int SessionStatus(uint Status)
        {
            return 0;
            // Do nothing
        }

        public int ChangeDebuggeeState(uint Flags, ulong Argument)
        {
            return 0;
            // Do nothing
        }

        public int ChangeEngineState(uint Flags, ulong Argument)
        {
            return 0;
            // Do nothing
        }

        public int ChangeSymbolState(uint Flags, ulong Argument)
        {
            // Do nothing
            return 0;
        }
        #endregion

        public void Dispose()
        {
            client.SetEventCallbacks(null);
        }
    }
}
