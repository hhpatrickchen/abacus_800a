namespace Sopdu.UI
{
    partial class FrmAllDIO
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
            this.gbAllDIList = new System.Windows.Forms.GroupBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.clbAllDIList = new System.Windows.Forms.CheckedListBox();
            this.gbAllDOList = new System.Windows.Forms.GroupBox();
            this.clbAllDOList = new System.Windows.Forms.CheckedListBox();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.btnClose = new System.Windows.Forms.Button();
            this.ledCnntModbus1 = new System.Windows.Forms.Button();
            this.gbModbus1 = new System.Windows.Forms.GroupBox();
            this.tbIPAddrModbus1 = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.gbModbus2 = new System.Windows.Forms.GroupBox();
            this.tbIPAddrModbus2 = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.ledCnntModbus2 = new System.Windows.Forms.Button();
            this.gbAllDIList.SuspendLayout();
            this.gbAllDOList.SuspendLayout();
            this.gbModbus1.SuspendLayout();
            this.gbModbus2.SuspendLayout();
            this.SuspendLayout();
            // 
            // gbAllDIList
            // 
            this.gbAllDIList.Controls.Add(this.groupBox1);
            this.gbAllDIList.Controls.Add(this.clbAllDIList);
            this.gbAllDIList.Location = new System.Drawing.Point(10, 8);
            this.gbAllDIList.Name = "gbAllDIList";
            this.gbAllDIList.Size = new System.Drawing.Size(500, 540);
            this.gbAllDIList.TabIndex = 0;
            this.gbAllDIList.TabStop = false;
            this.gbAllDIList.Text = "All Digital Input List";
            // 
            // groupBox1
            // 
            this.groupBox1.Location = new System.Drawing.Point(0, 693);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(314, 48);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "groupBox1";
            // 
            // clbAllDIList
            // 
            this.clbAllDIList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.clbAllDIList.FormattingEnabled = true;
            this.clbAllDIList.Location = new System.Drawing.Point(6, 19);
            this.clbAllDIList.Name = "clbAllDIList";
            this.clbAllDIList.Size = new System.Drawing.Size(488, 514);
            this.clbAllDIList.TabIndex = 0;
            // 
            // gbAllDOList
            // 
            this.gbAllDOList.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.gbAllDOList.Controls.Add(this.clbAllDOList);
            this.gbAllDOList.Location = new System.Drawing.Point(518, 8);
            this.gbAllDOList.Name = "gbAllDOList";
            this.gbAllDOList.Size = new System.Drawing.Size(500, 540);
            this.gbAllDOList.TabIndex = 1;
            this.gbAllDOList.TabStop = false;
            this.gbAllDOList.Text = "All Digital Output List";
            // 
            // clbAllDOList
            // 
            this.clbAllDOList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.clbAllDOList.FormattingEnabled = true;
            this.clbAllDOList.Location = new System.Drawing.Point(6, 19);
            this.clbAllDOList.Name = "clbAllDOList";
            this.clbAllDOList.Size = new System.Drawing.Size(488, 514);
            this.clbAllDOList.TabIndex = 0;
            this.clbAllDOList.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.clbAllDOList_ItemCheck);
            // 
            // timer1
            // 
            this.timer1.Interval = 500;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // btnClose
            // 
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnClose.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnClose.Location = new System.Drawing.Point(461, 554);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(105, 54);
            this.btnClose.TabIndex = 2;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = false;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // ledCnntModbus1
            // 
            this.ledCnntModbus1.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.ledCnntModbus1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ledCnntModbus1.Location = new System.Drawing.Point(66, 11);
            this.ledCnntModbus1.Name = "ledCnntModbus1";
            this.ledCnntModbus1.Size = new System.Drawing.Size(96, 34);
            this.ledCnntModbus1.TabIndex = 3;
            this.ledCnntModbus1.Text = "Connection";
            this.ledCnntModbus1.UseVisualStyleBackColor = true;
            // 
            // gbModbus1
            // 
            this.gbModbus1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.gbModbus1.Controls.Add(this.tbIPAddrModbus1);
            this.gbModbus1.Controls.Add(this.label1);
            this.gbModbus1.Controls.Add(this.ledCnntModbus1);
            this.gbModbus1.Location = new System.Drawing.Point(10, 554);
            this.gbModbus1.Name = "gbModbus1";
            this.gbModbus1.Size = new System.Drawing.Size(440, 54);
            this.gbModbus1.TabIndex = 4;
            this.gbModbus1.TabStop = false;
            this.gbModbus1.Text = "Modbus1";
            // 
            // tbIPAddrModbus1
            // 
            this.tbIPAddrModbus1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbIPAddrModbus1.Location = new System.Drawing.Point(268, 17);
            this.tbIPAddrModbus1.Name = "tbIPAddrModbus1";
            this.tbIPAddrModbus1.Size = new System.Drawing.Size(149, 22);
            this.tbIPAddrModbus1.TabIndex = 5;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(196, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(58, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "IP Address";
            // 
            // gbModbus2
            // 
            this.gbModbus2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.gbModbus2.Controls.Add(this.tbIPAddrModbus2);
            this.gbModbus2.Controls.Add(this.label2);
            this.gbModbus2.Controls.Add(this.ledCnntModbus2);
            this.gbModbus2.Location = new System.Drawing.Point(578, 554);
            this.gbModbus2.Name = "gbModbus2";
            this.gbModbus2.Size = new System.Drawing.Size(440, 54);
            this.gbModbus2.TabIndex = 5;
            this.gbModbus2.TabStop = false;
            this.gbModbus2.Text = "Modbus2";
            // 
            // tbIPAddrModbus2
            // 
            this.tbIPAddrModbus2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbIPAddrModbus2.Location = new System.Drawing.Point(269, 17);
            this.tbIPAddrModbus2.Name = "tbIPAddrModbus2";
            this.tbIPAddrModbus2.Size = new System.Drawing.Size(149, 22);
            this.tbIPAddrModbus2.TabIndex = 5;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(197, 22);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(58, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "IP Address";
            // 
            // ledCnntModbus2
            // 
            this.ledCnntModbus2.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.ledCnntModbus2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ledCnntModbus2.Location = new System.Drawing.Point(67, 11);
            this.ledCnntModbus2.Name = "ledCnntModbus2";
            this.ledCnntModbus2.Size = new System.Drawing.Size(96, 34);
            this.ledCnntModbus2.TabIndex = 3;
            this.ledCnntModbus2.Text = "Connection";
            this.ledCnntModbus2.UseVisualStyleBackColor = true;
            // 
            // FrmAllDIO
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1030, 637);
            this.ControlBox = false;
            this.Controls.Add(this.gbModbus2);
            this.Controls.Add(this.gbModbus1);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.gbAllDOList);
            this.Controls.Add(this.gbAllDIList);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Name = "FrmAllDIO";
            this.Text = "All DIO View";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmAllDIO_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FrmAllDIO_FormClosed);
            this.Load += new System.EventHandler(this.FrmAllDIO_Load);
            this.SizeChanged += new System.EventHandler(this.FrmAllDIO_SizeChanged);
            this.gbAllDIList.ResumeLayout(false);
            this.gbAllDOList.ResumeLayout(false);
            this.gbModbus1.ResumeLayout(false);
            this.gbModbus1.PerformLayout();
            this.gbModbus2.ResumeLayout(false);
            this.gbModbus2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox gbAllDIList;
        private System.Windows.Forms.CheckedListBox clbAllDIList;
        private System.Windows.Forms.GroupBox gbAllDOList;
        private System.Windows.Forms.CheckedListBox clbAllDOList;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button ledCnntModbus1;
        private System.Windows.Forms.GroupBox gbModbus1;
        private System.Windows.Forms.TextBox tbIPAddrModbus1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox gbModbus2;
        private System.Windows.Forms.TextBox tbIPAddrModbus2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button ledCnntModbus2;
    }
}