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

namespace MangaCreator {
    class Program {
        static string tmpSaveFolder = "data";
        static string mobiFolder = "mobi";

        static List<AutoResetEvent> autoResetEvents = new List<AutoResetEvent>();

        static ConcurrentQueue<string> queue;
        static void Main(string[] args) {
            try {
                if (0 == args.Length) {
                    Console.WriteLine("请输入路径");
                    return;
                }
                string[] directories = Directory.GetDirectories(args[0], "*", SearchOption.AllDirectories);

                Directory.CreateDirectory(tmpSaveFolder);
                Directory.CreateDirectory(mobiFolder);

                queue = new ConcurrentQueue<string>();

                if (directories.Count() == 0) {
                    queue.Enqueue(args[0]);
                } else {
                    foreach (string directory in directories) {
                        queue.Enqueue(directory);
                    }
                }

                // 开启两个线程工作
                for (int i = 0; i < 2; i++) {
                    AutoResetEvent autoReset = new AutoResetEvent(false);
                    new Thread(KindleGenThread).Start(autoReset);
                    autoResetEvents.Add(autoReset);
                }

                WaitHandle.WaitAll(autoResetEvents.ToArray());

                // 完成删除文件夹
                DirectoryInfo di = new DirectoryInfo(tmpSaveFolder);
                di.Delete(true);
            } catch (Exception e) {
                Console.WriteLine(e.Message);
            }
        }
        private static void KindleGenThread(Object obj) {
            int i = 1;
            try {
                while (true) {
                    string directory;
                    if (queue.IsEmpty) {
                        ((AutoResetEvent)obj).Set();
                        return;
                    }
                    queue.TryDequeue(out directory);


                    string name = Path.GetFileName(directory);
                    // 去除字符串里的所有空格
                    name = name.Replace(" ", "");

                    Generator gen = new Generator(tmpSaveFolder, name, "作者");
                    var files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories).OrderBy((s => int.Parse(Regex.Match(Path.GetFileNameWithoutExtension(s), @"\d+").Value)));

                    foreach (string file in files) {
                        gen.HtmlGenerator(file, i);
                        i++;
                    }
                    gen.OpfGenerator();
                    gen.TocGenerator();

                    var p = new Process();
                    //是否使用操作系统shell启动
                    p.StartInfo.UseShellExecute = false;
                    //输出信息
                    p.StartInfo.RedirectStandardOutput = true;
                    // 不显示程序窗口
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                    p.StartInfo.FileName = "kindlegen.exe";

                    string mobiFileName = name + ".mobi";
                    string commandLine = @"{0}\{1}\content.opf -c1 -o {2}";
                    commandLine = String.Format(commandLine, tmpSaveFolder, name, mobiFileName);
                    p.StartInfo.Arguments = commandLine;
                    p.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
                    {
                        // Prepend line numbers to each line of the output.
                        if (!String.IsNullOrEmpty(e.Data)) {
                            Console.WriteLine(e.Data);
                        }
                    });
                    p.Start();
                    p.BeginOutputReadLine();

                    p.WaitForExit();
                    p.Dispose();
                    p.Close();
                    
                    try {
                        File.Move(Path.Combine(tmpSaveFolder, name, mobiFileName), Path.Combine(mobiFolder, mobiFileName));
                    } catch (Exception e) {
                        Console.WriteLine(e.Message);
                        continue;
                    }
                }
            } catch (Exception e) {
                Console.WriteLine(e.Message);
                return;
            }


        }
    }

}