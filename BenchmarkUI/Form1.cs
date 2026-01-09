using CompressionAlgorithms;
using CompressionAlgorithms.Common;

namespace BenchmarkUI
{
    public partial class Form1 : Form
    {
        private readonly string[] algorithmNames =
        [
            "Run-Length Encoding",
            "Delta Encoding",
            "Huffman Coding",
            "Lempel-Ziv 77 (LZ77)",
            "Lempel-Ziv-Welch (LZW)",
        ];
        private readonly double[] cacheSizes = [0.125, 0.25, 0.5, 1, 2, 4, 8, 16];
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //listBox1.Items.AddRange(algorithmNames);
            comboBox1.Items.AddRange(algorithmNames);
            foreach (var size in cacheSizes)
                comboBox2.Items.Add(size.ToString());
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

            if (!double.TryParse(Console.ReadLine(), out double size))
                size = 0.5;
            int bufferSize = (int)(size * 1024 * 1024);

            var timer = new System.Diagnostics.Stopwatch();
            timer.Restart();
            
            
            await FileUtility.CompressFile<LZWOptimized>(bufferSize, filePath);



            timer.Stop();
            label2.Text = $"Compression Elapsed Time: {timer.ElapsedMilliseconds} ms";

            timer.Restart();
            await FileUtility.DecompressFile<LZWOptimized>();
            timer.Stop();
            label2.Text += $"\nDeCompression Elapsed Time: {timer.ElapsedMilliseconds} ms";
        }
    }
}
