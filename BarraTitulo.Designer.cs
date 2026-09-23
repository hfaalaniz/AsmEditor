namespace AsmEditor
{
    partial class BarraTitulo
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
            this.lblLogo = new AsmEditor.EtiquetaTitulo();
            this.menu = new System.Windows.Forms.MenuStrip();
            this.txtBuscar = new System.Windows.Forms.TextBox();
            this.lblInsignia = new AsmEditor.EtiquetaTitulo();
            this.btnMinimizar = new System.Windows.Forms.Button();
            this.btnMaximizar = new System.Windows.Forms.Button();
            this.btnCerrar = new System.Windows.Forms.Button();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            //
            // lblLogo
            //
            this.lblLogo.Location = new System.Drawing.Point(8, 6);
            this.lblLogo.Name = "lblLogo";
            this.lblLogo.Size = new System.Drawing.Size(20, 20);
            this.lblLogo.TabIndex = 0;
            //
            // menu
            //
            this.menu.AutoSize = true;
            this.menu.Dock = System.Windows.Forms.DockStyle.None;
            this.menu.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.menu.Location = new System.Drawing.Point(34, 4);
            this.menu.Name = "menu";
            this.menu.Padding = new System.Windows.Forms.Padding(0);
            this.menu.Size = new System.Drawing.Size(202, 24);
            this.menu.TabIndex = 1;
            this.menu.SizeChanged += new System.EventHandler(this.menu_SizeChanged);
            //
            // txtBuscar
            //
            this.txtBuscar.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtBuscar.Location = new System.Drawing.Point(250, 5);
            this.txtBuscar.Name = "txtBuscar";
            this.txtBuscar.PlaceholderText = "Buscar (Ctrl+Q)";
            this.txtBuscar.Size = new System.Drawing.Size(240, 23);
            this.txtBuscar.TabIndex = 2;
            this.txtBuscar.TabStop = false;
            this.txtBuscar.TextChanged += new System.EventHandler(this.txtBuscar_TextChanged);
            this.txtBuscar.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtBuscar_KeyDown);
            this.txtBuscar.Leave += new System.EventHandler(this.txtBuscar_Leave);
            //
            // lblInsignia
            //
            this.lblInsignia.AutoSize = true;
            this.lblInsignia.ConFondo = true;
            this.lblInsignia.DejaPasarRaton = false;
            this.lblInsignia.Location = new System.Drawing.Point(504, 7);
            this.lblInsignia.Name = "lblInsignia";
            this.lblInsignia.Padding = new System.Windows.Forms.Padding(8, 2, 8, 2);
            this.lblInsignia.Size = new System.Drawing.Size(80, 19);
            this.lblInsignia.TabIndex = 3;
            this.lblInsignia.Text = "Proyecto";
            //
            // btnMinimizar
            //
            this.btnMinimizar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnMinimizar.FlatAppearance.BorderSize = 0;
            this.btnMinimizar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMinimizar.Font = new System.Drawing.Font("Segoe MDL2 Assets", 8F);
            this.btnMinimizar.Location = new System.Drawing.Point(862, 0);
            this.btnMinimizar.Name = "btnMinimizar";
            this.btnMinimizar.Size = new System.Drawing.Size(46, 32);
            this.btnMinimizar.TabIndex = 4;
            this.btnMinimizar.TabStop = false;
            this.btnMinimizar.Text = "\uE921";
            this.btnMinimizar.UseVisualStyleBackColor = false;
            this.btnMinimizar.Click += new System.EventHandler(this.btnMinimizar_Click);
            //
            // btnMaximizar
            //
            this.btnMaximizar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnMaximizar.FlatAppearance.BorderSize = 0;
            this.btnMaximizar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMaximizar.Font = new System.Drawing.Font("Segoe MDL2 Assets", 8F);
            this.btnMaximizar.Location = new System.Drawing.Point(908, 0);
            this.btnMaximizar.Name = "btnMaximizar";
            this.btnMaximizar.Size = new System.Drawing.Size(46, 32);
            this.btnMaximizar.TabIndex = 5;
            this.btnMaximizar.TabStop = false;
            this.btnMaximizar.Text = "\uE922";
            this.btnMaximizar.UseVisualStyleBackColor = false;
            this.btnMaximizar.Click += new System.EventHandler(this.btnMaximizar_Click);
            //
            // btnCerrar
            //
            this.btnCerrar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCerrar.FlatAppearance.BorderSize = 0;
            this.btnCerrar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCerrar.Font = new System.Drawing.Font("Segoe MDL2 Assets", 8F);
            this.btnCerrar.Location = new System.Drawing.Point(954, 0);
            this.btnCerrar.Name = "btnCerrar";
            this.btnCerrar.Size = new System.Drawing.Size(46, 32);
            this.btnCerrar.TabIndex = 6;
            this.btnCerrar.TabStop = false;
            this.btnCerrar.Text = "\uE8BB";
            this.btnCerrar.UseVisualStyleBackColor = false;
            this.btnCerrar.Click += new System.EventHandler(this.btnCerrar_Click);
            //
            // BarraTitulo
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.btnCerrar);
            this.Controls.Add(this.btnMaximizar);
            this.Controls.Add(this.btnMinimizar);
            this.Controls.Add(this.lblInsignia);
            this.Controls.Add(this.txtBuscar);
            this.Controls.Add(this.menu);
            this.Controls.Add(this.lblLogo);
            this.Name = "BarraTitulo";
            this.Size = new System.Drawing.Size(1000, 32);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private AsmEditor.EtiquetaTitulo lblLogo;
        private System.Windows.Forms.MenuStrip menu;
        private System.Windows.Forms.TextBox txtBuscar;
        private AsmEditor.EtiquetaTitulo lblInsignia;
        private System.Windows.Forms.Button btnMinimizar;
        private System.Windows.Forms.Button btnMaximizar;
        private System.Windows.Forms.Button btnCerrar;
        private System.Windows.Forms.ToolTip toolTip;
    }
}
