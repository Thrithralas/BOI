using System;
using System.Windows.Forms;

namespace Blep
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Console.WriteLine("Reminder: you can always select text in console and then copy it by pressing enter. It also pauses the app.\n");
            BlepOut Currblep = new BlepOut();
            Application.Run(Currblep);
            
        }
    }
}
