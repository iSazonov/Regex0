// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

using System.Text.RegularExpressions.RegexLight;

namespace System.Text.RegularExpressions.RegexLightPerfTest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<IntroBenchmarkBaseline>();
            //Console.WriteLine("Result: {0}", SimpleCaseFolding.SimpleCaseFold("cASEfOLDING2"));
            //Console.WriteLine("Result: {0}", SimpleCaseFolding.SimpleCaseFold("яЯяЯяЯяЯяЯя2"));

            var r = new RegexLight0();
            var result = r.re_match("a", "fdssa");
            Console.WriteLine(result);
        }
    }

    [DisassemblyDiagnoser(printAsm: true, printSource: true, recursiveDepth: 3)]
    [MemoryDiagnoser]
    [RyuJitX64Job]
    public class IntroBenchmarkBaseline
    {
        [Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(Data))]
        public int Regex0(string pattern, string text)
        {
            var r = new RegexLight0();
            return r.re_match(pattern, text);
        }

        [Benchmark]
        [ArgumentsSource(nameof(Data))]
        public bool DotnetRegex(string pattern, string text)
        {
            var rr = new System.Text.RegularExpressions.Regex(pattern);
            return rr.IsMatch(text);
        }

        public IEnumerable<object[]> Data()
        {
//            yield return new object[] { @"a", "a" };
//            yield return new object[] { @"a", "1234567890a" };
//            yield return new object[] { @"a", "12345678901234567890123456789012345678901234567890a" };
//            yield return new object[] { @"\da\d", "12345678901234567890123456789012345678901234567890a0" };
            yield return new object[] { @"\d[a]\d", "12345678901234567890123456789012345678901234567890a0" };
            yield return new object[] { @"\d[a-z]\d", "12345678901234567890123456789012345678901234567890a0" };
            yield return new object[] { @"\d[a-z][0-9]", "12345678901234567890123456789012345678901234567890a0" };
        }
    }
}
