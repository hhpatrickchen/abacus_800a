namespace Sopdu.UI
{
    partial class frmSearchTrain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private Sopdu.UI.popups.PMAlignControl pmAlignControl1;
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
            this.pmAlignControl1 = new Sopdu.UI.popups.PMAlignControl();
            ((System.ComponentModel.ISupportInitialize)(this.pmAlignControl1)).BeginInit();
            this.SuspendLayout();
            // 
            // pmAlignControl1
            // 
            this.pmAlignControl1.Location = new System.Drawing.Point(-2, -2);
            this.pmAlignControl1.MinimumSize = new System.Drawing.Size(489, 0);
            this.pmAlignControl1.Name = "pmAlignControl1";
            this.pmAlignControl1.Size = new System.Drawing.Size(987, 605);
            this.pmAlignControl1.SuspendElectricRuns = false;
            this.pmAlignControl1.TabIndex = 0;
            // 
            // FormNoTrain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(987, 605);
            this.Controls.Add(this.pmAlignControl1);
            this.Name = "FormNoTrain";
            this.Text = "FormNoTrain";
            this.ResumeLayout(false);

        }

        #endregion

    }
}