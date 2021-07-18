using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;

namespace Blep.Backend
{
    /// <summary>
    /// Static class for managing search tag fields for mod entries.
    /// </summary>
    public static class TagManager
    {
        /// <summary>
        /// Deserializes tag data from a given file.
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns><c>true</c> if successful, otherwise <c>false</c>.</returns>
        public static bool ReadTagsFromFile(string filepath)
        {
            try
            {
                Wood.WriteLine($"Reading mod tags from file: {filepath}");
                string json = File.ReadAllText(filepath);
                ReadTagData(json);
                Wood.WriteLine("Mod tags loaded successfully.");
                return true;
            }
            catch (Exception e)
            {
                Wood.WriteLine($"ERROR READING TAGS FILE FROM {filepath}:");
                Wood.Indent();
                Wood.WriteLine(e);
                Wood.Unindent();
                return false;
            }
        }
        private static void ReadTagData(string json)
        {
            try
            {
                if (!string.IsNullOrEmpty(json)) TagData = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            }
            catch (Exception e)
            {
                Wood.WriteLine("ERROR PARSING TAG DATA FILE:");
                Wood.Indent();
                Wood.WriteLine(e);
                Wood.Unindent();
            }
            
        }

        private static Dictionary<string, string> TagData { get { _td = _td ?? new Dictionary<string, string>(); return _td; } set => _td = value; }
        private static Dictionary<string, string> _td;

        /// <summary>
        /// Gets JSON for modtags.
        /// </summary>
        /// <returns>JSON to be saved.</returns>
        private static string GetTDToSave()
        {
            return JsonConvert.SerializeObject(TagData, Formatting.Indented);
        }
        public static bool SaveToFile(string filepath)
        {
            try
            {
                Wood.WriteLine($"Saving tag data to file: {filepath}...");
                File.WriteAllText(filepath, GetTDToSave());
                Wood.WriteLine("Tag data saved successfully.");
                return true;
            }
            catch (Exception e)
            {
                Wood.WriteLine($"ERROR WRITING TAGS FILE TO {filepath}:");
                Wood.Indent();
                Wood.WriteLine(e);
                Wood.Unindent();
                return false;
            }
        }

        /// <summary>
        /// Sets tag data for a specified modname
        /// </summary>
        /// <param name="modname"></param>
        /// <param name="tagText"></param>
        public static void SetTagData(string modname, string tagText)
        {
            if (!TagData.ContainsKey(modname)) TagData.Add(modname, tagText);
            else TagData[modname] = tagText;
        }
        public static string GetTagString(string modname)
        {
            try
            {
                if (!TagData.ContainsKey(modname)) return string.Empty;
                return TagData[modname];
            }
            catch (ArgumentNullException)
            {
                return string.Empty;
            }
        }
        public static string[] GetTagsArray(string modname)
        {
            return System.Text.RegularExpressions.Regex.Split(GetTagString(modname), ", |\n|,", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
        public static void TagCleanup(string[] modnames)
        {
            Dictionary<string, string> ndic = new Dictionary<string, string>();
            foreach (string mn in modnames)
            {
                if (TagData.ContainsKey(mn) && !ndic.ContainsKey(mn)) ndic.Add(mn, TagData[mn]);
            }
            TagData = ndic;
        }
    }
}
