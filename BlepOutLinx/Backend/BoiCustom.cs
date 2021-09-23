﻿using System.IO;

namespace Blep.Backend
{
    public static class BoiCustom
    {
        /// <summary>
        /// Compares two byte arrays.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns><c>true</c> if arrays are identical; <c>false</c> otherwise.</returns>
        public static bool BOIC_Bytearr_Compare(byte[] a, byte[] b)
        {
            if (a == null || b == null) return false;
            if (a.Length == 0 || b.Length == 0) return false;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }
        /// <summary>
        /// Recursively moves a directory and all its contents into a new location.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns>Number of IO errors encountered during the operation. </returns>
        public static int BOIC_RecursiveDirectoryCopy(string from, string to)
        {
            int errc = 0;
            DirectoryInfo din = new DirectoryInfo(from);
            DirectoryInfo dout = new DirectoryInfo(to);
            if (!din.Exists) { throw new IOException($"An attempt to copy a nonexistent directory ({from}) to {to} has occured."); }
            if (!dout.Exists) Directory.CreateDirectory(to);
            foreach (FileInfo fi in din.GetFiles())
            {
                try { File.Copy(fi.FullName, Path.Combine(to, fi.Name)); }
                catch (IOException ioe)
                {
                    Wood.Write("Could not copy a file during recursive copy process");
                    Wood.Indent();
                    Wood.WriteLine(ioe);
                    Wood.Unindent();
                    errc++;
                }

            }
            foreach (DirectoryInfo di in din.GetDirectories())
            {
                try { errc += BOIC_RecursiveDirectoryCopy(di.FullName, Path.Combine(to, di.Name)); }
                catch (IOException ioe)
                {
                    Wood.Write("Could not copy a subfolder during recursive copy process");
                    Wood.Indent();
                    Wood.WriteLine(ioe);
                    Wood.Unindent();

                }

            }
            return errc;
        }
    }

    
}
