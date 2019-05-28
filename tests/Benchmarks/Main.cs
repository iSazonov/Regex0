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

using Regex;

namespace System.Text.RegularExpressions.RegexPerfTest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<IntroBenchmarkBaseline>();
            //Console.WriteLine("Result: {0}", SimpleCaseFolding.SimpleCaseFold("cASEfOLDING2"));
            //Console.WriteLine("Result: {0}", SimpleCaseFolding.SimpleCaseFold("яЯяЯяЯяЯяЯя2"));

            var r = new Regex0();
            var result = r.re_match("a", "fdssa");
            Console.WriteLine(result);
        }
    }

    [DisassemblyDiagnoser(printAsm: true, printSource: true, recursiveDepth: 3)]
    [RyuJitX64Job]
    public class IntroBenchmarkBaseline
    {
        [Benchmark(Baseline = true)]
        //[ArgumentsSource(nameof(Data))]
        public int Regex0()
        {
            var r = new Regex0();
            return r.re_match("a", "1111111111111111111111111111111111111111111111111111111fdssa");
        }

        [Benchmark]
        //[ArgumentsSource(nameof(Data))]
        public bool DotnetRegex()
        {
            var rr = new System.Text.RegularExpressions.Regex("a");
            return rr.IsMatch("1111111111111111111111111111111111111111111111111111111fdssa");
        }

        public IEnumerable<object> Data()
        {
            yield return '\u0600';
        }
    }
}
