using System.Drawing.Drawing2D;

namespace CopilotRemap;

public static class IconHelper
{
    /// <summary>
    /// Generates a keyboard key icon at runtime — no .ico file needed.
    /// Draws a keycap shape with a remap arrow symbol inside.
    /// </summary>
    public static Icon CreateTrayIcon()
    {
        const int size = 32;
        using var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        g.Clear(Color.Transparent);

        // Key cap body — indigo/purple
        var keyRect = new Rectangle(1, 1, size - 2, size - 2);
        using var bodyPath = RoundedRect(keyRect, 6);
        using var bodyBrush = new SolidBrush(Color.FromArgb(79, 70, 229)); // indigo-600
        g.FillPath(bodyBrush, bodyPath);

        // Top highlight — subtle 3D keycap look
        var highlightRect = new Rectangle(3, 3, size - 6, (size - 6) / 2);
        using var highlightPath = RoundedRect(highlightRect, 4);
        using var highlightBrush = new SolidBrush(Color.FromArgb(40, 255, 255, 255));
        g.FillPath(highlightBrush, highlightPath);

        // Draw remap arrow "⟳" style — a curved arrow
        using var arrowPen = new Pen(Color.White, 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Custom, CustomEndCap = new AdjustableArrowCap(3, 3) };
        var arcRect = new Rectangle(8, 8, 16, 16);
        g.DrawArc(arrowPen, arcRect, 200, 280);

        return Icon.FromHandle(bmp.GetHicon());
    }

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2;
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
