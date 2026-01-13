namespace BenchmarkUI
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            labelFilePath = new Label();
            button1 = new Button();
            openFileDialog1 = new OpenFileDialog();
            button2 = new Button();
            labelResult = new Label();
            comboBoxAlgorithm = new ComboBox();
            comboBoxBlockSize = new ComboBox();
            progressBarCompression = new ProgressBar();
            progressBarDecompress = new ProgressBar();
            label3 = new Label();
            label4 = new Label();
            checkedListBoxAlgorithm = new CheckedListBox();
            label5 = new Label();
            comboBoxAlgorithm2 = new ComboBox();
            SuspendLayout();
            // 
            // labelFilePath
            // 
            labelFilePath.AutoSize = true;
            labelFilePath.Location = new Point(12, 9);
            labelFilePath.Name = "labelFilePath";
            labelFilePath.Size = new Size(98, 20);
            labelFilePath.TabIndex = 0;
            labelFilePath.Text = "Dosya seçiniz";
            // 
            // button1
            // 
            button1.Location = new Point(12, 32);
            button1.Name = "button1";
            button1.Size = new Size(261, 48);
            button1.TabIndex = 2;
            button1.Text = "Dosya Seç";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog1";
            openFileDialog1.FileOk += openFileDialog1_FileOk;
            // 
            // button2
            // 
            button2.Location = new Point(279, 32);
            button2.Name = "button2";
            button2.Size = new Size(164, 48);
            button2.TabIndex = 3;
            button2.Text = "Dosyayı Sıkıştır";
            button2.UseVisualStyleBackColor = false;
            button2.Click += button2_ClickAsync;
            // 
            // labelResult
            // 
            labelResult.AutoSize = true;
            labelResult.Font = new Font("Consolas", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            labelResult.Location = new Point(449, 32);
            labelResult.Name = "labelResult";
            labelResult.Size = new Size(65, 23);
            labelResult.TabIndex = 4;
            labelResult.Text = "Sonuç";
            // 
            // comboBoxAlgorithm
            // 
            comboBoxAlgorithm.FormattingEnabled = true;
            comboBoxAlgorithm.Location = new Point(97, 88);
            comboBoxAlgorithm.Name = "comboBoxAlgorithm";
            comboBoxAlgorithm.Size = new Size(176, 28);
            comboBoxAlgorithm.TabIndex = 5;
            // 
            // comboBoxBlockSize
            // 
            comboBoxBlockSize.FormattingEnabled = true;
            comboBoxBlockSize.Location = new Point(163, 122);
            comboBoxBlockSize.Name = "comboBoxBlockSize";
            comboBoxBlockSize.Size = new Size(110, 28);
            comboBoxBlockSize.TabIndex = 6;
            // 
            // progressBarCompression
            // 
            progressBarCompression.Location = new Point(279, 88);
            progressBarCompression.Name = "progressBarCompression";
            progressBarCompression.Size = new Size(164, 29);
            progressBarCompression.TabIndex = 7;
            // 
            // progressBarDecompress
            // 
            progressBarDecompress.Location = new Point(279, 122);
            progressBarDecompress.Name = "progressBarDecompress";
            progressBarDecompress.Size = new Size(164, 29);
            progressBarDecompress.TabIndex = 8;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 126);
            label3.Name = "label3";
            label3.Size = new Size(145, 20);
            label3.TabIndex = 9;
            label3.Text = "Blok büyüklüğü (KB):";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(12, 92);
            label4.Name = "label4";
            label4.Size = new Size(79, 20);
            label4.TabIndex = 10;
            label4.Text = "Algoritma:";
            // 
            // checkedListBoxAlgorithm
            // 
            checkedListBoxAlgorithm.FormattingEnabled = true;
            checkedListBoxAlgorithm.Location = new Point(12, 242);
            checkedListBoxAlgorithm.Name = "checkedListBoxAlgorithm";
            checkedListBoxAlgorithm.Size = new Size(162, 180);
            checkedListBoxAlgorithm.TabIndex = 11;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(12, 159);
            label5.Name = "label5";
            label5.Size = new Size(91, 20);
            label5.TabIndex = 13;
            label5.Text = "Algoritma 2:";
            // 
            // comboBoxAlgorithm2
            // 
            comboBoxAlgorithm2.FormattingEnabled = true;
            comboBoxAlgorithm2.Location = new Point(109, 156);
            comboBoxAlgorithm2.Name = "comboBoxAlgorithm2";
            comboBoxAlgorithm2.Size = new Size(164, 28);
            comboBoxAlgorithm2.TabIndex = 12;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1004, 496);
            Controls.Add(label5);
            Controls.Add(comboBoxAlgorithm2);
            Controls.Add(checkedListBoxAlgorithm);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(progressBarDecompress);
            Controls.Add(progressBarCompression);
            Controls.Add(comboBoxBlockSize);
            Controls.Add(comboBoxAlgorithm);
            Controls.Add(labelResult);
            Controls.Add(button2);
            Controls.Add(button1);
            Controls.Add(labelFilePath);
            Name = "Form1";
            Text = "Data Compression Algorithms Benchmark";
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label labelFilePath;
        private Button button1;
        private OpenFileDialog openFileDialog1;
        private Button button2;
        private Label labelResult;
        private ComboBox comboBoxAlgorithm;
        private ComboBox comboBoxBlockSize;
        private ProgressBar progressBarCompression;
        private ProgressBar progressBarDecompress;
        private Label label3;
        private Label label4;
        private CheckedListBox checkedListBoxAlgorithm;
        private Label label5;
        private ComboBox comboBoxAlgorithm2;
    }
}
