﻿using ICU4N.Globalization;
using ICU4N.Text;
using ICU4N.Util;
using J2N;
using J2N.IO;
using J2N.Numerics;
using System;
using System.Diagnostics;
using System.IO;
using System.Resources;
using System.Text;

namespace ICU4N.Impl
{
    /// <summary>
    /// Constants for which data and implementation files provide which properties.
    /// Used by <see cref="UnicodeSet"/> for service-specific property enumeration.
    /// </summary>
    public enum UPropertySource
    {
        /// <summary>No source, not a supported property.</summary>
        None = 0,
        /// <summary>From uchar.c/uprops.icu main trie</summary>
        Char = 1,
        /// <summary>From uchar.c/uprops.icu properties vectors trie</summary>
        PropertiesVectorsTrie = 2,
        /// <summary>From unames.c/unames.icu</summary>
        Names = 3,
        /// <summary>From ucase.c/ucase.icu</summary>
        Case = 4,
        /// <summary>From ubidi_props.c/ubidi.icu</summary>
        BiDi = 5,
        /// <summary>From uchar.c/uprops.icu main trie as well as properties vectors trie</summary>
        CharAndPropertiesVectorsTrie = 6,
        /// <summary>From ucase.c/ucase.icu as well as unorm.cpp/unorm.icu</summary>
        CaseAndNormalizer = 7,
        /// <summary>From normalizer2impl.cpp/nfc.nrm</summary>
        NFC = 8,
        /// <summary>From normalizer2impl.cpp/nfkc.nrm</summary>
        NFKC = 9,
        /// <summary>From normalizer2impl.cpp/nfkc_cf.nrm</summary>
        NFKCCaseFold = 10,
        /// <summary>From normalizer2impl.cpp/nfc.nrm canonical iterator data</summary>
        NFCCanonicalIterator = 11,
        /// <summary>One more than the highest UPropertySource (SRC_) constant.</summary>
        Count = 12,
    }

    /// <summary>
    /// Internal class used for Unicode character property database.
    /// </summary>
    /// <remarks>
    /// This classes store binary data read from uprops.icu.
    /// It does not have the capability to parse the data into more high-level
    /// information. It only returns bytes of information when required.
    /// <para/>
    /// Due to the form most commonly used for retrieval, array of char is used
    /// to store the binary data.
    /// <para/>
    /// UCharacterPropertyDB also contains information on accessing indexes to
    /// significant points in the binary data.
    /// <para/>
    /// Responsibility for molding the binary data into more meaning form lies on
    /// <see cref="UChar"/>.
    /// </remarks>
    /// <author>Syn Wee Quek</author>
    /// <since>release 2.1, february 1st 2002</since>
    public sealed class UCharacterProperty
    {
        // public data members -----------------------------------------------

        // ICU4N: Moved public Instance property to where static constructor was (we initialize inline and need to ensure it is last)
        
        private Trie2_16 m_trie_;
        /// <summary>
        /// Trie data.
        /// </summary>
        public Trie2_16 Trie => m_trie_;


        private VersionInfo m_unicodeVersion_;
        /// <summary>
        /// Unicode version.
        /// </summary>
        public VersionInfo UnicodeVersion => m_unicodeVersion_;

        /// <summary>
        /// Latin capital letter i with dot above
        /// </summary>
        public const char Latin_Capital_Letter_I_With_Dot_Above = (char)0x130;
        /// <summary>
        /// Latin small letter i with dot above
        /// </summary>
        public const char Latin_Small_Letter_Dotless_I = (char)0x131;
        /// <summary>
        /// Latin lowercase i
        /// </summary>
        public const char Latin_Small_Letter_I = (char)0x69;
        /// <summary>
        /// Character type mask
        /// </summary>
        public const int TypeMask = 0x1F;

        // uprops.h enum UPropertySource --------------------------------------- ***

        // ICU4N specific - made constants int UPropertySource enum

        // public methods ----------------------------------------------------

        /// <summary>
        /// Gets the main property value for code point <paramref name="ch"/>.
        /// </summary>
        /// <param name="ch">Code point whose property value is to be retrieved.</param>
        /// <returns>Property value of code point.</returns>
        public int GetProperty(int ch)
        {
            return m_trie_.Get(ch);
        }

        /// <summary>
        /// Gets the unicode additional properties.
        /// .NET version of C u_getUnicodeProperties().
        /// </summary>
        /// <param name="codepoint">Codepoint whose additional properties is to be retrieved.</param>
        /// <param name="column">The column index.</param>
        /// <returns>Unicode properties.</returns>
        public int GetAdditional(int codepoint, int column)
        {
            Debug.Assert(column >= 0);
            if (column >= m_additionalColumnsCount_)
            {
                return 0;
            }
            return m_additionalVectors_[m_additionalTrie_.Get(codepoint) + column];
        }

        internal const int MY_MASK = UCharacterProperty.TypeMask
            & ((1 << (int)UUnicodeCategory.UppercaseLetter) |
                (1 << (int)UUnicodeCategory.LowercaseLetter) |
                (1 << (int)UUnicodeCategory.TitlecaseLetter) |
                (1 << (int)UUnicodeCategory.ModifierLetter) |
                (1 << (int)UUnicodeCategory.OtherLetter));

        /// <summary>
        /// Get the "age" of the code point.
        /// </summary>
        /// <remarks>
        /// The "age" is the Unicode version when the code point was first
        /// designated (as a non-character or for Private Use) or assigned a
        /// character.
        /// <para/>
        /// This can be useful to avoid emitting code points to receiving
        /// processes that do not accept newer characters.
        /// <para/>
        /// The data is from the UCD file DerivedAge.txt.
        /// <para/>
        /// This API does not check the validity of the codepoint.
        /// </remarks>
        /// <param name="codepoint">The code point.</param>
        /// <returns>The Unicode version number.</returns>
        public VersionInfo GetAge(int codepoint)
        {
            int version = GetAdditional(codepoint, 0) >> AGE_SHIFT_;
            return VersionInfo.GetInstance(
                               (version >> FIRST_NIBBLE_SHIFT_) & LAST_NIBBLE_MASK_,
                               version & LAST_NIBBLE_MASK_, 0, 0);
        }

        private const int GC_CN_MASK = 1 << (int)UUnicodeCategory.OtherNotAssigned;
        private const int GC_CC_MASK = 1 << (int)UUnicodeCategory.Control;
        private const int GC_CS_MASK = 1 << (int)UUnicodeCategory.Surrogate;
        private const int GC_ZS_MASK = 1 << (int)UUnicodeCategory.SpaceSeparator;
        private const int GC_ZL_MASK = 1 << (int)UUnicodeCategory.LineSeparator;
        private const int GC_ZP_MASK = 1 << (int)UUnicodeCategory.ParagraphSeparator;
        /// <summary>Mask constant for multiple UCharCategory bits (Z Separators).</summary>
        private const int GC_Z_MASK = GC_ZS_MASK | GC_ZL_MASK | GC_ZP_MASK;

        /// <summary>
        /// Checks if <paramref name="c"/> is in
        /// [^\p{space}\p{gc=Control}\p{gc=Surrogate}\p{gc=Unassigned}]
        /// with space=\p{Whitespace} and Control=Cc.
        /// Implements UCHAR_POSIX_GRAPH.
        /// </summary>
        /// <internal/>
        private static bool IsgraphPOSIX(int c)
        {
            /* \p{space}\p{gc=Control} == \p{gc=Z}\p{Control} */
            /* comparing ==0 returns FALSE for the categories mentioned */
            return (GetMask(UChar.GetUnicodeCategory(c)) &
                    (GC_CC_MASK | GC_CS_MASK | GC_CN_MASK | GC_Z_MASK))
                   == 0;
        }

        // binary properties --------------------------------------------------- ***

        private class BinaryProperty
        {
            private readonly UCharacterProperty outerInstance;
            private readonly int column;  // SRC_PROPSVEC column, or "source" if mask==0
            private readonly UPropertySource source;
            private readonly int mask;
            internal BinaryProperty(UCharacterProperty outerInstance, int column, int mask)
            {
                this.outerInstance = outerInstance;
                this.column = column;
                this.mask = mask;
            }
            internal BinaryProperty(UCharacterProperty outerInstance, UPropertySource source)
            {
                this.outerInstance = outerInstance;
                this.source = source;
                this.mask = 0;
            }
            internal UPropertySource Source => mask == 0 ? source : UPropertySource.PropertiesVectorsTrie;

