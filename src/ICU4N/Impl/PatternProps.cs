﻿namespace ICU4N.Impl
{
    /// <summary>
    /// Implements the immutable Unicode properties Pattern_Syntax and Pattern_White_Space.
    /// Hardcodes these properties, does not load data, does not depend on other ICU classes.
    /// </summary>
    /// <remarks>
    /// Note: Both properties include ASCII as well as non-ASCII, non-Latin-1 code points,
    /// and both properties only include BMP code points (no supplementary ones).
    /// Pattern_Syntax includes some unassigned code points.
    /// <para/>
    /// [:Pattern_White_Space:] =
    ///     [\u0009-\u000D\ \u0085\u200E\u200F\u2028\u2029]
    /// <para/>
    /// [:Pattern_Syntax:] =
    ///     [!-/\:-@\[-\^`\{-~\u00A1-\u00A7\u00A9\u00AB\u00AC\u00AE
    ///     \u00B0\u00B1\u00B6\u00BB\u00BF\u00D7\u00F7
    ///     \u2010-\u2027\u2030-\u203E\u2041-\u2053\u2055-\u205E
    ///     \u2190-\u245F\u2500-\u2775\u2794-\u2BFF\u2E00-\u2E7F
    ///     \u3001-\u3003\u3008-\u3020\u3030\uFD3E\uFD3F\uFE45\uFE46]
    /// </remarks>
    public sealed partial class PatternProps
    {
        /// <summary>
        /// Returns true if c is a Pattern_Syntax code point.
        /// </summary>
        public static bool IsSyntax(int c)
        {
            if (c < 0)
            {
                return false;
            }
            else if (c <= 0xff)
            {
                return latin1[c] == 3;
            }
            else if (c < 0x2010)
            {
                return false;
            }
            else if (c <= 0x3030)
            {
                int bits = syntax2000[index2000[(c - 0x2000) >> 5]];
                return ((bits >> (c & 0x1f)) & 1) != 0;
            }
            else if (0xfd3e <= c && c <= 0xfe46)
            {
                return c <= 0xfd3f || 0xfe45 <= c;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if c is a Pattern_Syntax or Pattern_White_Space code point.
        /// </summary>
        public static bool IsSyntaxOrWhiteSpace(int c)
        {
            if (c < 0)
            {
                return false;
            }
            else if (c <= 0xff)
            {
                return latin1[c] != 0;
            }
            else if (c < 0x200e)
            {
                return false;
            }
            else if (c <= 0x3030)
            {
                int bits = syntaxOrWhiteSpace2000[index2000[(c - 0x2000) >> 5]];
                return ((bits >> (c & 0x1f)) & 1) != 0;
            }
            else if (0xfd3e <= c && c <= 0xfe46)
            {
                return c <= 0xfd3f || 0xfe45 <= c;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if c is a Pattern_White_Space character.
        /// </summary>
        public static bool IsWhiteSpace(int c)
        {
            if (c < 0)
            {
                return false;
            }
            else if (c <= 0xff)
            {
                return latin1[c] == 5;
            }
            else if (0x200e <= c && c <= 0x2029)
            {
                return c <= 0x200f || 0x2028 <= c;
            }
            else
            {
                return false;
            }
        }

        // ICU4N specific - SkipWhiteSpace(ICharSequence s, int i) moved to PatternPropsExtension.tt

        /// <returns><paramref name="s"/> except with leading and trailing Pattern_White_Space removed.</returns>
        public static string TrimWhiteSpace(string s)
        {
            if (s.Length == 0 || (!IsWhiteSpace(s[0]) && !IsWhiteSpace(s[s.Length - 1])))
            {
                return s;
            }
            int start = 0;
            int limit = s.Length;
            while (start < limit && IsWhiteSpace(s[start]))
            {
                ++start;
            }
            if (start < limit)
            {
                // There is non-white space at start; we will not move limit below that,
                // so we need not test start<limit in the loop.
                while (IsWhiteSpace(s[limit - 1]))
                {
                    --limit;
                }
            }
            return s.Substring(start, limit - start); // ICU4N: Corrected 2nd parameter
        }


        // ICU4N specific - IsIdentifier(ICharSequence s) moved to PatternPropsExtension.tt

        // ICU4N specific - IsIdentifier(ICharSequence s, int start, int limit) moved to PatternPropsExtension.tt

        // ICU4N specific - SkipIdentifier(ICharSequence s, int i) moved to PatternPropsExtension.tt


        /// <summary>
        /// One byte per Latin-1 character.
        /// Bit 0 is set if either Pattern property is true,
        /// bit 1 if Pattern_Syntax is true,
        /// bit 2 if Pattern_White_Space is true.
        /// That is, Pattern_Syntax is encoded as 3 and Pattern_White_Space as 5.
        /// </summary>
        private static readonly byte[] latin1 = new byte[] {  // 256
            // WS: 9..D
            0, 0, 0, 0, 0, 0, 0, 0, 0, 5, 5, 5, 5, 5, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            // WS: 20  Syntax: 21..2F
            5, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
            // Syntax: 3A..40
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 3, 3, 3, 3, 3,
            3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            // Syntax: 5B..5E
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 3, 3, 3, 0,
            // Syntax: 60
            3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            // Syntax: 7B..7E
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 3, 3, 3, 0,
            // WS: 85
            0, 0, 0, 0, 0, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            // Syntax: A1..A7, A9, AB, AC, AE
            0, 3, 3, 3, 3, 3, 3, 3, 0, 3, 0, 3, 3, 0, 3, 0,
            // Syntax: B0, B1, B6, BB, BF
            3, 3, 0, 0, 0, 0, 3, 0, 0, 0, 0, 3, 0, 0, 0, 3,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            // Syntax: D7
            0, 0, 0, 0, 0, 0, 0, 3, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            // Syntax: F7
            0, 0, 0, 0, 0, 0, 0, 3, 0, 0, 0, 0, 0, 0, 0, 0
        };

        /// <summary>
        /// One byte per 32 characters from U+2000..U+303F indexing into
        /// a small table of 32-bit data words.
        /// The first two data words are all-zeros and all-ones.
        /// </summary>
        private static readonly byte[] index2000 = new byte[] {  // 130
            2, 3, 4, 0, 0, 0, 0, 0,  // 20xx
            0, 0, 0, 0, 5, 1, 1, 1,  // 21xx
            1, 1, 1, 1, 1, 1, 1, 1,  // 22xx
            1, 1, 1, 1, 1, 1, 1, 1,  // 23xx
            1, 1, 1, 0, 0, 0, 0, 0,  // 24xx
            1, 1, 1, 1, 1, 1, 1, 1,  // 25xx
            1, 1, 1, 1, 1, 1, 1, 1,  // 26xx
            1, 1, 1, 6, 7, 1, 1, 1,  // 27xx
            1, 1, 1, 1, 1, 1, 1, 1,  // 28xx
            1, 1, 1, 1, 1, 1, 1, 1,  // 29xx
            1, 1, 1, 1, 1, 1, 1, 1,  // 2Axx
            1, 1, 1, 1, 1, 1, 1, 1,  // 2Bxx
            0, 0, 0, 0, 0, 0, 0, 0,  // 2Cxx
            0, 0, 0, 0, 0, 0, 0, 0,  // 2Dxx
            1, 1, 1, 1, 0, 0, 0, 0,  // 2Exx
            0, 0, 0, 0, 0, 0, 0, 0,  // 2Fxx
            8, 9  // 3000..303F
        };

        /// <summary>
        /// One 32-bit integer per 32 characters. Ranges of all-false and all-true
        /// are mapped to the first two values, other ranges map to appropriate bit patterns.
        /// </summary>
        private static readonly int[] syntax2000 = new int[] {
            0,
            -1,
            unchecked((int)0xffff0000),  // 2: 2010..201F
            0x7fff00ff,  // 3: 2020..2027, 2030..203E
            0x7feffffe,  // 4: 2041..2053, 2055..205E
            unchecked((int)0xffff0000),  // 5: 2190..219F
            0x003fffff,  // 6: 2760..2775
            unchecked((int)0xfff00000),  // 7: 2794..279F
            unchecked((int)0xffffff0e),  // 8: 3001..3003, 3008..301F
            0x00010001   // 9: 3020, 3030
        };

        /// <summary>
        /// Same as syntax2000, but with additional bits set for the
        /// Pattern_White_Space characters 200E 200F 2028 2029.
        /// </summary>
        private static readonly int[] syntaxOrWhiteSpace2000 = new int[] {
            0,
            -1,
            unchecked((int)0xffffc000),  // 2: 200E..201F
            0x7fff03ff,  // 3: 2020..2029, 2030..203E
            0x7feffffe,  // 4: 2041..2053, 2055..205E
            unchecked((int)0xffff0000),  // 5: 2190..219F
            0x003fffff,  // 6: 2760..2775
            unchecked((int)0xfff00000),  // 7: 2794..279F
            unchecked((int)0xffffff0e),  // 8: 3001..3003, 3008..301F
            0x00010001   // 9: 3020, 3030
        };
    }
}
