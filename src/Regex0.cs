// Mini regex-module inspired by Rob Pike's regex code described in:
// http://www.cs.princeton.edu/courses/archive/spr09/cos333/beautiful.html
// Supports:
// ---------
//   '.'        Dot, matches any Unicode character
//   '^'        Start anchor, matches beginning of string
//   '$'        End anchor, matches end of string
//   '*'        Asterisk, match zero or more (greedy)
//   '+'        Plus, match one or more (greedy)
//   '?'        Question, match zero or one (non-greedy)
//   '[abc]'    Character class, match if one of {'a', 'b', 'c'}
//   '[^abc]'   Inverted class, match if NOT one of {'a', 'b', 'c'} -- NOTE: feature is currently broken!
//   '[a-zA-Z]' Character ranges, the character set of the ranges { a-z | A-Z }
//   '\s'       Whitespace, \t \f \r \n \v and spaces (including U+00a0 = NO-BREAK SPACE)
//   '\S'       Non-whitespace
//   '\w'       Alphanumeric, [a-zA-Z0-9_] including all Unicode digits and letters by category
//   '\W'       Non-alphanumeric
//   '\d'       Digits, only ASCII [0-9] (not all Unicode digits)
//   '\D'       Non-digits, negate of \d
//   '\r'       Return char
//   '\n'       New line char
//   '\t'       Tab char
//

