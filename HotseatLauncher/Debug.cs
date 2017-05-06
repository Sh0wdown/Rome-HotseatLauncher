using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace HotseatLauncher
{
    static class Debug
    {
        public static void ShowException(Exception e)
        {
            if (e.InnerException != null)
                e = e.InnerException;

            MessageBox.Show(e.Message + "\n\n" + e.StackTrace, e.GetType().ToString());
        }

        public static void ShowWarning(string text, params string[] args)
        {
            MessageBox.Show(string.Format(text, args), "Warning");
        }
    }
}
