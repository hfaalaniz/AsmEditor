namespace PruebaBarraTitulo
{
    partial class FormPrueba
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
            this.barra = new PruebaBarraTitulo.BarraTitulo();
            this.panelContenido = new System.Windows.Forms.Panel();
            this.lblInfo = new System.Windows.Forms.Label();
            this.panelContenido.SuspendLayout();
            this.SuspendLayout();
            //
            // barra
            //
            this.barra.Dock = System.Windows.Forms.DockStyle.Top;
            this.barra.Location = new System.Drawing.Point(0, 0);
            this.barra.Name = "barra";
            this.barra.Size = new System.Drawing.Size(900, 32);
            this.barra.TabIndex = 0;
            //
            // panelContenido
            //
            this.panelContenido.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelContenido.Controls.Add(this.lblInfo);
            this.panelContenido.Location = new System.Drawing.Point(0, 32);
            this.panelContenido.Name = "panelContenido";
            this.panelContenido.Size = new System.Drawing.Size(900, 568);
            this.panelContenido.TabIndex = 1;
            //
            // lblInfo
            //
            this.lblInfo.Location = new System.Drawing.Point(24, 24);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(600, 160);
            this.lblInfo.TabIndex = 0;
            this.lblInfo.Text = "Prototipo de barra de título propia (Etapa 0.2).\r\n\r\nProbar: arrastrar desde el título, doble clic para maximizar, bordes para redimensionar, Win+flechas (Snap), llevarla al otro monitor, Alt+Espacio.";
            //
            // FormPrueba
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 600);
            this.Controls.Add(this.panelContenido);
            this.Controls.Add(this.barra);
            this.MinimumSize = new System.Drawing.Size(500, 300);
            this.Name = "FormPrueba";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Prueba barra de título";
            this.Resize += new System.EventHandler(this.FormPrueba_Resize);
            this.panelContenido.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private PruebaBarraTitulo.BarraTitulo barra;
        private System.Windows.Forms.Panel panelContenido;
        private System.Windows.Forms.Label lblInfo;
    }
}
