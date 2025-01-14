﻿using ICU4N.Dev.Test;
using ICU4N.Dev.Test.Util;
using ICU4N.Support.Collections;
using ICU4N.Util;
using J2N.Collections.Generic.Extensions;
using J2N.Text;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace ICU4N.Globalization
{
    public class UCultureInfoTest : TestFmwk
    {
        [Test]
        public void TestSetULocaleKeywords()
        {
            UCultureInfo uloc = new UCultureInfo("en_Latn_US");
            uloc = uloc.SetKeywordValue("Foo", "FooValue");
            if (!"en_Latn_US@foo=FooValue".Equals(uloc.FullName))
            {
                Errln("failed to add foo keyword, got: " + uloc.FullName);
            }
            uloc = uloc.SetKeywordValue("Bar", "BarValue");
            if (!"en_Latn_US@bar=BarValue;foo=FooValue".Equals(uloc.FullName))
            {
                Errln("failed to add bar keyword, got: " + uloc.FullName);
            }
            uloc = uloc.SetKeywordValue("BAR", "NewBarValue");
            if (!"en_Latn_US@bar=NewBarValue;foo=FooValue".Equals(uloc.FullName))
            {
                Errln("failed to change bar keyword, got: " + uloc.FullName);
            }
            uloc = uloc.SetKeywordValue("BaR", null);
            if (!"en_Latn_US@foo=FooValue".Equals(uloc.FullName))
            {
                Errln("failed to delete bar keyword, got: " + uloc.FullName);
            }
            uloc = uloc.SetKeywordValue(null, null);
            if (!"en_Latn_US".Equals(uloc.FullName))
            {
                Errln("failed to delete all keywords, got: " + uloc.FullName);
            }
        }


        // ================= Infrastructure =================

        /**
         * Compare two locale IDs.  If they are equal, return 0.  If `string'
         * starts with `prefix' plus an additional element, that is, string ==
         * prefix + '_' + x, then return 1.  Otherwise return a value &lt; 0.
         */
        internal static int loccmp(String str, String prefix)
        {
            int slen = str.Length,
                    plen = prefix.Length;
            /* 'root' is "less than" everything */
            if (prefix.Equals("root"))
            {
                return str.Equals("root") ? 0 : 1;
            }
            // ON JAVA (only -- not on C -- someone correct me if I'm wrong)
            // consider "" to be an alternate name for "root".
            if (plen == 0)
            {
                return slen == 0 ? 0 : 1;
            }
            if (!str.StartsWith(prefix, StringComparison.Ordinal)) return -1; /* mismatch */
            if (slen == plen) return 0;
            if (str[plen] == '_') return 1;
            return -2; /* false match, e.g. "en_USX" cmp "en_US" */
        }

        /**
         * Check the relationship between requested locales, and report problems.
         * The caller specifies the expected relationships between requested
         * and valid (expReqValid) and between valid and actual (expValidActual).
         * Possible values are:
         * "gt" strictly greater than, e.g., en_US > en
         * "ge" greater or equal,      e.g., en >= en
         * "eq" equal,                 e.g., en == en
         */
        internal void checklocs(String label,
                String req,
                CultureInfo validLoc,
                CultureInfo actualLoc,
                String expReqValid,
                String expValidActual)
        {
            String valid = validLoc.ToString();
            String actual = actualLoc.ToString();
            int reqValid = loccmp(req, valid);
            int validActual = loccmp(valid, actual);
            bool reqOK = (expReqValid.Equals("gt") && reqValid > 0) ||
                    (expReqValid.Equals("ge") && reqValid >= 0) ||
                    (expReqValid.Equals("eq") && reqValid == 0);
            bool valOK = (expValidActual.Equals("gt") && validActual > 0) ||
                    (expValidActual.Equals("ge") && validActual >= 0) ||
                    (expValidActual.Equals("eq") && validActual == 0);
            if (reqOK && valOK)
            {
                Logln("Ok: " + label + "; req=" + req + ", valid=" + valid +
                        ", actual=" + actual);
            }
            else
            {
                Errln("FAIL: " + label + "; req=" + req + ", valid=" + valid +
                        ", actual=" + actual +
                        (reqOK ? "" : "\n  req !" + expReqValid + " valid") +
                        (valOK ? "" : "\n  val !" + expValidActual + " actual"));
            }
        }

        /**
         * Interface used by checkService defining a protocol to create an
         * object, given a requested locale.
         */
        internal interface IServiceFacade
        {
            object Create(UCultureInfo requestedLocale);
        }

        /**
         * Interface used by checkService defining a protocol to get a
         * contained subobject, given its parent object.
         */
        internal interface ISubObject
        {
            object Get(object parent);
        }

        /**
         * Interface used by checkService defining a protocol to register
         * and unregister a service object prototype.
         */
        internal interface IRegistrar
        {
            object Register(UCultureInfo loc, object prototype);
            bool Unregister(object key);
        }

        /**
         * Use reflection to call getLocale() on the given object to
         * determine both the valid and the actual locale.  Verify these
         * for correctness.
         */
        internal void checkObject(String requestedLocale, Object obj,
                String expReqValid, String expValidActual)
        {
            try
            {
                Type cls = obj.GetType();
                PropertyInfo validProperty = cls.GetProperty("ValidCulture");
                UCultureInfo valid = (UCultureInfo)validProperty.GetValue(obj, new object[0]);
                PropertyInfo actualProperty = cls.GetProperty("ActualCulture");
                UCultureInfo actual = (UCultureInfo)actualProperty.GetValue(obj, new object[0]);

                checklocs(cls.Name, requestedLocale,
                        valid.ToCultureInfo(), actual.ToCultureInfo(),
                        expReqValid, expValidActual);
            }

            // Make the following exceptions _specific_ -- do not
            // catch(Exception), since that will catch the exception
            // that Errln throws.
            catch (MissingMethodException e1)
            {
                // no longer an error, Currency has no getLocale
                // Errln("FAIL: reflection failed: " + e1);
            }
            catch (MethodAccessException e2)
            {
                Errln("FAIL: reflection failed: " + e2);
            }
            catch (InvalidOperationException e3)
            {
                Errln("FAIL: reflection failed: " + e3);
            }
            catch (ArgumentException e4)
            {
                Errln("FAIL: reflection failed: " + e4);
            }
            catch (TargetInvocationException e5)
            {
                // no longer an error, Currency has no getLocale
                // Errln("FAIL: reflection failed: " + e5);
            }
        }

        /**
         * Verify the correct getLocale() behavior for the given service.
         * @param requestedLocale the locale to request.  This MUST BE
         * FAKE.  In other words, it should be something like
         * en_US_FAKEVARIANT so this method can verify correct fallback
         * behavior.
         * @param svc a factory object that can create the object to be
         * tested.  This isn't necessary here (one could just pass in the
         * object) but is required for the overload of this method that
         * takes a Registrar.
         */
        internal void checkService(String requestedLocale, IServiceFacade svc)
        {
            checkService(requestedLocale, svc, null, null);
        }

        /**
         * Verify the correct getLocale() behavior for the given service.
         * @param requestedLocale the locale to request.  This MUST BE
         * FAKE.  In other words, it should be something like
         * en_US_FAKEVARIANT so this method can verify correct fallback
         * behavior.
         * @param svc a factory object that can create the object to be
         * tested.
         * @param sub an object that can be used to retrieve a subobject
         * which should also be tested.  May be null.
         * @param reg an object that supplies the registration and
         * unregistration functionality to be tested.  May be null.
         */
        internal void checkService(String requestedLocale, IServiceFacade svc,
                ISubObject sub, IRegistrar reg)
        {
            UCultureInfo req = new UCultureInfo(requestedLocale);
            Object obj = svc.Create(req);
            checkObject(requestedLocale, obj, "gt", "ge");
            if (sub != null)
            {
                Object subobj = sub.Get(obj);
                checkObject(requestedLocale, subobj, "gt", "ge");
            }
            if (reg != null)
            {
                Logln("Info: Registering service");
                Object key = reg.Register(req, obj);
                Object objReg = svc.Create(req);
                checkObject(requestedLocale, objReg, "eq", "eq");
                if (sub != null)
                {
                    Object subobj = sub.Get(obj);
                    // Assume subobjects don't come from services, so
                    // their metadata should be structured normally.
                    checkObject(requestedLocale, subobj, "gt", "ge");
                }
                Logln("Info: Unregistering service");
                if (!reg.Unregister(key))
                {
                    Errln("FAIL: unregister failed");
                }
                Object objUnreg = svc.Create(req);
                checkObject(requestedLocale, objUnreg, "gt", "ge");
            }
        }
        private const int LOCALE_SIZE = 9;
        private static readonly string[][] rawData2 = new string[][]{
                /* language code */
                new string[]{   "en",   "fr",   "ca",   "el",   "no",   "zh",   "de",   "es",  "ja"    },
                /* script code */
                new string[]{   "",     "",     "",     "",     "",     "Hans", "", "", ""  },
                /* country code */
                new string[]{   "US",   "FR",   "ES",   "GR",   "NO",   "CN", "DE", "", "JP"    },
                /* variant code */
                new string[]{   "",     "",     "",     "",     "NY",   "", "", "", ""      },
                /* full name */
                new string[]{   "en_US",    "fr_FR",    "ca_ES",
                    "el_GR",    "no_NO_NY", "zh_Hans_CN",
                    "de_DE@collation=phonebook", "es@collation=traditional",  "ja_JP@calendar=japanese" },
                /* ISO-3 language */
                new string[]{   "eng",  "fra",  "cat",  "ell",  "nor",  "zho", "deu", "spa", "jpn"   },
                /* ISO-3 country */
                new string[]{   "USA",  "FRA",  "ESP",  "GRC",  "NOR",  "CHN", "DEU", "", "JPN"   },
                /* LCID */
                new string[]{   "409", "40c", "403", "408", "814",  "804", "407", "a", "411"     },

                /* display language (English) */
                new string[]{   "English",  "French",   "Catalan", "Greek",    "Norwegian", "Chinese", "German", "Spanish", "Japanese"    },
                /* display script code (English) */
                new string[]{   "",     "",     "",     "",     "",     "Simplified Han", "", "", ""       },
                /* display country (English) */
                new string[]{   "United States",    "France",   "Spain",  "Greece",   "Norway", "China", "Germany", "", "Japan"       },
                /* display variant (English) */
                new string[]{   "",     "",     "",     "",     "NY",  "", "", "", ""       },
                /* display name (English) */
                new string[]{   "English (United States)", "French (France)", "Catalan (Spain)",
                    "Greek (Greece)", "Norwegian (Norway, NY)", "Chinese (Simplified Han, China)",
                    "German (Germany, Collation=Phonebook Sort Order)", "Spanish (Collation=Traditional)", "Japanese (Japan, Calendar=Japanese Calendar)" },

                /* display language (French) */
                new string[]{   "anglais",  "fran\\u00E7ais",   "catalan", "grec",    "norv\\u00E9gien",    "chinois", "allemand", "espagnol", "japonais"     },
                /* display script code (French) */
                new string[]{   "",     "",     "",     "",     "",     "Hans", "", "", ""         },
                /* display country (French) */
                new string[]{   "\\u00C9tats-Unis",    "France",   "Espagne",  "Gr\\u00E8ce",   "Norv\\u00E8ge",    "Chine", "Allemagne", "", "Japon"       },
                /* display variant (French) */
                new string[]{   "",     "",     "",     "",     "NY",   "", "", "", ""       },
                /* display name (French) */
                new string[]{   "anglais (\\u00C9tats-Unis)", "fran\\u00E7ais (France)", "catalan (Espagne)",
                    "grec (Gr\\u00E8ce)", "norv\\u00E9gien (Norv\\u00E8ge, NY)",  "chinois (Hans, Chine)",
                    "allemand (Allemagne, Ordonnancement=Ordre de l'annuaire)", "espagnol (Ordonnancement=Ordre traditionnel)", "japonais (Japon, Calendrier=Calendrier japonais)" },

                /* display language (Catalan) */
                new string[]{   "angl\\u00E8s", "franc\\u00E8s", "catal\\u00E0", "grec",  "noruec", "xin\\u00E9s", "alemany", "espanyol", "japon\\u00E8s"    },
                /* display script code (Catalan) */
                new string[]{   "",     "",     "",     "",     "",     "Hans", "", "", ""         },
                /* display country (Catalan) */
                new string[]{   "Estats Units", "Fran\\u00E7a", "Espanya",  "Gr\\u00E8cia", "Noruega",  "Xina", "Alemanya", "", "Jap\\u00F3"    },
                /* display variant (Catalan) */
                new string[]{   "", "", "",                    "", "NY",    "", "", "", ""    },
                /* display name (Catalan) */
                new string[]{   "angl\\u00E8s (Estats Units)", "franc\\u00E8s (Fran\\u00E7a)", "catal\\u00E0 (Espanya)",
                    "grec (Gr\\u00E8cia)", "noruec (Noruega, NY)", "xin\\u00E9s (Hans, Xina)",
                    "alemany (Alemanya, COLLATION=PHONEBOOK)", "espanyol (COLLATION=TRADITIONAL)", "japon\\u00E8s (Jap\\u00F3, CALENDAR=JAPANESE)" },

                /* display language (Greek) */
                new string[]{
                    "\\u0391\\u03b3\\u03b3\\u03bb\\u03b9\\u03ba\\u03ac",
                    "\\u0393\\u03b1\\u03bb\\u03bb\\u03b9\\u03ba\\u03ac",
                    "\\u039a\\u03b1\\u03c4\\u03b1\\u03bb\\u03b1\\u03bd\\u03b9\\u03ba\\u03ac",
                    "\\u0395\\u03bb\\u03bb\\u03b7\\u03bd\\u03b9\\u03ba\\u03ac",
                    "\\u039d\\u03bf\\u03c1\\u03b2\\u03b7\\u03b3\\u03b9\\u03ba\\u03ac",
                    "\\u039A\\u03B9\\u03BD\\u03B5\\u03B6\\u03B9\\u03BA\\u03AC",
                    "\\u0393\\u03B5\\u03C1\\u03BC\\u03B1\\u03BD\\u03B9\\u03BA\\u03AC",
                    "\\u0399\\u03C3\\u03C0\\u03B1\\u03BD\\u03B9\\u03BA\\u03AC",
                    "\\u0399\\u03B1\\u03C0\\u03C9\\u03BD\\u03B9\\u03BA\\u03AC"
                },
                /* display script code (Greek) */
                new string[]{   "",     "",     "",     "",     "",     "Hans", "", "", ""         },
                /* display country (Greek) */
                new string[]{
                    "\\u0397\\u03bd\\u03c9\\u03bc\\u03ad\\u03bd\\u03b5\\u03c2 \\u03a0\\u03bf\\u03bb\\u03b9\\u03c4\\u03b5\\u03af\\u03b5\\u03c2",
                    "\\u0393\\u03b1\\u03bb\\u03bb\\u03af\\u03b1",
                    "\\u0399\\u03c3\\u03c0\\u03b1\\u03bd\\u03af\\u03b1",
                    "\\u0395\\u03bb\\u03bb\\u03ac\\u03b4\\u03b1",
                    "\\u039d\\u03bf\\u03c1\\u03b2\\u03b7\\u03b3\\u03af\\u03b1",
                    "\\u039A\\u03AF\\u03BD\\u03B1",
                    "\\u0393\\u03B5\\u03C1\\u03BC\\u03B1\\u03BD\\u03AF\\u03B1",
                    "",
                    "\\u0399\\u03B1\\u03C0\\u03C9\\u03BD\\u03AF\\u03B1"
                },
                /* display variant (Greek) */
                new string[]{   "", "", "", "", "NY", "", "", "", ""    }, /* TODO: currently there is no translation for NY in Greek fix this test when we have it */
                /* display name (Greek) */
                new string[]{
                    "\\u0391\\u03b3\\u03b3\\u03bb\\u03b9\\u03ba\\u03ac (\\u0397\\u03bd\\u03c9\\u03bc\\u03ad\\u03bd\\u03b5\\u03c2 \\u03a0\\u03bf\\u03bb\\u03b9\\u03c4\\u03b5\\u03af\\u03b5\\u03c2)",
                    "\\u0393\\u03b1\\u03bb\\u03bb\\u03b9\\u03ba\\u03ac (\\u0393\\u03b1\\u03bb\\u03bb\\u03af\\u03b1)",
                    "\\u039a\\u03b1\\u03c4\\u03b1\\u03bb\\u03b1\\u03bd\\u03b9\\u03ba\\u03ac (\\u0399\\u03c3\\u03c0\\u03b1\\u03bd\\u03af\\u03b1)",
                    "\\u0395\\u03bb\\u03bb\\u03b7\\u03bd\\u03b9\\u03ba\\u03ac (\\u0395\\u03bb\\u03bb\\u03ac\\u03b4\\u03b1)",
                    "\\u039d\\u03bf\\u03c1\\u03b2\\u03b7\\u03b3\\u03b9\\u03ba\\u03ac (\\u039d\\u03bf\\u03c1\\u03b2\\u03b7\\u03b3\\u03af\\u03b1, NY)",
                    "\\u039A\\u03B9\\u03BD\\u03B5\\u03B6\\u03B9\\u03BA\\u03AC (Hans, \\u039A\\u03AF\\u03BD\\u03B1)",
                    "\\u0393\\u03B5\\u03C1\\u03BC\\u03B1\\u03BD\\u03B9\\u03BA\\u03AC (\\u0393\\u03B5\\u03C1\\u03BC\\u03B1\\u03BD\\u03AF\\u03B1, COLLATION=PHONEBOOK)",
                    "\\u0399\\u03C3\\u03C0\\u03B1\\u03BD\\u03B9\\u03BA\\u03AC (COLLATION=TRADITIONAL)",
                    "\\u0399\\u03B1\\u03C0\\u03C9\\u03BD\\u03B9\\u03BA\\u03AC (\\u0399\\u03B1\\u03C0\\u03C9\\u03BD\\u03AF\\u03B1, CALENDAR=JAPANESE)"
                }
        };
        //    private const int ENGLISH = 0;
        //    private const int FRENCH = 1;
        //    private const int CATALAN = 2;
        //    private const int GREEK = 3;
        //    private const int NORWEGIAN = 4;
        private const int LANG = 0;
        private const int SCRIPT = 1;
        private const int CTRY = 2;
        private const int VAR = 3;
        private const int NAME = 4;
        //    private const int LANG3 = 5;
        //    private const int CTRY3 = 6;
        //    private const int LCID = 7;
        //    private const int DLANG_EN = 8;
        //    private const int DSCRIPT_EN = 9;
        //    private const int DCTRY_EN = 10;
        //    private const int DVAR_EN = 11;
        //    private const int DNAME_EN = 12;
        //    private const int DLANG_FR = 13;
        //    private const int DSCRIPT_FR = 14;
        //    private const int DCTRY_FR = 15;
        //    private const int DVAR_FR = 16;
        //    private const int DNAME_FR = 17;
        //    private const int DLANG_CA = 18;
        //    private const int DSCRIPT_CA = 19;
        //    private const int DCTRY_CA = 20;
        //    private const int DVAR_CA = 21;
        //    private const int DNAME_CA = 22;
        //    private const int DLANG_EL = 23;
        //    private const int DSCRIPT_EL = 24;
        //    private const int DCTRY_EL = 25;
        //    private const int DVAR_EL = 26;
        //    private const int DNAME_EL = 27;



        [Test]
        public void TestBasicGetters()
        {
            int i;
            Logln("Testing Basic Getters\n");
            for (i = 0; i < LOCALE_SIZE; i++)
            {
                String testLocale = (rawData2[NAME][i]);
                Logln("Testing " + testLocale + ".....\n");

                String lang = UCultureInfo.GetLanguage(testLocale);
                if (0 != lang.CompareToOrdinal(rawData2[LANG][i]))
                {
                    Errln("  Language code mismatch: " + lang + " versus " + rawData2[LANG][i]);
                }

                String ctry = UCultureInfo.GetCountry(testLocale);
                if (0 != ctry.CompareToOrdinal(rawData2[CTRY][i]))
                {
                    Errln("  Country code mismatch: " + ctry + " versus " + rawData2[CTRY][i]);
                }

                String var = UCultureInfo.GetVariant(testLocale);
                if (0 != var.CompareToOrdinal(rawData2[VAR][i]))
                {
                    Errln("  Variant code mismatch: " + var + " versus " + rawData2[VAR][i]);
                }

                String name = UCultureInfo.GetFullName(testLocale);
                if (0 != name.CompareToOrdinal(rawData2[NAME][i]))
                {
                    Errln("  Name mismatch: " + name + " versus " + rawData2[NAME][i]);
                }

            }
        }

        [Test]
        public void TestPrefixes()
        {
            // POSIX ids are no longer handled by getName, so POSIX failures are ignored
            string[][] testData = new string[][]{
                    /* null canonicalize() column means "expect same as getName()" */
                    new string[]{"sv", "", "FI", "AL", "sv-fi-al", "sv_FI_AL", null},
                    new string[]{"en", "", "GB", "", "en-gb", "en_GB", null},
                    new string[]{"i-hakka", "", "MT", "XEMXIJA", "i-hakka_MT_XEMXIJA", "i-hakka_MT_XEMXIJA", null},
                    new string[]{"i-hakka", "", "CN", "", "i-hakka_CN", "i-hakka_CN", null},
                    new string[]{"i-hakka", "", "MX", "", "I-hakka_MX", "i-hakka_MX", null},
                    new string[]{"x-klingon", "", "US", "SANJOSE", "X-KLINGON_us_SANJOSE", "x-klingon_US_SANJOSE", null},

                    new string[]{"de", "", "", "1901", "de-1901", "de__1901", null},
                    new string[]{"mr", "", "", "", "mr.utf8", "mr.utf8", "mr"},
                    new string[]{"de", "", "TV", "", "de-tv.koi8r", "de_TV.koi8r", "de_TV"},
                    new string[]{"x-piglatin", "", "ML", "", "x-piglatin_ML.MBE", "x-piglatin_ML.MBE", "x-piglatin_ML"},  /* Multibyte English */
                    new string[]{"i-cherokee", "","US", "", "i-Cherokee_US.utf7", "i-cherokee_US.utf7", "i-cherokee_US"},
                    new string[]{"x-filfli", "", "MT", "FILFLA", "x-filfli_MT_FILFLA.gb-18030", "x-filfli_MT_FILFLA.gb-18030", "x-filfli_MT_FILFLA"},
                    new string[]{"no", "", "NO", "NY_B", "no-no-ny.utf32@B", "no_NO_NY.utf32@B", "no_NO_NY_B"},
                    new string[]{"no", "", "NO", "B",  "no-no.utf32@B", "no_NO.utf32@B", "no_NO_B"},
                    new string[]{"no", "", "",   "NY", "no__ny", "no__NY", null},
                    new string[]{"no", "", "",   "NY", "no@ny", "no@ny", "no__NY"},
                    new string[]{"el", "Latn", "", "", "el-latn", "el_Latn", null},
                    new string[]{"en", "Cyrl", "RU", "", "en-cyrl-ru", "en_Cyrl_RU", null},
                    new string[]{"zh", "Hant", "TW", "STROKE", "zh-hant_TW_STROKE", "zh_Hant_TW_STROKE", "zh_Hant_TW@collation=stroke"},
                    new string[]{"zh", "Hant", "CN", "STROKE", "zh-hant_CN_STROKE", "zh_Hant_CN_STROKE", "zh_Hant_CN@collation=stroke"},
                    new string[]{"zh", "Hant", "TW", "PINYIN", "zh-hant_TW_PINYIN", "zh_Hant_TW_PINYIN", "zh_Hant_TW@collation=pinyin"},
                    new string[]{"qq", "Qqqq", "QQ", "QQ", "qq_Qqqq_QQ_QQ", "qq_Qqqq_QQ_QQ", null},
                    new string[]{"qq", "Qqqq", "", "QQ", "qq_Qqqq__QQ", "qq_Qqqq__QQ", null},
                    new string[]{"ab", "Cdef", "GH", "IJ", "ab_cdef_gh_ij", "ab_Cdef_GH_IJ", null}, /* total garbage */

                    // odd cases
                    new string[]{"", "", "", "", "@FOO=bar", "@foo=bar", null},
                    new string[]{"", "", "", "", "_@FOO=bar", "@foo=bar", null},
                    new string[]{"", "", "", "", "__@FOO=bar", "@foo=bar", null},
                    new string[]{"", "", "", "FOO", "__foo@FOO=bar", "__FOO@foo=bar", null}, // we have some of these prefixes
            };

            string loc, buf, buf1;
            string[] testTitles = {
                    "UCultureInfo.Language",
                    "UCultureInfo.Script",
                    "UCultureInfo.Country",
                    "UCultureInfo.Variant",
                    "Name",
                    "UCultureInfo.FullName",
                    "Canonicalize()",
            };
            UCultureInfo uloc;

            for (int row = 0; row < testData.Length; row++)
            {
                loc = testData[row][NAME];
                Logln("Test #" + row + ": " + loc);

                uloc = new UCultureInfo(loc);

                for (int n = 0; n <= (NAME + 2); n++)
                {
                    if (n == NAME) continue;

                    switch (n)
                    {
                        case LANG:
                            buf = UCultureInfo.GetLanguage(loc);
                            buf1 = uloc.Language;
                            break;

                        case SCRIPT:
                            buf = UCultureInfo.GetScript(loc);
                            buf1 = uloc.Script;
                            break;

                        case CTRY:
                            buf = UCultureInfo.GetCountry(loc);
                            buf1 = uloc.Country;
                            break;

                        case VAR:
                            buf = UCultureInfo.GetVariant(loc);
                            buf1 = buf;
                            break;

                        case NAME + 1:
                            buf = UCultureInfo.GetFullName(loc);
                            buf1 = uloc.FullName;
                            break;

                        case NAME + 2:
                            buf = UCultureInfo.Canonicalize(loc);
                            buf1 = UCultureInfo.CreateCanonical(loc).FullName;
                            break;

                        default:
                            buf = "**??";
                            buf1 = buf;
                            break;
                    }

                    Logln("#" + row + ": " + testTitles[n] + " on " + loc + ": -> [" + buf + "]");

                    string expected = testData[row][n];
                    if (expected == null && n == (NAME + 2))
                    {
                        expected = testData[row][NAME + 1];
                    }

                    // ignore POSIX failures in getName, we don't spec behavior in this case
                    if (n == NAME + 1 &&
                            (expected.IndexOf('.') != -1 ||
                            expected.IndexOf('@') != -1))
                    {
                        continue;
                    }

                    if (buf.CompareToOrdinal(expected) != 0)
                    {
                        Errln("#" + row + ": " + testTitles[n] + " on " + loc + ": -> [" + buf + "] (expected '" + expected + "'!)");
                    }
                    if (buf1.CompareToOrdinal(expected) != 0)
                    {
                        Errln("#" + row + ": " + testTitles[n] + " on UCultureInfo object " + loc + ": -> [" + buf1 + "] (expected '" + expected + "'!)");
                    }
                }
            }
        }

        [Test]
        public void TestUldnWithGarbage()
        {
            CultureDisplayNames ldn = CultureDisplayNames.GetInstance(new CultureInfo("en-US"),  DialectHandling.DialectNames);
            String badLocaleID = "english (United States) [w";
            String expectedResult = "english [united states] [w"; // case changed from input
            String result = ldn.GetLocaleDisplayName(badLocaleID);
            if (result.CompareToOrdinal(expectedResult) != 0)
            {
                Errln("FAIL: CultureDisplayNames.GetLocaleDisplayName(string) for bad locale ID \"" + badLocaleID + "\", expected \"" + expectedResult + "\", got \"" + result + "\"");
            }
            UCultureInfo badLocale = new UCultureInfo(badLocaleID);
            result = ldn.GetLocaleDisplayName(badLocale);
            if (result.CompareToOrdinal(expectedResult) != 0)
            {
                Errln("FAIL: CultureDisplayNames.GetLocaleDisplayName(UCultureInfo) for bad locale ID \"" + badLocaleID + "\", expected \"" + expectedResult + "\", got \"" + result + "\"");
            }
        }

        [Test]
        public void TestObsoleteNames()
        {
            string[][] tests = new string[][]{
                    /* locale, language3, language2, Country3, country2 */
                    new string[]{ "eng_USA", "eng", "en", "USA", "US" },
                    new string[]{ "kok",  "kok", "kok", "", "" },
                    new string[]{ "in",  "ind", "in", "", "" },
                    new string[]{ "id",  "ind", "id", "", "" }, /* NO aliasing */
                    new string[]{ "sh",  "srp", "sh", "", "" },
                    new string[]{ "zz_CS",  "", "zz", "SCG", "CS" },
                    new string[]{ "zz_FX",  "", "zz", "FXX", "FX" },
                    new string[]{ "zz_RO",  "", "zz", "ROU", "RO" },
                    new string[]{ "zz_TP",  "", "zz", "TMP", "TP" },
                    new string[]{ "zz_TL",  "", "zz", "TLS", "TL" },
                    new string[]{ "zz_ZR",  "", "zz", "ZAR", "ZR" },
                    new string[]{ "zz_FXX",  "", "zz", "FXX", "FX" }, /* no aliasing. Doesn't go to PS(PSE). */
                    new string[]{ "zz_ROM",  "", "zz", "ROU", "RO" },
                    new string[]{ "zz_ROU",  "", "zz", "ROU", "RO" },
                    new string[]{ "zz_ZAR",  "", "zz", "ZAR", "ZR" },
                    new string[]{ "zz_TMP",  "", "zz", "TMP", "TP" },
                    new string[]{ "zz_TLS",  "", "zz", "TLS", "TL" },
                    new string[]{ "zz_YUG",  "", "zz", "YUG", "YU" },
                    new string[]{ "mlt_PSE", "mlt", "mt", "PSE", "PS" },
                    new string[]{ "iw", "heb", "iw", "", "" },
                    new string[]{ "ji", "yid", "ji", "", "" },
                    new string[]{ "jw", "jaw", "jw", "", "" },
                    new string[]{ "sh", "srp", "sh", "", "" },
                    new string[]{ "", "", "", "", "" }
            };

            for (int i = 0; i < tests.Length; i++)
            {
                String locale = tests[i][0];
                Logln("** Testing : " + locale);
                String buff, buff1;
                UCultureInfo uloc = new UCultureInfo(locale);

                buff = UCultureInfo.GetThreeLetterISOLanguageName(locale);
                if (buff.CompareToOrdinal(tests[i][1]) != 0)
                {
                    Errln("FAIL: UCultureInfo.GetThreeLetterISOLanguageName(" + locale + ")==" +
                            buff + ",\t expected " + tests[i][1]);
                }
                else
                {
                    Logln("   UCultureInfo.GetThreeLetterISOLanguageName(" + locale + ")==" + buff);
                }

                buff1 = uloc.ThreeLetterISOLanguageName;
                if (buff1.CompareToOrdinal(tests[i][1]) != 0)
                {
                    Errln("FAIL: UCultureInfo.ThreeLetterISOLanguageName(" + locale + ")==" +
                            buff + ",\t expected " + tests[i][1]);
                }
                else
                {
                    Logln("   UCultureInfo.ThreeLetterISOLanguageName(" + locale + ")==" + buff);
                }

                buff = UCultureInfo.GetLanguage(locale);
                if (buff.CompareToOrdinal(tests[i][2]) != 0)
                {
                    Errln("FAIL: UCultureInfo.GetLanguage(" + locale + ")==" +
                            buff + ",\t expected " + tests[i][2]);
                }
                else
                {
                    Logln("   UCultureInfo.GetLanguage(" + locale + ")==" + buff);
                }

                buff = UCultureInfo.GetThreeLetterISOCountryName(locale);
                if (buff.CompareToOrdinal(tests[i][3]) != 0)
                {
                    Errln("FAIL: UCultureInfo.GetThreeLetterISOCountryName(" + locale + ")==" +
                            buff + ",\t expected " + tests[i][3]);
                }
                else
                {
                    Logln("   UCultureInfo.GetThreeLetterISOCountryName(" + locale + ")==" + buff);
                }

                buff1 = uloc.ThreeLetterISOCountryName;
                if (buff1.CompareToOrdinal(tests[i][3]) != 0)
                {
                    Errln("FAIL: UCultureInfo.ThreeLetterISOCountryName (" + locale + ")==" +
                            buff + ",\t expected " + tests[i][3]);
                }
                else
                {
                    Logln("   UCultureInfo.ThreeLetterISOCountryName (" + locale + ")==" + buff);
                }

                buff = UCultureInfo.GetCountry(locale);
                if (buff.CompareToOrdinal(tests[i][4]) != 0)
                {
                    Errln("FAIL: UCultureInfo.GetCountry(" + locale + ")==" +
                            buff + ",\t expected " + tests[i][4]);
                }
                else
                {
                    Logln("   UCultureInfo.GetCountry(" + locale + ")==" + buff);
                }
            }

            if (UCultureInfo.GetLanguage("iw_IL").CompareToOrdinal(UCultureInfo.GetLanguage("he_IL")) == 0)
            {
                Errln("he,iw UCultureInfo.GetLanguage mismatch");
            }

            String buff2 = UCultureInfo.GetLanguage("kok_IN");
            if (buff2.CompareToOrdinal("kok") != 0)
            {
                Errln("UCultureInfo.GetLanguage(\"kok\") failed. Expected: kok Got: " + buff2);
            }
        }

        [Test]
        public void TestCanonicalization()
        {
            string[][] testCases = new string[][]{
                new string[]{ "ca_ES_PREEURO", "ca_ES_PREEURO", "ca_ES@currency=ESP" },
                new string[]{ "de_AT_PREEURO", "de_AT_PREEURO", "de_AT@currency=ATS" },
                new string[]{ "de_DE_PREEURO", "de_DE_PREEURO", "de_DE@currency=DEM" },
                new string[]{ "de_LU_PREEURO", "de_LU_PREEURO", "de_LU@currency=EUR" },
                new string[]{ "el_GR_PREEURO", "el_GR_PREEURO", "el_GR@currency=GRD" },
                new string[]{ "en_BE_PREEURO", "en_BE_PREEURO", "en_BE@currency=BEF" },
                new string[]{ "en_IE_PREEURO", "en_IE_PREEURO", "en_IE@currency=IEP" },
                new string[]{ "es_ES_PREEURO", "es_ES_PREEURO", "es_ES@currency=ESP" },
                new string[]{ "eu_ES_PREEURO", "eu_ES_PREEURO", "eu_ES@currency=ESP" },
                new string[]{ "fi_FI_PREEURO", "fi_FI_PREEURO", "fi_FI@currency=FIM" },
                new string[]{ "fr_BE_PREEURO", "fr_BE_PREEURO", "fr_BE@currency=BEF" },
                new string[]{ "fr_FR_PREEURO", "fr_FR_PREEURO", "fr_FR@currency=FRF" },
                new string[]{ "fr_LU_PREEURO", "fr_LU_PREEURO", "fr_LU@currency=LUF" },
                new string[]{ "ga_IE_PREEURO", "ga_IE_PREEURO", "ga_IE@currency=IEP" },
                new string[]{ "gl_ES_PREEURO", "gl_ES_PREEURO", "gl_ES@currency=ESP" },
                new string[]{ "it_IT_PREEURO", "it_IT_PREEURO", "it_IT@currency=ITL" },
                new string[]{ "nl_BE_PREEURO", "nl_BE_PREEURO", "nl_BE@currency=BEF" },
                new string[]{ "nl_NL_PREEURO", "nl_NL_PREEURO", "nl_NL@currency=NLG" },
                new string[]{ "pt_PT_PREEURO", "pt_PT_PREEURO", "pt_PT@currency=PTE" },
                new string[]{ "de__PHONEBOOK", "de__PHONEBOOK", "de@collation=phonebook" },
                new string[]{ "de_PHONEBOOK", "de__PHONEBOOK", "de@collation=phonebook" },
                new string[]{ "en_GB_EURO", "en_GB_EURO", "en_GB@currency=EUR" },
                new string[]{ "en_GB@EURO", null, "en_GB@currency=EUR" }, /* POSIX ID */
                new string[]{ "es__TRADITIONAL", "es__TRADITIONAL", "es@collation=traditional" },
                new string[]{ "hi__DIRECT", "hi__DIRECT", "hi@collation=direct" },
                new string[]{ "ja_JP_TRADITIONAL", "ja_JP_TRADITIONAL", "ja_JP@calendar=japanese" },
                new string[]{ "th_TH_TRADITIONAL", "th_TH_TRADITIONAL", "th_TH@calendar=buddhist" },
                new string[]{ "zh_TW_STROKE", "zh_TW_STROKE", "zh_TW@collation=stroke" },
                new string[]{ "zh__PINYIN", "zh__PINYIN", "zh@collation=pinyin" },
                new string[]{ "zh@collation=pinyin", "zh@collation=pinyin", "zh@collation=pinyin" },
                new string[]{ "zh_CN@collation=pinyin", "zh_CN@collation=pinyin", "zh_CN@collation=pinyin" },
                new string[]{ "zh_CN_CA@collation=pinyin", "zh_CN_CA@collation=pinyin", "zh_CN_CA@collation=pinyin" },
                new string[]{ "en_US_POSIX", "en_US_POSIX", "en_US_POSIX" },
                new string[]{ "hy_AM_REVISED", "hy_AM_REVISED", "hy_AM_REVISED" },
                new string[]{ "no_NO_NY", "no_NO_NY", "no_NO_NY" /* not: "nn_NO" [alan ICU3.0] */ },
                new string[]{ "no@ny", null, "no__NY" /* not: "nn" [alan ICU3.0] */ }, /* POSIX ID */
                new string[]{ "no-no.utf32@B", null, "no_NO_B" /* not: "nb_NO_B" [alan ICU3.0] */ }, /* POSIX ID */
                new string[]{ "qz-qz@Euro", null, "qz_QZ@currency=EUR" }, /* qz-qz uses private use iso codes */
                new string[]{ "en-BOONT", "en__BOONT", "en__BOONT" }, /* registered name */
                new string[]{ "de-1901", "de__1901", "de__1901" }, /* registered name */
                new string[]{ "de-1906", "de__1906", "de__1906" }, /* registered name */
                new string[]{ "sr-SP-Cyrl", "sr_SP_CYRL", "sr_Cyrl_RS" }, /* .NET name */
                new string[]{ "sr-SP-Latn", "sr_SP_LATN", "sr_Latn_RS" }, /* .NET name */
                new string[]{ "sr_YU_CYRILLIC", "sr_YU_CYRILLIC", "sr_Cyrl_RS" }, /* Linux name */
                new string[]{ "uz-UZ-Cyrl", "uz_UZ_CYRL", "uz_Cyrl_UZ" }, /* .NET name */
                new string[]{ "uz-UZ-Latn", "uz_UZ_LATN", "uz_Latn_UZ" }, /* .NET name */
                new string[]{ "zh-CHS", "zh_CHS", "zh_Hans" }, /* .NET name */
                new string[]{ "zh-CHT", "zh_CHT", "zh_Hant" }, /* .NET name This may change back to zh_Hant */

                /* posix behavior that used to be performed by getName */
                new string[]{ "mr.utf8", null, "mr" },
                new string[]{ "de-tv.koi8r", null, "de_TV" },
                new string[]{ "x-piglatin_ML.MBE", null, "x-piglatin_ML" },
                new string[]{ "i-cherokee_US.utf7", null, "i-cherokee_US" },
                new string[]{ "x-filfli_MT_FILFLA.gb-18030", null, "x-filfli_MT_FILFLA" },
                new string[]{ "no-no-ny.utf8@B", null, "no_NO_NY_B" /* not: "nn_NO" [alan ICU3.0] */ }, /* @ ignored unless variant is empty */

                /* fleshing out canonicalization */
                /* sort keywords, ';' is separator so not present at end in canonical form */
                new string[]{ "en_Hant_IL_VALLEY_GIRL@currency=EUR;calendar=Japanese;", "en_Hant_IL_VALLEY_GIRL@calendar=Japanese;currency=EUR", "en_Hant_IL_VALLEY_GIRL@calendar=Japanese;currency=EUR" },
                /* already-canonical ids are not changed */
                new string[]{ "en_Hant_IL_VALLEY_GIRL@calendar=Japanese;currency=EUR", "en_Hant_IL_VALLEY_GIRL@calendar=Japanese;currency=EUR", "en_Hant_IL_VALLEY_GIRL@calendar=Japanese;currency=EUR" },
                /* PRE_EURO and EURO conversions don't affect other keywords */
                /* not in spec
               new string[]{ "es_ES_PREEURO@CALendar=Japanese", "es_ES_PREEURO@calendar=Japanese", "es_ES@calendar=Japanese;currency=ESP" },
               new string[]{ "es_ES_EURO@SHOUT=zipeedeedoodah", "es_ES_EURO@shout=zipeedeedoodah", "es_ES@currency=EUR;shout=zipeedeedoodah" },
                 */
                /* currency keyword overrides PRE_EURO and EURO currency */
                /* not in spec
               new string[]{ "es_ES_PREEURO@currency=EUR", "es_ES_PREEURO@currency=EUR", "es_ES@currency=EUR" },
               new string[]{ "es_ES_EURO@currency=ESP", "es_ES_EURO@currency=ESP", "es_ES@currency=ESP" },
                 */
                /* norwegian is just too weird, if we handle things in their full generality */
                /* this is a negative test to show that we DO NOT handle 'lang=no,var=NY' specially. */
                new string[]{ "no-Hant-GB_NY@currency=$$$", "no_Hant_GB_NY@currency=$$$", "no_Hant_GB_NY@currency=$$$" /* not: "nn_Hant_GB@currency=$$$" [alan ICU3.0] */ },

                /* test cases reflecting internal resource bundle usage */
                /* root is just a language */
                new string[]{ "root@kw=foo", "root@kw=foo", "root@kw=foo" },
                /* level 2 canonicalization should not touch basename when there are keywords and it is null */
                new string[]{ "@calendar=gregorian", "@calendar=gregorian", "@calendar=gregorian" },
            };

            for (int i = 0; i < testCases.Length; i++)
            {
                String[] testCase = testCases[i];
                String source = testCase[0];
                String level1Expected = testCase[1];
                String level2Expected = testCase[2];

                if (level1Expected != null)
                { // null means we have no expectations for how this case is handled
                    String level1 = UCultureInfo.GetFullName(source);
                    if (!level1.Equals(level1Expected))
                    {
                        Errln("UCultureInfo.GetFullName error for: '" + source +
                                "' expected: '" + level1Expected + "' but got: '" + level1 + "'");
                    }
                    else
                    {
                        Logln("UCultureInfo.GetFullName for: '" + source + "' returned: '" + level1 + "'");
                    }
                }
                else
                {
                    Logln("UCultureInfo.GetFullName skipped: '" + source + "'");
                }

                if (level2Expected != null)
                {
                    String level2 = UCultureInfo.Canonicalize(source);
                    if (!level2.Equals(level2Expected))
                    {
                        Errln("UCultureInfo.Canonicalize error for: '" + source +
                                "' expected: '" + level2Expected + "' but got: '" + level2 + "'");
                    }
                    else
                    {
                        Logln("UCultureInfo.Canonicalize for: '" + source + "' returned: '" + level2 + "'");
                    }
                }
                else
                {
                    Logln("UCultureInfo.Canonicalize skipped: '" + source + "'");
                }
            }
        }

        [Test]
        public void TestGetAvailable()
        {
            UCultureInfo[] locales = UCultureInfo.GetCultures(UCultureTypes.AllCultures);
            if (locales.Length < 10)
            {
                Errln("Did not get the correct result from getAvailableLocales");
            }
            if (!locales[locales.Length - 1].FullName.Equals("zu_ZA"))
            {
                Errln("Did not get the expected result");
            }
            foreach (var specificCulture in UCultureInfo.GetCultures(UCultureTypes.SpecificCultures))
            {
                assertFalse($"Expected a specific culture, got '{specificCulture.Name}'", specificCulture.IsNeutralCulture);
            }
            foreach (var neutralCulture in UCultureInfo.GetCultures(UCultureTypes.NeutralCultures))
            {
                assertTrue($"Expected a neutral culture, got '{neutralCulture.Name}'", neutralCulture.IsNeutralCulture);
            }
        }

        internal class DisplayNamesItem
        {
            public string displayLocale;
            public DialectHandling dialectHandling;
            public Capitalization capitalization;
            public DisplayLength nameLength;
            public SubstituteHandling substituteHandling;
            public String localeToBeNamed;
            public String result;
            public DisplayNamesItem(String dLoc, DialectHandling dia, Capitalization cap, DisplayLength nameLen, SubstituteHandling sub, String locToName, String res)
            {
                displayLocale = dLoc;
                dialectHandling = dia;
                capitalization = cap;
                nameLength = nameLen;
                substituteHandling = sub;
                localeToBeNamed = locToName;
                result = res;
            }
        }

        [Test]
        public void TestDisplayNames()
        {
            // consistency check, also check that all data is available
            {
                UCultureInfo[] locales = UCultureInfo.GetCultures(UCultureTypes.AllCultures);
                for (int i = 0; i < locales.Length; ++i)
                {
                    UCultureInfo l = locales[i];
                    String name = l.DisplayName;

                    Logln(l + " --> " + name +
                            ", " + l.GetDisplayName(new UCultureInfo("de")) +
                            ", " + l.GetDisplayName(new UCultureInfo("fr_FR")));

                    String language = l.DisplayLanguage;
                    String script = l.DisplayScriptInContext;
                    String country = l.DisplayCountry;
                    String variant = l.DisplayVariant;

                    checkName(name, language, script, country, variant, UCultureInfo.CurrentCulture);

                    for (int j = 0; j < locales.Length; ++j)
                    {
                        UCultureInfo dl = locales[j];

                        name = l.GetDisplayName(dl);
                        language = l.GetDisplayLanguage(dl);
                        script = l.GetDisplayScriptInContext(dl);
                        country = l.GetDisplayCountry(dl);
                        variant = l.GetDisplayVariant(dl);

                        if (!checkName(name, language, script, country, variant, dl))
                        {
                            break;
                        }
                    }
                }
            }
            // spot check
            {
                UCultureInfo[] locales = {
                    new UCultureInfo("en_US"), new UCultureInfo("de_DE"), new UCultureInfo("fr_FR")
                };
                String[] names = {
                    "Chinese (China)", "Chinesisch (China)", "chinois (Chine)"
                };
                String[] names2 = {
                    "Simplified Chinese (China)", "Chinesisch (vereinfacht) (China)", "chinois simplifi\u00E9 (Chine)"
                };
                UCultureInfo locale = new UCultureInfo("zh_CN");
                UCultureInfo locale2 = new UCultureInfo("zh_Hans_CN");

                for (int i = 0; i < locales.Length; ++i)
                {
                    String name = locale.GetDisplayName(locales[i]);
                    if (!names[i].Equals(name))
                    {
                        Errln("expected '" + names[i] + "' but got '" + name + "'");
                    }
                }
                for (int i = 0; i < locales.Length; ++i)
                {
                    String name = locale2.GetDisplayNameWithDialect(locales[i]);
                    if (!names2[i].Equals(name))
                    {
                        Errln("expected '" + names2[i] + "' but got '" + name + "'");
                    }
                }
            }
            // test use of context
            {
                DialectHandling NM_STD = DialectHandling.StandardNames;
                DialectHandling NM_DIA = DialectHandling.DialectNames;
                Capitalization CAP_BEG = Capitalization.BeginningOfSentence;
                Capitalization CAP_MID = Capitalization.MiddleOfSentence;
                Capitalization CAP_UIL = Capitalization.UIListOrMenu;
                Capitalization CAP_STA = Capitalization.Standalone;
                Capitalization CAP_NON = Capitalization.None;
                DisplayLength LEN_FU = DisplayLength.Full;
                DisplayLength LEN_SH = DisplayLength.Short;
                SubstituteHandling SUB_SU = SubstituteHandling.Substitute;
                SubstituteHandling SUB_NO = SubstituteHandling.NoSubstitute;


                DisplayNamesItem[] items = {
                new DisplayNamesItem("da", NM_STD, CAP_MID, LEN_FU, SUB_SU, "en", "engelsk"),
                new DisplayNamesItem("da", NM_STD, CAP_BEG, LEN_FU, SUB_SU, "en", "Engelsk"),
                new DisplayNamesItem("da", NM_STD, CAP_UIL, LEN_FU, SUB_SU, "en", "Engelsk"),
                new DisplayNamesItem("da", NM_STD, CAP_STA, LEN_FU, SUB_SU, "en", "engelsk"),
                new DisplayNamesItem("da", NM_STD, CAP_MID, LEN_FU, SUB_SU, "en@calendar=buddhist", "engelsk (buddhistisk kalender)"),
                new DisplayNamesItem("da", NM_STD, CAP_BEG, LEN_FU, SUB_SU, "en@calendar=buddhist", "Engelsk (buddhistisk kalender)"),
                new DisplayNamesItem("da", NM_STD, CAP_UIL, LEN_FU, SUB_SU, "en@calendar=buddhist", "Engelsk (buddhistisk kalender)"),
                new DisplayNamesItem("da", NM_STD, CAP_STA, LEN_FU, SUB_SU, "en@calendar=buddhist", "engelsk (buddhistisk kalender)"),
                new DisplayNamesItem("da", NM_STD, CAP_MID, LEN_FU, SUB_SU, "en_GB", "engelsk (Storbritannien)"),
                new DisplayNamesItem("da", NM_STD, CAP_BEG, LEN_FU, SUB_SU, "en_GB", "Engelsk (Storbritannien)"),
                new DisplayNamesItem("da", NM_STD, CAP_UIL, LEN_FU, SUB_SU, "en_GB", "Engelsk (Storbritannien)"),
                new DisplayNamesItem("da", NM_STD, CAP_STA, LEN_FU, SUB_SU, "en_GB", "engelsk (Storbritannien)"),
                new DisplayNamesItem("da", NM_STD, CAP_MID, LEN_SH, SUB_SU, "en_GB", "engelsk (UK)"),
                new DisplayNamesItem("da", NM_STD, CAP_BEG, LEN_SH, SUB_SU, "en_GB", "Engelsk (UK)"),
                new DisplayNamesItem("da", NM_STD, CAP_UIL, LEN_SH, SUB_SU, "en_GB", "Engelsk (UK)"),
                new DisplayNamesItem("da", NM_STD, CAP_STA, LEN_SH, SUB_SU, "en_GB", "engelsk (UK)"),
                new DisplayNamesItem("da", NM_DIA, CAP_MID, LEN_FU, SUB_SU, "en_GB", "britisk engelsk"),
                new DisplayNamesItem("da", NM_DIA, CAP_BEG, LEN_FU, SUB_SU, "en_GB", "Britisk engelsk"),
                new DisplayNamesItem("da", NM_DIA, CAP_UIL, LEN_FU, SUB_SU, "en_GB", "Britisk engelsk"),
                new DisplayNamesItem("da", NM_DIA, CAP_STA, LEN_FU, SUB_SU, "en_GB", "britisk engelsk"),
                new DisplayNamesItem("es", NM_STD, CAP_MID, LEN_FU, SUB_SU, "en", "ingl\u00E9s"),
                new DisplayNamesItem("es", NM_STD, CAP_BEG, LEN_FU, SUB_SU, "en", "Ingl\u00E9s"),
                new DisplayNamesItem("es", NM_STD, CAP_UIL, LEN_FU, SUB_SU, "en", "Ingl\u00E9s"),
                new DisplayNamesItem("es", NM_STD, CAP_STA, LEN_FU, SUB_SU, "en", "Ingl\u00E9s"),
                new DisplayNamesItem("es", NM_STD, CAP_MID, LEN_FU, SUB_SU, "en_GB", "ingl\u00E9s (Reino Unido)"),
                new DisplayNamesItem("es", NM_STD, CAP_BEG, LEN_FU, SUB_SU, "en_GB", "Ingl\u00E9s (Reino Unido)"),
                new DisplayNamesItem("es", NM_STD, CAP_UIL, LEN_FU, SUB_SU, "en_GB", "Ingl\u00E9s (Reino Unido)"),
                new DisplayNamesItem("es", NM_STD, CAP_STA, LEN_FU, SUB_SU, "en_GB", "Ingl\u00E9s (Reino Unido)"),
                new DisplayNamesItem("es", NM_STD, CAP_MID, LEN_SH, SUB_SU, "en_GB", "ingl\u00E9s (RU)"),
                new DisplayNamesItem("es", NM_STD, CAP_BEG, LEN_SH, SUB_SU, "en_GB", "Ingl\u00E9s (RU)"),
                new DisplayNamesItem("es", NM_STD, CAP_UIL, LEN_SH, SUB_SU, "en_GB", "Ingl\u00E9s (RU)"),
                new DisplayNamesItem("es", NM_STD, CAP_STA, LEN_SH, SUB_SU, "en_GB", "Ingl\u00E9s (RU)"),
                new DisplayNamesItem("es", NM_DIA, CAP_MID, LEN_FU, SUB_SU, "en_GB", "ingl\u00E9s brit\u00E1nico"),
                new DisplayNamesItem("es", NM_DIA, CAP_BEG, LEN_FU, SUB_SU, "en_GB", "Ingl\u00E9s brit\u00E1nico"),
                new DisplayNamesItem("es", NM_DIA, CAP_UIL, LEN_FU, SUB_SU, "en_GB", "Ingl\u00E9s brit\u00E1nico"),
                new DisplayNamesItem("es", NM_DIA, CAP_STA, LEN_FU, SUB_SU, "en_GB", "Ingl\u00E9s brit\u00E1nico"),
                new DisplayNamesItem("ru", NM_STD, CAP_MID, LEN_FU, SUB_SU, "uz_Latn", "\u0443\u0437\u0431\u0435\u043A\u0441\u043A\u0438\u0439 (\u043B\u0430\u0442\u0438\u043D\u0438\u0446\u0430)"),
                new DisplayNamesItem("ru", NM_STD, CAP_BEG, LEN_FU, SUB_SU, "uz_Latn", "\u0423\u0437\u0431\u0435\u043A\u0441\u043A\u0438\u0439 (\u043B\u0430\u0442\u0438\u043D\u0438\u0446\u0430)"),
                new DisplayNamesItem("ru", NM_STD, CAP_UIL, LEN_FU, SUB_SU, "uz_Latn", "\u0423\u0437\u0431\u0435\u043A\u0441\u043A\u0438\u0439 (\u043B\u0430\u0442\u0438\u043D\u0438\u0446\u0430)"),
                new DisplayNamesItem("ru", NM_STD, CAP_STA, LEN_FU, SUB_SU, "uz_Latn", "\u0423\u0437\u0431\u0435\u043A\u0441\u043A\u0438\u0439 (\u043B\u0430\u0442\u0438\u043D\u0438\u0446\u0430)"),
                new DisplayNamesItem("en", NM_STD, CAP_MID, LEN_FU, SUB_SU, "ur@numbers=latn", "Urdu (Western Digits)"),
                new DisplayNamesItem("en", NM_STD, CAP_MID, LEN_FU, SUB_SU, "ur@numbers=arabext", "Urdu (Extended Arabic-Indic Digits)"),
                new DisplayNamesItem("en", NM_STD, CAP_MID, LEN_SH, SUB_SU, "ur@numbers=arabext", "Urdu (X Arabic-Indic Digits)"),
                new DisplayNamesItem("af", NM_STD, CAP_NON, LEN_FU, SUB_NO, "aa", "Afar"),
                new DisplayNamesItem("cs", NM_STD, CAP_NON, LEN_FU, SUB_NO, "vai", "vai"),
            };
                foreach (DisplayNamesItem item in items)
                {
                    UCultureInfo locale = new UCultureInfo(item.displayLocale);
                    CultureDisplayNames ldn = CultureDisplayNames.GetInstance(locale, new DisplayContextOptions { DialectHandling = item.dialectHandling, Capitalization = item.capitalization, DisplayLength = item.nameLength, SubstituteHandling = item.substituteHandling });
                    DialectHandling dialectHandling = ldn.DisplayContextOptions.DialectHandling;
                    assertEquals("consistent dialect handling",
                            dialectHandling == DialectHandling.DialectNames,
                            ldn.DisplayContextOptions.DialectHandling == DialectHandling.DialectNames);
                    Capitalization capitalization = ldn.DisplayContextOptions.Capitalization;
                    DisplayLength nameLength = ldn.DisplayContextOptions.DisplayLength;
                    SubstituteHandling substituteHandling = ldn.DisplayContextOptions.SubstituteHandling;
                    if (dialectHandling != item.dialectHandling || capitalization != item.capitalization || nameLength != item.nameLength || substituteHandling != item.substituteHandling)
                    {
                        Errln("FAIL: displayLoc: " + item.displayLocale + ", dialectNam?: " + item.dialectHandling +
                                ", capitalize: " + item.capitalization + ", nameLen: " + item.nameLength + ", substituteHandling: " + item.substituteHandling + ", locToName: " + item.localeToBeNamed +
                                ", => read back dialectNam?: " + dialectHandling + ", capitalize: " + capitalization + ", nameLen: " + nameLength + ", substituteHandling: " + substituteHandling);
                    }
                    else
                    {
                        String result = ldn.GetLocaleDisplayName(item.localeToBeNamed);
                        if (!(item.result == null && result == null) && !(result != null && result.Equals(item.result)))
                        {
                            Errln("FAIL: displayLoc: " + item.displayLocale + ", dialectNam?: " + item.dialectHandling +
                                    ", capitalize: " + item.capitalization + ", nameLen: " + item.nameLength + ", substituteHandling: " + item.substituteHandling + ", locToName: " + item.localeToBeNamed +
                                    ", => expected result: " + item.result + ", got: " + result);
                        }
                    }
                }
            }
        }

        [Test]
        public void TestDisplayLanguageWithDialectCoverage()
        {
            // Coverage test. Implementation is in class CultureDisplayNames.
            assertFalse("en in system default locale: anything but empty",
                    new UCultureInfo("en").GetDisplayLanguageWithDialect() == string.Empty);
            assertEquals("en in de", "Englisch",
                    new UCultureInfo("en").GetDisplayLanguageWithDialect(new UCultureInfo("de")));
            assertEquals("en (string) in de", "Englisch",
                    UCultureInfo.GetDisplayLanguageWithDialect("en", new UCultureInfo("de")));
            assertEquals("en (string) in de (string)", "Englisch",
                    UCultureInfo.GetDisplayLanguageWithDialect("en", "de"));
        }

        [Test]
        public void TestDisplayNameWithDialectCoverage()
        {
            // Coverage test. Implementation is in class CultureDisplayNames.
            assertFalse("en-GB in system default locale: anything but empty",
                    new UCultureInfo("en_GB").DisplayNameWithDialect == string.Empty);
            assertEquals("en-GB in de", "Britisches Englisch",
                    new UCultureInfo("en_GB").GetDisplayNameWithDialect(new UCultureInfo("de")));
            assertEquals("en-GB (string) in de", "Britisches Englisch",
                    UCultureInfo.GetDisplayNameWithDialect("en-GB", new UCultureInfo("de")));
            assertEquals("en-GB (string) in de (string)", "Britisches Englisch",
                    UCultureInfo.GetDisplayNameWithDialect("en-GB", "de"));
        }

        [Test]
        public void TestDisplayScriptCoverage()
        {
            // Coverage test. Implementation is in class CultureDisplayNames.
            assertFalse("zh-Hans in system default locale: anything but empty",
                    new UCultureInfo("zh_Hans").DisplayScript == string.Empty);
            // Stand-alone script name, so not just "Vereinfacht".
            assertEquals("zh-Hans in de", "Vereinfachtes Chinesisch",
                    new UCultureInfo("zh_Hans").GetDisplayScript(new UCultureInfo("de")));
            assertEquals("zh-Hans (string) in de", "Vereinfachtes Chinesisch",
                    UCultureInfo.GetDisplayScript("zh-Hans", new UCultureInfo("de")));
            assertEquals("zh-Hans (string) in de (string)", "Vereinfachtes Chinesisch",
                    UCultureInfo.GetDisplayScript("zh-Hans", "de"));
        }

        private bool checkName(String name, String language, String script, String country, String variant, UCultureInfo dl)
        {
            if (!checkInclusion(dl, name, language, "language"))
            {
                return false;
            }
            if (!checkInclusion(dl, name, script, "script"))
            {
                return false;
            }
            if (!checkInclusion(dl, name, country, "country"))
            {
                return false;
            }
            if (!checkInclusion(dl, name, variant, "variant"))
            {
                return false;
            }
            return true;
        }

        private bool checkInclusion(UCultureInfo dl, String name, String substring, String substringName)
        {
            if (substring.Length > 0 && name.IndexOf(substring, StringComparison.Ordinal) == -1)
            {
                String country2 = substring.Replace('(', '[').Replace(')', ']').Replace('（', '［').Replace('）', '］');
                if (name.IndexOf(country2, StringComparison.Ordinal) == -1)
                {
                    Errln("loc: " + dl + " name '" + name + "' does not contain " +
                            substringName +
                            " '" + substring + "'");
                    return false;
                }
            }
            return true;
        }

        [Test]
        public void TestCoverage()
        {
            {
                //Cover displayXXX
                int i, j;
                String localeID = "zh_CN";
                String name, language, script, country, variant;
                Logln("Covering APIs with signature displayXXX(String, String)");
                for (i = 0; i < LOCALE_SIZE; i++)
                {
                    //localeID String
                    String testLocale = (rawData2[NAME][i]);

                    Logln("Testing " + testLocale + ".....");
                    name = UCultureInfo.GetDisplayName(localeID, testLocale);
                    language = UCultureInfo.GetDisplayLanguage(localeID, testLocale);
                    script = UCultureInfo.GetDisplayScriptInContext(localeID, testLocale);
                    country = UCultureInfo.GetDisplayCountry(localeID, testLocale);
                    variant = UCultureInfo.GetDisplayVariant(localeID, testLocale);
                    if (!checkName(name, language, script, country, variant, new UCultureInfo(testLocale)))
                    {
                        break;
                    }
                }

                Logln("Covering APIs with signature displayXXX(String, UCultureInfo)\n");
                for (j = 0; j < LOCALE_SIZE; j++)
                {
                    String testLocale = (rawData2[NAME][j]);
                    UCultureInfo loc = new UCultureInfo(testLocale);

                    Logln("Testing " + testLocale + ".....");
                    name = UCultureInfo.GetDisplayName(localeID, loc);
                    language = UCultureInfo.GetDisplayLanguage(localeID, loc);
                    script = UCultureInfo.GetDisplayScriptInContext(localeID, loc);
                    country = UCultureInfo.GetDisplayCountry(localeID, loc);
                    variant = UCultureInfo.GetDisplayVariant(localeID, loc);

                    if (!checkName(name, language, script, country, variant, loc))
                    {
                        break;
                    }
                }
            }
            // ICU4N: No need to cover APIs that don't map to .NET
            //UCultureInfo loc1 = new UCultureInfo("en_US_BROOKLYN");
            //UCultureInfo loc2 = new UCultureInfo("en", "US", "BROOKLYN");
            //if (!loc2.Equals(loc1))
            //{
            //    Errln("ULocale.ULocale(String a, String b, String c)");
            //}

            UCultureInfo loc3 = new UCultureInfo("en_US");
            //UCultureInfo loc4 = new UCultureInfo("en", "US");
            //if (!loc4.Equals(loc3))
            //{
            //    Errln("ULocale.ULocale(String a, String b)");
            //}

            UCultureInfo loc5 = (UCultureInfo)loc3.Clone();
            if (!loc5.Equals(loc3))
            {
                Errln("ULocale.clone should get the same UCultureInfo");
            }
            UCultureInfo.GetISOCountries(); // To check the result ?!
        }

        [Test]
        public void TestBamBm()
        {
            // "bam" shouldn't be there since the official code is 'bm'
            String[] isoLanguages = UCultureInfo.GetISOLanguages();
            for (int i = 0; i < isoLanguages.Length; ++i)
            {
                if ("bam".Equals(isoLanguages[i]))
                {
                    Errln("found bam");
                }
                if (i > 0 && isoLanguages[i].CompareToOrdinal(isoLanguages[i - 1]) <= 0)
                {
                    Errln("language list out of order: '" + isoLanguages[i] + " <= " + isoLanguages[i - 1]);
                }
            }
        }

        [Test]
        public void TestDisplayKeyword()
        {
            //prepare testing data
            initHashtable();
            string[] data = {
                "en_US@collation=phonebook;calendar=islamic-civil",
                "zh_Hans@collation=pinyin;calendar=chinese",
                "foo_Bar_BAZ@collation=traditional;calendar=buddhist"
            };

            for (int i = 0; i < data.Length; i++)
            {
                string localeID = data[i];
                Logln("");
                Logln("Testing locale " + localeID + " ...");
                UCultureInfo loc = new UCultureInfo(localeID);

                var it = loc.Keywords.GetEnumerator();
                var it2 = UCultureInfo.GetKeywords(localeID).GetEnumerator();
                //it and it2 are not equal here. No way to verify their equivalence yet.
                while (it.MoveNext())
                {
                    string key = (string)it.Current.Key;
                    it2.MoveNext();
                    string key2 = (string)it2.Current.Key;
                    if (!key.Equals(key2))
                    {
                        Errln("FAIL: static and non-static getKeywords returned different results.");
                    }

                    //To verify display of Keyword
                    // display the above key in English
                    string s0 = UCultureInfo.GetDisplayKeyword(key); //display in default locale
                    string s1 = UCultureInfo.GetDisplayKeyword(key, new UCultureInfo("en_US"));
                    string s2 = UCultureInfo.GetDisplayKeyword(key, "en_US");
                    if (!s1.Equals(s2))
                    {
                        Errln("FAIL: one of the getDisplayKeyword methods failed.");
                    }
                    // ICU4N: This was ULocale.getDefault() in Java, but the production code is using display locale.
                    // This seems like a bug with the original implementation, as that test is technically invalid.
                    if (UCultureInfo.CurrentUICulture.Equals(new UCultureInfo("en-US")) && !s1.Equals(s0))
                    {
                        Errln("FAIL: getDisplayKeyword methods failed for the default locale.");
                    }
                    if (!s1.Equals(h[0].Get(key)))
                    {
                        Errln("Locale " + localeID + " getDisplayKeyword for key: " + key +
                                " in English expected \"" + h[0].Get(key) + "\" saw \"" + s1 + "\" instead");
                    }
                    else
                    {
                        Logln("OK: getDisplayKeyword for key: " + key + " in English got " + s1);
                    }

                    // display the key in S-Chinese
                    s1 = UCultureInfo.GetDisplayKeyword(key, new UCultureInfo("zh_Hans_CN"));
                    s2 = UCultureInfo.GetDisplayKeyword(key, "zh_Hans");
                    if (!s1.Equals(s2))
                    {
                        Errln("one of the getDisplayKeyword methods failed.");
                    }
                    if (!s1.Equals(h[1].Get(key)))
                    {
                        Errln("Locale " + localeID + " getDisplayKeyword for key: " + key +
                                " in Chinese expected \"" + h[1].Get(key) + "\" saw \"" + s1 + "\" instead");
                    }
                    else
                    {
                        Logln("OK: getDisplayKeyword for key: " + key + " in Chinese got " + s1);
                    }

                    //To verify display of Keyword values
                    loc.Keywords.TryGetValue(key, out string type);
                    // display type in English
                    string ss0 = loc.GetDisplayKeywordValue(key);
                    string ss1 = loc.GetDisplayKeywordValue(key, new UCultureInfo("en_US"));
                    string ss2 = UCultureInfo.GetDisplayKeywordValue(localeID, key, "en_US");
                    string ss3 = UCultureInfo.GetDisplayKeywordValue(localeID, key, new UCultureInfo("en_US"));
                    if (!ss1.Equals(ss2) || !ss1.Equals(ss3))
                    {
                        Errln("FAIL: one of the getDisplayKeywordValue methods failed.");
                    }
                    // ICU4N: This was ULocale.getDefault() in Java, but the production code is using display locale.
                    // This seems like a bug with the original implementation, as that test is technically invalid.
                    if (UCultureInfo.CurrentUICulture.Equals(new UCultureInfo("en_US")) && !ss1.Equals(ss0))
                    {
                        Errln("FAIL: getDisplayKeyword methods failed for the default locale.");
                    }
                    if (!ss1.Equals(h[0].Get(type)))
                    {
                        Errln(" Locale " + localeID + " getDisplayKeywordValue for key: " + key +
                                " in English expected \"" + h[0].Get(type) + "\" saw \"" + ss1 + "\" instead");
                    }
                    else
                    {
                        Logln("OK: getDisplayKeywordValue for key: " + key + " in English got " + ss1);
                    }

                    // display type in Chinese
                    ss0 = loc.GetDisplayKeywordValue(key);
                    ss1 = loc.GetDisplayKeywordValue(key, new UCultureInfo("zh_Hans_CN"));
                    ss2 = UCultureInfo.GetDisplayKeywordValue(localeID, key, "zh_Hans");
                    ss3 = UCultureInfo.GetDisplayKeywordValue(localeID, key, new UCultureInfo("zh_Hans_CN"));
                    if (!ss1.Equals(ss2) || !ss1.Equals(ss3))
                    {
                        Errln("one of the getDisplayKeywordValue methods failed.");
                    }
                    if (!ss1.Equals(h[1].Get(type)))
                    {
                        Errln("Locale " + localeID + " getDisplayKeywordValue for key: " + key +
                                " in Chinese expected \"" + h[1].Get(type) + "\" saw \"" + ss1 + "\" instead");
                    }
                    else
                    {
                        Logln("OK: getDisplayKeywordValue for key: " + key + " in Chinese got " + ss1);
                    }
                }
            }
        }

        [Test]
        public void TestDisplayWithKeyword()
        {
            // Note, this test depends on locale display data for the U.S. and Taiwan.
            // If the data changes (in particular, the keyTypePattern may change for Taiwan),
            // this test will break.
            CultureDisplayNames dn = CultureDisplayNames.GetInstance(new UCultureInfo("en_US"),
                    DialectHandling.DialectNames);
            CultureDisplayNames tdn = CultureDisplayNames.GetInstance(new UCultureInfo("zh_Hant_TW"),
                    DialectHandling.DialectNames);
            String name = dn.GetLocaleDisplayName("de@collation=phonebook");
            String target = "German (Phonebook Sort Order)";
            assertEquals("collation", target, name);

            name = tdn.GetLocaleDisplayName("de@collation=phonebook");
            target = "德文（電話簿排序）"; // \u5FB7\u6587\uFF08\u96FB\u8A71\u7C3F\u6392\u5E8F\uFF09
            assertEquals("collation", target, name);

            name = dn.GetLocaleDisplayName("ja@currency=JPY");
            target = "Japanese (Japanese Yen)";
            assertEquals("currency (JPY)", target, name);

            name = tdn.GetLocaleDisplayName("ja@currency=JPY");
            target = "日文（日圓）"; // \u65E5\u6587\uFF08\u65E5\u5713\uFF09
            assertEquals("currency (JPY)", target, name);

            name = dn.GetLocaleDisplayName("de@currency=XYZ");
            target = "German (Currency: XYZ)";
            assertEquals("currency (XYZ)", target, name);

            name = dn.GetLocaleDisplayName("de@collation=phonebook;currency=XYZ");
            target = "German (Phonebook Sort Order, Currency: XYZ)";
            assertEquals("currency", target, name);

            name = dn.GetLocaleDisplayName("de_Latn_DE");
            target = "German (Latin, Germany)";
            assertEquals("currency", target, name);

            name = tdn.GetLocaleDisplayName("de@currency=XYZ");
            target = "德文（貨幣：XYZ）";  // \u5FB7\u6587\uFF08\u8CA8\u5E63: XYZ\uFF09
            assertEquals("currency", target, name);

            name = tdn.GetLocaleDisplayName("de@collation=phonebook;currency=XYZ");
            target = "德文（電話簿排序，貨幣：XYZ）"; // \u5FB7\u6587\uFF08\u96FB\u8A71\u7C3F\u6392\u5E8F\uFF09，\u5FB7\u6587\uFF08\u8CA8\u5E63: XYZ\uFF09
            assertEquals("collation", target, name);

            name = dn.GetLocaleDisplayName("de@foo=bar");
            target = "German (foo=bar)";
            assertEquals("foo", target, name);

            name = tdn.GetLocaleDisplayName("de@foo=bar");
            target = "德文（foo=bar）"; // \u5FB7\u6587\uFF08foo=bar\uFF09
            assertEquals("foo", target, name);

            UCultureInfo locale = UCultureInfo.GetCultureInfoByIetfLanguageTag("de-x-foobar");
            name = dn.GetLocaleDisplayName(locale);
            target = "German (Private-Use: foobar)";
            assertEquals("foobar", target, name);

            name = tdn.GetLocaleDisplayName(locale);
            target = "德文（私人使用：foobar）"; // \u5FB7\u6587\uFF08\u79C1\u4EBA\u4F7F\u7528: foobar\uFF09
            assertEquals("foobar", target, name);
        }

        private void initHashtable()
        {
            h[0] = new Dictionary<String, String>();
            h[1] = new Dictionary<String, String>();

            //display in English
            h[0]["collation"] = "Sort Order";
            h[0]["calendar"] = "Calendar";
            h[0]["currency"] = "Currency";
            h[0]["phonebook"] = "Phonebook Sort Order";
            h[0]["pinyin"] = "Pinyin Sort Order";
            h[0]["traditional"] = "Traditional Sort Order";
            h[0]["stroke"] = "Stroke Order";
            h[0]["japanese"] = "Japanese Calendar";
            h[0]["buddhist"] = "Buddhist Calendar";
            h[0]["islamic"] = "Islamic Calendar";
            h[0]["islamic-civil"] = "Islamic Calendar (tabular, civil epoch)";
            h[0]["hebrew"] = "Hebrew Calendar";
            h[0]["chinese"] = "Chinese Calendar";
            h[0]["gregorian"] = "Gregorian Calendar";

            //display in S-Chinese
            h[1]["collation"] = "\u6392\u5E8F";
            h[1]["calendar"] = "\u65E5\u5386";
            h[1]["currency"] = "\u8D27\u5E01";
            h[1]["phonebook"] = "\u7535\u8BDD\u7C3F\u6392\u5E8F";
            h[1]["pinyin"] = "\u62FC\u97F3\u6392\u5E8F";
            h[1]["stroke"] = "\u7B14\u5212\u987A\u5E8F";
            h[1]["traditional"] = "\u4F20\u7EDF\u6392\u5E8F";
            h[1]["japanese"] = "\u65E5\u672C\u65E5\u5386";
            h[1]["buddhist"] = "\u4F5B\u5386";
            h[1]["islamic"] = "\u4F0A\u65AF\u5170\u65E5\u5386";
            h[1]["islamic-civil"] = "\u4F0A\u65AF\u5170\u5E0C\u5409\u6765\u65E5\u5386";
            h[1]["hebrew"] = "\u5E0C\u4F2F\u6765\u65E5\u5386";
            h[1]["chinese"] = "\u519C\u5386";
            h[1]["gregorian"] = "\u516C\u5386";
        }

        //Hashtables for storing expected display of keys/types of locale in English and Chinese
        private static readonly IDictionary<string, string>[] h = new Dictionary<string, string>[2];

        private static readonly string[][] ACCEPT_LANGUAGE_TESTS =  {
            /*#      result  fallback? */
            /*0*/ new string[]{ "mt_MT", "false" },
            /*1*/ new string[]{ "en", "false" },
            /*2*/ new string[]{ "en", "true" }, // fell back from en-zzz to en
            /*3*/ new string[]{ null, "true" },
            /*4*/ new string[]{ "es", "false" },
            /*5*/ new string[]{ "de", "false" },
            /*6*/ new string[]{ "zh_TW", "false" },
            /*7*/ new string[]{ "zh", "true" },
        };

        private static readonly string[] ACCEPT_LANGUAGE_HTTP = {
            /*0*/ "mt-mt, ja;q=0.76, en-us;q=0.95, en;q=0.92, en-gb;q=0.89, fr;q=0.87, iu-ca;q=0.84, iu;q=0.82, ja-jp;q=0.79, mt;q=0.97, de-de;q=0.74, de;q=0.71, es;q=0.68, it-it;q=0.66, it;q=0.63, vi-vn;q=0.61, vi;q=0.58, nl-nl;q=0.55, nl;q=0.53, th-th-traditional;q=.01",
            /*1*/ "ja;q=0.5, en;q=0.8, tlh",
            /*2*/ "en-zzz, de-lx;q=0.8",
            /*3*/ "mga-ie;q=0.9, tlh",
            /*4*/ "xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, "+
                    "xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, "+
                    "xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, "+
                    "xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, "+
                    "xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, "+
                    "xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, "+
                    "xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, "+
                    "xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, "+
                    "xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, "+
                    "xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, xxx-yyy;q=.01, "+
                    "es",
            /*5*/ "de;q=.9, fr;q=.9, xxx-yyy, sr;q=.8",
            /*6*/ "zh-tw",
            /*7*/ "zh-hant-cn",
        };


        [Test]
        public void TestAcceptLanguage()
        {
            for (int i = 0; i < (ACCEPT_LANGUAGE_HTTP.Length); i++)
            {
                bool expectBoolean = bool.Parse(ACCEPT_LANGUAGE_TESTS[i][1]);
                String expectLocale = ACCEPT_LANGUAGE_TESTS[i][0];

                Logln("#" + i + ": expecting: " + expectLocale + " (" + expectBoolean + ")");

                bool r = false;
                UCultureInfo n = UCultureInfo.AcceptLanguage(ACCEPT_LANGUAGE_HTTP[i], out r);
                if ((n == null) && (expectLocale != null))
                {
                    Errln("result was null! line #" + i);
                    continue;
                }
                if (((n == null) && (expectLocale == null)) || (n.ToString().Equals(expectLocale)))
                {
                    Logln(" locale: OK.");
                }
                else
                {
                    Errln("expected " + expectLocale + " but got " + n.ToString());
                }
                if (expectBoolean.Equals(r))
                {
                    Logln(" bool: OK.");
                }
                else
                {
                    Errln("bool: not OK, was " + (r).ToString() + " expected " + expectBoolean.ToString());
                }
            }
        }

        private class ULocaleAcceptLanguageQ : IComparable
        {
            private double q;
            private double serial;
            public ULocaleAcceptLanguageQ(double theq, int theserial)
            {
                q = theq;
                serial = theserial;
            }

            public int CompareTo(Object o)
            {
                ULocaleAcceptLanguageQ other = (ULocaleAcceptLanguageQ)o;
                if (q > other.q)
                { // reverse - to sort in descending order
                    return -1;
                }
                else if (q < other.q)
                {
                    return 1;
                }
                if (serial < other.serial)
                {
                    return -1;
                }
                else if (serial > other.serial)
                {
                    return 1;
                }
                else
                {
                    return 0; // same object
                }
            }
        }

        private UCultureInfo[] StringToULocaleArray(String acceptLanguageList)
        {
            //following code is copied from
            //ULocale.acceptLanguage(String acceptLanguageList, ULocale[] availableLocales, bool[] fallback)


            // 1st: parse out the acceptLanguageList into an array

            SortedDictionary<ULocaleAcceptLanguageQ, UCultureInfo> map = new SortedDictionary<ULocaleAcceptLanguageQ, UCultureInfo>();

            int l = acceptLanguageList.Length;
            int n;
            for (n = 0; n < l; n++)
            {
                int itemEnd = acceptLanguageList.IndexOf(',', n);
                if (itemEnd == -1)
                {
                    itemEnd = l;
                }
                int paramEnd = acceptLanguageList.IndexOf(';', n);
                double q = 1.0;

                if ((paramEnd != -1) && (paramEnd < itemEnd))
                {
                    /* semicolon (;) is closer than end (,) */
                    int t = paramEnd + 1;
                    while (UChar.IsWhiteSpace(acceptLanguageList[t]))
                    {
                        t++;
                    }
                    if (acceptLanguageList[t] == 'q')
                    {
                        t++;
                    }
                    while (UChar.IsWhiteSpace(acceptLanguageList[t]))
                    {
                        t++;
                    }
                    if (acceptLanguageList[t] == '=')
                    {
                        t++;
                    }
                    while (UChar.IsWhiteSpace(acceptLanguageList[t]))
                    {
                        t++;
                    }
                    try
                    {
                        String val = acceptLanguageList.Substring(t, itemEnd - t).Trim(); // ICU4N: Corrected 2nd parameter of Substring
                        q = double.Parse(val, CultureInfo.InvariantCulture);
                    }
                    catch (FormatException nfe)
                    {
                        q = 1.0;
                    }
                }
                else
                {
                    q = 1.0; //default
                    paramEnd = itemEnd;
                }

                String loc = acceptLanguageList.Substring(n, paramEnd - n).Trim(); // ICU4N: Corrected 2nd parameter of Substring
                int serial = map.Count;
                ULocaleAcceptLanguageQ entry = new ULocaleAcceptLanguageQ(q, serial);
                map[entry] = new UCultureInfo(UCultureInfo.Canonicalize(loc)); // sort in reverse order..   1.0, 0.9, 0.8 .. etc
                n = itemEnd; // get next item. (n++ will skip over delimiter)
            }

            // 2. pull out the map
            UCultureInfo[] acceptList = (UCultureInfo[])map.Values.ToArray();
            return acceptList;
        }

        [Test]
        public void TestAcceptLanguage2()
        {
            for (int i = 0; i < (ACCEPT_LANGUAGE_HTTP.Length); i++)
            {
                bool expectBoolean = bool.Parse(ACCEPT_LANGUAGE_TESTS[i][1]);
                String expectLocale = ACCEPT_LANGUAGE_TESTS[i][0];

                Logln("#" + i + ": expecting: " + expectLocale + " (" + expectBoolean + ")");

                bool r = false;
                UCultureInfo n = UCultureInfo.AcceptLanguage(StringToULocaleArray(ACCEPT_LANGUAGE_HTTP[i]), out r);
                if ((n == null) && (expectLocale != null))
                {
                    Errln("result was null! line #" + i);
                    continue;
                }
                if (((n == null) && (expectLocale == null)) || (n.ToString().Equals(expectLocale)))
                {
                    Logln(" locale: OK.");
                }
                else
                {
                    Errln("expected " + expectLocale + " but got " + n.ToString());
                }
                if (expectBoolean.Equals(r))
                {
                    Logln(" bool: OK.");
                }
                else
                {
                    Errln("bool: not OK, was " + (r).ToString() + " expected " + expectBoolean.ToString());
                }
            }
        }

        [Test] // ICU4N specific
        public void TestAcceptLanguageWithNullParameters()
        {
            AssertThrows<ArgumentNullException>(() => UCultureInfo.AcceptLanguage((string)null, out bool _));
            AssertThrows<ArgumentNullException>(() => UCultureInfo.AcceptLanguage((IList<UCultureInfo>)null, out bool _));
            AssertThrows<ArgumentNullException>(() => UCultureInfo.AcceptLanguage((string)null, UCultureInfo.GetCultures(UCultureTypes.AllCultures), out bool _));
            AssertThrows<ArgumentNullException>(() => UCultureInfo.AcceptLanguage("zh-hant-cn", null, out bool _));
            AssertThrows<ArgumentNullException>(() => UCultureInfo.AcceptLanguage((IList<UCultureInfo>)null, UCultureInfo.GetCultures(UCultureTypes.AllCultures), out bool _));
            AssertThrows<ArgumentNullException>(() => UCultureInfo.AcceptLanguage(new UCultureInfo[] { new UCultureInfo("en_US") }, null, out bool _));
        }

        [Test] // ICU4N specific
        public void TestAcceptLanguageWithNullElementsWithinParameters()
        {
            AssertThrows<ArgumentException>(() => UCultureInfo.AcceptLanguage(new UCultureInfo[] { null }, out bool _));
            AssertThrows<ArgumentException>(() => UCultureInfo.AcceptLanguage(new UCultureInfo[] { new UCultureInfo("en_US"), null }, new UCultureInfo[] { new UCultureInfo("fr_FR") }, out bool _));
            AssertThrows<ArgumentException>(() => UCultureInfo.AcceptLanguage(new UCultureInfo[] { new UCultureInfo("en_US") }, new UCultureInfo[] { new UCultureInfo("fr_FR"), null }, out bool _));
        }

        [Test]
        public void TestOrientation()
        {
            {
                string[][] toTest = {
                        new string[]{ "ar", "right-to-left", "top-to-bottom" },
                        new string[]{ "ar_Arab", "right-to-left", "top-to-bottom" },
                        new string[]{ "fa", "right-to-left", "top-to-bottom" },
                        new string[]{ "he", "right-to-left", "top-to-bottom" },
                        new string[]{ "ps", "right-to-left", "top-to-bottom" },
                        new string[]{ "ur", "right-to-left", "top-to-bottom" },
                        new string[]{ "en", "left-to-right", "top-to-bottom" }
                };

                for (int i = 0; i < toTest.Length; ++i)
                {
                    UCultureInfo loc = new UCultureInfo(toTest[i][0]);
                    String co = loc.CharacterOrientation;
                    String lo = loc.LineOrientation;
                    if (!co.Equals(toTest[i][1]))
                    {
                        Errln("Locale \"" + toTest[i][0] + "\" should have \"" + toTest[i][1] + "\" character orientation, but got \'" + co + "\"");
                    }
                    else if (!lo.Equals(toTest[i][2]))
                    {
                        Errln("Locale \"" + toTest[i][0] + "\" should have \"" + toTest[i][2] + "\" line orientation, but got \'" + lo + "\"");
                    }
                }
            }
        }

        [Test]
        public void TestJB3962()
        {
            UCultureInfo loc = new UCultureInfo("de_CH");
            String disp = loc.GetDisplayName(new UCultureInfo("de"));
            if (!disp.Equals("Deutsch (Schweiz)"))
            {
                Errln("Did not get the expected display name for de_CH locale. Got: " + Prettify(disp));
            }
        }

        [Test]
        public void TestMinimize()
        {
            string[][] data = {
                    // source, favorRegion, favorScript
                    new string[]{"zh-Hans-CN", "zh", "zh"},
                    new string[]{"zh-Hant-TW", "zh-TW", "zh-Hant"},
                    new string[]{"zh-Hant-SG", "zh-Hant-SG", "zh-Hant-SG"},
                    new string[]{"zh-Hans-SG", "zh-SG", "zh-SG"},
                    new string[]{"zh-Hant-HK", "zh-HK", "zh-HK"},
                    new string[]{"en_Latn_US", "en", "en"},
                    new string[]{"en_Cyrl-US", "en-Cyrl", "en-Cyrl"},
                    new string[]{"en_Cyrl-RU", "en-Cyrl-RU", "en-Cyrl-RU"},
                    new string[]{"en_Latn-RU", "en-RU", "en-RU"},
                    new string[]{"sr_Cyrl-US", "sr-US", "sr-US"},
                    new string[]{"sr_Cyrl-RU", "sr-Cyrl-RU", "sr-Cyrl-RU"},
                    new string[]{"sr_Latn-RU", "sr-RU", "sr-RU"},
            };
            foreach (string[] test in data)
            {
                UCultureInfo source = new UCultureInfo(test[0]);
                UCultureInfo expectedFavorRegion = new UCultureInfo(test[1]);
                UCultureInfo expectedFavorScript = new UCultureInfo(test[2]);
                assertEquals(string.Format(StringFormatter.CurrentCulture, "favor region:\t{0}", (object)test), expectedFavorRegion,
                        UCultureInfo.MinimizeSubtags(source, UCultureInfo.Minimize.FavorRegion));
                assertEquals(string.Format(StringFormatter.CurrentCulture, "favor script:\t{0}", (object)test), expectedFavorScript,
                        UCultureInfo.MinimizeSubtags(source, UCultureInfo.Minimize.FavorScript));
            }
        }

        [Test]
        public void TestAddLikelySubtags()
        {
            string[][] data = {
                    new string[]{"en", "en_Latn_US"},
                    new string[]{"en_US_BOSTON", "en_Latn_US_BOSTON"},
                    new string[]{"th@calendar=buddhist", "th_Thai_TH@calendar=buddhist"},
                    new string[]{"ar_ZZ", "ar_Arab_EG"},
                    new string[]{"zh", "zh_Hans_CN"},
                    new string[]{"zh_TW", "zh_Hant_TW"},
                    new string[]{"zh_HK", "zh_Hant_HK"},
                    new string[]{"zh_Hant", "zh_Hant_TW"},
                    new string[]{"zh_Zzzz_CN", "zh_Hans_CN"},
                    new string[]{"und_US", "en_Latn_US"},
                    new string[]{"und_HK", "zh_Hant_HK"},
                    /* Not yet implemented
                    new string[]{"art_lojban", "arg_lojban"},
                    new string[]{"zh_cmn_Hans", "zh_cmn_Hans"},
                     */
            };
            for (int i = 0; i < data.Length; i++)
            {
                UCultureInfo org = new UCultureInfo(data[i][0]);
                UCultureInfo res = UCultureInfo.AddLikelySubtags(org);
                if (!res.ToString().Equals(data[i][1]))
                {
                    Errln("Original: " + data[i][0] + " Expected: " + data[i][1] + " - but got " + res.ToString());
                }
            }

            string[][] basic_maximize_data = {
                new string[]{
                    "zu_Zzzz_Zz",
                    "zu_Latn_ZA",
                }, new string[]{
                    "zu_Zz",
                    "zu_Latn_ZA"
                }, new string[]{
                    "en_Zz",
                    "en_Latn_US"
                }, new string[]{
                    "en_Kore",
                    "en_Kore_US"
                }, new string[]{
                    "en_Kore_Zz",
                    "en_Kore_US"
                }, new string[]{
                    "en_Kore_ZA",
                    "en_Kore_ZA"
                }, new string[]{
                    "en_Kore_ZA_POSIX",
                    "en_Kore_ZA_POSIX"
                }, new string[]{
                    "en_Gujr",
                    "en_Gujr_US"
                }, new string[]{
                    "en_ZA",
                    "en_Latn_ZA"
                }, new string[]{
                    "en_Gujr_Zz",
                    "en_Gujr_US"
                }, new string[]{
                    "en_Gujr_ZA",
                    "en_Gujr_ZA"
                }, new string[]{
                    "en_Gujr_ZA_POSIX",
                    "en_Gujr_ZA_POSIX"
                }, new string[]{
                    "en_US_POSIX_1901",
                    "en_Latn_US_POSIX_1901"
                }, new string[]{
                    "en_Latn__POSIX_1901",
                    "en_Latn_US_POSIX_1901"
                }, new string[]{
                    "en__POSIX_1901",
                    "en_Latn_US_POSIX_1901"
                }, new string[]{
                    "de__POSIX_1901",
                    "de_Latn_DE_POSIX_1901"
                }, new string[]{
                    "zzz",
                    ""
                }
        };

            for (int i = 0; i < basic_maximize_data.Length; i++)
            {
                UCultureInfo org = new UCultureInfo(basic_maximize_data[i][0]);
                UCultureInfo res = UCultureInfo.AddLikelySubtags(org);
                String exp = basic_maximize_data[i][1];
                if (exp.Length == 0)
                {
                    if (!org.Equals(res))
                    {
                        Errln("Original: " + basic_maximize_data[i][0] + " expected: " + exp + " - but got " + res.ToString());
                    }
                }
                else if (!res.ToString().Equals(exp))
                {
                    Errln("Original: " + basic_maximize_data[i][0] + " expected: " + exp + " - but got " + res.ToString());
                }
            }

            string[][] basic_minimize_data = {
                new string[]{
                    "en_Latn_US",
                    "en"
                }, new string[]{
                    "en_Latn_US_POSIX_1901",
                    "en__POSIX_1901"
                }, new string[]{
                    "en_Zzzz_US_POSIX_1901",
                    "en__POSIX_1901"
                }, new string[]{
                    "de_Latn_DE_POSIX_1901",
                    "de__POSIX_1901"
                }, new string[]{
                    "und",
                    ""
                }
            };

            for (int i = 0; i < basic_minimize_data.Length; i++)
            {
                UCultureInfo org = new UCultureInfo(basic_minimize_data[i][0]);
                UCultureInfo res = UCultureInfo.MinimizeSubtags(org);
                String exp = basic_minimize_data[i][1];
                if (exp.Length == 0)
                {
                    if (!org.Equals(res))
                    {
                        Errln("Original: " + basic_minimize_data[i][0] + " expected: " + exp + " - but got " + res.ToString());
                    }
                }
                else if (!res.ToString().Equals(exp))
                {
                    Errln("Original: " + basic_minimize_data[i][0] + " expected: " + exp + " - but got " + res.ToString());
                }
            }

            string[][] full_data = {
                new string[]{
                    /*   "FROM", */
                    /*   "ADD-LIKELY", */
                    /*   "REMOVE-LIKELY" */
                    /* }, { */
                    "aa",
                    "aa_Latn_ET",
                    "aa"
                }, new string[]{
                    "af",
                    "af_Latn_ZA",
                    "af"
                }, new string[]{
                    "ak",
                    "ak_Latn_GH",
                    "ak"
                }, new string[]{
                    "am",
                    "am_Ethi_ET",
                    "am"
                }, new string[]{
                    "ar",
                    "ar_Arab_EG",
                    "ar"
                }, new string[]{
                    "as",
                    "as_Beng_IN",
                    "as"
                }, new string[]{
                    "az",
                    "az_Latn_AZ",
                    "az"
                }, new string[]{
                    "be",
                    "be_Cyrl_BY",
                    "be"
                }, new string[]{
                    "bg",
                    "bg_Cyrl_BG",
                    "bg"
                }, new string[]{
                    "bn",
                    "bn_Beng_BD",
                    "bn"
                }, new string[]{
                    "bo",
                    "bo_Tibt_CN",
                    "bo"
                }, new string[]{
                    "bs",
                    "bs_Latn_BA",
                    "bs"
                }, new string[]{
                    "ca",
                    "ca_Latn_ES",
                    "ca"
                }, new string[]{
                    "ch",
                    "ch_Latn_GU",
                    "ch"
                }, new string[]{
                    "chk",
                    "chk_Latn_FM",
                    "chk"
                }, new string[]{
                    "cs",
                    "cs_Latn_CZ",
                    "cs"
                }, new string[]{
                    "cy",
                    "cy_Latn_GB",
                    "cy"
                }, new string[]{
                    "da",
                    "da_Latn_DK",
                    "da"
                }, new string[]{
                    "de",
                    "de_Latn_DE",
                    "de"
                }, new string[]{
                    "dv",
                    "dv_Thaa_MV",
                    "dv"
                }, new string[]{
                    "dz",
                    "dz_Tibt_BT",
                    "dz"
                }, new string[]{
                    "ee",
                    "ee_Latn_GH",
                    "ee"
                }, new string[]{
                    "el",
                    "el_Grek_GR",
                    "el"
                }, new string[]{
                    "en",
                    "en_Latn_US",
                    "en"
                }, new string[]{
                    "es",
                    "es_Latn_ES",
                    "es"
                }, new string[]{
                    "et",
                    "et_Latn_EE",
                    "et"
                }, new string[]{
                    "eu",
                    "eu_Latn_ES",
                    "eu"
                }, new string[]{
                    "fa",
                    "fa_Arab_IR",
                    "fa"
                }, new string[]{
                    "fi",
                    "fi_Latn_FI",
                    "fi"
                }, new string[]{
                    "fil",
                    "fil_Latn_PH",
                    "fil"
                }, new string[]{
                    "fj",
                    "fj_Latn_FJ",
                    "fj"
                }, new string[]{
                    "fo",
                    "fo_Latn_FO",
                    "fo"
                }, new string[]{
                    "fr",
                    "fr_Latn_FR",
                    "fr"
                }, new string[]{
                    "fur",
                    "fur_Latn_IT",
                    "fur"
                }, new string[]{
                    "ga",
                    "ga_Latn_IE",
                    "ga"
                }, new string[]{
                    "gaa",
                    "gaa_Latn_GH",
                    "gaa"
                }, new string[]{
                    "gl",
                    "gl_Latn_ES",
                    "gl"
                }, new string[]{
                    "gn",
                    "gn_Latn_PY",
                    "gn"
                }, new string[]{
                    "gu",
                    "gu_Gujr_IN",
                    "gu"
                }, new string[]{
                    "ha",
                    "ha_Latn_NG",
                    "ha"
                }, new string[]{
                    "haw",
                    "haw_Latn_US",
                    "haw"
                }, new string[]{
                    "he",
                    "he_Hebr_IL",
                    "he"
                }, new string[]{
                    "hi",
                    "hi_Deva_IN",
                    "hi"
                }, new string[]{
                    "hr",
                    "hr_Latn_HR",
                    "hr"
                }, new string[]{
                    "ht",
                    "ht_Latn_HT",
                    "ht"
                }, new string[]{
                    "hu",
                    "hu_Latn_HU",
                    "hu"
                }, new string[]{
                    "hy",
                    "hy_Armn_AM",
                    "hy"
                }, new string[]{
                    "id",
                    "id_Latn_ID",
                    "id"
                }, new string[]{
                    "ig",
                    "ig_Latn_NG",
                    "ig"
                }, new string[]{
                    "ii",
                    "ii_Yiii_CN",
                    "ii"
                }, new string[]{
                    "is",
                    "is_Latn_IS",
                    "is"
                }, new string[]{
                    "it",
                    "it_Latn_IT",
                    "it"
                }, new string[]{
                    "ja",
                    "ja_Jpan_JP",
                    "ja"
                }, new string[]{
                    "ka",
                    "ka_Geor_GE",
                    "ka"
                }, new string[]{
                    "kaj",
                    "kaj_Latn_NG",
                    "kaj"
                }, new string[]{
                    "kam",
                    "kam_Latn_KE",
                    "kam"
                }, new string[]{
                    "kk",
                    "kk_Cyrl_KZ",
                    "kk"
                }, new string[]{
                    "kl",
                    "kl_Latn_GL",
                    "kl"
                }, new string[]{
                    "km",
                    "km_Khmr_KH",
                    "km"
                }, new string[]{
                    "kn",
                    "kn_Knda_IN",
                    "kn"
                }, new string[]{
                    "ko",
                    "ko_Kore_KR",
                    "ko"
                }, new string[]{
                    "kok",
                    "kok_Deva_IN",
                    "kok"
                }, new string[]{
                    "kpe",
                    "kpe_Latn_LR",
                    "kpe"
                }, new string[]{
                    "ku",
                    "ku_Latn_TR",
                    "ku"
                }, new string[]{
                    "ky",
                    "ky_Cyrl_KG",
                    "ky"
                }, new string[]{
                    "la",
                    "la_Latn_VA",
                    "la"
                }, new string[]{
                    "ln",
                    "ln_Latn_CD",
                    "ln"
                }, new string[]{
                    "lo",
                    "lo_Laoo_LA",
                    "lo"
                }, new string[]{
                    "lt",
                    "lt_Latn_LT",
                    "lt"
                }, new string[]{
                    "lv",
                    "lv_Latn_LV",
                    "lv"
                }, new string[]{
                    "mg",
                    "mg_Latn_MG",
                    "mg"
                }, new string[]{
                    "mh",
                    "mh_Latn_MH",
                    "mh"
                }, new string[]{
                    "mk",
                    "mk_Cyrl_MK",
                    "mk"
                }, new string[]{
                    "ml",
                    "ml_Mlym_IN",
                    "ml"
                }, new string[]{
                    "mn",
                    "mn_Cyrl_MN",
                    "mn"
                }, new string[]{
                    "mr",
                    "mr_Deva_IN",
                    "mr"
                }, new string[]{
                    "ms",
                    "ms_Latn_MY",
                    "ms"
                }, new string[]{
                    "mt",
                    "mt_Latn_MT",
                    "mt"
                }, new string[]{
                    "my",
                    "my_Mymr_MM",
                    "my"
                }, new string[]{
                    "na",
                    "na_Latn_NR",
                    "na"
                }, new string[]{
                    "ne",
                    "ne_Deva_NP",
                    "ne"
                }, new string[]{
                    "niu",
                    "niu_Latn_NU",
                    "niu"
                }, new string[]{
                    "nl",
                    "nl_Latn_NL",
                    "nl"
                }, new string[]{
                    "nn",
                    "nn_Latn_NO",
                    "nn"
                }, new string[]{
                    "nr",
                    "nr_Latn_ZA",
                    "nr"
                }, new string[]{
                    "nso",
                    "nso_Latn_ZA",
                    "nso"
                }, new string[]{
                    "om",
                    "om_Latn_ET",
                    "om"
                }, new string[]{
                    "or",
                    "or_Orya_IN",
                    "or"
                }, new string[]{
                    "pa",
                    "pa_Guru_IN",
                    "pa"
                }, new string[]{
                    "pa_Arab",
                    "pa_Arab_PK",
                    "pa_PK"
                }, new string[]{
                    "pa_PK",
                    "pa_Arab_PK",
                    "pa_PK"
                }, new string[]{
                    "pap",
                    "pap_Latn_AW",
                    "pap"
                }, new string[]{
                    "pau",
                    "pau_Latn_PW",
                    "pau"
                }, new string[]{
                    "pl",
                    "pl_Latn_PL",
                    "pl"
                }, new string[]{
                    "ps",
                    "ps_Arab_AF",
                    "ps"
                }, new string[]{
                    "pt",
                    "pt_Latn_BR",
                    "pt"
                }, new string[]{
                    "rn",
                    "rn_Latn_BI",
                    "rn"
                }, new string[]{
                    "ro",
                    "ro_Latn_RO",
                    "ro"
                }, new string[]{
                    "ru",
                    "ru_Cyrl_RU",
                    "ru"
                }, new string[]{
                    "rw",
                    "rw_Latn_RW",
                    "rw"
                }, new string[]{
                    "sa",
                    "sa_Deva_IN",
                    "sa"
                }, new string[]{
                    "se",
                    "se_Latn_NO",
                    "se"
                }, new string[]{
                    "sg",
                    "sg_Latn_CF",
                    "sg"
                }, new string[]{
                    "si",
                    "si_Sinh_LK",
                    "si"
                }, new string[]{
                    "sid",
                    "sid_Latn_ET",
                    "sid"
                }, new string[]{
                    "sk",
                    "sk_Latn_SK",
                    "sk"
                }, new string[]{
                    "sl",
                    "sl_Latn_SI",
                    "sl"
                }, new string[]{
                    "sm",
                    "sm_Latn_WS",
                    "sm"
                }, new string[]{
                    "so",
                    "so_Latn_SO",
                    "so"
                }, new string[]{
                    "sq",
                    "sq_Latn_AL",
                    "sq"
                }, new string[]{
                    "sr",
                    "sr_Cyrl_RS",
                    "sr"
                }, new string[]{
                    "ss",
                    "ss_Latn_ZA",
                    "ss"
                }, new string[]{
                    "st",
                    "st_Latn_ZA",
                    "st"
                }, new string[]{
                    "sv",
                    "sv_Latn_SE",
                    "sv"
                }, new string[]{
                    "sw",
                    "sw_Latn_TZ",
                    "sw"
                }, new string[]{
                    "ta",
                    "ta_Taml_IN",
                    "ta"
                }, new string[]{
                    "te",
                    "te_Telu_IN",
                    "te"
                }, new string[]{
                    "tet",
                    "tet_Latn_TL",
                    "tet"
                }, new string[]{
                    "tg",
                    "tg_Cyrl_TJ",
                    "tg"
                }, new string[]{
                    "th",
                    "th_Thai_TH",
                    "th"
                }, new string[]{
                    "ti",
                    "ti_Ethi_ET",
                    "ti"
                }, new string[]{
                    "tig",
                    "tig_Ethi_ER",
                    "tig"
                }, new string[]{
                    "tk",
                    "tk_Latn_TM",
                    "tk"
                }, new string[]{
                    "tkl",
                    "tkl_Latn_TK",
                    "tkl"
                }, new string[]{
                    "tn",
                    "tn_Latn_ZA",
                    "tn"
                }, new string[]{
                    "to",
                    "to_Latn_TO",
                    "to"
                }, new string[]{
                    "tpi",
                    "tpi_Latn_PG",
                    "tpi"
                }, new string[]{
                    "tr",
                    "tr_Latn_TR",
                    "tr"
                }, new string[]{
                    "ts",
                    "ts_Latn_ZA",
                    "ts"
                }, new string[]{
                    "tt",
                    "tt_Cyrl_RU",
                    "tt"
                }, new string[]{
                    "tvl",
                    "tvl_Latn_TV",
                    "tvl"
                }, new string[]{
                    "ty",
                    "ty_Latn_PF",
                    "ty"
                }, new string[]{
                    "uk",
                    "uk_Cyrl_UA",
                    "uk"
                }, new string[]{
                    "und",
                    "en_Latn_US",
                    "en"
                }, new string[]{
                    "und_AD",
                    "ca_Latn_AD",
                    "ca_AD"
                }, new string[]{
                    "und_AE",
                    "ar_Arab_AE",
                    "ar_AE"
                }, new string[]{
                    "und_AF",
                    "fa_Arab_AF",
                    "fa_AF"
                }, new string[]{
                    "und_AL",
                    "sq_Latn_AL",
                    "sq"
                }, new string[]{
                    "und_AM",
                    "hy_Armn_AM",
                    "hy"
                }, new string[]{
                    "und_AO",
                    "pt_Latn_AO",
                    "pt_AO"
                }, new string[]{
                    "und_AR",
                    "es_Latn_AR",
                    "es_AR"
                }, new string[]{
                    "und_AS",
                    "sm_Latn_AS",
                    "sm_AS"
                }, new string[]{
                    "und_AT",
                    "de_Latn_AT",
                    "de_AT"
                }, new string[]{
                    "und_AW",
                    "nl_Latn_AW",
                    "nl_AW"
                }, new string[]{
                    "und_AX",
                    "sv_Latn_AX",
                    "sv_AX"
                }, new string[]{
                    "und_AZ",
                    "az_Latn_AZ",
                    "az"
                }, new string[]{
                    "und_Arab",
                    "ar_Arab_EG",
                    "ar"
                }, new string[]{
                    "und_Arab_IN",
                    "ur_Arab_IN",
                    "ur_IN"
                }, new string[]{
                    "und_Arab_PK",
                    "ur_Arab_PK",
                    "ur"
                }, new string[]{
                    "und_Arab_SN",
                    "ar_Arab_SN",
                    "ar_SN"
                }, new string[]{
                    "und_Armn",
                    "hy_Armn_AM",
                    "hy"
                }, new string[]{
                    "und_BA",
                    "bs_Latn_BA",
                    "bs"
                }, new string[]{
                    "und_BD",
                    "bn_Beng_BD",
                    "bn"
                }, new string[]{
                    "und_BE",
                    "nl_Latn_BE",
                    "nl_BE"
                }, new string[]{
                    "und_BF",
                    "fr_Latn_BF",
                    "fr_BF"
                }, new string[]{
                    "und_BG",
                    "bg_Cyrl_BG",
                    "bg"
                }, new string[]{
                    "und_BH",
                    "ar_Arab_BH",
                    "ar_BH"
                }, new string[]{
                    "und_BI",
                    "rn_Latn_BI",
                    "rn"
                }, new string[]{
                    "und_BJ",
                    "fr_Latn_BJ",
                    "fr_BJ"
                }, new string[]{
                    "und_BN",
                    "ms_Latn_BN",
                    "ms_BN"
                }, new string[]{
                    "und_BO",
                    "es_Latn_BO",
                    "es_BO"
                }, new string[]{
                    "und_BR",
                    "pt_Latn_BR",
                    "pt"
                }, new string[]{
                    "und_BT",
                    "dz_Tibt_BT",
                    "dz"
                }, new string[]{
                    "und_BY",
                    "be_Cyrl_BY",
                    "be"
                }, new string[]{
                    "und_Beng",
                    "bn_Beng_BD",
                    "bn"
                }, new string[]{
                    "und_Beng_IN",
                    "bn_Beng_IN",
                    "bn_IN"
                }, new string[]{
                    "und_CD",
                    "sw_Latn_CD",
                    "sw_CD"
                }, new string[]{
                    "und_CF",
                    "fr_Latn_CF",
                    "fr_CF"
                }, new string[]{
                    "und_CG",
                    "fr_Latn_CG",
                    "fr_CG"
                }, new string[]{
                    "und_CH",
                    "de_Latn_CH",
                    "de_CH"
                }, new string[]{
                    "und_CI",
                    "fr_Latn_CI",
                    "fr_CI"
                }, new string[]{
                    "und_CL",
                    "es_Latn_CL",
                    "es_CL"
                }, new string[]{
                    "und_CM",
                    "fr_Latn_CM",
                    "fr_CM"
                }, new string[]{
                    "und_CN",
                    "zh_Hans_CN",
                    "zh"
                }, new string[]{
                    "und_CO",
                    "es_Latn_CO",
                    "es_CO"
                }, new string[]{
                    "und_CR",
                    "es_Latn_CR",
                    "es_CR"
                }, new string[]{
                    "und_CU",
                    "es_Latn_CU",
                    "es_CU"
                }, new string[]{
                    "und_CV",
                    "pt_Latn_CV",
                    "pt_CV"
                }, new string[]{
                    "und_CY",
                    "el_Grek_CY",
                    "el_CY"
                }, new string[]{
                    "und_CZ",
                    "cs_Latn_CZ",
                    "cs"
                }, new string[]{
                    "und_Cyrl",
                    "ru_Cyrl_RU",
                    "ru"
                }, new string[]{
                    "und_Cyrl_KZ",
                    "ru_Cyrl_KZ",
                    "ru_KZ"
                }, new string[]{
                    "und_DE",
                    "de_Latn_DE",
                    "de"
                }, new string[]{
                    "und_DJ",
                    "aa_Latn_DJ",
                    "aa_DJ"
                }, new string[]{
                    "und_DK",
                    "da_Latn_DK",
                    "da"
                }, new string[]{
                    "und_DO",
                    "es_Latn_DO",
                    "es_DO"
                }, new string[]{
                    "und_DZ",
                    "ar_Arab_DZ",
                    "ar_DZ"
                }, new string[]{
                    "und_Deva",
                    "hi_Deva_IN",
                    "hi"
                }, new string[]{
                    "und_EC",
                    "es_Latn_EC",
                    "es_EC"
                }, new string[]{
                    "und_EE",
                    "et_Latn_EE",
                    "et"
                }, new string[]{
                    "und_EG",
                    "ar_Arab_EG",
                    "ar"
                }, new string[]{
                    "und_EH",
                    "ar_Arab_EH",
                    "ar_EH"
                }, new string[]{
                    "und_ER",
                    "ti_Ethi_ER",
                    "ti_ER"
                }, new string[]{
                    "und_ES",
                    "es_Latn_ES",
                    "es"
                }, new string[]{
                    "und_ET",
                    "am_Ethi_ET",
                    "am"
                }, new string[]{
                    "und_Ethi",
                    "am_Ethi_ET",
                    "am"
                }, new string[]{
                    "und_Ethi_ER",
                    "am_Ethi_ER",
                    "am_ER"
                }, new string[]{
                    "und_FI",
                    "fi_Latn_FI",
                    "fi"
                }, new string[]{
                    "und_FM",
                    "en_Latn_FM",
                    "en_FM"
                }, new string[]{
                    "und_FO",
                    "fo_Latn_FO",
                    "fo"
                }, new string[]{
                    "und_FR",
                    "fr_Latn_FR",
                    "fr"
                }, new string[]{
                    "und_GA",
                    "fr_Latn_GA",
                    "fr_GA"
                }, new string[]{
                    "und_GE",
                    "ka_Geor_GE",
                    "ka"
                }, new string[]{
                    "und_GF",
                    "fr_Latn_GF",
                    "fr_GF"
                }, new string[]{
                    "und_GL",
                    "kl_Latn_GL",
                    "kl"
                }, new string[]{
                    "und_GN",
                    "fr_Latn_GN",
                    "fr_GN"
                }, new string[]{
                    "und_GP",
                    "fr_Latn_GP",
                    "fr_GP"
                }, new string[]{
                    "und_GQ",
                    "es_Latn_GQ",
                    "es_GQ"
                }, new string[]{
                    "und_GR",
                    "el_Grek_GR",
                    "el"
                }, new string[]{
                    "und_GT",
                    "es_Latn_GT",
                    "es_GT"
                }, new string[]{
                    "und_GU",
                    "en_Latn_GU",
                    "en_GU"
                }, new string[]{
                    "und_GW",
                    "pt_Latn_GW",
                    "pt_GW"
                }, new string[]{
                    "und_Geor",
                    "ka_Geor_GE",
                    "ka"
                }, new string[]{
                    "und_Grek",
                    "el_Grek_GR",
                    "el"
                }, new string[]{
                    "und_Gujr",
                    "gu_Gujr_IN",
                    "gu"
                }, new string[]{
                    "und_Guru",
                    "pa_Guru_IN",
                    "pa"
                }, new string[]{
                    "und_HK",
                    "zh_Hant_HK",
                    "zh_HK"
                }, new string[]{
                    "und_HN",
                    "es_Latn_HN",
                    "es_HN"
                }, new string[]{
                    "und_HR",
                    "hr_Latn_HR",
                    "hr"
                }, new string[]{
                    "und_HT",
                    "ht_Latn_HT",
                    "ht"
                }, new string[]{
                    "und_HU",
                    "hu_Latn_HU",
                    "hu"
                }, new string[]{
                    "und_Hani",
                    "zh_Hani_CN",
                    "zh_Hani"
                }, new string[]{
                    "und_Hans",
                    "zh_Hans_CN",
                    "zh"
                }, new string[]{
                    "und_Hant",
                    "zh_Hant_TW",
                    "zh_TW"
                }, new string[]{
                    "und_Hebr",
                    "he_Hebr_IL",
                    "he"
                }, new string[]{
                    "und_ID",
                    "id_Latn_ID",
                    "id"
                }, new string[]{
                    "und_IL",
                    "he_Hebr_IL",
                    "he"
                }, new string[]{
                    "und_IN",
                    "hi_Deva_IN",
                    "hi"
                }, new string[]{
                    "und_IQ",
                    "ar_Arab_IQ",
                    "ar_IQ"
                }, new string[]{
                    "und_IR",
                    "fa_Arab_IR",
                    "fa"
                }, new string[]{
                    "und_IS",
                    "is_Latn_IS",
                    "is"
                }, new string[]{
                    "und_IT",
                    "it_Latn_IT",
                    "it"
                }, new string[]{
                    "und_JO",
                    "ar_Arab_JO",
                    "ar_JO"
                }, new string[]{
                    "und_JP",
                    "ja_Jpan_JP",
                    "ja"
                }, new string[]{
                    "und_Jpan",
                    "ja_Jpan_JP",
                    "ja"
                }, new string[]{
                    "und_KG",
                    "ky_Cyrl_KG",
                    "ky"
                }, new string[]{
                    "und_KH",
                    "km_Khmr_KH",
                    "km"
                }, new string[]{
                    "und_KM",
                    "ar_Arab_KM",
                    "ar_KM"
                }, new string[]{
                    "und_KP",
                    "ko_Kore_KP",
                    "ko_KP"
                }, new string[]{
                    "und_KR",
                    "ko_Kore_KR",
                    "ko"
                }, new string[]{
                    "und_KW",
                    "ar_Arab_KW",
                    "ar_KW"
                }, new string[]{
                    "und_KZ",
                    "ru_Cyrl_KZ",
                    "ru_KZ"
                }, new string[]{
                    "und_Khmr",
                    "km_Khmr_KH",
                    "km"
                }, new string[]{
                    "und_Knda",
                    "kn_Knda_IN",
                    "kn"
                }, new string[]{
                    "und_Kore",
                    "ko_Kore_KR",
                    "ko"
                }, new string[]{
                    "und_LA",
                    "lo_Laoo_LA",
                    "lo"
                }, new string[]{
                    "und_LB",
                    "ar_Arab_LB",
                    "ar_LB"
                }, new string[]{
                    "und_LI",
                    "de_Latn_LI",
                    "de_LI"
                }, new string[]{
                    "und_LK",
                    "si_Sinh_LK",
                    "si"
                }, new string[]{
                    "und_LS",
                    "st_Latn_LS",
                    "st_LS"
                }, new string[]{
                    "und_LT",
                    "lt_Latn_LT",
                    "lt"
                }, new string[]{
                    "und_LU",
                    "fr_Latn_LU",
                    "fr_LU"
                }, new string[]{
                    "und_LV",
                    "lv_Latn_LV",
                    "lv"
                }, new string[]{
                    "und_LY",
                    "ar_Arab_LY",
                    "ar_LY"
                }, new string[]{
                    "und_Laoo",
                    "lo_Laoo_LA",
                    "lo"
                }, new string[]{
                    "und_Latn_ES",
                    "es_Latn_ES",
                    "es"
                }, new string[]{
                    "und_Latn_ET",
                    "en_Latn_ET",
                    "en_ET"
                }, new string[]{
                    "und_Latn_GB",
                    "en_Latn_GB",
                    "en_GB"
                }, new string[]{
                    "und_Latn_GH",
                    "ak_Latn_GH",
                    "ak"
                }, new string[]{
                    "und_Latn_ID",
                    "id_Latn_ID",
                    "id"
                }, new string[]{
                    "und_Latn_IT",
                    "it_Latn_IT",
                    "it"
                }, new string[]{
                    "und_Latn_NG",
                    "en_Latn_NG",
                    "en_NG"
                }, new string[]{
                    "und_Latn_TR",
                    "tr_Latn_TR",
                    "tr"
                }, new string[]{
                    "und_Latn_ZA",
                    "en_Latn_ZA",
                    "en_ZA"
                }, new string[]{
                    "und_MA",
                    "ar_Arab_MA",
                    "ar_MA"
                }, new string[]{
                    "und_MC",
                    "fr_Latn_MC",
                    "fr_MC"
                }, new string[]{
                    "und_MD",
                    "ro_Latn_MD",
                    "ro_MD"
                }, new string[]{
                    "und_ME",
                    "sr_Latn_ME",
                    "sr_ME"
                }, new string[]{
                    "und_MG",
                    "mg_Latn_MG",
                    "mg"
                }, new string[]{
                    "und_MK",
                    "mk_Cyrl_MK",
                    "mk"
                }, new string[]{
                    "und_ML",
                    "bm_Latn_ML",
                    "bm"
                }, new string[]{
                    "und_MM",
                    "my_Mymr_MM",
                    "my"
                }, new string[]{
                    "und_MN",
                    "mn_Cyrl_MN",
                    "mn"
                }, new string[]{
                    "und_MO",
                    "zh_Hant_MO",
                    "zh_MO"
                }, new string[]{
                    "und_MQ",
                    "fr_Latn_MQ",
                    "fr_MQ"
                }, new string[]{
                    "und_MR",
                    "ar_Arab_MR",
                    "ar_MR"
                }, new string[]{
                    "und_MT",
                    "mt_Latn_MT",
                    "mt"
                }, new string[]{
                    "und_MV",
                    "dv_Thaa_MV",
                    "dv"
                }, new string[]{
                    "und_MX",
                    "es_Latn_MX",
                    "es_MX"
                }, new string[]{
                    "und_MY",
                    "ms_Latn_MY",
                    "ms"
                }, new string[]{
                    "und_MZ",
                    "pt_Latn_MZ",
                    "pt_MZ"
                }, new string[]{
                    "und_Mlym",
                    "ml_Mlym_IN",
                    "ml"
                }, new string[]{
                    "und_Mymr",
                    "my_Mymr_MM",
                    "my"
                }, new string[]{
                    "und_NC",
                    "fr_Latn_NC",
                    "fr_NC"
                }, new string[]{
                    "und_NE",
                    "ha_Latn_NE",
                    "ha_NE"
                }, new string[]{
                    "und_NG",
                    "en_Latn_NG",
                    "en_NG"
                }, new string[]{
                    "und_NI",
                    "es_Latn_NI",
                    "es_NI"
                }, new string[]{
                    "und_NL",
                    "nl_Latn_NL",
                    "nl"
                }, new string[]{
                    "und_NO",
                    "nb_Latn_NO",
                    "nb"
                }, new string[]{
                    "und_NP",
                    "ne_Deva_NP",
                    "ne"
                }, new string[]{
                    "und_NR",
                    "en_Latn_NR",
                    "en_NR"
                }, new string[]{
                    "und_OM",
                    "ar_Arab_OM",
                    "ar_OM"
                }, new string[]{
                    "und_Orya",
                    "or_Orya_IN",
                    "or"
                }, new string[]{
                    "und_PA",
                    "es_Latn_PA",
                    "es_PA"
                }, new string[]{
                    "und_PE",
                    "es_Latn_PE",
                    "es_PE"
                }, new string[]{
                    "und_PF",
                    "fr_Latn_PF",
                    "fr_PF"
                }, new string[]{
                    "und_PG",
                    "tpi_Latn_PG",
                    "tpi"
                }, new string[]{
                    "und_PH",
                    "fil_Latn_PH",
                    "fil"
                }, new string[]{
                    "und_PL",
                    "pl_Latn_PL",
                    "pl"
                }, new string[]{
                    "und_PM",
                    "fr_Latn_PM",
                    "fr_PM"
                }, new string[]{
                    "und_PR",
                    "es_Latn_PR",
                    "es_PR"
                }, new string[]{
                    "und_PS",
                    "ar_Arab_PS",
                    "ar_PS"
                }, new string[]{
                    "und_PT",
                    "pt_Latn_PT",
                    "pt_PT"
                }, new string[]{
                    "und_PW",
                    "pau_Latn_PW",
                    "pau"
                }, new string[]{
                    "und_PY",
                    "gn_Latn_PY",
                    "gn"
                }, new string[]{
                    "und_QA",
                    "ar_Arab_QA",
                    "ar_QA"
                }, new string[]{
                    "und_RE",
                    "fr_Latn_RE",
                    "fr_RE"
                }, new string[]{
                    "und_RO",
                    "ro_Latn_RO",
                    "ro"
                }, new string[]{
                    "und_RS",
                    "sr_Cyrl_RS",
                    "sr"
                }, new string[]{
                    "und_RU",
                    "ru_Cyrl_RU",
                    "ru"
                }, new string[]{
                    "und_RW",
                    "rw_Latn_RW",
                    "rw"
                }, new string[]{
                    "und_SA",
                    "ar_Arab_SA",
                    "ar_SA"
                }, new string[]{
                    "und_SD",
                    "ar_Arab_SD",
                    "ar_SD"
                }, new string[]{
                    "und_SE",
                    "sv_Latn_SE",
                    "sv"
                }, new string[]{
                    "und_SG",
                    "en_Latn_SG",
                    "en_SG"
                }, new string[]{
                    "und_SI",
                    "sl_Latn_SI",
                    "sl"
                }, new string[]{
                    "und_SJ",
                    "nb_Latn_SJ",
                    "nb_SJ"
                }, new string[]{
                    "und_SK",
                    "sk_Latn_SK",
                    "sk"
                }, new string[]{
                    "und_SM",
                    "it_Latn_SM",
                    "it_SM"
                }, new string[]{
                    "und_SN",
                    "fr_Latn_SN",
                    "fr_SN"
                }, new string[]{
                    "und_SO",
                    "so_Latn_SO",
                    "so"
                }, new string[]{
                    "und_SR",
                    "nl_Latn_SR",
                    "nl_SR"
                }, new string[]{
                    "und_ST",
                    "pt_Latn_ST",
                    "pt_ST"
                }, new string[]{
                    "und_SV",
                    "es_Latn_SV",
                    "es_SV"
                }, new string[]{
                    "und_SY",
                    "ar_Arab_SY",
                    "ar_SY"
                }, new string[]{
                    "und_Sinh",
                    "si_Sinh_LK",
                    "si"
                }, new string[]{
                    "und_Syrc",
                    "syr_Syrc_IQ",
                    "syr"
                }, new string[]{
                    "und_TD",
                    "fr_Latn_TD",
                    "fr_TD"
                }, new string[]{
                    "und_TG",
                    "fr_Latn_TG",
                    "fr_TG"
                }, new string[]{
                    "und_TH",
                    "th_Thai_TH",
                    "th"
                }, new string[]{
                    "und_TJ",
                    "tg_Cyrl_TJ",
                    "tg"
                }, new string[]{
                    "und_TK",
                    "tkl_Latn_TK",
                    "tkl"
                }, new string[]{
                    "und_TL",
                    "pt_Latn_TL",
                    "pt_TL"
                }, new string[]{
                    "und_TM",
                    "tk_Latn_TM",
                    "tk"
                }, new string[]{
                    "und_TN",
                    "ar_Arab_TN",
                    "ar_TN"
                }, new string[]{
                    "und_TO",
                    "to_Latn_TO",
                    "to"
                }, new string[]{
                    "und_TR",
                    "tr_Latn_TR",
                    "tr"
                }, new string[]{
                    "und_TV",
                    "tvl_Latn_TV",
                    "tvl"
                }, new string[]{
                    "und_TW",
                    "zh_Hant_TW",
                    "zh_TW"
                }, new string[]{
                    "und_Taml",
                    "ta_Taml_IN",
                    "ta"
                }, new string[]{
                    "und_Telu",
                    "te_Telu_IN",
                    "te"
                }, new string[]{
                    "und_Thaa",
                    "dv_Thaa_MV",
                    "dv"
                }, new string[]{
                    "und_Thai",
                    "th_Thai_TH",
                    "th"
                }, new string[]{
                    "und_Tibt",
                    "bo_Tibt_CN",
                    "bo"
                }, new string[]{
                    "und_UA",
                    "uk_Cyrl_UA",
                    "uk"
                }, new string[]{
                    "und_UY",
                    "es_Latn_UY",
                    "es_UY"
                }, new string[]{
                    "und_UZ",
                    "uz_Latn_UZ",
                    "uz"
                }, new string[]{
                    "und_VA",
                    "it_Latn_VA",
                    "it_VA"
                }, new string[]{
                    "und_VE",
                    "es_Latn_VE",
                    "es_VE"
                }, new string[]{
                    "und_VN",
                    "vi_Latn_VN",
                    "vi"
                }, new string[]{
                    "und_VU",
                    "bi_Latn_VU",
                    "bi"
                }, new string[]{
                    "und_WF",
                    "fr_Latn_WF",
                    "fr_WF"
                }, new string[]{
                    "und_WS",
                    "sm_Latn_WS",
                    "sm"
                }, new string[]{
                    "und_YE",
                    "ar_Arab_YE",
                    "ar_YE"
                }, new string[]{
                    "und_YT",
                    "fr_Latn_YT",
                    "fr_YT"
                }, new string[]{
                    "und_Yiii",
                    "ii_Yiii_CN",
                    "ii"
                }, new string[]{
                    "ur",
                    "ur_Arab_PK",
                    "ur"
                }, new string[]{
                    "uz",
                    "uz_Latn_UZ",
                    "uz"
                }, new string[]{
                    "uz_AF",
                    "uz_Arab_AF",
                    "uz_AF"
                }, new string[]{
                    "uz_Arab",
                    "uz_Arab_AF",
                    "uz_AF"
                }, new string[]{
                    "ve",
                    "ve_Latn_ZA",
                    "ve"
                }, new string[]{
                    "vi",
                    "vi_Latn_VN",
                    "vi"
                }, new string[]{
                    "wal",
                    "wal_Ethi_ET",
                    "wal"
                }, new string[]{
                    "wo",
                    "wo_Latn_SN",
                    "wo"
                }, new string[]{
                    "wo_SN",
                    "wo_Latn_SN",
                    "wo"
                }, new string[]{
                    "xh",
                    "xh_Latn_ZA",
                    "xh"
                }, new string[]{
                    "yo",
                    "yo_Latn_NG",
                    "yo"
                }, new string[]{
                    "zh",
                    "zh_Hans_CN",
                    "zh"
                }, new string[]{
                    "zh_HK",
                    "zh_Hant_HK",
                    "zh_HK"
                }, new string[]{
                    "zh_Hani",
                    "zh_Hani_CN",
                    "zh_Hani"
                }, new string[]{
                    "zh_Hant",
                    "zh_Hant_TW",
                    "zh_TW"
                }, new string[]{
                    "zh_MO",
                    "zh_Hant_MO",
                    "zh_MO"
                }, new string[]{
                    "zh_TW",
                    "zh_Hant_TW",
                    "zh_TW"
                }, new string[]{
                    "zu",
                    "zu_Latn_ZA",
                    "zu"
                }, new string[]{
                    "und",
                    "en_Latn_US",
                    "en"
                }, new string[]{
                    "und_ZZ",
                    "en_Latn_US",
                    "en"
                }, new string[]{
                    "und_CN",
                    "zh_Hans_CN",
                    "zh"
                }, new string[]{
                    "und_TW",
                    "zh_Hant_TW",
                    "zh_TW"
                }, new string[]{
                    "und_HK",
                    "zh_Hant_HK",
                    "zh_HK"
                }, new string[]{
                    "und_AQ",
                    "und_Latn_AQ",
                    "und_AQ"
                }, new string[]{
                    "und_Zzzz",
                    "en_Latn_US",
                    "en"
                }, new string[]{
                    "und_Zzzz_ZZ",
                    "en_Latn_US",
                    "en"
                }, new string[]{
                    "und_Zzzz_CN",
                    "zh_Hans_CN",
                    "zh"
                }, new string[]{
                    "und_Zzzz_TW",
                    "zh_Hant_TW",
                    "zh_TW"
                }, new string[]{
                    "und_Zzzz_HK",
                    "zh_Hant_HK",
                    "zh_HK"
                }, new string[]{
                    "und_Zzzz_AQ",
                    "und_Latn_AQ",
                    "und_AQ"
                }, new string[]{
                    "und_Latn",
                    "en_Latn_US",
                    "en"
                }, new string[]{
                    "und_Latn_ZZ",
                    "en_Latn_US",
                    "en"
                }, new string[]{
                    "und_Latn_CN",
                    "za_Latn_CN",
                    "za"
                }, new string[]{
                    "und_Latn_TW",
                    "trv_Latn_TW",
                    "trv"
                }, new string[]{
                    "und_Latn_HK",
                    "zh_Latn_HK",
                    "zh_Latn_HK"
                }, new string[]{
                    "und_Latn_AQ",
                    "und_Latn_AQ",
                    "und_AQ"
                }, new string[]{
                    "und_Hans",
                    "zh_Hans_CN",
                    "zh"
                }, new string[]{
                    "und_Hans_ZZ",
                    "zh_Hans_CN",
                    "zh"
                }, new string[]{
                    "und_Hans_CN",
                    "zh_Hans_CN",
                    "zh"
                }, new string[]{
                    "und_Hans_TW",
                    "zh_Hans_TW",
                    "zh_Hans_TW"
                }, new string[]{
                    "und_Hans_HK",
                    "zh_Hans_HK",
                    "zh_Hans_HK"
                }, new string[]{
                    "und_Hans_AQ",
                    "zh_Hans_AQ",
                    "zh_AQ"
                }, new string[]{
                    "und_Hant",
                    "zh_Hant_TW",
                    "zh_TW"
                }, new string[]{
                    "und_Hant_ZZ",
                    "zh_Hant_TW",
                    "zh_TW"
                }, new string[]{
                    "und_Hant_CN",
                    "zh_Hant_CN",
                    "zh_Hant_CN"
                }, new string[]{
                    "und_Hant_TW",
                    "zh_Hant_TW",
                    "zh_TW"
                }, new string[]{
                    "und_Hant_HK",
                    "zh_Hant_HK",
                    "zh_HK"
                }, new string[]{
                    "und_Hant_AQ",
                    "zh_Hant_AQ",
                    "zh_Hant_AQ"
                }, new string[]{
                    "und_Moon",
                    "en_Moon_US",
                    "en_Moon"
                }, new string[]{
                    "und_Moon_ZZ",
                    "en_Moon_US",
                    "en_Moon"
                }, new string[]{
                    "und_Moon_CN",
                    "zh_Moon_CN",
                    "zh_Moon"
                }, new string[]{
                    "und_Moon_TW",
                    "zh_Moon_TW",
                    "zh_Moon_TW"
                }, new string[]{
                    "und_Moon_HK",
                    "zh_Moon_HK",
                    "zh_Moon_HK"
                }, new string[]{
                    "und_Moon_AQ",
                    "und_Moon_AQ",
                    "und_Moon_AQ"
                }, new string[]{
                    "es",
                    "es_Latn_ES",
                    "es"
                }, new string[]{
                    "es_ZZ",
                    "es_Latn_ES",
                    "es"
                }, new string[]{
                    "es_CN",
                    "es_Latn_CN",
                    "es_CN"
                }, new string[]{
                    "es_TW",
                    "es_Latn_TW",
                    "es_TW"
                }, new string[]{
                    "es_HK",
                    "es_Latn_HK",
                    "es_HK"
                }, new string[]{
                    "es_AQ",
                    "es_Latn_AQ",
                    "es_AQ"
                }, new string[]{
                    "es_Zzzz",
                    "es_Latn_ES",
                    "es"
                }, new string[]{
                    "es_Zzzz_ZZ",
                    "es_Latn_ES",
                    "es"
                }, new string[]{
                    "es_Zzzz_CN",
                    "es_Latn_CN",
                    "es_CN"
                }, new string[]{
                    "es_Zzzz_TW",
                    "es_Latn_TW",
                    "es_TW"
                }, new string[]{
                    "es_Zzzz_HK",
                    "es_Latn_HK",
                    "es_HK"
                }, new string[]{
                    "es_Zzzz_AQ",
                    "es_Latn_AQ",
                    "es_AQ"
                }, new string[]{
                    "es_Latn",
                    "es_Latn_ES",
                    "es"
                }, new string[]{
                    "es_Latn_ZZ",
                    "es_Latn_ES",
                    "es"
                }, new string[]{
                    "es_Latn_CN",
                    "es_Latn_CN",
                    "es_CN"
                }, new string[]{
                    "es_Latn_TW",
                    "es_Latn_TW",
                    "es_TW"
                }, new string[]{
                    "es_Latn_HK",
                    "es_Latn_HK",
                    "es_HK"
                }, new string[]{
                    "es_Latn_AQ",
                    "es_Latn_AQ",
                    "es_AQ"
                }, new string[]{
                    "es_Hans",
                    "es_Hans_ES",
                    "es_Hans"
                }, new string[]{
                    "es_Hans_ZZ",
                    "es_Hans_ES",
                    "es_Hans"
                }, new string[]{
                    "es_Hans_CN",
                    "es_Hans_CN",
                    "es_Hans_CN"
                }, new string[]{
                    "es_Hans_TW",
                    "es_Hans_TW",
                    "es_Hans_TW"
                }, new string[]{
                    "es_Hans_HK",
                    "es_Hans_HK",
                    "es_Hans_HK"
                }, new string[]{
                    "es_Hans_AQ",
                    "es_Hans_AQ",
                    "es_Hans_AQ"
                }, new string[]{
                    "es_Hant",
                    "es_Hant_ES",
                    "es_Hant"
                }, new string[]{
                    "es_Hant_ZZ",
                    "es_Hant_ES",
                    "es_Hant"
                }, new string[]{
                    "es_Hant_CN",
                    "es_Hant_CN",
                    "es_Hant_CN"
                }, new string[]{
                    "es_Hant_TW",
                    "es_Hant_TW",
                    "es_Hant_TW"
                }, new string[]{
                    "es_Hant_HK",
                    "es_Hant_HK",
                    "es_Hant_HK"
                }, new string[]{
                    "es_Hant_AQ",
                    "es_Hant_AQ",
                    "es_Hant_AQ"
                }, new string[]{
                    "es_Moon",
                    "es_Moon_ES",
                    "es_Moon"
                }, new string[]{
                    "es_Moon_ZZ",
                    "es_Moon_ES",
                    "es_Moon"
                }, new string[]{
                    "es_Moon_CN",
                    "es_Moon_CN",
                    "es_Moon_CN"
                }, new string[]{
                    "es_Moon_TW",
                    "es_Moon_TW",
                    "es_Moon_TW"
                }, new string[]{
                    "es_Moon_HK",
                    "es_Moon_HK",
                    "es_Moon_HK"
                }, new string[]{
                    "es_Moon_AQ",
                    "es_Moon_AQ",
                    "es_Moon_AQ"
                }, new string[]{
                    "zh",
                    "zh_Hans_CN",
                    "zh"
                }, new string[]{
                    "zh_ZZ",
                    "zh_Hans_CN",
                    "zh"
                }, new string[]{
                    "zh_CN",
                    "zh_Hans_CN",
                    "zh"
                }, new string[]{
                    "zh_TW",
                    "zh_Hant_TW",
                    "zh_TW"
                }, new string[]{
                    "zh_HK",
                    "zh_Hant_HK",
                    "zh_HK"
                }, new string[]{
                    "zh_AQ",
                    "zh_Hans_AQ",
                    "zh_AQ"
                }, new string[]{
                    "zh_Zzzz",
                    "zh_Hans_CN",
                    "zh"
                }, new string[]{
                    "zh_Zzzz_ZZ",
                    "zh_Hans_CN",
                    "zh"
                }, new string[]{
                    "zh_Zzzz_CN",
                    "zh_Hans_CN",
                    "zh"
                }, new string[]{
                    "zh_Zzzz_TW",
                    "zh_Hant_TW",
                    "zh_TW"
                }, new string[]{
                    "zh_Zzzz_HK",
                    "zh_Hant_HK",
                    "zh_HK"
                }, new string[]{
                    "zh_Zzzz_AQ",
                    "zh_Hans_AQ",
                    "zh_AQ"
                }, new string[]{
                    "zh_Latn",
                    "zh_Latn_CN",
                    "zh_Latn"
                }, new string[]{
                    "zh_Latn_ZZ",
                    "zh_Latn_CN",
                    "zh_Latn"
                }, new string[]{
                    "zh_Latn_CN",
                    "zh_Latn_CN",
                    "zh_Latn"
                }, new string[]{
                    "zh_Latn_TW",
                    "zh_Latn_TW",
                    "zh_Latn_TW"
                }, new string[]{
                    "zh_Latn_HK",
                    "zh_Latn_HK",
                    "zh_Latn_HK"
                }, new string[]{
                    "zh_Latn_AQ",
                    "zh_Latn_AQ",
                    "zh_Latn_AQ"
                }, new string[]{
                    "zh_Hans",
                    "zh_Hans_CN",
                    "zh"
                }, new string[]{
                    "zh_Hans_ZZ",
                    "zh_Hans_CN",
                    "zh"
                }, new string[]{
                    "zh_Hans_TW",
                    "zh_Hans_TW",
                    "zh_Hans_TW"
                }, new string[]{
                    "zh_Hans_HK",
                    "zh_Hans_HK",
                    "zh_Hans_HK"
                }, new string[]{
                    "zh_Hans_AQ",
                    "zh_Hans_AQ",
                    "zh_AQ"
                }, new string[]{
                    "zh_Hant",
                    "zh_Hant_TW",
                    "zh_TW"
                }, new string[]{
                    "zh_Hant_ZZ",
                    "zh_Hant_TW",
                    "zh_TW"
                }, new string[]{
                    "zh_Hant_CN",
                    "zh_Hant_CN",
                    "zh_Hant_CN"
                }, new string[]{
                    "zh_Hant_AQ",
                    "zh_Hant_AQ",
                    "zh_Hant_AQ"
                }, new string[]{
                    "zh_Moon",
                    "zh_Moon_CN",
                    "zh_Moon"
                }, new string[]{
                    "zh_Moon_ZZ",
                    "zh_Moon_CN",
                    "zh_Moon"
                }, new string[]{
                    "zh_Moon_CN",
                    "zh_Moon_CN",
                    "zh_Moon"
                }, new string[]{
                    "zh_Moon_TW",
                    "zh_Moon_TW",
                    "zh_Moon_TW"
                }, new string[]{
                    "zh_Moon_HK",
                    "zh_Moon_HK",
                    "zh_Moon_HK"
                }, new string[]{
                    "zh_Moon_AQ",
                    "zh_Moon_AQ",
                    "zh_Moon_AQ"
                }, new string[]{
                    "art",
                    "",
                    ""
                }, new string[]{
                    "art_ZZ",
                    "",
                    ""
                }, new string[]{
                    "art_CN",
                    "",
                    ""
                }, new string[]{
                    "art_TW",
                    "",
                    ""
                }, new string[]{
                    "art_HK",
                    "",
                    ""
                }, new string[]{
                    "art_AQ",
                    "",
                    ""
                }, new string[]{
                    "art_Zzzz",
                    "",
                    ""
                }, new string[]{
                    "art_Zzzz_ZZ",
                    "",
                    ""
                }, new string[]{
                    "art_Zzzz_CN",
                    "",
                    ""
                }, new string[]{
                    "art_Zzzz_TW",
                    "",
                    ""
                }, new string[]{
                    "art_Zzzz_HK",
                    "",
                    ""
                }, new string[]{
                    "art_Zzzz_AQ",
                    "",
                    ""
                }, new string[]{
                    "art_Latn",
                    "",
                    ""
                }, new string[]{
                    "art_Latn_ZZ",
                    "",
                    ""
                }, new string[]{
                    "art_Latn_CN",
                    "",
                    ""
                }, new string[]{
                    "art_Latn_TW",
                    "",
                    ""
                }, new string[]{
                    "art_Latn_HK",
                    "",
                    ""
                }, new string[]{
                    "art_Latn_AQ",
                    "",
                    ""
                }, new string[]{
                    "art_Hans",
                    "",
                    ""
                }, new string[]{
                    "art_Hans_ZZ",
                    "",
                    ""
                }, new string[]{
                    "art_Hans_CN",
                    "",
                    ""
                }, new string[]{
                    "art_Hans_TW",
                    "",
                    ""
                }, new string[]{
                    "art_Hans_HK",
                    "",
                    ""
                }, new string[]{
                    "art_Hans_AQ",
                    "",
                    ""
                }, new string[]{
                    "art_Hant",
                    "",
                    ""
                }, new string[]{
                    "art_Hant_ZZ",
                    "",
                    ""
                }, new string[]{
                    "art_Hant_CN",
                    "",
                    ""
                }, new string[]{
                    "art_Hant_TW",
                    "",
                    ""
                }, new string[]{
                    "art_Hant_HK",
                    "",
                    ""
                }, new string[]{
                    "art_Hant_AQ",
                    "",
                    ""
                }, new string[]{
                    "art_Moon",
                    "",
                    ""
                }, new string[]{
                    "art_Moon_ZZ",
                    "",
                    ""
                }, new string[]{
                    "art_Moon_CN",
                    "",
                    ""
                }, new string[]{
                    "art_Moon_TW",
                    "",
                    ""
                }, new string[]{
                    "art_Moon_HK",
                    "",
                    ""
                }, new string[]{
                    "art_Moon_AQ",
                    "",
                    ""
                }
        };

            for (int i = 0; i < full_data.Length; i++)
            {
                UCultureInfo org = new UCultureInfo(full_data[i][0]);
                UCultureInfo res = UCultureInfo.AddLikelySubtags(org);
                String exp = full_data[i][1];
                if (exp.Length == 0)
                {
                    if (!org.Equals(res))
                    {
                        Errln("Original: " + full_data[i][0] + " expected: " + exp + " - but got " + res.ToString());
                    }
                }
                else if (!res.ToString().Equals(exp))
                {
                    Errln("Original: " + full_data[i][0] + " expected: " + exp + " - but got " + res.ToString());
                }
            }

            for (int i = 0; i < full_data.Length; i++)
            {
                String maximal = full_data[i][1];

                if (maximal.Length > 0)
                {
                    UCultureInfo org = new UCultureInfo(maximal);
                    UCultureInfo res = UCultureInfo.MinimizeSubtags(org);
                    String exp = full_data[i][2];
                    if (exp.Length == 0)
                    {
                        if (!org.Equals(res))
                        {
                            Errln("Original: " + full_data[i][1] + " expected: " + exp + " - but got " + res.ToString());
                        }
                    }
                    else if (!res.ToString().Equals(exp))
                    {
                        Errln("Original: " + full_data[i][1] + " expected: " + exp + " - but got " + res.ToString());
                    }
                }
            }
        }


        [Test]
        public void TestCLDRVersion()
        {
            //VersionInfo zeroVersion = VersionInfo.getInstance(0, 0, 0, 0);
            VersionInfo testExpect;
            VersionInfo testCurrent;
            VersionInfo cldrVersion;

            cldrVersion = LocaleData.GetCLDRVersion();

            TestFmwk.Logln("uloc_getCLDRVersion() returned: '" + cldrVersion + "'");

            // why isn't this public for tests somewhere?
            Assembly testLoader =
#if FEATURE_TYPEEXTENSIONS_GETTYPEINFO
                typeof(ICUResourceBundleTest).GetTypeInfo().Assembly;
#else
                typeof(ICUResourceBundleTest).Assembly;
#endif
            UResourceBundle bundle = UResourceBundle.GetBundleInstance("Dev/Data/TestData", UCultureInfo.InvariantCulture, testLoader);

            testExpect = VersionInfo.GetInstance(bundle.GetString("ExpectCLDRVersionAtLeast"));
            testCurrent = VersionInfo.GetInstance(bundle.GetString("CurrentCLDRVersion"));


            Logln("(data) ExpectCLDRVersionAtLeast { " + testExpect + "");
            if (cldrVersion.CompareTo(testExpect) < 0)
            {
                Errln("CLDR version is too old, expect at least " + testExpect + ".");
            }

            int r = cldrVersion.CompareTo(testCurrent);
            if (r < 0)
            {
                Logln("CLDR version is behind 'current' (for testdata/root.txt) " + testCurrent + ". Some things may fail.\n");
            }
            else if (r > 0)
            {
                Logln("CLDR version is ahead of 'current' (for testdata/root.txt) " + testCurrent + ". Some things may fail.\n");
            }
            else
            {
                // CLDR version is OK.
            }
        }

        [Test]
        public void TestToLanguageTag()
        {
            string[][] locale_to_langtag = {
                    new string[]{"",            "und"},
                    new string[]{"en",          "en"},
                    new string[]{"en_US",       "en-US"},
                    new string[]{"iw_IL",       "he-IL"},
                    new string[]{"sr_Latn_SR",  "sr-Latn-SR"},
                    new string[]{"en_US_POSIX@ca=japanese", "en-US-u-ca-japanese-va-posix"},
                    new string[]{"en__POSIX",   "en-u-va-posix"},
                    new string[]{"en_US_POSIX_VAR", "en-US-posix-x-lvariant-var"},  // variant POSIX_VAR is processed as regular variant
                    new string[]{"en_US_VAR_POSIX", "en-US-x-lvariant-var-posix"},  // variant VAR_POSIX is processed as regular variant
                    new string[]{"en_US_POSIX@va=posix2",   "en-US-u-va-posix2"},   // if keyword va=xxx already exists, variant POSIX is simply dropped
                    new string[]{"und_555",     "und-555"},
                    new string[]{"123",         "und"},
                    new string[]{"%$#&",        "und"},
                    new string[]{"_Latn",       "und-Latn"},
                    new string[]{"_DE",         "und-DE"},
                    new string[]{"und_FR",      "und-FR"},
                    new string[]{"th_TH_TH",    "th-TH-x-lvariant-th"},
                    new string[]{"bogus",       "bogus"},
                    new string[]{"foooobarrr",  "und"},
                    new string[]{"aa_BB_CYRL",  "aa-BB-x-lvariant-cyrl"},
                    new string[]{"en_US_1234",  "en-US-1234"},
                    new string[]{"en_US_VARIANTA_VARIANTB", "en-US-varianta-variantb"},
                    new string[]{"en_US_VARIANTB_VARIANTA", "en-US-variantb-varianta"},
                    new string[]{"ja__9876_5432",   "ja-9876-5432"},
                    new string[]{"zh_Hant__VAR",    "zh-Hant-x-lvariant-var"},
                    new string[]{"es__BADVARIANT_GOODVAR",  "es"},
                    new string[]{"es__GOODVAR_BAD_BADVARIANT",  "es-goodvar-x-lvariant-bad"},
                    new string[]{"en@calendar=gregorian",   "en-u-ca-gregory"},
                    new string[]{"de@collation=phonebook;calendar=gregorian",   "de-u-ca-gregory-co-phonebk"},
                    new string[]{"th@numbers=thai;z=extz;x=priv-use;a=exta",   "th-a-exta-u-nu-thai-z-extz-x-priv-use"},
                    new string[]{"en@timezone=America/New_York;calendar=japanese",    "en-u-ca-japanese-tz-usnyc"},
                    new string[]{"en@timezone=US/Eastern",    "en-u-tz-usnyc"},
                    new string[]{"en@x=x-y-z;a=a-b-c",  "en-x-x-y-z"},
                    new string[]{"it@collation=badcollationtype;colStrength=identical;cu=usd-eur", "it-u-cu-usd-eur-ks-identic"},
                    new string[]{"en_US_POSIX", "en-US-u-va-posix"},
                    new string[]{"en_US_POSIX@calendar=japanese;currency=EUR","en-US-u-ca-japanese-cu-eur-va-posix"},
                    new string[]{"@x=elmer",    "x-elmer"},
                    new string[]{"_US@x=elmer", "und-US-x-elmer"},
                    /* #12671 */
                    new string[]{"en@a=bar;attribute=baz",  "en-a-bar-u-baz"},
                    new string[]{"en@a=bar;attribute=baz;x=u-foo",  "en-a-bar-u-baz-x-u-foo"},
                    new string[]{"en@attribute=baz",    "en-u-baz"},
                    new string[]{"en@attribute=baz;calendar=islamic-civil", "en-u-baz-ca-islamic-civil"},
                    new string[]{"en@a=bar;calendar=islamic-civil;x=u-foo", "en-a-bar-u-ca-islamic-civil-x-u-foo"},
                    new string[]{"en@a=bar;attribute=baz;calendar=islamic-civil;x=u-foo",   "en-a-bar-u-baz-ca-islamic-civil-x-u-foo"},
            };

            for (int i = 0; i < locale_to_langtag.Length; i++)
            {
                try
                {
                    UCultureInfo loc = new UCultureInfo(locale_to_langtag[i][0]);
                    String langtag = loc.ToIetfLanguageTag();
                    if (!langtag.Equals(locale_to_langtag[i][1]))
                    {
                        Errln("FAIL: ToIetfLanguageTag returned language tag [" + langtag + "] for locale ["
                                + loc + "] - expected: [" + locale_to_langtag[i][1] + "]");
                    }
                }
                catch (Exception e)
                {

                }
            }
        }

        [Test]
        public void TestForLanguageTag()
        {
            int? NOERROR = new int?(-1);

            object[][] langtag_to_locale = {
                    new object[]{"en",                  "en",                   NOERROR},
                    new object[]{"en-us",               "en_US",                NOERROR},
                    new object[]{"und-us",              "_US",                  NOERROR},
                    new object[]{"und-latn",            "_Latn",                NOERROR},
                    new object[]{"en-us-posix",         "en_US_POSIX",          NOERROR},
                    new object[]{"de-de_euro",          "de",                   new int?(3)},
                    new object[]{"kok-in",              "kok_IN",               NOERROR},
                    new object[]{"123",                 "",                     new int?(0)},
                    new object[]{"en_us",               "",                     new int?(0)},
                    new object[]{"en-latn-x",           "en_Latn",              new int?(8)},
                    new object[]{"art-lojban",          "jbo",                  NOERROR},
                    new object[]{"zh-hakka",            "hak",                  NOERROR},
                    new object[]{"zh-cmn-CH",           "cmn_CH",               NOERROR},
                    new object[]{"xxx-yy",              "xxx_YY",               NOERROR},
                    new object[]{"fr-234",              "fr_234",               NOERROR},
                    new object[]{"i-default",           "en@x=i-default",       NOERROR},
                    new object[]{"i-test",              "",                     new int?(0)},
                    new object[]{"ja-jp-jp",            "ja_JP",                new int?(6)},
                    new object[]{"bogus",               "bogus",                NOERROR},
                    new object[]{"boguslang",           "",                     new int?(0)},
                    new object[]{"EN-lATN-us",          "en_Latn_US",           NOERROR},
                    new object[]{"und-variant-1234",    "__VARIANT_1234",       NOERROR},
                    new object[]{"und-varzero-var1-vartwo", "__VARZERO",        new int?(12)},
                    new object[]{"en-u-ca-gregory",     "en@calendar=gregorian",    NOERROR},
                    new object[]{"en-U-cu-USD",         "en@currency=usd",      NOERROR},
                    new object[]{"en-us-u-va-posix",    "en_US_POSIX",          NOERROR},
                    new object[]{"en-us-u-ca-gregory-va-posix", "en_US_POSIX@calendar=gregorian",   NOERROR},
                    new object[]{"en-us-posix-u-va-posix",  "en_US_POSIX@va=posix", NOERROR},
                    new object[]{"en-us-u-va-posix2",   "en_US@va=posix2",      NOERROR},
                    new object[]{"en-us-vari1-u-va-posix",   "en_US_VARI1@va=posix",  NOERROR},
                    new object[]{"ar-x-1-2-3",          "ar@x=1-2-3",           NOERROR},
                    new object[]{"fr-u-nu-latn-cu-eur", "fr@currency=eur;numbers=latn", NOERROR},
                    new object[]{"de-k-kext-u-co-phonebk-nu-latn",  "de@collation=phonebook;k=kext;numbers=latn",   NOERROR},
                    new object[]{"ja-u-cu-jpy-ca-jp",   "ja@calendar=yes;currency=jpy;jp=yes",  NOERROR},
                    new object[]{"en-us-u-tz-usnyc",    "en_US@timezone=America/New_York",      NOERROR},
                    new object[]{"und-a-abc-def",       "@a=abc-def",           NOERROR},
                    new object[]{"zh-u-ca-chinese-x-u-ca-chinese",  "zh@calendar=chinese;x=u-ca-chinese",   NOERROR},
                    new object[]{"fr--FR",              "fr",                   new int?(3)},
                    new object[]{"fr-",                 "fr",                   new int?(3)},
                    new object[]{"x-elmer",             "@x=elmer",             NOERROR},
                    new object[]{"en-US-u-attr1-attr2-ca-gregory", "en_US@attribute=attr1-attr2;calendar=gregorian",    NOERROR},
                    new object[]{"sr-u-kn",             "sr@colnumeric=yes",    NOERROR},
                    new object[]{"de-u-kn-co-phonebk",  "de@collation=phonebook;colnumeric=yes",    NOERROR},
                    new object[]{"en-u-attr2-attr1-kn-kb",  "en@attribute=attr1-attr2;colbackwards=yes;colnumeric=yes", NOERROR},
                    new object[]{"ja-u-ijkl-efgh-abcd-ca-japanese-xx-yyy-zzz-kn",   "ja@attribute=abcd-efgh-ijkl;calendar=japanese;colnumeric=yes;xx=yyy-zzz",  NOERROR},
                    new object[]{"de-u-xc-xphonebk-co-phonebk-ca-buddhist-mo-very-lo-extensi-xd-that-de-should-vc-probably-xz-killthebuffer",
                        "de@calendar=buddhist;collation=phonebook;de=should;lo=extensi;mo=very;vc=probably;xc=xphonebk;xd=that;xz=yes", new int?(92)},
                    /* #12761 */
                    new object[]{"en-a-bar-u-baz",      "en@a=bar;attribute=baz",   NOERROR},
                    new object[]{"en-a-bar-u-baz-x-u-foo",  "en@a=bar;attribute=baz;x=u-foo",   NOERROR},
                    new object[]{"en-u-baz",            "en@attribute=baz",     NOERROR},
                    new object[]{"en-u-baz-ca-islamic-civil",   "en@attribute=baz;calendar=islamic-civil",  NOERROR},
                    new object[]{"en-a-bar-u-ca-islamic-civil-x-u-foo", "en@a=bar;calendar=islamic-civil;x=u-foo",  NOERROR},
                    new object[]{"en-a-bar-u-baz-ca-islamic-civil-x-u-foo", "en@a=bar;attribute=baz;calendar=islamic-civil;x=u-foo",    NOERROR},

            };

            for (int i = 0; i < langtag_to_locale.Length; i++)
            {
                String tag = (String)langtag_to_locale[i][0];
                UCultureInfo expected = new UCultureInfo((String)langtag_to_locale[i][1]);
                UCultureInfo loc = UCultureInfo.GetCultureInfoByIetfLanguageTag(tag);

                if (!loc.Equals(expected))
                {
                    Errln("FAIL: forLanguageTag returned locale [" + loc + "] for language tag [" + tag
                            + "] - expected: [" + expected + "]");
                }
            }

            // Use locale builder to check errors
            for (int i = 0; i < langtag_to_locale.Length; i++)
            {
                String tag = (String)langtag_to_locale[i][0];
                UCultureInfo expected = new UCultureInfo((String)langtag_to_locale[i][1]);
                int errorIdx = ((int?)langtag_to_locale[i][2]).Value;

                try
                {
                    UCultureInfoBuilder bld = new UCultureInfoBuilder();
                    bld.SetLanguageTag(tag);
                    UCultureInfo loc = bld.Build();

                    if (!loc.Equals(expected))
                    {
                        Errln("FAIL: forLanguageTag returned locale [" + loc + "] for language tag [" + tag
                                + "] - expected: [" + expected + "]");
                    }
                    if (errorIdx != NOERROR.Value)
                    {
                        Errln("FAIL: UCultureInfoBuilder.setLanguageTag should throw an exception for input tag [" + tag + "]");
                    }
                }
                catch (IllformedLocaleException ifle)
                {
                    if (ifle.ErrorIndex != errorIdx)
                    {
                        Errln("FAIL: UCultureInfoBuilder.setLanguageTag returned error index " + ifle.ErrorIndex
                                + " for input language tag [" + tag + "] expected: " + errorIdx);
                    }
                }
            }
        }

        /*
         * Test that if you use any locale without keyword that you will get a NULL
         * string returned and not throw and exception.
         */
        [Test]
        public void Test4735()
        {
            try
            {
                new UCultureInfo("und").GetDisplayKeywordValue("calendar", new UCultureInfo("de"));
                new UCultureInfo("en").GetDisplayKeywordValue("calendar", new UCultureInfo("de"));
            }
            catch (Exception e)
            {
                Errln("Unexpected exception: " + e.ToString());
            }
        }

        [Test]
        public void TestGetFallback()
        {
            // Testing static String getFallback(String)
            string[][] TESTIDS =
                    {
                new string[]{"en_US", "en", "", ""},    // UCultureInfo.GetParent("") should return ""
                new string[]{"EN_us_Var", "en_US", "en", ""},   // Case is always normalized
                new string[]{"de_DE@collation=phonebook", "de@collation=phonebook", "@collation=phonebook", "@collation=phonebook"},    // Keyword is preserved
                new string[]{"en__POSIX", "en", ""},    // Trailing empty segment should be truncated
                new string[]{"_US_POSIX", "_US", ""},   // Same as above
                new string[]{"root", ""},               // No canonicalization
            };

            foreach (String[] chain in TESTIDS)
            {
                for (int i = 1; i < chain.Length; i++)
                {
                    String fallback = UCultureInfo.GetParent(chain[i - 1]);
                    assertEquals("GetParent(\"" + chain[i - 1] + "\")", chain[i], fallback);
                }
            }

            // Testing UCultureInfo GetParent()
            UCultureInfo[][] TESTLOCALES =
                {
                new UCultureInfo[]{new UCultureInfo("en_US"), new UCultureInfo("en"), UCultureInfo.InvariantCulture, null},
                new UCultureInfo[]{new UCultureInfo("en__POSIX"), new UCultureInfo("en"), UCultureInfo.InvariantCulture, null},
                new UCultureInfo[]{new UCultureInfo("de_DE@collation=phonebook"), new UCultureInfo("de@collation=phonebook"), new UCultureInfo("@collation=phonebook"), null},
                new UCultureInfo[]{new UCultureInfo("_US_POSIX"), new UCultureInfo("_US"), UCultureInfo.InvariantCulture, null},
                new UCultureInfo[]{new UCultureInfo("root"), UCultureInfo.InvariantCulture, null},
            };

            foreach (UCultureInfo[] chain in TESTLOCALES)
            {
                for (int i = 1; i < chain.Length; i++)
                {
                    UCultureInfo fallback = chain[i - 1].GetParent();
                    assertEquals("UCultureInfo(" + chain[i - 1] + ").GetParent()", chain[i], fallback);
                }
            }
        }

        [Test]
        public void TestExtension()
        {
            string[][] TESTCASES = {
                    // new string[]{"<langtag>", "<ext key1>", "<ext val1>", "<ext key2>", "<ext val2>", ....},
                    new string[]{"en"},
                    new string[]{"en-a-exta-b-extb", "a", "exta", "b", "extb"},
                    new string[]{"en-b-extb-a-exta", "a", "exta", "b", "extb"},
                    new string[]{"de-x-a-bc-def", "x", "a-bc-def"},
                    new string[]{"ja-JP-u-cu-jpy-ca-japanese-x-java", "u", "ca-japanese-cu-jpy", "x", "java"},
            };

            foreach (String[] testcase in TESTCASES)
            {
                UCultureInfo loc = UCultureInfo.GetCultureInfoByIetfLanguageTag(testcase[0]);

                int nExtensions = (testcase.Length - 1) / 2;

                //ICollection<char> keys = loc.Extensions.Keys;
                if (loc.Extensions.Count != nExtensions)
                {
                    Errln("Incorrect number of extensions: returned="
                            + loc.Extensions.Count + ", expected=" + nExtensions
                            + ", locale=" + testcase[0]);
                }

                for (int i = 0; i < nExtensions; i++)
                {
                    String kstr = testcase[i / 2 + 1];
                    //String ext = //loc.GetExtension(kstr[0]);
                    loc.Extensions.TryGetValue(kstr[0], out string ext);
                    if (ext == null || !ext.Equals(testcase[i / 2 + 2]))
                    {
                        Errln("Incorrect extension value: key="
                                + kstr + ", returned=" + ext + ", expected=" + testcase[i / 2 + 2]
                                        + ", locale=" + testcase[0]);
                    }
                }
            }

            // Exception handling
            bool sawException = false;
            try
            {
                UCultureInfo l = UCultureInfo.GetCultureInfoByIetfLanguageTag("en-US-a-exta");
                //l.GetExtension('$');
                var _ = l.Extensions['$'];
            }
            catch (KeyNotFoundException e)
            {
                sawException = true;
            }
            if (!sawException)
            {
                Errln("getExtension must throw an exception on illegal input key");
            }
        }

        [Test]
        public void TestUnicodeLocaleExtension()
        {
            string[][] TESTCASES = {
                    //"<langtag>", "<attr1>,<attr2>,...", "<key1>,<key2>,...", "<type1>", "<type2>", ...},
                    new string[]{"en", null, null},
                    new string[]{"en-a-ext1-x-privuse", null, null},
                    new string[]{"en-u-attr1-attr2", "attr1,attr2", null},
                    new string[]{"ja-u-ca-japanese-cu-jpy", null, "ca,cu", "japanese", "jpy"},
                    new string[]{"th-TH-u-number-attr-nu-thai-ca-buddhist", "attr,number", "ca,nu", "buddhist", "thai"},
            };

            foreach (String[] testcase in TESTCASES)
            {
                UCultureInfo loc = UCultureInfo.GetCultureInfoByIetfLanguageTag(testcase[0]);

                ISet<String> expectedAttributes = new HashSet<String>();
                if (testcase[1] != null)
                {
                    String[] attrs = testcase[1].Split(',');
                    foreach (String s in attrs)
                    {
                        expectedAttributes.Add(s);
                    }
                }

                IDictionary<String, String> expectedKeywords = new Dictionary<String, String>();
                if (testcase[2] != null)
                {
                    String[] ukeys = testcase[2].Split(',');
                    for (int i = 0; i < ukeys.Length; i++)
                    {
                        expectedKeywords[ukeys[i]] = testcase[i + 3];
                    }
                }

                // Check attributes
                var attributes = loc.UnicodeLocaleAttributes;
                if (attributes.Count != expectedAttributes.Count)
                {
                    Errln("Incorrect number for Unicode locale attributes: returned="
                            + attributes.Count + ", expected=" + expectedAttributes.Count
                            + ", locale=" + testcase[0]);
                }
                if (!attributes.IsSupersetOf(expectedAttributes) || !expectedAttributes.IsSupersetOf(attributes))
                {
                    Errln("Incorrect set of attributes for locale " + testcase[0]);
                }

                // Check keywords
                //ICollection<String> keys = loc.UnicodeLocales.Keys;
                ICollection<String> expectedKeys = expectedKeywords.Keys;
                if (loc.UnicodeLocales.Count != expectedKeys.Count)
                {
                    Errln("Incorrect number for Unicode locale keys: returned="
                            + loc.UnicodeLocales.Count + ", expected=" + expectedKeys.Count
                            + ", locale=" + testcase[0]);
                }

                foreach (String expKey in expectedKeys)
                {
                    String type = loc.UnicodeLocales[expKey];
                    String expType = expectedKeywords[expKey];

                    if (type == null || !expType.Equals(type))
                    {
                        Errln("Incorrect Unicode locale type: key="
                                + expKey + ", returned=" + type + ", expected=" + expType
                                + ", locale=" + testcase[0]);
                    }
                }
            }

            // Exception handling
            bool sawException = false;
            try
            {
                UCultureInfo l = UCultureInfo.GetCultureInfoByIetfLanguageTag("en-US-u-ca-gregory");
                //l.GetUnicodeLocaleType("$%");
                var _ = l.UnicodeLocales["$%"];
            }
            catch (KeyNotFoundException e)
            {
                sawException = true;
            }
            if (!sawException)
            {
                Errln("getUnicodeLocaleType must throw an exception on illegal input key");
            }
        }

        // NOTE: We purposely don't provide an Equals and GetHashCode overload here to test the
        // comparison that is used for caching.
        private class CultureWithCalendar : CultureInfo
        {
            private readonly Calendar calendar;
            private Calendar[] optionalCalendars;

            public CultureWithCalendar(string culture, Calendar calendar)
                : base(culture)
            {
                this.calendar = calendar;
                this.DateTimeFormat.Calendar = calendar;
            }

            public override Calendar Calendar => calendar;

            public override Calendar[] OptionalCalendars
            {
                get
                {
                    if (optionalCalendars == null)
                    {
                        if (!(base.OptionalCalendars.Contains(calendar)))
                            optionalCalendars = new List<Calendar>(optionalCalendars) { calendar }.ToArray();
                        else
                            optionalCalendars = base.OptionalCalendars;
                    }
                    return optionalCalendars;
                }
            }

            public override string ToString()
            {
                return base.ToString() + $" with {calendar.GetType().Name}";
            }
        }
            

        public static IEnumerable<TestCaseData> GetCultureInfoData()
        {
            yield return new TestCaseData(CultureInfo.InvariantCulture,    "");
            yield return new TestCaseData(new CultureInfo("en-US"),        "en_US");
            yield return new TestCaseData(new CultureInfo("en-US-POSIX"),  "en_US_POSIX"); // Not recognized by .NET, but allowed

            // Cultures names that are unsupported in ICU.
            // These names are automatically converted to .NET equivalents. This test
            // ensures we can convert those .NET equivalent names back to ICU names.
            yield return new TestCaseData(new CultureInfo("ckb"),          "ckb");      // DotNet: ku
            yield return new TestCaseData(new CultureInfo("ckb-IQ"),       "ckb_IQ");   // DotNet: ku-Arab-IQ
            yield return new TestCaseData(new CultureInfo("ckb-IR"),       "ckb_IR");   // DotNet: ku-Arab-IR
            yield return new TestCaseData(new CultureInfo("fa-AF"),        "fa_AF");    // DotNet: prs-AF
            yield return new TestCaseData(new CultureInfo("qu"),           "qu");       // DotNet: quz
            yield return new TestCaseData(new CultureInfo("qu-BO"),        "qu_BO");    // DotNet: quz-BO
            yield return new TestCaseData(new CultureInfo("qu-EC"),        "qu_EC");    // DotNet: quz-EC
            yield return new TestCaseData(new CultureInfo("qu-PE"),        "qu_PE");    // DotNet: quz-PE


            // Alternate collation order
            yield return new TestCaseData(new CultureInfo("es-ES_tradnl"), "es_ES@collation=traditional");
            yield return new TestCaseData(new CultureInfo("zh-TW_pronun"), "zh_TW@collation=zhuyin");
            yield return new TestCaseData(new CultureInfo("zh-CN_stroke"), "zh_CN@collation=stroke");
            yield return new TestCaseData(new CultureInfo("zh-SG_stroke"), "zh_SG@collation=stroke");
            yield return new TestCaseData(new CultureInfo("zh-MO_stroke"), "zh_MO");
            yield return new TestCaseData(new CultureInfo("zh-MO"),        "zh_MO@collation=pinyin");
            yield return new TestCaseData(new CultureInfo("de-DE_phoneb"), "de_DE@collation=phonebook");
            yield return new TestCaseData(new CultureInfo("hu-HU_technl"), "hu_HU"); // Not supported in ICU
            yield return new TestCaseData(new CultureInfo("ka-GE_modern"), "ka_GE");


            // Default calendar
            yield return new TestCaseData(new CultureInfo("ja-JP"), "ja_JP");             // Gregorian
            yield return new TestCaseData(new CultureInfo("fa"), "fa");                   // Persian
            yield return new TestCaseData(new CultureInfo("th-TH"), "th_TH");             // buddhist

            yield return new TestCaseData(new CultureInfo("ar-SA"), "ar_SA");             // islamic-umalqura
            yield return new TestCaseData(new CultureInfo("ckb-IR"), "ckb_IR");           // persian
            yield return new TestCaseData(new CultureInfo("fa-IR"), "fa_IR");             // persian
            yield return new TestCaseData(new CultureInfo("lrc-IR"), "lrc_IR");           // persian
            yield return new TestCaseData(new CultureInfo("mzn-IR"), "mzn_IR");           // persian
            yield return new TestCaseData(new CultureInfo("ps-AF"), "ps_AF");             // persian
            yield return new TestCaseData(new CultureInfo("uz-Arab-AF"), "uz_Arab_AF");   // persian

            // Alternate calendar
            yield return new TestCaseData(new CultureWithCalendar("ja-JP", new JapaneseCalendar()), "ja_JP@calendar=japanese");
            yield return new TestCaseData(new CultureWithCalendar("fa", new GregorianCalendar()), "fa@calendar=gregorian");
            yield return new TestCaseData(new CultureWithCalendar("th-TH", new GregorianCalendar()), "th_TH@calendar=gregorian");
        }

        [Test, TestCaseSource(nameof(GetCultureInfoData))]
        public void TestToUCultureInfo(CultureInfo culture, string fullName)
        {
            assertEquals($"ToUCultureInfo() with {culture}", fullName, culture.ToUCultureInfo().FullName);
        }


        public static IEnumerable<TestCaseData> GetUCultureInfoData()
        {
            yield return new TestCaseData("",                                   CultureInfo.InvariantCulture);
            yield return new TestCaseData("en_US",                              new CultureInfo("en-US"));
            yield return new TestCaseData("_US",                                new CultureInfo("US"));

            // Since .NET doesn't recognize nn-NO-NY, we are simply doing default conversion
            yield return new TestCaseData("nn_NO",                              new CultureInfo("nn-NO"));
            
            // ICU4N TODO: Create conversions for calendars
            //yield return new TestCaseData("ja_JP@calendar=japanese", new CultureInfo("ja-JP"));

            // In .NET, there is no way to switch to the thai numbering system, so we should
            // simply return the default Thai culture
            yield return new TestCaseData("th_TH@numbers=thai",                 new CultureInfo("th-TH"));

            // Alternate collation order
            yield return new TestCaseData("es_ES@collation=traditional",        new CultureInfo("es-ES_tradnl"));
            yield return new TestCaseData("zh_TW@collation=zhuyin",             new CultureInfo("zh-TW_pronun"));
            yield return new TestCaseData("zh_CN@collation=stroke",             new CultureInfo("zh-CN_stroke"));
            yield return new TestCaseData("zh_SG@collation=stroke",             new CultureInfo("zh-SG_stroke"));
            yield return new TestCaseData("zh_MO",                              new CultureInfo("zh-MO_stroke"));
            yield return new TestCaseData("zh_MO@collation=pinyin",             new CultureInfo("zh-MO"));
            yield return new TestCaseData("de_DE@collation=phonebook",          new CultureInfo("de-DE_phoneb"));
        }

        [Test, TestCaseSource("GetUCultureInfoData")]
        public void TestToCultureInfo(string fullName, CultureInfo culture)
        {
            assertEquals($"ToCultureInfo() with {fullName}", culture, new UCultureInfo(fullName).ToCultureInfo());
        }

        [Test]
        [Ignore("Run this to find gaps in cultures between platforms")]
        // ICU4N specific - make sure all ICU cultures will convert to .NET cultures without exceptions
        public void TestToCultureInfo_AllCultures()
        {
            var locales = UCultureInfo.GetCultures(UCultureTypes.AllCultures);
            var unknownCultures = new J2N.Collections.Generic.Dictionary<CultureInfo, string>();
            

            CultureInfo ci = null;
            foreach (var locale in locales)
            {
                ci = locale.ToCultureInfo();
                if (ci.EnglishName.StartsWith("Unknown Language", StringComparison.Ordinal))
                    unknownCultures.Add(ci, locale.EnglishName);
            }

            assertEquals($"Cultures not mapped: \n\n{unknownCultures.ToString(StringFormatter.InvariantCulture)}\n\n", 0, unknownCultures.Count);
        }

        [Test]
        [Ignore("ICU4N TOOD: Fix this")]
        public void TestCategoryDefault()
        {
            CultureInfo backupDefault = CultureInfo.CurrentCulture;

            UCultureInfo orgDefault = UCultureInfo.CurrentCulture;

            // Setting a category default won't change default UCultureInfo
            UCultureInfo uJaJp = new UCultureInfo("ja_JP");
            UCultureInfo uDeDePhonebook = new UCultureInfo("de_DE@collation=phonebook");

            //ULocale.SetDefault(Category.DISPLAY, uJaJp);
            //ULocale.SetDefault(Category.FORMAT, uDeDePhonebook);
//#if FEATURE_CULTUREINFO_CURRENTCULTURE_SETTER
//            UCultureInfo.CurrentUICulture = uJaJp;
//            UCultureInfo.CurrentCulture = uDeDePhonebook;
//#else
//            System.Threading.Thread.CurrentThread.CurrentUICulture = uJaJp;
//            System.Threading.Thread.CurrentThread.CurrentCulture = uDeDePhonebook;
//#endif
            UCultureInfo.CurrentUICulture = uJaJp;
            UCultureInfo.CurrentCulture = uDeDePhonebook;

            //if (!UCultureInfo.CurrentCulture.Equals(orgDefault))
            //{
            //    Errln("FAIL: Default ULocale is " + UCultureInfo.CurrentCulture + ", expected: " + orgDefault);
            //}

            if (!UCultureInfo.CurrentUICulture.Equals(uJaJp))
            {
                Errln("FAIL: DISPLAY ULocale is " + UCultureInfo.CurrentUICulture + ", expected: " + uJaJp);
            }

            if (!UCultureInfo.CurrentCulture.Equals(uDeDePhonebook))
            {
                Errln("FAIL: FORMAT ULocale is " + UCultureInfo.CurrentCulture + ", expected: " + uDeDePhonebook);
            }

            // Setting ULocale default will overrides category defaults
            UCultureInfo uFrFr = new UCultureInfo("fr_FR");

            //UCultureInfo.DotNetLocaleHelper.CurrentCulture = uFrFr;
            //UCultureInfo.DotNetLocaleHelper.CurrentUICulture = uFrFr; // ICU4N: .NET dosen't automatically set both values, you have to do it explicitly

            if (!UCultureInfo.CurrentUICulture.Equals(uFrFr))
            {
                Errln("FAIL: DISPLAY ULocale is " + UCultureInfo.CurrentUICulture + ", expected: " + uFrFr);
            }

            if (!UCultureInfo.CurrentCulture.Equals(uFrFr))
            {
                Errln("FAIL: FORMAT ULocale is " + UCultureInfo.CurrentCulture + ", expected: " + uFrFr);
            }

            // Setting Locale default will updates ULocale default and category defaults
            CultureInfo arEg = new CultureInfo("ar-EG");
            UCultureInfo uArEg = arEg.ToUCultureInfo();

#if FEATURE_CULTUREINFO_CURRENTCULTURE_SETTER
            CultureInfo.CurrentCulture = arEg;
#else
            System.Threading.Thread.CurrentThread.CurrentCulture = arEg;
#endif
            

            if (!UCultureInfo.CurrentCulture.Equals(uArEg))
            {
                Errln("FAIL: Default ULocale is " + UCultureInfo.CurrentCulture + ", expected: " + uArEg);
            }

            if (!UCultureInfo.CurrentUICulture.Equals(uArEg))
            {
                Errln("FAIL: DISPLAY ULocale is " + UCultureInfo.CurrentUICulture + ", expected: " + uArEg);
            }

            if (!UCultureInfo.CurrentCulture.Equals(uArEg))
            {
                Errln("FAIL: FORMAT ULocale is " + UCultureInfo.CurrentCulture + ", expected: " + uArEg);
            }

            // Restore back up
#if FEATURE_CULTUREINFO_CURRENTCULTURE_SETTER
            CultureInfo.CurrentCulture = backupDefault;
#else
            System.Threading.Thread.CurrentThread.CurrentCulture = backupDefault;
#endif
        }

        //
        // Test case for the behavior of Comparable implementation.
        //
        [Test]
        public void TestComparable()
        {
            // Test strings used for creating UCultureInfo objects.
            // This list contains multiple different strings creating
            // multiple equivalent locales.
            string[] localeStrings = {
                    "en",
                    "EN",
                    "en_US",
                    "en_GB",
                    "en_US_POSIX",
                    "en_us_posix",
                    "ar_EG",
                    "zh_Hans_CN",
                    "zh_Hant_TW",
                    "zh_Hans",
                    "zh_CN",
                    "zh_TW",
                    "th_TH@calendar=buddhist;numbers=thai",
                    "TH_TH@NUMBERS=thai;CALENDAR=buddhist",
                    "th_TH@calendar=buddhist",
                    "th_TH@calendar=gergorian",
                    "th_TH@numbers=latn",
                    "abc_def_ghi_jkl_opq",
                    "abc_DEF_ghi_JKL_opq",
                    "",
                    "und",
                    "This is a bogus locale ID",
                    "This is a BOGUS locale ID",
                    "en_POSIX",
                    "en__POSIX",
            };

            UCultureInfo[] locales = new UCultureInfo[localeStrings.Length];
            for (int i = 0; i < locales.Length; i++)
            {
                locales[i] = new UCultureInfo(localeStrings[i]);
            }

            // compares all permutations
            for (int i = 0; i < locales.Length; i++)
            {
                for (int j = i /* including the locale itself */; j < locales.Length; j++)
                {
                    bool eqls1 = locales[i].Equals(locales[j]);
                    bool eqls2 = locales[i].Equals(locales[j]);

                    if (eqls1 != eqls2)
                    {
                        Errln("FAILED: loc1.Equals(loc2) and loc2.Equals(loc1) return different results: loc1="
                                + locales[i] + ", loc2=" + locales[j]);
                    }

                    int cmp1 = locales[i].CompareTo(locales[j]);
                    int cmp2 = locales[j].CompareTo(locales[i]);

                    if ((cmp1 == 0) != eqls1)
                    {
                        Errln("FAILED: inconsistent equals and compareTo: loc1="
                                + locales[i] + ", loc2=" + locales[j]);
                    }
                    if (cmp1 < 0 && cmp2 <= 0 || cmp1 > 0 && cmp2 >= 0 || cmp1 == 0 && cmp2 != 0)
                    {
                        Errln("FAILED: loc1.compareTo(loc2) is inconsistent with loc2.compareTo(loc1): loc1="
                                + locales[i] + ", loc2=" + locales[j]);
                    }
                }
            }

            // Make sure UCultureInfo objects can be sorted by the Java collection
            // framework class without providing a Comparator, and equals/compareTo
            // are consistent.

            // The sorted locale list created from localeStrings above.
            // Duplicated locales are removed and locale string is normalized
            // (by the UCultureInfo constructor).
            String[] sortedLocaleStrings = {
                    "",
                    "abc_DEF_GHI_JKL_OPQ",
                    "ar_EG",
                    "en",
                    "en__POSIX",
                    "en_GB",
                    "en_US",
                    "en_US_POSIX",
                    "th_TH@calendar=buddhist",
                    "th_TH@calendar=buddhist;numbers=thai",
                    "th_TH@calendar=gergorian",
                    "th_TH@numbers=latn",
                    "this is a bogus locale id",
                    "und",
                    "zh_CN",
                    "zh_TW",
                    "zh_Hans",
                    "zh_Hans_CN",
                    "zh_Hant_TW",
            };

            SortedSet<UCultureInfo> sortedLocales = new SortedSet<UCultureInfo>();
            foreach (UCultureInfo locale in locales)
            {
                sortedLocales.Add(locale);
            }

            // Check the number of unique locales
            if (sortedLocales.Count != sortedLocaleStrings.Length)
            {
                Errln("FAILED: Number of unique locales: " + sortedLocales.Count + ", expected: " + sortedLocaleStrings.Length);
            }

            // Check the order
            int i2 = 0;
            foreach (UCultureInfo loc in sortedLocales)
            {
                if (!loc.ToString().Equals(sortedLocaleStrings[i2++]))
                {
                    Errln("FAILED: Sort order is incorrect for " + loc.ToString());
                    break;
                }
            }
        }

        [Test]
        public void TestToUnicodeLocaleKey()
        {
            string[][] DATA = {
                    new string[] {"calendar",    "ca"},
                    new string[] {"CALEndar",    "ca"},  // difference casing
                    new string[] {"ca",          "ca"},  // bcp key itself
                    new string[] {"kv",          "kv"},  // no difference between legacy and bcp
                    new string[] {"foo",         null},  // unknown, bcp ill-formed
                    new string[] {"ZZ",          "zz"},  // unknown, bcp well-formed
            };

            foreach (String[] d in DATA)
            {
                String keyword = d[0];
                String expected = d[1];

                String bcpKey = UCultureInfo.ToUnicodeLocaleKey(keyword);
                assertEquals("keyword=" + keyword, expected, bcpKey);
            }
        }

        [Test]
        public void TestToLegacyKey()
        {
            string[][] DATA = {
                    new string[] {"kb",          "colbackwards"},
                    new string[] {"kB",          "colbackwards"},    // different casing
                    new string[] {"Collation",   "collation"},   // keyword itself with different casing
                    new string[] {"kv",          "kv"},  // no difference between legacy and bcp
                    new string[] {"foo",         "foo"}, // unknown, bcp ill-formed
                    new string[] {"ZZ",          "zz"},  // unknown, bcp well-formed
                    new string[] {"e=mc2",       null},  // unknown, bcp/legacy ill-formed
            };

            foreach (String[] d in DATA)
            {
                String keyword = d[0];
                String expected = d[1];

                String legacyKey = UCultureInfo.ToLegacyKey(keyword);
                assertEquals("bcpKey=" + keyword, expected, legacyKey);
            }
        }

        [Test]
        public void TestToUnicodeLocaleType()
        {
            string[][] DATA = {
                    new string[] {"tz",              "Asia/Kolkata",     "inccu"},
                    new string[] {"calendar",        "gregorian",        "gregory"},
                    new string[] {"ca",              "gregorian",        "gregory"},
                    new string[] {"ca",              "Gregorian",        "gregory"},
                    new string[] {"ca",              "buddhist",         "buddhist"},
                    new string[] {"Calendar",        "Japanese",         "japanese"},
                    new string[] {"calendar",        "Islamic-Civil",    "islamic-civil"},
                    new string[] {"calendar",        "islamicc",         "islamic-civil"},   // bcp type alias
                    new string[] {"colalternate",    "NON-IGNORABLE",    "noignore"},
                    new string[] {"colcaselevel",    "yes",              "true"},
                    new string[] {"rg",              "GBzzzz",           "gbzzzz"},
                    new string[] {"tz",              "america/new_york", "usnyc"},
                    new string[] {"tz",              "Asia/Kolkata",     "inccu"},
                    new string[] {"timezone",        "navajo",           "usden"},
                    new string[] {"ca",              "aaaa",             "aaaa"},    // unknown type, well-formed type
                    new string[] {"ca",              "gregory-japanese-islamic", "gregory-japanese-islamic"},    // unknown type, well-formed type
                    new string[] {"zz",              "gregorian",        null},      // unknown key, ill-formed type
                    new string[] {"co",              "foo-",             null},      // unknown type, ill-formed type
                    new string[] {"variableTop",     "00A0",             "00a0"},    // valid codepoints type
                    new string[] {"variableTop",     "wxyz",             "wxyz"},      // invalid codepoints type - return as is for now
                    new string[] {"kr",              "space-punct",      "space-punct"}, // valid reordercode type
                    new string[] {"kr",              "digit-spacepunct", null},      // invalid reordercode type
            };

            foreach (String[] d in DATA)
            {
                String keyword = d[0];
                String value = d[1];
                String expected = d[2];

                String bcpType = UCultureInfo.ToUnicodeLocaleType(keyword, value);
                assertEquals("keyword=" + keyword + ", value=" + value, expected, bcpType);
            }

        }

        [Test]
        public void TestToLegacyType()
        {
            string[][] DATA = {
                    new string[] {"calendar",        "gregory",          "gregorian"},
                    new string[] {"ca",              "gregory",          "gregorian"},
                    new string[] {"ca",              "Gregory",          "gregorian"},
                    new string[] {"ca",              "buddhist",         "buddhist"},
                    new string[] {"Calendar",        "Japanese",         "japanese"},
                    new string[] {"calendar",        "Islamic-Civil",    "islamic-civil"},
                    new string[] {"calendar",        "islamicc",         "islamic-civil"},   // bcp type alias
                    new string[] {"colalternate",    "noignore",         "non-ignorable"},
                    new string[] {"colcaselevel",    "true",             "yes"},
                    new string[] {"rg",              "gbzzzz",           "gbzzzz"},
                    new string[] {"tz",              "usnyc",            "America/New_York"},
                    new string[] {"tz",              "inccu",            "Asia/Calcutta"},
                    new string[] {"timezone",        "usden",            "America/Denver"},
                    new string[] {"timezone",        "usnavajo",         "America/Denver"},  // bcp type alias
                    new string[] {"colstrength",     "quarternary",      "quaternary"},  // type alias
                    new string[] {"ca",              "aaaa",             "aaaa"},    // unknown type
                    new string[] {"calendar",        "gregory-japanese-islamic", "gregory-japanese-islamic"},    // unknown type, well-formed type
                    new string[] {"zz",              "gregorian",        "gregorian"},   // unknown key, bcp ill-formed type
                    new string[] {"ca",              "gregorian-calendar",   "gregorian-calendar"},  // known key, bcp ill-formed type
                    new string[] {"co",              "e=mc2",            null},  // known key, ill-formed bcp/legacy type
                    new string[] {"variableTop",     "00A0",             "00a0"},        // valid codepoints type
                    new string[] {"variableTop",     "wxyz",             "wxyz"},        // invalid codepoints type - return as is for now
                    new string[] {"kr",              "space-punct",      "space-punct"}, // valid reordercode type
                    new string[] {"kr",              "digit-spacepunct", "digit-spacepunct"},    // invalid reordercode type, but ok for legacy syntax
            };

            foreach (String[] d in DATA)
            {
                String keyword = d[0];
                String value = d[1];
                String expected = d[2];

                String legacyType = UCultureInfo.ToLegacyType(keyword, value);
                assertEquals("keyword=" + keyword + ", value=" + value, expected, legacyType);
            }
        }

        [Test]
        public void TestIsRightToLeft()
        {
            assertFalse("root LTR", UCultureInfo.InvariantCulture.IsRightToLeft);
            assertFalse("zh LTR", new UCultureInfo("zh").IsRightToLeft);
            assertTrue("ar RTL", new UCultureInfo("ar").IsRightToLeft);
            assertTrue("und-EG RTL", new UCultureInfo("und-EG").IsRightToLeft);
            assertFalse("fa-Cyrl LTR", new UCultureInfo("fa-Cyrl").IsRightToLeft);
            assertTrue("en-Hebr RTL", new UCultureInfo("en-Hebr").IsRightToLeft);
            assertTrue("ckb RTL", new UCultureInfo("ckb").IsRightToLeft);  // Sorani Kurdish
            assertFalse("fil LTR", new UCultureInfo("fil").IsRightToLeft);
            assertFalse("he-Zyxw LTR", new UCultureInfo("he-Zyxw").IsRightToLeft);
        }

        [Test]
        public void TestChineseToLocale()
        {
            UCultureInfo[][] LOCALES = {
                    new UCultureInfo[]{ new UCultureInfo("zh"),             new UCultureInfo("zh")},
                    new UCultureInfo[]{ new UCultureInfo("zh_Hans"),        new UCultureInfo("zh_Hans")},
                    new UCultureInfo[]{ new UCultureInfo("zh_Hant"),        new UCultureInfo("zh_Hant")},
                    new UCultureInfo[]{ new UCultureInfo("zh_Hans_CN"),     new UCultureInfo("zh_Hans_CN")},
                    new UCultureInfo[]{ new UCultureInfo("zh_Hans_CN"),     new UCultureInfo("zh_Hans_CN")},
                    new UCultureInfo[]{ new UCultureInfo("zh_Hant_TW"),     new UCultureInfo("zh_Hant_TW")},
            };

            // When two UCultureInfo are equal, results of UCultureInfo.ToCultureInfo() must be
            // also equal.
            foreach (UCultureInfo[] pair in LOCALES)
            {
                if (pair[0].Equals(pair[1]))
                {
                    assertEquals(pair[0].ToString(), pair[0].ToCultureInfo(), pair[1].ToCultureInfo());
                }
                else
                {
                    // This could happen when the definition of UCultureInfo constant is changed.
                    // When it happens, it could be a mistake. So we use Errln below.
                    // If we change the definitioin for a legitimate reason, then the hardcoded
                    // test data above should be reviewed and updated.
                    Errln("Error: " + pair[0] + " is not equal to " + pair[1]);
                }
            }
        }



        // From System.Globalization

        [TestCase("en_US", "en")]
        [TestCase("en", "")]
        [TestCase("", "")]
        public void TestParent(string name, string expectedParentName)
        {
            UCultureInfo culture = new UCultureInfo(name);
            Assert.AreEqual(new UCultureInfo(expectedParentName), culture.Parent);
        }

        [Test]
        [Ignore("ICU4N TODO: Differs from .NET behavior")]
        public void TestParent_ParentChain()
        {
            UCultureInfo myExpectParentCulture = new UCultureInfo("uz_Cyrl_UZ");
            Assert.AreEqual("uz_Cyrl", myExpectParentCulture.Parent.Name);
            Assert.AreEqual("uz", myExpectParentCulture.Parent.Parent.Name);
            Assert.AreEqual("", myExpectParentCulture.Parent.Parent.Parent.Name);
        }

        [TestCase("de_DE", "de")]
        [TestCase("en", "en")]
        //[TestCase("", "iv")]
        [TestCase("", null)] // ICU4N TODO: ICU behaves differently than .NET in this regard
        public void TestTwoLetterISOLanguageName(string name, string expected)
        {
            Assert.AreEqual(expected, new UCultureInfo(name).TwoLetterISOLanguageName);
        }

        public static IEnumerable<object[]> NativeName_TestData()
        {
            yield return new object[] { UCultureInfo.CurrentCulture.Name, UCultureInfo.CurrentCulture.NativeName };
            yield return new object[] { "en_US", "English (United States)" };
            yield return new object[] { "en_CA", "English (Canada)" };
        }

        [Test, TestCaseSource(nameof(NativeName_TestData))]
        public void TestNativeName(string name, string expected)
        {
            UCultureInfo myTestCulture = new UCultureInfo(name);
            Assert.AreEqual(expected, myTestCulture.NativeName);
        }

        [TestCase("en_US")]
        [TestCase("en")]
        [TestCase("")]
        public void TestGetHashCode(string name)
        {
            // The only guarantee that can be made about HashCodes is that they will be the same across calls
            UCultureInfo culture = new UCultureInfo(name);
            Assert.AreEqual(culture.GetHashCode(), culture.GetHashCode());
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            UCultureInfo frFRCulture = new UCultureInfo("fr_FR");
            yield return new object[] { frFRCulture, frFRCulture.Clone(), true };
            yield return new object[] { frFRCulture, frFRCulture, true };
            yield return new object[] { new UCultureInfo("en"), new UCultureInfo("en"), true };
            yield return new object[] { new UCultureInfo("en_US"), new UCultureInfo("en_US"), true };
            yield return new object[] { UCultureInfo.InvariantCulture, UCultureInfo.InvariantCulture, true };
            yield return new object[] { UCultureInfo.InvariantCulture, new UCultureInfo(""), true };

            yield return new object[] { new UCultureInfo("en"), new UCultureInfo("en_US"), false };
            yield return new object[] { new UCultureInfo("en_US"), new UCultureInfo("fr_FR"), false };
            yield return new object[] { new UCultureInfo("en_US"), null, false };
        }

        [Theory]
        [TestCaseSource(nameof(Equals_TestData))]
        public void TestEquals(UCultureInfo culture, object value, bool expected)
        {
            Assert.AreEqual(expected, culture.Equals(value));
        }


        //[Test]
        //public void TestLanguageTagAllCultures()
        //{
        //    var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
        //    foreach (var culture in cultures)
        //    {
        //        Assert.AreEqual(culture.IetfLanguageTag, culture.ToUCultureInfo().IetfLanguageTag);
        //    }
        //}

        //[Test]
        //public void TestDisplayNameAllCultures()
        //{
        //    var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
        //    foreach (var culture in cultures)
        //    {
        //        Assert.AreEqual(culture.DisplayName, culture.ToUCultureInfo().DisplayName2);
        //    }
        //}

        //[Test]
        //public void TestEnglishNameAllCultures()
        //{
        //    var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
        //    foreach (var culture in cultures)
        //    {
        //        //culture.
        //        var expected = culture.EnglishName;
        //        var actualCulture = culture.ToUCultureInfo();
        //        var actual = actualCulture.EnglishName;
        //        Assert.AreEqual(expected, actual);
        //    }
        //}

        [Test]
        [TestCase("zh-TW")]
        [TestCase("zh-CN")]
        [TestCase("zh-HK")]
        [TestCase("zh-MO")]
        [TestCase("zh-SG")]
        public void TestChineseCharactersGH29(string cultureName)
        {
            // If you're not running Windows in Chinese, set explicitly
            CurrentCulture = new CultureInfo(cultureName);

            Assert.DoesNotThrow(() => { var _ = UCultureInfo.CurrentCulture; });
        }

    }
}
