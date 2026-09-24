namespace AsmEditor
{
    partial class PaginaOpcionesMsvc
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
            this.lblNotaApareados = new System.Windows.Forms.Label();
            this.lblToolchain64 = new System.Windows.Forms.Label();
            this.cmbToolchain64 = new System.Windows.Forms.ComboBox();
            this.lblToolchain32 = new System.Windows.Forms.Label();
            this.cmbToolchain32 = new System.Windows.Forms.ComboBox();
            this.lblSdk64 = new System.Windows.Forms.Label();
            this.cmbSdk64 = new System.Windows.Forms.ComboBox();
            this.lblSdk32 = new System.Windows.Forms.Label();
            this.cmbSdk32 = new System.Windows.Forms.ComboBox();
            this.lblNotaDetalle = new System.Windows.Forms.Label();
            this.lblNotaTargets = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // lblNotaApareados
            //
            this.lblNotaApareados.Location = new System.Drawing.Point(0, 0);
            this.lblNotaApareados.Name = "lblNotaApareados";
            this.lblNotaApareados.Size = new System.Drawing.Size(516, 36);
            this.lblNotaApareados.TabIndex = 0;
            this.lblNotaApareados.Text = "ml64/ml y link.exe van apareados: se elige el toolchain entero. El SDK va aparte, porque sus versiones son independientes.";
            //
            // lblToolchain64
            //
            this.lblToolchain64.Location = new System.Drawing.Point(0, 47);
            this.lblToolchain64.Name = "lblToolchain64";
            this.lblToolchain64.Size = new System.Drawing.Size(150, 20);
            this.lblToolchain64.TabIndex = 1;
            this.lblToolchain64.Text = "Toolchain 64 bits:";
            //
            // cmbToolchain64
            //
            this.cmbToolchain64.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbToolchain64.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbToolchain64.Location = new System.Drawing.Point(160, 44);
            this.cmbToolchain64.Name = "cmbToolchain64";
            this.cmbToolchain64.Size = new System.Drawing.Size(356, 23);
            this.cmbToolchain64.TabIndex = 2;
            this.cmbToolchain64.SelectedIndexChanged += new System.EventHandler(this.Combo_SelectedIndexChanged);
            //
            // lblToolchain32
            //
            this.lblToolchain32.Location = new System.Drawing.Point(0, 79);
            this.lblToolchain32.Name = "lblToolchain32";
            this.lblToolchain32.Size = new System.Drawing.Size(150, 20);
            this.lblToolchain32.TabIndex = 3;
            this.lblToolchain32.Text = "Toolchain 32 bits:";
            //
            // cmbToolchain32
            //
            this.cmbToolchain32.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbToolchain32.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbToolchain32.Location = new System.Drawing.Point(160, 76);
            this.cmbToolchain32.Name = "cmbToolchain32";
            this.cmbToolchain32.Size = new System.Drawing.Size(356, 23);
            this.cmbToolchain32.TabIndex = 4;
            this.cmbToolchain32.SelectedIndexChanged += new System.EventHandler(this.Combo_SelectedIndexChanged);
            //
            // lblSdk64
            //
            this.lblSdk64.Location = new System.Drawing.Point(0, 111);
            this.lblSdk64.Name = "lblSdk64";
            this.lblSdk64.Size = new System.Drawing.Size(150, 20);
            this.lblSdk64.TabIndex = 5;
            this.lblSdk64.Text = "SDK 64 bits:";
            //
            // cmbSdk64
            //
            this.cmbSdk64.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSdk64.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbSdk64.Location = new System.Drawing.Point(160, 108);
            this.cmbSdk64.Name = "cmbSdk64";
            this.cmbSdk64.Size = new System.Drawing.Size(356, 23);
            this.cmbSdk64.TabIndex = 6;
            this.cmbSdk64.SelectedIndexChanged += new System.EventHandler(this.Combo_SelectedIndexChanged);
            //
            // lblSdk32
            //
            this.lblSdk32.Location = new System.Drawing.Point(0, 143);
            this.lblSdk32.Name = "lblSdk32";
            this.lblSdk32.Size = new System.Drawing.Size(150, 20);
            this.lblSdk32.TabIndex = 7;
            this.lblSdk32.Text = "SDK 32 bits:";
            //
            // cmbSdk32
            //
            this.cmbSdk32.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSdk32.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbSdk32.Location = new System.Drawing.Point(160, 140);
            this.cmbSdk32.Name = "cmbSdk32";
            this.cmbSdk32.Size = new System.Drawing.Size(356, 23);
            this.cmbSdk32.TabIndex = 8;
            this.cmbSdk32.SelectedIndexChanged += new System.EventHandler(this.Combo_SelectedIndexChanged);
            //
            // lblNotaDetalle
            //
            this.lblNotaDetalle.Font = new System.Drawing.Font("Consolas", 8F);
            this.lblNotaDetalle.Location = new System.Drawing.Point(0, 180);
            this.lblNotaDetalle.Name = "lblNotaDetalle";
            this.lblNotaDetalle.Size = new System.Drawing.Size(516, 46);
            this.lblNotaDetalle.TabIndex = 9;
            //
            // lblNotaTargets
            //
            this.lblNotaTargets.Location = new System.Drawing.Point(0, 236);
            this.lblNotaTargets.Name = "lblNotaTargets";
            this.lblNotaTargets.Size = new System.Drawing.Size(516, 40);
            this.lblNotaTargets.TabIndex = 10;
            this.lblNotaTargets.Text = "Se usan cuando el target no fija los suyos. Las librerías y el punto de entrada se configuran por target, en Compilar > Administrar targets.";
            //
            // PaginaOpcionesMsvc
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lblNotaTargets);
            this.Controls.Add(this.lblNotaDetalle);
            this.Controls.Add(this.cmbSdk32);
            this.Controls.Add(this.lblSdk32);
            this.Controls.Add(this.cmbSdk64);
            this.Controls.Add(this.lblSdk64);
            this.Controls.Add(this.cmbToolchain32);
            this.Controls.Add(this.lblToolchain32);
            this.Controls.Add(this.cmbToolchain64);
            this.Controls.Add(this.lblToolchain64);
            this.Controls.Add(this.lblNotaApareados);
            this.Name = "PaginaOpcionesMsvc";
            this.Size = new System.Drawing.Size(520, 396);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Label lblNotaApareados;
        private System.Windows.Forms.Label lblToolchain64;
        private System.Windows.Forms.ComboBox cmbToolchain64;
        private System.Windows.Forms.Label lblToolchain32;
        private System.Windows.Forms.ComboBox cmbToolchain32;
        private System.Windows.Forms.Label lblSdk64;
        private System.Windows.Forms.ComboBox cmbSdk64;
        private System.Windows.Forms.Label lblSdk32;
        private System.Windows.Forms.ComboBox cmbSdk32;
        private System.Windows.Forms.Label lblNotaDetalle;
        private System.Windows.Forms.Label lblNotaTargets;
    }
}
