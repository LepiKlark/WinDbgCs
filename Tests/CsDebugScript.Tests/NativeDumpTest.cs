﻿using std = CsDebugScript.CommonUserTypes.NativeTypes.std;
using System;
using System.Linq;
using Xunit;
using System.Collections.Generic;
using CsDebugScript.Engine;
using CsDebugScript.Tests.Utils;

namespace CsDebugScript.Tests
{
    public abstract class NativeDumpTest : DumpTestBase
    {
        private const string MainSourceFileName = "nativedumptest.cpp";

        public NativeDumpTest(DumpInitialization dumpInitialization, bool executeCodeGen = true)
            : base(dumpInitialization)
        {
            ExecuteCodeGen = executeCodeGen;
            if (ExecuteCodeGen && !DumpInitialization.CodeGenExecuted)
            {
                InterpretInteractive($@"
var options = new ImportUserTypeOptions();
options.Modules.Add(""{DefaultModuleName}"");
ImportUserTypes(options, true);
                    ");
                DumpInitialization.CodeGenExecuted = true;
            }
        }

        public bool ExecuteCodeGen { get; private set; }

        public bool ReleaseDump { get; set; }

        public bool LinuxDump { get; set; }

        [Fact]
        public void CurrentThreadContainsMainSourceFileName()
        {
            foreach (var frame in Thread.Current.StackTrace.Frames)
            {
                try
                {
                    if (frame.SourceFileName.ToLower().EndsWith(MainSourceFileName))
                    {
                        return;
                    }
                }
                catch
                {
                    // Ignore exception for getting source file name for frames where we don't have PDBs
                }
            }

            throw new Exception($"{MainSourceFileName} not found on the current thread stack trace");
        }

        [Fact]
        public void CurrentThreadContainsDefaultTestCaseFunction()
        {
            Assert.NotNull(GetFrame($"{DefaultModuleName}!DefaultTestCase"));
        }

        [Fact]
        public void CurrentProcessContainsDefaultModule()
        {
            Assert.Contains(Module.All, module => module.Name == DefaultModuleName);
        }

        [SkippableFact(SkipOnFailurePropertyName = nameof(LinuxDump))]
        public void CheckProcess()
        {
            Process process = Process.Current;

            Console.WriteLine("Architecture type: {0}", process.ArchitectureType);
            Console.WriteLine("SystemId: {0}", process.SystemId);
            Assert.NotEqual(0U, process.SystemId);
            Assert.NotEmpty(Process.All);
            Assert.NotEqual(-1, process.FindMemoryRegion(DefaultModule.Address));
            Assert.Equal(DefaultModule.ImageName, process.ExecutableName);
            Assert.NotNull(process.PEB);
            Assert.Null(process.CurrentCLRAppDomain);
        }

        [SkippableFact(SkipOnFailurePropertyName = nameof(LinuxDump))]
        public void CheckThread()
        {
            Thread thread = Thread.Current;

            Assert.NotEmpty(Thread.All);
            Assert.NotNull(thread.Locals);
            Assert.NotNull(thread.TEB);
            Assert.NotNull(thread.ThreadContext);
        }

        [Fact]
        public void CheckCodeFunction()
        {
            StackFrame defaultTestCaseFrame = GetFrame($"{DefaultModuleName}!DefaultTestCase");
            CodeFunction defaultTestCaseFunction = new CodeFunction(defaultTestCaseFrame.InstructionOffset);

            Assert.NotEqual(0U, defaultTestCaseFunction.Address);
            Assert.NotEqual(0U, defaultTestCaseFunction.FunctionDisplacement);
            Assert.Equal($"{DefaultModuleName}!DefaultTestCase", defaultTestCaseFunction.FunctionName);
            Assert.Equal($"DefaultTestCase", defaultTestCaseFunction.FunctionNameWithoutModule);
            Assert.Equal(Process.Current, defaultTestCaseFunction.Process);
            Assert.Contains(MainSourceFileName, defaultTestCaseFunction.SourceFileName.ToLower());
            Assert.NotEqual(0U, defaultTestCaseFunction.SourceFileLine);
            Console.WriteLine("SourceFileDisplacement: {0}", defaultTestCaseFunction.SourceFileDisplacement);

            Variable codeFunctionVariable = DefaultModule.GetVariable($"{DefaultModuleName}!defaultTestCaseAddress");

            Assert.True(codeFunctionVariable.GetCodeType().IsPointer);

            CodeFunction codeFunction = new CodePointer<CodeFunction>(new NakedPointer(codeFunctionVariable)).Element;

            Assert.Equal($"{DefaultModuleName}!DefaultTestCase", codeFunction.FunctionName);
        }

        [SkippableFact(SkipOnFailurePropertyName = nameof(LinuxDump))]
        public void CheckDebugger()
        {
            Assert.NotEmpty(Debugger.FindAllPatternInMemory(0x1212121212121212));
            Assert.NotEmpty(Debugger.FindAllBytePatternInMemory(new byte[] { 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12 }));
            Assert.NotEmpty(Debugger.FindAllTextPatternInMemory("qwerty"));
        }

        [Fact]
        public void ReadingFloatPointTypes()
        {
            Variable doubleTest = DefaultModule.GetVariable("doubleTest");

            Assert.Equal(3.5, (double)doubleTest.GetField("d"));
            Assert.Equal(2.5, (float)doubleTest.GetField("f"));
            Assert.Equal(5, (int)doubleTest.GetField("i"));

            Variable doubleTest2 = Process.Current.GetGlobal($"{DefaultModuleName}!doubleTest");

            Assert.Equal(doubleTest.GetPointerAddress(), doubleTest2.GetPointerAddress());
            Assert.Equal(doubleTest.GetCodeType(), doubleTest2.GetCodeType());
        }

        [SkippableFact(SkipOnFailurePropertyName = nameof(ReleaseDump))]
        public void GettingClassStaticMember()
        {
            Variable staticVariable = DefaultModule.GetVariable("MyTestClass::staticVariable");

            Assert.Equal(1212121212, (int)staticVariable);
        }

        [Fact]
        public void CheckMainArguments()
        {
            StackFrame mainFrame = GetFrame($"{DefaultModuleName}!main");
            VariableCollection arguments = mainFrame.Arguments;

            Assert.True(arguments.ContainsName("argc"));
            Assert.True(arguments.ContainsName("argv"));
            Assert.Equal(1, (int)arguments["argc"]);
            Assert.Equal(2, arguments.Count);

            for (int i = 0; i < arguments.Count; i++)
            {
                Variable argument = arguments[i];

                Assert.False(argument.IsNullPointer());
            }

            string command = arguments["argv"].GetArrayElement(0).ToString();
            Assert.Contains("NativeDumpTest", command);

            Variable p;
            Assert.False(arguments.TryGetValue("p", out p));
            Assert.Empty(arguments.Names.Where(n => n == "p"));
            Assert.Empty(arguments.Where(a => a.GetName() == "p"));
        }

        [Fact]
        public void CheckDefaultTestCase()
        {
            StackFrame frame = GetFrame($"{DefaultModuleName}!DefaultTestCase");
            VariableCollection locals = frame.Locals;
            dynamic p = locals["p"];
            std.wstring string1 = new std.wstring(p.string1);
            Assert.Equal("qwerty", string1.Text);
            std.list<std.wstring> strings = new std.list<std.wstring>(p.strings);
            std.vector<std.@string> ansiStrings = new std.vector<std.@string>(p.ansiStrings);
            std.map<std.wstring, std.@string> stringMap = new std.map<std.wstring, std.@string>(p.stringMap);
            std.unordered_map<std.wstring, std.@string> stringUMap = new std.unordered_map<std.wstring, std.@string>(p.stringUMap);

            string[] stringsConverted = strings.Select(s => s.Text).ToArray();
            string[] ansiStringsConverted = ansiStrings.Select(s => s.Text).ToArray();

            CompareArrays(new[] { "Foo", "Bar" }, stringsConverted);
            CompareArrays(new[] { "AnsiFoo", "AnsiBar" }, ansiStringsConverted);

            foreach (std.wstring s in strings)
            {
                Assert.True(s.Length <= s.Reserved);
            }
            for (int i = 0; i < ansiStrings.Count; i++)
            {
                Assert.True(ansiStrings[i].Length <= ansiStrings[i].Reserved);
            }

            VerifyMap(stringMap);
            VerifyMap(stringUMap);

            // Verify enum value
            dynamic e = locals["e"];

            Assert.Equal("enumEntry3", e.ToString());
            Assert.Equal(3, (int)e);

            dynamic pEnumeration = p.enumeration;
            dynamic pInnerEnumeration = p.innerEnumeration;
            Assert.Equal("enumEntry2", pEnumeration.ToString());
            Assert.Equal(2, (int)pEnumeration);
            Assert.Equal("simple4", pInnerEnumeration.ToString());
            Assert.Equal(4, (int)pInnerEnumeration);
        }

        [Fact]
        public void CheckSharedWeakPointers()
        {
            StackFrame frame = GetFrame($"{DefaultModuleName}!TestSharedWeakPointers");
            VariableCollection locals = frame.Locals;

            // Verify shared/weak pointers
            std.shared_ptr<int> sptr1 = new std.shared_ptr<int>(locals["sptr1"]);
            std.shared_ptr<int> esptr1 = new std.shared_ptr<int>(locals["esptr1"]);
            std.shared_ptr<int> esptr2 = new std.shared_ptr<int>(locals["esptr2"]);
            std.weak_ptr<int> wptr1 = new std.weak_ptr<int>(locals["wptr1"]);
            std.weak_ptr<int> ewptr1 = new std.weak_ptr<int>(locals["ewptr1"]);
            std.weak_ptr<int> ewptr2 = new std.weak_ptr<int>(locals["ewptr2"]);

            Assert.False(sptr1.IsEmpty);
            Assert.Equal(1, sptr1.SharedCount);
            Assert.Equal(2, sptr1.WeakCount);
            Assert.Equal(5, sptr1.Element);
            Assert.True(sptr1.IsCreatedWithMakeShared);

            Assert.False(wptr1.IsEmpty);
            Assert.Equal(1, wptr1.SharedCount);
            Assert.Equal(2, wptr1.WeakCount);
            Assert.Equal(5, wptr1.Element);
            Assert.True(wptr1.IsCreatedWithMakeShared);

            Assert.True(esptr1.IsEmpty);

            Assert.True(ewptr1.IsEmpty);
            Assert.Equal(0, ewptr1.SharedCount);
            Assert.Equal(1, ewptr1.WeakCount);
            Assert.Equal(42, ewptr1.UnsafeElement);
            Assert.True(ewptr1.IsCreatedWithMakeShared);

            Assert.True(esptr2.IsEmpty);

            Assert.True(ewptr2.IsEmpty);
            Assert.Equal(0, ewptr2.SharedCount);
            Assert.Equal(1, ewptr2.WeakCount);
            Assert.False(ewptr2.IsCreatedWithMakeShared);
        }

        [Fact]
        public void CheckCodeArray()
        {
            StackFrame defaultTestCaseFrame = GetFrame($"{DefaultModuleName}!TestArray");
            VariableCollection locals = defaultTestCaseFrame.Locals;
            Variable testArrayVariable = locals["testArray"];
            CodeArray<int> testArray = new CodeArray<int>(testArrayVariable);

            Assert.Equal(10000, testArray.Length);
            foreach (int value in testArray)
            {
                Assert.Equal(0x12121212, value);
            }
        }

        [Fact]
        public void TestBasicTemplateType()
        {
            StackFrame defaultTestCaseFrame = GetFrame($"{DefaultModuleName}!TestBasicTemplateType");
            VariableCollection locals = defaultTestCaseFrame.Locals;
            Variable floatTemplate = locals["floatTemplate"];
            Variable doubleTemplate = locals["doubleTemplate"];
            Variable intTemplate = locals["intTemplate"];
            Variable[] templateVariables = new Variable[] { floatTemplate, doubleTemplate, intTemplate };

            foreach (Variable variable in templateVariables)
            {
                Variable value = variable.GetField("value");
                Variable values = variable.GetField("values");
                Assert.Equal("42", value.ToString());
                for (int i = 0, n = values.GetArrayLength(); i < n; i++)
                {
                    Assert.Equal(i.ToString(), values.GetArrayElement(i).ToString());
                }
            }

            if (ExecuteCodeGen)
            {
                InterpretInteractive($@"
StackFrame frame = GetFrame(""{DefaultModuleName}!TestBasicTemplateType"");
VariableCollection locals = frame.Locals;

var floatTemplate = new BasicTemplateType<float>(locals[""floatTemplate""]);
AreEqual(42, floatTemplate.value);
for (int i = 0; i < floatTemplate.values.Length; i++)
    AreEqual(i, floatTemplate.values[i]);

var doubleTemplate = new BasicTemplateType<double>(locals[""doubleTemplate""]);
AreEqual(42, doubleTemplate.value);
for (int i = 0; i < doubleTemplate.values.Length; i++)
    AreEqual(i, doubleTemplate.values[i]);

var intTemplate = new BasicTemplateType<int>(locals[""intTemplate""]);
AreEqual(42, intTemplate.value);
for (int i = 0; i < intTemplate.values.Length; i++)
    AreEqual(i, intTemplate.values[i]);
                   ");
            }
        }

        [Fact]
        public void TestBuiltinTypes()
        {
            CodeType codeType = CodeType.Create("BuiltinTypesTest", DefaultModule);

            VerifyFieldBuiltinType(codeType, "b", BuiltinType.Bool);
            VerifyFieldBuiltinType(codeType, "c1", BuiltinType.Char8, BuiltinType.Int8);
            VerifyFieldBuiltinType(codeType, "c2", BuiltinType.Char16, BuiltinType.Char32);
            VerifyFieldBuiltinType(codeType, "i8", BuiltinType.Int8, BuiltinType.Char8);
            VerifyFieldBuiltinType(codeType, "i16", BuiltinType.Int16);
            VerifyFieldBuiltinType(codeType, "i32", BuiltinType.Int32);
            VerifyFieldBuiltinType(codeType, "i64", BuiltinType.Int64);
            VerifyFieldBuiltinType(codeType, "u8", BuiltinType.UInt8);
            VerifyFieldBuiltinType(codeType, "u16", BuiltinType.UInt16);
            VerifyFieldBuiltinType(codeType, "u32", BuiltinType.UInt32);
            VerifyFieldBuiltinType(codeType, "u64", BuiltinType.UInt64);
            VerifyFieldBuiltinType(codeType, "f32", BuiltinType.Float32);
            VerifyFieldBuiltinType(codeType, "f64", BuiltinType.Float64);
            VerifyFieldBuiltinType(codeType, "f80", BuiltinType.Float80, BuiltinType.Float64);
        }

        private void VerifyFieldBuiltinType(CodeType codeType, string fieldName, params BuiltinType[] expected)
        {
            CodeType fieldCodeType = codeType.GetFieldType(fieldName);
            NativeCodeType nativeCodeType = fieldCodeType as NativeCodeType;

            Assert.NotNull(nativeCodeType);
            Assert.True(nativeCodeType.Tag == CodeTypeTag.BuiltinType || nativeCodeType.Tag == CodeTypeTag.Enum);
            VerifyBuiltinType(nativeCodeType, expected);
        }

        private void VerifyBuiltinType(NativeCodeType codeType, params BuiltinType[] expected)
        {
            BuiltinType actual = codeType.BuiltinType;

            foreach (BuiltinType builtinType in expected)
            {
                if (actual == builtinType)
                {
                    return;
                }
            }

            Assert.Equal(expected[0], actual);
        }

        private static void VerifyMap(IReadOnlyDictionary<std.wstring, std.@string> stringMap)
        {
            string[] mapKeys = stringMap.Keys.Select(s => s.Text).ToArray();
            string[] mapValues = stringMap.Values.Select(s => s.Text).ToArray();
            Dictionary<string, string> stringMapExpected = new Dictionary<string, string>()
            {
                { "foo", "ansiFoo" },
                { "bar", "ansiBar" },
            };

            Assert.Equal(2, stringMap.Count);
            CompareArrays(new[] { "foo", "bar" }, mapKeys);
            CompareArrays(new[] { "ansiFoo", "ansiBar" }, mapValues);
            foreach (KeyValuePair<std.wstring, std.@string> kvp in stringMap)
            {
                std.@string value;

                Assert.True(stringMap.ContainsKey(kvp.Key));
                Assert.True(stringMap.TryGetValue(kvp.Key, out value));
                Assert.Equal(kvp.Value.Text, value.Text);
                value = stringMap[kvp.Key];
                Assert.Equal(kvp.Value.Text, value.Text);
                Assert.Equal(stringMapExpected[kvp.Key.Text], kvp.Value.Text);
            }

            Assert.Equal(2, stringMap.ToStringDictionary().ToStringStringDictionary().Count);
        }

        private void InterpretInteractive(string code)
        {
            code = @"
StackFrame GetFrame(string functionName)
{
    foreach (var frame in Thread.Current.StackTrace.Frames)
    {
        try
        {
            if (frame.FunctionName == functionName)
            {
                return frame;
            }
        }
        catch (Exception)
        {
            // Ignore exception for getting source file name for frames where we don't have PDBs
        }
    }

    throw new Exception($""Frame not found '{functionName}'"");
}

void AreEqual<T>(T value1, T value2)
    where T : IEquatable<T>
{
    if (!value1.Equals(value2))
    {
        throw new Exception($""Not equal. value1 = {value1}, value2 = {value2}"");
    }
}
                " + code;

            DumpInitialization.InteractiveExecution.UnsafeInterpret(code);
        }
    }

    #region Test configurations
    [Collection("NativeDumpTest.x64.mdmp")]
    [Trait("x64", "true")]
    [Trait("x86", "true")]
    public class NativeDumpTest_x64 : NativeDumpTest
    {
        public NativeDumpTest_x64(NativeDumpTest_x64_dmp_Initialization initialization)
            : base(initialization)
        {
        }
    }

    [Collection("NativeDumpTest.x64.mdmp NoDia")]
    [Trait("x64", "true")]
    [Trait("x86", "true")]
    public class NativeDumpTest_x64_NoDia : NativeDumpTest
    {
        public NativeDumpTest_x64_NoDia(NativeDumpTest_x64_dmp_NoDia_Initialization initialization)
            : base(initialization, executeCodeGen: false)
        {
        }
    }

    [Collection("NativeDumpTest.x64.Release.mdmp")]
    [Trait("x64", "true")]
    [Trait("x86", "true")]
    public class NativeDumpTest_x64_Release : NativeDumpTest
    {
        public NativeDumpTest_x64_Release(NativeDumpTest_x64_Release_dmp_Initialization initialization)
            : base(initialization)
        {
            ReleaseDump = true;
        }
    }

    [Collection("NativeDumpTest.x86.mdmp")]
    [Trait("x64", "true")]
    [Trait("x86", "true")]
    public class NativeDumpTest_x86 : NativeDumpTest
    {
        public NativeDumpTest_x86(NativeDumpTest_x86_dmp_Initialization initialization)
            : base(initialization)
        {
        }
    }

    [Collection("NativeDumpTest.x86.Release.mdmp")]
    [Trait("x64", "true")]
    [Trait("x86", "true")]
    public class NativeDumpTest_x86_Release : NativeDumpTest
    {
        public NativeDumpTest_x86_Release(NativeDumpTest_x86_Release_dmp_Initialization initialization)
            : base(initialization)
        {
            ReleaseDump = true;
        }
    }

    [Collection("NativeDumpTest.x64.VS2013.mdmp")]
    [Trait("x64", "true")]
    [Trait("x86", "true")]
    public class NativeDumpTest_x64_VS2013 : NativeDumpTest
    {
        public NativeDumpTest_x64_VS2013(NativeDumpTest_x64_VS2013_mdmp_Initialization initialization)
            : base(initialization)
        {
        }
    }

    [Collection("NativeDumpTest.x64.VS2015.mdmp")]
    [Trait("x64", "true")]
    [Trait("x86", "true")]
    public class NativeDumpTest_x64_VS2015 : NativeDumpTest
    {
        public NativeDumpTest_x64_VS2015(NativeDumpTest_x64_VS2015_mdmp_Initialization initialization)
            : base(initialization)
        {
        }
    }

    [Collection("NativeDumpTest.gcc.mdmp")]
    [Trait("x64", "true")]
    [Trait("x86", "true")]
    public class NativeDumpTest_x86_gcc : NativeDumpTest
    {
        public NativeDumpTest_x86_gcc(NativeDumpTest_gcc_dmp_Initialization initialization)
            : base(initialization)
        {
        }
    }

    [Collection("NativeDumpTest.x64.gcc.mdmp")]
    [Trait("x64", "true")]
    [Trait("x86", "true")]
    public class NativeDumpTest_x64_gcc : NativeDumpTest
    {
        public NativeDumpTest_x64_gcc(NativeDumpTest_x64_gcc_Initialization initialization)
            : base(initialization)
        {
        }
    }

    [Collection("NativeDumpTest.linux.x86.gcc.coredump")]
    [Trait("x64", "true")]
    [Trait("x86", "true")]
    public class NativeDumpTest_x86_Linux_gcc : NativeDumpTest
    {
        public NativeDumpTest_x86_Linux_gcc(NativeDumpTest_linux_x86_gcc_Initialization initialization)
            : base(initialization)
        {
            LinuxDump = true;
        }
    }

    [Collection("NativeDumpTest.linux.x64.gcc.coredump")]
    [Trait("x64", "true")]
    [Trait("x86", "true")]
    public class NativeDumpTest_x64_Linux_gcc : NativeDumpTest
    {
        public NativeDumpTest_x64_Linux_gcc(NativeDumpTest_linux_x64_gcc_Initialization initialization)
            : base(initialization)
        {
            LinuxDump = true;
        }
    }

    [Collection("NativeDumpTest.linux.x64.clang.coredump")]
    [Trait("x64", "true")]
    [Trait("x86", "true")]
    public class NativeDumpTest_x64_Linux_clang : NativeDumpTest
    {
        public NativeDumpTest_x64_Linux_clang(NativeDumpTest_linux_x64_clang_Initialization initialization)
            : base(initialization, executeCodeGen: false)
        {
            LinuxDump = true;
        }
    }
    #endregion
}
