namespace AsmEditor
{
    partial class BarraEstado
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
            this.lblIcono = new System.Windows.Forms.Label();
            this.lblEstado = new System.Windows.Forms.Label();
            this.lblErrores = new System.Windows.Forms.Label();
            this.lblAdvertencias = new System.Windows.Forms.Label();
            this.lblPosicion = new System.Windows.Forms.Label();
            this.lblTarget = new System.Windows.Forms.Label();
            this.menuTargets = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tmrActividad = new System.Windows.Forms.Timer(this.components);
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            //
            // lblIcono
            //
            this.lblIcono.Location = new System.Drawing.Point(8, 4);
            this.lblIcono.Name = "lblIcono";
            this.lblIcono.Size = new System.Drawing.Size(16, 16);
            this.lblIcono.TabIndex = 0;
            //
            // lblEstado
            //
            this.lblEstado.AutoEllipsis = true;
            this.lblEstado.Location = new System.Drawing.Point(28, 4);
            this.lblEstado.Name = "lblEstado";
            this.lblEstado.Size = new System.Drawing.Size(560, 16);
            this.lblEstado.TabIndex = 1;
            this.lblEstado.Text = "Listo";
            this.lblEstado.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblErrores
            //
            this.lblErrores.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblErrores.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblErrores.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblErrores.Location = new System.Drawing.Point(600, 4);
            this.lblErrores.Name = "lblErrores";
            this.lblErrores.Size = new System.Drawing.Size(48, 16);
            this.lblErrores.TabIndex = 2;
            this.lblErrores.Text = "0";
            this.lblErrores.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblErrores.Click += new System.EventHandler(this.lblDiagnosticos_Click);
            //
            // lblAdvertencias
            //
            this.lblAdvertencias.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblAdvertencias.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblAdvertencias.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblAdvertencias.Location = new System.Drawing.Point(652, 4);
            this.lblAdvertencias.Name = "lblAdvertencias";
            this.lblAdvertencias.Size = new System.Drawing.Size(48, 16);
            this.lblAdvertencias.TabIndex = 3;
            this.lblAdvertencias.Text = "0";
            this.lblAdvertencias.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblAdvertencias.Click += new System.EventHandler(this.lblDiagnosticos_Click);
            //
            // lblPosicion
            //
            this.lblPosicion.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblPosicion.Location = new System.Drawing.Point(716, 4);
            this.lblPosicion.Name = "lblPosicion";
            this.lblPosicion.Size = new System.Drawing.Size(110, 16);
            this.lblPosicion.TabIndex = 4;
            this.lblPosicion.Text = "Ln 1, Col 1";
            this.lblPosicion.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // lblTarget
            //
            this.lblTarget.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblTarget.AutoEllipsis = true;
            this.lblTarget.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblTarget.Location = new System.Drawing.Point(834, 4);
            this.lblTarget.Name = "lblTarget";
            this.lblTarget.Size = new System.Drawing.Size(158, 16);
            this.lblTarget.TabIndex = 5;
            this.lblTarget.Text = "Target";
            this.lblTarget.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblTarget.Click += new System.EventHandler(this.lblTarget_Click);
            //
            // menuTargets
            //
            this.menuTargets.Name = "menuTargets";
            this.menuTargets.Size = new System.Drawing.Size(61, 4);
            //
            // tmrActividad
            //
            this.tmrActividad.Interval = 90;
            this.tmrActividad.Tick += new System.EventHandler(this.tmrActividad_Tick);
            //
            // BarraEstado
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lblTarget);
            this.Controls.Add(this.lblPosicion);
            this.Controls.Add(this.lblAdvertencias);
            this.Controls.Add(this.lblErrores);
            this.Controls.Add(this.lblEstado);
            this.Controls.Add(this.lblIcono);
            this.Name = "BarraEstado";
            this.Size = new System.Drawing.Size(1000, 24);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.BarraEstado_Paint);
            this.Resize += new System.EventHandler(this.BarraEstado_Resize);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Label lblIcono;
        private System.Windows.Forms.Label lblEstado;
        private System.Windows.Forms.Label lblErrores;
        private System.Windows.Forms.Label lblAdvertencias;
        private System.Windows.Forms.Label lblPosicion;
        private System.Windows.Forms.Label lblTarget;
        private System.Windows.Forms.ContextMenuStrip menuTargets;
        private System.Windows.Forms.Timer tmrActividad;
        private System.Windows.Forms.ToolTip toolTip;
    }
}
