namespace AsmEditor
{
    partial class PaginaOpcionesGeneral
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
            this.lblTema = new System.Windows.Forms.Label();
            this.cmbTema = new System.Windows.Forms.ComboBox();
            this.lblNotaTema = new System.Windows.Forms.Label();
            this.lblAlIniciar = new System.Windows.Forms.Label();
            this.cmbAlIniciar = new System.Windows.Forms.ComboBox();
            this.lblNotaAlIniciar = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // lblTema
            //
            this.lblTema.Location = new System.Drawing.Point(0, 3);
            this.lblTema.Name = "lblTema";
            this.lblTema.Size = new System.Drawing.Size(150, 20);
            this.lblTema.TabIndex = 0;
            this.lblTema.Text = "Tema:";
            //
            // cmbTema
            //
            this.cmbTema.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTema.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbTema.Items.AddRange(new object[] {
            "Oscuro",
            "Claro"});
            this.cmbTema.Location = new System.Drawing.Point(160, 0);
            this.cmbTema.Name = "cmbTema";
            this.cmbTema.Size = new System.Drawing.Size(240, 23);
            this.cmbTema.TabIndex = 1;
            //
            // lblNotaTema
            //
            this.lblNotaTema.Location = new System.Drawing.Point(160, 28);
            this.lblNotaTema.Name = "lblNotaTema";
            this.lblNotaTema.Size = new System.Drawing.Size(356, 20);
            this.lblNotaTema.TabIndex = 2;
            this.lblNotaTema.Text = "También se cambia desde Ver → Tema.";
            //
            // lblAlIniciar
            //
            this.lblAlIniciar.Location = new System.Drawing.Point(0, 67);
            this.lblAlIniciar.Name = "lblAlIniciar";
            this.lblAlIniciar.Size = new System.Drawing.Size(150, 20);
            this.lblAlIniciar.TabIndex = 3;
            this.lblAlIniciar.Text = "Al iniciar:";
            //
            // cmbAlIniciar
            //
            this.cmbAlIniciar.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAlIniciar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbAlIniciar.Items.AddRange(new object[] {
            "Ventana de inicio",
            "Último proyecto",
            "Entorno vacío"});
            this.cmbAlIniciar.Location = new System.Drawing.Point(160, 64);
            this.cmbAlIniciar.Name = "cmbAlIniciar";
            this.cmbAlIniciar.Size = new System.Drawing.Size(240, 23);
            this.cmbAlIniciar.TabIndex = 4;
            //
            // lblNotaAlIniciar
            //
            this.lblNotaAlIniciar.Location = new System.Drawing.Point(160, 92);
            this.lblNotaAlIniciar.Name = "lblNotaAlIniciar";
            this.lblNotaAlIniciar.Size = new System.Drawing.Size(356, 64);
            this.lblNotaAlIniciar.TabIndex = 5;
            this.lblNotaAlIniciar.Text = "Qué se abre al arrancar el editor sin un archivo. «Último proyecto» es el que estaba abierto al salir. Vale desde el próximo arranque.";
            //
            // PaginaOpcionesGeneral
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lblNotaAlIniciar);
            this.Controls.Add(this.cmbAlIniciar);
            this.Controls.Add(this.lblAlIniciar);
            this.Controls.Add(this.lblNotaTema);
            this.Controls.Add(this.cmbTema);
            this.Controls.Add(this.lblTema);
            this.Name = "PaginaOpcionesGeneral";
            this.Size = new System.Drawing.Size(520, 396);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Label lblTema;
        private System.Windows.Forms.ComboBox cmbTema;
        private System.Windows.Forms.Label lblNotaTema;
        private System.Windows.Forms.Label lblAlIniciar;
        private System.Windows.Forms.ComboBox cmbAlIniciar;
        private System.Windows.Forms.Label lblNotaAlIniciar;
    }
}
