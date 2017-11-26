﻿using ICU4N.Lang;
using ICU4N.Support;
using ICU4N.Support.Text;
using ICU4N.Text;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using StringBuffer = System.Text.StringBuilder;

namespace ICU4N.Impl
{
    public sealed partial class Utility
    {
        private static readonly char APOSTROPHE = '\'';
        private static readonly char BACKSLASH = '\\';
        private static readonly int MAGIC_UNSIGNED = unchecked((int)0x80000000);

        /**
         * Convenience utility to compare two Object[]s.
         * Ought to be in System
         */
        public static bool ArrayEquals(object[] source, object target)
        {
            if (source == null) return (target == null);
            if (!(target is object[])) return false;
            object[] targ = (object[])target;
            return (source.Length == targ.Length
                    && ArrayRegionMatches(source, 0, targ, 0, source.Length));
        }

        /**
         * Convenience utility to compare two int[]s
         * Ought to be in System
         */
        public static bool ArrayEquals(int[] source, object target)
        {
            if (source == null) return (target == null);
            if (!(target is int[])) return false;
            int[] targ = (int[])target;
            return (source.Length == targ.Length
                    && ArrayRegionMatches(source, 0, targ, 0, source.Length));
        }

        /**
         * Convenience utility to compare two double[]s
         * Ought to be in System
         */
        public static bool ArrayEquals(double[] source, object target)
        {
            if (source == null) return (target == null);
            if (!(target is double[])) return false;
            double[] targ = (double[])target;
            return (source.Length == targ.Length
                    && ArrayRegionMatches(source, 0, targ, 0, source.Length));
        }
        public static bool ArrayEquals(byte[] source, object target)
        {
            if (source == null) return (target == null);
            if (!(target is byte[])) return false;
            byte[] targ = (byte[])target;
            return (source.Length == targ.Length
                    && ArrayRegionMatches(source, 0, targ, 0, source.Length));
        }

        /**
         * Convenience utility to compare two Object[]s
         * Ought to be in System
         */
        public static bool ArrayEquals(object source, object target)
        {
            if (source == null) return (target == null);
            // for some reason, the correct arrayEquals is not being called
            // so do it by hand for now.
            if (source is Object[])
                return (ArrayEquals((Object[])source, target));
            if (source is int[])
                return (ArrayEquals((int[])source, target));
            if (source is double[])
                return (ArrayEquals((double[])source, target));
            if (source is byte[])
                return (ArrayEquals((byte[])source, target));
            return source.Equals(target);
        }

        /**
         * Convenience utility to compare two Object[]s
         * Ought to be in System.
         * @param len the length to compare.
         * The start indices and start+len must be valid.
         */
        public static bool ArrayRegionMatches(object[] source, int sourceStart,
                object[] target, int targetStart,
                int len)
        {
            int sourceEnd = sourceStart + len;
            int delta = targetStart - sourceStart;
            for (int i = sourceStart; i < sourceEnd; i++)
            {
                if (!ArrayEquals(source[i], target[i + delta]))
                    return false;
            }
            return true;
        }

        /**
         * Convenience utility to compare two Object[]s
         * Ought to be in System.
         * @param len the length to compare.
         * The start indices and start+len must be valid.
         */
        public static bool ArrayRegionMatches(char[] source, int sourceStart,
                char[] target, int targetStart,
                int len)
        {
            int sourceEnd = sourceStart + len;
            int delta = targetStart - sourceStart;
            for (int i = sourceStart; i < sourceEnd; i++)
            {
                if (source[i] != target[i + delta])
                    return false;
            }
            return true;
        }

        /**
         * Convenience utility to compare two int[]s.
         * @param len the length to compare.
         * The start indices and start+len must be valid.
         * Ought to be in System
         */
        public static bool ArrayRegionMatches(int[] source, int sourceStart,
                int[] target, int targetStart,
                int len)
        {
            int sourceEnd = sourceStart + len;
            int delta = targetStart - sourceStart;
            for (int i = sourceStart; i < sourceEnd; i++)
            {
                if (source[i] != target[i + delta])
                    return false;
            }
            return true;
        }

        /**
         * Convenience utility to compare two arrays of doubles.
         * @param len the length to compare.
         * The start indices and start+len must be valid.
         * Ought to be in System
         */
        public static bool ArrayRegionMatches(double[] source, int sourceStart,
                double[] target, int targetStart,
                int len)
        {
            int sourceEnd = sourceStart + len;
            int delta = targetStart - sourceStart;
            for (int i = sourceStart; i < sourceEnd; i++)
            {
                if (source[i] != target[i + delta])
                    return false;
            }
            return true;
        }
        public static bool ArrayRegionMatches(byte[] source, int sourceStart,
                byte[] target, int targetStart, int len)
        {
            int sourceEnd = sourceStart + len;
            int delta = targetStart - sourceStart;
            for (int i = sourceStart; i < sourceEnd; i++)
            {
                if (source[i] != target[i + delta])
                    return false;
            }
            return true;
        }

        /**
         * Trivial reference equality.
         * This method should help document that we really want == not equals(),
         * and to have a single place to suppress warnings from static analysis tools.
         */
        public static bool SameObjects(object a, object b)
        {
            return a == b;
        }

        /**
         * Convenience utility. Does null checks on objects, then calls equals.
         */
        public static bool ObjectEquals(object a, object b)
        {
            return a == null ?
                    b == null ? true : false :
                        b == null ? false : a.Equals(b);
        }

        // ICU4N specific - overload to ensure culture insensitive comparison when comparing strings
        /// <summary>
        /// Convenience utility. Does null checks on objects, then calls compare.
        /// </summary>
        public static int CheckCompare(string a, string b)
        {
            return a == null ?
                    b == null ? 0 : -1 :
                        b == null ? 1 : a.CompareToOrdinal(b);
        }

        // ICU4N specific - generic overload for comparing objects of known type
        /// <summary>
        /// Convenience utility. Does null checks on objects, then calls compare.
        /// </summary>
        public static int CheckCompare<T>(T a, T b) where T : IComparable<T>
        {
            return a == null ?
                    b == null ? 0 : -1 :
                        b == null ? 1 : a.CompareTo(b);
        }

        /// <summary>
        /// Convenience utility. Does null checks on objects, then calls compare.
        /// </summary>
        public static int CheckCompare(IComparable a, IComparable b)
        {
            return a == null ?
                    b == null ? 0 : -1 :
                        b == null ? 1 : a.CompareTo(b);
        }

        /**
         * Convenience utility. Does null checks on object, then calls hashCode.
         */
        public static int CheckHashCode(object a)
        {
            return a == null ? 0 : a.GetHashCode();
        }

        /**
         * The ESCAPE character is used during run-length encoding.  It signals
         * a run of identical chars.
         */
        private static readonly char ESCAPE = '\uA5A5';

