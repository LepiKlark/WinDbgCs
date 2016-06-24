using CommandLine;
using CsDebugScript.Engine.Utility;
using DbgEngManaged;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace CsDebugScript.Engine.Test
{
    class Options
    {
        [Option('d', "dump", Default = "NativeDumpTest.x64.dmp", HelpText = "Path to memory dump file that will be debugged")]
        public string DumpPath { get; set; }

        [Option('p', "symbol-path", Default = @"srv*;.\", HelpText = "Symbol path to be set in debugger")]
        public string SymbolPath { get; set; }
    }

    class Program
    {
        unsafe static void Main(string[] args)
        {


            var client = OpenDebugSession(@"C:\Users\atomic\Documents\Visual Studio 2013\Projects\JustPlayC\x64\Debug\");
            var control = (IDebugControl7)client;

            return;
        }

        public static void PrintDebugeeState(IDebugClient client)
        {
            var fetchClient = client.CreateClient();

            Console.WriteLine("-----------------------------");

            Context.Initalize(fetchClient);

            foreach (var module in Module.All)
            {
                Console.WriteLine("Image name {0}, symbol {1}.", module.ImageName, module.SymbolFileName);
            }

            // Need to disable caching here.
            //

            foreach (var thread in Thread.All)
            {
                foreach (var frame in thread.StackTrace.Frames)
                {
                    try
                    {
                        Console.WriteLine(frame.FunctionName);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("failed to extract function name");
                    }
                }
            }

            Console.WriteLine("-----------------------------");
        }

        // Debugger state machine.
        //
        class DebugCallbacks : IDebugEventCallbacks
        {
            public IDebugClient Client { get; set; }

            public int Breakpoint(IDebugBreakpoint Bp)
            {
                throw new NotImplementedException();
            }

            public int ChangeDebuggeeState(uint Flags, ulong Argument)
            {
                Console.WriteLine("Changing debuggee state.");

                uint executionStatus = ((IDebugControl7)Client).GetExecutionStatus();

                if (executionStatus == (uint)Defines.DebugStatusBreak)
                {
                    Console.WriteLine("break state");

                    return (int)executionStatus;
                }
                else if (executionStatus == (uint)Defines.DebugStatusGo)
                {
                    Console.WriteLine("go state");
                    s_ClientGo.Set();

                    return (int)executionStatus;
                }

                Console.WriteLine($"Execution status {executionStatus}");

                return (int)Defines.DebugStatusNoChange;
            }

            public int ChangeEngineState(uint Flags, ulong Argument)
            {
                throw new NotImplementedException();
            }

            public int ChangeSymbolState(uint Flags, ulong Argument)
            {
                throw new NotImplementedException();
            }

            public int CreateProcess(ulong ImageFileHandle, ulong Handle, ulong BaseOffset, uint ModuleSize, string ModuleName, string ImageName, uint CheckSum, uint TimeDateStamp, ulong InitialThreadHandle, ulong ThreadDataOffset, ulong StartOffset)
            {
                Console.WriteLine("Calling Create Process {0}", ImageName);
                return (int)Defines.DebugStatusNoChange;
            }

            public int CreateThread(ulong Handle, ulong DataOffset, ulong StartOffset)
            {
                Console.WriteLine("Creating a thread");
                return (int)Defines.DebugStatusNoChange;
            }

            public int Exception(ref _EXCEPTION_RECORD64 Exception, uint FirstChance)
            {
                Console.WriteLine("Exception happened {0} -  First Change {1}", Exception, FirstChance);
                return (int)Defines.DebugStatusNoChange;
            }

            public int ExitProcess(uint ExitCode)
            {
                Console.WriteLine("Exit process with error code {0}", ExitCode);
                return (int)Defines.DebugStatusNoChange;
            }

            public int ExitThread(uint ExitCode)
            {
                Console.WriteLine("Thread exiting with exit code {0}", ExitCode);

                return (int)Defines.DebugStatusNoChange;
            }

            public uint GetInterestMask()
            {
                DebugOutput captureFlags = DebugOutput.Normal | DebugOutput.Error | DebugOutput.Warning | DebugOutput.Verbose
                | DebugOutput.Prompt | DebugOutput.PromptRegisters | DebugOutput.ExtensionWarning | DebugOutput.Debuggee
                | DebugOutput.DebuggeePrompt | DebugOutput.Symbols | DebugOutput.Status;

                return (uint)captureFlags;
            }

            public int LoadModule(ulong ImageFileHandle, ulong BaseOffset, uint ModuleSize, string ModuleName, string ImageName, uint CheckSum, uint TimeDateStamp)
            {
                Console.WriteLine("Loaded module {0}", ModuleName);
                return (int)Defines.DebugStatusNoChange;
            }

            public int SessionStatus(uint Status)
            {
                Console.WriteLine("Session status is {0}", Status);
                return (int)Defines.DebugStatusNoChange;
            }

            public int SystemError(uint Error, uint Level)
            {
                throw new NotImplementedException();
            }

            public int UnloadModule(string ImageBaseName, ulong BaseOffset)
            {
                Console.WriteLine("Module unload {0}", ImageBaseName);
                return (int)Defines.DebugStatusNoChange;
            }
        }

        public static void Go(IDebugClient client)
        {
            // Wait for debugger to break.
            //

            // I could assert here that client is in break state.
            //


            s_ClientBreak.WaitOne();

            var c = client.CreateClient();
            Console.WriteLine("doing go");
            ((IDebugControl7)c).Execute(0, "g", 0);
        }

        public static void Break(IDebugClient client)
        {
            Console.WriteLine("doing break");

            s_ClientGo.Reset();

            var c = client.CreateClient();
            ((IDebugControl7)c).SetInterrupt(0);

            // Wait for debugger to break.
            //
            s_ClientBreak.WaitOne();
        }

        public static System.Threading.AutoResetEvent s_ClientBreak = new System.Threading.AutoResetEvent(false);
        public static System.Threading.AutoResetEvent s_ClientGo = new System.Threading.AutoResetEvent(false);
        public static Object lock_obj = new object();

        public static IDebugClient OpenDebugSession(string symbolPath)
        {
            // Ok see where does this get set.
            // Defines.DebugStatusBreak;

            IDebugClient client;
            int hresult = DebugCreateEx(Marshal.GenerateGuidForType(typeof(IDebugClient)), 0x100000, out client);

            if (hresult > 0)
            {
                Marshal.ThrowExceptionForHR(hresult);
            }

            //         Opt |= (DEBUG_ENGOPT_INITIAL_BREAK |
            //          DEBUG_ENGOPT_FINAL_BREAK);
            ((IDebugControl7)client).SetEngineOptions(0xa0);

            // Flags are create flags.
            //
            //DEBUG_ATTACH_INVASIVE_NO_INITIAL_BREAK

            DebugCallbacks callbacks = new DebugCallbacks();
            callbacks.Client = client;


            // t.Start();


            ((IDebugClient7)client).CreateProcessAndAttachWide(0,
                  @"C:\Users\atomic\Documents\Visual Studio 2013\Projects\JustPlayC\x64\Debug\JustPlayC.exe", 0x00000002 , 0, 0);


            // Wait for debugger to get attached...
            ((IDebugControl7)client).WaitForEvent(0, UInt32.MaxValue);

            // Set up main task which will handle wait for event.
            // Think about how to setup interactions with colling threads.
            var t = new System.Threading.Thread(() =>
            {
                bool hasClientExited = false;

                var loopClient = client.CreateClient();


                //loopClient.SetEventCallbacks(callbacks);

                while (!hasClientExited)
                {
                    uint executionStatus = 0;

                    // Todo we may want to have more finely grained control on errors returned by wait for event.
                    //

                    Console.WriteLine("------Loop thread is waiting on event---------");
                    ((IDebugControl7)loopClient).WaitForEvent(0, UInt32.MaxValue);

                    executionStatus = ((IDebugControl7)loopClient).GetExecutionStatus();

                    while (executionStatus == (uint)Defines.DebugStatusBreak)
                    {
                        s_ClientBreak.Set();

                        // Signal client can proceed and wait.
                        //

                        // Wait for unblock command.
                        //
                        s_ClientGo.WaitOne();

                        executionStatus = ((IDebugControl7)loopClient).GetExecutionStatus();
                    }

                    hasClientExited = ((IDebugControl7)loopClient).GetExecutionStatus() == (uint)Defines.DebugStatusNoDebuggee;

                    if (hasClientExited)
                    {
                        Console.WriteLine("Debugger exiting");
                    }

                }
            })
            { IsBackground = true };



            // Print state before starting debugger.
            //

            PrintDebugeeState(client);


            t.Start();

            System.Threading.Thread.Sleep(1000 * 1000);

            // Actual test
            Go(client);

            // Wait for some time.
            //
            System.Threading.Thread.Sleep(2000);

            Break(client);

            PrintDebugeeState(client);

            // Actual test

            return client;
        }

        [DllImport("dbgeng.dll", EntryPoint = "DebugCreateEx", SetLastError = false)]
        public static extern int DebugCreateEx(Guid iid, UInt32 flags, out IDebugClient client);

    }
}
