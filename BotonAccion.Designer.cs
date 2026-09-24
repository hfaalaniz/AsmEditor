namespace AsmEditor
{
    partial class BotonAccion
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

        #region Código generado por el Diseñador de componentes

        private void InitializeComponent()
        {
            this.lblIcono = new System.Windows.Forms.Label();
            this.lblTexto = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // lblIcono
            //
            this.lblIcono.Location = new System.Drawing.Point(10, 10);
            this.lblIcono.Name = "lblIcono";
            this.lblIcono.Size = new System.Drawing.Size(20, 20);
            this.lblIcono.TabIndex = 0;
            this.lblIcono.Click += new System.EventHandler(this.Parte_Click);
            this.lblIcono.MouseEnter += new System.EventHandler(this.Parte_MouseEnter);
            this.lblIcono.MouseLeave += new System.EventHandler(this.Parte_MouseLeave);
            //
            // lblTexto
            //
            this.lblTexto.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblTexto.AutoEllipsis = true;
            this.lblTexto.Location = new System.Drawing.Point(40, 0);
            this.lblTexto.Name = "lblTexto";
            this.lblTexto.Size = new System.Drawing.Size(230, 40);
            this.lblTexto.TabIndex = 1;
            this.lblTexto.Text = "Acción";
            this.lblTexto.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblTexto.Click += new System.EventHandler(this.Parte_Click);
            this.lblTexto.MouseEnter += new System.EventHandler(this.Parte_MouseEnter);
            this.lblTexto.MouseLeave += new System.EventHandler(this.Parte_MouseLeave);
            //
            // BotonAccion
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lblTexto);
            this.Controls.Add(this.lblIcono);
            this.Cursor = System.Windows.Forms.Cursors.Hand;
            this.Name = "BotonAccion";
            this.Size = new System.Drawing.Size(270, 40);
            this.MouseEnter += new System.EventHandler(this.Parte_MouseEnter);
            this.MouseLeave += new System.EventHandler(this.Parte_MouseLeave);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Label lblIcono;
        private System.Windows.Forms.Label lblTexto;
    }
}
