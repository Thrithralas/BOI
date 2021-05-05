using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace Blep.Backend
{
    /// <summary>
    /// Replacement for <see cref="Debug"/> because <see cref="Debug"/> doesn't work on release build
    /// </summary>
    public static class Wood
    {
        public static void Indent()
        {
            IndentLevel++;
        }
        public static void Unindent()
        {
            IndentLevel--;
        }
        public static void WriteLineIf(bool cond, object o)
        {
            if (cond) WriteLine(o);
        }
        public static void WriteLine(object o, int AddedIndent)
        {
            IndentLevel += AddedIndent;
            WriteLine(o);
            IndentLevel -= AddedIndent;
        }
        public static void WriteLine(object o)
        {
            string result = string.Empty;
            for (int i = 0; i < IndentLevel; i++) { result += "\t"; }
            result += o?.ToString() ?? "null";
            result += "\n";
            Write(result);
        }
        public static void WriteLine()
        {
            WriteLine(string.Empty);
        }
        public static void Write(object o)
        {
            if (LogPath == null || !File.Exists(LogPath) || !LogPath.EndsWith(".txt"))
            {
                LogPath = Path.Combine(Directory.GetCurrentDirectory(), "BOILOG.txt");
            }

            try
            {
                int ml = 512;
                while (WriteQueue.Count > 0 && ml > 0) 
                {
                    ml--;
                    RawWrite(WriteQueue[0]);
                    WriteQueue.RemoveAt(0);
                }
            }
            catch (IOException) { };
            FileInfo lf = new FileInfo(LogPath);
            try
            {
                RawWrite(o);
            }
            catch (IOException)
            {
                WriteQueue.Add(o);
            }
        }
        private static void RawWrite(object o)
        {
            Debug.Write(o);
            string result = o?.ToString() ?? "null";
            File.AppendAllText(LogPath, result);
        }
        public static void SetNewPathAndErase(string tar)
        {
            LogPath = tar;
            if (File.Exists(tar)) File.Delete(tar);
        }

        public static List<object> WriteQueue { get { _wc = _wc ?? new List<object>(); return _wc; } set { _wc = value; } }
        private static List<Object> _wc;
        public static string LogPath { get; set; } = string.Empty;
        

        public static int IndentLevel { get { return _il; } set { _il = Math.Max(value, 0); } }
        private static int _il = 0;
    }
}
