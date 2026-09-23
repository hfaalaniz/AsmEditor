namespace PruebaAcople
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
            this.anfitrion = new PruebaAcople.AnfitrionAcople();
            this.ventanaExplorador = new PruebaAcople.VentanaHerramienta();
            this.ventanaSalida = new PruebaAcople.VentanaHerramienta();
            this.ventanaErrores = new PruebaAcople.VentanaHerramienta();
            this.SuspendLayout();
            //
            // anfitrion
            //
            this.anfitrion.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.anfitrion.Location = new System.Drawing.Point(0, 0);
            this.anfitrion.Name = "anfitrion";
            this.anfitrion.Size = new System.Drawing.Size(1000, 700);
            this.anfitrion.TabIndex = 0;
            //
            // ventanaExplorador
            //
            this.ventanaExplorador.Descripcion = "Proyecto «X»\r\n  principal.asm\r\n  auxiliar.inc";
            this.ventanaExplorador.Location = new System.Drawing.Point(740, 0);
            this.ventanaExplorador.Name = "ventanaExplorador";
            this.ventanaExplorador.Size = new System.Drawing.Size(260, 300);
            this.ventanaExplorador.TabIndex = 1;
            this.ventanaExplorador.Titulo = "Explorador";
            //
            // ventanaSalida
            //
            this.ventanaSalida.Descripcion = "Salida de la compilación";
            this.ventanaSalida.Location = new System.Drawing.Point(224, 510);
            this.ventanaSalida.Name = "ventanaSalida";
            this.ventanaSalida.Size = new System.Drawing.Size(512, 190);
            this.ventanaSalida.TabIndex = 2;
            this.ventanaSalida.Titulo = "Salida";
            //
            // ventanaErrores
            //
            this.ventanaErrores.Descripcion = "Lista de errores";
            this.ventanaErrores.Location = new System.Drawing.Point(0, 0);
            this.ventanaErrores.Name = "ventanaErrores";
            this.ventanaErrores.Size = new System.Drawing.Size(220, 300);
            this.ventanaErrores.TabIndex = 3;
            this.ventanaErrores.Titulo = "Lista de errores";
            //
            // FormPrueba
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1000, 700);
            this.Controls.Add(this.ventanaErrores);
            this.Controls.Add(this.ventanaSalida);
            this.Controls.Add(this.ventanaExplorador);
            this.Controls.Add(this.anfitrion);
            this.Name = "FormPrueba";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Prueba de acople";
            this.Load += new System.EventHandler(this.FormPrueba_Load);
            this.ResumeLayout(false);
        }

        #endregion

        private PruebaAcople.AnfitrionAcople anfitrion;
        private PruebaAcople.VentanaHerramienta ventanaExplorador;
        private PruebaAcople.VentanaHerramienta ventanaSalida;
        private PruebaAcople.VentanaHerramienta ventanaErrores;
    }
}
