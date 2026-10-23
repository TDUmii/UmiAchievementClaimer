using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

// Exercises the compiled UI with synthetic data only. Never invokes detection,
// ClaimAchievements, StartWorker, or a Steam API. No desktop capture or input.
internal static class UiRegression
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private static Assembly application;
    private static string output;
    private static int passed;
    private static int failed;

    [DllImport("dwmapi.dll", ExactSpelling = true)]
    private static extern int DwmGetWindowAttribute(IntPtr window, int attribute, out int value, int size);

    private static object Field(object target, string name) { return target.GetType().GetField(name, Members).GetValue(target); }
    private static void Set(object target, string name, object value) { target.GetType().GetField(name, Members).SetValue(target, value); }
    private static void SetProperty(object target, string name, object value) { target.GetType().GetProperty(name, Members).SetValue(target, value, null); }
    private static object Property(object target, string name) { return target.GetType().GetProperty(name, Members).GetValue(target, null); }
    private static object Call(object target, string name, params object[] args) { return target.GetType().GetMethod(name, Members).Invoke(target, args); }
    private static Control Control(object target, string name) { return (Control)Field(target, name); }
    private static object[] Checked(object grid) { return ((IEnumerable)Property(grid, "CheckedItems")).Cast<object>().ToArray(); }
    private static int Visible(object grid) { return (int)Property(grid, "VisibleCount"); }
    private static void Check(bool condition, string name)
    {
        Console.WriteLine((condition ? "PASS " : "FAIL ") + name);
        if (condition) passed++; else failed++;
    }

    private static object New(string name) { return Activator.CreateInstance(application.GetType("UmiAchievementClaimer." + name), true); }
    private static object Item(string id, string name, string description, bool achieved)
    {
        object item = New("AchievementItem");
        Set(item, "Id", id); Set(item, "Name", name); Set(item, "Description", description); Set(item, "Achieved", achieved);
        return item;
    }

    private static void Prepare(Form form)
    {
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(-30000, -30000);
        form.ShowInTaskbar = false;
        form.Text += " - UI TEST (synthetic data)";
        form.Show();
        form.PerformLayout();
        Application.DoEvents();
    }

    private static void Capture(Form form, string name)
    {
        form.PerformLayout();
        Application.DoEvents();
        using (Bitmap bitmap = new Bitmap(form.Width, form.Height))
        {
            form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
            bitmap.Save(Path.Combine(output, name), ImageFormat.Png);
        }
        Console.WriteLine("RENDER " + name + " " + form.Width + "x" + form.Height);
    }

    private static void CaptureClient(Form form, string name)
    {
        form.PerformLayout();
        Application.DoEvents();
        Point clientOrigin = form.PointToScreen(Point.Empty);
        Rectangle clientBounds = new Rectangle(clientOrigin.X - form.Left, clientOrigin.Y - form.Top,
            form.ClientSize.Width, form.ClientSize.Height);
        using (Bitmap window = new Bitmap(form.Width, form.Height))
        {
            form.DrawToBitmap(window, new Rectangle(Point.Empty, window.Size));
            using (Bitmap client = window.Clone(clientBounds, PixelFormat.Format32bppArgb))
                client.Save(Path.Combine(output, name), ImageFormat.Png);
        }
        Console.WriteLine("RENDER_CLIENT " + name + " " + form.ClientSize.Width + "x" + form.ClientSize.Height);
    }

    private static void CheckWindowTheme(Form form, string name)
    {
        Type theme = application.GetType("UmiAchievementClaimer.WindowTheme");
        bool accepted = (bool)theme.GetMethod("Apply", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Invoke(null, new object[] { form });
        int build;
        string buildNumber = Convert.ToString(Microsoft.Win32.Registry.GetValue(
            @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentBuildNumber", null));
        if (accepted || int.TryParse(buildNumber, out build) && build >= 22000)
            Check(accepted, name + " Windows accepts the requested black caption, black border and light caption text");
        else
            Console.WriteLine("SKIP " + name + " explicit frame-color attributes unsupported by this Windows build");

        try
        {
            int value;
            int result = DwmGetWindowAttribute(form.Handle, 20, out value, sizeof(int));
            if (result < 0)
                Console.WriteLine("SKIP " + name + " DWM immersive dark-mode readback unsupported (HRESULT 0x" + result.ToString("X8") + ")");
            else
                Check(value == 1, name + " DWM immersive dark mode is enabled");
        }
        catch (DllNotFoundException) { Console.WriteLine("SKIP " + name + " DWM unavailable on this system"); }
        catch (EntryPointNotFoundException) { Console.WriteLine("SKIP " + name + " DWM readback unavailable on this system"); }
    }

    private static void CheckArtworkFixture()
    {
        string coverPath = Path.Combine(output, "portal2-cover.png");
        string unlockedPath = Path.Combine(output, "portal2-icon-unlocked.png");
        string lockedPath = Path.Combine(output, "portal2-icon-locked.png");
        if (!File.Exists(coverPath) || !File.Exists(unlockedPath) || !File.Exists(lockedPath))
        {
            Console.WriteLine("SKIP artwork fixture capture: supply portal2-cover.png, portal2-icon-unlocked.png and portal2-icon-locked.png in the output directory");
            return;
        }

        using (Form form = (Form)New("ClaimerForm"))
        {
            Prepare(form);
            object grid = Field(form, "achievementList");
            object game = New("DetectedGame");
            Set(game, "Name", "Portal 2 (synthetic UI state)");
            Set(game, "AppId", "620");
            Set(game, "RootDirectory", @"D:\Synthetic Steam Library\Portal 2");
            Set(game, "SteamApiPath", @"D:\Synthetic Steam Library\Portal 2\bin\win64\steam_api64.dll");
            Set(game, "Architecture", "x64");
            Set(game, "InterfaceVersion", "STEAMUSERSTATS_INTERFACE_VERSION012");
            Set(form, "detectedGame", game);
            Call(form, "SetInformation", game);
            Control(form, "pathBox").Text = (string)Field(game, "RootDirectory");

            object[] items = {
                Item("ACH.SURVIVE_CONTAINER_RIDE", "Wake Up Call", "Synthetic locked state for UI verification.", false),
                Item("UI_SAMPLE_UNLOCKED", "Unlocked artwork sample", "Synthetic achievement used only to inspect icon rendering.", true),
                Item("UI_SAMPLE_LOCKED", "Locked artwork sample", "Synthetic achievement used only to inspect icon rendering.", false)
            };
            foreach (object item in items) Call(grid, "AddItem", item);

            object artwork = New("SteamArtwork");
            Image cover;
            using (Image source = Image.FromFile(coverPath)) cover = new Bitmap(source);
            Set(artwork, "Cover", cover);
            IDictionary icons = (IDictionary)Field(artwork, "IconUrls");
            IDictionary lockedIcons = (IDictionary)Field(artwork, "LockedIconUrls");
            IDictionary images = (IDictionary)Field(form, "achievementImages");
            const string unlockedKey = "fixture:portal2-unlocked";
            const string lockedKey = "fixture:portal2-locked";
            using (Image source = Image.FromFile(unlockedPath)) images.Add(unlockedKey, new Bitmap(source));
            using (Image source = Image.FromFile(lockedPath)) images.Add(lockedKey, new Bitmap(source));
            foreach (object item in items)
            {
                string id = "id:" + (string)Field(item, "Id");
                icons[id] = unlockedKey;
                lockedIcons[id] = lockedKey;
            }
            Set(form, "steamArtwork", artwork);
            PictureBox coverView = (PictureBox)Field(form, "gameCover");
            coverView.Image = cover;
            coverView.Visible = true;
            Call(form, "SetAchievementControlsEnabled", true);
            Call(form, "UpdateClaimButtonState");
            Check(coverView.Visible && ReferenceEquals(coverView.Image, cover), "fixture cover is assigned to the game sidebar");
            Check(ReferenceEquals(Call(form, "GetAchievementImage", items[0]), images[lockedKey])
                && ReferenceEquals(Call(form, "GetAchievementImage", items[1]), images[unlockedKey]),
                "locked and unlocked rows resolve the corresponding cached icon");
            Check(Checked(grid).Length == 0 && !Control(form, "claimButton").Enabled, "artwork injection never selects achievements");
            Call(form, "AppendLog", "ARTWORK UI TEST: public Portal 2 images, synthetic status rows; no Steam scan or claim.");
            CaptureClient(form, "artwork-client.png");

            form.Size = form.MinimumSize;
            ((Button)Field(form, "detailsButton")).PerformClick();
            ((Button)Field(form, "logToggleButton")).PerformClick();
            form.PerformLayout();
            Application.DoEvents();
            Check(Control(form, "technicalPanel").Visible && Control(form, "logBox").Visible,
                "minimum artwork layout opens technical details and activity log together");
            Check(Inside(form, Control(form, "appIdValue")) && Inside(form, Control(form, "architectureValue"))
                && Inside(form, Control(form, "dllValue")) && Inside(form, Control(form, "interfaceValue")),
                "minimum artwork layout keeps game metadata and expanded technical values inside the client");
            Control apiCell = Control(form, "dllValue").Parent;
            Control interfaceCell = Control(form, "interfaceValue").Parent;
            Check(!apiCell.Bounds.IntersectsWith(interfaceCell.Bounds),
                "expanded Steam API and User Stats cells do not overlap");
            Check(Inside(form, Control(form, "claimButton")) && Inside(form, Control(form, "searchBox")),
                "minimum artwork layout keeps Claim and search inside the client");
            DataGridView minGrid = (DataGridView)Field(form, "achievementList");
            Check(minGrid.Height >= minGrid.RowTemplate.Height, "minimum artwork layout preserves a full achievement row with log expanded");
            CaptureClient(form, "artwork-minimum-details-log-client.png");
            Call(form, "ClearArtwork");
            Check(Field(form, "steamArtwork") == null && images.Count == 0 && coverView.Image == null,
                "changing games clears fixture cover and cached achievement images");
            Check(Field(form, "workerProcess") == null, "artwork fixture never starts a worker");
        }
    }

    private static void Report(string name, Control control)
    {
        Console.WriteLine("LAYOUT " + name + " bounds=" + control.Bounds + " parent=" + control.Parent.ClientSize);
    }

    private static bool FitsParent(Control control)
    {
        return control.Parent.ClientRectangle.Contains(control.Bounds);
    }

    private static IEnumerable<Control> Descendants(Control control)
    {
        yield return control;
        foreach (Control child in control.Controls)
            foreach (Control descendant in Descendants(child))
                yield return descendant;
    }

    private static bool SingleRowsFit(Form form)
    {
        return Descendants(form).OfType<TableLayoutPanel>().Where(layout => layout.RowCount == 1 && layout.Visible)
            .All(layout => layout.Controls.Cast<Control>().Where(control => control.Visible).All(FitsParent));
    }

    private static bool Inside(Form form, Control control)
    {
        Rectangle bounds = form.RectangleToClient(control.RectangleToScreen(control.ClientRectangle));
        return control.Visible && bounds.Width > 0 && bounds.Height > 0 && form.ClientRectangle.Contains(bounds);
    }

    private static void CheckSteamLibrary()
    {
        string root = Path.Combine(Path.GetTempPath(), "UmiCatalogTest_" + Guid.NewGuid().ToString("N"));
        string steamapps = Path.Combine(root, "steamapps");
        string common = Path.Combine(steamapps, "common");
        try
        {
            Directory.CreateDirectory(Path.Combine(common, "Portal 2"));
            Directory.CreateDirectory(Path.Combine(common, "Game Việt"));
            Directory.CreateDirectory(Path.Combine(root, "elsewhere"));
            File.WriteAllText(Path.Combine(steamapps, "appmanifest_620.acf"),
                "\"AppState\"\n{\n\"appid\" \"620\"\n\"name\" \"Portal 2\"\n\"installdir\" \"Portal 2\"\n}", Encoding.UTF8);
            File.WriteAllText(Path.Combine(steamapps, "appmanifest_900.acf"),
                "\"AppState\"\n{\n\"appid\" \"900\"\n\"name\" \"Trò chơi Việt\"\n\"installdir\" \"Game Việt\"\n}", Encoding.UTF8);
            File.WriteAllText(Path.Combine(steamapps, "appmanifest_901.acf"),
                "\"AppState\"\n{\n\"appid\" \"901\"\n\"name\" \"Outside\"\n\"installdir\" \"..\\\\..\\\\elsewhere\"\n}", Encoding.UTF8);
            File.WriteAllText(Path.Combine(steamapps, "appmanifest_bad.acf"),
                "\"AppState\" { \"appid\" \"NOT_NUMERIC\" \"installdir\" \"Missing\" }", Encoding.UTF8);

            Type catalogType = application.GetType("UmiAchievementClaimer.SteamLibraryCatalog");
            object result = catalogType.GetMethod("FromLibraries", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                .Invoke(null, new object[] { new string[] { root, root, Path.Combine(root, "missing") } });
            object[] found = ((IEnumerable)result).Cast<object>().ToArray();
            Check(found.Length == 2, "Steam library inventories only installed valid manifest games");
            Check(found.Any(x => (string)Field(x, "AppId") == "620") &&
                  found.Any(x => (string)Field(x, "Name") == "Trò chơi Việt"), "Steam library preserves app IDs and Unicode game names");
            Check(found.All(x => ((string)Field(x, "Folder")).StartsWith(common + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)),
                "Steam library ignores folders outside steamapps/common");

            Type pickerType = application.GetType("UmiAchievementClaimer.SteamLibraryDialog");
            using (Form dialog = (Form)Activator.CreateInstance(pickerType, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance,
                null, new object[] { true, false }, null))
            {
                Prepare(dialog);
                CheckWindowTheme(dialog, "Steam library picker");
                Type gameType = application.GetType("UmiAchievementClaimer.SteamInstalledGame");
                Array typedGames = Array.CreateInstance(gameType, found.Length);
                for (int i = 0; i < found.Length; i++) typedGames.SetValue(found[i], i);
                Call(dialog, "Populate", typedGames);
                ListBox list = (ListBox)Field(dialog, "games");
                TextBox query = (TextBox)Field(dialog, "query");
                Check(list.Items.Count == 2, "Steam game picker shows installed game list");
                query.Text = "NOT_FOUND";
                Check(list.Items.Count == 0 && !Control(dialog, "choose").Enabled, "Steam game picker has empty-search and safe-disabled selection");
                query.Text = "620";
                Check(list.Items.Count == 1 && Control(dialog, "choose").Enabled, "Steam game picker searches by Steam AppID");
                query.Text = "";
                Check(list.Items.Count == 2 && SingleRowsFit(dialog), "Steam picker restores searchable games with fitted actions");
                Capture(dialog, "steam-library-en.png");
                Array longLibrary = Array.CreateInstance(gameType, 24);
                for (int i = 0; i < longLibrary.Length; i++)
                {
                    object sample = Activator.CreateInstance(gameType, true);
                    Set(sample, "Name", "Installed Sample Game " + (i + 1).ToString("00"));
                    Set(sample, "AppId", (1000 + i).ToString());
                    Set(sample, "Folder", Path.Combine(common, "Game " + i));
                    longLibrary.SetValue(sample, i);
                }
                Call(dialog, "Populate", longLibrary);
                Check(list.Items.Count == 24, "Steam picker handles a large installed-game library");
                Control libraryBar = Control(dialog, "libraryScroll");
                Check(libraryBar.Visible && libraryBar.Width == 12, "large Steam library uses the themed scrollbar");
                SetProperty(Field(dialog, "libraryScroll"), "Value", 4);
                Check(list.TopIndex >= 4, "Steam library themed scrollbar navigates the list");
                SetProperty(Field(dialog, "libraryScroll"), "Value", 0);
                Capture(dialog, "steam-library-overflow-en.png");
                Call(dialog, "Populate", typedGames);
                ((Button)Field(dialog, "choose")).PerformClick();
                Check(dialog.DialogResult == DialogResult.OK && !string.IsNullOrEmpty((string)Property(dialog, "SelectedPath")),
                    "Steam game picker returns an explicit selected installation path");
            }
        }
        finally
        {
            try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch { }
        }
    }

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length != 2) throw new ArgumentException("Usage: UiRegression.exe APPLICATION.exe OUTPUT_DIRECTORY");
            application = Assembly.LoadFrom(Path.GetFullPath(args[0]));
            output = Path.GetFullPath(args[1]);
            Directory.CreateDirectory(output);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (Form form = (Form)New("ClaimerForm"))
            {
                Prepare(form);
                CheckWindowTheme(form, "main window");
                object grid = Field(form, "achievementList");
                DataGridView table = (DataGridView)grid;
                Button language = (Button)Field(form, "languageToggleButton");
                Type themeType = application.GetType("UmiAchievementClaimer.UiTheme");
                themeType.GetMethod("ApplyPreset", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { "Carbon" });
                Check((string)themeType.GetProperty("ActiveName", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).GetValue(null, null) == "Carbon"
                    && Control(form, "settingsButton").Enabled, "theme preset changes while the client is idle");
                themeType.GetMethod("ApplyCustomAccent", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { Color.FromArgb(255, 88, 190, 224) });
                Check((string)themeType.GetProperty("ActiveName", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).GetValue(null, null) == "Custom", "custom accent theme is accepted");
                themeType.GetMethod("ApplyPreset", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { "Steam Midnight" });
                TextBox search = (TextBox)Field(form, "searchBox");
                Button[] filters = (Button[])Field(form, "filterButtons");
                Check(!Control(form, "claimButton").Enabled && Checked(grid).Length == 0, "initial Claim disabled and nothing selected");
                Check(Control(form, "libraryButton").Enabled && !Control(form, "scanButton").Enabled, "discoverable Steam library action and disabled empty-path scan");
                Check(Control(form, "informationCard").Top < Control(form, "achievementCard").Top,
                    "game summary sits above full-width achievement workspace");
                Check(Control(form, "informationCard").Width == Control(form, "achievementCard").Width,
                    "game summary and workspace are aligned at full width");
                Check(Control(form, "searchPlaceholderLabel").Visible, "search placeholder initially visible");
                Check(Control(form, "safeModeLabel").Height >= 14, "safety caption remains legible");
                Check(Control(form, "appIdValue").Height >= 18 && Control(form, "architectureValue").Height >= 18,
                    "Steam game metadata values have enough vertical space");
                Check(Control(form, "emptyLabel").Visible && Visible(grid) == 0, "initial empty state");

                Capture(form, "empty-en.png");

                object game = New("DetectedGame");
                Set(game, "Name", "Sample Game (synthetic UI test)"); Set(game, "AppId", "123456");
                Set(game, "RootDirectory", @"D:\Synthetic Steam Library\Trò chơi minh họa\Very long installation folder used only for UI regression");
                Set(game, "SteamApiPath", @"D:\Synthetic Steam Library\Trò chơi minh họa\bin\win64\steam_api64.dll");
                Set(game, "Architecture", "x64"); Set(game, "InterfaceVersion", "STEAMUSERSTATS_INTERFACE_VERSION012");
                Set(form, "detectedGame", game); Call(form, "SetInformation", game);
                Control(form, "pathBox").Text = (string)Field(game, "RootDirectory");
                object[] items = {
                    Item("FIRST_STEPS", "First Steps", "Complete your first mission.", false),
                    Item("MASTER_EXPLORER", "Master Explorer", "Discover all hidden areas.", false),
                    Item("COLLECTOR_100", "The Collector", "Collect 100 rare items.", false),
                    Item("WELCOME_ABOARD", "Welcome Aboard", "Launch the game for the first time.", true),
                    Item("VIETNAM_UNICODE", "Bước chân đầu tiên - Quyết tâm Việt Nam", "Hoàn thành nhiệm vụ tiếng Việt có dấu.", false),
                    Item("LONG_NAME", "A deliberately long achievement title that must remain readable with ellipsis instead of overlapping the status", "Synthetic description with a long line to exercise clipping and full-description tooltips.", true)
                };
                foreach (object item in items) Call(grid, "AddItem", item);
                Call(form, "SetAchievementControlsEnabled", true); Call(form, "UpdateClaimButtonState");
                Check(Checked(grid).Length == 0 && !Control(form, "claimButton").Enabled, "loaded rows are never automatically checked");
                Check(Visible(grid) == 6, "all synthetic achievements are visible");
                Check(Control(form, "achievementProgressFill").Width > 0 &&
                    Control(form, "achievementProgressFill").Width < Control(form, "achievementProgressTrack").Width,
                    "achievement progress stripe reflects partial unlock rate");

                search.Text = "explorer"; Check(Visible(grid) == 1, "search by title ignores case");
                Check(!Control(form, "searchPlaceholderLabel").Visible, "search placeholder hides when filled");
                search.Text = "COLLECTOR_100"; Check(Visible(grid) == 1, "search by API ID");
                search.Text = "tiếng Việt"; Check(Visible(grid) == 1, "search by Unicode description");
                search.Text = "no-such-achievement";
                Check(Visible(grid) == 0 && Control(form, "emptyLabel").Visible, "zero-result search has empty state");
                search.Text = ""; filters[1].PerformClick(); Check(Visible(grid) == 4, "Locked filter");
                filters[2].PerformClick(); Check(Visible(grid) == 2, "Unlocked filter");
                filters[0].PerformClick();

                table.Rows[0].Cells[0].Value = true;
                Check(Checked(grid).Length == 1 && Control(form, "claimButton").Enabled, "explicit checkbox enables Claim");
                ((Button)Field(form, "selectionMenuButton")).PerformClick();
                ContextMenuStrip menu = (ContextMenuStrip)Field(form, "selectionMenu");
                Check(menu.Items.Count == 3 && menu.Renderer != null, "selection menu exposes three themed actions");
                menu.Close();
                search.Text = "Explorer";
                Check(Checked(grid).Length == 1 && Control(form, "selectedCountLabel").Text.Contains("1 hidden"), "filter preserves and discloses hidden selections");
                ((Button)Field(form, "selectAllButton")).PerformClick();
                Check(Checked(grid).Length == 2, "Select visible keeps hidden selections");
                ((Button)Field(form, "clearSelectionButton")).PerformClick();
                Check(Checked(grid).Length == 0 && !Control(form, "claimButton").Enabled, "Clear all also clears hidden selections");
                search.Text = "";
                table.CurrentCell = table.Rows[0].Cells[2];
                Call(grid, "ProcessDataGridViewKey", new KeyEventArgs(Keys.Space));
                Check(Checked(grid).Length == 1, "Space on title toggles checkbox");
                table.Rows[1].Cells[0].Value = true;

                object selection = Property(grid, "CheckedItems");
                string selectionPath = (string)Call(form, "CreateSelectionFile", selection);
                string[] selectedIds = File.ReadAllLines(selectionPath).Select(line => Encoding.UTF8.GetString(Convert.FromBase64String(line))).ToArray();
                Check(selectedIds.SequenceEqual(new[] { "FIRST_STEPS", "MASTER_EXPLORER" }), "selection file contains only the explicitly selected API IDs");
                Call(form, "CleanupWorker"); Check(!File.Exists(selectionPath), "temporary selection file cleaned up");
                Check(Field(form, "workerProcess") == null, "UI and serialization checks never start a worker");

                Call(form, "SetBusy", true);
                string[] busyControls = { "libraryButton", "browseButton", "pathBox", "languageToggleButton", "themeButton", "settingsButton", "searchBox", "achievementList", "selectAllButton", "selectLockedButton", "clearSelectionButton", "selectionMenuButton", "claimButton" };
                Check(busyControls.All(name => !Control(form, name).Enabled) && filters.All(button => !button.Enabled), "busy state locks game, language, filters and selection");
                Call(form, "SetBusy", false);
                Check(busyControls.All(name => Control(form, name).Enabled) && filters.All(button => button.Enabled)
                    && Control(form, "scanButton").Enabled == Directory.Exists(Control(form, "pathBox").Text), "idle state restores appropriate controls and respects valid scan path");
                Call(form, "AppendLog", "UI TEST: synthetic rows injected; no Steam scan or claim was performed.");
                Report("browse", Control(form, "browseButton")); Report("scan", Control(form, "scanButton"));
                Report("filter-all", filters[0]); Report("desktop-grid", table);
                Check(SingleRowsFit(form), "all single-row layout children fit their parents");
                Capture(form, "desktop-en.png");
                File.Copy(Path.Combine(output, "desktop-en.png"), Path.Combine(output, "hero-repro.png"), true);

                ((Button)Field(form, "logToggleButton")).PerformClick();
                Check(Control(form, "logBox").Visible && (bool)Field(form, "logExpanded"), "activity log expands");
                Check(Control(form, "logBox").Text.Contains("synthetic rows injected"), "activity log retains operation text");
                Check(Control(form, "logBox").Height >= 70, "expanded activity log remains legible at desktop size");
                Report("expanded-log-grid", table); Report("expanded-log", Control(form, "logBox"));
                Capture(form, "log-en.png");
                ((Button)Field(form, "logToggleButton")).PerformClick();
                Check(!Control(form, "logBox").Visible && !(bool)Field(form, "logExpanded"), "activity log collapses");

                language.PerformClick();
                Check(Control(form, "claimButton").Text == "Claim đã chọn" && (string)table.Rows[0].Cells[3].Value == "Chưa mở", "Vietnamese controls and row status");
                Check(Checked(grid).Length == 2, "language change preserves selection");
                Set(game, "Name", "Trò chơi minh họa (dữ liệu giả lập) - Tên rất dài để kiểm tra hiển thị Unicode");
                Call(form, "SetInformation", game);
                form.Size = form.MinimumSize;
                Check(Inside(form, Control(form, "claimButton")) && Inside(form, search), "minimum size keeps Claim and search inside client area");
                Report("minimum-grid", table);
                Check(Control(form, "achievementScrollBar").Visible && Control(form, "achievementScrollBar").Width == 12,
                    "overflowing achievements use the themed scrollbar at minimum size");
                SetProperty(Field(form, "achievementScrollBar"), "Value", 1);
                Check((int)Property(grid, "FirstDisplayedScrollingRowIndex") == 1, "themed scrollbar moves the achievement list");
                Check(table.Height >= table.RowTemplate.Height, "minimum size leaves at least one complete achievement row");
                Check(SingleRowsFit(form), "minimum-size single-row layout children fit their parents");
                Capture(form, "minimum-vi.png");
                language.PerformClick();
                Check(Control(form, "claimButton").Text == "Claim selected" && (string)table.Rows[0].Cells[3].Value == "Locked", "English controls and row status restored");

                Type confirmationType = application.GetType("UmiAchievementClaimer.ClaimConfirmationDialog");
                using (Form confirmation = (Form)Activator.CreateInstance(confirmationType, new object[] { game, 2, items.Length, true }))
                {
                    Prepare(confirmation);
                    CheckWindowTheme(confirmation, "confirmation window");
                    Check(confirmation.AcceptButton == confirmation.CancelButton && confirmation.CancelButton != null, "confirmation defaults Enter and Escape to Cancel");
                    Check(confirmation.DialogResult != DialogResult.OK, "opening confirmation does not accept a claim");
                    Check(confirmation.AcceptButton.DialogResult == DialogResult.Cancel, "default action has Cancel result");
                    Check(SingleRowsFit(confirmation), "confirmation action buttons fit their layout row");
                    Capture(confirmation, "confirmation-en.png");
                    confirmation.AcceptButton.PerformClick();
                    Check(confirmation.DialogResult == DialogResult.Cancel, "default action cancels confirmation");
                }

                using (Form settingsDialog = (Form)New("SettingsDialog"))
                {
                    Prepare(settingsDialog);
                    CheckWindowTheme(settingsDialog, "settings dialog");
                    Check(SingleRowsFit(settingsDialog), "settings controls fit their layout row");
                    Button dialogLanguage = (Button)Field(settingsDialog, "languageButton");
                    dialogLanguage.PerformClick();
                    Check(!(bool)Property(settingsDialog, "English"), "settings language toggle switches to Vietnamese");
                    dialogLanguage.PerformClick();
                    Check((bool)Property(settingsDialog, "English"), "settings language toggle switches back to English");
                    Capture(settingsDialog, "settings-dialog.png");
                }

                form.ClientSize = new Size(1220, 840);
                var fonts = Descendants(form).Select(control => new { Control = control, Font = control.Font }).ToArray();
                form.AutoScaleMode = AutoScaleMode.None;
                form.MaximumSize = new Size(6000, 6000);
                form.Scale(new SizeF(1.5F, 1.5F));
                foreach (var entry in fonts)
                    entry.Control.Font = new Font(entry.Font.FontFamily, entry.Font.SizeInPoints * 1.5F, entry.Font.Style);
                Capture(form, "dpi150.png");
                Check(Inside(form, Control(form, "claimButton")), "150-percent geometry and font simulation keeps Claim visible");
                Check(Field(form, "workerProcess") == null && Field(form, "selectedAchievementFile") == null, "no worker or temporary selection remains");
            }
            CheckSteamLibrary();
            CheckArtworkFixture();
            Console.WriteLine("RESULT " + passed + " PASS, " + failed + " FAIL");
            Console.WriteLine("Scope: synthetic UI regression, cached public-artwork fixture rendering, DWM frame-color request acceptance and dark-mode readback only; Steam runtime and physical DPI are not exercised.");
            Console.WriteLine("DrawToBitmap does not verify native titlebar appearance. Caption/border color attributes are set-only, so their request HRESULTs are checked; client captures contain no painted imitation of the frame.");
            return failed == 0 ? 0 : 1;
        }
        catch (Exception error)
        {
            Console.WriteLine("FATAL " + error);
            return 2;
        }
    }
}
