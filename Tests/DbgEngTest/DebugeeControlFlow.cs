using CsDebugScript;
using DbgEngManaged;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace DbgEngTest
{
    /// <summary>
    /// Tests for debug control - release, break, breakpoints etc.
    /// </summary>
    [TestClass]
    public class DebugeeControlFlow : TestBase
    {
        private const string ProcessPath = @"C:\Users\atomic\Documents\Visual Studio 2013\Projects\JustPlayC\x64\Debug\JustPlayC.exe";

        private const string DefaultSymbolPath = @"srv*;.\;C:\Users\atomic\Documents\Visual Studio 2013\Projects\JustPlayC\x64\Debug\";

        [ClassInitialize]
        public static void TestSetup(TestContext context)
        {
            // InitializeAttachProcess(ProcessPath, DefaultSymbolPath);
        }

        [TestMethod]
        public void SimpleControlTest()
        {
            var client = DebugClient.OpenExecutable(ProcessPath, DefaultSymbolPath);

            var control = (IDebugControl7)client;
            control.Execute(0, "g", 0);

            System.Threading.Thread.Sleep(1000);


            // Ok what if I put this here.
            //
            for (int i = 0; i < 5; i++)
            {
                DebugeeControl.Continue();


                Console.WriteLine("Debugee is running");

                System.Threading.Thread.Sleep(1000);

                DebugeeControl.Break();

                Console.WriteLine("Ok it now stopped.");

                Console.WriteLine("Threads:");

                Console.WriteLine("----------------------------------");

                // foreach (Thread thread in Thread.All)
                // {
                //     Console.WriteLine("Thread {0}", thread.Id);


                //     try
                //     {
                //         foreach (StackFrame frame in thread.StackTrace.Frames)
                //         {
                //             Console.WriteLine(frame.FunctionName);
                //         }
                //     }
                //     catch (Exception)
                //     {
                //         Console.WriteLine("Exception happened while reading the frame");
                //     }

                //     Console.WriteLine("===============");
                // }

                Console.WriteLine("----------------------------------");
            }
        }
    }
}