            internal virtual bool Contains(int c)
            {
                // systematic, directly stored properties
                return (outerInstance.GetAdditional(c, column) & mask) != 0;
            }
        }

        private class CaseBinaryProperty : BinaryProperty
        {  // case mapping properties
            private readonly UProperty which;
            internal CaseBinaryProperty(UCharacterProperty outerInstance, UProperty which)
                : base(outerInstance, UPropertySource.Case)
            {
                this.which = which;
            }
            internal override bool Contains(int c)
            {
                return UCaseProperties.Instance.HasBinaryProperty(c, which);
            }
        }

        private class NormInertBinaryProperty : BinaryProperty
        {  // UCHAR_NF*_INERT properties
            private readonly UProperty which;
            internal NormInertBinaryProperty(UCharacterProperty outerInstance, UPropertySource source, UProperty which)
                : base(outerInstance, source)
            {
                this.which = which;
            }
            internal override bool Contains(int c)
            {
                return Norm2AllModes.GetN2WithImpl((int)which - (int)UProperty.NFD_Inert).IsInert(c);
            }
        }

        // ICU4N specific class for building BinaryProperties on the fly
        private class AnonymousBinaryProperty : BinaryProperty
        {
            private readonly Func<int, bool> contains;

            internal AnonymousBinaryProperty(UCharacterProperty outerInstance, UPropertySource source, Func<int, bool> contains)
                : base(outerInstance, source)
            {
                this.contains = contains;
            }

            internal override bool Contains(int c)
            {
                return contains(c);
            }
        }

