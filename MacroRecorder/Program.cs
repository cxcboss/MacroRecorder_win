using System;
using System.IO;
using System.Runtime.InteropServices;

namespace MacroRecorder
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
    }
}
