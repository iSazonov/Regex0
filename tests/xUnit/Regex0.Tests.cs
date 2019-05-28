// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Regex;
using Xunit;

namespace Regex.Regex0Tests
{
    public class FoldCharTests
    {
        [Theory]
        [InlineData("a", "fdssa", 4)]
        [InlineData("d", "fdssa", 1)]
        [InlineData(@"\d", "fd1ssa", 2)]
        public static void RegexTests(string pattern, string text, int expected)
        {
            var r = new Regex0();
            var result = r.re_match(pattern, text);
            Assert.Equal(expected, result);
        }
    }
}
