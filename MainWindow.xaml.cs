using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace ParticleEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static string AssemblyLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        public static string UexpExtractor = $"{AssemblyLocation}{Path.DirectorySeparatorChar}extract.exe";
        public MainWindow()
        {
            InitializeComponent();
        }

        private void FileBox_DragEnter(object sender, DragEventArgs e)
        {
            e.Handled = true;
            e.Effects = DragDropEffects.Move;
        }
        public static string file;
        private void FileBox_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                Reset();
                // Get list of all files dropped
                string[] fileList = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                if (fileList.Length > 0)
                    // But just check the first one
                    if (ProcessFile(fileList[0]))
                    {
                        file = Path.ChangeExtension(fileList[0], ".uexp");
                        ParseData(file);
                        Particles.ItemsSource = ParticleNames;
                        Particles.SelectedIndex = 0;
                    }
            }
        }
        private void Reset()
        {
            // Reset all current values and boxes
            ParticleData = null;
            EnforceEquality = false;
            MaxValueBoxR.Text = String.Empty;
            MaxValueBoxG.Text = String.Empty;
            MaxValueBoxB.Text = String.Empty;
            MinValueBoxR.Text = String.Empty;
            MinValueBoxG.Text = String.Empty;
            MinValueBoxB.Text = String.Empty;
            MaxValueBoxR.IsEnabled = false;
            MaxValueBoxG.IsEnabled = false;
            MaxValueBoxB.IsEnabled = false;
            MinValueBoxR.IsEnabled = false;
            MinValueBoxG.IsEnabled = false;
            MinValueBoxB.IsEnabled = false;
            Particles.ItemsSource = null;
        }
        Map<string, int> NameTable;
        HashSet<string> ParticleNames;
        Dictionary<string, ColorOverLife> ParticleData;
        bool EnforceEquality;
        public enum Game
        {
            GGS,
            DBFZ
        }
        private bool ProcessFile(string file)
        {
            // Error checking
            var ext = Path.GetExtension(file);
            if (!ext.Equals(".uasset", StringComparison.InvariantCultureIgnoreCase) &&
                !ext.Equals(".uexp", StringComparison.InvariantCultureIgnoreCase))
            {
                MessageBox.Show("Invalid file extension");
                return false;
            }
            if ((ext.Equals(".uasset", StringComparison.InvariantCultureIgnoreCase) && !File.Exists(Path.ChangeExtension(file, ".uexp")))
                || (ext.Equals(".uexp", StringComparison.InvariantCultureIgnoreCase) && !File.Exists(Path.ChangeExtension(file, ".uasset"))))
            {
                MessageBox.Show("Both .uexp and .uasset not present");
                return false;
            }
            if (!File.Exists(UexpExtractor))
            {
                MessageBox.Show("extract.exe missing");
                return false;
            }
            // Run extract.exe on the uasset to get Tables
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.FileName = UexpExtractor;
            startInfo.Arguments = $@"-game=ue4.25 ""{Path.ChangeExtension(file, ".uasset")}""";
            startInfo.WorkingDirectory = AssemblyLocation;
            var output = $"{AssemblyLocation}{Path.DirectorySeparatorChar}{Path.GetFileNameWithoutExtension(file)}";
            if (Directory.Exists(output))
                Directory.Delete(output, true);
            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
            }
            string line;
            // Use ExportTable and regex to get all unique ParticleModuleColorOverLife names (probably not the best idea)
            ParticleNames = new();
            using (StreamReader stream = new StreamReader($"{output}{Path.DirectorySeparatorChar}ExportTable.txt"))
            {
                while ((line = stream.ReadLine()) != null)
                {
                    var reg = new Regex(@"ParticleModuleColorOverLife_\d+'");
                    var match = reg.Match(line);
                    if (match.Success)
                        ParticleNames.Add(match.Value.Replace("'", String.Empty));
                }
            }
            if (ParticleNames.Count == 0)
            {
                MessageBox.Show("uasset doesn't contain ParticleModuleColorOverLife");
                return false;
            }
            // Create a map of the names to integers
            NameTable = new();
            using (StreamReader stream = new StreamReader($"{output}{Path.DirectorySeparatorChar}NameTable.txt"))
            {
                while ((line = stream.ReadLine()) != null)
                {
                    var tokens = line.Split(" = ");
                    NameTable.Add(tokens[1].Replace(@"""", String.Empty), Int32.Parse(tokens[0]));
                }
            }
            return true;
        }
        private void ParseData(string file)
        {
            ParticleData = new();
            // Use ColorOverLife integer to find the start of each Particle
            var Pattern = new byte[] { (byte)NameTable.Forward["ColorOverLife"], 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            var fileBytes = File.ReadAllBytes(file);
            int offset = 1;
            foreach (var particle in ParticleNames)
            {
                var color = new ColorOverLife();
                // Get offset of found pattern
                var found = Search(fileBytes[offset..], Pattern);
                offset += found;
                color.offset = offset;
                var pos = offset;
                // Skip pass unneeded info
                pos += 49;
                // Loop and get data until Values are grabbed
                while (pos != -1)
                    pos = color.AddData(fileBytes, pos, NameTable, (Game)GameBox.SelectedIndex);
                ParticleData.Add(particle, color);
                offset++;
            }
        }
        
        // Sig scanning
        private static int Search(byte[] src, byte[] pattern)
        {
            int c = src.Length - pattern.Length + 1;
            int j;
            for (int i = 0; i < c; i++)
            {
                if (src[i] != pattern[0]) continue;
                for (j = pattern.Length - 1; j >= 1 && src[i + j] == pattern[j]; j--) ;
                if (j == 0) return i;
            }
            return -1;
        }

        // Get particle data from dictionary when selection is changed
        private void Particles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded && Particles.ItemsSource != null)
            {
                var selected = (sender as ComboBox).SelectedItem as string;
                var data = ParticleData[selected];
                EnforceEquality = data.EnforceEqualVectors;
                MaxValueBoxR.IsEnabled = true;
                MaxValueBoxG.IsEnabled = true;
                MaxValueBoxB.IsEnabled = true;
                MinValueBoxR.IsEnabled = true;
                MinValueBoxG.IsEnabled = true;
                MinValueBoxB.IsEnabled = true;
                MaxColorPreview.IsEnabled = true;
                MinColorPreview.IsEnabled = true;
                if (data.MaxValueVec != null)
                {
                    MaxValueBoxR.Text = data.MaxValueVec.R.ToString();
                    MaxValueBoxG.Text = data.MaxValueVec.G.ToString();
                    MaxValueBoxB.Text = data.MaxValueVec.B.ToString();
                }
                else
                {
                    MaxValueBoxR.Text = String.Empty;
                    MaxValueBoxG.Text = String.Empty;
                    MaxValueBoxB.Text = String.Empty;
                    MaxValueBoxR.IsEnabled = false;
                    MaxValueBoxG.IsEnabled = false;
                    MaxValueBoxB.IsEnabled = false;
                    ExternalColorChange = true;
                    MaxColorPreview.SelectedColor = Colors.Black;
                    ExternalColorChange = false;
                    MaxColorPreview.IsEnabled = false;
                }
                if (data.MinValueVec != null)
                {
                    MinValueBoxR.Text = data.MinValueVec.R.ToString();
                    MinValueBoxG.Text = data.MinValueVec.G.ToString();
                    MinValueBoxB.Text = data.MinValueVec.B.ToString();
                }
                else
                {
                    MinValueBoxR.Text = String.Empty;
                    MinValueBoxG.Text = String.Empty;
                    MinValueBoxB.Text = String.Empty;
                    MinValueBoxR.IsEnabled = false;
                    MinValueBoxG.IsEnabled = false;
                    MinValueBoxB.IsEnabled = false;
                    ExternalColorChange = true;
                    MinColorPreview.SelectedColor = Colors.Black;
                    ExternalColorChange = false;
                    MaxColorPreview.IsEnabled = false;
                }
                UpdateColors();
            }
        }

        bool ExternalColorChange;
        private void UpdateColors()
        {
            ExternalColorChange = true;
            float MaxR, MaxG, MaxB, MinR, MinG, MinB;
            if (Single.TryParse(MaxValueBoxR.Text, out MaxR) && Single.TryParse(MaxValueBoxG.Text, out MaxG) && Single.TryParse(MaxValueBoxB.Text, out MaxB))
                MaxColorPreview.SelectedColor = Color.FromScRgb(1.0f, MaxR, MaxG, MaxB);
            if (Single.TryParse(MinValueBoxR.Text, out MinR) && Single.TryParse(MinValueBoxG.Text, out MinG) && Single.TryParse(MinValueBoxB.Text, out MinB))
                MinColorPreview.SelectedColor = Color.FromScRgb(1.0f, MinR, MinG, MinB);
            ExternalColorChange = false;
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(file))
            {
                MessageBox.Show("Nothing to export");
                return;
            }
            // Overwrite all saved data
            var fileBytes = ExportData();

            SaveFileDialog dialog = new SaveFileDialog()
            {
                Filter = "Uasset Data(*.uexp)|*.uexp",
                InitialDirectory = Path.GetDirectoryName(file),
                FileName = Path.GetFileName(file)
            };

            if (dialog.ShowDialog() == true)
            {
                // Write to new file
                File.WriteAllBytes(dialog.FileName, fileBytes);
            }
        }
        private byte[] ExportData()
        {
            var fileBytes = File.ReadAllBytes(file);
            foreach (var particle in ParticleData.Values)
            {
                var pos = particle.offset;
                pos += 49;
                // Loop through all particles and write the info
                while (pos != -1)
                    pos = particle.OverwriteData(ref fileBytes, pos, NameTable, (Game)GameBox.SelectedIndex);
            }
            return fileBytes;
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            float MaxR, MaxG, MaxB, MinR, MinG, MinB;
            if (!Single.TryParse(MaxValueBoxR.Text, out MaxR) || !Single.TryParse(MaxValueBoxG.Text, out MaxG) || !Single.TryParse(MaxValueBoxB.Text, out MaxB)
                || !Single.TryParse(MinValueBoxR.Text, out MinR) || !Single.TryParse(MinValueBoxG.Text, out MinG) || !Single.TryParse(MinValueBoxB.Text, out MinB))
            {
                MessageBox.Show("Not all values are valid floats");
                return;
            }
            if (MinR < 0 || MinG < 0 || MinB < 0)
            {
                MessageBox.Show("All values must be nonnegative");
                return;
            }
            var selected = Particles.SelectedItem as string;
            var data = ParticleData[selected];

            data.MaxValueVec.R = MaxR;
            data.MaxValueVec.G = MaxG;
            data.MaxValueVec.B = MaxB;
            data.MinValueVec.R = MinR;
            data.MinValueVec.G = MinG;
            data.MinValueVec.B = MinB;

            if (!data.KeepMinValueZero)
                data.MinValue = Math.Min(data.MinValueVec.Min, data.MaxValueVec.Min);
            data.MaxValue = Math.Max(data.MaxValueVec.Max, data.MinValueVec.Max);
            if ((data.Values.Count == 3 && !data.NoVectors) || (data.Values.Count == 6 && data.NoMinOrMaxValue))
            {
                data.Values[0] = data.MinValueVec.R;
                data.Values[1] = data.MinValueVec.G;
                data.Values[2] = data.MinValueVec.B;
            }
            else if (data.Values.Count == 6)
            {
                data.Values[0] = data.MaxValueVec.R;
                data.Values[1] = data.MaxValueVec.G;
                data.Values[2] = data.MaxValueVec.B;
                data.Values[3] = data.MinValueVec.R;
                data.Values[4] = data.MinValueVec.G;
                data.Values[5] = data.MinValueVec.B;
            }
            ParticleData[selected] = data;
        }

        // Used for keeping the values the same when needed
        private void MaxValueBoxR_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (EnforceEquality)
                MinValueBoxR.Text = MaxValueBoxR.Text;
            UpdateColors();
        }
        private void MaxValueBoxG_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (EnforceEquality)
                MinValueBoxG.Text = MaxValueBoxG.Text;
            UpdateColors();
        }
        private void MaxValueBoxB_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (EnforceEquality)
                MinValueBoxB.Text = MaxValueBoxB.Text;
            UpdateColors();
        }
        private void MinValueBoxR_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (EnforceEquality)
                MaxValueBoxR.Text = MinValueBoxR.Text;
            UpdateColors();
        }
        private void MinValueBoxG_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (EnforceEquality)
                MaxValueBoxG.Text = MinValueBoxG.Text;
            UpdateColors();
        }
        private void MinValueBoxB_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (EnforceEquality)
                MaxValueBoxB.Text = MinValueBoxB.Text;
            UpdateColors();
        }

        private void MaxColorPreview_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (!ExternalColorChange && IsLoaded)
            {
                MaxValueBoxR.Text = MaxColorPreview.SelectedColor.Value.ScR.ToString();
                MaxValueBoxG.Text = MaxColorPreview.SelectedColor.Value.ScG.ToString();
                MaxValueBoxB.Text = MaxColorPreview.SelectedColor.Value.ScB.ToString();
            }
        }
        private void MinColorPreview_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (!ExternalColorChange && IsLoaded)
            {
                MinValueBoxR.Text = MinColorPreview.SelectedColor.Value.ScR.ToString();
                MinValueBoxG.Text = MinColorPreview.SelectedColor.Value.ScG.ToString();
                MinValueBoxB.Text = MinColorPreview.SelectedColor.Value.ScB.ToString();
            }
        }
    }
}
