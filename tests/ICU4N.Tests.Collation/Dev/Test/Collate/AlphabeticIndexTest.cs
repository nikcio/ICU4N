﻿using ICU4N.Globalization;
using ICU4N.Impl;
using ICU4N.Text;
using J2N;
using J2N.Text;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ICU4N.Dev.Test.Collate
{
    /// <author>Mark Davis</author>
    public class AlphabeticIndexTest : TestFmwk
    {
        private const string ARROW = "\u2192";
        private static readonly bool DEBUG = ICUDebug.Enabled("alphabeticindex");

        public static IList<string> KEY_LOCALES = new List<string>() {
            "en", "es", "de", "fr", "ja", "it", "tr", "pt", "zh", "nl",
            "pl", "ar", "ru", "zh_Hant", "ko", "th", "sv", "fi", "da",
            "he", "nb", "el", "hr", "bg", "sk", "lt", "vi", "lv", "sr",
            "pt_PT", "ro", "hu", "cs", "id", "sl", "fil", "fa", "uk",
            "ca", "hi", "et", "eu", "is", "sw", "ms", "bn", "am", "ta",
            "te", "mr", "ur", "ml", "kn", "gu", "or" };
        private string[][] localeAndIndexCharactersLists = new string[][] {
            /* Arabic*/ new string[] {"ar", "\u0627:\u0628:\u062A:\u062B:\u062C:\u062D:\u062E:\u062F:\u0630:\u0631:\u0632:\u0633:\u0634:\u0635:\u0636:\u0637:\u0638:\u0639:\u063A:\u0641:\u0642:\u0643:\u0644:\u0645:\u0646:\u0647:\u0648:\u064A"},
            /* Bulgarian*/  new string[] {"bg", "\u0410:\u0411:\u0412:\u0413:\u0414:\u0415:\u0416:\u0417:\u0418:\u0419:\u041A:\u041B:\u041C:\u041D:\u041E:\u041F:\u0420:\u0421:\u0422:\u0423:\u0424:\u0425:\u0426:\u0427:\u0428:\u0429:\u042E:\u042F"},
            /* Catalan*/    new string[] {"ca", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:Q:R:S:T:U:V:W:X:Y:Z"},
            /* Czech*/  new string[] {"cs", "A:B:C:\u010C:D:E:F:G:H:CH:I:J:K:L:M:N:O:P:Q:R:\u0158:S:\u0160:T:U:V:W:X:Y:Z:\u017D"},
            /* Danish*/ new string[] {"da", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:Q:R:S:T:U:V:W:X:Y:Z:\u00C6:\u00D8:\u00C5"},
            /* German*/ new string[] {"de", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:Q:R:S:T:U:V:W:X:Y:Z"},
            /* Greek*/  new string[] {"el", "\u0391:\u0392:\u0393:\u0394:\u0395:\u0396:\u0397:\u0398:\u0399:\u039A:\u039B:\u039C:\u039D:\u039E:\u039F:\u03A0:\u03A1:\u03A3:\u03A4:\u03A5:\u03A6:\u03A7:\u03A8:\u03A9"},
            /* English*/    new string[] {"en", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:Q:R:S:T:U:V:W:X:Y:Z"},
            /* Spanish*/    new string[] {"es", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:\u00D1:O:P:Q:R:S:T:U:V:W:X:Y:Z"},
            /* Estonian*/   new string[] {"et", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:Q:R:S:\u0160:Z:\u017D:T:U:V:\u00D5:\u00C4:\u00D6:\u00DC:X:Y"},
            /* Basque*/ new string[] {"eu", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:Q:R:S:T:U:V:W:X:Y:Z"},
            /* Finnish*/    new string[] {"fi", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:Q:R:S:T:U:V:W:X:Y:Z:\u00C5:\u00C4:\u00D6"},
            /* Filipino*/   new string[] {"fil", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:\u00D1:Ng:O:P:Q:R:S:T:U:V:W:X:Y:Z"},
            /* French*/ new string[] {"fr", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:Q:R:S:T:U:V:W:X:Y:Z"},
            /* Hebrew*/ new string[] {"he", "\u05D0:\u05D1:\u05D2:\u05D3:\u05D4:\u05D5:\u05D6:\u05D7:\u05D8:\u05D9:\u05DB:\u05DC:\u05DE:\u05E0:\u05E1:\u05E2:\u05E4:\u05E6:\u05E7:\u05E8:\u05E9:\u05EA"},
            /* Icelandic*/  new string[] {"is", "A:\u00C1:B:C:D:\u00D0:E:\u00C9:F:G:H:I:\u00CD:J:K:L:M:N:O:\u00D3:P:Q:R:S:T:U:\u00DA:V:W:X:Y:\u00DD:Z:\u00DE:\u00C6:\u00D6"},
            /* Italian*/    new string[] {"it", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:Q:R:S:T:U:V:W:X:Y:Z"},
            /* Japanese*/   new string[] {"ja", "\u3042:\u304B:\u3055:\u305F:\u306A:\u306F:\u307E:\u3084:\u3089:\u308F"},
            /* Korean*/ new string[] {"ko", "\u3131:\u3134:\u3137:\u3139:\u3141:\u3142:\u3145:\u3147:\u3148:\u314A:\u314B:\u314C:\u314D:\u314E"},
            /* Lithuanian*/ new string[] {"lt", "A:B:C:\u010C:D:E:F:G:H:I:J:K:L:M:N:O:P:R:S:\u0160:T:U:V:Z:\u017D"},
            /* Latvian*/    new string[] {"lv", "A:B:C:\u010C:D:E:F:G:\u0122:H:I:J:K:\u0136:L:\u013B:M:N:\u0145:O:P:Q:R:S:\u0160:T:U:V:W:X:Z:\u017D"},
            /* Norwegian Bokm\u00E5l*/  new string[] {"nb", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:Q:R:S:T:U:V:W:X:Y:Z:\u00C6:\u00D8:\u00C5"},
            /* Dutch*/  new string[] {"nl", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:Q:R:S:T:U:V:W:X:Y:Z"},
            /* Polish*/ new string[] {"pl", "A:\u0104:B:C:\u0106:D:E:\u0118:F:G:H:I:J:K:L:\u0141:M:N:\u0143:O:\u00D3:P:Q:R:S:\u015A:T:U:V:W:X:Y:Z:\u0179:\u017B"},
            /* Portuguese*/ new string[] {"pt", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:Q:R:S:T:U:V:W:X:Y:Z"},
            /* Romanian*/   new string[] {"ro", "A:\u0102:\u00C2:B:C:D:E:F:G:H:I:\u00CE:J:K:L:M:N:O:P:Q:R:S:\u0218:T:\u021A:U:V:W:X:Y:Z"},
            /* Russian*/    new string[] {"ru", "\u0410:\u0411:\u0412:\u0413:\u0414:\u0415:\u0416:\u0417:\u0418:\u0419:\u041A:\u041B:\u041C:\u041D:\u041E:\u041F:\u0420:\u0421:\u0422:\u0423:\u0424:\u0425:\u0426:\u0427:\u0428:\u0429:\u042B:\u042D:\u042E:\u042F"},
            /* Slovak*/ new string[] {"sk", "A:\u00C4:B:C:\u010C:D:E:F:G:H:CH:I:J:K:L:M:N:O:\u00D4:P:Q:R:S:\u0160:T:U:V:W:X:Y:Z:\u017D"},
            /* Slovenian*/  new string[] {"sl", "A:B:C:\u010C:\u0106:D:\u0110:E:F:G:H:I:J:K:L:M:N:O:P:Q:R:S:\u0160:T:U:V:W:X:Y:Z:\u017D"},
            /* Serbian*/    new string[] {"sr", "\u0410:\u0411:\u0412:\u0413:\u0414:\u0402:\u0415:\u0416:\u0417:\u0418:\u0408:\u041A:\u041B:\u0409:\u041C:\u041D:\u040A:\u041E:\u041F:\u0420:\u0421:\u0422:\u040B:\u0423:\u0424:\u0425:\u0426:\u0427:\u040F:\u0428"},
            /* Swedish*/    new string[] {"sv", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:Q:R:S:T:U:V:W:X:Y:Z:\u00C5:\u00C4:\u00D6"},
            /* Turkish*/    new string[] {"tr", "A:B:C:\u00C7:D:E:F:G:H:I:\u0130:J:K:L:M:N:O:\u00D6:P:Q:R:S:\u015E:T:U:\u00DC:V:W:X:Y:Z"},
            /* Ukrainian*/  new string[] {"uk", "\u0410:\u0411:\u0412:\u0413:\u0490:\u0414:\u0415:\u0404:\u0416:\u0417:\u0418:\u0406:\u0407:\u0419:\u041A:\u041B:\u041C:\u041D:\u041E:\u041F:\u0420:\u0421:\u0422:\u0423:\u0424:\u0425:\u0426:\u0427:\u0428:\u0429:\u042E:\u042F"},
            /* Vietnamese*/ new string[] {"vi", "A:\u0102:\u00C2:B:C:D:\u0110:E:\u00CA:F:G:H:I:J:K:L:M:N:O:\u00D4:\u01A0:P:Q:R:S:T:U:\u01AF:V:W:X:Y:Z"},
            /* Chinese*/    new string[] {"zh", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:Q:R:S:T:U:V:W:X:Y:Z"},
            /* Chinese (Traditional Han)*/  new string[] {"zh_Hant", "1\u5283:2\u5283:3\u5283:4\u5283:5\u5283:6\u5283:7\u5283:8\u5283:9\u5283:10\u5283:11\u5283:12\u5283:13\u5283:14\u5283:15\u5283:16\u5283:17\u5283:18\u5283:19\u5283:20\u5283:21\u5283:22\u5283:23\u5283:24\u5283:25\u5283:26\u5283:27\u5283:28\u5283:29\u5283:30\u5283:31\u5283:32\u5283:33\u5283:35\u5283:36\u5283:39\u5283:48\u5283"},

            // Comment these out to make the test run faster. Later, make these run under extended

            //            /* Afrikaans*/  new string[] {"af", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:Q:R:S:T:U:V:W:X:Y:Z"},
            //            /* Akan*/   new string[] {"ak", "A:B:C:D:E:\u0190:F:G:H:I:J:K:L:M:N:O:\u0186:P:Q:R:S:T:U:V:W:X:Y:Z"},
            //            /* Asu*/    new string[] {"asa", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:R:S:T:U:V:W:Y:Z"},
            //            /* Azerbaijani*/    new string[] {"az", "A:B:C:\u00C7:D:E:\u018F:F:G:\u011E:H:X:I:\u0130:J:K:Q:L:M:N:O:\u00D6:P:R:S:\u015E:T:U:\u00DC:V:W:Y:Z"},
            //            /* Belarusian*/ new string[] {"be", "\u0410:\u0411:\u0412:\u0413:\u0414:\u0415:\u0416:\u0417:\u0406:\u0419:\u041A:\u041B:\u041C:\u041D:\u041E:\u041F:\u0420:\u0421:\u0422:\u0423:\u0424:\u0425:\u0426:\u0427:\u0428:\u042B:\u042D:\u042E:\u042F"},
            //            /* Bemba*/  new string[] {"bem", "A:B:C:E:F:G:I:J:K:L:M:N:O:P:S:T:U:W:Y"},
            //            /* Bena*/   new string[] {"bez", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:Q:R:S:T:U:V:W:Y:Z"},
            //            /* Bambara*/    new string[] {"bm", "A:B:C:D:E:\u0190:F:G:H:I:J:K:L:M:N:\u019D:\u014A:O:\u0186:P:R:S:T:U:W:Y:Z"},
            //            /* Tibetan*/    new string[] {"bo", "\u0F40:\u0F41:\u0F42:\u0F44:\u0F45:\u0F46:\u0F47:\u0F49:\u0F4F:\u0F50:\u0F51:\u0F53:\u0F54:\u0F55:\u0F56:\u0F58:\u0F59:\u0F5A:\u0F5B:\u0F5D:\u0F5E:\u0F5F:\u0F60:\u0F61:\u0F62:\u0F63:\u0F64:\u0F66:\u0F67:\u0F68"},
            //            /* Chiga*/  new string[] {"cgg", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:Q:R:S:T:U:V:W:X:Y:Z"},
            //            /* Cherokee*/   new string[] {"chr", "\u13A0:\u13A6:\u13AD:\u13B3:\u13B9:\u13BE:\u13C6:\u13CC:\u13D3:\u13DC:\u13E3:\u13E9:\u13EF"},
            //            /* Welsh*/  new string[] {"cy", "A:B:C:CH:D:E:F:FF:G:H:I:J:L:LL:M:N:O:P:PH:R:RH:S:T:TH:U:W:Y"},
            //            /* Taita*/  new string[] {"dav", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:R:S:T:U:V:W:Y:Z"},
            //            /* Embu*/   new string[] {"ebu", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:Q:R:S:T:U:V:W:X:Y:Z"},
            //            /* Ewe*/    new string[] {"ee", "A:B:C:D:\u0189:E:\u0190:F:\u0191:G:\u0194:H:I:J:K:L:M:N:\u014A:O:\u0186:P:Q:R:S:T:U:V:\u01B2:W:X:Y:Z"},
            //            /* Esperanto*/  new string[] {"eo", "A:B:C:\u0108:D:E:F:G:\u011C:H:\u0124:I:J:\u0134:K:L:M:N:O:P:R:S:\u015C:T:U:\u016C:V:Z"},
            //            /* Fulah*/  new string[] {"ff", "A:B:\u0181:C:D:\u018A:E:F:G:H:I:J:K:L:M:N:\u014A:O:P:R:S:T:U:W:Y:\u01B3"},
            //            /* Faroese*/    new string[] {"fo", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:Q:R:S:T:U:V:W:X:Y:Z:\u00C6:\u00D8"},
            //            /* Gusii*/  new string[] {"guz", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:R:S:T:U:V:W:Y:Z"},
            //            /* Hausa*/  new string[] {"ha", "A:B:\u0181:C:D:\u018A:E:F:G:H:I:J:K:\u0198:L:M:N:O:P:Q:R:S:T:U:V:W:X:Y:Z"},
            //            /* Igbo*/   new string[] {"ig", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:Q:R:S:T:U:V:W:X:Y:Z"},
            //            /* Machame*/    new string[] {"jmc", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:R:S:T:U:V:W:Y:Z"},
            //            /* Kabyle*/ new string[] {"kab", "A:B:C:D:E:\u0190:F:G:\u0194:H:I:J:K:L:M:N:P:Q:R:S:T:U:W:X:Y:Z"},
            //            /* Kamba*/  new string[] {"kam", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:Q:R:S:T:U:V:W:X:Y:Z"},
            //            /* Makonde*/    new string[] {"kde", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:Q:R:S:T:U:V:W:X:Y:Z"},
            //            /* Kabuverdianu*/   new string[] {"kea", "A:B:D:E:F:G:H:I:J:K:L:M:N:O:P:R:S:T:U:V:X:Z"},
            //            /* Koyra Chiini*/   new string[] {"khq", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:\u019D:\u014A:O:P:Q:R:S:T:U:W:X:Y:Z"},
            //            /* Kikuyu*/ new string[] {"ki", "A:B:C:D:E:G:H:I:J:K:M:N:O:R:T:U:W:Y"},
            //            /* Kalenjin*/   new string[] {"kln", "A:B:C:D:E:G:H:I:J:K:L:M:N:O:P:R:S:T:U:W:Y"},
            //            /* Langi*/  new string[] {"lag", "A:B:C:D:E:F:G:H:I:\u0197:J:K:L:M:N:O:P:Q:R:S:T:U:\u0244:V:W:X:Y:Z"},
            //            /* Ganda*/  new string[] {"lg", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:Q:R:S:T:U:V:W:X:Y:Z"},
            //            /* Luo*/    new string[] {"luo", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:R:S:T:U:V:W:Y"},
            //            /* Luyia*/  new string[] {"luy", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:Q:R:S:T:U:V:W:X:Y:Z"},
            //            /* Masai*/  new string[] {"mas", "A:B:C:D:E:\u0190:G:H:I:\u0197:J:K:L:M:N:\u014A:O:\u0186:P:R:S:T:U:\u0244:W:Y"},
            //            /* Meru*/   new string[] {"mer", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:Q:R:S:T:U:V:W:X:Y:Z"},
            //            /* Morisyen*/   new string[] {"mfe", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:R:S:T:U:V:W:X:Y:Z"},
            //            /* Malagasy*/   new string[] {"mg", "A:B:D:E:F:G:H:I:J:K:L:M:N:O:P:R:S:T:V:Y:Z"},
            // This should be the correct data.  Commented till it is fixed in CLDR collation data.
            // new string[] {"mk", "\u0410:\u0411:\u0412:\u0413:\u0403:\u0414:\u0415:\u0416:\u0417:\u0405:\u0418:\u0408:\u041A:\u040C:\u041B:\u0409:\u041C:\u041D:\u040A:\u041E:\u041F:\u0420:\u0421:\u0422:\u0423:\u0424:\u0425:\u0426:\u0427:\u040F:\u0428"},
            //            /* Macedonian*/ new string[] {"mk", "\u0410:\u0411:\u0412:\u0413:\u0414:\u0403:\u0415:\u0416:\u0417:\u0405:\u0418:\u0408:\u041A:\u041B:\u0409:\u041C:\u041D:\u040A:\u041E:\u041F:\u0420:\u0421:\u0422:\u040C:\u0423:\u0424:\u0425:\u0426:\u0427:\u040F:\u0428"},
            // This should be the correct data.  Commented till it is fixed in CLDR collation data.
            // new string[] {"mt", "A:B:C:\u010A:D:E:F:\u0120:G:G\u0126:H:\u0126:I:J:K:L:M:N:O:P:Q:R:S:T:U:V:W:X:Y:\u017B:Z"},
            //            /* Maltese*/    new string[] {"mt", "A:B:\u010A:C:D:E:F:\u0120:G:G\u0126:H:\u0126:I:J:K:L:M:N:O:P:Q:R:S:T:U:V:W:X:Y:\u017B:Z"},
            //            /* Nama*/   new string[] {"naq", "A:B:C:D:E:F:G:H:I:K:M:N:O:P:Q:R:S:T:U:W:X:Y:Z"},
            //            /* North Ndebele*/  new string[] {"nd", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:Q:S:T:U:V:W:X:Y:Z"},
            //            /* Norwegian Nynorsk*/  new string[] {"nn", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:Q:R:S:T:U:V:W:X:Y:Z:\u00C6:\u00D8:\u00C5"},
            //            /* Nyankole*/   new string[] {"nyn", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:Q:R:S:T:U:V:W:X:Y:Z"},
            //            /* Oromo*/  new string[] {"om", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:Q:R:S:T:U:V:W:X:Y:Z"},
            //            /* Romansh*/    new string[] {"rm", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:Q:R:S:T:U:V:W:X:Y:Z"},
            //            /* Rombo*/  new string[] {"rof", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:R:S:T:U:V:W:Y:Z"},
            //            /* Kinyarwanda*/    new string[] {"rw", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:Q:R:S:T:U:V:W:X:Y:Z"},
            //            /* Rwa*/    new string[] {"rwk", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:R:S:T:U:V:W:Y:Z"},
            //            /* Samburu*/    new string[] {"saq", "A:B:C:D:E:G:H:I:J:K:L:M:N:O:P:R:S:T:U:V:W:Y"},
            //            /* Sena*/   new string[] {"seh", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:Q:R:S:T:U:V:W:X:Y:Z"},
            //            /* Koyraboro Senni*/    new string[] {"ses", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:\u019D:\u014A:O:P:Q:R:S:T:U:W:X:Y:Z"},
            //            /* Sango*/  new string[] {"sg", "A:B:D:E:F:G:H:I:J:K:L:M:N:O:P:R:S:T:U:V:W:Y:Z"},
            //            /* Tachelhit*/  new string[] {"shi", "A:B:C:D:E:\u0190:F:G:\u0194:H:I:J:K:L:M:N:Q:R:S:T:U:W:X:Y:Z"},
            //            /* Tachelhit (Tifinagh)*/   new string[] {"shi_Tfng", "\u2D30:\u2D31:\u2D33:\u2D37:\u2D39:\u2D3B:\u2D3C:\u2D3D:\u2D40:\u2D43:\u2D44:\u2D45:\u2D47:\u2D49:\u2D4A:\u2D4D:\u2D4E:\u2D4F:\u2D53:\u2D54:\u2D55:\u2D56:\u2D59:\u2D5A:\u2D5B:\u2D5C:\u2D5F:\u2D61:\u2D62:\u2D63:\u2D65"},
            //            /* Shona*/  new string[] {"sn", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:R:S:T:U:V:W:Y:Z"},
            //            /* Teso*/   new string[] {"teo", "A:B:C:D:E:G:H:I:J:K:L:M:N:O:P:R:S:T:U:V:W:X:Y"},
            //            /* Tonga*/  new string[] {"to", "A:B:C:D:E:F:G:H:\u02BB:I:J:K:L:M:N:O:P:Q:R:S:T:U:V:W:X:Y:Z"},
            //            /* Central Morocco Tamazight*/  new string[] {"tzm", "A:B:C:D:E:\u0190:F:G:\u0194:H:I:J:K:L:M:N:Q:R:S:T:U:W:X:Y:Z"},
            //            /* Uzbek (Latin)*/  new string[] {"uz_Latn", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:Q:R:S:T:U:V:W:X:Y:Z:\u02BF"},
            //            /* Vunjo*/  new string[] {"vun", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:R:S:T:U:V:W:Y:Z"},
            //            /* Soga*/   new string[] {"xog", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:Q:R:S:T:U:V:W:X:Y:Z"},
            //            /* Yoruba*/ new string[] {"yo", "A:B:C:D:E:F:G:H:I:J:K:L:M:N:O:P:Q:R:S:T:U:V:W:X:Y:Z"},

    };

        //    public void TestAAKeyword() {
        //    ICUResourceBundle rb = (ICUResourceBundle) UResourceBundle.getBundleInstance(
        //            ICUResourceBundle.ICU_COLLATION_BASE_NAME, "zh");
        //    showBundle(rb, 0);
        //        String[] keywords = Collator.getKeywords();
        //        System.out.println(Arrays.asList(keywords));
        //        String locale = "zh";
        //        UCultureInfo ulocale = new UCultureInfo(locale);
        //        for (String keyword : keywords) {
        //            List<String> values = Arrays.asList(Collator.getKeywordValuesForLocale(keyword, ulocale, false));
        //            List<String> allValues = Arrays.asList(Collator.getKeywordValues(keyword));
        //            for (String value : allValues) {
        //                System.out.println(keyword + "=" + value);
        //                checkKeyword(locale, value, values.contains(value));
        //            }
        //        }
        //    }
        //
        //    private void checkKeyword(String locale, String collationValue, boolean shouldExist) {
        //        final UCultureInfo base = new UCultureInfo(locale);
        //        final UCultureInfo desired = new UCultureInfo(locale + "@collation=" + collationValue);
        //        Collator foo = Collator.getInstance(desired);
        //        UCultureInfo actual = foo.ActualCulture;
        //        if (shouldExist) {
        //            assertEquals("actual should match desired", desired, actual);
        //        } else {
        //            assertEquals("actual should match base", base, actual);
        //        }
        //        int comp = foo.compare("a", "ā");
        //        assertEquals("should fall back to default for zh", -1, comp);
        //    }
        //
        //    /**
        //     * @param rb
        //     * @param i
        //     */
        //    private static void showBundle(UResourceBundle rb, int i) {
        //        for (String key : rb.keySet()) {
        //            System.out.print("\n" + Utility.repeat("  ", i) + key);
        //            UResourceBundle rb2 = rb.get(key);
        //            showBundle(rb2, i+1);
        //        }
        //    }


        [Test]
        public void TestA()
        {
            String[][] tests = {
                new string[] {"zh_Hant", "渡辺", "12劃"},
                new string[] {"zh", "渡辺", "D"}
                /*, "zh@collation=unihan", "ja@collation=unihan", "ko@collation=unihan"*/
                };
            foreach (String[] test in tests)
            {
                AlphabeticIndex<int> alphabeticIndex = new AlphabeticIndex<int>(new UCultureInfo(test[0]));
                String probe = test[1];
                String expectedLabel = test[2];
                alphabeticIndex.AddRecord(probe, 1);
                IList<string> labels = alphabeticIndex.GetBucketLabels();
                Logln(string.Format(StringFormatter.CurrentCulture, "{0}", labels));
                var bucket = Find(alphabeticIndex, probe);
                assertEquals("locale " + test[0] + " name=" + probe + " in bucket",
                        expectedLabel, bucket.Label);
            }
        }

        private Bucket<int> Find(AlphabeticIndex<int> alphabeticIndex, string probe)
        {
            foreach (var bucket in alphabeticIndex)
            {
                foreach (var record in bucket)
                {
                    if (record.Name.Equals(probe))
                    {
                        return bucket;
                    }
                }
            }
            return null;
        }

        [Test]
        public void TestFirstCharacters()
        {

            AlphabeticIndex<object> alphabeticIndex = new AlphabeticIndex<object>(new CultureInfo("en") /* Locale.ENGLISH */);
            RuleBasedCollator collator = alphabeticIndex.Collator;
            collator.Strength = (Collator.Identical);
            ICollection<String> firsts = alphabeticIndex.GetFirstCharactersInScripts();
            // Verify that each script is represented exactly once.
            // Exclude pseudo-scripts like Common (no letters).
            // Exclude scripts like Braille and Sutton SignWriting
            // because they only have symbols, not letters.
            UnicodeSet missingScripts = new UnicodeSet(
                    "[^[:inherited:][:unknown:][:common:][:Braille:][:SignWriting:]]");
            String last = "";
            foreach (String index in firsts)
            {
                if (collator.Compare(last, index) >= 0)
                {
                    Errln("Characters not in order: " + last + " !< " + index);
                }
                int script = GetFirstRealScript(index);
                if (script == UScript.Unknown) { continue; }
                UnicodeSet s = new UnicodeSet().ApplyInt32PropertyValue(UProperty.Script, script);
                if (missingScripts.ContainsNone(s))
                {
                    Errln("2nd character in script: " + index + "\t" + new UnicodeSet(missingScripts).RetainAll(s).ToPattern(false));
                }
                missingScripts.RemoveAll(s);
            }
            if (missingScripts.Count != 0)
            {
                String missingScriptNames = "";
                UnicodeSet missingChars = new UnicodeSet(missingScripts);
                for (; ; )
                {
                    int c = missingChars[0];
                    if (c < 0)
                    {
                        break;
                    }
                    int script = UScript.GetScript(c);
                    missingScriptNames += " " +
                            UChar.GetPropertyValueName(
                                    UProperty.Script, script, NameChoice.Short);
                    missingChars.RemoveAll(new UnicodeSet().ApplyInt32PropertyValue(UProperty.Script, script));
                }
                Errln("Missing character from:" + missingScriptNames + " -- " + missingScripts);
            }
        }

        private static int GetFirstRealScript(string s) // ICU4N specific - changed s from ICharSequence to string
        {
            for (int i = 0; i < s.Length;)
            {
                int c = Character.CodePointAt(s, i);
                int script = UScript.GetScript(c);
                if (script != UScript.Unknown && script != UScript.Inherited && script != UScript.Common)
                {
                    return script;
                }
                i += Character.CharCount(c);
            }
            return UScript.Unknown;
        }

        [Test]
        public void TestBuckets()
        {
            UCultureInfo additionalLocale = new UCultureInfo("en");

            foreach (String[] pair in localeAndIndexCharactersLists)
            {
                CheckBuckets(pair[0], SimpleTests, additionalLocale, "E", "edgar", "Effron", "Effron");
            }
        }

        [Test]
        public void TestEmpty()
        {
            // just verify that it doesn't blow up.
            List<UCultureInfo> locales = new List<UCultureInfo>(); // new LinkedHashSet<ULocale>();
            locales.Add(UCultureInfo.InvariantCulture);
            locales.AddRange(UCultureInfo.GetCultures(UCultureTypes.AllCultures));
            foreach (UCultureInfo locale in locales)
            {
                try
                {
                    AlphabeticIndex<string> alphabeticIndex = new AlphabeticIndex<string>(locale);
                    alphabeticIndex.AddRecord("hi", "HI");
                    foreach (Bucket<string> bucket in alphabeticIndex)
                    {
                        BucketLabelType labelType = bucket.LabelType;
                    }
                }
                catch (Exception e)
                {
                    Errln("Exception when creating AlphabeticIndex for:\t" + locale.IetfLanguageTag);
                    Errln(e.ToString());
                }
            }
        }

        [Test]
        public void TestSetGetSpecialLabels()
        {
            AlphabeticIndex<object> index = new AlphabeticIndex<object>(new CultureInfo("de")).AddLabels(new CultureInfo("ru"));
            index.SetUnderflowLabel("__");
            index.SetInflowLabel("--");
            index.SetOverflowLabel("^^");
            assertEquals("underflow label", "__", index.UnderflowLabel);
            assertEquals("inflow label", "--", index.InflowLabel);
            assertEquals("overflow label", "^^", index.OverflowLabel);

            ImmutableIndex<object> ii = index.BuildImmutableIndex();
            assertEquals("0 -> underflow", "__", ii.GetBucket(ii.GetBucketIndex("0")).Label);
            assertEquals("Ω -> inflow", "--", ii.GetBucket(ii.GetBucketIndex("Ω")).Label);
            assertEquals("字 -> overflow", "^^", ii.GetBucket(ii.GetBucketIndex("字")).Label);
        }

        [Test]
        public void TestInflow()
        {
            object[][] tests = {
                new object[] {0, new UCultureInfo("en")},
                new object[] {0, new UCultureInfo("en"), new UCultureInfo("el")},
                new object[] {1, new UCultureInfo("en"), new UCultureInfo("ru")},
                new object[] {0, new UCultureInfo("en"), new UCultureInfo("el"), new UnicodeSet("[\u2C80]"), new UCultureInfo("ru")},
                new object[] {0, new UCultureInfo("en")},
                new object[] {2, new UCultureInfo("en"), new UCultureInfo("ru"), new UCultureInfo("ja")},
        };
            foreach (Object[] test in tests)
            {
                int expected = (int)test[0];
                AlphabeticIndex<double> alphabeticIndex = new AlphabeticIndex<double>((UCultureInfo)test[1]);
                for (int i = 2; i < test.Length; ++i)
                {
                    if (test[i] is UCultureInfo)
                    {
                        alphabeticIndex.AddLabels((UCultureInfo)test[i]);
                    }
                    else
                    {
                        alphabeticIndex.AddLabels((UnicodeSet)test[i]);
                    }
                }
                Counter<BucketLabelType> counter = new Counter<BucketLabelType>();
                foreach (var bucket in alphabeticIndex)
                {
                    var labelType = bucket.LabelType;
                    counter.Add(labelType, 1);
                }
                String printList = string.Format(StringFormatter.CurrentCulture, "{0}", (object)test);
                assertEquals(BucketLabelType.Underflow + "\t" + printList, 1, counter.Get(BucketLabelType.Underflow));
                assertEquals(BucketLabelType.Inflow + "\t" + printList, expected, counter.Get(BucketLabelType.Inflow));
                if (expected != counter.Get(BucketLabelType.Inflow))
                {
                    // for debugging
                    AlphabeticIndex<Double> indexCharacters2 = new AlphabeticIndex<double>((UCultureInfo)test[1]);
                    for (int i = 2; i < test.Length; ++i)
                    {
                        if (test[i] is UCultureInfo)
                        {
                            indexCharacters2.AddLabels((UCultureInfo)test[i]);
                        }
                        else
                        {
                            indexCharacters2.AddLabels((UnicodeSet)test[i]);
                        }
                    }
                    List<Bucket<double>> buckets = new List<Bucket<double>>(alphabeticIndex);  //CollectionUtilities.addAll(alphabeticIndex.iterator(), new List<AlphabeticIndex<double>.Bucket>());
                    Logln(buckets.ToString());
                }
                assertEquals(BucketLabelType.Overflow + "\t" + printList, 1, counter.Get(BucketLabelType.Overflow));
            }
        }

        private void CheckBuckets(string localeString, string[] test, UCultureInfo additionalLocale, string testBucket, params string[] items)
        {
            StringBuilder UI = new StringBuilder();
            UCultureInfo desiredLocale = new UCultureInfo(localeString);

            // Create a simple index where the values for the strings are Integers, and add the strings
            AlphabeticIndex<int> index = new AlphabeticIndex<int>(desiredLocale).AddLabels(additionalLocale);
            int counter = 0;
            Counter<string> itemCount = new Counter<string>();
            foreach (string item in test)
            {
                index.AddRecord(item, counter++);
                itemCount.Add(item, 1);
            }
            assertEquals("getRecordCount()", (int)itemCount.GetTotal(), index.RecordCount);  // code coverage

            IList<string> labels = index.GetBucketLabels();
            var immIndex = index.BuildImmutableIndex();

            Logln(desiredLocale + "\t" + desiredLocale.GetDisplayName(new UCultureInfo("en")) + " - " + desiredLocale.GetDisplayName(desiredLocale) + "\t"
                    + index.Collator.ActualCulture);
            UI.Length = (0);
            UI.Append(desiredLocale + "\t");
            bool showAll = true;

            // Show index at top. We could skip or gray out empty buckets
            foreach (Bucket<int> bucket in index)
            {
                if (showAll || bucket.Count != 0)
                {
                    ShowLabelAtTop(UI, bucket.Label);
                }
            }
            Logln(UI.ToString());

            // Show the buckets with their contents, skipping empty buckets
            int bucketIndex = 0;
            foreach (var bucket in index)
            {
                assertEquals("bucket label vs. iterator",
                        labels[bucketIndex], bucket.Label);
                assertEquals("bucket label vs. immutable",
                        labels[bucketIndex], immIndex.GetBucket(bucketIndex).Label);
                assertEquals("bucket label type vs. immutable",
                        bucket.LabelType, immIndex.GetBucket(bucketIndex).LabelType);
                foreach (var r in bucket)
                {
                    string name = r.Name; // ICU4N specific - changed from ICharSequence to string
                    assertEquals("getBucketIndex(" + name + ")",
                            bucketIndex, index.GetBucketIndex(name));
                    assertEquals("immutable getBucketIndex(" + name + ")",
                            bucketIndex, immIndex.GetBucketIndex(name));
                }
                if (bucket.Label.Equals(testBucket))
                {
                    Counter<String> keys = GetKeys(bucket);
                    foreach (String item in items)
                    {
                        long globalCount = itemCount.Get(item);
                        long localeCount = keys.Get(item);
                        if (globalCount != localeCount)
                        {
                            Errln("Error: in " + "'" + testBucket + "', '" + item + "' should have count "
                                    + globalCount + " but has count " + localeCount);
                        }

                    }
                }

                if (bucket.Count != 0)
                {
                    ShowLabelInList(UI, bucket.Label);
                    foreach (var item in bucket)
                    {
                        ShowIndexedItem(UI, item.Name, item.Data);
                    }
                    Logln(UI.ToString());
                }
                ++bucketIndex;
            }
            assertEquals("getBucketCount()", bucketIndex, index.BucketCount);
            assertEquals("immutable getBucketCount()", bucketIndex, immIndex.BucketCount);

            assertNull("immutable getBucket(-1)", immIndex.GetBucket(-1));
            assertNull("immutable getBucket(count)", immIndex.GetBucket(bucketIndex));

            foreach (var bucket in immIndex)
            {
                assertEquals("immutable bucket size", 0, bucket.Count);
                assertFalse("immutable bucket iterator.hasNext()", bucket.GetEnumerator().MoveNext());
            }
        }

        public void ShowIndex<T>(AlphabeticIndex<T> index, bool showEmpty)
        {
            Logln("Actual");
            StringBuilder UI = new StringBuilder();
            foreach (var bucket in index)
            {
                if (showEmpty || bucket.Count != 0)
                {
                    ShowLabelInList(UI, bucket.Label);
                    foreach (var item in bucket)
                    {
                        ShowIndexedItem(UI, item.Name, item.Data);
                    }
                    Logln(UI.ToString());
                }
            }
        }

        /**
         * @param myBucketLabels
         * @param myBucketContents
         * @param b
         */
        private void ShowIndex(IList<String> myBucketLabels, List<ISet<Row<RawCollationKey, string, int, double>>> myBucketContents, bool showEmpty)
        {
            Logln("Alternative");
            StringBuilder UI = new StringBuilder();

            for (int i = 0; i < myBucketLabels.Count; ++i)
            {
                var bucket = myBucketContents[i];
                if (!showEmpty && bucket.Count == 0)
                {
                    continue;
                }
                UI.Length = (0);
                UI.Append("*").Append(myBucketLabels[i]);
                foreach (var item in bucket)
                {
                    UI.Append("\t ").Append(item.Get1().ToString()).Append(ARROW).Append(item.Get3().ToString());
                }
                Logln(UI.ToString());
            }
        }

        private void ShowLabelAtTop(StringBuilder buffer, String label)
        {
            buffer.Append(label + " ");
        }

        private void ShowIndexedItem<T>(StringBuilder buffer, string key, T value) // ICU4N specific - changed key from ICharSequence to string
        {
            buffer.Append("\t " + key + ARROW + value);
        }

        private void ShowLabelInList(StringBuilder buffer, String label)
        {
            buffer.Length = (0);
            buffer.Append(label);
        }

        private Counter<String> GetKeys(Bucket<int> entry)
        {
            Counter<String> keys = new Counter<String>();
            foreach (var x in entry)
            {
                String key = x.Name.ToString();
                keys.Add(key, 1);
            }
            return keys;
        }

        [Test]
        public void TestIndexCharactersList()
        {
            foreach (String[] localeAndIndexCharacters in localeAndIndexCharactersLists)
            {
                UCultureInfo locale = new UCultureInfo(localeAndIndexCharacters[0]);
                String expectedIndexCharacters = "\u2026:" + localeAndIndexCharacters[1] + ":\u2026";
                ICollection<string> alphabeticIndex = new AlphabeticIndex<string>(locale).GetBucketLabels();

                // Join the elements of the list to a string with delimiter ":"
                StringBuilder sb = new StringBuilder();
                using (var iter = alphabeticIndex.GetEnumerator())
                {
                    bool first = true;
                    while (iter.MoveNext())
                    {
                        if (first)
                            first = false;
                        else
                            sb.Append(":");
                        sb.Append(iter.Current);
                    }
                }
                String actualIndexCharacters = sb.ToString();
                if (!expectedIndexCharacters.Equals(actualIndexCharacters))
                {
                    Errln("Test failed for locale " + localeAndIndexCharacters[0] +
                            "\n  Expected = |" + expectedIndexCharacters + "|\n  actual   = |" + actualIndexCharacters + "|");
                }
            }
        }

        [Test]
        public void TestBasics()
        {
            UCultureInfo[] list = UCultureInfo.GetCultures(UCultureTypes.AllCultures);
            // get keywords combinations
            // don't bother with multiple combinations at this point
            List<object> keywords = new List<object>();
            keywords.Add("");

            String[] collationValues = Collator.GetKeywordValues("collation");
            for (int j = 0; j < collationValues.Length; ++j)
            {
                keywords.Add("@collation=" + collationValues[j]);
            }

            for (int i = 0; i < list.Length; ++i)
            {
                foreach (string collationValue in keywords)
                {
                    string localeString = list[i].ToString();
                    if (!KEY_LOCALES.Contains(localeString)) continue; // TODO change in exhaustive
                    UCultureInfo locale = new UCultureInfo(localeString + collationValue);
                    if (collationValue.Length > 0 && !Collator.GetFunctionalEquivalent("collation", locale).Equals(locale))
                    {
                        //Logln("Skipping " + locale);
                        continue;
                    }

                    if (locale.Country.Length != 0)
                    {
                        continue;
                    }
                    bool isUnihan = collationValue.Contains("unihan");
                    AlphabeticIndex<object> alphabeticIndex = new AlphabeticIndex<object>(locale);
                    if (isUnihan)
                    {
                        // Unihan tailorings have a label per radical, and there are at least 214,
                        // if not more when simplified radicals are distinguished.
                        alphabeticIndex.SetMaxLabelCount(500);
                    }
                    var mainChars = alphabeticIndex.GetBucketLabels();
                    string mainCharString = mainChars.ToString();
                    if (mainCharString.Length > 500)
                    {
                        mainCharString = mainCharString.Substring(0, 500) + "..."; // ICU4N: Checked 2nd parameter
                    }
                    Logln(mainChars.Count + "\t" + locale + "\t" + locale.GetDisplayName(new UCultureInfo("en")));
                    Logln("Index:\t" + mainCharString);
                    if (!isUnihan && mainChars.Count > 100)
                    {
                        Errln("Index character set too large: " +
                                locale + " [" + mainChars.Count + "]:\n    " + mainChars);
                    }
                }
            }
        }

        [Test]
        public void TestClientSupport()
        {
            foreach (String localeString in new String[] { "zh" })
            { // KEY_LOCALES, new String[] {"zh"}
                UCultureInfo ulocale = new UCultureInfo(localeString);
                AlphabeticIndex<Double> alphabeticIndex = new AlphabeticIndex<Double>(ulocale).AddLabels(new CultureInfo("en") /* Locale.ENGLISH */);
                RuleBasedCollator collator = alphabeticIndex.Collator;
                String[][] tests;

                if (!localeString.Equals("zh"))
                {
                    tests = new String[][] { SimpleTests };
                }
                else
                {
                    tests = new String[][] { SimpleTests, hackPinyin, simplifiedNames };
                }

                foreach (String[] shortTest in tests)
                {
                    double testValue = 100;
                    alphabeticIndex.ClearRecords();
                    foreach (String name in shortTest)
                    {
                        alphabeticIndex.AddRecord(name, testValue++);
                    }

                    if (DEBUG) ShowIndex(alphabeticIndex, false);

                    // make my own copy
                    testValue = 100;
                    IList<String> myBucketLabels = alphabeticIndex.GetBucketLabels();
                    var myBucketContents = new List<ISet<Row<RawCollationKey, string, int, double>>>(myBucketLabels.Count);
                    for (int i = 0; i < myBucketLabels.Count; ++i)
                    {
                        myBucketContents.Add(new SortedSet<Row<RawCollationKey, string, int, double>>());
                    }
                    foreach (String name in shortTest)
                    {
                        int bucketIndex = alphabeticIndex.GetBucketIndex(name);
                        if (bucketIndex > myBucketContents.Count)
                        {
                            alphabeticIndex.GetBucketIndex(name); // call again for debugging
                        }
                        var myBucket = myBucketContents[bucketIndex];
                        RawCollationKey rawCollationKey = collator.GetRawCollationKey(name, null);
                        var row = new Row<RawCollationKey, string, int, double>(rawCollationKey, name, name.Length, testValue++);
                        myBucket.Add(row);
                    }
                    if (DEBUG) ShowIndex(myBucketLabels, myBucketContents, false);

                    // now compare
                    int index = 0;
                    bool gotError = false;
                    foreach (Bucket<double> bucket in alphabeticIndex)
                    {
                        String bucketLabel = bucket.Label;
                        String myLabel = myBucketLabels[index];
                        if (!bucketLabel.Equals(myLabel))
                        {
                            gotError |= !assertEquals(ulocale + "\tBucket Labels (" + index + ")", bucketLabel, myLabel);
                        }
                        var myBucket = myBucketContents[index];
                        using (var myBucketIterator = myBucket.GetEnumerator())
                        {
                            int recordIndex = 0;
                            foreach (var record in bucket)
                            {
                                String myName = null;
                                if (myBucketIterator.MoveNext())
                                {
                                    var myRecord = myBucketIterator.Current;
                                    myName = myRecord.Get1();
                                }
                                if (!record.Name.Equals(myName))
                                {
                                    gotError |= !assertEquals(ulocale + "\t" + bucketLabel + "\t" + "Record Names (" + index + "." + recordIndex++ + ")", record.Name, myName);
                                }
                            }
                            while (myBucketIterator.MoveNext())
                            {
                                var myRecord = myBucketIterator.Current;
                                String myName = myRecord.Get1();
                                gotError |= !assertEquals(ulocale + "\t" + bucketLabel + "\t" + "Record Names (" + index + "." + recordIndex++ + ")", null, myName);
                            }
                            index++;
                        }
                    }
                    if (gotError)
                    {
                        ShowIndex(myBucketLabels, myBucketContents, false);
                        ShowIndex(alphabeticIndex, false);
                    }
                }
            }
        }

        [Test]
        public void TestFirstScriptCharacters()
        {
            ICollection<String> firstCharacters =
                    new AlphabeticIndex<string>(new UCultureInfo("en")).GetFirstCharactersInScripts();
            ICollection<String> expectedFirstCharacters = FirstStringsInScript((RuleBasedCollator)Collator.GetInstance(UCultureInfo.InvariantCulture));
            ISet<String> diff = new SortedSet<String>(firstCharacters, StringComparer.Ordinal);
            diff.ExceptWith(expectedFirstCharacters);
            assertTrue("First Characters contains unexpected ones: " + diff, diff.Count == 0);
            diff.Clear();
            diff.UnionWith(expectedFirstCharacters);
            diff.ExceptWith(firstCharacters);
            assertTrue("First Characters missing expected ones: " + diff, diff.Count == 0);
        }

        private static readonly UnicodeSet TO_TRY = new UnicodeSet("[[:^nfcqc=no:]-[:sc=Common:]-[:sc=Inherited:]-[:sc=Unknown:]]").Freeze();

        /**
         * Returns a collection of all the "First" characters of scripts, according to the collation.
         */
        private static ICollection<String> FirstStringsInScript(RuleBasedCollator ruleBasedCollator)
        {
            String[] results = new String[UScript.CodeLimit];
            foreach (String current in TO_TRY)
            {
                if (ruleBasedCollator.Compare(current, "a") < 0)
                { // we only want "real" script characters, not symbols.
                    continue;
                }
                int script = (int)UScript.GetScript(current.CodePointAt(0));
                if (results[script] == null)
                {
                    results[script] = current;
                }
                else if (ruleBasedCollator.Compare(current, results[script]) < 0)
                {
                    results[script] = current;
                }
            }

            try
            {
                UnicodeSet extras = new UnicodeSet();
                UnicodeSet expansions = new UnicodeSet();
                ruleBasedCollator.GetContractionsAndExpansions(extras, expansions, true);
                extras.AddAll(expansions).RemoveAll(TO_TRY);
                if (extras.Count != 0)
                {
                    Normalizer2 normalizer = Normalizer2.GetNFKCInstance();
                    foreach (String current in extras)
                    {
                        if (!normalizer.IsNormalized(current) || ruleBasedCollator.Compare(current, "9") <= 0)
                        {
                            continue;
                        }
                        int script = (int)GetFirstRealScript(current);
                        if (script == UScript.Unknown && !IsUnassignedBoundary(current)) { continue; }
                        if (results[script] == null)
                        {
                            results[script] = current;
                        }
                        else if (ruleBasedCollator.Compare(current, results[script]) < 0)
                        {
                            results[script] = current;
                        }
                    }
                }
            }
            catch (Exception e)
            {
            } // why have a checked exception???

            // TODO: We should not test that we get the same strings, but that we
            // get strings that sort primary-equal to those from the implementation.

            ICollection<String> result = new List<String>();
            for (int i = 0; i < results.Length; ++i)
            {
                if (results[i] != null)
                {
                    result.Add(results[i]);
                }
            }
            return result;
        }

        private static bool IsUnassignedBoundary(string s) // ICU4N specific - changed s from ICharSequence to string
        {
            // The root collator provides a script-first-primary boundary contraction
            // for the unassigned-implicit range.
            return s[0] == 0xfdd1 &&
                    UScript.GetScript(Character.CodePointAt(s, 1)) == UScript.Unknown;
        }

        [Test]
        public void TestZZZ()
        {
            //            int x = 3;
            //            AlphabeticIndex index = new AlphabeticIndex(new UCultureInfo("en"));
            //            UnicodeSet additions = new UnicodeSet();
            //            additions.add(0x410).add(0x415);  // Cyrillic
            //            // additions.add(0x391).add(0x393);     // Greek
            //            index.addLabels(additions);
            //            int lc = index.getLabels().size();
            //            List  labels = index.getLabels();
            //            System.out.println("Label Count = " + lc + "\t" + labels);
            //            System.out.println("Bucket Count =" + index.getBucketCount());
        }

        [Test]
        public void TestSimplified()
        {
            CheckBuckets("zh", simplifiedNames, new UCultureInfo("en"), "W", "\u897f");
        }

        [Test]
        public void TestTraditional()
        {
            CheckBuckets("zh_Hant", traditionalNames, new UCultureInfo("en"), "\u4e9f", "\u5357\u9580");
        }

        internal static readonly String[] SimpleTests = {
            "斎藤",
            "\u1f2d\u03c1\u03b1",
            "$", "\u00a3", "12", "2",
            "Davis", "Davis", "Abbot", "\u1D05avis", "Zach", "\u1D05avis", "\u01b5", "\u0130stanbul", "Istanbul", "istanbul", "\u0131stanbul",
            "\u00deor", "\u00c5berg", "\u00d6stlund",
            "\u1f2d\u03c1\u03b1", "\u1f08\u03b8\u03b7\u03bd\u1fb6",
            "\u0396\u03b5\u03cd\u03c2", "\u03a0\u03bf\u03c3\u03b5\u03b9\u03b4\u1f63\u03bd", "\u1f0d\u03b9\u03b4\u03b7\u03c2", "\u0394\u03b7\u03bc\u03ae\u03c4\u03b7\u03c1", "\u1f19\u03c3\u03c4\u03b9\u03ac",
            //"\u1f08\u03c0\u03cc\u03bb\u03bb\u03c9\u03bd", "\u1f0c\u03c1\u03c4\u03b5\u03bc\u03b9\u03c2", "\u1f19\u03c1\u03bc\u1f23\u03c2", "\u1f0c\u03c1\u03b7\u03c2", "\u1f08\u03c6\u03c1\u03bf\u03b4\u03af\u03c4\u03b7", "\u1f2d\u03c6\u03b1\u03b9\u03c3\u03c4\u03bf\u03c2", "\u0394\u03b9\u03cc\u03bd\u03c5\u03c3\u03bf\u03c2",
            "\u6589\u85e4", "\u4f50\u85e4", "\u9234\u6728", "\u9ad8\u6a4b", "\u7530\u4e2d", "\u6e21\u8fba", "\u4f0a\u85e4", "\u5c71\u672c", "\u4e2d\u6751", "\u5c0f\u6797", "\u658e\u85e4", "\u52a0\u85e4",
            //"\u5409\u7530", "\u5c71\u7530", "\u4f50\u3005\u6728", "\u5c71\u53e3", "\u677e\u672c", "\u4e95\u4e0a", "\u6728\u6751", "\u6797", "\u6e05\u6c34"
        };

        internal static readonly String[] hackPinyin = {
            "a", "\u5416", "\u58ba", //
            "b", "\u516b", "\u62d4", "\u8500", //
            "c", "\u5693", "\u7938", "\u9e7e", //
            "d", "\u5491", "\u8fcf", "\u964a", //
            "e","\u59b8", "\u92e8", "\u834b", //
            "f", "\u53d1", "\u9197", "\u99a5", //
            "g", "\u7324", "\u91d3", "\u8142", //
            "h", "\u598e", "\u927f", "\u593b", //
            "j", "\u4e0c", "\u6785", "\u9d58", //
            "k", "\u5494", "\u958b", "\u7a52", //
            "l", "\u5783", "\u62c9", "\u9ba5", //
            "m", "\u5638", "\u9ebb", "\u65c0", //
            "n", "\u62ff", "\u80ad", "\u685b", //
            "o", "\u5662", "\u6bee", "\u8bb4", //
            "p", "\u5991", "\u8019", "\u8c31", //
            "q", "\u4e03", "\u6053", "\u7f56", //
            "r", "\u5465", "\u72aa", "\u6e03", //
            "s", "\u4ee8", "\u9491", "\u93c1", //
            "t", "\u4ed6", "\u9248", "\u67dd", //
            "w", "\u5c72", "\u5558", "\u5a7a", //
            "x", "\u5915", "\u5438", "\u6bbe", //
            "y", "\u4e2b", "\u82bd", "\u8574", //
            "z", "\u5e00", "\u707d", "\u5c0a"
        };

        internal static readonly String[] simplifiedNames = {
            "Abbot", "Morton", "Zachary", "Williams", "\u8d75", "\u94b1", "\u5b59", "\u674e", "\u5468", "\u5434", "\u90d1", "\u738b", "\u51af", "\u9648", "\u696e", "\u536b", "\u848b", "\u6c88",
            "\u97e9", "\u6768", "\u6731", "\u79e6", "\u5c24", "\u8bb8", "\u4f55", "\u5415", "\u65bd", "\u5f20", "\u5b54", "\u66f9", "\u4e25", "\u534e", "\u91d1", "\u9b4f", "\u9676", "\u59dc", "\u621a", "\u8c22", "\u90b9",
            "\u55bb", "\u67cf", "\u6c34", "\u7aa6", "\u7ae0", "\u4e91", "\u82cf", "\u6f58", "\u845b", "\u595a", "\u8303", "\u5f6d", "\u90ce", "\u9c81", "\u97e6", "\u660c", "\u9a6c", "\u82d7", "\u51e4", "\u82b1", "\u65b9",
            "\u4fde", "\u4efb", "\u8881", "\u67f3", "\u9146", "\u9c8d", "\u53f2", "\u5510", "\u8d39", "\u5ec9", "\u5c91", "\u859b", "\u96f7", "\u8d3a", "\u502a", "\u6c64", "\u6ed5", "\u6bb7", "\u7f57", "\u6bd5", "\u90dd",
            "\u90ac", "\u5b89", "\u5e38", "\u4e50", "\u4e8e", "\u65f6", "\u5085", "\u76ae", "\u535e", "\u9f50", "\u5eb7", "\u4f0d", "\u4f59", "\u5143", "\u535c", "\u987e", "\u5b5f", "\u5e73", "\u9ec4", "\u548c", "\u7a46",
            "\u8427", "\u5c39", "\u59da", "\u90b5", "\u6e5b", "\u6c6a", "\u7941", "\u6bdb", "\u79b9", "\u72c4", "\u7c73", "\u8d1d", "\u660e", "\u81e7", "\u8ba1", "\u4f0f", "\u6210", "\u6234", "\u8c08", "\u5b8b", "\u8305",
            "\u5e9e", "\u718a", "\u7eaa", "\u8212", "\u5c48", "\u9879", "\u795d", "\u8463", "\u6881", "\u675c", "\u962e", "\u84dd", "\u95fd", "\u5e2d", "\u5b63", "\u9ebb", "\u5f3a", "\u8d3e", "\u8def", "\u5a04", "\u5371",
            "\u6c5f", "\u7ae5", "\u989c", "\u90ed", "\u6885", "\u76db", "\u6797", "\u5201", "\u953a", "\u5f90", "\u4e18", "\u9a86", "\u9ad8", "\u590f", "\u8521", "\u7530", "\u6a0a", "\u80e1", "\u51cc", "\u970d", "\u865e",
            "\u4e07", "\u652f", "\u67ef", "\u661d", "\u7ba1", "\u5362", "\u83ab", "\u7ecf", "\u623f", "\u88d8", "\u7f2a", "\u5e72", "\u89e3", "\u5e94", "\u5b97", "\u4e01", "\u5ba3", "\u8d32", "\u9093", "\u90c1", "\u5355",
            "\u676d", "\u6d2a", "\u5305", "\u8bf8", "\u5de6", "\u77f3", "\u5d14", "\u5409", "\u94ae", "\u9f9a", "\u7a0b", "\u5d47", "\u90a2", "\u6ed1", "\u88f4", "\u9646", "\u8363", "\u7fc1", "\u8340", "\u7f8a", "\u65bc",
            "\u60e0", "\u7504", "\u9eb9", "\u5bb6", "\u5c01", "\u82ae", "\u7fbf", "\u50a8", "\u9773", "\u6c72", "\u90b4", "\u7cdc", "\u677e", "\u4e95", "\u6bb5", "\u5bcc", "\u5deb", "\u4e4c", "\u7126", "\u5df4", "\u5f13",
            "\u7267", "\u9697", "\u5c71", "\u8c37", "\u8f66", "\u4faf", "\u5b93", "\u84ec", "\u5168", "\u90d7", "\u73ed", "\u4ef0", "\u79cb", "\u4ef2", "\u4f0a", "\u5bab", "\u5b81", "\u4ec7", "\u683e", "\u66b4", "\u7518",
            "\u659c", "\u5389", "\u620e", "\u7956", "\u6b66", "\u7b26", "\u5218", "\u666f", "\u8a79", "\u675f", "\u9f99", "\u53f6", "\u5e78", "\u53f8", "\u97f6", "\u90dc", "\u9ece", "\u84df", "\u8584", "\u5370", "\u5bbf",
            "\u767d", "\u6000", "\u84b2", "\u90b0", "\u4ece", "\u9102", "\u7d22", "\u54b8", "\u7c4d", "\u8d56", "\u5353", "\u853a", "\u5c60", "\u8499", "\u6c60", "\u4e54", "\u9634", "\u90c1", "\u80e5", "\u80fd", "\u82cd",
            "\u53cc", "\u95fb", "\u8398", "\u515a", "\u7fdf", "\u8c2d", "\u8d21", "\u52b3", "\u9004", "\u59ec", "\u7533", "\u6276", "\u5835", "\u5189", "\u5bb0", "\u90e6", "\u96cd", "\u90e4", "\u74a9", "\u6851", "\u6842",
            "\u6fee", "\u725b", "\u5bff", "\u901a", "\u8fb9", "\u6248", "\u71d5", "\u5180", "\u90cf", "\u6d66", "\u5c1a", "\u519c", "\u6e29", "\u522b", "\u5e84", "\u664f", "\u67f4", "\u77bf", "\u960e", "\u5145", "\u6155",
            "\u8fde", "\u8339", "\u4e60", "\u5ba6", "\u827e", "\u9c7c", "\u5bb9", "\u5411", "\u53e4", "\u6613", "\u614e", "\u6208", "\u5ed6", "\u5ebe", "\u7ec8", "\u66a8", "\u5c45", "\u8861", "\u6b65", "\u90fd", "\u803f",
            "\u6ee1", "\u5f18", "\u5321", "\u56fd", "\u6587", "\u5bc7", "\u5e7f", "\u7984", "\u9619", "\u4e1c", "\u6b27", "\u6bb3", "\u6c83", "\u5229", "\u851a", "\u8d8a", "\u5914", "\u9686", "\u5e08", "\u5de9", "\u538d",
            "\u8042", "\u6641", "\u52fe", "\u6556", "\u878d", "\u51b7", "\u8a3e", "\u8f9b", "\u961a", "\u90a3", "\u7b80", "\u9976", "\u7a7a", "\u66fe", "\u6bcb", "\u6c99", "\u4e5c", "\u517b", "\u97a0", "\u987b", "\u4e30",
            "\u5de2", "\u5173", "\u84af", "\u76f8", "\u67e5", "\u540e", "\u8346", "\u7ea2", "\u6e38", "\u7afa", "\u6743", "\u9011", "\u76d6", "\u76ca", "\u6853", "\u516c", "\u4e07\u4fdf", "\u53f8\u9a6c", "\u4e0a\u5b98", "\u6b27\u9633",
            "\u590f\u4faf", "\u8bf8\u845b", "\u95fb\u4eba", "\u4e1c\u65b9", "\u8d6b\u8fde", "\u7687\u752b", "\u5c09\u8fdf", "\u516c\u7f8a", "\u6fb9\u53f0", "\u516c\u51b6", "\u5b97\u653f", "\u6fee\u9633", "\u6df3\u4e8e", "\u5355\u4e8e", "\u592a\u53d4", "\u7533\u5c60", "\u516c\u5b59", "\u4ef2\u5b59",
            "\u8f69\u8f95", "\u4ee4\u72d0", "\u953a\u79bb", "\u5b87\u6587", "\u957f\u5b59", "\u6155\u5bb9", "\u9c9c\u4e8e", "\u95fe\u4e18", "\u53f8\u5f92", "\u53f8\u7a7a", "\u4e0c\u5b98", "\u53f8\u5bc7", "\u4ec9", "\u7763", "\u5b50\u8f66", "\u989b\u5b59", "\u7aef\u6728", "\u5deb\u9a6c",
            "\u516c\u897f", "\u6f06\u96d5", "\u4e50\u6b63", "\u58e4\u9a77", "\u516c\u826f", "\u62d3\u62d4", "\u5939\u8c37", "\u5bb0\u7236", "\u8c37\u6881", "\u664b", "\u695a", "\u960e", "\u6cd5", "\u6c5d", "\u9122", "\u6d82", "\u94a6", "\u6bb5\u5e72", "\u767e\u91cc",
            "\u4e1c\u90ed", "\u5357\u95e8", "\u547c\u5ef6", "\u5f52", "\u6d77", "\u7f8a\u820c", "\u5fae\u751f", "\u5cb3", "\u5e05", "\u7f11", "\u4ea2", "\u51b5", "\u540e", "\u6709", "\u7434", "\u6881\u4e18", "\u5de6\u4e18", "\u4e1c\u95e8", "\u897f\u95e8",
            "\u5546", "\u725f", "\u4f58", "\u4f74", "\u4f2f", "\u8d4f", "\u5357\u5bab", "\u58a8", "\u54c8", "\u8c2f", "\u7b2a", "\u5e74", "\u7231", "\u9633", "\u4f5f"
        };

        internal static readonly String[] traditionalNames = { "丁", "Abbot", "Morton", "Zachary", "Williams", "\u8d99", "\u9322", "\u5b6b",
            "\u674e", "\u5468", "\u5433", "\u912d", "\u738b", "\u99ae", "\u9673", "\u696e", "\u885b", "\u8523",
            "\u6c88", "\u97d3", "\u694a", "\u6731", "\u79e6", "\u5c24", "\u8a31", "\u4f55", "\u5442", "\u65bd",
            "\u5f35", "\u5b54", "\u66f9", "\u56b4", "\u83ef", "\u91d1", "\u9b4f", "\u9676", "\u59dc", "\u621a",
            "\u8b1d", "\u9112", "\u55bb", "\u67cf", "\u6c34", "\u7ac7", "\u7ae0", "\u96f2", "\u8607", "\u6f58",
            "\u845b", "\u595a", "\u7bc4", "\u5f6d", "\u90ce", "\u9b6f", "\u97cb", "\u660c", "\u99ac", "\u82d7",
            "\u9cf3", "\u82b1", "\u65b9", "\u4fde", "\u4efb", "\u8881", "\u67f3", "\u9146", "\u9b91", "\u53f2",
            "\u5510", "\u8cbb", "\u5ec9", "\u5c91", "\u859b", "\u96f7", "\u8cc0", "\u502a", "\u6e6f", "\u6ed5",
            "\u6bb7", "\u7f85", "\u7562", "\u90dd", "\u9114", "\u5b89", "\u5e38", "\u6a02", "\u65bc", "\u6642",
            "\u5085", "\u76ae", "\u535e", "\u9f4a", "\u5eb7", "\u4f0d", "\u9918", "\u5143", "\u535c", "\u9867",
            "\u5b5f", "\u5e73", "\u9ec3", "\u548c", "\u7a46", "\u856d", "\u5c39", "\u59da", "\u90b5", "\u6e5b",
            "\u6c6a", "\u7941", "\u6bdb", "\u79b9", "\u72c4", "\u7c73", "\u8c9d", "\u660e", "\u81e7", "\u8a08",
            "\u4f0f", "\u6210", "\u6234", "\u8ac7", "\u5b8b", "\u8305", "\u9f90", "\u718a", "\u7d00", "\u8212",
            "\u5c48", "\u9805", "\u795d", "\u8463", "\u6881", "\u675c", "\u962e", "\u85cd", "\u95a9", "\u5e2d",
            "\u5b63", "\u9ebb", "\u5f37", "\u8cc8", "\u8def", "\u5a41", "\u5371", "\u6c5f", "\u7ae5", "\u984f",
            "\u90ed", "\u6885", "\u76db", "\u6797", "\u5201", "\u937e", "\u5f90", "\u4e18", "\u99f1", "\u9ad8",
            "\u590f", "\u8521", "\u7530", "\u6a0a", "\u80e1", "\u51cc", "\u970d", "\u865e", "\u842c", "\u652f",
            "\u67ef", "\u661d", "\u7ba1", "\u76e7", "\u83ab", "\u7d93", "\u623f", "\u88d8", "\u7e46", "\u5e79",
            "\u89e3", "\u61c9", "\u5b97", "\u4e01", "\u5ba3", "\u8cc1", "\u9127", "\u9b31", "\u55ae", "\u676d",
            "\u6d2a", "\u5305", "\u8af8", "\u5de6", "\u77f3", "\u5d14", "\u5409", "\u9215", "\u9f94", "\u7a0b",
            "\u5d47", "\u90a2", "\u6ed1", "\u88f4", "\u9678", "\u69ae", "\u7fc1", "\u8340", "\u7f8a", "\u65bc",
            "\u60e0", "\u7504", "\u9eb4", "\u5bb6", "\u5c01", "\u82ae", "\u7fbf", "\u5132", "\u9773", "\u6c72",
            "\u90b4", "\u7cdc", "\u677e", "\u4e95", "\u6bb5", "\u5bcc", "\u5deb", "\u70cf", "\u7126", "\u5df4",
            "\u5f13", "\u7267", "\u9697", "\u5c71", "\u8c37", "\u8eca", "\u4faf", "\u5b93", "\u84ec", "\u5168",
            "\u90d7", "\u73ed", "\u4ef0", "\u79cb", "\u4ef2", "\u4f0a", "\u5bae", "\u5be7", "\u4ec7", "\u6b12",
            "\u66b4", "\u7518", "\u659c", "\u53b2", "\u620e", "\u7956", "\u6b66", "\u7b26", "\u5289", "\u666f",
            "\u8a79", "\u675f", "\u9f8d", "\u8449", "\u5e78", "\u53f8", "\u97f6", "\u90dc", "\u9ece", "\u858a",
            "\u8584", "\u5370", "\u5bbf", "\u767d", "\u61f7", "\u84b2", "\u90b0", "\u5f9e", "\u9102", "\u7d22",
            "\u54b8", "\u7c4d", "\u8cf4", "\u5353", "\u85fa", "\u5c60", "\u8499", "\u6c60", "\u55ac", "\u9670",
            "\u9b31", "\u80e5", "\u80fd", "\u84bc", "\u96d9", "\u805e", "\u8398", "\u9ee8", "\u7fdf", "\u8b5a",
            "\u8ca2", "\u52de", "\u9004", "\u59ec", "\u7533", "\u6276", "\u5835", "\u5189", "\u5bb0", "\u9148",
            "\u96cd", "\u90e4", "\u74a9", "\u6851", "\u6842", "\u6fee", "\u725b", "\u58fd", "\u901a", "\u908a",
            "\u6248", "\u71d5", "\u5180", "\u90df", "\u6d66", "\u5c1a", "\u8fb2", "\u6eab", "\u5225", "\u838a",
            "\u664f", "\u67f4", "\u77bf", "\u95bb", "\u5145", "\u6155", "\u9023", "\u8339", "\u7fd2", "\u5ba6",
            "\u827e", "\u9b5a", "\u5bb9", "\u5411", "\u53e4", "\u6613", "\u614e", "\u6208", "\u5ed6", "\u5ebe",
            "\u7d42", "\u66a8", "\u5c45", "\u8861", "\u6b65", "\u90fd", "\u803f", "\u6eff", "\u5f18", "\u5321",
            "\u570b", "\u6587", "\u5bc7", "\u5ee3", "\u797f", "\u95d5", "\u6771", "\u6b50", "\u6bb3", "\u6c83",
            "\u5229", "\u851a", "\u8d8a", "\u5914", "\u9686", "\u5e2b", "\u978f", "\u5399", "\u8076", "\u6641",
            "\u52fe", "\u6556", "\u878d", "\u51b7", "\u8a3e", "\u8f9b", "\u95de", "\u90a3", "\u7c21", "\u9952",
            "\u7a7a", "\u66fe", "\u6bcb", "\u6c99", "\u4e5c", "\u990a", "\u97a0", "\u9808", "\u8c50", "\u5de2",
            "\u95dc", "\u84af", "\u76f8", "\u67e5", "\u5f8c", "\u834a", "\u7d05", "\u904a", "\u7afa", "\u6b0a",
            "\u9011", "\u84cb", "\u76ca", "\u6853", "\u516c", "\u4e07\u4fdf", "\u53f8\u99ac", "\u4e0a\u5b98",
            "\u6b50\u967d", "\u590f\u4faf", "\u8af8\u845b", "\u805e\u4eba", "\u6771\u65b9", "\u8d6b\u9023",
            "\u7687\u752b", "\u5c09\u9072", "\u516c\u7f8a", "\u6fb9\u53f0", "\u516c\u51b6", "\u5b97\u653f",
            "\u6fee\u967d", "\u6df3\u4e8e", "\u55ae\u4e8e", "\u592a\u53d4", "\u7533\u5c60", "\u516c\u5b6b",
            "\u4ef2\u5b6b", "\u8ed2\u8f45", "\u4ee4\u72d0", "\u937e\u96e2", "\u5b87\u6587", "\u9577\u5b6b",
            "\u6155\u5bb9", "\u9bae\u4e8e", "\u95ad\u4e18", "\u53f8\u5f92", "\u53f8\u7a7a", "\u4e0c\u5b98",
            "\u53f8\u5bc7", "\u4ec9", "\u7763", "\u5b50\u8eca", "\u9853\u5b6b", "\u7aef\u6728", "\u5deb\u99ac",
            "\u516c\u897f", "\u6f06\u96d5", "\u6a02\u6b63", "\u58e4\u99df", "\u516c\u826f", "\u62d3\u62d4",
            "\u593e\u8c37", "\u5bb0\u7236", "\u7a40\u6881", "\u6649", "\u695a", "\u95bb", "\u6cd5", "\u6c5d", "\u9122",
            "\u5857", "\u6b3d", "\u6bb5\u5e72", "\u767e\u91cc", "\u6771\u90ed", "\u5357\u9580", "\u547c\u5ef6",
            "\u6b78", "\u6d77", "\u7f8a\u820c", "\u5fae\u751f", "\u5cb3", "\u5e25", "\u7df1", "\u4ea2", "\u6cc1",
            "\u5f8c", "\u6709", "\u7434", "\u6881\u4e18", "\u5de6\u4e18", "\u6771\u9580", "\u897f\u9580", "\u5546",
            "\u725f", "\u4f58", "\u4f74", "\u4f2f", "\u8cde", "\u5357\u5bae", "\u58a8", "\u54c8", "\u8b59", "\u7b2a",
            "\u5e74", "\u611b", "\u967d", "\u4f5f", "\u3401", "\u3422", "\u3426", "\u3493", "\u34A5", "\u34A7",
            "\u34AA", "\u3536", "\u4A3B", "\u4E00", "\u4E01", "\u4E07", "\u4E0D", "\u4E17", "\u4E23", "\u4E26",
            "\u4E34", "\u4E82", "\u4EB8", "\u4EB9", "\u511F", "\u512D", "\u513D", "\u513E", "\u53B5", "\u56D4",
            "\u56D6", "\u7065", "\u7069", "\u706A", "\u7E9E", "\u9750", "\u9F49", "\u9F7E", "\u9F98", "\uD840\uDC35",
            "\uD840\uDC3D", "\uD840\uDC3E", "\uD840\uDC41", "\uD840\uDC46", "\uD840\uDC4C", "\uD840\uDC4E",
            "\uD840\uDC53", "\uD840\uDC55", "\uD840\uDC56", "\uD840\uDC5F", "\uD840\uDC60", "\uD840\uDC7A",
            "\uD840\uDC7B", "\uD840\uDCC8", "\uD840\uDD9E", "\uD840\uDD9F", "\uD840\uDDA0", "\uD840\uDDA1",
            "\uD841\uDD3B", "\uD842\uDCCA", "\uD842\uDCCB", "\uD842\uDD6C", "\uD842\uDE0B", "\uD842\uDE0C",
            "\uD842\uDED1", "\uD844\uDD9F", "\uD845\uDD19", "\uD845\uDD1A", "\uD846\uDD3B", "\uD84C\uDF5C",
            "\uD85A\uDDC4", "\uD85A\uDDC5", "\uD85C\uDD98", "\uD85E\uDCB1", "\uD861\uDC04", "\uD864\uDDD3",
            "\uD865\uDE63", "\uD869\uDCCA", "\uD86B\uDE9A", };

        /**
         * Test AlphabeticIndex vs. root with script reordering.
         */
        [Test]
        public void TestHaniFirst()
        {
            RuleBasedCollator coll = (RuleBasedCollator)Collator.GetInstance(UCultureInfo.InvariantCulture);
            coll.SetReorderCodes(UScript.Han);
            AlphabeticIndex<object> index = new AlphabeticIndex<object>(coll);
            assertEquals("getBucketCount()", 1, index.BucketCount);   // ... (underflow only)
            index.AddLabels(new CultureInfo("en") /* Locale.ENGLISH */);
            assertEquals("getBucketCount()", 28, index.BucketCount);  // ... A-Z ...
            int bucketIndex = index.GetBucketIndex("\u897f");
            assertEquals("getBucketIndex(U+897F)", 0, bucketIndex);  // underflow bucket
            bucketIndex = index.GetBucketIndex("i");
            assertEquals("getBucketIndex(i)", 9, bucketIndex);
            bucketIndex = index.GetBucketIndex("\u03B1");
            assertEquals("getBucketIndex(Greek alpha)", 27, bucketIndex);
            // U+50005 is an unassigned code point which sorts at the end, independent of the Hani group.
            bucketIndex = index.GetBucketIndex(UTF16.ValueOf(0x50005));
            assertEquals("getBucketIndex(U+50005)", 27, bucketIndex);
            bucketIndex = index.GetBucketIndex("\uFFFF");
            assertEquals("getBucketIndex(U+FFFF)", 27, bucketIndex);
        }

        /**
         * Test AlphabeticIndex vs. Pinyin with script reordering.
         */
        [Test]
        public void TestPinyinFirst()
        {
            RuleBasedCollator coll = (RuleBasedCollator)Collator.GetInstance(new UCultureInfo("zh"));
            coll.SetReorderCodes(UScript.Han);
            AlphabeticIndex<object> index = new AlphabeticIndex<object>(coll);
            assertEquals("getBucketCount()", 28, index.BucketCount);   // ... A-Z ...
            index.AddLabels(new CultureInfo("zh") /* Locale.CHINESE */);
            assertEquals("getBucketCount()", 28, index.BucketCount);  // ... A-Z ...
            int bucketIndex = index.GetBucketIndex("\u897f");
            assertEquals("getBucketIndex(U+897F)", 'X' - 'A' + 1, bucketIndex);
            bucketIndex = index.GetBucketIndex("i");
            assertEquals("getBucketIndex(i)", 9, bucketIndex);
            bucketIndex = index.GetBucketIndex("\u03B1");
            assertEquals("getBucketIndex(Greek alpha)", 27, bucketIndex);
            // U+50005 is an unassigned code point which sorts at the end, independent of the Hani group.
            bucketIndex = index.GetBucketIndex(UTF16.ValueOf(0x50005));
            assertEquals("getBucketIndex(U+50005)", 27, bucketIndex);
            bucketIndex = index.GetBucketIndex("\uFFFF");
            assertEquals("getBucketIndex(U+FFFF)", 27, bucketIndex);
        }

        /**
         * Test labels with multiple primary weights.
         */
        [Test]
        public void TestSchSt()
        {
            AlphabeticIndex<object> index = new AlphabeticIndex<object>(new UCultureInfo("de"));
            index.AddLabels(new UnicodeSet("[Æ{Sch*}{St*}]"));
            // ... A Æ B-R S Sch St T-Z ...
            var immIndex = index.BuildImmutableIndex();
            assertEquals("getBucketCount()", 31, index.BucketCount);
            assertEquals("immutable getBucketCount()", 31, immIndex.BucketCount);
            String[][] testCases = new String[][] {
            // name, bucket index, bucket label
            new string[] { "Adelbert", "1", "A" },
            new string[] { "Afrika", "1", "A" },
            new string[] { "Æsculap", "2", "Æ" },
            new string[] { "Aesthet", "2", "Æ" },
            new string[] { "Berlin", "3", "B" },
            new string[] { "Rilke", "19", "R" },
            new string[] { "Sacher", "20", "S" },
            new string[] { "Seiler", "20", "S" },
            new string[] { "Sultan", "20", "S" },
            new string[] { "Schiller", "21", "Sch" },
            new string[] { "Steiff", "22", "St" },
            new string[] { "Thomas", "23", "T" }
        };
            IList<String> labels = index.GetBucketLabels();
            foreach (String[] testCase in testCases)
            {
                String name = testCase[0];
                int bucketIndex = int.Parse(testCase[1], CultureInfo.InvariantCulture);
                String label = testCase[2];
                String msg = "getBucketIndex(" + name + ")";
                assertEquals(msg, bucketIndex, index.GetBucketIndex(name));
                msg = "immutable " + msg;
                assertEquals(msg, bucketIndex, immIndex.GetBucketIndex(name));
                msg = "bucket label (" + name + ")";
                assertEquals(msg, label, labels[index.GetBucketIndex(name)]);
                msg = "immutable " + msg;
                assertEquals(msg, label, immIndex.GetBucket(bucketIndex).Label);
            }
        }

        /**
         * With no real labels, there should be only the underflow label.
         */
        [Test]
        public void TestNoLabels()
        {
            RuleBasedCollator coll = (RuleBasedCollator)Collator.GetInstance(UCultureInfo.InvariantCulture);
            AlphabeticIndex<int> index = new AlphabeticIndex<int>(coll);
            index.AddRecord("\u897f", 0);
            index.AddRecord("i", 0);
            index.AddRecord("\u03B1", 0);
            assertEquals("getRecordCount()", 3, index.RecordCount);  // code coverage
            assertEquals("getBucketCount()", 1, index.BucketCount);  // ...
            var bucket = index.First();
            assertEquals("underflow label type", BucketLabelType.Underflow, bucket.LabelType);
            assertEquals("all records in the underflow bucket", 3, bucket.Count);
        }

        /**
         * Test with the Bopomofo-phonetic tailoring.
         */
        [Test]
        public void TestChineseZhuyin()
        {
            AlphabeticIndex<object> index = new AlphabeticIndex<object>(UCultureInfo.GetCultureInfoByIetfLanguageTag("zh-u-co-zhuyin"));
            var immIndex = index.BuildImmutableIndex();
            assertEquals("getBucketCount()", 38, immIndex.BucketCount);  // ... ㄅ ㄆ ㄇ ㄈ ㄉ -- ㄩ ...
            assertEquals("label 1", "ㄅ", immIndex.GetBucket(1).Label);
            assertEquals("label 2", "ㄆ", immIndex.GetBucket(2).Label);
            assertEquals("label 3", "ㄇ", immIndex.GetBucket(3).Label);
            assertEquals("label 4", "ㄈ", immIndex.GetBucket(4).Label);
            assertEquals("label 5", "ㄉ", immIndex.GetBucket(5).Label);
        }

        [Test]
        public void TestJapaneseKanji()
        {
            AlphabeticIndex<object> index = new AlphabeticIndex<object>(new UCultureInfo("ja"));
            ImmutableIndex<object> immIndex = index.BuildImmutableIndex();
            // There are no index characters for Kanji in the Japanese standard collator.
            // They should all go into the overflow bucket.
            int[] kanji = { 0x4E9C, 0x95C7, 0x4E00, 0x58F1 };
            int overflowIndex = immIndex.BucketCount - 1;
            for (int i = 0; i < kanji.Length; ++i)
            {
                String msg = String.Format("kanji[{0}]=U+{1:x4} in overflow bucket", i, kanji[i]);
                assertEquals(msg, overflowIndex, immIndex.GetBucketIndex(UTF16.ValueOf(kanji[i])));
            }
        }

        [Test]
        public void TestFrozenCollator()
        {
            // Ticket #9472
            RuleBasedCollator coll = (RuleBasedCollator)Collator.GetInstance(new UCultureInfo("da"));
            coll.Strength = (Collator.Identical);
            coll.Freeze();
            // The AlphabeticIndex constructor used to throw an exception
            // because it cloned the collator (which preserves frozenness)
            // and set the clone's strength to PRIMARY.
            AlphabeticIndex<object> index = new AlphabeticIndex<object>(coll);
            assertEquals("same strength as input Collator",
                    Collator.Identical, index.Collator.Strength);
        }

        [Test]
        public void TestChineseUnihan()
        {
            AlphabeticIndex<object> index = new AlphabeticIndex<object>(new UCultureInfo("zh-u-co-unihan"));
            index.SetMaxLabelCount(500);  // ICU 54 default is 99.
            assertEquals("getMaxLabelCount()", 500, index.MaxLabelCount);  // code coverage
            ImmutableIndex<object> immIndex = index.BuildImmutableIndex();
            int bucketCount = immIndex.BucketCount;
            if (bucketCount < 216)
            {
                // There should be at least an underflow and overflow label,
                // and one for each of 214 radicals,
                // and maybe additional labels for simplified radicals.
                // (ICU4C: dataerrln(), prints only a warning if the data is missing)
                Errln("too few buckets/labels for Chinese/unihan: " + bucketCount +
                        " (is zh/unihan data available?)");
                return;
            }
            else
            {
                Logln("Chinese/unihan has " + bucketCount + " buckets/labels");
            }
            // bucketIndex = radical number, adjusted for simplified radicals in lower buckets.
            int bucketIndex = index.GetBucketIndex("\u4e5d");
            assertEquals("getBucketIndex(U+4E5D)", 5, bucketIndex);
            // radical 100, and there is a 90' since Unicode 8
            bucketIndex = index.GetBucketIndex("\u7527");
            assertEquals("getBucketIndex(U+7527)", 101, bucketIndex);
        }

        [Test]
        public void TestAddLabels_Locale()
        {
            AlphabeticIndex<string> ulocaleIndex = new AlphabeticIndex<String>(new UCultureInfo("en_CA"));
            AlphabeticIndex<string> localeIndex = new AlphabeticIndex<String>(new CultureInfo("en-CA") /* Locale.CANADA */);
            ulocaleIndex.AddLabels(new UCultureInfo("zh_Hans") /* ULocale.SIMPLIFIED_CHINESE */);
            localeIndex.AddLabels(new CultureInfo("zh-Hans") /*  Locale.SIMPLIFIED_CHINESE */);
            assertEquals("getBucketLables() results of ulocaleIndex and localeIndex differ",
                    ulocaleIndex.GetBucketLabels(), localeIndex.GetBucketLabels());
        }

        [Test]
        public void TestGetRecordCount_empty()
        {
            assertEquals("Record count of empty index not 0", 0,
                    new AlphabeticIndex<String>(new UCultureInfo("en_CA")).RecordCount);
        }

        [Test]
        public void TestGetRecordCount_withRecords()
        {
            assertEquals("Record count of index with one record not 1", 1,
                    new AlphabeticIndex<String>(new UCultureInfo("en_CA")).AddRecord("foo", null).RecordCount);
        }

        /**
         * Check that setUnderflowLabel/setOverflowLabel/setInflowLabel correctly influence the name of
         * generated labels.
         */
        [Test]
        public void TestFlowLabels()
        {
            AlphabeticIndex<string> index = new AlphabeticIndex<string>(new UCultureInfo("en"))
                    .AddLabels(UCultureInfo.GetCultureInfoByIetfLanguageTag("ru"));
            index.SetUnderflowLabel("underflow");
            index.SetOverflowLabel("overflow");
            index.SetInflowLabel("inflow");
            index.AddRecord("!", null);
            index.AddRecord("\u03B1", null); // GREEK SMALL LETTER ALPHA
            index.AddRecord("\uab70", null); // CHEROKEE SMALL LETTER A
            Bucket<string> underflowBucket = null;
            Bucket<string> overflowBucket = null;
            Bucket<string> inflowBucket = null;
            foreach (var bucket in index)
            {
                switch (bucket.LabelType)
                {
                    case BucketLabelType.Underflow:
                        assertNull("LabelType not null", underflowBucket);
                        underflowBucket = bucket;
                        break;
                    case BucketLabelType.Overflow:
                        assertNull("LabelType not null", overflowBucket);
                        overflowBucket = bucket;
                        break;
                    case BucketLabelType.Inflow:
                        assertNull("LabelType not null", inflowBucket);
                        inflowBucket = bucket;
                        break;
                }
            }
            assertNotNull("No bucket 'underflow'", underflowBucket);
            assertEquals("Wrong bucket label", "underflow", underflowBucket.Label);
            assertEquals("Wrong bucket label", "underflow", index.UnderflowLabel);
            assertEquals("Bucket size not 1", 1, underflowBucket.Count);
            assertNotNull("No bucket 'overflow'", overflowBucket);
            assertEquals("Wrong bucket label", "overflow", overflowBucket.Label);
            assertEquals("Wrong bucket label", "overflow", index.OverflowLabel);
            assertEquals("Bucket size not 1", 1, overflowBucket.Count);
            assertNotNull("No bucket 'inflow'", inflowBucket);
            assertEquals("Wrong bucket label", "inflow", inflowBucket.Label);
            assertEquals("Wrong bucket label", "inflow", index.InflowLabel);
            assertEquals("Bucket size not 1", 1, inflowBucket.Count);
        }
    }
}