        private void Init()
        {
            binProps = new BinaryProperty[] {
                /*
                 * Binary-property implementations must be in order of corresponding UProperty,
                 * and there must be exactly one entry per binary UProperty.
                 */
                new BinaryProperty(this, 1, (1 << ALPHABETIC_PROPERTY_)),
                new BinaryProperty(this, 1, (1 << ASCII_HEX_DIGIT_PROPERTY_)),
                new AnonymousBinaryProperty(this, UPropertySource.BiDi, contains: (c) =>
                    {
                        return UBiDiProps.Instance.IsBidiControl(c);
                    }),
                new AnonymousBinaryProperty(this, UPropertySource.BiDi, contains: (c) =>
                    {
                        return UBiDiProps.Instance.IsMirrored(c);
                    }),
                new BinaryProperty(this, 1, (1<<DASH_PROPERTY_)),
                new BinaryProperty(this, 1, (1<<DEFAULT_IGNORABLE_CODE_POINT_PROPERTY_)),
                new BinaryProperty(this, 1, (1<<DEPRECATED_PROPERTY_)),
                new BinaryProperty(this, 1, (1<<DIACRITIC_PROPERTY_)),
                new BinaryProperty(this, 1, (1<<EXTENDER_PROPERTY_)),
                new AnonymousBinaryProperty(this, UPropertySource.NFC, contains: (c) =>
                    {// UCHAR_FULL_COMPOSITION_EXCLUSION
                        // By definition, Full_Composition_Exclusion is the same as NFC_QC=No.
                        Normalizer2Impl impl = Norm2AllModes.GetNFCInstance().Impl;
                        return impl.IsCompNo(impl.GetNorm16(c));
                    }),
                new BinaryProperty(this,1, (1<<GRAPHEME_BASE_PROPERTY_)),
                new BinaryProperty(this,1, (1<<GRAPHEME_EXTEND_PROPERTY_)),
                new BinaryProperty(this,1, (1<<GRAPHEME_LINK_PROPERTY_)),
                new BinaryProperty(this,1, (1<<HEX_DIGIT_PROPERTY_)),
                new BinaryProperty(this,1, (1<<HYPHEN_PROPERTY_)),
                new BinaryProperty(this,1, (1<<ID_CONTINUE_PROPERTY_)),
                new BinaryProperty(this,1, (1<<ID_START_PROPERTY_)),
                new BinaryProperty(this,1, (1<<IDEOGRAPHIC_PROPERTY_)),
                new BinaryProperty(this,1, (1<<IDS_BINARY_OPERATOR_PROPERTY_)),
                new BinaryProperty(this,1, (1<<IDS_TRINARY_OPERATOR_PROPERTY_)),
                new AnonymousBinaryProperty(this, UPropertySource.BiDi, contains: (c) =>
                    { // UCHAR_JOIN_CONTROL
                        return UBiDiProps.Instance.IsJoinControl(c);
                    }),
                new BinaryProperty(this,1, (1<<LOGICAL_ORDER_EXCEPTION_PROPERTY_)),
                new CaseBinaryProperty(this, UProperty.Lowercase),
                new BinaryProperty(this,1, (1<<MATH_PROPERTY_)),
                new BinaryProperty(this,1, (1<<NONCHARACTER_CODE_POINT_PROPERTY_)),
                new BinaryProperty(this,1, (1<<QUOTATION_MARK_PROPERTY_)),
                new BinaryProperty(this,1, (1<<RADICAL_PROPERTY_)),
                new CaseBinaryProperty(this, UProperty.Soft_Dotted),
                new BinaryProperty(this,1, (1<<TERMINAL_PUNCTUATION_PROPERTY_)),
                new BinaryProperty(this,1, (1<<UNIFIED_IDEOGRAPH_PROPERTY_)),
                new CaseBinaryProperty(this, UProperty.Uppercase),
                new BinaryProperty(this,1, (1<<WHITE_SPACE_PROPERTY_)),
                new BinaryProperty(this,1, (1<<XID_CONTINUE_PROPERTY_)),
                new BinaryProperty(this,1, (1<<XID_START_PROPERTY_)),
                new CaseBinaryProperty(this, UProperty.Case_Sensitive),
                new BinaryProperty(this,1, (1<<S_TERM_PROPERTY_)),
                new BinaryProperty(this,1, (1<<VARIATION_SELECTOR_PROPERTY_)),
                new NormInertBinaryProperty(this,UPropertySource.NFC, UProperty.NFD_Inert),
                new NormInertBinaryProperty(this,UPropertySource.NFKC, UProperty.NFKD_Inert),
                new NormInertBinaryProperty(this,UPropertySource.NFC, UProperty.NFC_Inert),
                new NormInertBinaryProperty(this,UPropertySource.NFKC, UProperty.NFKC_Inert),
                new AnonymousBinaryProperty(this, UPropertySource.NFCCanonicalIterator, contains: (c) =>
                    {  // UCHAR_SEGMENT_STARTER
                        return Norm2AllModes.GetNFCInstance().Impl.
                            EnsureCanonIterData().IsCanonSegmentStarter(c);
                    }),
                new BinaryProperty(this, 1, (1<<PATTERN_SYNTAX)),
                new BinaryProperty(this, 1, (1<<PATTERN_WHITE_SPACE)),
                new AnonymousBinaryProperty(this, UPropertySource.CharAndPropertiesVectorsTrie, contains: (c) =>
                    {  // UCHAR_POSIX_ALNUM
                        return UChar.IsUAlphabetic(c) || UChar.IsDigit(c);
                    }),
                new AnonymousBinaryProperty(this, UPropertySource.Char, contains: (c) =>
                    {  // UCHAR_POSIX_BLANK
                        // "horizontal space"
                        if (c <= 0x9f)
                        {
                            return c == 9 || c == 0x20; /* TAB or SPACE */
                        }
                        else
                        {
                            /* Zs */
                            return UChar.GetUnicodeCategory(c) == UUnicodeCategory.SpaceSeparator;
                        }
                    }),
                new AnonymousBinaryProperty(this, UPropertySource.Char, contains: (c) =>
                    {  // UCHAR_POSIX_GRAPH
                        return IsgraphPOSIX(c);
                    }),
                new AnonymousBinaryProperty(this, UPropertySource.Char, contains: (c) =>
                    {  // UCHAR_POSIX_PRINT
                        /*
                        * Checks if codepoint is in \p{graph}\p{blank} - \p{cntrl}.
                        *
                        * The only cntrl character in graph+blank is TAB (in blank).
                        * Here we implement (blank-TAB)=Zs instead of calling u_isblank().
                        */
                        return (UChar.GetUnicodeCategory(c) == UUnicodeCategory.SpaceSeparator) || IsgraphPOSIX(c);
                    }),
                new AnonymousBinaryProperty(this, UPropertySource.Char, contains: (c) =>
                    {  // UCHAR_POSIX_XDIGIT
                        /* check ASCII and Fullwidth ASCII a-fA-F */
                        if (
                            (c <= 0x66 && c >= 0x41 && (c <= 0x46 || c >= 0x61)) ||
                            (c >= 0xff21 && c <= 0xff46 && (c <= 0xff26 || c >= 0xff41))
                        )
                        {
                            return true;
                        }
                        return UChar.GetUnicodeCategory(c) == UUnicodeCategory.DecimalDigitNumber;
                    }),
                new CaseBinaryProperty(this, UProperty.Cased),
                new CaseBinaryProperty(this, UProperty.Case_Ignorable),
                new CaseBinaryProperty(this, UProperty.Changes_When_Lowercased),
                new CaseBinaryProperty(this, UProperty.Changes_When_Uppercased),
                new CaseBinaryProperty(this, UProperty.Changes_When_Titlecased),
                new AnonymousBinaryProperty(this, UPropertySource.CaseAndNormalizer, contains: (c) =>
                    {  // UCHAR_CHANGES_WHEN_CASEFOLDED
                        string nfd = Norm2AllModes.GetNFCInstance().Impl.GetDecomposition(c);
                        if (nfd != null)
                        {
                            /* c has a decomposition */
                            c = nfd.CodePointAt(0);
                            if (Character.CharCount(c) != nfd.Length)
                            {
                                /* multiple code points */
                                c = -1;
                            }
                        }
                        else if (c < 0)
                        {
                            return false;  /* protect against bad input */
                        }
                        if (c >= 0)
                        {
                            /* single code point */
                            UCaseProperties csp = UCaseProperties.Instance;
                            UCaseProperties.DummyStringBuilder.Length = 0;
                            return csp.ToFullFolding(c, UCaseProperties.DummyStringBuilder,
                                                        UChar.FoldCaseDefault) >= 0;
                        }
                        else
                        {
                            string folded = UChar.FoldCase(nfd, true);
                            return !folded.Equals(nfd);
                        }
                    }),
                new CaseBinaryProperty(this, UProperty.Changes_When_Casemapped),
                new AnonymousBinaryProperty(this, UPropertySource.NFKCCaseFold, contains: (c) =>
                    {  // UCHAR_CHANGES_WHEN_NFKC_CASEFOLDED
                        Normalizer2Impl kcf = Norm2AllModes.GetNFKC_CFInstance().Impl;
                        string src = UTF16.ValueOf(c);
                        StringBuilder dest = new StringBuilder();
                        // Small destCapacity for NFKC_CF(c).
                        ReorderingBuffer buffer = new ReorderingBuffer(kcf, dest, 5);
                        kcf.Compose(src, 0, src.Length, false, true, buffer);
                        return !UTF16Plus.Equal(dest, src);
                    }),
                new BinaryProperty(this, 2, 1<<PROPS_2_EMOJI),
                new BinaryProperty(this, 2, 1<<PROPS_2_EMOJI_PRESENTATION),
                new BinaryProperty(this, 2, 1<<PROPS_2_EMOJI_MODIFIER),
                new BinaryProperty(this, 2, 1<<PROPS_2_EMOJI_MODIFIER_BASE),
                new BinaryProperty(this, 2, 1<<PROPS_2_EMOJI_COMPONENT),
                new AnonymousBinaryProperty(this, UPropertySource.PropertiesVectorsTrie, contains: (c) =>
                    {  // REGIONAL_INDICATOR
                        // Property starts are a subset of lb=RI etc.
                        return 0x1F1E6 <= c && c <= 0x1F1FF;
                    }),
                new BinaryProperty(this, 1, 1<<PREPENDED_CONCATENATION_MARK),
            };

            intProps = new IntProperty[]
            {
                new BiDiIntProperty(this, getValue: (c) =>
                    {
                        return UBiDiProps.Instance.GetClass(c).ToInt32();
                    }),
                new IntProperty(this, 0, BLOCK_MASK_, BLOCK_SHIFT_),
                new CombiningClassIntProperty(this, UPropertySource.NFC, getValue: (c) =>
                    { // CANONICAL_COMBINING_CLASS
                        return Normalizer2.GetNFDInstance().GetCombiningClass(c);
                    }),
                new IntProperty(this, 2, DECOMPOSITION_TYPE_MASK_, 0),
                new IntProperty(this, 0, EAST_ASIAN_MASK_, EAST_ASIAN_SHIFT_),
                new AnonymousIntProperty(this, UPropertySource.Char, getValue: (c) =>
                    {  // GENERAL_CATEGORY
                        return (int)GetUnicodeCategory(c);
                    }, getMaxValue: (which) =>
                    {
                        return UUnicodeCategoryExtensions.CharCategoryCount - 1;
                    }),
                new BiDiIntProperty(this, getValue: (c) =>
                    {  // JOINING_GROUP
                        return UBiDiProps.Instance.GetJoiningGroup(c);
                    }),
                new BiDiIntProperty(this, getValue: (c) =>
                    {  // JOINING_TYPE
                        return UBiDiProps.Instance.GetJoiningType(c);
                    }),
                new IntProperty(this, 2, LB_MASK, LB_SHIFT),  // LINE_BREAK
                new AnonymousIntProperty(this, UPropertySource.Char, getValue: (c) =>
                    {  // NUMERIC_TYPE
                        return NtvGetType(GetNumericTypeValue(GetProperty(c)));
                    }, getMaxValue: (which) =>
                    {
#pragma warning disable 612, 618
                        return NumericType.Count - 1;
#pragma warning restore 612, 618
                    }),
                new AnonymousIntProperty(this, 0, ScriptMask, 0, getValue: (c) =>
                    {
                        return (int)UScript.GetScript(c);
                    }, getMaxValue: null),
                new AnonymousIntProperty(this, UPropertySource.PropertiesVectorsTrie, getValue: (c) =>
                    {  // HANGUL_SYLLABLE_TYPE
                        /* see comments on gcbToHst[] above */
                        int gcb = (GetAdditional(c, 2) & GCB_MASK).TripleShift(GCB_SHIFT);
                        if (gcb < gcbToHst.Length)
                        {
                            return gcbToHst[gcb];
                        }
                        else
                        {
                            return HangulSyllableType.NotApplicable;
                        }
                    }, getMaxValue: (which) =>
                    {
#pragma warning disable 612, 618
                        return HangulSyllableType.Count - 1;
#pragma warning restore 612, 618
                    }),
                // max=1=YES -- these are never "maybe", only "no" or "yes"
                new NormQuickCheckIntProperty(this, UPropertySource.NFC, UProperty.NFD_Quick_Check, 1),
                new NormQuickCheckIntProperty(this, UPropertySource.NFKC, UProperty.NFKD_Quick_Check, 1),
                // max=2=MAYBE
                new NormQuickCheckIntProperty(this, UPropertySource.NFC, UProperty.NFC_Quick_Check, 2),
                new NormQuickCheckIntProperty(this, UPropertySource.NFKC, UProperty.NFKC_Quick_Check, 2),
                new CombiningClassIntProperty(this, UPropertySource.NFC, getValue: (c) =>
                    {  // LEAD_CANONICAL_COMBINING_CLASS
                        return Norm2AllModes.GetNFCInstance().Impl.GetFCD16(c) >> 8;
                    }),
                new CombiningClassIntProperty(this, UPropertySource.NFC, getValue: (c) =>
                    {  // TRAIL_CANONICAL_COMBINING_CLASS
                        return Norm2AllModes.GetNFCInstance().Impl.GetFCD16(c) & 0xff;
                    }),
                new IntProperty(this, 2, GCB_MASK, GCB_SHIFT),  // GRAPHEME_CLUSTER_BREAK
                new IntProperty(this, 2, SB_MASK, SB_SHIFT),  // SENTENCE_BREAK
                new IntProperty(this, 2, WB_MASK, WB_SHIFT),  // WORD_BREAK
                new BiDiIntProperty(this, getValue: (c) =>
                    {  // BIDI_PAIRED_BRACKET_TYPE
                        return UBiDiProps.Instance.GetPairedBracketType(c);
                    }),
            };
        }


        private BinaryProperty[] binProps;