        /**
         * The ESCAPE_BYTE character is used during run-length encoding.  It signals
         * a run of identical bytes.
         */
        static readonly byte ESCAPE_BYTE = (byte)0xA5;

        /**
         * Construct a string representing an int array.  Use run-length encoding.
         * A character represents itself, unless it is the ESCAPE character.  Then
         * the following notations are possible:
         *   ESCAPE ESCAPE   ESCAPE literal
         *   ESCAPE n c      n instances of character c
         * Since an encoded run occupies 3 characters, we only encode runs of 4 or
         * more characters.  Thus we have n > 0 and n != ESCAPE and n <= 0xFFFF.
         * If we encounter a run where n == ESCAPE, we represent this as:
         *   c ESCAPE n-1 c
         * The ESCAPE value is chosen so as not to collide with commonly
         * seen values.
         */
        static public string ArrayToRLEString(int[] a)
        {
            StringBuilder buffer = new StringBuilder();

            AppendInt32(buffer, a.Length);
            int runValue = a[0];
            int runLength = 1;
            for (int i = 1; i < a.Length; ++i)
            {
                int s = a[i];
                if (s == runValue && runLength < 0xFFFF)
                {
                    ++runLength;
                }
                else
                {
                    EncodeRun(buffer, runValue, runLength);
                    runValue = s;
                    runLength = 1;
                }
            }
            EncodeRun(buffer, runValue, runLength);
            return buffer.ToString();
        }

        /**
         * Construct a string representing a short array.  Use run-length encoding.
         * A character represents itself, unless it is the ESCAPE character.  Then
         * the following notations are possible:
         *   ESCAPE ESCAPE   ESCAPE literal
         *   ESCAPE n c      n instances of character c
         * Since an encoded run occupies 3 characters, we only encode runs of 4 or
         * more characters.  Thus we have n > 0 and n != ESCAPE and n <= 0xFFFF.
         * If we encounter a run where n == ESCAPE, we represent this as:
         *   c ESCAPE n-1 c
         * The ESCAPE value is chosen so as not to collide with commonly
         * seen values.
         */
        static public string ArrayToRLEString(short[] a)
        {
            StringBuilder buffer = new StringBuilder();
            // for (int i=0; i<a.length; ++i) buffer.append((char) a[i]);
            buffer.Append((char)(a.Length >> 16));
            buffer.Append((char)a.Length);
            short runValue = a[0];
            int runLength = 1;
            for (int i = 1; i < a.Length; ++i)
            {
                short s = a[i];
                if (s == runValue && runLength < 0xFFFF) ++runLength;
                else
                {
                    EncodeRun(buffer, runValue, runLength);
                    runValue = s;
                    runLength = 1;
                }
            }
            EncodeRun(buffer, runValue, runLength);
            return buffer.ToString();
        }

        /**
         * Construct a string representing a char array.  Use run-length encoding.
         * A character represents itself, unless it is the ESCAPE character.  Then
         * the following notations are possible:
         *   ESCAPE ESCAPE   ESCAPE literal
         *   ESCAPE n c      n instances of character c
         * Since an encoded run occupies 3 characters, we only encode runs of 4 or
         * more characters.  Thus we have n > 0 and n != ESCAPE and n <= 0xFFFF.
         * If we encounter a run where n == ESCAPE, we represent this as:
         *   c ESCAPE n-1 c
         * The ESCAPE value is chosen so as not to collide with commonly
         * seen values.
         */
        static public string ArrayToRLEString(char[] a)
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append((char)(a.Length >> 16));
            buffer.Append((char)a.Length);
            char runValue = a[0];
            int runLength = 1;
            for (int i = 1; i < a.Length; ++i)
            {
                char s = a[i];
                if (s == runValue && runLength < 0xFFFF) ++runLength;
                else
                {
                    EncodeRun(buffer, (short)runValue, runLength);
                    runValue = s;
                    runLength = 1;
                }
            }
            EncodeRun(buffer, (short)runValue, runLength);
            return buffer.ToString();
        }

        /**
         * Construct a string representing a byte array.  Use run-length encoding.
         * Two bytes are packed into a single char, with a single extra zero byte at
         * the end if needed.  A byte represents itself, unless it is the
         * ESCAPE_BYTE.  Then the following notations are possible:
         *   ESCAPE_BYTE ESCAPE_BYTE   ESCAPE_BYTE literal
         *   ESCAPE_BYTE n b           n instances of byte b
         * Since an encoded run occupies 3 bytes, we only encode runs of 4 or
         * more bytes.  Thus we have n > 0 and n != ESCAPE_BYTE and n <= 0xFF.
         * If we encounter a run where n == ESCAPE_BYTE, we represent this as:
         *   b ESCAPE_BYTE n-1 b
         * The ESCAPE_BYTE value is chosen so as not to collide with commonly
         * seen values.
         */
        static public string ArrayToRLEString(byte[] a)
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append((char)(a.Length >> 16));
            buffer.Append((char)a.Length);
            byte runValue = a[0];
            int runLength = 1;
            byte[] state = new byte[2];
            for (int i = 1; i < a.Length; ++i)
            {
                byte b = a[i];
                if (b == runValue && runLength < 0xFF) ++runLength;
                else
                {
                    EncodeRun(buffer, runValue, runLength, state);
                    runValue = b;
                    runLength = 1;
                }
            }
            EncodeRun(buffer, runValue, runLength, state);

            // We must save the final byte, if there is one, by padding
            // an extra zero.
            if (state[0] != 0) AppendEncodedByte(buffer, (byte)0, state);

