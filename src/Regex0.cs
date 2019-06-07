/*
 *
 * Mini regex-module inspired by Rob Pike's regex code described in:
 *
 * http://www.cs.princeton.edu/courses/archive/spr09/cos333/beautiful.html
 *
 *
 *
 * Supports:
 * ---------
 *   '.'        Dot, matches any character
 *   '^'        Start anchor, matches beginning of string
 *   '$'        End anchor, matches end of string
 *   '*'        Asterisk, match zero or more (greedy)
 *   '+'        Plus, match one or more (greedy)
 *   '?'        Question, match zero or one (non-greedy)
 *   '[abc]'    Character class, match if one of {'a', 'b', 'c'}
 *   '[^abc]'   Inverted class, match if NOT one of {'a', 'b', 'c'} -- NOTE: feature is currently broken!
 *   '[a-zA-Z]' Character ranges, the character set of the ranges { a-z | A-Z }
 *   '\s'       Whitespace, \t \f \r \n \v and spaces
 *   '\S'       Non-whitespace
 *   '\w'       Alphanumeric, [a-zA-Z0-9_]
 *   '\W'       Non-alphanumeric
 *   '\d'       Digits, [0-9]
 *   '\D'       Non-digits
 *   '\r'       Return char
 *   '\n'       New line char
 *   '\t'       Tab char
 *
 */

