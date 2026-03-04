namespace CopilotRemap;

/// <summary>
/// Minimal text input dialog — used for custom URL/command entry.
/// </summary>
public sealed class InputDialog : Form
{
    private readonly TextBox _textBox;

    public string Value => _textBox.Text;

    public InputDialog(string title, string prompt, string defaultValue = "")
    {
        Text = title;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(400, 120);

        var label = new Label
        {
            Text = prompt,
            Location = new Point(12, 12),
            AutoSize = true
        };

        _textBox = new TextBox
        {
            Text = defaultValue,
            Location = new Point(12, 36),
            Width = 370
        };

        var ok = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Location = new Point(220, 76),
            Width = 75
        };

        var cancel = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Location = new Point(305, 76),
            Width = 75
        };

        AcceptButton = ok;
        CancelButton = cancel;
        Controls.AddRange([label, _textBox, ok, cancel]);
    }
}