        public bool HasBinaryProperty(int c, UProperty which)
        {
            if (which < UPropertyConstants.Binary_Start
#pragma warning disable 612, 618
                || UPropertyConstants.Binary_Limit <= which)
#pragma warning restore 612, 618
            {
                // not a known binary property
                return false;
            }
            else
            {
                return binProps[(int)which].Contains(c);
            }
        }

        // int-value and enumerated properties --------------------------------- ***

        public UUnicodeCategory GetUnicodeCategory(int c)  // ICU4N specific - renamed from GetType() to cover System.Char.GetUnicodeCategory()
        {
            return (UUnicodeCategory)(GetProperty(c) & TypeMask);
        }

        /// <summary>
        /// Map some of the Grapheme Cluster Break values to Hangul Syllable Types.
        /// Hangul_Syllable_Type is fully redundant with a subset of Grapheme_Cluster_Break.
        /// </summary>
        private static readonly int[] /* UHangulSyllableType */ gcbToHst ={
            HangulSyllableType.NotApplicable,   /* U_GCB_OTHER */
            HangulSyllableType.NotApplicable,   /* U_GCB_CONTROL */
            HangulSyllableType.NotApplicable,   /* U_GCB_CR */
            HangulSyllableType.NotApplicable,   /* U_GCB_EXTEND */
            HangulSyllableType.LeadingJamo,     /* U_GCB_L */
            HangulSyllableType.NotApplicable,   /* U_GCB_LF */
            HangulSyllableType.LvSyllable,      /* U_GCB_LV */
            HangulSyllableType.LvtSyllable,     /* U_GCB_LVT */
            HangulSyllableType.TrailingJamo,    /* U_GCB_T */
            HangulSyllableType.VowelJamo        /* U_GCB_V */
            /*
             * Omit GCB values beyond what we need for hst.
             * The code below checks for the array length.
             */
        };

        private class IntProperty
        {
            private readonly UCharacterProperty outerInstance;
            private readonly int column;  // SRC_PROPSVEC column, or "source" if mask==0
            private readonly UPropertySource source;
            private readonly int mask;
            private readonly int shift;
            internal IntProperty(UCharacterProperty outerInstance, int column, int mask, int shift)
            {
                this.outerInstance = outerInstance;
                this.column = column;
                this.mask = mask;
                this.shift = shift;
            }
            internal IntProperty(UCharacterProperty outerInstance, UPropertySource source)
            {
                this.outerInstance = outerInstance;
                this.source = source;
                this.mask = 0;
            }
            internal UPropertySource Source => mask == 0 ? source : UPropertySource.PropertiesVectorsTrie;

            internal virtual int GetValue(int c)
            {
                // systematic, directly stored properties
                return (outerInstance.GetAdditional(c, column) & mask).TripleShift(shift);
            }
            internal virtual int GetMaxValue(UProperty which)
            {
                return (outerInstance.GetMaxValues(column) & mask).TripleShift(shift);
            }
        }

        private class AnonymousIntProperty : IntProperty
        {
            private readonly Func<int, int> getValue;
            private readonly Func<UProperty, int> getMaxValue;

            internal AnonymousIntProperty(UCharacterProperty outerInstance, UPropertySource source, Func<int, int> getValue, Func<UProperty, int> getMaxValue)
                : base(outerInstance, source)
            {
                this.getValue = getValue;
                this.getMaxValue = getMaxValue;
            }

            internal AnonymousIntProperty(UCharacterProperty outerInstance, int column, int mask, int shift, Func<int, int> getValue, Func<UProperty, int> getMaxValue)
                : base(outerInstance, column, mask, shift)
            {
                this.getValue = getValue;
                this.getMaxValue = getMaxValue;
            }

            internal override int GetValue(int c)
            {
                return getValue == null ? base.GetValue(c) : getValue(c);
            }

            internal override int GetMaxValue(UProperty which)
            {
                return getMaxValue == null ? base.GetMaxValue(which) : getMaxValue(which);
            }
        }


        private class BiDiIntProperty : IntProperty
        {
            private readonly Func<int, int> getValue;

            internal BiDiIntProperty(UCharacterProperty outerInstance, Func<int, int> getValue)
                        : base(outerInstance, UPropertySource.BiDi)
            {
                this.getValue = getValue;
            }

            internal override int GetValue(int c)
            {
                return getValue == null ? base.GetValue(c) : getValue(c);
            }

            internal override int GetMaxValue(UProperty which)
            {
                return UBiDiProps.Instance.GetMaxValue(which);
            }
        }

        private class CombiningClassIntProperty : IntProperty
        {
            private readonly Func<int, int> getValue;

            internal CombiningClassIntProperty(UCharacterProperty outerInstance, UPropertySource source, Func<int, int> getValue)
                : base(outerInstance, source)
            {
                this.getValue = getValue;
            }

            internal override int GetValue(int c)
            {
                return getValue == null ? base.GetValue(c) : getValue(c);
            }

            internal override int GetMaxValue(UProperty which)
            {
                return 0xff;
            }
        }

        private class NormQuickCheckIntProperty : IntProperty
        {  // UCHAR_NF*_QUICK_CHECK properties
            private readonly UProperty which;
            private readonly int max;
            internal NormQuickCheckIntProperty(UCharacterProperty outerInstance, UPropertySource source, UProperty which, int max)
                : base(outerInstance, source)
            {
                this.which = which;
                this.max = max;
            }

            internal override int GetValue(int c)
            {
                return Norm2AllModes.GetN2WithImpl((int)which - (int)UProperty.NFD_Quick_Check).GetQuickCheck(c);
            }

            internal override int GetMaxValue(UProperty which)
            {
                return max;
            }
        }

        private IntProperty[] intProps;

        public int GetIntPropertyValue(int c, UProperty which)
        {
            if (which < UPropertyConstants.Int_Start)
            {
                if (UPropertyConstants.Binary_Start <= which
#pragma warning disable 612, 618
                    && which < UPropertyConstants.Binary_Limit)
#pragma warning restore 612, 618
                {
                    return binProps[(int)which].Contains(c) ? 1 : 0;
                }
            }
#pragma warning disable 612, 618
            else if (which < UPropertyConstants.Int_Limit)
#pragma warning restore 612, 618
            {
                return intProps[which - UPropertyConstants.Int_Start].GetValue(c);
            }
            else if (which == UProperty.General_Category_Mask)
            {
                return GetMask(GetUnicodeCategory(c));
            }
            return 0; // undefined
        }

        public int GetIntPropertyMaxValue(UProperty which)
        {
            if (which < UPropertyConstants.Int_Start)
            {
                if (UPropertyConstants.Binary_Start <= which
#pragma warning disable 612, 618
                    && which < UPropertyConstants.Binary_Limit)
#pragma warning restore 612, 618
                {
                    return 1;  // maximum TRUE for all binary properties
                }
            }
#pragma warning disable 612, 618
            else if (which < UPropertyConstants.Int_Limit)
#pragma warning restore 612, 618
            {
                return intProps[which - UPropertyConstants.Int_Start].GetMaxValue(which);
            }
            return -1; // undefined
        }

        public UPropertySource GetSource(UProperty which)
        {
            if (which < UPropertyConstants.Binary_Start)
            {
                return UPropertySource.None; /* undefined */
            }
#pragma warning disable 612, 618
            else if (which < UPropertyConstants.Binary_Limit)
#pragma warning restore 612, 618
            {
                return binProps[(int)which].Source;
            }
            else if (which < UPropertyConstants.Int_Start)
            {
                return UPropertySource.None; /* undefined */
            }
#pragma warning disable 612, 618
            else if (which < UPropertyConstants.Int_Limit)
#pragma warning restore 612, 618
            {
                return intProps[which - UPropertyConstants.Int_Start].Source;
            }
            else if (which < UPropertyConstants.String_Start)
            {
                switch (which)
                {
                    case UProperty.General_Category_Mask:
                    case UProperty.Numeric_Value:
                        return UPropertySource.Char;

                    default:
                        return UPropertySource.None;
                }
            }
#pragma warning disable 612, 618
            else if (which < UPropertyConstants.String_Limit)
#pragma warning restore 612, 618
            {
                switch (which)
                {
                    case UProperty.Age:
                        return UPropertySource.PropertiesVectorsTrie;

                    case UProperty.Bidi_Mirroring_Glyph:
                        return UPropertySource.BiDi;

                    case UProperty.Case_Folding:
                    case UProperty.Lowercase_Mapping:
                    case UProperty.Simple_Case_Folding:
                    case UProperty.Simple_Lowercase_Mapping:
                    case UProperty.Simple_Titlecase_Mapping:
                    case UProperty.Simple_Uppercase_Mapping:
                    case UProperty.Titlecase_Mapping:
                    case UProperty.Uppercase_Mapping:
                        return UPropertySource.Case;

#pragma warning disable 612, 618
                    case UPropertyConstants.ISO_Comment:
                    case UProperty.Name:
                    case UPropertyConstants.Unicode_1_Name:
#pragma warning restore 612, 618
                        return UPropertySource.Names;

                    default:
                        return UPropertySource.None;
                }
            }
            else
            {
                switch (which)
                {
                    case UProperty.Script_Extensions:
                        return UPropertySource.PropertiesVectorsTrie;
                    default:
                        return UPropertySource.None; /* undefined */
                }
            }
        }

