namespace Sopdu.UI
{
    partial class testform
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
            this.cogCopyRegionEditV21 = new Cognex.VisionPro.ImageProcessing.CogCopyRegionEditV2();
            ((System.ComponentModel.ISupportInitialize)(this.cogCopyRegionEditV21)).BeginInit();
            this.SuspendLayout();
            // 
            // cogCopyRegionEditV21
            // 
            this.cogCopyRegionEditV21.Location = new System.Drawing.Point(78, 62);
            this.cogCopyRegionEditV21.MinimumSize = new System.Drawing.Size(489, 0);
            this.cogCopyRegionEditV21.Name = "cogCopyRegionEditV21";
            this.cogCopyRegionEditV21.Size = new System.Drawing.Size(748, 405);
            this.cogCopyRegionEditV21.SuspendElectricRuns = false;
            this.cogCopyRegionEditV21.TabIndex = 0;
            // 
            // testform
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(949, 695);
            this.Controls.Add(this.cogCopyRegionEditV21);
            this.Name = "testform";
            this.Text = "testform";
            ((System.ComponentModel.ISupportInitialize)(this.cogCopyRegionEditV21)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private Cognex.VisionPro.ImageProcessing.CogCopyRegionEditV2 cogCopyRegionEditV21;
    }
}