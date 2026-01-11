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
        private readonly double[] cacheSizes = [0.125, 0.25, 0.5, 1, 2, 4, 8, 16];
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            foreach (var a in algorithms)
                comboBox1.Items.Add(a.AlgorithmName);
            foreach (var size in cacheSizes)
                comboBox2.Items.Add(size.ToString());
            comboBox1.SelectedIndex = 2;
            comboBox2.SelectedIndex = 3;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
        }

        private void openFileDialog1_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            label1.Text = openFileDialog1.FileName;
        }

        private async void button2_ClickAsync(object sender, EventArgs e)
        {
            string filePath = openFileDialog1.FileName;
            IAlgorithm selectedAlgorithm = algorithms[comboBox1.SelectedIndex];
            int bufferSize = (int)(cacheSizes[comboBox2.SelectedIndex] * 1024 * 1024);

            progressBarCompression.Value = 0;
            progressBarDecompress.Value = 0;
            label2.Text = "";

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
            await Task.Run(() => FileUtility.CompressFile(selectedAlgorithm.Compress, progressC, bufferSize, filePath));
            timer.Stop();
            label2.Text += $"Original File Size:        {new FileInfo(filePath).Length} bytes";
            label2.Text += "\n";
            label2.Text += $"\nCompression Elapsed Time:  {timer.ElapsedMilliseconds} ms";
            label2.Text += $"\nCompressed File Size:      {new FileInfo("test_compressed.bin").Length} bytes";


            timer.Restart();
            //await FileUtility.DecompressFile(selectedAlgorithm.Decompress);
            await Task.Run(() => FileUtility.DecompressFile(selectedAlgorithm.Decompress, progressD));
            timer.Stop();
            label2.Text += "\n";
            label2.Text += $"\nDecompression Elapsed Time: {timer.ElapsedMilliseconds} ms";

            var originalBytes = File.ReadAllBytes(filePath);
            var decompressedBytes = File.ReadAllBytes("test_decompressed.txt");
            label2.Text += "\n";
            label2.Text += $"\nFiles are identical:    {originalBytes.SequenceEqual(decompressedBytes)}";

        }
    }
}
