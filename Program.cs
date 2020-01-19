using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace MangaCreator {
    static class Program {
        
        [STAThread]
        static void Main() {
            try {

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Main());
            } catch (Exception e) {
                Console.WriteLine(e.Message);
            }
        }

    }

}