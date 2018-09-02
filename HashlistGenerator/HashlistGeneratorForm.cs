using HashlistGenerator.HashProcessing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HashlistGenerator
{
    public partial class HashlistGeneratorForm : Form
    {
        private string m_dirSamples;
        private string m_badSamples;

        public HashlistGeneratorForm()
        {
            InitializeComponent();
            m_dirSamples = Path.GetFullPath(@".\ban");
            m_badSamples = Path.GetFullPath(@".\badSamples\");
        }

        private void Generate(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Would you like to generate based on an existing file?", "Question", MessageBoxButtons.YesNoCancel);
            if (result == DialogResult.Yes)
            {
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    var jsonFile = openFileDialog1.FileName;
                    var sourceContent = File.ReadAllText(jsonFile);
                    var heroInfo = JsonConvert.DeserializeObject<List<EachHero>>(sourceContent);

                    ValidateRecursive(heroInfo);
                }
            }
            else if (result == DialogResult.No)
            {
                var heroInfo = new List<EachHero>();
                ValidateRecursive(heroInfo);
            }
        }

        private void Simplify(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                var jsonFile = openFileDialog1.FileName;
                var sourceContent = File.ReadAllText(jsonFile);
                var heroInfo = JsonConvert.DeserializeObject<List<EachHero>>(sourceContent);

                Task.Run(() => Simplify(heroInfo));
            }
        }

        private void Simplify(List<EachHero> heroInfo)
        {
            EnableAllButtons(false);
            var totalCount = heroInfo.Where(h => h.DHash.Count > 10).SelectMany(e => e.DHash).Count();
            var totalFiles = Directory.EnumerateFiles(m_dirSamples, "*.*", SearchOption.AllDirectories).Count();
            int count = 0;
            int processed = 0;

            foreach (var eachHero in heroInfo.Where(h => h.DHash.Count > 10))
            {
                foreach (var hash in eachHero.DHash.ToList())
                {
                    processed++;
                    if (eachHero.DHash.Count <= 10)
                        break;

                    eachHero.DHash.Remove(hash);
                    if (!Validate(heroInfo, count, processed, totalCount, totalFiles))
                        eachHero.DHash.Add(hash);
                    else
                        count++;
                }
            }

            PrintLabel(count + " / " + processed + " / " + totalCount);
            File.WriteAllText(@".\simplifiedHashlist.json", JsonConvert.SerializeObject(heroInfo));
            EnableAllButtons(true);
        }

        private void Check(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if (Directory.Exists(m_badSamples))
                    Directory.Delete(m_badSamples, true);

                Directory.CreateDirectory(m_badSamples);

                ResultLabel.Text = string.Empty;
                var jsonFile = openFileDialog1.FileName;
                var sourceContent = File.ReadAllText(jsonFile);
                var heroInfo = JsonConvert.DeserializeObject<List<EachHero>>(sourceContent);

                Task.Run(() => Check(heroInfo));
            }
        }

        public void Check(List<EachHero> heroInfo)
        {
            EnableAllButtons(false);
            var totalCount = Directory.EnumerateFiles(m_dirSamples, "*.*", SearchOption.AllDirectories).Count();
            int correct = 0;
            var directories = Directory.GetDirectories(m_dirSamples);
            foreach (var heroDir in directories)
            {
                var dirName = heroDir.Substring(heroDir.LastIndexOf('\\') + 1);
                foreach (var file in Directory.GetFiles(heroDir))
                {
                    using (var bitmap = new Bitmap(file))
                    {
                        var result = HeroIdentifier.Identify(bitmap, heroInfo);
                        if (result.Item1.ToString() == dirName)
                            correct++;
                        else
                        {
                            if (!Directory.Exists(m_badSamples + dirName))
                                Directory.CreateDirectory(m_badSamples + dirName);

                            File.Copy(file, m_badSamples + dirName + @"\" + file.Substring(file.LastIndexOf('\\') + 1));
                        }

                        PrintLabel(correct + " / " + totalCount);
                    }
                }
            }

            EnableAllButtons(true);
        }

        public bool Validate(List<EachHero> heroInfo, int count, int processed, int totalCount, int totalFileCount)
        {
            int fileCount = 0;
            var directories = Directory.GetDirectories(m_dirSamples);
            foreach (var heroDir in directories)
            {
                var dirName = heroDir.Substring(heroDir.LastIndexOf('\\') + 1);
                foreach (var file in Directory.GetFiles(heroDir))
                {
                    fileCount++;

                    PrintLabel(count + " / " + processed + " / " + totalCount + "    " + fileCount + " / " + totalFileCount);
                    using (var bitmap = new Bitmap(file))
                    {
                        var result = HeroIdentifier.Identify(bitmap, heroInfo);
                        if (result.Item1.ToString() != dirName)
                            return false;
                    }
                }
            }

            return true;
        }

        private void ValidateRecursive(List<EachHero> heroInfo)
        {
            Task.Run(() => Processing(heroInfo));
        }

        public void Processing(List<EachHero> heroInfo)
        {
            EnableAllButtons(false);
            var fautyFiles = new HashSet<string>();
            var directories = Directory.GetDirectories(m_dirSamples);
            bool done = true;
            var totalCount = Directory.EnumerateFiles(m_dirSamples, "*.*", SearchOption.AllDirectories).Count();
            var totalPassed = 0;
            do
            {
                var passed = 0;
                var processed = 0;
                bool freezePassed = false;
                done = true;
                foreach (var heroDir in directories)
                {
                    var dirName = heroDir.Substring(heroDir.LastIndexOf('\\') + 1);
                    foreach (var file in Directory.GetFiles(heroDir))
                    {
                        processed++;
                        PrintLabel(totalPassed + " / " + processed + " / " + totalCount);
                        using (var bitmap = new Bitmap(file))
                        {
                            var result = HeroIdentifier.Identify(bitmap, heroInfo);
                            if (result.Item1.ToString() == dirName)
                            {
                                var resultHero = heroInfo.First(l => l.Id.ToString() == dirName);
                                if (result.Item2 > 3 && resultHero.DHash.Count < 10)
                                {
                                    var hash = HeroIdentifier.GetHash(bitmap);
                                    if (!resultHero.DHash.Contains(hash))
                                        resultHero.DHash.Add(hash);
                                }

                                if (!freezePassed)
                                {
                                    passed++;
                                    if (passed > totalPassed)
                                        totalPassed = passed;

                                    PrintLabel(totalPassed + " / " + processed + " / " + totalCount);
                                }

                                continue;
                            }

                            freezePassed = true;

                            if (result.Item1 != 0 && result.Item2 < 5)
                            {
                                fautyFiles.Add(result.Item1 + " : " + file);
                                if (result.Item2 == 0)
                                    continue;
                            }

                            var eachHero = heroInfo.FirstOrDefault(l => l.Id.ToString() == dirName);
                            if (eachHero == null)
                            {
                                eachHero = new EachHero() { Id = int.Parse(dirName) };
                                heroInfo.Add(eachHero);
                            }

                            eachHero.DHash.Add(HeroIdentifier.GetHash(bitmap));

                            done = false;
                        }
                    }
                }
            }
            while (!done);

            File.WriteAllText(@".\FautyFile.log", string.Join(Environment.NewLine, fautyFiles));

            File.WriteAllText(@".\newHashlist.json", JsonConvert.SerializeObject(heroInfo));

            EnableAllButtons(true);
        }

        public void EnableAllButtons(bool enabled)
        {
            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)delegate () { GenerateButton.Enabled = button1.Enabled = button2.Enabled = enabled; });
            }
            else
                GenerateButton.Enabled = button1.Enabled = button2.Enabled = enabled;
        }

        public void PrintLabel(string label)
        {
            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)delegate () { ResultLabel.Text = label; });
            }
            else
                ResultLabel.Text = label;
        }
    }
}
