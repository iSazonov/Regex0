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
            var result = r.Match("a", "fdssa");
            Console.WriteLine(result);
        }
    }

    [DisassemblyDiagnoser(printAsm: true, printSource: true, recursiveDepth: 2)]
    [MemoryDiagnoser]
    [RyuJitX64Job]
    public class IntroBenchmarkBaseline
    {
        [Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(Data))]
        public bool matchrangeOrig(char ch, string text)
        {
            return matchrange_orig(ch, text);
        }

        [Benchmark]
        [ArgumentsSource(nameof(Data))]
        public bool matchrangeNew(char ch, string text)
        {
            return matchrange(ch,text);
        }

        private static bool matchrange(char c, ReadOnlySpan<char> str)
        {
            return (str.Length >= 3) && (str[1] == '-') && ((uint)(c - str[0]) <= (uint)str[2]);
        }

        private static bool matchrange_orig(char c, ReadOnlySpan<char> str)
        {
            return (str.Length >= 3) && (str[1] == '-') && (c >= str[0]) && (c <= str[2]);
        }


        public IEnumerable<object[]> Data()
        {
            yield return new object[] { 'b', "a-z" };
        }
    }
}
