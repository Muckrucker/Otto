namespace Test_Harness
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.tbx_Url = new System.Windows.Forms.TextBox();
            this.tbx_Classname = new System.Windows.Forms.TextBox();
            this.btn_Go = new System.Windows.Forms.Button();
            this.btn_Generate = new System.Windows.Forms.Button();
            this.tbx_Output = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(51, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(23, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Url:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 39);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(61, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Classname:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // tbx_Url
            // 
            this.tbx_Url.Location = new System.Drawing.Point(88, 10);
            this.tbx_Url.Name = "tbx_Url";
            this.tbx_Url.Size = new System.Drawing.Size(317, 20);
            this.tbx_Url.TabIndex = 2;
            // 
            // tbx_Classname
            // 
            this.tbx_Classname.Location = new System.Drawing.Point(88, 36);
            this.tbx_Classname.Name = "tbx_Classname";
            this.tbx_Classname.Size = new System.Drawing.Size(317, 20);
            this.tbx_Classname.TabIndex = 3;
            // 
            // btn_Go
            // 
            this.btn_Go.Location = new System.Drawing.Point(411, 8);
            this.btn_Go.Name = "btn_Go";
            this.btn_Go.Size = new System.Drawing.Size(75, 23);
            this.btn_Go.TabIndex = 4;
            this.btn_Go.Text = "Go";
            this.btn_Go.UseVisualStyleBackColor = true;
            this.btn_Go.Click += new System.EventHandler(this.btn_Go_Click);
            // 
            // btn_Generate
            // 
            this.btn_Generate.Location = new System.Drawing.Point(411, 34);
            this.btn_Generate.Name = "btn_Generate";
            this.btn_Generate.Size = new System.Drawing.Size(75, 23);
            this.btn_Generate.TabIndex = 5;
            this.btn_Generate.Text = "Generate";
            this.btn_Generate.UseVisualStyleBackColor = true;
            // 
            // tbx_Output
            // 
            this.tbx_Output.Location = new System.Drawing.Point(12, 62);
            this.tbx_Output.Multiline = true;
            this.tbx_Output.Name = "tbx_Output";
            this.tbx_Output.Size = new System.Drawing.Size(474, 509);
            this.tbx_Output.TabIndex = 6;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(498, 583);
            this.Controls.Add(this.tbx_Output);
            this.Controls.Add(this.btn_Generate);
            this.Controls.Add(this.btn_Go);
            this.Controls.Add(this.tbx_Classname);
            this.Controls.Add(this.tbx_Url);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox tbx_Url;
        private System.Windows.Forms.TextBox tbx_Classname;
        private System.Windows.Forms.Button btn_Go;
        private System.Windows.Forms.Button btn_Generate;
        private System.Windows.Forms.TextBox tbx_Output;
    }
}

