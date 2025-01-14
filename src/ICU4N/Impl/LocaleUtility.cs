﻿using System;
using System.Globalization;
using System.Linq;

namespace ICU4N.Globalization // ICU4N: Moved from ICU4N.Impl namespace
{
    /// <summary>
    /// A class to hold utility functions missing from java.util.Locale.
    /// </summary>
     // ICU4N TODO: Move to Globalization namespace ?
    public class LocaleUtility // ICU4N TODO: Evaluate the need for this class, or whether UCultureInfo can serve as an all-inclusive culture object
    {
        /**
         * A helper function to convert a string of the form
         * aa_BB_CC to a locale object.  Why isn't this in Locale?
         */
        public static CultureInfo GetLocaleFromName(string name)
        {
            if (name.Equals("root", StringComparison.OrdinalIgnoreCase) || name.Equals("any", StringComparison.OrdinalIgnoreCase))
            {
                return CultureInfo.InvariantCulture;
            }

            // Strip off the config options
            int optionsIndex = name.IndexOf('@');
            if (optionsIndex > -1)
            {
                // ICU4N TODO: Need to convert calendar, currency, number, and collation options by
                // creating a custom CultureInfo subclass...where possible

                name = name.Substring(0, optionsIndex); // ICU4N: Checked 2nd parameter
            }

            string newName = name.Replace('_', '-').Trim('-');


            try
            {
                CultureInfo culture = new CultureInfo(newName);

#if FEATURE_CULTUREINFO_UNKNOWNLANGUAGE
                // ICU4N: In .NET Standard 1.x, some invalid cultures are allowed
                // to be created, but will be "unknown" languages. We need to manually
                // ignore these.
                if (culture.EnglishName.StartsWith("Unknown Language", StringComparison.Ordinal))
                {
                    return null;
                }
#endif
                return culture;
            }
            catch (CultureNotFoundException)
            {
                return null;
            }



            //// ICU4N TODO: Not sure what to do with "any". It is in the
            //// IANA subtag registry, but not supported in .NET.
            //if (name.Equals("any", StringComparison.OrdinalIgnoreCase))
            //{
            //    return CultureInfo.InvariantCulture;
            //}

            //return new CultureInfo(name);

            //string language = "";
            //string country = "";
            //string variant = "";

            //int i1 = name.indexOf('_');
            //if (i1 < 0)
            //{
            //    language = name;
            //}
            //else
            //{
            //    language = name.substring(0, i1);
            //    ++i1;
            //    int i2 = name.indexOf('_', i1);
            //    if (i2 < 0)
            //    {
            //        country = name.substring(i1);
            //    }
            //    else
            //    {
            //        country = name.substring(i1, i2);
            //        variant = name.substring(i2 + 1);
            //    }
            //}

            //return new Locale(language, country, variant);
        }

        /**
         * Compare two locale strings of the form aa_BB_CC, and
         * return true if parent is a 'strict' fallback of child, that is,
         * if child =~ "^parent(_.+)*" (roughly).
         */
        public static bool IsFallbackOf(string parent, string child)
        {
            if (!child.StartsWith(parent, StringComparison.Ordinal))
            {
                return false;
            }
            int i = parent.Length;
            return (i == child.Length ||
                    child[i] == '-');
        }

        /**
         * Compare two locales, and return true if the parent is a
         * 'strict' fallback of the child (parent string is a fallback
         * of child string).
         */
        public static bool IsFallbackOf(CultureInfo parent, CultureInfo child)
        {
            return IsFallbackOf(parent.ToString(), child.ToString());
        }


        //   /*
        //    * Convenience method that calls canonicalLocaleString(string) with
        //    * locale.toString();
        //    */
        //   /*public static string canonicalLocaleString(Locale locale) {
        //       return canonicalLocaleString(locale.toString());
        //   }*/

        //   /*
        //    * You'd think that Locale canonicalizes, since it munges the
        //    * renamed languages, but it doesn't quite.  It forces the region
        //    * to be upper case but doesn't do anything about the language or
        //    * variant.  Our canonical form is 'lower_UPPER_UPPER'.  
        //    */
        //   /*public static string canonicalLocaleString(string id) {
        //       if (id != null) {
        //           int x = id.indexOf('_');
        //           if (x == -1) {
        //               id = id.toLowerCase(Locale.ENGLISH);
        //           } else {
        //               StringBuffer buf = new StringBuffer();
        //               buf.append(id.substring(0, x).toLowerCase(Locale.ENGLISH));
        //               buf.append(id.substring(x).toUpperCase(Locale.ENGLISH));

        //               int len = buf.length();
        //               int n = len;
        //               while (--n >= 0 && buf.charAt(n) == '_') {
        //               }
        //               if (++n != len) {
        //                   buf.delete(n, len);
        //               }
        //               id = buf.toString();
        //           }
        //       }
        //       return id;
        //   }*/

        //        /**
        //         * Fallback from the given locale name by removing the rightmost _-delimited
        //         * element. If there is none, return the root locale ("", "", ""). If this
        //         * is the root locale, return null. NOTE: The string "root" is not
        //         * recognized; do not use it.
        //         * 
        //         * @return a new Locale that is a fallback from the given locale, or null.
        //         */
        //        public static CultureInfo Fallback(CultureInfo loc)
        //        {
        //            if (loc.Equals(CultureInfo.InvariantCulture))
        //            {
        //                return null;
        //            }

