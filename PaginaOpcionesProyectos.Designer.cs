namespace AsmEditor
{
    partial class PaginaOpcionesProyectos
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
            this.lblCarpeta = new System.Windows.Forms.Label();
            this.txtCarpeta = new System.Windows.Forms.TextBox();
            this.btnCarpeta = new System.Windows.Forms.Button();
            this.lblNotaCarpeta = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // lblCarpeta
            //
            this.lblCarpeta.Location = new System.Drawing.Point(0, 3);
            this.lblCarpeta.Name = "lblCarpeta";
            this.lblCarpeta.Size = new System.Drawing.Size(150, 20);
            this.lblCarpeta.TabIndex = 0;
            this.lblCarpeta.Text = "Carpeta del proyecto:";
            //
            // txtCarpeta
            //
            this.txtCarpeta.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtCarpeta.Location = new System.Drawing.Point(160, 0);
            this.txtCarpeta.Name = "txtCarpeta";
            this.txtCarpeta.Size = new System.Drawing.Size(310, 23);
            this.txtCarpeta.TabIndex = 1;
            //
            // btnCarpeta
            //
            this.btnCarpeta.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCarpeta.Location = new System.Drawing.Point(476, 0);
            this.btnCarpeta.Name = "btnCarpeta";
            this.btnCarpeta.Size = new System.Drawing.Size(40, 23);
            this.btnCarpeta.TabIndex = 2;
            this.btnCarpeta.Text = "...";
            this.btnCarpeta.UseVisualStyleBackColor = false;
            this.btnCarpeta.Click += new System.EventHandler(this.btnCarpeta_Click);
            //
            // lblNotaCarpeta
            //
            this.lblNotaCarpeta.Location = new System.Drawing.Point(160, 30);
            this.lblNotaCarpeta.Name = "lblNotaCarpeta";
            this.lblNotaCarpeta.Size = new System.Drawing.Size(356, 64);
            this.lblNotaCarpeta.TabIndex = 3;
            this.lblNotaCarpeta.Text = "El explorador la muestra cuando no hay un proyecto abierto, y ahí arrancan los diálogos de abrir y guardar. También se buscan ahí MASM y link.exe copiados al proyecto.";
            //
            // PaginaOpcionesProyectos
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lblNotaCarpeta);
            this.Controls.Add(this.btnCarpeta);
            this.Controls.Add(this.txtCarpeta);
            this.Controls.Add(this.lblCarpeta);
            this.Name = "PaginaOpcionesProyectos";
            this.Size = new System.Drawing.Size(520, 396);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblCarpeta;
        private System.Windows.Forms.TextBox txtCarpeta;
        private System.Windows.Forms.Button btnCarpeta;
        private System.Windows.Forms.Label lblNotaCarpeta;
    }
}
