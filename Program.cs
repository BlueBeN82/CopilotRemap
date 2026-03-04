namespace CopilotRemap;

static class Program
{
    [STAThread]
    static void Main()
    {
        // Single instance — don't allow multiple copies
        using var mutex = new Mutex(true, "CopilotRemap_SingleInstance", out bool isNew);
        if (!isNew)
        {
            MessageBox.Show("CopilotRemap is already running.", "CopilotRemap",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new TrayApp());
    }
}