        //#if FEATURE_CULTUREINFO_UNKNOWNLANGUAGE
        //            // ICU4N: In .NET Standard 1.x, some invalid cultures are allowed
        //            // to be created, but will be "unknown" languages. We need to manually
        //            // ignore these.
        //            if (loc.EnglishName.StartsWith("Unknown Language", StringComparison.Ordinal))
        //            {
        //                return CultureInfo.InvariantCulture;
        //            }
        //#endif
        //            // ICU4N: We use the original ICU fallback scheme rather than
        //            // simply using loc.Parent.

        //            // Split the locale into parts and remove the rightmost part
        //            string[] parts = loc.Name.Split('-');
        //            if (parts.Length == 1)
        //            {
        //                return null; // All parts were empty
        //            }
        //            string culture = parts[0];
        //            for (int i = 1; i < parts.Length - 1; i++)
        //            {
        //                culture += '-' + parts[i];
        //            }
        //            return new CultureInfo(culture);
        //        }

        /**
 * Fallback from the given locale name by removing the rightmost _-delimited
 * element. If there is none, return the root locale ("", "", ""). If this
 * is the root locale, return null. NOTE: The string "root" is not
 * recognized; do not use it.
 * 
 * @return a new Locale that is a fallback from the given locale, or null.
 */
        public static CultureInfo Fallback(CultureInfo loc)
        {
            if (CultureInfo.InvariantCulture.Equals(loc))
                return null;

            //if (CultureInfo.InvariantCulture.Equals(loc.Parent))
            //    return null;

            //return loc.Parent;

#if FEATURE_CULTUREINFO_UNKNOWNLANGUAGE
            // ICU4N: In .NET Standard 1.x, some invalid cultures are allowed
            // to be created, but will be "unknown" languages. We need to manually
            // ignore these.
            if (loc.EnglishName.StartsWith("Unknown Language", StringComparison.Ordinal))
            {
                return null;
            }
#endif
            // ICU4N: We use the original ICU fallback scheme rather than
            // simply using loc.Parent.

            var parser = new LocaleIDParser(loc.Name);

            // Split the locale into parts and remove the rightmost part
            const int language = 0;
            const int country = 1;
            const int variant = 2;

            string[] parts = new string[] { parser.GetLanguage(), parser.GetCountry(), parser.GetVariant() };
            int i;
            for (i = 2; i >= 0; --i)
            {
                if (parts[i].Length != 0)
                {
                    parts[i] = "";
                    break;
                }
            }
            if (i < 0)
            {
                return null; // All parts were empty
            }
            return new CultureInfo(
                parts[language] +
                (parts[country].Length > 0 ? '-' + parts[country] : "") +
                (parts[variant].Length > 0 ? '-' + parts[variant] : ""));


            //if (parts.Length == 1)
            //{
            //    return null; // All parts were empty
            //}
            //string culture = parts[0];
            //for (int i = 1; i < parts.Length - 1; i++)
            //{
            //    culture += '-' + parts[i];
            //}
            //return new CultureInfo(culture);




            //            // Split the locale into parts and remove the rightmost part
            //            string[] parts = loc.Name.Split('-');
            //            if (parts.Length == 1)
            //            {
            //                return null; // All parts were empty
            //            }
            //            string culture = parts[0];
            //            for (int i = 1; i < parts.Length - 1; i++)
            //            {
            //                culture += '-' + parts[i];
            //            }
            //            return new CultureInfo(culture);
        }

        public static CultureInfo Fallback(UCultureInfo locale)
        {
            return Fallback(locale.FullName);
        }

        /// <summary>
        /// Fallback from the given ICU locale name 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static CultureInfo Fallback(string name)
        {
            //if (name.Equals("root", StringComparison.OrdinalIgnoreCase) || name.Equals("any", StringComparison.OrdinalIgnoreCase))
            //{
            //    return CultureInfo.InvariantCulture;
            //}

            //// Strip off the config options
            //int optionsIndex = name.IndexOf('@');
            //if (optionsIndex > -1)
            //{
            //    // ICU4N TODO: Need to convert calendar, currency, number, and collation options by
            //    // creating a custom CultureInfo subclass...where possible

            //    name = name.Substring(0, optionsIndex); // ICU4N: Checked 2nd parameter
            //}

            // There should be no more than 3...
            string[] segments = name.Split(new char[] { '_', '-' });

            CultureInfo culture = null;

            // Fallback to more general culture...
            for (int i = Math.Max(3, segments.Length); i > 0; i--)
            {
                try
                {
                    string newName = string.Join("-", segments.Take(i));
                    culture = new CultureInfo(newName);

#if FEATURE_CULTUREINFO_UNKNOWNLANGUAGE
                    // ICU4N: In .NET Standard 1.x, some invalid cultures are allowed
                    // to be created, but will be "unknown" languages. We need to manually
                    // ignore these.
                    if (culture.EnglishName.StartsWith("Unknown Language", StringComparison.Ordinal))
                    {
                        continue;
                    }
#endif
                    break;
                }
                catch (CultureNotFoundException)
                {
                    continue;
                }
            }

            //if (culture == null)
            //{
            //    // Hopefully we don't get here...the only logical fallback is InvariantCulture
            //    culture = CultureInfo.InvariantCulture;
            //}

            return culture;
        }
    }
}
