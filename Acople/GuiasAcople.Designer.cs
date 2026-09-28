namespace AsmEditor
{
    partial class GuiasAcople
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        private void InitializeComponent()
        {
            this.SuspendLayout();
            //
            // GuiasAcople
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Magenta;
            this.ClientSize = new System.Drawing.Size(400, 300);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "GuiasAcople";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Guías de acople";
            this.TopMost = true;
            this.TransparencyKey = System.Drawing.Color.Magenta;
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.GuiasAcople_Paint);
            this.ResumeLayout(false);
        }

        #endregion
    }
}
