namespace Sopdu.UI
{
    partial class frmImageFileTool
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
            this.cogImageFileEditV21 = new Cognex.VisionPro.ImageFile.CogImageFileEditV2();
            ((System.ComponentModel.ISupportInitialize)(this.cogImageFileEditV21)).BeginInit();
            this.SuspendLayout();
            // 
            // cogImageFileEditV21
            // 
            this.cogImageFileEditV21.AllowDrop = true;
            this.cogImageFileEditV21.Location = new System.Drawing.Point(12, -3);
            this.cogImageFileEditV21.MinimumSize = new System.Drawing.Size(489, 0);
            this.cogImageFileEditV21.Name = "cogImageFileEditV21";
            this.cogImageFileEditV21.OutputHighLight = System.Drawing.Color.Lime;
            this.cogImageFileEditV21.Size = new System.Drawing.Size(934, 506);
            this.cogImageFileEditV21.SuspendElectricRuns = false;
            this.cogImageFileEditV21.TabIndex = 0;
            // 
            // frmImageFileTool
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(943, 501);
            this.Controls.Add(this.cogImageFileEditV21);
            this.Name = "frmImageFileTool";
            this.Text = "frmImageFileTool";
            ((System.ComponentModel.ISupportInitialize)(this.cogImageFileEditV21)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private Cognex.VisionPro.ImageFile.CogImageFileEditV2 cogImageFileEditV21;
    }
}