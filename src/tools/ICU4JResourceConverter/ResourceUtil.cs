﻿using ICU4N.Globalization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace JavaResourceConverter
{
    /// <summary>
    /// Utilities for building .NET resources.
    /// </summary>
    public static class ResourceUtil
    {
        public const string LocaleListFileName = "fullLocaleNames.lst";
        public const string InvariantResourcesDirectoryName = "invariantResources";
        public const string DataDirectoryName = "data";

        public static readonly string[] SupportedFeatures = new string[] {
            "brkitr",
            "coll", 
            "curr", // Do we need these?
            "lang",
            //"rbnf",
            "region",
            "translit",
            //"unit",
            //"zone",
        };

        private const string UnsupportedDotNet6LocalesFileName = "locales-unsupported-by-dotnet-6.txt";
        public static readonly string[] UnsupportedDotNet6Locales = LoadUnsupportedDotNet6Locales();

        private static string[] LoadUnsupportedDotNet6Locales()
        {
            var thisType = typeof(ResourceUtil);
            using var stream = thisType.Assembly.GetManifestResourceStream("ICU4JResourceConverter." + UnsupportedDotNet6LocalesFileName);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var temp = new List<string>();
            string line = null;
            while ((line = reader.ReadLine()) != null)
                temp.Add(line.Trim());
            return temp.ToArray();
        }


        public static void TransformResources(string dataPath, string outputDirectory)
        {
            // locales (from root data directory)
            TransformFeature(string.Empty, dataPath, outputDirectory);

            // Features
            foreach (var dir in Directory.GetDirectories(dataPath, "*.*", SearchOption.TopDirectoryOnly))
            {
                string featureName = new DirectoryInfo(dir).Name;

                // Exclude features ICU4N doesn't support
                if (!SupportedFeatures.Contains(featureName))
                    continue;

                TransformFeature(featureName, dir, outputDirectory);
            }
        }

        public static void TransformFeature(string featureName, string featurePath, string outputDirectory)
        {
            var localeList = LoadLocaleList(featurePath);

            foreach (var filePath in Directory.GetFiles(featurePath, "*.*", SearchOption.TopDirectoryOnly))
            {
                //string fileName = Path.GetFileName(filePath);
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
                //string fileExtension = Path.GetExtension(filePath);
                if (localeList.Contains(fileNameWithoutExtension))
                {
                    string icuLocaleName = fileNameWithoutExtension;

                    // ICU4N TODO: These are gaps we need to handle, but for now we skip them
                    // for a PoC.
                    if (UnsupportedDotNet6Locales.Contains(icuLocaleName))
                        continue;

                    if (icuLocaleName.Equals("root"))
                        TransformInvariantFeature(filePath, featureName, outputDirectory);
                    else
                        TransformLocalizedFeature(filePath, featureName, icuLocaleName, outputDirectory);
                }
                //else if (LocaleListFileName.Equals(Path.GetFileName(filePath)))
                //{
                //    PackInvariantFeature(filePath, featureName, outputDirectory);
                //}
                else
                {
                    // For now, we pack everything else as invariant.
                    // ICU4N TODO: Need to test other scenarios such as putting breakiterator dictionaries into
                    // localized resources and other such cases. But we will need to inventory special cases in
                    // manifests to provide ICU4N with a speedy way to decide where to look.
                    TransformInvariantFeature(filePath, featureName, outputDirectory);
                }
            }
        }

        public static void TransformLocalizedFeature(string filePath, string featureName, string icuLocaleName, string outputDirectory)
        {
            string fileName = Path.GetFileName(filePath);

            // ICU4N TODO: We may need some special cases here to fallback to if the locale name doesn't comply with RFC1766: https://datatracker.ietf.org/doc/rfc1766/
            // Should special cases exist, we need to catch that situation and pack them in the corresponding neutral language.
            // We also need to create a manifest of these special cases to give ICU4N a speedy way to decide where to look.
            string dotnetLocaleName = GetDotNetLocaleName(icuLocaleName);

            string outFileName = GetFeatureFileName(featureName, fileName);
            string outDirectoryName = Path.Combine(outputDirectory, dotnetLocaleName);
            string outFilePath = Path.Combine(outDirectoryName, outFileName);

            Directory.CreateDirectory(outDirectoryName);
            File.Copy(filePath, outFilePath, overwrite: true);
            //using var input = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            //using var output = new FileStream(outFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
            //input.CopyTo(output);
        }

        public static void TransformInvariantFeature(string filePath, string featureName, string outputDirectory)
        {
            string fileName = Path.GetFileName(filePath);
            string outFileName = GetFeatureFileName(featureName, fileName);
            string outFilePath = Path.Combine(outputDirectory, outFileName);

            Directory.CreateDirectory(outputDirectory);
            File.Copy(filePath, outFilePath, overwrite: true);
            //using var input = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            //using var output = new FileStream(outFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
            //input.CopyTo(output);
        }

        private static string GetFeatureFileName(string featureName, string fileName)
        {
            return featureName == string.Empty ? // locale names don't have a feature name
                string.Concat(DataDirectoryName, ".", fileName) :
                string.Concat(DataDirectoryName, ".", featureName, ".", fileName);
        }

        // These were created to use ResourceManager, which requires things to be in .resources files. However, it will be cleaner to simply
        // rename and embed the raw binaries from ICU4J and access them through Assembly.GetSatelliteAssembly(CultureInfo).GetManifestResourceStream(<renamed filename>);
        // We also don't need to worry about giving the resource a name to identify it from the resource table.
        //public static void PackLocalizedFeature(string filePath, string featureName, string icuLocaleName, string outputDirectory)
        //{
        //    string dotnetLocaleName = GetDotNetLocaleName(icuLocaleName);

        //    string outFileName = featureName + (dotnetLocaleName == "" ? "" : "." + dotnetLocaleName) + ".resources";
        //    string outDirectoryName = Path.Combine(outputDirectory, dotnetLocaleName);
        //    string outFilePath = Path.Combine(outDirectoryName, outFileName);

        //    CreateResourceFile(featureName, filePath, outFilePath);
        //}


        //public static void CreateResourceFile(string featureName, string inputFilePath, string outputFilePath)
        //{
        //    Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));

        //    using var stream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        //    using var writer = new ResourceWriter(outputFilePath);

        //    writer.AddResource(featureName, stream); // TODO: Should we just use "data" or "resource", since it is the only one in the file?
        //}

        public static string GetDotNetLocaleName(string baseName)
        {
            return baseName == "root" ? "" : new LocaleIDParser(baseName).GetName();
        }

        /// <summary>
        /// Loads the list of locale names that are supported for a given feature.
        /// </summary>
        /// <param name="dataPath">The directory of the locale list.</param>
        /// <param name="localeListFileName">The file name of the locale list. Defaults to 'fullLocaleNames.lst'.</param>
        /// <returns>A <see cref="HashSet{T}"/> containing the locale list.</returns>
        public static ISet<string> LoadLocaleList(string dataPath, string localeListFileName = LocaleListFileName)
        {
            var result = new HashSet<string>();
            using var reader = new StreamReader(Path.Combine(dataPath, localeListFileName), Encoding.UTF8);

            string line;
            while ((line = reader.ReadLine()) != null)
                result.Add(line.Trim());

            return result;
        }
    }
}
