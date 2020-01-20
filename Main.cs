using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MangaCreator {
    public partial class Main : Form {
        static string tmpSaveFolder = "data";
        static string mobiFolder = "mobi";

        private delegate void SafeCallDelegate(string text);

        private static bool ifStop = false;

        private static Thread genThread;

        public Main() {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e) {
            if (DialogResult.OK == this.folderBrowserDialog1.ShowDialog()) {
                this.textBox1.Text = this.folderBrowserDialog1.SelectedPath;
            }
        }

        private void Work(string path) {
            try {
                string[] directories = Directory.GetDirectories(path, "*", SearchOption.AllDirectories);

                Directory.CreateDirectory(tmpSaveFolder);
                Directory.CreateDirectory(mobiFolder);

                List<string> directoryList = new List<string>();
                if (directories.Count() == 0) {
                    directoryList.Add(path);
                } else {
                    directoryList = directories.OrderBy(s => int.Parse(Regex.Match(Path.GetFileName(s), @"\d+").Value)).ToList();
                }
                // 开始生成
                ifStop = false;
                genThread = new Thread(KindleGen);
                genThread.Start(directoryList);
            } catch (Exception e) {
                MessageBox.Show(e.Message);
            }
        }

        private void KindleGen(Object directoryList) {
            try {
                int count = 1;
                int max_count = 1;
                if (!string.IsNullOrEmpty(this.textBox3.Text)) {
                    if (!int.TryParse(this.textBox3.Text, out max_count)) {
                        MessageBox.Show("请在合并一栏输入数字");
                    }
                }

                List<string> files = new List<string>();
                string name = string.Empty;
                for (int k = 0; k < ((List<string>)directoryList).Count; k++) {
                    if (ifStop) {
                        return;
                    }

                    string directory = ((List<string>)directoryList)[k];
                    WriteTextSafe(string.Format("开始转换：{0}", directory));

                    files = files.Concat(Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories).OrderBy((s => int.Parse(Regex.Match(Path.GetFileNameWithoutExtension(s), @"\d+").Value))).ToList()).ToList();

                    if (1 == count) {
                        name = Path.GetFileName(directory);
                    }
                    if (count < max_count && k != ((List<string>)directoryList).Count - 1) {
                        count++;
                        continue;
                    } else {
                        if (1 != count) {
                            name += "-" + Path.GetFileName(directory);
                        }
                        count = 1;
                    }

                    // 去除字符串里的所有空格
                    name = name.Replace(" ", "");

                    Generator gen = new Generator(tmpSaveFolder, name, "作者");

                    for (int i = 0; i < files.Count; i++) {
                        gen.HtmlGenerator(files[i], i);
                    }

                    gen.OpfGenerator();
                    gen.TocGenerator();

                    files.Clear();

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
                    p.OutputDataReceived += new DataReceivedEventHandler((sender, e) => {
                        WriteTextSafe(e.Data);
                    });
                    p.Start();
                    p.BeginOutputReadLine();

                    p.WaitForExit();
                    p.Dispose();
                    p.Close();

                    try {
                        File.Copy(Path.Combine(tmpSaveFolder, name, mobiFileName), Path.Combine(mobiFolder, mobiFileName), true);
                        WriteTextSafe(string.Format("创建成功：{0}", Path.Combine(mobiFolder, mobiFileName)));
                        DirectoryInfo di = new DirectoryInfo(Path.Combine(tmpSaveFolder, name));
                        di.Delete(true);
                    } catch (Exception e) {
                        Console.WriteLine(e.Message);
                        continue;
                    }
                }
            } catch (Exception e) {
                MessageBox.Show(e.Message);
                return;
            }
        }

        private void WriteTextSafe(string msg) {
            try {
                if (!String.IsNullOrEmpty(msg)) {
                    if (textBox2.InvokeRequired) {
                        SafeCallDelegate d = new SafeCallDelegate(WriteTextSafe);
                        textBox2.Invoke(d, new object[] { msg });
                    } else {
                        textBox2.AppendText(msg);
                        textBox2.AppendText(Environment.NewLine);
                        textBox2.ScrollToCaret();
                    }
                }
            } catch (Exception e) {
                MessageBox.Show(e.Message);
            }
        }

        private void button2_Click(object sender, EventArgs e) {
            if (!string.IsNullOrEmpty(this.textBox1.Text)) {
                Work(this.textBox1.Text);
            }
        }

        private void button3_Click(object sender, EventArgs e) {
            if (DialogResult.Yes == MessageBox.Show("是否确认停止生成？程序将会在生成当前mobi结束后停止", "警告", MessageBoxButtons.YesNo)) {
                ifStop = true;
            }
        }

        [Obsolete]
        private void button4_Click(object sender, EventArgs e) {
            try {
                if (genThread != null && genThread.ThreadState != System.Threading.ThreadState.Stopped) {
                    if (genThread != null && genThread.ThreadState != System.Threading.ThreadState.Suspended) {
                        genThread.Suspend();
                    } else {
                        genThread.Resume();
                    }
                }
            } catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
