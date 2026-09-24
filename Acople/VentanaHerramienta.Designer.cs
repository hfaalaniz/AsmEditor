namespace AsmEditor
{
    partial class VentanaHerramienta
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
            this.components = new System.ComponentModel.Container();
            this.pnlTitulo = new System.Windows.Forms.Panel();
            this.lblTitulo = new System.Windows.Forms.Label();
            this.btnMenu = new System.Windows.Forms.Button();
            this.btnChincheta = new System.Windows.Forms.Button();
            this.btnCerrar = new System.Windows.Forms.Button();
            this.pnlContenido = new System.Windows.Forms.Panel();
            this.cmsVentana = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.miOcultar = new System.Windows.Forms.ToolStripMenuItem();
            this.pnlTitulo.SuspendLayout();
            this.cmsVentana.SuspendLayout();
            this.SuspendLayout();
            //
            // pnlTitulo
            //
            this.pnlTitulo.Controls.Add(this.btnCerrar);
            this.pnlTitulo.Controls.Add(this.btnChincheta);
            this.pnlTitulo.Controls.Add(this.btnMenu);
            this.pnlTitulo.Controls.Add(this.lblTitulo);
            this.pnlTitulo.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTitulo.Location = new System.Drawing.Point(0, 0);
            this.pnlTitulo.Name = "pnlTitulo";
            this.pnlTitulo.Size = new System.Drawing.Size(260, 26);
            this.pnlTitulo.TabIndex = 0;
            this.pnlTitulo.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Titulo_MouseDown);
            //
            // lblTitulo
            //
            this.lblTitulo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblTitulo.AutoEllipsis = true;
            this.lblTitulo.Location = new System.Drawing.Point(8, 0);
            this.lblTitulo.Name = "lblTitulo";
            this.lblTitulo.Size = new System.Drawing.Size(172, 26);
            this.lblTitulo.TabIndex = 0;
            this.lblTitulo.Text = "Herramienta";
            this.lblTitulo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblTitulo.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Titulo_MouseDown);
            //
            // btnMenu
            //
            this.btnMenu.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnMenu.FlatAppearance.BorderSize = 0;
            this.btnMenu.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMenu.Font = new System.Drawing.Font("Segoe MDL2 Assets", 7F);
            this.btnMenu.Location = new System.Drawing.Point(186, 2);
            this.btnMenu.Name = "btnMenu";
            this.btnMenu.Size = new System.Drawing.Size(22, 22);
            this.btnMenu.TabIndex = 1;
            this.btnMenu.TabStop = false;
            this.btnMenu.UseVisualStyleBackColor = false;
            this.btnMenu.Click += new System.EventHandler(this.btnMenu_Click);
            //
            // btnChincheta
            //
            this.btnChincheta.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnChincheta.FlatAppearance.BorderSize = 0;
            this.btnChincheta.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnChincheta.Font = new System.Drawing.Font("Segoe MDL2 Assets", 7F);
            this.btnChincheta.Location = new System.Drawing.Point(210, 2);
            this.btnChincheta.Name = "btnChincheta";
            this.btnChincheta.Size = new System.Drawing.Size(22, 22);
            this.btnChincheta.TabIndex = 2;
            this.btnChincheta.TabStop = false;
            this.btnChincheta.UseVisualStyleBackColor = false;
            this.btnChincheta.Visible = false;
            //
            // btnCerrar
            //
            this.btnCerrar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCerrar.FlatAppearance.BorderSize = 0;
            this.btnCerrar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCerrar.Font = new System.Drawing.Font("Segoe MDL2 Assets", 7F);
            this.btnCerrar.Location = new System.Drawing.Point(234, 2);
            this.btnCerrar.Name = "btnCerrar";
            this.btnCerrar.Size = new System.Drawing.Size(22, 22);
            this.btnCerrar.TabIndex = 3;
            this.btnCerrar.TabStop = false;
            this.btnCerrar.UseVisualStyleBackColor = false;
            this.btnCerrar.Click += new System.EventHandler(this.btnCerrar_Click);
            //
            // pnlContenido
            //
            this.pnlContenido.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlContenido.Location = new System.Drawing.Point(0, 26);
            this.pnlContenido.Name = "pnlContenido";
            this.pnlContenido.Size = new System.Drawing.Size(260, 274);
            this.pnlContenido.TabIndex = 1;
            //
            // cmsVentana
            //
            this.cmsVentana.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miOcultar});
            this.cmsVentana.Name = "cmsVentana";
            this.cmsVentana.Size = new System.Drawing.Size(120, 26);
            //
            // miOcultar
            //
            this.miOcultar.Name = "miOcultar";
            this.miOcultar.Size = new System.Drawing.Size(119, 22);
            this.miOcultar.Text = "Ocultar";
            this.miOcultar.Click += new System.EventHandler(this.miOcultar_Click);
            //
            // VentanaHerramienta
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pnlContenido);
            this.Controls.Add(this.pnlTitulo);
            this.Name = "VentanaHerramienta";
            this.Size = new System.Drawing.Size(260, 300);
            this.Enter += new System.EventHandler(this.VentanaHerramienta_Enter);
            this.Leave += new System.EventHandler(this.VentanaHerramienta_Leave);
            this.pnlTitulo.ResumeLayout(false);
            this.cmsVentana.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel pnlTitulo;
        private System.Windows.Forms.Label lblTitulo;
        private System.Windows.Forms.Button btnMenu;
        private System.Windows.Forms.Button btnChincheta;
        private System.Windows.Forms.Button btnCerrar;
        private System.Windows.Forms.Panel pnlContenido;
        private System.Windows.Forms.ContextMenuStrip cmsVentana;
        private System.Windows.Forms.ToolStripMenuItem miOcultar;
    }
}