            return buffer.ToString();
        }

        // ICU4N specific - EncodeRun(IAppendable buffer, int value, int length)
        //    moved to UtilityExtension.tt

        // ICU4N specific - AppendInt32(IAppendable buffer, int value)
        //    moved to UtilityExtension.tt

        // ICU4N specific - EncodeRun(IAppendable buffer, short value, int length)
        //    moved to UtilityExtension.tt

        // ICU4N specific - EncodeRun(IAppendable buffer, byte value, int length,
        //    byte[] state) moved to UtilityExtension.tt

        // ICU4N specific - AppendEncodedByte(IAppendable buffer, byte value,
        //    byte[] state) moved to UtilityExtension.tt

        /// <summary>
        /// Construct an array of <see cref="int"/>s from a run-length encoded <see cref="string"/>.
        /// </summary>
        static public int[] RLEStringToIntArray(string s)
        {
            int length = GetInt(s, 0);
            int[] array = new int[length];
            int ai = 0, i = 1;

            int maxI = s.Length / 2;
            while (ai < length && i < maxI)
            {
                int c = GetInt(s, i++);

                if (c == ESCAPE)
                {
                    c = GetInt(s, i++);
                    if (c == ESCAPE)
                    {
                        array[ai++] = c;
                    }
                    else
                    {
                        int runLength = c;
                        int runValue = GetInt(s, i++);
                        for (int j = 0; j < runLength; ++j)
                        {
                            array[ai++] = runValue;
                        }
                    }
                }
                else
                {
                    array[ai++] = c;
                }
            }

            if (ai != length || i != maxI)
            {
                throw new InvalidOperationException("Bad run-length encoded int array");
            }

            return array;
        }
        internal static int GetInt(string s, int i)
        {
            return ((s[2 * i]) << 16) | s[2 * i + 1];
        }

        /// <summary>
        /// Construct an array of <see cref="short"/>s from a run-length encoded <see cref="string"/>.
        /// </summary>
        static public short[] RLEStringToShortArray(string s)
        {
            int length = ((s[0]) << 16) | (s[1]);
            short[] array = new short[length];
            int ai = 0;
            for (int i = 2; i < s.Length; ++i)
            {
                char c = s[i];
                if (c == ESCAPE)
                {
                    c = s[++i];
                    if (c == ESCAPE)
                    {
                        array[ai++] = (short)c;
                    }
                    else
                    {
                        int runLength = c;
                        short runValue = (short)s[++i];
                        for (int j = 0; j < runLength; ++j) array[ai++] = runValue;
                    }
                }
                else
                {
                    array[ai++] = (short)c;
                }
            }

            if (ai != length)
                throw new InvalidOperationException("Bad run-length encoded short array");

            return array;
        }

        /// <summary>
        /// Construct an array of <see cref="char"/>s from a run-length encoded <see cref="string"/>.
        /// </summary>
        static public char[] RLEStringToCharArray(string s)
        {
            int length = ((s[0]) << 16) | (s[1]);
            char[] array = new char[length];
            int ai = 0;
            for (int i = 2; i < s.Length; ++i)
            {
                char c = s[i];
                if (c == ESCAPE)
                {
                    c = s[++i];
                    if (c == ESCAPE)
                    {
                        array[ai++] = c;
                    }
                    else
                    {
                        int runLength = c;
                        char runValue = s[++i];
                        for (int j = 0; j < runLength; ++j) array[ai++] = runValue;
                    }
                }
                else
                {
                    array[ai++] = c;
                }
            }

            if (ai != length)
                throw new InvalidOperationException("Bad run-length encoded short array");

            return array;
        }

        /// <summary>
        /// Construct an array of <see cref="byte"/>s from a run-length encoded <see cref="string"/>.
        /// </summary>
        static public byte[] RLEStringToByteArray(string s)
        {
            int length = ((s[0]) << 16) | (s[1]);
            byte[] array = new byte[length];
            bool nextChar = true;
            char c = (char)0;
            int node = 0;
            int runLength = 0;
            int i = 2;
            for (int ai = 0; ai < length;)
            {
                // This part of the loop places the next byte into the local
                // variable 'b' each time through the loop.  It keeps the
                // current character in 'c' and uses the boolean 'nextChar'
                // to see if we've taken both bytes out of 'c' yet.
                byte b;
                if (nextChar)
                {
                    c = s[i++];
                    b = (byte)(c >> 8);
                    nextChar = false;
                }
                else
                {
                    b = (byte)(c & 0xFF);
                    nextChar = true;
                }

                // This part of the loop is a tiny state machine which handles
                // the parsing of the run-length encoding.  This would be simpler
                // if we could look ahead, but we can't, so we use 'node' to
                // move between three nodes in the state machine.
                switch (node)
                {
                    case 0:
                        // Normal idle node
                        if (b == ESCAPE_BYTE)
                        {
                            node = 1;
                        }
                        else
                        {
                            array[ai++] = b;
                        }
                        break;
                    case 1:
                        // We have seen one ESCAPE_BYTE; we expect either a second
                        // one, or a run length and value.
                        if (b == ESCAPE_BYTE)
                        {
                            array[ai++] = ESCAPE_BYTE;
                            node = 0;
                        }
                        else
                        {
                            runLength = b;
                            // Interpret signed byte as unsigned
                            if (runLength < 0) runLength += 0x100;
                            node = 2;
                        }
                        break;
                    case 2:
                        // We have seen an ESCAPE_BYTE and length byte.  We interpret
                        // the next byte as the value to be repeated.
                        for (int j = 0; j < runLength; ++j) array[ai++] = b;
                        node = 0;
                        break;
                }
            }

            if (node != 0)
                throw new InvalidOperationException("Bad run-length encoded byte array");

            if (i != s.Length)
                throw new InvalidOperationException("Excess data in RLE byte array string");

            return array;
        }

        static public string LINE_SEPARATOR = Environment.NewLine;

        /**
         * Format a string for representation in a source file.  This includes
         * breaking it into lines and escaping characters using octal notation
         * when necessary (control characters and double quotes).
         */
        static public string FormatForSource(string s)
        {
            StringBuilder buffer = new StringBuilder();
            for (int i = 0; i < s.Length;)
            {
                if (i > 0) buffer.Append('+').Append(LINE_SEPARATOR);
                buffer.Append("        \"");
                int count = 11;
                while (i < s.Length && count < 80)
                {
                    char c = s[i++];
                    if (c < '\u0020' || c == '"' || c == '\\')
                    {
                        if (c == '\n')
                        {
                            buffer.Append("\\n");
                            count += 2;
                        }
                        else if (c == '\t')
                        {
                            buffer.Append("\\t");
                            count += 2;
                        }
                        else if (c == '\r')
                        {
                            buffer.Append("\\r");
                            count += 2;
                        }
                        else
                        {
                            // Represent control characters, backslash and double quote
                            // using octal notation; otherwise the string we form
                            // won't compile, since Unicode escape sequences are
                            // processed before tokenization.
                            //buffer.Append('\\');
                            //buffer.Append(HEX_DIGIT[(c & 0700) >> 6]); // HEX_DIGIT works for octal
                            //buffer.Append(HEX_DIGIT[(c & 0070) >> 3]);
                            //buffer.Append(HEX_DIGIT[(c & 0007)]);

                            // ICU4N specific - converted octal literals to decimal literals (.NET has no octal literals)
                            buffer.Append('\\');
                            buffer.Append(HEX_DIGIT[(c & 0448) >> 6]); // HEX_DIGIT works for octal
                            buffer.Append(HEX_DIGIT[(c & 0056) >> 3]);
                            buffer.Append(HEX_DIGIT[(c & 0007)]);

                            count += 4;
                        }
                    }
                    else if (c <= '\u007E')
                    {
                        buffer.Append(c);
                        count += 1;
                    }
                    else
                    {
                        buffer.Append("\\u");
                        buffer.Append(HEX_DIGIT[(c & 0xF000) >> 12]);
                        buffer.Append(HEX_DIGIT[(c & 0x0F00) >> 8]);
                        buffer.Append(HEX_DIGIT[(c & 0x00F0) >> 4]);
                        buffer.Append(HEX_DIGIT[(c & 0x000F)]);
                        count += 6;
                    }
                }
                buffer.Append('"');
            }
            return buffer.ToString();
        }

        internal static readonly char[] HEX_DIGIT = {'0','1','2','3','4','5','6','7',
            '8','9','A','B','C','D','E','F'};

        /**
         * Format a string for representation in a source file.  Like
         * formatForSource but does not do line breaking.
         */
        static public string Format1ForSource(string s)
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("\"");
            for (int i = 0; i < s.Length;)
            {
                char c = s[i++];
                if (c < '\u0020' || c == '"' || c == '\\')
                {
                    if (c == '\n')
                    {
                        buffer.Append("\\n");
                    }
                    else if (c == '\t')
                    {
                        buffer.Append("\\t");
                    }
                    else if (c == '\r')
                    {
                        buffer.Append("\\r");
                    }
                    else
                    {
                        // Represent control characters, backslash and double quote
                        // using octal notation; otherwise the string we form
                        // won't compile, since Unicode escape sequences are
                        // processed before tokenization.
                        //buffer.Append('\\');
                        //buffer.Append(HEX_DIGIT[(c & 0700) >> 6]); // HEX_DIGIT works for octal
                        //buffer.Append(HEX_DIGIT[(c & 0070) >> 3]);
                        //buffer.Append(HEX_DIGIT[(c & 0007)]);

                        // ICU4N specific - converted octal literals to decimal literals (.NET has no octal literals)
                        buffer.Append('\\');
                        buffer.Append(HEX_DIGIT[(c & 0448) >> 6]); // HEX_DIGIT works for octal
                        buffer.Append(HEX_DIGIT[(c & 0056) >> 3]);
                        buffer.Append(HEX_DIGIT[(c & 0007)]);
                    }
                }
                else if (c <= '\u007E')
                {
                    buffer.Append(c);
                }
                else
                {
                    buffer.Append("\\u");
                    buffer.Append(HEX_DIGIT[(c & 0xF000) >> 12]);
                    buffer.Append(HEX_DIGIT[(c & 0x0F00) >> 8]);
                    buffer.Append(HEX_DIGIT[(c & 0x00F0) >> 4]);
                    buffer.Append(HEX_DIGIT[(c & 0x000F)]);
                }
            }
            buffer.Append('"');
            return buffer.ToString();
        }

        /**
         * Convert characters outside the range U+0020 to U+007F to
         * Unicode escapes, and convert backslash to a double backslash.
         */
        public static string Escape(string s)
        {
            StringBuilder buf = new StringBuilder();
            for (int i = 0; i < s.Length;)
            {
                int c = Character.CodePointAt(s, i);
                i += UTF16.GetCharCount(c);
                if (c >= ' ' && c <= 0x007F)
                {
                    if (c == '\\')
                    {
                        buf.Append("\\\\"); // That is, "\\"
                    }
                    else
                    {
                        buf.Append((char)c);
                    }
                }
                else
                {
                    bool four = c <= 0xFFFF;
                    buf.Append(four ? "\\u" : "\\U");
                    buf.Append(Hex(c, four ? 4 : 8));
                }
            }
            return buf.ToString();
        }

        /* This map must be in ASCENDING ORDER OF THE ESCAPE CODE */
        static private readonly char[] UNESCAPE_MAP = {
            /*"   (char)0x22, (char)0x22 */
            /*'   (char)0x27, (char)0x27 */
            /*?   (char)0x3F, (char)0x3F */
            /*\   (char)0x5C, (char)0x5C */
            /*a*/ (char)0x61, (char)0x07,
            /*b*/ (char)0x62, (char)0x08,
            /*e*/ (char)0x65, (char)0x1b,
            /*f*/ (char)0x66, (char)0x0c,
            /*n*/ (char)0x6E, (char)0x0a,
            /*r*/ (char)0x72, (char)0x0d,
            /*t*/ (char)0x74, (char)0x09,
            /*v*/ (char)0x76, (char)0x0b
        };

        /**
         * Convert an escape to a 32-bit code point value.  We attempt
         * to parallel the icu4c unescapeAt() function.
         * @param offset16 an array containing offset to the character
         * <em>after</em> the backslash.  Upon return offset16[0] will
         * be updated to point after the escape sequence.
         * @return character value from 0 to 10FFFF, or -1 on error.
         */
        public static int UnescapeAt(string s, int[] offset16)
        {
            int c;
            int result = 0;
            int n = 0;
            int minDig = 0;
            int maxDig = 0;
            int bitsPerDigit = 4;
            int dig;
            int i;
            bool braces = false;

            /* Check that offset is in range */
            int offset = offset16[0];
            int length = s.Length;
            if (offset < 0 || offset >= length)
            {
                return -1;
            }

            /* Fetch first UChar after '\\' */
            c = Character.CodePointAt(s, offset);
            offset += UTF16.GetCharCount(c);

            /* Convert hexadecimal and octal escapes */
            switch (c)
            {
                case 'u':
                    minDig = maxDig = 4;
                    break;
                case 'U':
                    minDig = maxDig = 8;
                    break;
                case 'x':
                    minDig = 1;
                    if (offset < length && UTF16.CharAt(s, offset) == 0x7B /*{*/)
                    {
                        ++offset;
                        braces = true;
                        maxDig = 8;
                    }
                    else
                    {
                        maxDig = 2;
                    }
                    break;
                default:
                    dig = UCharacter.Digit(c, 8);
                    if (dig >= 0)
                    {
                        minDig = 1;
                        maxDig = 3;
                        n = 1; /* Already have first octal digit */
                        bitsPerDigit = 3;
                        result = dig;
                    }
                    break;
            }
            if (minDig != 0)
            {
                while (offset < length && n < maxDig)
                {
                    c = UTF16.CharAt(s, offset);
                    dig = UCharacter.Digit(c, (bitsPerDigit == 3) ? 8 : 16);
                    if (dig < 0)
                    {
                        break;
                    }
                    result = (result << bitsPerDigit) | dig;
                    offset += UTF16.GetCharCount(c);
                    ++n;
                }
                if (n < minDig)
                {
                    return -1;
                }
                if (braces)
                {
                    if (c != 0x7D /*}*/)
                    {
                        return -1;
                    }
                    ++offset;
                }
                if (result < 0 || result >= 0x110000)
                {
                    return -1;
                }
                // If an escape sequence specifies a lead surrogate, see
                // if there is a trail surrogate after it, either as an
                // escape or as a literal.  If so, join them up into a
                // supplementary.
                if (offset < length &&
                        UTF16.IsLeadSurrogate((char)result))
                {
                    int ahead = offset + 1;
                    c = s[offset]; // [sic] get 16-bit code unit
                    if (c == '\\' && ahead < length)
                    {
                        int[] o = new int[] { ahead };
                        c = UnescapeAt(s, o);
                        ahead = o[0];
                    }
                    if (UTF16.IsTrailSurrogate((char)c))
                    {
                        offset = ahead;
                        result = Character.ToCodePoint((char)result, (char)c);
                    }
                }
                offset16[0] = offset;
                return result;
            }

            /* Convert C-style escapes in table */
            for (i = 0; i < UNESCAPE_MAP.Length; i += 2)
            {
                if (c == UNESCAPE_MAP[i])
                {
                    offset16[0] = offset;
                    return UNESCAPE_MAP[i + 1];
                }
                else if (c < UNESCAPE_MAP[i])
                {
                    break;
                }
            }

            /* Map \cX to control-X: X & 0x1F */
            if (c == 'c' && offset < length)
            {
                c = UTF16.CharAt(s, offset);
                offset16[0] = offset + UTF16.GetCharCount(c);
                return 0x1F & c;
            }

            /* If no special forms are recognized, then consider
             * the backslash to generically escape the next character. */
            offset16[0] = offset;
            return c;
        }

        /**
         * Convert all escapes in a given string using unescapeAt().
         * @exception IllegalArgumentException if an invalid escape is
         * seen.
         */
        public static string Unescape(string s)
        {
            StringBuilder buf = new StringBuilder();
            int[] pos = new int[1];
            for (int i = 0; i < s.Length;)
            {
                char c = s[i++];
                if (c == '\\')
                {
                    pos[0] = i;
                    int e = UnescapeAt(s, pos);
                    if (e < 0)
                    {
                        throw new ArgumentException("Invalid escape sequence " +
                                s.Substring(i - 1, Math.Min(i + 8, s.Length) - (i - 1)));
                    }
                    buf.AppendCodePoint(e);
                    i = pos[0];
                }
                else
                {
                    buf.Append(c);
                }
            }
            return buf.ToString();
        }

        /**
         * Convert all escapes in a given string using unescapeAt().
         * Leave invalid escape sequences unchanged.
         */
        public static string UnescapeLeniently(string s)
        {
            StringBuilder buf = new StringBuilder();
            int[] pos = new int[1];
            for (int i = 0; i < s.Length;)
            {
                char c = s[i++];
                if (c == '\\')
                {
                    pos[0] = i;
                    int e = UnescapeAt(s, pos);
                    if (e < 0)
                    {
                        buf.Append(c);
                    }
                    else
                    {
                        buf.AppendCodePoint(e);
                        i = pos[0];
                    }
                }
                else
                {
                    buf.Append(c);
                }
            }
            return buf.ToString();
        }

        /**
         * Convert a char to 4 hex uppercase digits.  E.g., hex('a') =>
         * "0041".
         */
        public static string Hex(long ch)
        {
            return Hex(ch, 4);
        }

        /**
         * Supplies a zero-padded hex representation of an integer (without 0x)
         */
        static public string Hex(long i, int places)
        {
            if (i == long.MinValue) return "-8000000000000000";
            bool negative = i < 0;
            if (negative)
            {
                i = -i;
            }
            //string result = Long.toString(i, 16).toUpperCase(Locale.ENGLISH);
            string result = i.ToString("X", CultureInfo.InvariantCulture);
            if (result.Length < places)
            {
                result = "0000000000000000".Substring(result.Length, places - result.Length) + result; // ICU4N: Corrected 2nd parameter
            }
            if (negative)
            {
                return '-' + result;
            }
            return result;
        }

        // ICU4N specific - Hex(ICharSequence s) moved to UtilityExtension.tt

        // ICU4N specific - Hex(ICharSequence s, int width, ICharSequence separator, bool useCodePoints, 
        //      StringBuilder result) moved to UtilityExtension.tt

        // ICU4N specific - Hex(ICharSequence s, int width, ICharSequence separator) moved to UtilityExtension.tt

        /**
         * Split a string into pieces based on the given divider character
         * @param s the string to split
         * @param divider the character on which to split.  Occurrences of
         * this character are not included in the output
         * @param output an array to receive the substrings between
         * instances of divider.  It must be large enough on entry to
         * accomodate all output.  Adjacent instances of the divider
         * character will place empty strings into output.  Before
         * returning, output is padded out with empty strings.
         */
        public static void Split(string s, char divider, string[] output)
        {
            int last = 0;
            int current = 0;
            int i;
            for (i = 0; i < s.Length; ++i)
            {
                if (s[i] == divider)
                {
                    output[current++] = s.Substring(last, i - last);
                    last = i + 1;
                }
            }
            output[current++] = s.Substring(last, i - last);
            while (current < output.Length)
            {
                output[current++] = "";
            }
        }

        /**
         * Split a string into pieces based on the given divider character
         * @param s the string to split
         * @param divider the character on which to split.  Occurrences of
         * this character are not included in the output
         * @return output an array to receive the substrings between
         * instances of divider. Adjacent instances of the divider
         * character will place empty strings into output.
         */
        public static string[] Split(string s, char divider)
        {
            int last = 0;
            int i;
            List<string> output = new List<string>();
            for (i = 0; i < s.Length; ++i)
            {
                if (s[i] == divider)
                {
                    output.Add(s.Substring(last, i - last));
                    last = i + 1;
                }
            }
            output.Add(s.Substring(last, i - last));
            return output.ToArray();
        }

        /**
         * Look up a given string in a string array.  Returns the index at
         * which the first occurrence of the string was found in the
         * array, or -1 if it was not found.
         * @param source the string to search for
         * @param target the array of zero or more strings in which to
         * look for source
         * @return the index of target at which source first occurs, or -1
         * if not found
         */
        public static int Lookup(string source, string[] target)
        {
            for (int i = 0; i < target.Length; ++i)
            {
                if (source.Equals(target[i])) return i;
            }
            return -1;
        }

        /**
         * Parse a single non-whitespace character 'ch', optionally
         * preceded by whitespace.
         * @param id the string to be parsed
         * @param pos INPUT-OUTPUT parameter.  On input, pos[0] is the
         * offset of the first character to be parsed.  On output, pos[0]
         * is the index after the last parsed character.  If the parse
         * fails, pos[0] will be unchanged.
         * @param ch the non-whitespace character to be parsed.
         * @return true if 'ch' is seen preceded by zero or more
         * whitespace characters.
         */
        public static bool ParseChar(string id, int[] pos, char ch)
        {
            int start = pos[0];
            pos[0] = PatternProps.SkipWhiteSpace(id, pos[0]);
            if (pos[0] == id.Length ||
                    id[pos[0]] != ch)
            {
                pos[0] = start;
                return false;
            }
            ++pos[0];
            return true;
        }

        /**
         * Parse a pattern string starting at offset pos.  Keywords are
         * matched case-insensitively.  Spaces may be skipped and may be
         * optional or required.  Integer values may be parsed, and if
         * they are, they will be returned in the given array.  If
         * successful, the offset of the next non-space character is
         * returned.  On failure, -1 is returned.
         * @param pattern must only contain lowercase characters, which
         * will match their uppercase equivalents as well.  A space
         * character matches one or more required spaces.  A '~' character
         * matches zero or more optional spaces.  A '#' character matches
         * an integer and stores it in parsedInts, which the caller must
         * ensure has enough capacity.
         * @param parsedInts array to receive parsed integers.  Caller
         * must ensure that parsedInts.length is >= the number of '#'
         * signs in 'pattern'.
         * @return the position after the last character parsed, or -1 if
         * the parse failed
         */
        public static int ParsePattern(string rule, int pos, int limit,
                string pattern, int[] parsedInts)
        {
            // TODO Update this to handle surrogates
            int[] p = new int[1];
            int intCount = 0; // number of integers parsed
            for (int i = 0; i < pattern.Length; ++i)
            {
                char cpat = pattern[i];
                char c;
                switch (cpat)
                {
                    case ' ':
                        if (pos >= limit)
                        {
                            return -1;
                        }
                        c = rule[pos++];
                        if (!PatternProps.IsWhiteSpace(c))
                        {
                            return -1;
                        }
                        // FALL THROUGH to skipWhitespace
                        pos = PatternProps.SkipWhiteSpace(rule, pos);
                        break;
                    case '~':
                        pos = PatternProps.SkipWhiteSpace(rule, pos);
                        break;
                    case '#':
                        p[0] = pos;
                        parsedInts[intCount++] = ParseInteger(rule, p, limit);
                        if (p[0] == pos)
                        {
                            // Syntax error; failed to parse integer
                            return -1;
                        }
                        pos = p[0];
                        break;
                    default:
                        if (pos >= limit)
                        {
                            return -1;
                        }
                        c = (char)UCharacter.ToLower(rule[pos++]);
                        if (c != cpat)
                        {
                            return -1;
                        }
                        break;
                }
            }
            return pos;
        }

        /**
         * Parse a pattern string within the given Replaceable and a parsing
         * pattern.  Characters are matched literally and case-sensitively
         * except for the following special characters:
         *
         * ~  zero or more Pattern_White_Space chars
         *
         * If end of pattern is reached with all matches along the way,
         * pos is advanced to the first unparsed index and returned.
         * Otherwise -1 is returned.
         * @param pat pattern that controls parsing
         * @param text text to be parsed, starting at index
         * @param index offset to first character to parse
         * @param limit offset after last character to parse
         * @return index after last parsed character, or -1 on parse failure.
         */
        public static int ParsePattern(string pat,
                IReplaceable text,
                int index,
                int limit)
        {
            int ipat = 0;

            // empty pattern matches immediately
            if (ipat == pat.Length)
            {
                return index;
            }

            int cpat = Character.CodePointAt(pat, ipat);

            while (index < limit)
            {
                int c = text.Char32At(index);

                // parse \s*
                if (cpat == '~')
                {
                    if (PatternProps.IsWhiteSpace(c))
                    {
                        index += UTF16.GetCharCount(c);
                        continue;
                    }
                    else
                    {
                        if (++ipat == pat.Length)
                        {
                            return index; // success; c unparsed
                        }
                        // fall thru; process c again with next cpat
                    }
                }

                // parse literal
                else if (c == cpat)
                {
                    int n = UTF16.GetCharCount(c);
                    index += n;
                    ipat += n;
                    if (ipat == pat.Length)
                    {
                        return index; // success; c parsed
                    }
                    // fall thru; get next cpat
                }

                // match failure of literal
                else
                {
                    return -1;
                }

                cpat = UTF16.CharAt(pat, ipat);
            }

            return -1; // text ended before end of pat
        }

        /**
         * Parse an integer at pos, either of the form \d+ or of the form
         * 0x[0-9A-Fa-f]+ or 0[0-7]+, that is, in standard decimal, hex,
         * or octal format.
         * @param pos INPUT-OUTPUT parameter.  On input, the first
         * character to parse.  On output, the character after the last
         * parsed character.
         */
        public static int ParseInteger(string rule, int[] pos, int limit)
        {
            int count = 0;
            int value = 0;
            int p = pos[0];
            int radix = 10;

            if (rule.RegionMatches(/*true,*/ p, "0x", 0, 2))
            {
                p += 2;
                radix = 16;
            }
            else if (p < limit && rule[p] == '0')
            {
                p++;
                count = 1;
                radix = 8;
            }

            while (p < limit)
            {
                int d = UCharacter.Digit(rule[p++], radix);
                if (d < 0)
                {
                    --p;
                    break;
                }
                ++count;
                int v = (value * radix) + d;
                if (v <= value)
                {
                    // If there are too many input digits, at some point
                    // the value will go negative, e.g., if we have seen
                    // "0x8000000" already and there is another '0', when
                    // we parse the next 0 the value will go negative.
                    return 0;
                }
                value = v;
            }
            if (count > 0)
            {
                pos[0] = p;
            }
            return value;
        }

        /**
         * Parse a Unicode identifier from the given string at the given
         * position.  Return the identifier, or null if there is no
         * identifier.
         * @param str the string to parse
         * @param pos INPUT-OUPUT parameter.  On INPUT, pos[0] is the
         * first character to examine.  It must be less than str.length(),
         * and it must not point to a whitespace character.  That is, must
         * have pos[0] &lt; str.length().  On
         * OUTPUT, the position after the last parsed character.
         * @return the Unicode identifier, or null if there is no valid
         * identifier at pos[0].
         */
        public static string ParseUnicodeIdentifier(string str, int[] pos)
        {
            // assert(pos[0] < str.length());
            StringBuilder buf = new StringBuilder();
            int p = pos[0];
            while (p < str.Length)
            {
                int ch = Character.CodePointAt(str, p);
                if (buf.Length == 0)
                {
                    if (UCharacter.IsUnicodeIdentifierStart(ch))
                    {
                        buf.AppendCodePoint(ch);
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    if (UCharacter.IsUnicodeIdentifierPart(ch))
                    {
                        buf.AppendCodePoint(ch);
                    }
                    else
                    {
                        break;
                    }
                }
                p += UTF16.GetCharCount(ch);
            }
            pos[0] = p;
            return buf.ToString();
        }

        internal static readonly char[] DIGITS = {
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J',
            'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T',
            'U', 'V', 'W', 'X', 'Y', 'Z'
        };

        // ICU4N specific - RecursiveAppendNumber(IAppendable result, int n,
        //    int radix, int minDigits) moved to UtilityExtension.tt

        // ICU4N specific - AppendNumber(T result, int n,
        //    int radix, int minDigits) where T : IAppendable moved to UtilityExtension.tt

        /**
         * Parse an unsigned 31-bit integer at the given offset.  Use
         * UCharacter.digit() to parse individual characters into digits.
         * @param text the text to be parsed
         * @param pos INPUT-OUTPUT parameter.  On entry, pos[0] is the
         * offset within text at which to start parsing; it should point
         * to a valid digit.  On exit, pos[0] is the offset after the last
         * parsed character.  If the parse failed, it will be unchanged on
         * exit.  Must be >= 0 on entry.
         * @param radix the radix in which to parse; must be >= 2 and &lt;=
         * 36.
         * @return a non-negative parsed number, or -1 upon parse failure.
         * Parse fails if there are no digits, that is, if pos[0] does not
         * point to a valid digit on entry, or if the number to be parsed
         * does not fit into a 31-bit unsigned integer.
         */
        public static int ParseNumber(string text, int[] pos, int radix)
        {
            // assert(pos[0] >= 0);
            // assert(radix >= 2);
            // assert(radix <= 36);
            int n = 0;
            int p = pos[0];
            while (p < text.Length)
            {
                int ch = Character.CodePointAt(text, p);
                int d = UCharacter.Digit(ch, radix);
                if (d < 0)
                {
                    break;
                }
                n = radix * n + d;
                // ASSUME that when a 32-bit integer overflows it becomes
                // negative.  E.g., 214748364 * 10 + 8 => negative value.
                if (n < 0)
                {
                    return -1;
                }
                ++p;
            }
            if (p == pos[0])
            {
                return -1;
            }
            pos[0] = p;
            return n;
        }

        /**
         * Return true if the character is NOT printable ASCII.  The tab,
         * newline and linefeed characters are considered unprintable.
         */
        public static bool IsUnprintable(int c)
        {
            //0x20 = 32 and 0x7E = 126
            return !(c >= 0x20 && c <= 0x7E);
        }

        // ICU4N specific - EscapeUnprintable(IAppendable result, int c)
        //    moved to UtilityExtension.tt


        /**
         * Returns the index of the first character in a set, ignoring quoted text.
         * For example, in the string "abc'hide'h", the 'h' in "hide" will not be
         * found by a search for "h".  Unlike string.indexOf(), this method searches
         * not for a single character, but for any character of the string
         * <code>setOfChars</code>.
         * @param text text to be searched
         * @param start the beginning index, inclusive; <code>0 <= start
         * <= limit</code>.
         * @param limit the ending index, exclusive; <code>start <= limit
         * <= text.length()</code>.
         * @param setOfChars string with one or more distinct characters
         * @return Offset of the first character in <code>setOfChars</code>
         * found, or -1 if not found.
         * @see string#indexOf
         */
        public static int QuotedIndexOf(string text, int start, int limit,
                string setOfChars)
        {
            for (int i = start; i < limit; ++i)
            {
                char c = text[i];
                if (c == BACKSLASH)
                {
                    ++i;
                }
                else if (c == APOSTROPHE)
                {
                    while (++i < limit
                            && text[i] != APOSTROPHE) { }
                }
                else if (setOfChars[c] >= 0)
                {
                    return i;
                }
            }
            return -1;
        }

        /**
         * Append a character to a rule that is being built up.  To flush
         * the quoteBuf to rule, make one final call with isLiteral == true.
         * If there is no final character, pass in (int)-1 as c.
         * @param rule the string to append the character to
         * @param c the character to append, or (int)-1 if none.
         * @param isLiteral if true, then the given character should not be
         * quoted or escaped.  Usually this means it is a syntactic element
         * such as > or $
         * @param escapeUnprintable if true, then unprintable characters
         * should be escaped using escapeUnprintable().  These escapes will
         * appear outside of quotes.
         * @param quoteBuf a buffer which is used to build up quoted
         * substrings.  The caller should initially supply an empty buffer,
         * and thereafter should not modify the buffer.  The buffer should be
         * cleared out by, at the end, calling this method with a literal
         * character (which may be -1).
         */
        public static void AppendToRule(StringBuffer rule,
                int c,
                bool isLiteral,
                bool escapeUnprintable,
                StringBuffer quoteBuf)
        {
            // If we are escaping unprintables, then escape them outside
            // quotes.  \\u and \\U are not recognized within quotes.  The same
            // logic applies to literals, but literals are never escaped.
            if (isLiteral ||
                    (escapeUnprintable && Utility.IsUnprintable(c)))
            {
                if (quoteBuf.Length > 0)
                {
                    // We prefer backslash APOSTROPHE to double APOSTROPHE
                    // (more readable, less similar to ") so if there are
                    // double APOSTROPHEs at the ends, we pull them outside
                    // of the quote.

                    // If the first thing in the quoteBuf is APOSTROPHE
                    // (doubled) then pull it out.
                    while (quoteBuf.Length >= 2 &&
                            quoteBuf[0] == APOSTROPHE &&
                            quoteBuf[1] == APOSTROPHE)
                    {
                        rule.Append(BACKSLASH).Append(APOSTROPHE);
                        quoteBuf.Delete(0, 2);
                    }
                    // If the last thing in the quoteBuf is APOSTROPHE
                    // (doubled) then remove and count it and add it after.
                    int trailingCount = 0;
                    while (quoteBuf.Length >= 2 &&
                            quoteBuf[quoteBuf.Length - 2] == APOSTROPHE &&
                            quoteBuf[quoteBuf.Length - 1] == APOSTROPHE)
                    {
                        quoteBuf.Length = quoteBuf.Length - 2;
                        ++trailingCount;
                    }
                    if (quoteBuf.Length > 0)
                    {
                        rule.Append(APOSTROPHE);
                        rule.Append(quoteBuf);
                        rule.Append(APOSTROPHE);
                        quoteBuf.Length = 0;
                    }
                    while (trailingCount-- > 0)
                    {
                        rule.Append(BACKSLASH).Append(APOSTROPHE);
                    }
                }
                if (c != -1)
                {
                    /* Since spaces are ignored during parsing, they are
                     * emitted only for readability.  We emit one here
                     * only if there isn't already one at the end of the
                     * rule.
                     */
                    if (c == ' ')
                    {
                        int len = rule.Length;
                        if (len > 0 && rule[len - 1] != ' ')
                        {
                            rule.Append(' ');
                        }
                    }
                    else if (!escapeUnprintable || !Utility.EscapeUnprintable(rule, c))
                    {
                        rule.AppendCodePoint(c);
                    }
                }
            }

            // Escape ' and '\' and don't begin a quote just for them
            else if (quoteBuf.Length == 0 &&
                    (c == APOSTROPHE || c == BACKSLASH))
            {
                rule.Append(BACKSLASH).Append((char)c);
            }

            // Specials (printable ascii that isn't [0-9a-zA-Z]) and
            // whitespace need quoting.  Also append stuff to quotes if we are
            // building up a quoted substring already.
            else if (quoteBuf.Length > 0 ||
                    (c >= 0x0021 && c <= 0x007E &&
                            !((c >= 0x0030/*'0'*/ && c <= 0x0039/*'9'*/) ||
                                    (c >= 0x0041/*'A'*/ && c <= 0x005A/*'Z'*/) ||
                                    (c >= 0x0061/*'a'*/ && c <= 0x007A/*'z'*/))) ||
                                    PatternProps.IsWhiteSpace(c))
            {
                quoteBuf.AppendCodePoint(c);
                // Double ' within a quote
                if (c == APOSTROPHE)
                {
                    quoteBuf.Append((char)c);
                }
            }

            // Otherwise just append
            else
            {
                rule.AppendCodePoint(c);
            }
        }

        /**
         * Append the given string to the rule.  Calls the single-character
         * version of appendToRule for each character.
         */
        public static void AppendToRule(StringBuffer rule,
                string text,
                bool isLiteral,
                bool escapeUnprintable,
                StringBuffer quoteBuf)
        {
            for (int i = 0; i < text.Length; ++i)
            {
                // Okay to process in 16-bit code units here
                AppendToRule(rule, text[i], isLiteral, escapeUnprintable, quoteBuf);
            }
        }

        /**
         * Given a matcher reference, which may be null, append its
         * pattern as a literal to the given rule.
         */
        public static void AppendToRule(StringBuffer rule,
                IUnicodeMatcher matcher,
                bool escapeUnprintable,
                StringBuffer quoteBuf)
        {
            if (matcher != null)
            {
                AppendToRule(rule, matcher.ToString(),
                        true, escapeUnprintable, quoteBuf);
            }
        }

        /**
         * Compares 2 unsigned integers
         * @param source 32 bit unsigned integer
         * @param target 32 bit unsigned integer
         * @return 0 if equals, 1 if source is greater than target and -1
         *         otherwise
         */
        public static int CompareUnsigned(int source, int target)
        {
            source += MAGIC_UNSIGNED;
            target += MAGIC_UNSIGNED;
            if (source < target)
            {
                return -1;
            }
            else if (source > target)
            {
                return 1;
            }
            return 0;
        }

        /**
         * Find the highest bit in a positive integer. This is done
         * by doing a binary search through the bits.
         *
         * @param n is the integer
         *
         * @return the bit number of the highest bit, with 0 being
         * the low order bit, or -1 if <code>n</code> is not positive
         */
        public static byte HighBit(int n) // ICU4N NOTE: Returning byte means no negative results. Be sure to cast to sbyte for usage.
        {
            if (n <= 0)
            {
                return unchecked((byte)-1);
            }

            byte bit = 0;

            if (n >= 1 << 16)
            {
                n >>= 16;
                bit += 16;
            }

            if (n >= 1 << 8)
            {
                n >>= 8;
                bit += 8;
            }

            if (n >= 1 << 4)
            {
                n >>= 4;
                bit += 4;
            }

            if (n >= 1 << 2)
            {
                n >>= 2;
                bit += 2;
            }

            if (n >= 1 << 1)
            {
                n >>= 1;
                bit += 1;
            }

            return bit;
        }
        /**
         * Utility method to take a int[] containing codepoints and return
         * a string representation with code units.
         */
        public static string ValueOf(int[] source)
        {
            // TODO: Investigate why this method is not on UTF16 class
            StringBuilder result = new StringBuilder(source.Length);
            for (int i = 0; i < source.Length; i++)
            {
                result.AppendCodePoint(source[i]);
            }
            return result.ToString();
        }


        /**
         * Utility to duplicate a string count times
         * @param s string to be duplicated.
         * @param count Number of times to duplicate a string.
         */
        public static string Repeat(string s, int count)
        {
            if (count <= 0) return "";
            if (count == 1) return s;
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < count; ++i)
            {
                result.Append(s);
            }
            return result.ToString();
        }

        public static string[] SplitString(string src, string target)
        {
            return Regex.Split(src, "\\Q" + target + "\\E");
        }

        /**
         * Split the string at runs of ascii whitespace characters.
         */
        public static string[] SplitWhitespace(string src)
        {
            return Regex.Split(src, "\\s+");
        }

        /**
         * Parse a list of hex numbers and return a string
         * @param string string of hex numbers.
         * @param minLength Minimal length.
         * @param separator Separator.
         * @return A string from hex numbers.
         */
        public static string FromHex(string str, int minLength, string separator)
        {
            return FromHex(str, minLength, new Regex(separator != null ? separator : "\\s+"));
        }

        /**
         * Parse a list of hex numbers and return a string
         * @param string string of hex numbers.
         * @param minLength Minimal length.
         * @param separator Separator.
         * @return A string from hex numbers.
         */
        public static string FromHex(string str, int minLength, Regex separator)
        {
            StringBuilder buffer = new StringBuilder();
            string[] parts = separator.Split(str);
            foreach (string part in parts)
            {
                if (part.Length < minLength)
                {
                    throw new ArgumentException("code point too short: " + part);
                }
                //int cp = Integer.parseInt(part, 16);
                int cp = Convert.ToInt32(part, 16);
                buffer.AppendCodePoint(cp);
            }
            return buffer.ToString();
        }

        /**
         * This implementation is equivalent to Java 7+ Objects#equals(Object a, Object b)
         *
         * @param a an object
         * @param b an object to be compared with a for equality
         * @return true if the arguments are equal to each other and false otherwise
         */
        new public static bool Equals(object a, object b)
        {
            return (a == b)
                    || (a != null && b != null && a.Equals(b));
        }

        /**
         * This implementation is equivalent to Java 7+ Objects#hash(Object... values)
         * @param values the values to be hashed
         * @return a hash value of the sequence of input values
         */
        public static int Hash(params object[] values)
        {
            //return Arrays.hashCode(values);
            if (values == null)
            {
                return 0;
            }
            int hashCode = 1;
            foreach (object element in values)
            {
                int elementHashCode;

                if (element == null)
                {
                    elementHashCode = 0;
                }
                else
                {
                    elementHashCode = (element).GetHashCode();
                }
                hashCode = 31 * hashCode + elementHashCode;
            }
            return hashCode;
        }

        /**
         * This implementation is equivalent to Java 7+ Objects#hashCode(Object o)
         * @param o an object
         * @return a hash value of a non-null argument and 0 for null argument
         */
        public static int GetHashCode(object o)
        {
            return o == null ? 0 : o.GetHashCode();
        }

        /**
         * This implementation is equivalent to Java 7+ Objects#toString(Object o)
         * @param o an object
         * @return the result of calling toStirng for a non-null argument and "null" for a
         * null argument
         */
        public static string ToString(object o)
        {
            return o == null ? "null" : o.ToString();
        }
    }
}