        ///// <summary>
        ///// Unicode property names and property value names are compared
        ///// "loosely". Property[Value]Aliases.txt say:
        ///// <quote>
        /////   "With loose matching of property names, the case distinctions,
        /////   whitespace, and '_' are ignored."
        ///// </quote>
        ///// <para/>
        ///// This function does just that, for ASCII (char *) name strings.
        ///// It is almost identical to ucnv_compareNames() but also ignores
        ///// ASCII White_Space characters (U+0009..U+000d).
        ///// </summary>
        ///// <param name="name1">Name to compare.</param>
        ///// <param name="name2">Name to compare.</param>
        ///// <returns>0 if names are equal, &lt; 0 if name1 is less than name2 and &gt; 0
        ///// if name1 is greater than name2.</returns>
        //// to be implemented in 2.4
        //public static int ComparePropertyNames(string name1, string name2)
        //{
        //    int result = 0;
        //    int i1 = 0;
        //    int i2 = 0;
        //    while (true) {
        //        char ch1 = (char)0;
        //        char ch2 = (char)0;
        //        // Ignore delimiters '-', '_', and ASCII White_Space
        //        if (i1 < name1.Length) {
        //            ch1 = name1[i1++];
        //        }
        //        while (ch1 == '-' || ch1 == '_' || ch1 == ' ' || ch1 == '\t'
        //               || ch1 == '\n' // synwee what is || ch1 == '\v'
        //               || ch1 == '\f' || ch1=='\r') {
        //            if (i1 < name1.Length) {
        //                ch1 = name1[i1++];
        //            }
        //            else {
        //                ch1 = (char)0;
        //            }
        //        }
        //        if (i2 < name2.Length) {
        //            ch2 = name2[i2++];
        //        }
        //        while (ch2 == '-' || ch2 == '_' || ch2 == ' ' || ch2 == '\t'
        //               || ch2 == '\n' // synwee what is || ch1 == '\v'
        //               || ch2 == '\f' || ch2=='\r') {
        //            if (i2 < name2.Length) {
        //                ch2 = name2[i2++];
        //            }
        //            else {
        //                ch2 = (char)0;
        //            }
        //        }

        //        // If we reach the ends of both strings then they match
        //        if (ch1 == 0 && ch2 == 0) {
        //            return 0;
        //        }

        //        // Case-insensitive comparison
        //        if (ch1 != ch2) {
        //            result = Character.ToLower(ch1)
        //                                            - Character.ToLower(ch2);
        //            if (result != 0) {
        //                return result;
        //            }
        //        }
        //    }
        //}


