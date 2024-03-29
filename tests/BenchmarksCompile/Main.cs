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

//using System.Text.RegularExpressions.RegexLight;

namespace System.Text.RegularExpressions.RegexLight
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<RegexLight0>();
            //Console.WriteLine("Result: {0}", SimpleCaseFolding.SimpleCaseFold("cASEfOLDING2"));
            //Console.WriteLine("Result: {0}", SimpleCaseFolding.SimpleCaseFold("яЯяЯяЯяЯяЯя2"));

            var r = new RegexLight0();
            var result = r.Match("a", "fdssa");
            Console.WriteLine(result);
        }
    }

    [DisassemblyDiagnoser(printAsm: true, printSource: true, recursiveDepth: 3)]
    [MemoryDiagnoser]
    [RyuJitX64Job]
    public partial class RegexLight0
    {
        [Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(Data))]
        public int Compile_Orig(string pattern)
        {
            var r = new RegexLight0();
            return r.re_compile_orig(pattern).Length;
        }

        [Benchmark]
        [ArgumentsSource(nameof(Data))]
        public int Compile(string pattern)
        {
            var r = new RegexLight0();
            return r.CompileRegexPattern(pattern).Length;
        }

        public IEnumerable<object> Data()
        {
            yield return @"\d[a]\d";
            yield return @"\d[a-z]\d";
            yield return @"\d[a-z][0-9]";
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private regex_t[] re_compile_orig(ReadOnlySpan<char> pattern)
        {

            // If pattern length less than class char buffer
            // we can skip the buffer index checks.
            if (MAX_CHAR_CLASS_LEN < pattern.Length)
            {
                return null;
            }

            // Current char in pattern and pattern index.
            char currentChar;
            int i = 0;

            int compiledIndex = 0;

            while (i < pattern.Length && (compiledIndex+1 < MAX_REGEXP_OBJECTS))
            {
                currentChar = pattern[i];

                switch (currentChar)
                {
                    // Meta-characters:
                    case '^': {    compiledRegexPattern[compiledIndex].type = RegexElementType.BEGIN;           } break;
                    case '$': {    compiledRegexPattern[compiledIndex].type = RegexElementType.END;             } break;
                    case '.': {    compiledRegexPattern[compiledIndex].type = RegexElementType.DOT;             } break;
                    case '*': {    compiledRegexPattern[compiledIndex].type = RegexElementType.STAR;            } break;
                    case '+': {    compiledRegexPattern[compiledIndex].type = RegexElementType.PLUS;            } break;
                    case '?': {    compiledRegexPattern[compiledIndex].type = RegexElementType.QUESTIONMARK;    } break;
                    // case '|': {    compiledRegexPattern[compiledIndex].type = RegexElementType.BRANCH;          } break; <-- not working properly

                    // Escaped character-classes (\s \w ...):
                    case '\\':
                        {
                            // Skip the escape-char '\\'
                            i++;

                            if (i < pattern.Length)
                            {
                                // ... and check the next
                                switch (pattern[i])
                                {
                                    // Meta-character:
                                    case 'd': {    compiledRegexPattern[compiledIndex].type = RegexElementType.DIGIT;            } break;
                                    case 'D': {    compiledRegexPattern[compiledIndex].type = RegexElementType.NOT_DIGIT;        } break;
                                    case 'w': {    compiledRegexPattern[compiledIndex].type = RegexElementType.ALPHA;            } break;
                                    case 'W': {    compiledRegexPattern[compiledIndex].type = RegexElementType.NOT_ALPHA;        } break;
                                    case 's': {    compiledRegexPattern[compiledIndex].type = RegexElementType.WHITESPACE;       } break;
                                    case 'S': {    compiledRegexPattern[compiledIndex].type = RegexElementType.NOT_WHITESPACE;   } break;

                                    // Escaped character:
                                    default:
                                    {
                                        compiledRegexPattern[compiledIndex].type = RegexElementType.CHAR;

                                        switch (pattern[i])
                                        {
                                            case 'n':
                                                {
                                                    compiledRegexPattern[compiledIndex].ch = '\n';
                                                }
                                                break;
                                            case 'r':
                                                {
                                                    compiledRegexPattern[compiledIndex].ch = '\r';
                                                }
                                                break;
                                            case 't':
                                                {
                                                    compiledRegexPattern[compiledIndex].ch = '\t';
                                                }
                                                break;
                                            default:
                                                {
                                                    compiledRegexPattern[compiledIndex].ch = pattern[i];
                                                }
                                                break;
                                        }
                                    }

                                    break;
                                }
                            }

                            /* '\\' as last char in pattern -> invalid regular expression. */
                    /*
                            else
                            {
                            compiledRegexPattern[compiledIndex].type = CHAR;
                            compiledRegexPattern[compiledIndex].ch = pattern[i];
                            }
                    */
                        }

                        break;

                    // Character class:
                    case '[':
                        {
                            i++;

                            if (i >= pattern.Length)
                            {
                                return null;
                            }

                            // Remember where the char-buffer starts.
                            int buf_begin = charClassBufferIndex;

                            // Determine if negated.
                            if (pattern[i] == '^')
                            {
                                compiledRegexPattern[compiledIndex].type = RegexElementType.INV_CHAR_CLASS;

                                i++;

                                if (i >= pattern.Length)
                                {
                                    return null;
                                }
                            }
                            else
                            {
                                compiledRegexPattern[compiledIndex].type = RegexElementType.CHAR_CLASS;
                            }

                            // Copy characters inside [..] to char classbuffer.
                            while (pattern[i] != ']')
                            {
                                if (pattern[i] == '\\')
                                {
                                    charClassBuffer[charClassBufferIndex++] = pattern[i++];

                                    if (i >= pattern.Length)
                                    {
                                        return null;
                                    }
                                }

                                charClassBuffer[charClassBufferIndex++] = pattern[i++];

                                if (i >= pattern.Length)
                                {
                                    return null;
                                }
                            }

                            compiledRegexPattern[compiledIndex].charClass = (start: buf_begin, len: charClassBufferIndex - buf_begin);
                        }

                        break;

                    /* Other characters: */
                    default:
                        {
                            compiledRegexPattern[compiledIndex].type = RegexElementType.CHAR;
                            compiledRegexPattern[compiledIndex].ch = currentChar;
                        } break;
                }

                i++;
                compiledIndex++;
            }

            // 'UNUSED' is a sentinel used to indicate end-of-pattern.
            compiledRegexPattern[compiledIndex].type = RegexElementType.UNUSED;

            //re_print();

            return (regex_t[]) compiledRegexPattern;
        }


    }
}
