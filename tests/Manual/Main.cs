// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using System.Text.RegularExpressions.RegexLight;

namespace System.Text.RegularExpressions.RegexLight
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //var summary = BenchmarkRunner.Run<IntroBenchmarkBaseline>();
            //Console.WriteLine("Result: {0}", SimpleCaseFolding.SimpleCaseFold("cASEfOLDING2"));
            //Console.WriteLine("Result: {0}", SimpleCaseFolding.SimpleCaseFold("яЯяЯяЯяЯяЯя2"));
            Console.WriteLine("args: {0}", args.Length);
            Console.WriteLine("args: {0}:{1}", args[0], args[1]);
            var r = new RegexLight0();
            var result = r.Match(args[0], args[1], true);
            Console.WriteLine(result);
            r.re_print();
        }
    }
}
