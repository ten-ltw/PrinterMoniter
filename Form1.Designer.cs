namespace PrinterMoniter
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
            fileSelector = new OpenFileDialog();
            selectFileBtn = new Button();
            xmlFilePathLabel = new Label();
            startOrStopBtn = new Button();
            label1 = new Label();
            button1 = new Button();
            SuspendLayout();
            // 
            // fileSelector
            // 
            fileSelector.FileName = "openFileDialog1";
            // 
            // selectFileBtn
            // 
            selectFileBtn.Location = new Point(48, 52);
            selectFileBtn.Name = "selectFileBtn";
            selectFileBtn.Size = new Size(170, 40);
            selectFileBtn.TabIndex = 0;
            selectFileBtn.Text = "选择XML文件";
            selectFileBtn.UseVisualStyleBackColor = true;
            selectFileBtn.Click += selectFileBtn_Click;
            // 
            // xmlFilePathLabel
            // 
            xmlFilePathLabel.AutoSize = true;
            xmlFilePathLabel.Location = new Point(256, 59);
            xmlFilePathLabel.Name = "xmlFilePathLabel";
            xmlFilePathLabel.Size = new Size(163, 28);
            xmlFilePathLabel.TabIndex = 1;
            xmlFilePathLabel.Text = "请选择XML文件";
            // 
            // startOrStopBtn
            // 
            startOrStopBtn.Location = new Point(599, 382);
            startOrStopBtn.Name = "startOrStopBtn";
            startOrStopBtn.Size = new Size(131, 40);
            startOrStopBtn.TabIndex = 2;
            startOrStopBtn.Text = "开始";
            startOrStopBtn.UseVisualStyleBackColor = true;
            startOrStopBtn.Click += startOrStopBtn_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(267, 161);
            label1.Name = "label1";
            label1.Size = new Size(73, 28);
            label1.TabIndex = 3;
            label1.Text = "label1";
            // 
            // button1
            // 
            button1.Location = new Point(599, 264);
            button1.Name = "button1";
            button1.Size = new Size(131, 40);
            button1.TabIndex = 4;
            button1.Text = "播放测试";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(13F, 28F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(button1);
            Controls.Add(label1);
            Controls.Add(startOrStopBtn);
            Controls.Add(xmlFilePathLabel);
            Controls.Add(selectFileBtn);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private OpenFileDialog fileSelector;
        private Button selectFileBtn;
        private Label xmlFilePathLabel;
        private Button startOrStopBtn;
        private Label label1;
        private Button button1;
    }
}
