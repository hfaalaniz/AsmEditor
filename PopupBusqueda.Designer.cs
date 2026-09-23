namespace AsmEditor
{
    partial class PopupBusqueda
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
            this.lstResultados = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            //
            // lstResultados
            //
            this.lstResultados.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstResultados.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lstResultados.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.lstResultados.IntegralHeight = false;
            this.lstResultados.ItemHeight = 24;
            this.lstResultados.Location = new System.Drawing.Point(1, 1);
            this.lstResultados.Name = "lstResultados";
            this.lstResultados.Size = new System.Drawing.Size(398, 238);
            this.lstResultados.TabIndex = 0;
            this.lstResultados.TabStop = false;
            this.lstResultados.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.lstResultados_DrawItem);
            this.lstResultados.MouseDown += new System.Windows.Forms.MouseEventHandler(this.lstResultados_MouseDown);
            this.lstResultados.MouseMove += new System.Windows.Forms.MouseEventHandler(this.lstResultados_MouseMove);
            //
            // PopupBusqueda
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(400, 240);
            this.Controls.Add(this.lstResultados);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "PopupBusqueda";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Resultados de la búsqueda";
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.ListBox lstResultados;
    }
}
