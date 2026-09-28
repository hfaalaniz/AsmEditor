namespace AsmEditor
{
    partial class AnfitrionAcople
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
            this.pnlDesplegado = new System.Windows.Forms.Panel();
            this.bordeIzquierda = new AsmEditor.BordeAutoOcultos();
            this.bordeDerecha = new AsmEditor.BordeAutoOcultos();
            this.bordeAbajo = new AsmEditor.BordeAutoOcultos();
            this.tmrDesplegar = new System.Windows.Forms.Timer(this.components);
            this.tmrPlegar = new System.Windows.Forms.Timer(this.components);
            this.zonaIzquierda = new System.Windows.Forms.Panel();
            this.grupoIzquierda = new AsmEditor.GrupoHerramientas();
            this.divisorIzquierda = new System.Windows.Forms.Splitter();
            this.zonaDerecha = new System.Windows.Forms.Panel();
            this.grupoDerecha = new AsmEditor.GrupoHerramientas();
            this.divisorDerecha = new System.Windows.Forms.Splitter();
            this.zonaAbajo = new System.Windows.Forms.Panel();
            this.grupoAbajo = new AsmEditor.GrupoHerramientas();
            this.divisorAbajo = new System.Windows.Forms.Splitter();
            this.pnlCentro = new System.Windows.Forms.Panel();
            this.zonaIzquierda.SuspendLayout();
            this.zonaDerecha.SuspendLayout();
            this.zonaAbajo.SuspendLayout();
            this.SuspendLayout();
            //
            // zonaIzquierda
            //
            this.zonaIzquierda.Controls.Add(this.grupoIzquierda);
            this.zonaIzquierda.Dock = System.Windows.Forms.DockStyle.Left;
            this.zonaIzquierda.Location = new System.Drawing.Point(0, 0);
            this.zonaIzquierda.Name = "zonaIzquierda";
            this.zonaIzquierda.Size = new System.Drawing.Size(240, 700);
            this.zonaIzquierda.TabIndex = 0;
            //
            // grupoIzquierda
            //
            this.grupoIzquierda.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grupoIzquierda.Location = new System.Drawing.Point(0, 0);
            this.grupoIzquierda.Name = "grupoIzquierda";
            this.grupoIzquierda.Size = new System.Drawing.Size(240, 700);
            this.grupoIzquierda.TabIndex = 0;
            this.grupoIzquierda.PestanaElegida += new System.EventHandler<AsmEditor.VentanaHerramienta>(this.Grupo_PestanaElegida);
            //
            // divisorIzquierda
            //
            this.divisorIzquierda.Dock = System.Windows.Forms.DockStyle.Left;
            this.divisorIzquierda.Location = new System.Drawing.Point(240, 0);
            this.divisorIzquierda.MinExtra = 200;
            this.divisorIzquierda.MinSize = 120;
            this.divisorIzquierda.Name = "divisorIzquierda";
            this.divisorIzquierda.Size = new System.Drawing.Size(5, 700);
            this.divisorIzquierda.TabIndex = 1;
            this.divisorIzquierda.TabStop = false;
            this.divisorIzquierda.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.Divisor_SplitterMoved);
            //
            // zonaDerecha
            //
            this.zonaDerecha.Controls.Add(this.grupoDerecha);
            this.zonaDerecha.Dock = System.Windows.Forms.DockStyle.Right;
            this.zonaDerecha.Location = new System.Drawing.Point(720, 0);
            this.zonaDerecha.Name = "zonaDerecha";
            this.zonaDerecha.Size = new System.Drawing.Size(280, 700);
            this.zonaDerecha.TabIndex = 2;
            //
            // grupoDerecha
            //
            this.grupoDerecha.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grupoDerecha.Location = new System.Drawing.Point(0, 0);
            this.grupoDerecha.Name = "grupoDerecha";
            this.grupoDerecha.Size = new System.Drawing.Size(280, 700);
            this.grupoDerecha.TabIndex = 0;
            this.grupoDerecha.PestanaElegida += new System.EventHandler<AsmEditor.VentanaHerramienta>(this.Grupo_PestanaElegida);
            //
            // divisorDerecha
            //
            this.divisorDerecha.Dock = System.Windows.Forms.DockStyle.Right;
            this.divisorDerecha.Location = new System.Drawing.Point(715, 0);
            this.divisorDerecha.MinExtra = 200;
            this.divisorDerecha.MinSize = 120;
            this.divisorDerecha.Name = "divisorDerecha";
            this.divisorDerecha.Size = new System.Drawing.Size(5, 700);
            this.divisorDerecha.TabIndex = 3;
            this.divisorDerecha.TabStop = false;
            this.divisorDerecha.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.Divisor_SplitterMoved);
            //
            // zonaAbajo
            //
            this.zonaAbajo.Controls.Add(this.grupoAbajo);
            this.zonaAbajo.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.zonaAbajo.Location = new System.Drawing.Point(245, 480);
            this.zonaAbajo.Name = "zonaAbajo";
            this.zonaAbajo.Size = new System.Drawing.Size(470, 220);
            this.zonaAbajo.TabIndex = 4;
            //
            // grupoAbajo
            //
            this.grupoAbajo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grupoAbajo.Location = new System.Drawing.Point(0, 0);
            this.grupoAbajo.Name = "grupoAbajo";
            this.grupoAbajo.Size = new System.Drawing.Size(470, 220);
            this.grupoAbajo.TabIndex = 0;
            this.grupoAbajo.PestanaElegida += new System.EventHandler<AsmEditor.VentanaHerramienta>(this.Grupo_PestanaElegida);
            //
            // divisorAbajo
            //
            this.divisorAbajo.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.divisorAbajo.Location = new System.Drawing.Point(245, 475);
            this.divisorAbajo.MinExtra = 120;
            this.divisorAbajo.MinSize = 120;
            this.divisorAbajo.Name = "divisorAbajo";
            this.divisorAbajo.Size = new System.Drawing.Size(470, 5);
            this.divisorAbajo.TabIndex = 5;
            this.divisorAbajo.TabStop = false;
            this.divisorAbajo.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.Divisor_SplitterMoved);
            //
            // pnlCentro
            //
            this.pnlCentro.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlCentro.Location = new System.Drawing.Point(245, 0);
            this.pnlCentro.Name = "pnlCentro";
            this.pnlCentro.Size = new System.Drawing.Size(470, 475);
            this.pnlCentro.TabIndex = 6;
            //
            // pnlDesplegado
            //
            this.pnlDesplegado.Location = new System.Drawing.Point(420, 0);
            this.pnlDesplegado.Name = "pnlDesplegado";
            this.pnlDesplegado.Padding = new System.Windows.Forms.Padding(1);
            this.pnlDesplegado.Size = new System.Drawing.Size(280, 470);
            this.pnlDesplegado.TabIndex = 7;
            this.pnlDesplegado.Visible = false;
            //
            // bordeIzquierda
            //
            this.bordeIzquierda.Dock = System.Windows.Forms.DockStyle.Left;
            this.bordeIzquierda.Lado = AsmEditor.Core.Acople.ZonaAcople.Izquierda;
            this.bordeIzquierda.Location = new System.Drawing.Point(0, 0);
            this.bordeIzquierda.Name = "bordeIzquierda";
            this.bordeIzquierda.Size = new System.Drawing.Size(24, 700);
            this.bordeIzquierda.TabIndex = 8;
            this.bordeIzquierda.Visible = false;
            this.bordeIzquierda.PestanaSenalada += new System.EventHandler<string>(this.Borde_PestanaSenalada);
            this.bordeIzquierda.PestanaDejada += new System.EventHandler<string>(this.Borde_PestanaDejada);
            this.bordeIzquierda.PestanaElegida += new System.EventHandler<string>(this.Borde_PestanaElegida);
            //
            // bordeDerecha
            //
            this.bordeDerecha.Dock = System.Windows.Forms.DockStyle.Right;
            this.bordeDerecha.Lado = AsmEditor.Core.Acople.ZonaAcople.Derecha;
            this.bordeDerecha.Location = new System.Drawing.Point(976, 0);
            this.bordeDerecha.Name = "bordeDerecha";
            this.bordeDerecha.Size = new System.Drawing.Size(24, 700);
            this.bordeDerecha.TabIndex = 9;
            this.bordeDerecha.Visible = false;
            this.bordeDerecha.PestanaSenalada += new System.EventHandler<string>(this.Borde_PestanaSenalada);
            this.bordeDerecha.PestanaDejada += new System.EventHandler<string>(this.Borde_PestanaDejada);
            this.bordeDerecha.PestanaElegida += new System.EventHandler<string>(this.Borde_PestanaElegida);
            //
            // bordeAbajo
            //
            this.bordeAbajo.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.bordeAbajo.Lado = AsmEditor.Core.Acople.ZonaAcople.Abajo;
            this.bordeAbajo.Location = new System.Drawing.Point(0, 676);
            this.bordeAbajo.Name = "bordeAbajo";
            this.bordeAbajo.Size = new System.Drawing.Size(1000, 24);
            this.bordeAbajo.TabIndex = 10;
            this.bordeAbajo.Visible = false;
            this.bordeAbajo.PestanaSenalada += new System.EventHandler<string>(this.Borde_PestanaSenalada);
            this.bordeAbajo.PestanaDejada += new System.EventHandler<string>(this.Borde_PestanaDejada);
            this.bordeAbajo.PestanaElegida += new System.EventHandler<string>(this.Borde_PestanaElegida);
            //
            // tmrDesplegar
            //
            this.tmrDesplegar.Interval = 400;
            this.tmrDesplegar.Tick += new System.EventHandler(this.tmrDesplegar_Tick);
            //
            // tmrPlegar
            //
            this.tmrPlegar.Interval = 300;
            this.tmrPlegar.Tick += new System.EventHandler(this.tmrPlegar_Tick);
            //
            // AnfitrionAcople
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pnlDesplegado);
            this.Controls.Add(this.pnlCentro);
            this.Controls.Add(this.divisorAbajo);
            this.Controls.Add(this.zonaAbajo);
            this.Controls.Add(this.divisorDerecha);
            this.Controls.Add(this.zonaDerecha);
            this.Controls.Add(this.divisorIzquierda);
            this.Controls.Add(this.zonaIzquierda);
            this.Controls.Add(this.bordeAbajo);
            this.Controls.Add(this.bordeDerecha);
            this.Controls.Add(this.bordeIzquierda);
            this.Name = "AnfitrionAcople";
            this.Size = new System.Drawing.Size(1000, 700);
            this.SizeChanged += new System.EventHandler(this.AnfitrionAcople_SizeChanged);
            this.zonaIzquierda.ResumeLayout(false);
            this.zonaDerecha.ResumeLayout(false);
            this.zonaAbajo.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel zonaIzquierda;
        private AsmEditor.GrupoHerramientas grupoIzquierda;
        private System.Windows.Forms.Splitter divisorIzquierda;
        private System.Windows.Forms.Panel zonaDerecha;
        private AsmEditor.GrupoHerramientas grupoDerecha;
        private System.Windows.Forms.Splitter divisorDerecha;
        private System.Windows.Forms.Panel zonaAbajo;
        private AsmEditor.GrupoHerramientas grupoAbajo;
        private System.Windows.Forms.Splitter divisorAbajo;
        private System.Windows.Forms.Panel pnlCentro;
        private System.Windows.Forms.Panel pnlDesplegado;
        private AsmEditor.BordeAutoOcultos bordeIzquierda;
        private AsmEditor.BordeAutoOcultos bordeDerecha;
        private AsmEditor.BordeAutoOcultos bordeAbajo;
        private System.Windows.Forms.Timer tmrDesplegar;
        private System.Windows.Forms.Timer tmrPlegar;
    }
}
