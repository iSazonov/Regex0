// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.RegularExpressions.RegexLight;
using Xunit;

namespace System.Text.RegularExpressions.Regex.RegexLight.Regex0Tests
{
    public class Regex0Tests
    {
        [Theory]
        [InlineData("", "fdssa", 0)]
        [InlineData("a", "", -1)]
        [InlineData("a", "fdssa", 4)]
        [InlineData("d", "fdssa", 1)]
        [InlineData(@"\d", "fd1ssa", 2)]
        [InlineData(@"\d", "5", 0)]
        [InlineData(@"\w+", "test", 0)]
        [InlineData(@"\s", "\t \n", 0)]
        [InlineData(@"\S", "\t \n", -1)]
        [InlineData(@"[\s]", "\t \n", 0)]
        [InlineData(@"[\S]", "\t \n", -1)]
        [InlineData(@"\D", "5", -1)]
        [InlineData(@"\W+", "test", -1)]
        [InlineData(@"[0-9]+", "12345", 0)]
        [InlineData(@"\D", "test", 0)]
        [InlineData(@"\d", "test", -1)]
        [InlineData(@"[^\w]", "\\", 0)]
        [InlineData(@"[\W]", "\\", 0)]
        [InlineData(@"[\w]", "\\", -1)]
        [InlineData(@"[^\d]", "d", 0)]
        [InlineData(@"[\d]", "d", -1)]
        [InlineData(@"[^\D]", "d", -1)]
        [InlineData(@"[\D]", "d", 0)]
        [InlineData(@"^.*\\.*$", "c:\\Tools", 0)]
//        [InlineData(@"^[\+-]*[\d]+$", "+27", 0)]
        [InlineData(@"[abc]", "1c2", 1)]
        [InlineData(@"[abc]", "1C2", -1)]
        [InlineData(@"[1-5]+", "0123456789", 1)]
        [InlineData(@"[.2]", "1C2", 2)]
        [InlineData(@"a*$", "Xaa", 1)]
        [InlineData(@"[a-h]+", "abcdefghxxx", 0)]
        [InlineData(@"[a-h]+", "ABCDEFGH", -1)]
        [InlineData(@"[A-H]+", "ABCDEFGH", 0)]
        [InlineData(@"[A-H]+", "abcdefgh", -1)]
        [InlineData(@"[^\s]+", "abc def", 0)]
        [InlineData(@"[^fc]+", "abc def", 0)]
        [InlineData(@"[^d\sf]+", "abc def", 0)]
        [InlineData(@"\n", "abc\ndef", 3)]
        [InlineData(@"\r", "abc\rdef", 3)]
        [InlineData(@"\t", "abc\tdef", 3)]
        [InlineData(@"b.\s*\n", "aa\r\nbb\r\ncc\r\n\r\n", 4)]
        [InlineData(@".*c", "abcabc", 0)]
        [InlineData(@".+c", "abcabc", 0)]
        [InlineData(@"[b-z].*", "ab", 1)]
        [InlineData(@"b[k-z]*", "ab", 1)]
        [InlineData(@"[0-9]", "   -   ", -1)]
        [InlineData(@"[^0-9]", "   -   ", 0)]
        [InlineData(@"0|", "0|", 0)]
        [InlineData(@"\d\d:\d\d:\d\d", "0s:00:00", -1)]
        [InlineData(@"\d\d:\d\d:\d\d", "000:00", -1)]
        [InlineData(@"\d\d:\d\d:\d\d", "00:0000", -1)]
        [InlineData(@"\d\d:\d\d:\d\d", "100:0:00", -1)]
        [InlineData(@"\d\d:\d\d:\d\d", "00:100:00", -1)]
        [InlineData(@"\d\d:\d\d:\d\d", "0:00:100", -1)]
        [InlineData(@"\d\d?:\d\d?:\d\d?", "0:0:0", 0)]
        [InlineData(@"\d\d?:\d\d?:\d\d?", "0:00:0", 0)]
        [InlineData(@"\d\d?:\d\d?:\d\d?", "0:0:00", 0)]
        [InlineData(@"\d\d?:\d\d?:\d\d?", "00:0:0", 0)]
        [InlineData(@"\d\d?:\d\d?:\d\d?", "00:00:0", 0)]
        [InlineData(@"\d\d?:\d\d?:\d\d?", "00:0:00", 0)]
        [InlineData(@"\d\d?:\d\d?:\d\d?", "0:00:00", 0)]
        [InlineData(@"\d\d?:\d\d?:\d\d?", "00:00:00", 0)]
        [InlineData(@"[Hh]ello [Ww]orld\s*[!]?", "Hello world !", 0)]
        [InlineData(@"[Hh]ello [Ww]orld\s*[!]?", "hello world !", 0)]
        [InlineData(@"[Hh]ello [Ww]orld\s*[!]?", "hello World !", 0)]
        [InlineData(@"[Hh]ello [Ww]orld\s*[!]?", "Hello World !", 0)]
        [InlineData(@"[Hh]ello [Ww]orld\s*[!]?", "Hello world!   ", 0)]
        [InlineData(@"[Hh]ello [Ww]orld\s*[!]?", "Hello world   !", 0)]
        [InlineData(@"[Hh]ello [Ww]orld\s*[!]?", "hello World   !", 0)]
        [InlineData(@"\d\d?:\d\d?:\d\d?", "a:0", -1)]
        [InlineData(@".?bar", "real_bar", 4)]
        [InlineData(@".?bar", "real_foo", -1)]
        [InlineData(@"X?Y", "Z", -1)]
        [InlineData(@"\d:\d:\d", "s:0:0", -1)]
        public static void RegexTests(string pattern, string text, int expected)
        {
            var r = new RegexLight0();
            var result = r.Match(pattern, text);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(@"qwerty", "123QWERTY", 3)]
        [InlineData(@"[abc]", "1C2", 1)]
        [InlineData(@"[a-h]+", "ABCDEFGH", 0)]
        [InlineData(@"[A-H]+", "abcdefgh", 0)]
        public static void RegexTestsIgnoreCase(string pattern, string text, int expected)
        {
            var r = new RegexLight0();
            var result = r.Match(pattern, text, ignoreCase: true);
            Assert.Equal(expected, result);
        }
    }
}
