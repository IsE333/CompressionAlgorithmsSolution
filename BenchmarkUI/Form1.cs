using CompressionAlgorithms;
using CompressionAlgorithms.Common;

namespace BenchmarkUI
{
    public partial class Form1 : Form
    {
        private IAlgorithm[] algorithms = [
            new RunLengthEncoding(),
            new DeltaEncoding(),
            new HuffmanCoding(),
            new LZ77(),
            new LZW(),
            new LZWOptimized(),
            new LZWOptimized2(),
            new LZWOptimized3(),
        ];
        private readonly double[] cacheSizes = [16384, 8192, 4096, 2048, 1024, 512, 256, 128, 64];
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBoxAlgorithm2.Items.Add("-");
            foreach (var a in algorithms)
            {
                comboBoxAlgorithm.Items.Add(a.AlgorithmName);
                comboBoxAlgorithm2.Items.Add(a.AlgorithmName);
                checkedListBoxAlgorithm.Items.Add(a.AlgorithmName);
            }
            foreach (var size in cacheSizes)
                comboBoxBlockSize.Items.Add(size.ToString());
            comboBoxBlockSize.SelectedIndex = 3;
            comboBoxAlgorithm.SelectedIndex = 2;
            comboBoxAlgorithm2.SelectedIndex = 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
        }

        private void openFileDialog1_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            labelFilePath.Text = openFileDialog1.FileName;
        }

        private async void button2_ClickAsync(object sender, EventArgs e)
        {
            string filePath = openFileDialog1.FileName;
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                MessageBox.Show("Lütfen geçerli bir dosya seçin.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (comboBoxAlgorithm.SelectedIndex < 0 || comboBoxAlgorithm.SelectedIndex >= algorithms.Length)
            {
                MessageBox.Show("Lütfen geçerli bir algoritma seçin.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (comboBoxBlockSize.SelectedIndex < 0 || comboBoxBlockSize.SelectedIndex >= cacheSizes.Length)
            {
                MessageBox.Show("Lütfen geçerli bir blok boyutu seçin.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            IAlgorithm selectedAlgorithm = algorithms[comboBoxAlgorithm.SelectedIndex];
            int bufferSize = (int)(cacheSizes[comboBoxBlockSize.SelectedIndex] * 1024);

            progressBarCompression.Value = 0;
            progressBarDecompress.Value = 0;
            labelResult.Text = "";

            var progressC = new Progress<int>(percent =>
            {
                progressBarCompression.Value = percent;
            });
            var progressD = new Progress<int>(percent =>
            {
                progressBarDecompress.Value = percent;
            });

            var timer = new System.Diagnostics.Stopwatch();
            timer.Restart();

            //await FileUtility.CompressFile(selectedAlgorithm.Compress, bufferSize, filePath);
            long[] result = await Task.Run(() => FileUtility.CompressFile(selectedAlgorithm.Compress, progressC, bufferSize, filePath));
            long originalSize = result[0];
            labelResult.Text += $"Dosya boyutu:        {SizeStr(originalSize)}";

            if (comboBoxAlgorithm2.SelectedIndex != 0 && comboBoxAlgorithm2.SelectedIndex != -1)
            {
                IAlgorithm secondAlgorithm = algorithms[comboBoxAlgorithm2.SelectedIndex - 1];
                long[] intermediateResult = result;
                result = await Task.Run(() => FileUtility.CompressFile(secondAlgorithm.Compress, progressC, bufferSize, "test_compressed.bin", "test_compressed_2.bin"));
                File.Delete("test_compressed.bin");
                File.Move("test_compressed_2.bin", "test_compressed.bin");
                labelResult.Text += $"\nAra boyut:           {SizeStr(intermediateResult[1])}";
            }
                

            timer.Stop();
            progressBarCompression.Value = 100;
            long compressionTime = timer.ElapsedMilliseconds;

            labelResult.Text += $"\nSıkıştırılmış boyut: {SizeStr(result[1])}";

            labelResult.Text += $"\n\nSıkıştırma oranı:    {Math.Round(result[1] / (float)originalSize, 4)}";



            timer.Restart();
            //await FileUtility.DecompressFile(selectedAlgorithm.Decompress);
            if (comboBoxAlgorithm2.SelectedIndex != 0 && comboBoxAlgorithm2.SelectedIndex != -1)
            {
                IAlgorithm secondAlgorithm = algorithms[comboBoxAlgorithm2.SelectedIndex - 1];
                await Task.Run(() => FileUtility.DecompressFile(secondAlgorithm.Decompress, progressD, bufferSize, "test_compressed.bin", "test_decompressed.txt"));
                File.Delete("test_compressed.bin");
                File.Move("test_decompressed.txt", "test_compressed.bin");
            }
            await Task.Run(() => FileUtility.DecompressFile(selectedAlgorithm.Decompress, progressD));

            timer.Stop();
            progressBarDecompress.Value = 100;

            labelResult.Text += "\n";
            labelResult.Text += $"\nSıkıştırma süresi:   {compressionTime} ms";
            labelResult.Text += $"\nAçma süresi:         {timer.ElapsedMilliseconds} ms";

            var originalBytes = File.ReadAllBytes(filePath);
            var decompressedBytes = File.ReadAllBytes("test_decompressed.txt");
            labelResult.Text += "\n";
            labelResult.Text += $"\nKontrol sonucu :     {originalBytes.SequenceEqual(decompressedBytes)}";

        }

        public static string SizeStr(long byteCount)
        {
            string[] suf = { " B", " KB", " MB", " GB", " TB" };
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 3);
            return (Math.Sign(bytes) * num).ToString() + suf[place];
        }
    }
}
