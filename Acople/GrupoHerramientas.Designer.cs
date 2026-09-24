namespace AsmEditor
{
    partial class GrupoHerramientas
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
            this.pnlVentanas = new System.Windows.Forms.Panel();
            this.tira = new AsmEditor.TiraPestanasHerramienta();
            this.SuspendLayout();
            //
            // pnlVentanas
            //
            this.pnlVentanas.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlVentanas.Location = new System.Drawing.Point(0, 0);
            this.pnlVentanas.Name = "pnlVentanas";
            this.pnlVentanas.Size = new System.Drawing.Size(260, 276);
            this.pnlVentanas.TabIndex = 0;
            //
            // tira
            //
            this.tira.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.tira.Location = new System.Drawing.Point(0, 276);
            this.tira.Name = "tira";
            this.tira.Size = new System.Drawing.Size(260, 24);
            this.tira.TabIndex = 1;
            this.tira.PestanaElegida += new System.EventHandler<int>(this.tira_PestanaElegida);
            //
            // GrupoHerramientas
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pnlVentanas);
            this.Controls.Add(this.tira);
            this.Name = "GrupoHerramientas";
            this.Size = new System.Drawing.Size(260, 300);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel pnlVentanas;
        private AsmEditor.TiraPestanasHerramienta tira;
    }
}
