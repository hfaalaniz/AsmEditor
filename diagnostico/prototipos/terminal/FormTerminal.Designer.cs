namespace PruebaTerminal
{
    partial class FormTerminal
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
            this.terminal = new PruebaTerminal.ControlTerminal();
            this.lblEstado = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // terminal
            //
            this.terminal.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.terminal.Location = new System.Drawing.Point(0, 0);
            this.terminal.Name = "terminal";
            this.terminal.Size = new System.Drawing.Size(900, 536);
            this.terminal.TabIndex = 0;
            //
            // lblEstado
            //
            this.lblEstado.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblEstado.Location = new System.Drawing.Point(0, 536);
            this.lblEstado.Name = "lblEstado";
            this.lblEstado.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.lblEstado.Size = new System.Drawing.Size(900, 24);
            this.lblEstado.TabIndex = 1;
            this.lblEstado.Text = "Terminal";
            this.lblEstado.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // FormTerminal
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 560);
            this.Controls.Add(this.terminal);
            this.Controls.Add(this.lblEstado);
            this.Name = "FormTerminal";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Prueba de terminal (ConPTY)";
            this.Load += new System.EventHandler(this.FormTerminal_Load);
            this.ResumeLayout(false);
        }

        #endregion

        private PruebaTerminal.ControlTerminal terminal;
        private System.Windows.Forms.Label lblEstado;
    }
}
