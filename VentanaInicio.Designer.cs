namespace AsmEditor
{
    partial class VentanaInicio
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
            this.components = new System.ComponentModel.Container();
            this.lblTitulo = new System.Windows.Forms.Label();
            this.lblIntroduccion = new System.Windows.Forms.Label();
            this.txtBuscar = new System.Windows.Forms.TextBox();
            this.lstRecientes = new System.Windows.Forms.ListBox();
            this.cmsProyecto = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.miQuitar = new System.Windows.Forms.ToolStripMenuItem();
            this.miAbrirCarpeta = new System.Windows.Forms.ToolStripMenuItem();
            this.lblSinRecientes = new System.Windows.Forms.Label();
            this.btnCrear = new AsmEditor.BotonAccion();
            this.btnAbrirProyecto = new AsmEditor.BotonAccion();
            this.btnAbrirCarpeta = new AsmEditor.BotonAccion();
            this.btnAbrirArchivo = new AsmEditor.BotonAccion();
            this.btnContinuar = new System.Windows.Forms.Button();
            this.cmsProyecto.SuspendLayout();
            this.SuspendLayout();
            //
            // lblTitulo
            //
            this.lblTitulo.AutoSize = true;
            this.lblTitulo.Font = new System.Drawing.Font("Segoe UI Light", 22F);
            this.lblTitulo.Location = new System.Drawing.Point(26, 16);
            this.lblTitulo.Name = "lblTitulo";
            this.lblTitulo.Size = new System.Drawing.Size(160, 41);
            this.lblTitulo.TabIndex = 0;
            this.lblTitulo.Text = "Editor ASM";
            //
            // lblIntroduccion
            //
            this.lblIntroduccion.AutoSize = true;
            this.lblIntroduccion.Font = new System.Drawing.Font("Segoe UI Semibold", 11F);
            this.lblIntroduccion.Location = new System.Drawing.Point(28, 72);
            this.lblIntroduccion.Name = "lblIntroduccion";
            this.lblIntroduccion.Size = new System.Drawing.Size(92, 20);
            this.lblIntroduccion.TabIndex = 1;
            this.lblIntroduccion.Text = "Introducción";
            //
            // txtBuscar
            //
            this.txtBuscar.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtBuscar.Location = new System.Drawing.Point(30, 104);
            this.txtBuscar.Name = "txtBuscar";
            this.txtBuscar.PlaceholderText = "Buscar en recientes (Alt+U)";
            this.txtBuscar.Size = new System.Drawing.Size(560, 23);
            this.txtBuscar.TabIndex = 2;
            this.txtBuscar.TextChanged += new System.EventHandler(this.txtBuscar_TextChanged);
            this.txtBuscar.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtBuscar_KeyDown);
            //
            // lstRecientes
            //
            this.lstRecientes.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lstRecientes.ContextMenuStrip = this.cmsProyecto;
            this.lstRecientes.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
            this.lstRecientes.IntegralHeight = false;
            this.lstRecientes.Location = new System.Drawing.Point(30, 136);
            this.lstRecientes.Name = "lstRecientes";
            this.lstRecientes.Size = new System.Drawing.Size(560, 380);
            this.lstRecientes.TabIndex = 3;
            this.lstRecientes.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.lstRecientes_DrawItem);
            this.lstRecientes.MeasureItem += new System.Windows.Forms.MeasureItemEventHandler(this.lstRecientes_MeasureItem);
            this.lstRecientes.DoubleClick += new System.EventHandler(this.lstRecientes_DoubleClick);
            this.lstRecientes.KeyDown += new System.Windows.Forms.KeyEventHandler(this.lstRecientes_KeyDown);
            this.lstRecientes.MouseDown += new System.Windows.Forms.MouseEventHandler(this.lstRecientes_MouseDown);
            //
            // cmsProyecto
            //
            this.cmsProyecto.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miQuitar,
            this.miAbrirCarpeta});
            this.cmsProyecto.Name = "cmsProyecto";
            this.cmsProyecto.Size = new System.Drawing.Size(211, 48);
            this.cmsProyecto.Opening += new System.ComponentModel.CancelEventHandler(this.cmsProyecto_Opening);
            //
            // miQuitar
            //
            this.miQuitar.Name = "miQuitar";
            this.miQuitar.Size = new System.Drawing.Size(210, 22);
            this.miQuitar.Text = "Quitar de la lista";
            this.miQuitar.Click += new System.EventHandler(this.miQuitar_Click);
            //
            // miAbrirCarpeta
            //
            this.miAbrirCarpeta.Name = "miAbrirCarpeta";
            this.miAbrirCarpeta.Size = new System.Drawing.Size(210, 22);
            this.miAbrirCarpeta.Text = "Abrir carpeta contenedora";
            this.miAbrirCarpeta.Click += new System.EventHandler(this.miAbrirCarpeta_Click);
            //
            // lblSinRecientes
            //
            this.lblSinRecientes.Location = new System.Drawing.Point(30, 250);
            this.lblSinRecientes.Name = "lblSinRecientes";
            this.lblSinRecientes.Size = new System.Drawing.Size(560, 40);
            this.lblSinRecientes.TabIndex = 4;
            this.lblSinRecientes.Text = "Todavía no hay proyectos recientes.";
            this.lblSinRecientes.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblSinRecientes.Visible = false;
            //
            // btnCrear
            //
            this.btnCrear.Glifo = AsmEditor.Glifo.Nuevo;
            this.btnCrear.Location = new System.Drawing.Point(620, 104);
            this.btnCrear.Name = "btnCrear";
            this.btnCrear.Size = new System.Drawing.Size(270, 40);
            this.btnCrear.TabIndex = 5;
            this.btnCrear.Texto = "Crear un proyecto";
            this.btnCrear.Click += new System.EventHandler(this.btnCrear_Click);
            //
            // btnAbrirProyecto
            //
            this.btnAbrirProyecto.Glifo = AsmEditor.Glifo.Abrir;
            this.btnAbrirProyecto.Location = new System.Drawing.Point(620, 148);
            this.btnAbrirProyecto.Name = "btnAbrirProyecto";
            this.btnAbrirProyecto.Size = new System.Drawing.Size(270, 40);
            this.btnAbrirProyecto.TabIndex = 6;
            this.btnAbrirProyecto.Texto = "Abrir un proyecto";
            this.btnAbrirProyecto.Click += new System.EventHandler(this.btnAbrirProyecto_Click);
            //
            // btnAbrirCarpeta
            //
            this.btnAbrirCarpeta.Glifo = AsmEditor.Glifo.Explorador;
            this.btnAbrirCarpeta.Location = new System.Drawing.Point(620, 192);
            this.btnAbrirCarpeta.Name = "btnAbrirCarpeta";
            this.btnAbrirCarpeta.Size = new System.Drawing.Size(270, 40);
            this.btnAbrirCarpeta.TabIndex = 7;
            this.btnAbrirCarpeta.Texto = "Abrir una carpeta";
            this.btnAbrirCarpeta.Click += new System.EventHandler(this.btnAbrirCarpeta_Click);
            //
            // btnAbrirArchivo
            //
            this.btnAbrirArchivo.Glifo = AsmEditor.Glifo.Abrir;
            this.btnAbrirArchivo.Location = new System.Drawing.Point(620, 236);
            this.btnAbrirArchivo.Name = "btnAbrirArchivo";
            this.btnAbrirArchivo.Size = new System.Drawing.Size(270, 40);
            this.btnAbrirArchivo.TabIndex = 8;
            this.btnAbrirArchivo.Texto = "Abrir un archivo o proyecto";
            this.btnAbrirArchivo.Click += new System.EventHandler(this.btnAbrirArchivo_Click);
            //
            // btnContinuar
            //
            this.btnContinuar.FlatAppearance.BorderSize = 1;
            this.btnContinuar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnContinuar.Location = new System.Drawing.Point(730, 524);
            this.btnContinuar.Name = "btnContinuar";
            this.btnContinuar.Size = new System.Drawing.Size(160, 30);
            this.btnContinuar.TabIndex = 9;
            this.btnContinuar.Text = "Continuar sin código";
            this.btnContinuar.UseVisualStyleBackColor = false;
            this.btnContinuar.Click += new System.EventHandler(this.btnContinuar_Click);
            //
            // VentanaInicio
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(920, 574);
            this.Controls.Add(this.btnContinuar);
            this.Controls.Add(this.btnAbrirArchivo);
            this.Controls.Add(this.btnAbrirCarpeta);
            this.Controls.Add(this.btnAbrirProyecto);
            this.Controls.Add(this.btnCrear);
            this.Controls.Add(this.lblSinRecientes);
            this.Controls.Add(this.lstRecientes);
            this.Controls.Add(this.txtBuscar);
            this.Controls.Add(this.lblIntroduccion);
            this.Controls.Add(this.lblTitulo);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.Name = "VentanaInicio";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Editor ASM — Inicio";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.VentanaInicio_KeyDown);
            this.cmsProyecto.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblTitulo;
        private System.Windows.Forms.Label lblIntroduccion;
        private System.Windows.Forms.TextBox txtBuscar;
        private System.Windows.Forms.ListBox lstRecientes;
        private System.Windows.Forms.ContextMenuStrip cmsProyecto;
        private System.Windows.Forms.ToolStripMenuItem miQuitar;
        private System.Windows.Forms.ToolStripMenuItem miAbrirCarpeta;
        private System.Windows.Forms.Label lblSinRecientes;
        private AsmEditor.BotonAccion btnCrear;
        private AsmEditor.BotonAccion btnAbrirProyecto;
        private AsmEditor.BotonAccion btnAbrirCarpeta;
        private AsmEditor.BotonAccion btnAbrirArchivo;
        private System.Windows.Forms.Button btnContinuar;
    }
}
