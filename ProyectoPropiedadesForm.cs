using AsmEditor.Core;
using AsmEditor.Core.Proyecto;

namespace AsmEditor;

/// <summary>
/// Propiedades del proyecto: nombre, archivo que se compila, carpeta de salida
/// y qué targets usa.
///
/// Escribe sobre el proyecto solo al aceptar: trabaja sobre una copia, así
/// cancelar no deja cambios a medias.
/// </summary>
public sealed class ProyectoPropiedadesForm : Form
{
    private readonly ProyectoAsm _original;
    private readonly ProyectoAsm _copia;
    private readonly List<BuildTarget> _globales;

    private readonly TextBox _nombre = new();
    private readonly ComboBox _principal = new();
    private readonly TextBox _salida = new();
    private readonly Label _salidaEjemplo = new();
    private readonly CheckBox _heredaTargets = new();
    private readonly ComboBox _targetActivo = new();
    private readonly Label _avisos = new();

    /// <summary>Evita que rellenar los campos dispare los eventos de edición.</summary>
    private bool _cargando;

    public ProyectoPropiedadesForm(ProyectoAsm proyecto, List<BuildTarget> targetsGlobales)
    {
        _original = proyecto;
        _copia = proyecto.Clonar();
        _globales = targetsGlobales;

        Text = "Propiedades del proyecto";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(560, 400);

        ArmarLayout();
        Cargar();
        AplicarTema();
    }

    private void ArmarLayout()
    {
        int y = 16;

        // ---- Nombre ----
        Controls.Add(new Label { Text = "Nombre:", Left = 16, Top = y + 3, Width = 130 });
        _nombre.Left = 150;
        _nombre.Top = y;
        _nombre.Width = 390;
        Controls.Add(_nombre);

        y += 34;

        // ---- Archivo principal ----
        Controls.Add(new Label { Text = "Compila (F7):", Left = 16, Top = y + 3, Width = 130 });
        _principal.Left = 150;
        _principal.Top = y;
        _principal.Width = 390;
        _principal.DropDownStyle = ComboBoxStyle.DropDownList;
        _principal.SelectedIndexChanged += (_, _) => AlCambiarPrincipal();
        Controls.Add(_principal);

        y += 26;

        Controls.Add(new Label
        {
            Text = "Es el que se ensambla, sin importar qué pestaña estés mirando.",
            Left = 150,
            Top = y,
            Width = 390,
            ForeColor = SystemColors.GrayText
        });

        y += 32;

        // ---- Carpeta de salida ----
        Controls.Add(new Label { Text = "Salida (.obj/.exe):", Left = 16, Top = y + 3, Width = 130 });
        _salida.Left = 150;
        _salida.Top = y;
        _salida.Width = 390;
        _salida.TextChanged += (_, _) => AlCambiarSalida();
        Controls.Add(_salida);

        y += 26;

        _salidaEjemplo.Left = 150;
        _salidaEjemplo.Top = y;
        _salidaEjemplo.Width = 390;
        _salidaEjemplo.Height = 32;
        _salidaEjemplo.ForeColor = SystemColors.GrayText;
        Controls.Add(_salidaEjemplo);

        y += 44;

        // ---- Targets ----
        var linea = new Label
        {
            Left = 16,
            Top = y,
            Width = 524,
            Height = 2,
            BorderStyle = BorderStyle.Fixed3D
        };
        Controls.Add(linea);

        y += 14;

        _heredaTargets.Left = 16;
        _heredaTargets.Top = y;
        _heredaTargets.Width = 524;
        _heredaTargets.Text = "Usar los targets de la configuración general";
        _heredaTargets.CheckedChanged += (_, _) => AlCambiarHerencia();
        Controls.Add(_heredaTargets);

        y += 24;

        Controls.Add(new Label
        {
            Text = "Sin esto, el proyecto lleva su propia copia y deja de recibir los\n" +
                   "cambios que hagas en Compilar > Administrar targets.",
            Left = 36,
            Top = y,
            Width = 504,
            Height = 32,
            ForeColor = SystemColors.GrayText
        });

        y += 40;

        Controls.Add(new Label { Text = "Target activo:", Left = 16, Top = y + 3, Width = 130 });
        _targetActivo.Left = 150;
        _targetActivo.Top = y;
        _targetActivo.Width = 390;
        _targetActivo.DropDownStyle = ComboBoxStyle.DropDownList;
        _targetActivo.SelectedIndexChanged += (_, _) => AlCambiarTarget();
        Controls.Add(_targetActivo);

        y += 36;

        // ---- Avisos ----
        _avisos.Left = 16;
        _avisos.Top = y;
        _avisos.Width = 524;
        _avisos.Height = 40;
        Controls.Add(_avisos);

        // ---- Botones ----
        var aceptar = new Button
        {
            Text = "Aceptar",
            DialogResult = DialogResult.OK,
            Left = 374,
            Top = ClientSize.Height - 40,
            Width = 80
        };
        aceptar.Click += (_, _) => Aplicar();

        var cancelar = new Button
        {
            Text = "Cancelar",
            DialogResult = DialogResult.Cancel,
            Left = 460,
            Top = ClientSize.Height - 40,
            Width = 80
        };

        Controls.Add(aceptar);
        Controls.Add(cancelar);

        AcceptButton = aceptar;
        CancelButton = cancelar;
    }

