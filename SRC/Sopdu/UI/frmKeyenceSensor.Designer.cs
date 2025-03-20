namespace Sopdu.UI
{
    partial class frmKeyenceSensor
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
            this.TBS1 = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.TBS2 = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.nmthreshold = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.nmthreshold)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(237, 31);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(48, 12);
            this.label1.TabIndex = 0;
            this.label1.Text = "threshold";
            // 
            // TBS1
            // 
            this.TBS1.Location = new System.Drawing.Point(75, 28);
            this.TBS1.Name = "TBS1";
            this.TBS1.ReadOnly = true;
            this.TBS1.Size = new System.Drawing.Size(114, 22);
            this.TBS1.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(24, 31);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(45, 12);
            this.label2.TabIndex = 2;
            this.label2.Text = "S1 value";
            // 
            // TBS2
            // 
            this.TBS2.Location = new System.Drawing.Point(75, 68);
            this.TBS2.Name = "TBS2";
            this.TBS2.ReadOnly = true;
            this.TBS2.Size = new System.Drawing.Size(114, 22);
            this.TBS2.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(24, 71);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(45, 12);
            this.label3.TabIndex = 4;
            this.label3.Text = "S2 value";
            // 
            // buttonOK
            // 
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.Location = new System.Drawing.Point(370, 100);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 36);
            this.buttonOK.TabIndex = 6;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(451, 100);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 36);
            this.buttonCancel.TabIndex = 7;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // nmthreshold
            // 
            this.nmthreshold.Location = new System.Drawing.Point(291, 28);
            this.nmthreshold.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.nmthreshold.Minimum = new decimal(new int[] {
            1000000,
            0,
            0,
            -2147483648});
            this.nmthreshold.Name = "nmthreshold";
            this.nmthreshold.Size = new System.Drawing.Size(120, 22);
            this.nmthreshold.TabIndex = 8;
            this.nmthreshold.Value = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            // 
            // frmKeyenceSensor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(542, 148);
            this.Controls.Add(this.nmthreshold);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.TBS2);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.TBS1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Name = "frmKeyenceSensor";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Keyence Sensor";
            this.Load += new System.EventHandler(this.frmKeyenceSensor_Load);
            ((System.ComponentModel.ISupportInitialize)(this.nmthreshold)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox TBS1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox TBS2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.NumericUpDown nmthreshold;
    }
}