namespace AsmEditor
{
    partial class PaginaOpcionesNasmGoLink
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
            this.lblNasm = new System.Windows.Forms.Label();
            this.txtNasm = new System.Windows.Forms.TextBox();
            this.btnNasm = new System.Windows.Forms.Button();
            this.lblGoLink = new System.Windows.Forms.Label();
            this.txtGoLink = new System.Windows.Forms.TextBox();
            this.btnGoLink = new System.Windows.Forms.Button();
            this.lblNotaTargets = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // lblNasm
            //
            this.lblNasm.Location = new System.Drawing.Point(0, 3);
            this.lblNasm.Name = "lblNasm";
            this.lblNasm.Size = new System.Drawing.Size(150, 20);
            this.lblNasm.TabIndex = 0;
            this.lblNasm.Text = "Ruta de nasm.exe:";
            //
            // txtNasm
            //
            this.txtNasm.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtNasm.Location = new System.Drawing.Point(160, 0);
            this.txtNasm.Name = "txtNasm";
            this.txtNasm.Size = new System.Drawing.Size(310, 23);
            this.txtNasm.TabIndex = 1;
            //
            // btnNasm
            //
            this.btnNasm.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNasm.Location = new System.Drawing.Point(476, 0);
            this.btnNasm.Name = "btnNasm";
            this.btnNasm.Size = new System.Drawing.Size(40, 23);
            this.btnNasm.TabIndex = 2;
            this.btnNasm.Text = "...";
            this.btnNasm.UseVisualStyleBackColor = false;
            this.btnNasm.Click += new System.EventHandler(this.btnNasm_Click);
            //
            // lblGoLink
            //
            this.lblGoLink.Location = new System.Drawing.Point(0, 35);
            this.lblGoLink.Name = "lblGoLink";
            this.lblGoLink.Size = new System.Drawing.Size(150, 20);
            this.lblGoLink.TabIndex = 3;
            this.lblGoLink.Text = "Ruta de GoLink.exe:";
            //
            // txtGoLink
            //
            this.txtGoLink.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtGoLink.Location = new System.Drawing.Point(160, 32);
            this.txtGoLink.Name = "txtGoLink";
            this.txtGoLink.Size = new System.Drawing.Size(310, 23);
            this.txtGoLink.TabIndex = 4;
            //
            // btnGoLink
            //
            this.btnGoLink.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnGoLink.Location = new System.Drawing.Point(476, 32);
            this.btnGoLink.Name = "btnGoLink";
            this.btnGoLink.Size = new System.Drawing.Size(40, 23);
            this.btnGoLink.TabIndex = 5;
            this.btnGoLink.Text = "...";
            this.btnGoLink.UseVisualStyleBackColor = false;
            this.btnGoLink.Click += new System.EventHandler(this.btnGoLink_Click);
            //
            // lblNotaTargets
            //
            this.lblNotaTargets.Location = new System.Drawing.Point(0, 76);
            this.lblNotaTargets.Name = "lblNotaTargets";
            this.lblNotaTargets.Size = new System.Drawing.Size(516, 40);
            this.lblNotaTargets.TabIndex = 6;
            this.lblNotaTargets.Text = "Se usan cuando el target no fija las suyas. Las librerías y el punto de entrada se configuran por target, en Compilar > Administrar targets.";
            //
            // PaginaOpcionesNasmGoLink
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lblNotaTargets);
            this.Controls.Add(this.btnGoLink);
            this.Controls.Add(this.txtGoLink);
            this.Controls.Add(this.lblGoLink);
            this.Controls.Add(this.btnNasm);
            this.Controls.Add(this.txtNasm);
            this.Controls.Add(this.lblNasm);
            this.Name = "PaginaOpcionesNasmGoLink";
            this.Size = new System.Drawing.Size(520, 396);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblNasm;
        private System.Windows.Forms.TextBox txtNasm;
        private System.Windows.Forms.Button btnNasm;
        private System.Windows.Forms.Label lblGoLink;
        private System.Windows.Forms.TextBox txtGoLink;
        private System.Windows.Forms.Button btnGoLink;
        private System.Windows.Forms.Label lblNotaTargets;
    }
}
