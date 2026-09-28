namespace AsmEditor
{
    partial class VentanaFlotante
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
            this.pnlContenido = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            //
            // pnlContenido
            //
            this.pnlContenido.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlContenido.Location = new System.Drawing.Point(0, 4);
            this.pnlContenido.Name = "pnlContenido";
            this.pnlContenido.Size = new System.Drawing.Size(300, 396);
            this.pnlContenido.TabIndex = 0;
            //
            // VentanaFlotante
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(300, 400);
            this.Controls.Add(this.pnlContenido);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(160, 120);
            this.Name = "VentanaFlotante";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Flotante";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.VentanaFlotante_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.VentanaFlotante_FormClosed);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel pnlContenido;
    }
}
