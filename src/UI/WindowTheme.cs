using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace UmiAchievementClaimer
{
internal static class WindowTheme
{
    private const int UseImmersiveDarkMode = 20;
    private const int LegacyUseImmersiveDarkMode = 19;
    private const int BorderColor = 34;
    private const int CaptionColor = 35;
    private const int TextColor = 36;

    [DllImport("dwmapi.dll", ExactSpelling = true)]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);

    // Call after HandleCreated; keep the standard Windows frame and system buttons.
    public static bool Apply(Form form)
    {
        if (form == null || form.IsDisposed || !form.IsHandleCreated
            || Environment.OSVersion.Platform != PlatformID.Win32NT)
            return false;

        try
        {
            int enabled = 1;
            if (DwmSetWindowAttribute(form.Handle, UseImmersiveDarkMode, ref enabled, sizeof(int)) < 0)
            {
                // Compatibility fallback for Windows builds preceding the public attribute.
                DwmSetWindowAttribute(form.Handle, LegacyUseImmersiveDarkMode, ref enabled, sizeof(int));
            }

            // COLORREF is 0x00BBGGRR. Windows 11 supports these explicit frame colors;
            // earlier systems reject unsupported attributes and retain the dark-mode frame.
            int black = ColorTranslator.ToWin32(Color.Black);
            int text = ColorTranslator.ToWin32(UiTheme.Text);
            int caption = DwmSetWindowAttribute(form.Handle, CaptionColor, ref black, sizeof(int));
            int border = DwmSetWindowAttribute(form.Handle, BorderColor, ref black, sizeof(int));
            int captionText = DwmSetWindowAttribute(form.Handle, TextColor, ref text, sizeof(int));
            return caption >= 0 && border >= 0 && captionText >= 0;
        }
        catch (DllNotFoundException)
        {
            // DWM is unavailable. The operating system retains its native frame.
        }
        catch (EntryPointNotFoundException)
        {
            // Older Windows versions retain their native frame.
        }
        return false;
    }
}
}