using System;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.RegexLight
{

    // Definitions:


    enum RegexElementType { UNUSED, DOT, BEGIN, END, QUESTIONMARK, STAR, PLUS, CHAR, CHAR_CLASS, INV_CHAR_CLASS, DIGIT, NOT_DIGIT, ALPHA, NOT_ALPHA, WHITESPACE, NOT_WHITESPACE, /* BRANCH */ };

    struct regex_t
    {
        internal RegexElementType type;

        // The character itself
        internal char ch;

        // OR a pointer to characters in class
        internal (int start, int len) charClass;
    }

    public partial class RegexLight0
    {

        // Max number of regex symbols in expression.
        private const int MAX_REGEXP_OBJECTS = 30;

        // Max length of character-class buffer in.
        private const int MAX_CHAR_CLASS_LEN = 40;

        // Parsed regex pattern.
        private regex_t[] _compiledRegexPattern = new regex_t[MAX_REGEXP_OBJECTS];

        // Buffer for chars in all char-classes in the pattern.
        private char[] _charClassBuffer = new char[MAX_CHAR_CLASS_LEN];
        private int _charClassBufferIndex = 0;

        private bool _ignoreCase = false;

        public int Match(ReadOnlySpan<char> pattern, ReadOnlySpan<char> text)
        {
            return Match(CompileRegexPattern(pattern), text);
        }

        public int Match(ReadOnlySpan<char> pattern, ReadOnlySpan<char> text, bool ignoreCase)
        {
            _ignoreCase = ignoreCase;
            return Match(CompileRegexPattern(pattern), text);
        }

        private int Match(ReadOnlySpan<regex_t> pattern, ReadOnlySpan<char> text)
        {
            if (!pattern.IsEmpty && !text.IsEmpty)
            {
                if (pattern[0].type == RegexElementType.BEGIN)
                {
                    return ((MatchPattern(pattern.Slice(1), text, out int skip)) ? 0 : -1);
                }
                else
                {
                    int idx = 0;

                    do
                    {
                        if (MatchPattern(pattern, text.Slice(idx), out int skip))
                        {
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
        private regex_t[] CompileRegexPattern(ReadOnlySpan<char> pattern)
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
                // The minor optimization reduces size of generated code for the switch.
                ref regex_t regexType = ref _compiledRegexPattern[compiledIndex];
                currentChar = pattern[i];

                switch (currentChar)
                {
                    // Meta-characters:
                    case '^': {    regexType.type = RegexElementType.BEGIN;           } break;
                    case '$': {    regexType.type = RegexElementType.END;             } break;
                    case '.': {    regexType.type = RegexElementType.DOT;             } break;
                    case '*': {    regexType.type = RegexElementType.STAR;            } break;
                    case '+': {    regexType.type = RegexElementType.PLUS;            } break;
                    case '?': {    regexType.type = RegexElementType.QUESTIONMARK;    } break;
                    // case '|': {    _compiledRegexPattern[compiledIndex].type = RegexElementType.BRANCH;          } break; <-- not working properly

                    // Escaped character-classes (\s \w ...):
                    case '\\':
                        {
                            // Skip the escape-char '\\'
                            i++;

                            if (i < pattern.Length)
                            {
                                // The minor optimization reduces size of generated code for the switch.
                                ref regex_t regexType2 = ref _compiledRegexPattern[compiledIndex];

                                // ... and check the next
                                switch (pattern[i])
                                {
                                    // Meta-character:
                                    case 'd': {    regexType2.type = RegexElementType.DIGIT;            } break;
                                    case 'D': {    regexType2.type = RegexElementType.NOT_DIGIT;        } break;
                                    case 'w': {    regexType2.type = RegexElementType.ALPHA;            } break;
                                    case 'W': {    regexType2.type = RegexElementType.NOT_ALPHA;        } break;
                                    case 's': {    regexType2.type = RegexElementType.WHITESPACE;       } break;
                                    case 'S': {    regexType2.type = RegexElementType.NOT_WHITESPACE;   } break;

                                    // Escaped character:
                                    default:
                                    {
                                        regexType2.type = RegexElementType.CHAR;

                                        switch (pattern[i])
                                        {
                                            case 'n':
                                                {
                                                    regexType2.ch = '\n';
                                                }
                                                break;
                                            case 'r':
                                                {
                                                    regexType2.ch = '\r';
                                                }
                                                break;
                                            case 't':
                                                {
                                                    regexType2.ch = '\t';
                                                }
                                                break;
                                            default:
                                                {
                                                    regexType2.ch = _ignoreCase ? char.ToLowerInvariant(pattern[i]) : pattern[i];
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
                            _compiledRegexPattern[compiledIndex].type = CHAR;
                            _compiledRegexPattern[compiledIndex].ch = pattern[i];
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
                            int buf_begin = _charClassBufferIndex;

                            // Determine if negated.
                            if (pattern[i] == '^')
                            {
                                regexType.type = RegexElementType.INV_CHAR_CLASS;

                                i++;

                                if (i >= pattern.Length)
                                {
                                    return null;
                                }
                            }
                            else
                            {
                                regexType.type = RegexElementType.CHAR_CLASS;
                            }

                            // Copy characters inside [..] to char classbuffer.
                            char ch;
                            while ((ch = pattern[i]) != ']')
                            {
                                _charClassBuffer[_charClassBufferIndex++] = _ignoreCase ? char.ToLowerInvariant(ch) : ch;
                                i++;
                                if (i >= pattern.Length)
                                {
                                    return null;
                                }

                                switch (ch)
                                {
                                    case '-':
                                        {
                                            var c2 = (_ignoreCase ? char.ToLowerInvariant(pattern[i++]) : pattern[i++]);
                                            var c1 = _charClassBuffer[_charClassBufferIndex - 2];
                                            _charClassBuffer[_charClassBufferIndex++] = (char)(c2 - c1);
                                        }
                                        break;
                                    case '\\':
                                        {
                                            //_charClassBuffer[_charClassBufferIndex++] = _ignoreCase ? char.ToLowerInvariant(pattern[i++]) : pattern[i++];
                                            _charClassBuffer[_charClassBufferIndex++] = pattern[i++];
                                        }
                                        break;
                                }

                                if (i >= pattern.Length)
                                {
                                    return null;
                                }
                            }

                            regexType.charClass = (start: buf_begin, len: _charClassBufferIndex - buf_begin);
                        }

                        break;

                    // Other characters
                    default:
                        {
                            regexType.type = RegexElementType.CHAR;
                            regexType.ch = _ignoreCase ? char.ToLowerInvariant(currentChar) : currentChar;
                        } break;
                }

                i++;
                compiledIndex++;
            }

            // 'UNUSED' is a sentinel used to indicate end-of-pattern.
            _compiledRegexPattern[compiledIndex].type = RegexElementType.UNUSED;

            //re_print();

            return (regex_t[]) _compiledRegexPattern;
        }

        public void re_print()
        {
            int i;
            var pattern = _compiledRegexPattern;

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

                    var cclSpan = new ReadOnlySpan<char>(_charClassBuffer, pattern[i].charClass.start, pattern[i].charClass.len);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool MatchDigit(char c)
        {
            return (uint)(c - '0') <= (uint)('9' - '0');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool MatchAlpha(char c)
        {
            return char.IsLetter(c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool MatchWhitespace(char c)
        {
            return char.IsWhiteSpace(c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool MatchAlphaNum(char c)
        {
            return ((c == '_') || char.IsLetterOrDigit(c));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool MatchRange(char c, ReadOnlySpan<char> str)
        {
            return (str.Length >= 3) && (str[1] == '-') && ((uint)(c - str[0]) <= (uint)str[2]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsMetachar(char c)
        {
            return ((c == 's') || (c == 'd') || (c == 'w') || (c == 'S') || (c == 'D') || (c == 'W'));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool MatchMetachar(char c, char metaChar)
        {
            switch (metaChar)
            {
                case 'd': return  MatchDigit(c);
                case 's': return  MatchWhitespace(c);
                case 'w': return  MatchAlphaNum(c);
                case 'D': return !MatchDigit(c);
                case 'S': return !MatchWhitespace(c);
                case 'W': return !MatchAlphaNum(c);
                default:  return (c == metaChar);
            }
        }

        private bool MatchCharClass(char c, in (int start, int len) v)
        {
            int i = 0;

            ReadOnlySpan<char> str = new ReadOnlySpan<char>(_charClassBuffer, v.start, v.len);

            do
            {
                if (MatchRange(c, str))
                {
                    return true;
                }
                else if (str[i] == '\\')
                {
                    /* Escape-char: increment str-ptr and match on next char */
                    i++;
                    if (MatchMetachar(c, str[i]))
                    {
                        return true;
                    }
                    else if ((c == str[i]) && !IsMetachar(c))
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

        private bool MatchOneChar(in regex_t p, char c)
        {
            switch (p.type)
            {
                case RegexElementType.DOT:            return true;
                case RegexElementType.CHAR_CLASS:     return  MatchCharClass(_ignoreCase ? char.ToLowerInvariant(c) : c, p.charClass);
                case RegexElementType.INV_CHAR_CLASS: return !MatchCharClass(_ignoreCase ? char.ToLowerInvariant(c) : c, p.charClass);
                case RegexElementType.DIGIT:          return  MatchDigit(c);
                case RegexElementType.NOT_DIGIT:      return !MatchDigit(c);
                case RegexElementType.ALPHA:          return  MatchAlphaNum(c);
                case RegexElementType.NOT_ALPHA:      return !MatchAlphaNum(c);
                case RegexElementType.WHITESPACE:     return  MatchWhitespace(c);
                case RegexElementType.NOT_WHITESPACE: return !MatchWhitespace(c);
                default:                              return  (p.ch == (_ignoreCase ? char.ToLowerInvariant(c) : c));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool MatchStar(in regex_t p, ReadOnlySpan<regex_t> pattern, ReadOnlySpan<char> text, out int skip)
        {
            int i = 0;

            do
            {
                if (MatchPattern(pattern, text.Slice(i), out skip))
                {
                    return true;
                }

                i += skip;
            }
            while (i < text.Length && MatchOneChar(p, text[i++]));

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool MatchPlus(in regex_t p, ReadOnlySpan<regex_t> pattern, ReadOnlySpan<char> text, out int skip)
        {
            int i = 0;
            skip = 0;

            while (i < text.Length && MatchOneChar(p, text[i++]))
            {
                if (MatchPattern(pattern, text.Slice(i), out skip)) // ??? i == Length
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool MatchQuestion(in regex_t p, ReadOnlySpan<regex_t> pattern, ReadOnlySpan<char> text, out int skip)
        {
            skip = 0;
            if (p.type == RegexElementType.UNUSED || pattern[0].type == RegexElementType.UNUSED)
            {
                return true;
            }
            if (MatchPattern(pattern, text, out skip))
            {
                return true;
            }
            if (!text.IsEmpty && MatchOneChar(p, text[0]))
            {
                return MatchPattern(pattern, text.Slice(1), out skip);
            }

            return false;
        }


#if FALSE

/* Recursive matching */
static int MatchPattern(ReadOnlySpan<regex_t> pattern, ReadOnlySpan<char> text)
{
  if ((pattern[0].type == UNUSED) || (pattern[1].type == QUESTIONMARK))
  {
    return MatchQuestion(pattern[1], &pattern[2], text);
  }
  else if (pattern[1].type == STAR)
  {
    return MatchStar(pattern[0], &pattern[2], text);
  }
  else if (pattern[1].type == PLUS)
  {
    return MatchPlus(pattern[0], &pattern[2], text);
  }
  else if ((pattern[0].type == END) && pattern[1].type == UNUSED)
  {
    return text.IsEmpty;
  }
  else if (!text.IsEmpty && MatchOneChar(pattern[0], text[0]))
  {
    return MatchPattern(&pattern[1], text.Slice(1));
  }
  else
  {
    return 0;
  }
}

#else

        /* Iterative matching */
        private bool MatchPattern(ReadOnlySpan<regex_t> pattern, ReadOnlySpan<char> text, out int skip)
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
                    return MatchQuestion(pattern[i], pattern.Slice(i+2), text.Slice(j), out skip);
                }
                else if (pattern[i+1].type == RegexElementType.STAR)
                {
                    return MatchStar(pattern[i], pattern.Slice(i+2), text.Slice(j), out skip);
                }
                else if (pattern[i+1].type == RegexElementType.PLUS)
                {
                    return MatchPlus(pattern[i], pattern.Slice(i+2), text.Slice(j), out skip);
                }
                else if ((pattern[i].type == RegexElementType.END) && pattern[i+1].type == RegexElementType.UNUSED)
                {
                    return ((j+1) == text.Length);
                }
            /*  Branching is not working properly
                else if (pattern[i+1].type == BRANCH)
                {
                return (MatchPattern(pattern, text) || MatchPattern(&pattern[2], text));
                }
            */
                if (j >= text.Length) return false;

                if (_ignoreCase)
                {
                    if (pattern[i].type == RegexElementType.CHAR && pattern[i].ch != Char.ToLowerInvariant(text[j]))
                    {
                        //skip = text.IndexOf(pattern[i].ch, _ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) - 1;
                        skip = text.IndexOfAny(pattern[i].ch, Char.ToUpperInvariant(pattern[i].ch)) - 1;

                        if (skip < 0)
                        {
                            skip = text.Length - 1;
                        }

                        return false;
                    }
                }
                else
                {
                    if (pattern[i].type == RegexElementType.CHAR && pattern[i].ch != text[j])
                    {
                        //skip = text.IndexOf(pattern[i].ch, _ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) - 1;
                        skip = text.IndexOf(pattern[i].ch) - 1;

                        if (skip < 0)
                        {
                            skip = text.Length - 1;
                        }

                        return false;
                    }
                }
            }
            while ((j < text.Length) && MatchOneChar(pattern[i++], text[j++])); // ??? i < pattern.Length ???

            return false;
        }

#endif

    }
}