using System;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.RegexLight
{

    // Definitions:


    enum RegexElementType { UNUSED, DOT, BEGIN, END, QUESTIONMARK, STAR, PLUS, CHAR, CHAR_CLASS, INV_CHAR_CLASS, DIGIT, NOT_DIGIT, ALPHA, NOT_ALPHA, WHITESPACE, NOT_WHITESPACE, /* BRANCH */ };

    struct regex_t
    {
        internal RegexElementType type;

        // the character itself
        internal char  ch;

        // OR  a pointer to characters in class
        internal (int start, int len) ccl;
    }

    public class RegexLight0
    {

        // Max number of regex symbols in expression.
        private const int MAX_REGEXP_OBJECTS = 30;

        // Max length of character-class buffer in.
        private const int MAX_CHAR_CLASS_LEN = 40;

        // Parsed regex pattern.
        private regex_t[] re_compiled = new regex_t[MAX_REGEXP_OBJECTS];

        // Buffer for chars in all char-classes in the pattern.
        private char[] ccl_buf = new char[MAX_CHAR_CLASS_LEN];
        int ccl_bufidx = 0;


        // Public functions:
        public int re_match(ReadOnlySpan<char> pattern, ReadOnlySpan<char> text)
        {
            return re_matchp(re_compile(pattern), text);
        }

        private int re_matchp(ReadOnlySpan<regex_t> pattern, ReadOnlySpan<char> text)
        {
            if (!pattern.IsEmpty)
            {
                if (pattern[0].type == RegexElementType.BEGIN)
                {
                    return ((matchpattern(pattern.Slice(1), text, out int skip)) ? 0 : -1);
                }
                else
                {
                    int idx = 0;

                    do
                    {
                        //idx += 1;

                        if (matchpattern(pattern, text.Slice(idx), out int skip))
                        {
                            if (text.IsEmpty)
                            {
                                return -1;
                            }

                            return idx;
                        }

                        idx += skip;
                    }
                    while (++idx < text.Length);
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private regex_t[] re_compile(ReadOnlySpan<char> pattern)
        {

            // If pattern length less than class char buffer
            // we can skip the buffer index checks.
            if (MAX_CHAR_CLASS_LEN < pattern.Length)
            {
                return null;
            }

            char c;     /* current char in pattern   */
            int i = 0;  /* index into pattern        */
            int j = 0;  /* index into re_compiled    */

            while (i < pattern.Length && (j+1 < MAX_REGEXP_OBJECTS))
            {
                c = pattern[i];

                switch (c)
                {
                    /* Meta-characters: */
                    case '^': {    re_compiled[j].type = RegexElementType.BEGIN;           } break;
                    case '$': {    re_compiled[j].type = RegexElementType.END;             } break;
                    case '.': {    re_compiled[j].type = RegexElementType.DOT;             } break;
                    case '*': {    re_compiled[j].type = RegexElementType.STAR;            } break;
                    case '+': {    re_compiled[j].type = RegexElementType.PLUS;            } break;
                    case '?': {    re_compiled[j].type = RegexElementType.QUESTIONMARK;    } break;
            /*    case '|': {    re_compiled[j].type = RegexElementType.BRANCH;          } break; <-- not working properly */

                    /* Escaped character-classes (\s \w ...): */
                    case '\\':
                        {
                            /* Skip the escape-char '\\' */
                            i++;

                            if (i < pattern.Length)
                            {
                                /* ... and check the next */
                                switch (pattern[i])
                                {
                                    /* Meta-character: */
                                    case 'd': {    re_compiled[j].type = RegexElementType.DIGIT;            } break;
                                    case 'D': {    re_compiled[j].type = RegexElementType.NOT_DIGIT;        } break;
                                    case 'w': {    re_compiled[j].type = RegexElementType.ALPHA;            } break;
                                    case 'W': {    re_compiled[j].type = RegexElementType.NOT_ALPHA;        } break;
                                    case 's': {    re_compiled[j].type = RegexElementType.WHITESPACE;       } break;
                                    case 'S': {    re_compiled[j].type = RegexElementType.NOT_WHITESPACE;   } break;

                                    /* Escaped character, e.g. '.' or '$' */
                                    default:
                                    {
                                        re_compiled[j].type = RegexElementType.CHAR;

                                        switch (pattern[i])
                                        {
                                            case 'n':
                                                {
                                                    re_compiled[j].ch = '\n';
                                                }
                                                break;
                                            case 'r':
                                                {
                                                    re_compiled[j].ch = '\r';
                                                }
                                                break;
                                            case 't':
                                                {
                                                    re_compiled[j].ch = '\t';
                                                }
                                                break;
                                            default:
                                                {
                                                    re_compiled[j].ch = pattern[i];
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
                            re_compiled[j].type = CHAR;
                            re_compiled[j].ch = pattern[i];
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
                            int buf_begin = ccl_bufidx;

                            // Determine if negated.
                            if (pattern[i] == '^')
                            {
                                re_compiled[j].type = RegexElementType.INV_CHAR_CLASS;

                                i++;

                                if (i >= pattern.Length)
                                {
                                    return null;
                                }
                            }
                            else
                            {
                                re_compiled[j].type = RegexElementType.CHAR_CLASS;
                            }

                            /* Copy characters inside [..] to buffer */
                            while (pattern[i] != ']')
                            {
                                if (pattern[i] == '\\')
                                {
                                    ccl_buf[ccl_bufidx++] = pattern[i++];

                                    if (i >= pattern.Length)
                                    {
                                        return null;
                                    }
                                }

                                ccl_buf[ccl_bufidx++] = pattern[i++];

                                if (i >= pattern.Length)
                                {
                                    return null;
                                }
                            }

                            re_compiled[j].ccl = (start: buf_begin, len: ccl_bufidx - buf_begin);
                        }

                        break;

                    /* Other characters: */
                    default:
                        {
                            re_compiled[j].type = RegexElementType.CHAR;
                            re_compiled[j].ch = c;
                        } break;
                }

                i++;
                j++;
            }

            /* 'UNUSED' is a sentinel used to indicate end-of-pattern */
            re_compiled[j].type = RegexElementType.UNUSED;

            //re_print();

            return (regex_t[]) re_compiled;
        }

        public void re_print()
        {
            int i;
            var pattern = re_compiled;

            for (i = 0; i < MAX_REGEXP_OBJECTS; ++i)
            {
                if (pattern[i].type == RegexElementType.UNUSED)
                {
                    //break;
                    continue;
                }

                Console.Write("type: {0} -> ", pattern[i].type);
                if (pattern[i].type == RegexElementType.CHAR_CLASS || pattern[i].type == RegexElementType.INV_CHAR_CLASS)
                {
                    Console.Write(" [");

                    int j;
                    char c;

                    var cclSpan = new ReadOnlySpan<char>(ccl_buf, pattern[i].ccl.start, pattern[i].ccl.len);
                    for (j = 0; j < cclSpan.Length; ++j)
                    {
                        c = cclSpan[j];
                        if (c == ']')
                        {
                            break;
                        }
                        Console.Write("{0}", c);
                    }

                    Console.WriteLine("]");
                }
                else if (pattern[i].type == RegexElementType.CHAR)
                {
                    Console.WriteLine(" '{0}'", pattern[i].ch);
                }

                Console.WriteLine("\n");
            }
        }



        // Private functions:
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool matchdigit(char c)
        {
            return char.IsDigit(c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool matchalpha(char c)
        {
            return char.IsLetter(c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool matchwhitespace(char c)
        {
            return char.IsWhiteSpace(c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool matchalphanum(char c)
        {
            return ((c == '_') || char.IsLetterOrDigit(c));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool matchrange(char c, ReadOnlySpan<char> str)
        {
            return (str.Length >= 3) && (str[1] == '-') && ((c >= str[0]) && (c <= str[2]));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ismetachar(char c)
        {
            return ((c == 's') || (c == 'S') || (c == 'w') || (c == 'W') || (c == 'd') || (c == 'D'));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool matchmetachar(char c, ReadOnlySpan<char> str)
        {
            switch (str[0])
            {
                case 'd': return  matchdigit(c);
                case 'D': return !matchdigit(c);
                case 'w': return  matchalphanum(c);
                case 'W': return !matchalphanum(c);
                case 's': return  matchwhitespace(c);
                case 'S': return !matchwhitespace(c);
                default:  return (c == str[0]);
            }
        }

        private bool matchcharclass(char c, (int start, int len) v)
        {
            int i = 0;

            ReadOnlySpan<char> str = new ReadOnlySpan<char>(ccl_buf, v.start, v.len);

            do
            {
                if (matchrange(c, str))
                {
                    return true;
                }
                else if (str[i] == '\\')
                {
                    /* Escape-char: increment str-ptr and match on next char */
                    i++;
                    if (matchmetachar(c, str.Slice(i)))
                    {
                        return true;
                    }
                    else if ((c == str[i]) && !ismetachar(c))
                    {
                        return true;
                    }
                }
                else if (c == str[i])
                {
                    if (c == '-') // ???
                    {
                        return (i == str.Length); // ???
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            while (++i < str.Length);

            return false;
        }

        private bool matchone(regex_t p, char c)
        {
            switch (p.type)
            {
                case RegexElementType.DOT:            return true;
                case RegexElementType.CHAR_CLASS:     return  matchcharclass(c, p.ccl);
                case RegexElementType.INV_CHAR_CLASS: return !matchcharclass(c, p.ccl);
                case RegexElementType.DIGIT:          return  matchdigit(c);
                case RegexElementType.NOT_DIGIT:      return !matchdigit(c);
                case RegexElementType.ALPHA:          return  matchalphanum(c);
                case RegexElementType.NOT_ALPHA:      return !matchalphanum(c);
                case RegexElementType.WHITESPACE:     return  matchwhitespace(c);
                case RegexElementType.NOT_WHITESPACE: return !matchwhitespace(c);
                default:                              return  (p.ch == c);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool matchstar(regex_t p, ReadOnlySpan<regex_t> pattern, ReadOnlySpan<char> text, out int skip)
        {
            int i = 0;

            do
            {
                if (matchpattern(pattern, text.Slice(i), out skip))
                {
                    return true;
                }

                i += skip;
            }
            while (i < text.Length && matchone(p, text[i++]));

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool matchplus(regex_t p, ReadOnlySpan<regex_t> pattern, ReadOnlySpan<char> text, out int skip)
        {
            int i = 0;
            skip = 0;

            while (i < text.Length && matchone(p, text[i++]))
            {
                if (matchpattern(pattern, text.Slice(i), out skip)) // ??? i == Length
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool matchquestion(regex_t p, ReadOnlySpan<regex_t> pattern, ReadOnlySpan<char> text, out int skip)
        {
            skip = 0;
            if (p.type == RegexElementType.UNUSED || pattern[0].type == RegexElementType.UNUSED)
            {
                return true;
            }
            if (matchpattern(pattern, text, out skip))
            {
                return true;
            }
            if (!text.IsEmpty && matchone(p, text[0]))
            {
                return matchpattern(pattern, text.Slice(1), out skip);
            }

            return false;
        }


#if FALSE

/* Recursive matching */
static int matchpattern(ReadOnlySpan<regex_t> pattern, ReadOnlySpan<char> text)
{
  if ((pattern[0].type == UNUSED) || (pattern[1].type == QUESTIONMARK))
  {
    return matchquestion(pattern[1], &pattern[2], text);
  }
  else if (pattern[1].type == STAR)
  {
    return matchstar(pattern[0], &pattern[2], text);
  }
  else if (pattern[1].type == PLUS)
  {
    return matchplus(pattern[0], &pattern[2], text);
  }
  else if ((pattern[0].type == END) && pattern[1].type == UNUSED)
  {
    return text.IsEmpty;
  }
  else if (!text.IsEmpty && matchone(pattern[0], text[0]))
  {
    return matchpattern(&pattern[1], text.Slice(1));
  }
  else
  {
    return 0;
  }
}

#else

        /* Iterative matching */
        private bool matchpattern(ReadOnlySpan<regex_t> pattern, ReadOnlySpan<char> text, out int skip)
        {
            skip = 0;

            //if (text.IsEmpty)
            //{
            //    return false;
            //}

            int i = 0;
            int j = 0;
            do
            {
                if ((pattern[i].type == RegexElementType.UNUSED) || (pattern[i+1].type == RegexElementType.QUESTIONMARK))
                {
                    return matchquestion(pattern[i], pattern.Slice(i+2), text.Slice(j), out skip);
                }
                else if (pattern[i+1].type == RegexElementType.STAR)
                {
                    return matchstar(pattern[i], pattern.Slice(i+2), text.Slice(j), out skip);
                }
                else if (pattern[i+1].type == RegexElementType.PLUS)
                {
                    return matchplus(pattern[i], pattern.Slice(i+2), text.Slice(j), out skip);
                }
                else if ((pattern[i].type == RegexElementType.END) && pattern[i+1].type == RegexElementType.UNUSED)
                {
                    return ((j+1) == text.Length);
                }
            /*  Branching is not working properly
                else if (pattern[i+1].type == BRANCH)
                {
                return (matchpattern(pattern, text) || matchpattern(&pattern[2], text));
                }
            */
                if (j >= text.Length) return false;
                if (pattern[i].type == RegexElementType.CHAR && pattern[i].ch != text[j])
                {
                    skip = text.IndexOf(pattern[i].ch) - 1;

                    if (skip < 0)
                    {
                        skip = text.Length - 1;
                    }

                    return false;
                }
            }
            while ((j < text.Length) && matchone(pattern[i++], text[j++])); // ??? i < pattern.Length ???

            return false;
        }

#endif

    }
}