    private void Cargar()
    {
        _cargando = true;

        try
        {
            _nombre.Text = _copia.Nombre;
            _salida.Text = _copia.CarpetaSalida;
            _heredaTargets.Checked = _copia.HeredaTargets;

            // Solo los .asm pueden ser el principal: el resto no se ensambla.
            _principal.Items.Clear();
            _principal.Items.Add("(ninguno)");

            foreach (var a in _copia.ListarArchivos().Where(a => a.Extension == ".asm"))
            {
                _principal.Items.Add(a.Relativa);
            }

            var i = _principal.Items.IndexOf(_copia.ArchivoPrincipal);
            _principal.SelectedIndex = i >= 0 ? i : 0;

            RecargarTargets();
        }
        finally { _cargando = false; }

        ActualizarEjemplo();
        ActualizarAvisos();
    }

    private void RecargarTargets()
    {
        var lista = _copia.TargetsEfectivos(_globales);

        _targetActivo.Items.Clear();
        foreach (var t in lista) _targetActivo.Items.Add(t.DisplayName);

        if (_targetActivo.Items.Count > 0)
        {
            _targetActivo.SelectedIndex = Math.Clamp(_copia.TargetActivo, 0, _targetActivo.Items.Count - 1);
        }
    }

    private void AlCambiarPrincipal()
    {
        if (_cargando) return;

        _copia.ArchivoPrincipal = _principal.SelectedIndex <= 0
            ? ""
            : (string)_principal.SelectedItem!;

        ActualizarEjemplo();
        ActualizarAvisos();
    }

    private void AlCambiarSalida()
    {
        if (_cargando) return;

        _copia.CarpetaSalida = _salida.Text.Trim();
        ActualizarEjemplo();
    }

    private void AlCambiarHerencia()
    {
        if (_cargando) return;

        if (_heredaTargets.Checked)
        {
            // Volver a heredar tira la copia propia: es lo que significa heredar.
            _copia.Targets.Clear();
        }
        else if (_copia.Targets.Count == 0)
        {
            // Se copian los globales como punto de partida, para no dejar al
            // proyecto sin ningún target con el que compilar.
            _copia.Targets = _globales.Select(t => t.Clone()).ToList();
        }

        _cargando = true;
        try { RecargarTargets(); }
        finally { _cargando = false; }

        ActualizarAvisos();
    }

    private void AlCambiarTarget()
    {
        if (_cargando) return;
        _copia.TargetActivo = Math.Max(0, _targetActivo.SelectedIndex);
    }

    /// <summary>
    /// Muestra adónde va a ir el .exe con lo que está configurado. Es la forma
    /// de que se entienda qué significa dejar la carpeta vacía.
    /// </summary>
    private void ActualizarEjemplo()
    {
        var principal = _copia.RutaPrincipal;

        if (principal is null)
        {
            _salidaEjemplo.Text = "Vacío = al lado del fuente (como siempre hizo el editor).";
            return;
        }

        var exe = _copia.RutaDeSalida(principal, ".exe");

        _salidaEjemplo.Text = string.IsNullOrWhiteSpace(_copia.CarpetaSalida)
            ? $"Vacío = al lado del fuente:  {exe}"
            : $"Va a quedar en:  {exe}";
    }

    private void ActualizarAvisos()
    {
        var problemas = _copia.Validar();

        if (problemas.Count == 0)
        {
            _avisos.ForeColor = Tema.Ok;
            _avisos.Text = "El proyecto está bien.";
            return;
        }

        _avisos.ForeColor = Tema.Aviso;
        _avisos.Text = problemas.Count == 1
            ? problemas[0]
            : $"{problemas.Count} avisos: {problemas[0]}";
    }

    /// <summary>Copia lo editado al proyecto de verdad. Solo se llama al aceptar.</summary>
    private void Aplicar()
    {
        _original.Nombre = string.IsNullOrWhiteSpace(_nombre.Text) ? "Proyecto" : _nombre.Text.Trim();
        _original.ArchivoPrincipal = _copia.ArchivoPrincipal;
        _original.CarpetaSalida = _copia.CarpetaSalida;
        _original.Targets = _copia.Targets;
        _original.TargetActivo = _copia.TargetActivo;
    }

    private void AplicarTema()
    {
        BackColor = Tema.Fondo;
        ForeColor = Tema.Texto;

        foreach (Control c in Controls)
        {
            switch (c)
            {
                case TextBox or ComboBox:
                    c.BackColor = Tema.Superficie;
                    c.ForeColor = Tema.Texto;
                    break;

                case Button:
                    c.BackColor = Tema.Superficie3;
                    c.ForeColor = Tema.Texto;
                    break;

                case Label l when l.ForeColor != SystemColors.GrayText
                               && l != _avisos:
                    l.ForeColor = Tema.Texto;
                    break;

                case CheckBox:
                    c.ForeColor = Tema.Texto;
                    break;
            }
        }
    }
}
