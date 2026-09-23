namespace PruebaAcople
{
    partial class AnfitrionAcople
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
            this.zonaIzquierda = new System.Windows.Forms.Panel();
            this.divisorIzquierda = new System.Windows.Forms.Splitter();
            this.zonaDerecha = new System.Windows.Forms.Panel();
            this.divisorDerecha = new System.Windows.Forms.Splitter();
            this.zonaAbajo = new System.Windows.Forms.Panel();
            this.divisorAbajo = new System.Windows.Forms.Splitter();
            this.zonaCentro = new System.Windows.Forms.Panel();
            this.lblCentro = new System.Windows.Forms.Label();
            this.zonaCentro.SuspendLayout();
            this.SuspendLayout();
            //
            // zonaIzquierda
            //
            this.zonaIzquierda.Dock = System.Windows.Forms.DockStyle.Left;
            this.zonaIzquierda.Location = new System.Drawing.Point(0, 0);
            this.zonaIzquierda.Name = "zonaIzquierda";
            this.zonaIzquierda.Size = new System.Drawing.Size(220, 700);
            this.zonaIzquierda.TabIndex = 0;
            //
            // divisorIzquierda
            //
            this.divisorIzquierda.Dock = System.Windows.Forms.DockStyle.Left;
            this.divisorIzquierda.Location = new System.Drawing.Point(220, 0);
            this.divisorIzquierda.Name = "divisorIzquierda";
            this.divisorIzquierda.Size = new System.Drawing.Size(4, 700);
            this.divisorIzquierda.TabIndex = 1;
            this.divisorIzquierda.TabStop = false;
            //
            // zonaDerecha
            //
            this.zonaDerecha.Dock = System.Windows.Forms.DockStyle.Right;
            this.zonaDerecha.Location = new System.Drawing.Point(740, 0);
            this.zonaDerecha.Name = "zonaDerecha";
            this.zonaDerecha.Size = new System.Drawing.Size(260, 700);
            this.zonaDerecha.TabIndex = 2;
            //
            // divisorDerecha
            //
            this.divisorDerecha.Dock = System.Windows.Forms.DockStyle.Right;
            this.divisorDerecha.Location = new System.Drawing.Point(736, 0);
            this.divisorDerecha.Name = "divisorDerecha";
            this.divisorDerecha.Size = new System.Drawing.Size(4, 700);
            this.divisorDerecha.TabIndex = 3;
            this.divisorDerecha.TabStop = false;
            //
            // zonaAbajo
            //
            this.zonaAbajo.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.zonaAbajo.Location = new System.Drawing.Point(224, 510);
            this.zonaAbajo.Name = "zonaAbajo";
            this.zonaAbajo.Size = new System.Drawing.Size(512, 190);
            this.zonaAbajo.TabIndex = 4;
            //
            // divisorAbajo
            //
            this.divisorAbajo.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.divisorAbajo.Location = new System.Drawing.Point(224, 506);
            this.divisorAbajo.Name = "divisorAbajo";
            this.divisorAbajo.Size = new System.Drawing.Size(512, 4);
            this.divisorAbajo.TabIndex = 5;
            this.divisorAbajo.TabStop = false;
            //
            // zonaCentro
            //
            this.zonaCentro.Controls.Add(this.lblCentro);
            this.zonaCentro.Dock = System.Windows.Forms.DockStyle.Fill;
            this.zonaCentro.Location = new System.Drawing.Point(224, 0);
            this.zonaCentro.Name = "zonaCentro";
            this.zonaCentro.Size = new System.Drawing.Size(512, 506);
            this.zonaCentro.TabIndex = 6;
            //
            // lblCentro
            //
            this.lblCentro.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblCentro.Location = new System.Drawing.Point(0, 0);
            this.lblCentro.Name = "lblCentro";
            this.lblCentro.Size = new System.Drawing.Size(512, 506);
            this.lblCentro.TabIndex = 0;
            this.lblCentro.Text = "Documentos\r\n\r\nArrastrá la barra de título de un panel para sacarlo a una ventana flotante; soltala sobre una guía para acoplarla. Doble clic en el título de una flotante la devuelve a su lugar.";
            this.lblCentro.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // AnfitrionAcople
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.zonaCentro);
            this.Controls.Add(this.divisorAbajo);
            this.Controls.Add(this.zonaAbajo);
            this.Controls.Add(this.divisorDerecha);
            this.Controls.Add(this.zonaDerecha);
            this.Controls.Add(this.divisorIzquierda);
            this.Controls.Add(this.zonaIzquierda);
            this.Name = "AnfitrionAcople";
            this.Size = new System.Drawing.Size(1000, 700);
            this.zonaCentro.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel zonaIzquierda;
        private System.Windows.Forms.Splitter divisorIzquierda;
        private System.Windows.Forms.Panel zonaDerecha;
        private System.Windows.Forms.Splitter divisorDerecha;
        private System.Windows.Forms.Panel zonaAbajo;
        private System.Windows.Forms.Splitter divisorAbajo;
        private System.Windows.Forms.Panel zonaCentro;
        private System.Windows.Forms.Label lblCentro;
    }
}
