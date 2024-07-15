using System.Security.Cryptography;
using System.Text;

namespace FilesRegister
{
    public partial class Form1 : Form
    {
        class FileEntry
        {
            public string Hash { get; set; }
            public string FilePath { get; set; }
            public string FileName { get; set; }

            public static FileEntry Parse(string line)
            {
                var parts = line.Split(":", StringSplitOptions.RemoveEmptyEntries);

                return new FileEntry() { Hash = parts[0], FileName = parts[1] };
            }

            public override string ToString()
            {
                return Hash + ":" + FileName;
            }
        }


        readonly Dictionary<string, FileEntry> _register = new Dictionary<string, FileEntry>();

        readonly List<FileEntry> _tempList = new List<FileEntry>();

        public Form1()
        {
            InitializeComponent();
        }

        private static IEnumerable<string> GetFiles(string fileOrDirectory)
        {
            if (File.Exists(fileOrDirectory))
            {
                yield return fileOrDirectory;
                yield break;
            }

            foreach (var dir in Directory.GetDirectories(fileOrDirectory))
            {
                foreach (var file in GetFiles(dir))
                    yield return file;
            }

            foreach (var file in Directory.GetFiles(fileOrDirectory))
            {
                yield return file;
            }
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] filesOrDirs = (string[])e.Data.GetData(DataFormats.FileDrop);

            var files = new List<string>();

            foreach (string fileOrDir in filesOrDirs)
            {
                files.AddRange(GetFiles(fileOrDir));
            }

            _tempList.Clear();
            textBox1.Clear();

            textBox1.AppendText("=== BEGIN ===\r\n");

            backgroundWorker1.RunWorkerAsync(files.ToArray());
        }

        private void button2_Click(object sender, EventArgs e)
        {
            foreach (var fileEntry in _tempList)
            {
                if (_register.ContainsKey(fileEntry.Hash))
                    continue;

                _register.Add(fileEntry.Hash, fileEntry);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (DialogResult.OK != openFileDialog1.ShowDialog())
                return;

            var lines = File.ReadAllLines(openFileDialog1.FileName, Encoding.UTF8);

            foreach (var line in lines)
            {
                var fileEntry = FileEntry.Parse(line);
                _register.Add(fileEntry.Hash, fileEntry);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (DialogResult.OK != saveFileDialog1.ShowDialog())
                return;

            List<string> lines = new List<string>();

            foreach (var fileEntry in _register.Values.OrderBy(v => v.FileName))
            {
                lines.Add(fileEntry.ToString());
            }

            File.WriteAllLines(saveFileDialog1.FileName, lines, Encoding.UTF8);
        }

        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            var files = (string[])e.Argument;

            int i = 0;

            foreach (var file in files)
            {
                string hashString;

                using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Write))
                {
                    var hash = new SHA1Managed().ComputeHash(fs);
                    hashString = BitConverter.ToString(hash).Replace("-", "");

                    fs.Close();
                }

                if (_register.ContainsKey(hashString))
                {
                    backgroundWorker1.ReportProgress(i++, "+");
                    continue;
                }

                _tempList.Add(new FileEntry { FileName = Path.GetFileName(file), FilePath = file, Hash = hashString });
                backgroundWorker1.ReportProgress(i++, file);
            }

        }

        private void backgroundWorker1_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            var file = (string)e.UserState;

            textBox1.AppendText(e.ProgressPercentage + " " + file + "\r\n");
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            if (null != e.Error)
                textBox1.AppendText(e.Error.Message);

            textBox1.AppendText("=== " + _tempList.Count + " ===\r\n");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (DialogResult.OK != folderBrowserDialog1.ShowDialog())
                return;

            foreach (var fe in _tempList)
            {
                var fileName = fe.FilePath.Replace(textBox2.Text, string.Empty);

                if (fileName.StartsWith("\\"))
                    fileName = fileName.Remove(0, 1);

                fileName = Path.Combine(folderBrowserDialog1.SelectedPath, fileName);

                var dirName = Path.GetDirectoryName(fileName);

                if (!string.IsNullOrEmpty(dirName) && !Directory.Exists(dirName))
                    Directory.CreateDirectory(dirName);

                File.Copy(fe.FilePath, fileName);
            }
        }
    }
}