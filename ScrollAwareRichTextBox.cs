namespace AsmEditor;

/// <summary>
/// RichTextBox que expone un evento cuando el contenido visible cambia
/// (scroll con la rueda, barra de desplazamiento, flechas, Page Up/Down, etc.).
/// Se usa para mantener sincronizado el panel de números de línea.
/// </summary>
public class ScrollAwareRichTextBox : RichTextBox
{
    private const int WM_VSCROLL = 0x0115;
    private const int WM_MOUSEWHEEL = 0x020A;
    private const int WM_KEYDOWN = 0x0100;

    public event EventHandler? ContentScrolled;

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);

        if (m.Msg == WM_VSCROLL || m.Msg == WM_MOUSEWHEEL)
        {
            ContentScrolled?.Invoke(this, EventArgs.Empty);
        }
        else if (m.Msg == WM_KEYDOWN)
        {
            var key = (Keys)m.WParam.ToInt32();
            if (key is Keys.Up or Keys.Down or Keys.PageUp or Keys.PageDown or Keys.Home or Keys.End)
            {
                ContentScrolled?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
