namespace AsmEditor
{
    partial class FormOpciones
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
            this.tvCategorias = new System.Windows.Forms.TreeView();
            this.lblTituloPagina = new System.Windows.Forms.Label();
            this.pnlPaginas = new System.Windows.Forms.Panel();
            this.paginaGeneral = new AsmEditor.PaginaOpcionesGeneral();
            this.paginaProyectos = new AsmEditor.PaginaOpcionesProyectos();
            this.paginaNasmGoLink = new AsmEditor.PaginaOpcionesNasmGoLink();
            this.paginaMsvc = new AsmEditor.PaginaOpcionesMsvc();
            this.btnAceptar = new System.Windows.Forms.Button();
            this.btnCancelar = new System.Windows.Forms.Button();
            this.pnlPaginas.SuspendLayout();
            this.SuspendLayout();
            //
            // tvCategorias
            //
            this.tvCategorias.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.tvCategorias.FullRowSelect = true;
            this.tvCategorias.HideSelection = false;
            this.tvCategorias.ItemHeight = 22;
            this.tvCategorias.Location = new System.Drawing.Point(12, 12);
            this.tvCategorias.Name = "tvCategorias";
            this.tvCategorias.ShowLines = false;
            this.tvCategorias.Size = new System.Drawing.Size(200, 430);
            this.tvCategorias.TabIndex = 0;
            this.tvCategorias.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.tvCategorias_AfterSelect);
            //
            // lblTituloPagina
            //
            this.lblTituloPagina.Font = new System.Drawing.Font("Segoe UI Semibold", 12F);
            this.lblTituloPagina.Location = new System.Drawing.Point(228, 10);
            this.lblTituloPagina.Name = "lblTituloPagina";
            this.lblTituloPagina.Size = new System.Drawing.Size(520, 26);
            this.lblTituloPagina.TabIndex = 1;
            this.lblTituloPagina.Text = "General";
            //
            // pnlPaginas
            //
            this.pnlPaginas.Controls.Add(this.paginaMsvc);
            this.pnlPaginas.Controls.Add(this.paginaNasmGoLink);
            this.pnlPaginas.Controls.Add(this.paginaProyectos);
            this.pnlPaginas.Controls.Add(this.paginaGeneral);
            this.pnlPaginas.Location = new System.Drawing.Point(228, 46);
            this.pnlPaginas.Name = "pnlPaginas";
            this.pnlPaginas.Size = new System.Drawing.Size(520, 396);
            this.pnlPaginas.TabIndex = 2;
            //
            // paginaGeneral
            //
            this.paginaGeneral.Location = new System.Drawing.Point(0, 0);
            this.paginaGeneral.Name = "paginaGeneral";
            this.paginaGeneral.Size = new System.Drawing.Size(520, 396);
            this.paginaGeneral.TabIndex = 0;
            //
            // paginaProyectos
            //
            this.paginaProyectos.Location = new System.Drawing.Point(0, 0);
            this.paginaProyectos.Name = "paginaProyectos";
            this.paginaProyectos.Size = new System.Drawing.Size(520, 396);
            this.paginaProyectos.TabIndex = 1;
            //
            // paginaNasmGoLink
            //
            this.paginaNasmGoLink.Location = new System.Drawing.Point(0, 0);
            this.paginaNasmGoLink.Name = "paginaNasmGoLink";
            this.paginaNasmGoLink.Size = new System.Drawing.Size(520, 396);
            this.paginaNasmGoLink.TabIndex = 2;
            //
            // paginaMsvc
            //
            this.paginaMsvc.Location = new System.Drawing.Point(0, 0);
            this.paginaMsvc.Name = "paginaMsvc";
            this.paginaMsvc.Size = new System.Drawing.Size(520, 396);
            this.paginaMsvc.TabIndex = 3;
            //
            // btnAceptar
            //
            this.btnAceptar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAceptar.Location = new System.Drawing.Point(572, 456);
            this.btnAceptar.Name = "btnAceptar";
            this.btnAceptar.Size = new System.Drawing.Size(84, 28);
            this.btnAceptar.TabIndex = 3;
            this.btnAceptar.Text = "Aceptar";
            this.btnAceptar.UseVisualStyleBackColor = false;
            this.btnAceptar.Click += new System.EventHandler(this.btnAceptar_Click);
            //
            // btnCancelar
            //
            this.btnCancelar.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancelar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancelar.Location = new System.Drawing.Point(664, 456);
            this.btnCancelar.Name = "btnCancelar";
            this.btnCancelar.Size = new System.Drawing.Size(84, 28);
            this.btnCancelar.TabIndex = 4;
            this.btnCancelar.Text = "Cancelar";
            this.btnCancelar.UseVisualStyleBackColor = false;
            //
            // FormOpciones
            //
            this.AcceptButton = this.btnAceptar;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancelar;
            this.ClientSize = new System.Drawing.Size(760, 496);
            this.Controls.Add(this.btnCancelar);
            this.Controls.Add(this.btnAceptar);
            this.Controls.Add(this.pnlPaginas);
            this.Controls.Add(this.lblTituloPagina);
            this.Controls.Add(this.tvCategorias);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormOpciones";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Opciones";
            this.pnlPaginas.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TreeView tvCategorias;
        private System.Windows.Forms.Label lblTituloPagina;
        private System.Windows.Forms.Panel pnlPaginas;
        private AsmEditor.PaginaOpcionesGeneral paginaGeneral;
        private AsmEditor.PaginaOpcionesProyectos paginaProyectos;
        private AsmEditor.PaginaOpcionesNasmGoLink paginaNasmGoLink;
        private AsmEditor.PaginaOpcionesMsvc paginaMsvc;
        private System.Windows.Forms.Button btnAceptar;
        private System.Windows.Forms.Button btnCancelar;
    }
}
