﻿using ICU4N.Impl;

namespace ICU4N.Globalization
{
    /// <summary>
    /// The data table provider for language data that is read from embedded resources
    /// inside of this assembly.
    /// <para/>
    /// The data must reside in an embedded resource in a folder named <c>data\lang\</c>
    /// within the assembly, so it is resolved as <c>[assembly name].data.lang.[localeID].res</c>.
    /// </summary>
    public class ICULanguageDataTableProvider : LanguageDataTableProvider
    {
        private ICULanguageDataTableProvider()
            : base(ICUResourceBundle.IcuDataAssembly)
        { }

        /// <summary>
        /// The singleton instance of the language data table provider.
        /// </summary>
        public static ICULanguageDataTableProvider Instance => new ICULanguageDataTableProvider();
    }
}
