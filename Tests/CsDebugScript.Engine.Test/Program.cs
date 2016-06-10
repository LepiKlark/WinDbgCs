using CommandLine;
using CsDebugScript.Engine.Utility;
using DbgEngManaged;
using System;
using System.Runtime.InteropServices;

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

            Options options = null;

            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(o => options = o);

            if (options == null)
                return;

            Context.Initalize(DebugClient.OpenDumpFile(options.DumpPath, options.SymbolPath));

            Console.WriteLine("Threads: {0}", Thread.All.Length);
            Console.WriteLine("Current thread: {0}", Thread.Current.Id);
            var frames = Thread.Current.StackTrace.Frames;
            Console.WriteLine("Call stack:");
            foreach (var frame in frames)
                try
                {
                    Console.WriteLine("  {0,3:x} {1}+0x{2:x}   ({3}:{4})", frame.FrameNumber, frame.FunctionName, frame.FunctionDisplacement, frame.SourceFileName, frame.SourceFileLine);
                }
                catch (Exception)
                {
                    Console.WriteLine("  {0,3:x} {1}+0x{2:x}", frame.FrameNumber, frame.FunctionName, frame.FunctionDisplacement);
                }

            // In order to use console output and error in scripts, we must set it to debug client
            DebugOutput captureFlags = DebugOutput.Normal | DebugOutput.Error | DebugOutput.Warning | DebugOutput.Verbose
                | DebugOutput.Prompt | DebugOutput.PromptRegisters | DebugOutput.ExtensionWarning | DebugOutput.Debuggee
                | DebugOutput.DebuggeePrompt | DebugOutput.Symbols | DebugOutput.Status;
            var callbacks = DebuggerOutputToTextWriter.Create(Console.Out, captureFlags);

            using (OutputCallbacksSwitcher switcher = OutputCallbacksSwitcher.Create(callbacks))
            {
                Executor.Execute(@"..\..\..\samples\script.csx");
            }
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

        public static IDebugClient OpenDebugSession(string symbolPath)
        {

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
            _DEBUG_CREATE_PROCESS_OPTIONS options = new _DEBUG_CREATE_PROCESS_OPTIONS();

            options.CreateFlags = 0x00000002  /* Debug only this process */ ;

            ((IDebugClient7)client).CreateProcessAndAttachWide(0,
                  @"C:\Users\atomic\Documents\Visual Studio 2013\Projects\JustPlayC\x64\Debug\JustPlayC.exe", options.CreateFlags, 0, 0);

            uint execStatus = ((IDebugControl7)client).GetExecutionStatus();
            Console.WriteLine("Execution status - {0}", execStatus);

            ((IDebugControl7)client).WaitForEvent(0, UInt32.MaxValue);

            // Print state before starting debugger.
            //
            PrintDebugeeState(client);

            Console.WriteLine("go...");

            ((IDebugControl)client).Execute(0, "g", 0);

            System.Threading.Tasks.Task t = new System.Threading.Tasks.Task(() =>
            {
                var client2 = client.CreateClient();
                System.Threading.Thread.Sleep(5000);

                Console.WriteLine("thread2 interrupt");
                ((IDebugControl7)client).SetInterrupt(0);
            });

            t.Start();

            ((IDebugControl7)client).WaitForEvent(0, uint.MaxValue);

            PrintDebugeeState(client);

            return client;
        }

        [DllImport("dbgeng.dll", EntryPoint = "DebugCreateEx", SetLastError = false)]
        public static extern int DebugCreateEx(Guid iid, UInt32 flags, out IDebugClient client);

    }
}
