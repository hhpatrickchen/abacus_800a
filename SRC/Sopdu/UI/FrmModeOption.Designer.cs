namespace Sopdu.UI
{
    partial class FrmModeOption
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
            this.components = new System.ComponentModel.Container();
            this.cbbModeIP = new System.Windows.Forms.ComboBox();
            this.cbbModeOP = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.ledSFARdyIP = new System.Windows.Forms.Button();
            this.ledOHTRdyIP = new System.Windows.Forms.Button();
            this.ledPresentIPCV = new System.Windows.Forms.Button();
            this.ledPresentOPCV = new System.Windows.Forms.Button();
            this.ledOHTRdyOP = new System.Windows.Forms.Button();
            this.ledSFARdyOP = new System.Windows.Forms.Button();
            this.ledPresentIPSTK = new System.Windows.Forms.Button();
            this.ledPresentOPSTK = new System.Windows.Forms.Button();
            this.ledPresentSHT1 = new System.Windows.Forms.Button();
            this.ledPresentSHT2 = new System.Windows.Forms.Button();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.gbIPCV = new System.Windows.Forms.GroupBox();
            this.clbIPMode = new System.Windows.Forms.CheckedListBox();
            this.gbOPCV = new System.Windows.Forms.GroupBox();
            this.tbMessage = new System.Windows.Forms.TextBox();
            this.gbConfirm = new System.Windows.Forms.GroupBox();
            this.btnsave = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.numFailRate = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
            this.label5 = new System.Windows.Forms.Label();
            this.gbIPCV.SuspendLayout();
            this.gbOPCV.SuspendLayout();
            this.gbConfirm.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numFailRate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
            this.SuspendLayout();
            // 
            // cbbModeIP
            // 
            this.cbbModeIP.AutoCompleteCustomSource.AddRange(new string[] {
            "Manual",
            "Conveyor",
            "OHT"});
            this.cbbModeIP.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cbbModeIP.FormattingEnabled = true;
            this.cbbModeIP.Items.AddRange(new object[] {
            "Manual",
            "Conveyor",
            "OHT"});
            this.cbbModeIP.Location = new System.Drawing.Point(190, 37);
            this.cbbModeIP.Name = "cbbModeIP";
            this.cbbModeIP.Size = new System.Drawing.Size(130, 24);
            this.cbbModeIP.TabIndex = 0;
            this.cbbModeIP.Visible = false;
            this.cbbModeIP.SelectedIndexChanged += new System.EventHandler(this.cbbModeIP_SelectedIndexChanged);
            this.cbbModeIP.SelectionChangeCommitted += new System.EventHandler(this.cbbModeIP_SelectionChangeCommitted);
            this.cbbModeIP.Click += new System.EventHandler(this.cbbModeIP_Click);
            // 
            // cbbModeOP
            // 
            this.cbbModeOP.AutoCompleteCustomSource.AddRange(new string[] {
            "Manual",
            "Conveyor",
            "OHT"});
            this.cbbModeOP.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cbbModeOP.FormattingEnabled = true;
            this.cbbModeOP.Items.AddRange(new object[] {
            "OHT",
            "Conveyor",
            "Manual"});
            this.cbbModeOP.Location = new System.Drawing.Point(6, 34);
            this.cbbModeOP.Name = "cbbModeOP";
            this.cbbModeOP.Size = new System.Drawing.Size(130, 24);
            this.cbbModeOP.TabIndex = 1;
            this.cbbModeOP.SelectedIndexChanged += new System.EventHandler(this.cbbModeOP_SelectedIndexChanged);
            this.cbbModeOP.SelectionChangeCommitted += new System.EventHandler(this.cbbModeOP_SelectionChangeCommitted);
            this.cbbModeOP.Click += new System.EventHandler(this.cbbModeOP_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 14);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(83, 12);
            this.label1.TabIndex = 2;
            this.label1.Text = "Loading Mode:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 19);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(95, 12);
            this.label2.TabIndex = 3;
            this.label2.Text = "Unloading Mode:";
            // 
            // btnOK
            // 
            this.btnOK.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnOK.Location = new System.Drawing.Point(178, 65);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(88, 37);
            this.btnOK.TabIndex = 4;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = false;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.BackColor = System.Drawing.SystemColors.Control;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCancel.Location = new System.Drawing.Point(579, 65);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(88, 37);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = false;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // ledSFARdyIP
            // 
            this.ledSFARdyIP.Location = new System.Drawing.Point(6, 90);
            this.ledSFARdyIP.Name = "ledSFARdyIP";
            this.ledSFARdyIP.Size = new System.Drawing.Size(130, 34);
            this.ledSFARdyIP.TabIndex = 6;
            this.ledSFARdyIP.Text = "SFA Ready";
            this.ledSFARdyIP.UseVisualStyleBackColor = true;
            // 
            // ledOHTRdyIP
            // 
            this.ledOHTRdyIP.Location = new System.Drawing.Point(6, 141);
            this.ledOHTRdyIP.Name = "ledOHTRdyIP";
            this.ledOHTRdyIP.Size = new System.Drawing.Size(130, 34);
            this.ledOHTRdyIP.TabIndex = 7;
            this.ledOHTRdyIP.Text = "OHT Ready";
            this.ledOHTRdyIP.UseVisualStyleBackColor = true;
            // 
            // ledPresentIPCV
            // 
            this.ledPresentIPCV.Location = new System.Drawing.Point(6, 194);
            this.ledPresentIPCV.Name = "ledPresentIPCV";
            this.ledPresentIPCV.Size = new System.Drawing.Size(130, 34);
            this.ledPresentIPCV.TabIndex = 8;
            this.ledPresentIPCV.Text = "Present";
            this.ledPresentIPCV.UseVisualStyleBackColor = true;
            // 
            // ledPresentOPCV
            // 
            this.ledPresentOPCV.Location = new System.Drawing.Point(6, 194);
            this.ledPresentOPCV.Name = "ledPresentOPCV";
            this.ledPresentOPCV.Size = new System.Drawing.Size(130, 34);
            this.ledPresentOPCV.TabIndex = 11;
            this.ledPresentOPCV.Text = "Present";
            this.ledPresentOPCV.UseVisualStyleBackColor = true;
            // 
            // ledOHTRdyOP
            // 
            this.ledOHTRdyOP.Location = new System.Drawing.Point(6, 141);
            this.ledOHTRdyOP.Name = "ledOHTRdyOP";
            this.ledOHTRdyOP.Size = new System.Drawing.Size(130, 34);
            this.ledOHTRdyOP.TabIndex = 10;
            this.ledOHTRdyOP.Text = "OHT Ready";
            this.ledOHTRdyOP.UseVisualStyleBackColor = true;
            // 
            // ledSFARdyOP
            // 
            this.ledSFARdyOP.Location = new System.Drawing.Point(6, 90);
            this.ledSFARdyOP.Name = "ledSFARdyOP";
            this.ledSFARdyOP.Size = new System.Drawing.Size(130, 34);
            this.ledSFARdyOP.TabIndex = 9;
            this.ledSFARdyOP.Text = "SFA Ready";
            this.ledSFARdyOP.UseVisualStyleBackColor = true;
            // 
            // ledPresentIPSTK
            // 
            this.ledPresentIPSTK.Location = new System.Drawing.Point(190, 207);
            this.ledPresentIPSTK.Name = "ledPresentIPSTK";
            this.ledPresentIPSTK.Size = new System.Drawing.Size(130, 34);
            this.ledPresentIPSTK.TabIndex = 12;
            this.ledPresentIPSTK.Text = "Present";
            this.ledPresentIPSTK.UseVisualStyleBackColor = true;
            // 
            // ledPresentOPSTK
            // 
            this.ledPresentOPSTK.Location = new System.Drawing.Point(549, 207);
            this.ledPresentOPSTK.Name = "ledPresentOPSTK";
            this.ledPresentOPSTK.Size = new System.Drawing.Size(130, 34);
            this.ledPresentOPSTK.TabIndex = 13;
            this.ledPresentOPSTK.Text = "Present";
            this.ledPresentOPSTK.UseVisualStyleBackColor = true;
            // 
            // ledPresentSHT1
            // 
            this.ledPresentSHT1.Location = new System.Drawing.Point(369, 207);
            this.ledPresentSHT1.Name = "ledPresentSHT1";
            this.ledPresentSHT1.Size = new System.Drawing.Size(130, 34);
            this.ledPresentSHT1.TabIndex = 14;
            this.ledPresentSHT1.Text = "Present";
            this.ledPresentSHT1.UseVisualStyleBackColor = true;
            // 
            // ledPresentSHT2
            // 
            this.ledPresentSHT2.Location = new System.Drawing.Point(369, 152);
            this.ledPresentSHT2.Name = "ledPresentSHT2";
            this.ledPresentSHT2.Size = new System.Drawing.Size(130, 34);
            this.ledPresentSHT2.TabIndex = 15;
            this.ledPresentSHT2.Text = "Present";
            this.ledPresentSHT2.UseVisualStyleBackColor = true;
            // 
            // timer1
            // 
            this.timer1.Interval = 500;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // gbIPCV
            // 
            this.gbIPCV.Controls.Add(this.clbIPMode);
            this.gbIPCV.Controls.Add(this.label1);
            this.gbIPCV.Controls.Add(this.ledSFARdyIP);
            this.gbIPCV.Controls.Add(this.ledOHTRdyIP);
            this.gbIPCV.Controls.Add(this.ledPresentIPCV);
            this.gbIPCV.Location = new System.Drawing.Point(12, 11);
            this.gbIPCV.Name = "gbIPCV";
            this.gbIPCV.Size = new System.Drawing.Size(150, 239);
            this.gbIPCV.TabIndex = 16;
            this.gbIPCV.TabStop = false;
            this.gbIPCV.Text = "Input Conveyor";
            // 
            // clbIPMode
            // 
            this.clbIPMode.FormattingEnabled = true;
            this.clbIPMode.Items.AddRange(new object[] {
            "OHT",
            "Conveyor",
            "Manual"});
            this.clbIPMode.Location = new System.Drawing.Point(9, 29);
            this.clbIPMode.Name = "clbIPMode";
            this.clbIPMode.Size = new System.Drawing.Size(127, 36);
            this.clbIPMode.TabIndex = 9;
            // 
            // gbOPCV
            // 
            this.gbOPCV.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.gbOPCV.Controls.Add(this.label2);
            this.gbOPCV.Controls.Add(this.cbbModeOP);
            this.gbOPCV.Controls.Add(this.ledSFARdyOP);
            this.gbOPCV.Controls.Add(this.ledOHTRdyOP);
            this.gbOPCV.Controls.Add(this.ledPresentOPCV);
            this.gbOPCV.Location = new System.Drawing.Point(708, 11);
            this.gbOPCV.Name = "gbOPCV";
            this.gbOPCV.Size = new System.Drawing.Size(150, 239);
            this.gbOPCV.TabIndex = 17;
            this.gbOPCV.TabStop = false;
            this.gbOPCV.Text = "Output Conveyor";
            // 
            // tbMessage
            // 
            this.tbMessage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbMessage.BackColor = System.Drawing.SystemColors.Info;
            this.tbMessage.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbMessage.Location = new System.Drawing.Point(6, 26);
            this.tbMessage.Name = "tbMessage";
            this.tbMessage.ReadOnly = true;
            this.tbMessage.Size = new System.Drawing.Size(826, 26);
            this.tbMessage.TabIndex = 19;
            // 
            // gbConfirm
            // 
            this.gbConfirm.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbConfirm.Controls.Add(this.tbMessage);
            this.gbConfirm.Controls.Add(this.btnOK);
            this.gbConfirm.Controls.Add(this.btnCancel);
            this.gbConfirm.Location = new System.Drawing.Point(12, 276);
            this.gbConfirm.Name = "gbConfirm";
            this.gbConfirm.Size = new System.Drawing.Size(846, 107);
            this.gbConfirm.TabIndex = 20;
            this.gbConfirm.TabStop = false;
            this.gbConfirm.Text = "Message and Confirmation";
            // 
            // btnsave
            // 
            this.btnsave.Location = new System.Drawing.Point(521, 55);
            this.btnsave.Name = "btnsave";
            this.btnsave.Size = new System.Drawing.Size(75, 21);
            this.btnsave.TabIndex = 24;
            this.btnsave.Text = "Save";
            this.btnsave.UseVisualStyleBackColor = true;
            this.btnsave.Click += new System.EventHandler(this.btnsave_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(332, 25);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(101, 12);
            this.label3.TabIndex = 22;
            this.label3.Text = "Max Fail Rate(%)";
            // 
            // numFailRate
            // 
            this.numFailRate.Location = new System.Drawing.Point(335, 40);
            this.numFailRate.Name = "numFailRate";
            this.numFailRate.Size = new System.Drawing.Size(120, 21);
            this.numFailRate.TabIndex = 23;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(407, 89);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(0, 12);
            this.label4.TabIndex = 25;
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(334, 70);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(174, 16);
            this.checkBox1.TabIndex = 26;
            this.checkBox1.Text = "Enable Long Side Tray gap";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // numericUpDown1
            // 
            this.numericUpDown1.Location = new System.Drawing.Point(334, 114);
            this.numericUpDown1.Maximum = new decimal(new int[] {
            10000000,
            0,
            0,
            0});
            this.numericUpDown1.Name = "numericUpDown1";
            this.numericUpDown1.Size = new System.Drawing.Size(120, 21);
            this.numericUpDown1.TabIndex = 27;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(333, 95);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(131, 12);
            this.label5.TabIndex = 28;
            this.label5.Text = "Shutter Save Distance";
            // 
            // FrmModeOption
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(880, 422);
            this.ControlBox = false;
            this.Controls.Add(this.label5);
            this.Controls.Add(this.numericUpDown1);
            this.Controls.Add(this.checkBox1);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.btnsave);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.numFailRate);
            this.Controls.Add(this.cbbModeIP);
            this.Controls.Add(this.gbConfirm);
            this.Controls.Add(this.gbOPCV);
            this.Controls.Add(this.gbIPCV);
            this.Controls.Add(this.ledPresentSHT2);
            this.Controls.Add(this.ledPresentSHT1);
            this.Controls.Add(this.ledPresentOPSTK);
            this.Controls.Add(this.ledPresentIPSTK);
            this.Cursor = System.Windows.Forms.Cursors.Default;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Name = "FrmModeOption";
            this.Text = "Tray Loading/Unloading Mode Selection";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmModeOption_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FrmModeOption_FormClosed);
            this.Load += new System.EventHandler(this.FrmModeOption_Load);
            this.gbIPCV.ResumeLayout(false);
            this.gbIPCV.PerformLayout();
            this.gbOPCV.ResumeLayout(false);
            this.gbOPCV.PerformLayout();
            this.gbConfirm.ResumeLayout(false);
            this.gbConfirm.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numFailRate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox cbbModeIP;
        private System.Windows.Forms.ComboBox cbbModeOP;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button ledSFARdyIP;
        private System.Windows.Forms.Button ledOHTRdyIP;
        private System.Windows.Forms.Button ledPresentIPCV;
        private System.Windows.Forms.Button ledPresentOPCV;
        private System.Windows.Forms.Button ledOHTRdyOP;
        private System.Windows.Forms.Button ledSFARdyOP;
        private System.Windows.Forms.Button ledPresentIPSTK;
        private System.Windows.Forms.Button ledPresentOPSTK;
        private System.Windows.Forms.Button ledPresentSHT1;
        private System.Windows.Forms.Button ledPresentSHT2;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.GroupBox gbIPCV;
        private System.Windows.Forms.GroupBox gbOPCV;
        private System.Windows.Forms.TextBox tbMessage;
        private System.Windows.Forms.GroupBox gbConfirm;
        private System.Windows.Forms.CheckedListBox clbIPMode;
        private System.Windows.Forms.Button btnsave;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown numFailRate;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.NumericUpDown numericUpDown1;
        private System.Windows.Forms.Label label5;
    }
}