        /// <summary>
        /// Get the the maximum values for some enum/int properties.
        /// </summary>
        /// <param name="column"></param>
        /// <returns>Maximum values for the integer properties.</returns>
        public int GetMaxValues(int column)
        {
            // return m_maxBlockScriptValue_;

            switch (column)
            {
                case 0:
                    return m_maxBlockScriptValue_;
                case 2:
                    return m_maxJTGValue_;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Gets the type mask.
        /// </summary>
        /// <param name="type">Character type.</param>
        /// <returns>Mask.</returns>
        public static int GetMask(UUnicodeCategory type)
        {
            return 1 << (int)type;
        }

        /// <summary>
        /// Returns the digit values of characters like 'A' - 'Z', normal,
        /// half-width and full-width. This method assumes that the other digit
        /// characters are checked by the calling method.
        /// </summary>
        /// <param name="ch">Character to test.</param>
        /// <returns>-1 if ch is not a character of the form 'A' - 'Z', otherwise
        /// its corresponding digit will be returned.</returns>
        public static int GetEuropeanDigit(int ch)
        {
            if ((ch > 0x7a && ch < 0xff21)
                || ch < 0x41 || (ch > 0x5a && ch < 0x61)
                || ch > 0xff5a || (ch > 0xff3a && ch < 0xff41))
            {
                return -1;
            }
            if (ch <= 0x7a)
            {
                // ch >= 0x41 or ch < 0x61
                return ch + 10 - ((ch <= 0x5a) ? 0x41 : 0x61);
            }
            // ch >= 0xff21
            if (ch <= 0xff3a)
            {
                return ch + 10 - 0xff21;
            }
            // ch >= 0xff41 && ch <= 0xff5a
            return ch + 10 - 0xff41;
        }

        public int Digit(int c)
        {
            int value = GetNumericTypeValue(GetProperty(c)) - NTV_DECIMAL_START_;
            if (value <= 9)
            {
                return value;
            }
            else
            {
                return -1;
            }
        }

        public int GetNumericValue(int c)
        {
            // slightly pruned version of getUnicodeNumericValue(), plus getEuropeanDigit()
            int ntv = GetNumericTypeValue(GetProperty(c));

            if (ntv == NTV_NONE_)
            {
                return GetEuropeanDigit(c);
            }
            else if (ntv < NTV_DIGIT_START_)
            {
                /* decimal digit */
                return ntv - NTV_DECIMAL_START_;
            }
            else if (ntv < NTV_NUMERIC_START_)
            {
                /* other digit */
                return ntv - NTV_DIGIT_START_;
            }
            else if (ntv < NTV_FRACTION_START_)
            {
                /* small integer */
                return ntv - NTV_NUMERIC_START_;
            }
            else if (ntv < NTV_LARGE_START_)
            {
                /* fraction */
                return -2;
            }
            else if (ntv < NTV_BASE60_START_)
            {
                /* large, single-significant-digit integer */
                int mant = (ntv >> 5) - 14;
                int exp = (ntv & 0x1f) + 2;
                if (exp < 9 || (exp == 9 && mant <= 2))
                {
                    int numValue = mant;
                    do
                    {
                        numValue *= 10;
                    } while (--exp > 0);
                    return numValue;
                }
                else
                {
                    return -2;
                }
            }
            else if (ntv < NTV_FRACTION20_START_)
            {
                /* sexagesimal (base 60) integer */
                int numValue = (ntv >> 2) - 0xbf;
                int exp = (ntv & 3) + 1;

                switch (exp)
                {
                    case 4:
                        numValue *= 60 * 60 * 60 * 60;
                        break;
                    case 3:
                        numValue *= 60 * 60 * 60;
                        break;
                    case 2:
                        numValue *= 60 * 60;
                        break;
                    case 1:
                        numValue *= 60;
                        break;
                    case 0:
                    default:
                        break;
                }

                return numValue;
            }
            else if (ntv < NTV_RESERVED_START_)
            {
                // fraction-20 e.g. 3/80
                return -2;
            }
            else
            {
                /* reserved */
                return -2;
            }
        }

        public double GetUnicodeNumericValue(int c)
        {
            // equivalent to c version double u_getNumericValue(UChar32 c)
            int ntv = GetNumericTypeValue(GetProperty(c));

            if (ntv == NTV_NONE_)
            {
                return UChar.NoNumericValue;
            }
            else if (ntv < NTV_DIGIT_START_)
            {
                /* decimal digit */
                return ntv - NTV_DECIMAL_START_;
            }
            else if (ntv < NTV_NUMERIC_START_)
            {
                /* other digit */
                return ntv - NTV_DIGIT_START_;
            }
            else if (ntv < NTV_FRACTION_START_)
            {
                /* small integer */
                return ntv - NTV_NUMERIC_START_;
            }
            else if (ntv < NTV_LARGE_START_)
            {
                /* fraction */
                int numerator = (ntv >> 4) - 12;
                int denominator = (ntv & 0xf) + 1;
                return (double)numerator / denominator;
            }
            else if (ntv < NTV_BASE60_START_)
            {
                /* large, single-significant-digit integer */
                double numValue;
                int mant = (ntv >> 5) - 14;
                int exp = (ntv & 0x1f) + 2;
                numValue = mant;

                /* multiply by 10^exp without math.h */
                while (exp >= 4)
                {
                    numValue *= 10000;
                    exp -= 4;
                }
                switch (exp)
                {
                    case 3:
                        numValue *= 1000;
                        break;
                    case 2:
                        numValue *= 100;
                        break;
                    case 1:
                        numValue *= 10;
                        break;
                    case 0:
                    default:
                        break;
                }

                return numValue;
            }
            else if (ntv < NTV_FRACTION20_START_)
            {
                /* sexagesimal (base 60) integer */
                int numValue = (ntv >> 2) - 0xbf;
                int exp = (ntv & 3) + 1;

                switch (exp)
                {
                    case 4:
                        numValue *= 60 * 60 * 60 * 60;
                        break;
                    case 3:
                        numValue *= 60 * 60 * 60;
                        break;
                    case 2:
                        numValue *= 60 * 60;
                        break;
                    case 1:
                        numValue *= 60;
                        break;
                    case 0:
                    default:
                        break;
                }

                return numValue;
            }
            else if (ntv < NTV_RESERVED_START_)
            {
                // fraction-20 e.g. 3/80
                int frac20 = ntv - NTV_FRACTION20_START_;  // 0..0x17
                int numerator = 2 * (frac20 & 3) + 1;
                int denominator = 20 << (frac20 >> 2);
                return (double)numerator / denominator;
            }
            else
            {
                /* reserved */
                return UChar.NoNumericValue;
            }
        }

        // protected variables -----------------------------------------------

        /// <summary>
        /// Extra property trie
        /// </summary>
        private Trie2_16 m_additionalTrie_;
        /// <summary>
        /// Extra property vectors, 1st column for age and second for binary
        /// properties.
        /// </summary>
        private int[] m_additionalVectors_;
        /// <summary>
        /// Number of additional columns
        /// </summary>
        private int m_additionalColumnsCount_;
        /// <summary>
        /// Maximum values for block, bits used as in vector word
        /// 0
        /// </summary>
        private int m_maxBlockScriptValue_;
        /// <summary>
        /// Maximum values for script, bits used as in vector word
        /// 0
        /// </summary>
        private int m_maxJTGValue_;

        /// <summary>
        /// Script_Extensions data
        /// </summary>
        public char[] m_scriptExtensions_; // ICU4N TODO: API - make property

        // private variables -------------------------------------------------

        /// <summary>
        /// Default name of the datafile
        /// </summary>
        private const string DATA_FILE_NAME_ = "uprops.icu";

        // property data constants -------------------------------------------------

        /// <summary>
        /// Numeric types and values in the main properties words.
        /// </summary>
        private const int NUMERIC_TYPE_VALUE_SHIFT_ = 6;
        private static int GetNumericTypeValue(int props)
        {
            return props >> NUMERIC_TYPE_VALUE_SHIFT_;
        }
        /* constants for the storage form of numeric types and values */
        /// <summary> No numeric value.</summary>
        private const int NTV_NONE_ = 0;
        /// <summary>Decimal digits: nv=0..9</summary>
        private const int NTV_DECIMAL_START_ = 1;
        /// <summary>Other digits: nv=0..9</summary>
        private const int NTV_DIGIT_START_ = 11;
        /// <summary>Small integers: nv=0..154</summary>
        private const int NTV_NUMERIC_START_ = 21;
        /// <summary>Fractions: ((ntv>>4)-12) / ((ntv&amp;0xf)+1) = -1..17 / 1..16</summary>
        private const int NTV_FRACTION_START_ = 0xb0;

        /// <summary>
        /// Large integers:
        /// <code>
        /// ((ntv>>5)-14) * 10^((ntv&amp;0x1f)+2) = (1..9)*(10^2..10^33)
        /// (only one significant decimal digit)
        /// </code>
        /// </summary>
        private const int NTV_LARGE_START_ = 0x1e0;
        /// <summary>
        /// Sexagesimal numbers:
        /// <code>
        /// ((ntv>>2)-0xbf) * 60^((ntv&amp;3)+1) = (1..9)*(60^1..60^4)
        /// </code>
        /// </summary>
        private const int NTV_BASE60_START_ = 0x300;
        /// <summary>
        /// Fraction-20 values:
        /// <code>
        /// frac20 = ntv-0x324 = 0..0x17 -> 1|3|5|7 / 20|40|80|160|320|640
        /// numerator: num = 2*(frac20&amp;3)+1
        /// denominator: den = 20&lt;&lt;(frac20>>2)
        /// </code>
        /// </summary>
        private const int NTV_FRACTION20_START_ = NTV_BASE60_START_ + 36;  // 0x300+9*4=0x324
                                                                                     /** No numeric value (yet). */
        private const int NTV_RESERVED_START_ = NTV_FRACTION20_START_ + 24;  // 0x324+6*4=0x34c

        private static int NtvGetType(int ntv)
        {
            return
                (ntv == NTV_NONE_) ? NumericType.None :
                (ntv < NTV_DIGIT_START_) ? NumericType.Decimal :
                (ntv < NTV_NUMERIC_START_) ? NumericType.Digit :
                NumericType.Numeric;
        }

        /*
         * Properties in vector word 0
         * Bits
         * 31..24   DerivedAge version major/minor one nibble each
         * 23..22   3..1: Bits 7..0 = Script_Extensions index
         *             3: Script value from Script_Extensions
         *             2: Script=Inherited
         *             1: Script=Common
         *             0: Script=bits 7..0
         * 21..20   reserved
         * 19..17   East Asian Width
         * 16.. 8   UBlockCode
         *  7.. 0   UScriptCode
         */

        /// <summary>
        /// Script_Extensions: mask includes Script
        /// </summary>
        public const int ScriptXMask = 0x00c000ff;
        //private const int SCRIPT_X_SHIFT = 22;
        /// <summary>
        /// Integer properties mask and shift values for East Asian cell width.
        /// Equivalent to icu4c UPROPS_EA_MASK
        /// </summary>
        private const int EAST_ASIAN_MASK_ = 0x000e0000;
        /// <summary>
        /// Integer properties mask and shift values for East Asian cell width.
        /// Equivalent to icu4c UPROPS_EA_SHIFT
        /// </summary>
        private const int EAST_ASIAN_SHIFT_ = 17;
        /// <summary>
        /// Integer properties mask and shift values for blocks.
        /// Equivalent to icu4c UPROPS_BLOCK_MASK
        /// </summary>
        private const int BLOCK_MASK_ = 0x0001ff00;
        /// <summary>
        /// Integer properties mask and shift values for blocks.
        /// Equivalent to icu4c UPROPS_BLOCK_SHIFT
        /// </summary>
        private const int BLOCK_SHIFT_ = 8;
        /// <summary>
        /// Integer properties mask and shift values for scripts.
        /// Equivalent to icu4c UPROPS_SHIFT_MASK
        /// </summary>
        public const int ScriptMask = 0x000000ff;

        /* ScriptXWithCommon must be the lowest value that involves Script_Extensions. */
        public const int ScriptXWithCommon = 0x400000;
        public const int ScriptXWithInherited = 0x800000;
        public const int ScriptXWithOther = 0xc00000;

        /**
         * Additional properties used in internal trie data
         */
        /*
         * Properties in vector word 1
         * Each bit encodes one binary property.
         * The following constants represent the bit number, use 1<<UPROPS_XYZ.
         * UPROPS_BINARY_1_TOP<=32!
         *
         * Keep this list of property enums in sync with
         * propListNames[] in icu/source/tools/genprops/props2.c!
         *
         * ICU 2.6/uprops format version 3.2 stores full properties instead of "Other_".
         */
        private const int WHITE_SPACE_PROPERTY_ = 0;
        private const int DASH_PROPERTY_ = 1;
        private const int HYPHEN_PROPERTY_ = 2;
        private const int QUOTATION_MARK_PROPERTY_ = 3;
        private const int TERMINAL_PUNCTUATION_PROPERTY_ = 4;
        private const int MATH_PROPERTY_ = 5;
        private const int HEX_DIGIT_PROPERTY_ = 6;
        private const int ASCII_HEX_DIGIT_PROPERTY_ = 7;
        private const int ALPHABETIC_PROPERTY_ = 8;
        private const int IDEOGRAPHIC_PROPERTY_ = 9;
        private const int DIACRITIC_PROPERTY_ = 10;
        private const int EXTENDER_PROPERTY_ = 11;
        private const int NONCHARACTER_CODE_POINT_PROPERTY_ = 12;
        private const int GRAPHEME_EXTEND_PROPERTY_ = 13;
        private const int GRAPHEME_LINK_PROPERTY_ = 14;
        private const int IDS_BINARY_OPERATOR_PROPERTY_ = 15;
        private const int IDS_TRINARY_OPERATOR_PROPERTY_ = 16;
        private const int RADICAL_PROPERTY_ = 17;
        private const int UNIFIED_IDEOGRAPH_PROPERTY_ = 18;
        private const int DEFAULT_IGNORABLE_CODE_POINT_PROPERTY_ = 19;
        private const int DEPRECATED_PROPERTY_ = 20;
        private const int LOGICAL_ORDER_EXCEPTION_PROPERTY_ = 21;
        private const int XID_START_PROPERTY_ = 22;
        private const int XID_CONTINUE_PROPERTY_ = 23;
        private const int ID_START_PROPERTY_ = 24;
        private const int ID_CONTINUE_PROPERTY_ = 25;
        private const int GRAPHEME_BASE_PROPERTY_ = 26;
        private const int S_TERM_PROPERTY_ = 27;
        private const int VARIATION_SELECTOR_PROPERTY_ = 28;
        private const int PATTERN_SYNTAX = 29;                   /* new in ICU 3.4 and Unicode 4.1 */
        private const int PATTERN_WHITE_SPACE = 30;
        private const int PREPENDED_CONCATENATION_MARK = 31;     // new in ICU 60 and Unicode 10

        /*
         * Properties in vector word 2
         * Bits
         * 31..27   http://www.unicode.org/reports/tr51/#Emoji_Properties
         *     26   reserved
         * 25..20   Line Break
         * 19..15   Sentence Break
         * 14..10   Word Break
         *  9.. 5   Grapheme Cluster Break
         *  4.. 0   Decomposition Type
         */
        private const int PROPS_2_EMOJI_COMPONENT = 27;
        private const int PROPS_2_EMOJI = 28;
        private const int PROPS_2_EMOJI_PRESENTATION = 29;
        private const int PROPS_2_EMOJI_MODIFIER = 30;
        private const int PROPS_2_EMOJI_MODIFIER_BASE = 31;

        private const int LB_MASK = 0x03f00000;
        private const int LB_SHIFT = 20;

        private const int SB_MASK = 0x000f8000;
        private const int SB_SHIFT = 15;

        private const int WB_MASK = 0x00007c00;
        private const int WB_SHIFT = 10;

        private const int GCB_MASK = 0x000003e0;
        private const int GCB_SHIFT = 5;

        /// <summary>
        /// Integer properties mask for decomposition type.
        /// Equivalent to icu4c UPROPS_DT_MASK.
        /// </summary>
        private const int DECOMPOSITION_TYPE_MASK_ = 0x0000001f;

        /// <summary>
        /// First nibble shift
        /// </summary>
        private const int FIRST_NIBBLE_SHIFT_ = 0x4;
        /// <summary>
        /// Second nibble mask
        /// </summary>
        private const int LAST_NIBBLE_MASK_ = 0xF;
        /// <summary>
        /// Age value shift
        /// </summary>
        private const int AGE_SHIFT_ = 24;


        // private constructors --------------------------------------------------

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <exception cref="IOException">Thrown when data reading fails or data corrupted.</exception>
        private UCharacterProperty()
        {
            Init();

            // consistency check
#pragma warning disable 612, 618
            if (binProps.Length != (int)UPropertyConstants.Binary_Limit)
            {
                throw new ICUException("binProps.length!=UProperty.BINARY_LIMIT");
            }
            if (intProps.Length != ((int)UPropertyConstants.Int_Limit - (int)UPropertyConstants.Int_Start))
            {
                throw new ICUException("intProps.length!=(UProperty.INT_LIMIT-UProperty.INT_START)");
            }
#pragma warning restore 612, 618

            // jar access
            ByteBuffer bytes = ICUBinary.GetRequiredData(DATA_FILE_NAME_);
            m_unicodeVersion_ = ICUBinary.ReadHeaderAndDataVersion(bytes, DATA_FORMAT, new IsAcceptable());
            // Read or skip the 16 indexes.
            int propertyOffset = bytes.GetInt32();
            /* exceptionOffset = */
            bytes.GetInt32();
            /* caseOffset = */
            bytes.GetInt32();
            int additionalOffset = bytes.GetInt32();
            int additionalVectorsOffset = bytes.GetInt32();
            m_additionalColumnsCount_ = bytes.GetInt32();
            int scriptExtensionsOffset = bytes.GetInt32();
            int reservedOffset7 = bytes.GetInt32();
            /* reservedOffset8 = */
            bytes.GetInt32();
            /* dataTopOffset = */
            bytes.GetInt32();
            m_maxBlockScriptValue_ = bytes.GetInt32();
            m_maxJTGValue_ = bytes.GetInt32();
            ICUBinary.SkipBytes(bytes, (16 - 12) << 2);

            // read the main properties trie
            m_trie_ = Trie2_16.CreateFromSerialized(bytes);
            int expectedTrieLength = (propertyOffset - 16) * 4;
            int trieLength = m_trie_.SerializedLength;
            if (trieLength > expectedTrieLength)
            {
                throw new IOException("uprops.icu: not enough bytes for main trie");
            }
            // skip padding after trie bytes
            ICUBinary.SkipBytes(bytes, expectedTrieLength - trieLength);

            // skip unused intervening data structures
            ICUBinary.SkipBytes(bytes, (additionalOffset - propertyOffset) * 4);

            if (m_additionalColumnsCount_ > 0)
            {
                // reads the additional property block
                m_additionalTrie_ = Trie2_16.CreateFromSerialized(bytes);
                expectedTrieLength = (additionalVectorsOffset - additionalOffset) * 4;
                trieLength = m_additionalTrie_.SerializedLength;
                if (trieLength > expectedTrieLength)
                {
                    throw new IOException("uprops.icu: not enough bytes for additional-properties trie");
                }
                // skip padding after trie bytes
                ICUBinary.SkipBytes(bytes, expectedTrieLength - trieLength);

                // additional properties
                int size = scriptExtensionsOffset - additionalVectorsOffset;
                m_additionalVectors_ = ICUBinary.GetInt32s(bytes, size, 0);
            }

            // Script_Extensions
            int numChars = (reservedOffset7 - scriptExtensionsOffset) * 2;
            if (numChars > 0)
            {
                m_scriptExtensions_ = ICUBinary.GetChars(bytes, numChars, 0);
            }
        }

        private sealed class IsAcceptable : IAuthenticate
        {
            // @Override when we switch to Java 6
            public bool IsDataVersionAcceptable(byte[] version)
            {
                return version[0] == 7;
            }
        }
        private const int DATA_FORMAT = 0x5550726F;  // "UPro"

        // private methods -------------------------------------------------------

        /*
         * Compare additional properties to see if it has argument type
         * @param property 32 bit properties
         * @param type character type
         * @return true if property has type
         */
        /*private boolean compareAdditionalType(int property, int type)
        {
            return (property & (1 << type)) != 0;
        }*/

        // property starts for UnicodeSet -------------------------------------- ***

        private const int TAB = 0x0009;
        //private const int LF      = 0x000a;
        //private const int FF      = 0x000c;
        private const int CR = 0x000d;
        private const int U_A = 0x0041;
        private const int U_F = 0x0046;
        private const int U_Z = 0x005a;
        private const int U_a = 0x0061;
        private const int U_f = 0x0066;
        private const int U_z = 0x007a;
        private const int DEL = 0x007f;
        private const int NL = 0x0085;
        private const int NBSP = 0x00a0;
        private const int CGJ = 0x034f;
        private const int FIGURESP = 0x2007;
        private const int HAIRSP = 0x200a;
        //private const int ZWNJ    = 0x200c;
        //private const int ZWJ     = 0x200d;
        private const int RLM = 0x200f;
        private const int NNBSP = 0x202f;
        private const int WJ = 0x2060;
        private const int INHSWAP = 0x206a;
        private const int NOMDIG = 0x206f;
        private const int U_FW_A = 0xff21;
        private const int U_FW_F = 0xff26;
        private const int U_FW_Z = 0xff3a;
        private const int U_FW_a = 0xff41;
        private const int U_FW_f = 0xff46;
        private const int U_FW_z = 0xff5a;
        private const int ZWNBSP = 0xfeff;

        public UnicodeSet AddPropertyStarts(UnicodeSet set)
        {
            /* add the start code point of each same-value range of the main trie */
            using (var trieIterator = m_trie_.GetEnumerator())
            {
                Trie2Range range;
                while (trieIterator.MoveNext() && !(range = trieIterator.Current).IsLeadSurrogate)
                {
                    set.Add(range.StartCodePoint);
                }
            }

            /* add code points with hardcoded properties, plus the ones following them */

            /* add for u_isblank() */
            set.Add(TAB);
            set.Add(TAB + 1);

            /* add for IS_THAT_CONTROL_SPACE() */
            set.Add(CR + 1); /* range TAB..CR */
            set.Add(0x1c);
            set.Add(0x1f + 1);
            set.Add(NL);
            set.Add(NL + 1);

            /* add for u_isIDIgnorable() what was not added above */
            set.Add(DEL); /* range DEL..NBSP-1, NBSP added below */
            set.Add(HAIRSP);
            set.Add(RLM + 1);
            set.Add(INHSWAP);
            set.Add(NOMDIG + 1);
            set.Add(ZWNBSP);
            set.Add(ZWNBSP + 1);

            /* add no-break spaces for u_isWhitespace() what was not added above */
            set.Add(NBSP);
            set.Add(NBSP + 1);
            set.Add(FIGURESP);
            set.Add(FIGURESP + 1);
            set.Add(NNBSP);
            set.Add(NNBSP + 1);

            /* add for u_charDigitValue() */
            // TODO remove when UChar.getHanNumericValue() is changed to just return
            // Unicode numeric values
            set.Add(0x3007);
            set.Add(0x3008);
            set.Add(0x4e00);
            set.Add(0x4e01);
            set.Add(0x4e8c);
            set.Add(0x4e8d);
            set.Add(0x4e09);
            set.Add(0x4e0a);
            set.Add(0x56db);
            set.Add(0x56dc);
            set.Add(0x4e94);
            set.Add(0x4e95);
            set.Add(0x516d);
            set.Add(0x516e);
            set.Add(0x4e03);
            set.Add(0x4e04);
            set.Add(0x516b);
            set.Add(0x516c);
            set.Add(0x4e5d);
            set.Add(0x4e5e);

            /* add for u_digit() */
            set.Add(U_a);
            set.Add(U_z + 1);
            set.Add(U_A);
            set.Add(U_Z + 1);
            set.Add(U_FW_a);
            set.Add(U_FW_z + 1);
            set.Add(U_FW_A);
            set.Add(U_FW_Z + 1);

            /* add for u_isxdigit() */
            set.Add(U_f + 1);
            set.Add(U_F + 1);
            set.Add(U_FW_f + 1);
            set.Add(U_FW_F + 1);

            /* add for UCHAR_DEFAULT_IGNORABLE_CODE_POINT what was not added above */
            set.Add(WJ); /* range WJ..NOMDIG */
            set.Add(0xfff0);
            set.Add(0xfffb + 1);
            set.Add(0xe0000);
            set.Add(0xe0fff + 1);

            /* add for UCHAR_GRAPHEME_BASE and others */
            set.Add(CGJ);
            set.Add(CGJ + 1);

            return set; // for chaining
        }

        public void upropsvec_addPropertyStarts(UnicodeSet set) // ICU4N TODO: API - rename to use .NET Conventions
        {
            /* add the start code point of each same-value range of the properties vectors trie */
            if (m_additionalColumnsCount_ > 0)
            {
                /* if m_additionalColumnsCount_==0 then the properties vectors trie may not be there at all */
                using (var trieIterator = m_additionalTrie_.GetEnumerator())
                {
                    Trie2Range range;
                    while (trieIterator.MoveNext() && !(range = trieIterator.Current).IsLeadSurrogate)
                    {
                        set.Add(range.StartCodePoint);
                    }
                }
            }
        }

        /// <summary>
        /// Public singleton instance.
        /// </summary>
        public static UCharacterProperty Instance { get; private set; } = LoadSingletonInstance(); // ICU4N: Avoid static constructor by initializing inline

        // This static initializer block must be placed after
        // other static member initialization
        private static UCharacterProperty LoadSingletonInstance()
        {
            try
            {
                return new UCharacterProperty();
            }
            catch (IOException e)
            {
                throw new MissingManifestResourceException(e.ToString(), e);
            }
        }

        /*----------------------------------------------------------------
         * Inclusions list
         *----------------------------------------------------------------*/

        /*
         * Return a set of characters for property enumeration.
         * The set implicitly contains 0x110000 as well, which is one more than the highest
         * Unicode code point.
         *
         * This set is used as an ordered list - its code points are ordered, and
         * consecutive code points (in Unicode code point order) in the set define a range.
         * For each two consecutive characters (start, limit) in the set,
         * all of the UCD/normalization and related properties for
         * all code points start..limit-1 are all the same,
         * except for character names and ISO comments.
         *
         * All Unicode code points U+0000..U+10ffff are covered by these ranges.
         * The ranges define a partition of the Unicode code space.
         * ICU uses the inclusions set to enumerate properties for generating
         * UnicodeSets containing all code points that have a certain property value.
         *
         * The Inclusion List is generated from the UCD. It is generated
         * by enumerating the data tries, and code points for hardcoded properties
         * are added as well.
         *
         * --------------------------------------------------------------------------
         *
         * The following are ideas for getting properties-unique code point ranges,
         * with possible optimizations beyond the current implementation.
         * These optimizations would require more code and be more fragile.
         * The current implementation generates one single list (set) for all properties.
         *
         * To enumerate properties efficiently, one needs to know ranges of
         * repetitive values, so that the value of only each start code point
         * can be applied to the whole range.
         * This information is in principle available in the uprops.icu/unorm.icu data.
         *
         * There are two obstacles:
         *
         * 1. Some properties are computed from multiple data structures,
         *    making it necessary to get repetitive ranges by intersecting
         *    ranges from multiple tries.
         *
         * 2. It is not economical to write code for getting repetitive ranges
         *    that are precise for each of some 50 properties.
         *
         * Compromise ideas:
         *
         * - Get ranges per trie, not per individual property.
         *   Each range contains the same values for a whole group of properties.
         *   This would generate currently five range sets, two for uprops.icu tries
         *   and three for unorm.icu tries.
         *
         * - Combine sets of ranges for multiple tries to get sufficient sets
         *   for properties, e.g., the uprops.icu main and auxiliary tries
         *   for all non-normalization properties.
         *
         * Ideas for representing ranges and combining them:
         *
         * - A UnicodeSet could hold just the start code points of ranges.
         *   Multiple sets are easily combined by or-ing them together.
         *
         * - Alternatively, a UnicodeSet could hold each even-numbered range.
         *   All ranges could be enumerated by using each start code point
         *   (for the even-numbered ranges) as well as each limit (end+1) code point
         *   (for the odd-numbered ranges).
         *   It should be possible to combine two such sets by xor-ing them,
         *   but no more than two.
         *
         * The second way to represent ranges may(?!) yield smaller UnicodeSet arrays,
         * but the first one is certainly simpler and applicable for combining more than
         * two range sets.
         *
         * It is possible to combine all range sets for all uprops/unorm tries into one
         * set that can be used for all properties.
         * As an optimization, there could be less-combined range sets for certain
         * groups of properties.
         * The relationship of which less-combined range set to use for which property
         * depends on the implementation of the properties and must be hardcoded
         * - somewhat error-prone and higher maintenance but can be tested easily
         * by building property sets "the simple way" in test code.
         *
         * ---
         *
         * Do not use a UnicodeSet pattern because that causes infinite recursion;
         * UnicodeSet depends on the inclusions set.
         *
         * ---
         *
         * getInclusions() is commented out starting 2005-feb-12 because
         * UnicodeSet now calls the uxyz_addPropertyStarts() directly,
         * and only for the relevant property source.
         */
        /*
        public UnicodeSet getInclusions() {
            UnicodeSet set = new UnicodeSet();
            NormalizerImpl.addPropertyStarts(set);
            addPropertyStarts(set);
            return set;
        }
        */
    }
}
