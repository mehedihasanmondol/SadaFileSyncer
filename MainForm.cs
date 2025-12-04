using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace SadaFileSyncer
{
    public class MainForm : Form
    {
        TextBox txtWatch, txtDest;
        Button btnBrowseWatch, btnBrowseDest, btnStartStop, btnPauseResume, btnOpenLog;
        CheckBox chkCopyMode, chkOnlyNew, chkStartWithWindows, chkSyncExisting;
        ListBox lstLog;
        NotifyIcon trayIcon;
        ContextMenuStrip trayMenu;

        FileSystemWatcher? watcher;
        bool running = false;
        bool paused = false;
        HashSet<string> processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public MainForm()
        {
            Text = "SadaFileSyncer";
            Width = 700; Height = 420;
            MinimumSize = new Size(680, 420);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            
            // Set the form icon
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
                if (File.Exists(iconPath))
                {
                    this.Icon = new Icon(iconPath);
                }
                else
                {
                    // Try to extract from executable
                    this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                }
            }
            catch { }

            InitializeComponents();
            LoadSettings();
            SetupTray();
        }

        void InitializeComponents()
        {
            var panelTop = new Panel { Dock = DockStyle.Top, Height = 210, Padding = new Padding(12) };
            Controls.Add(panelTop);

            var lblWatch = new Label { Text = "Watch Folder (A):", Left = 8, Top = 8, AutoSize = true };
            panelTop.Controls.Add(lblWatch);
            txtWatch = new TextBox { Left = 8, Top = 28, Width = 520 };
            panelTop.Controls.Add(txtWatch);
            btnBrowseWatch = new Button { Text = "Browse", Left = 540, Top = 26, Width = 110 };
            btnBrowseWatch.Click += (s, e) => { var f = Browse(); if (!string.IsNullOrEmpty(f)) txtWatch.Text = f; SaveSettings(); };
            panelTop.Controls.Add(btnBrowseWatch);

            var lblDest = new Label { Text = "Destination Folder (B):", Left = 8, Top = 62, AutoSize = true };
            panelTop.Controls.Add(lblDest);
            txtDest = new TextBox { Left = 8, Top = 82, Width = 520 };
            panelTop.Controls.Add(txtDest);
            btnBrowseDest = new Button { Text = "Browse", Left = 540, Top = 80, Width = 110 };
            btnBrowseDest.Click += (s, e) => { var f = Browse(); if (!string.IsNullOrEmpty(f)) txtDest.Text = f; SaveSettings(); };
            panelTop.Controls.Add(btnBrowseDest);

            chkCopyMode = new CheckBox { Text = "Copy mode (leave original)", Left = 8, Top = 116, Width = 220 };
            chkCopyMode.CheckedChanged += (s, e) => SaveSettings();
            panelTop.Controls.Add(chkCopyMode);

            chkOnlyNew = new CheckBox { Text = "Only new files (ignore repeats)", Left = 240, Top = 116, Width = 220 };
            chkOnlyNew.CheckedChanged += (s, e) => SaveSettings();
            panelTop.Controls.Add(chkOnlyNew);

            chkStartWithWindows = new CheckBox { Text = "Start with Windows", Left = 460, Top = 116, Width = 140 };
            chkStartWithWindows.CheckedChanged += (s, e) => { SaveSettings(); ApplyStartup(chkStartWithWindows.Checked); };
            panelTop.Controls.Add(chkStartWithWindows);

            chkSyncExisting = new CheckBox { Text = "Sync existing files on start", Left = 8, Top = 140, Width = 220 };
            chkSyncExisting.CheckedChanged += (s, e) => SaveSettings();
            panelTop.Controls.Add(chkSyncExisting);

            btnStartStop = new Button { Text = "Start", Left = 8, Top = 168, Width = 120, Height = 36 };
            btnStartStop.Click += (s, e) => { if (!running) StartWatcher(); else StopWatcher(); };
            panelTop.Controls.Add(btnStartStop);

            btnPauseResume = new Button { Text = "Pause", Left = 140, Top = 168, Width = 120, Height = 36, Enabled = false };
            btnPauseResume.Click += (s, e) => TogglePause();
            panelTop.Controls.Add(btnPauseResume);

            btnOpenLog = new Button { Text = "Open Log File", Left = 272, Top = 168, Width = 120, Height = 36 };
            btnOpenLog.Click += (s, e) => { try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SadaFileSyncer", "logs", "activity.log"), UseShellExecute = true }); } catch { } };
            panelTop.Controls.Add(btnOpenLog);

            var btnAbout = new Button { Text = "About", Left = 404, Top = 168, Width = 80, Height = 36 };
            btnAbout.Click += (s, e) => ShowAboutDialog();
            panelTop.Controls.Add(btnAbout);

            lstLog = new ListBox { Left = 8, Top = panelTop.Bottom + 8, Width = ClientSize.Width - 24, Height = ClientSize.Height - panelTop.Height - 40, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
            Controls.Add(lstLog);

            Resize += (s, e) => { lstLog.Width = ClientSize.Width - 24; lstLog.Height = ClientSize.Height - panelTop.Height - 40; };
        }

        string? Browse()
        {
            using var dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == DialogResult.OK) return dlg.SelectedPath;
            return null;
        }

        void LoadSettings()
        {
            txtWatch.Text = ConfigManager.Current.WatchFolder;
            txtDest.Text = ConfigManager.Current.DestFolder;
            chkCopyMode.Checked = ConfigManager.Current.CopyMode;
            chkOnlyNew.Checked = ConfigManager.Current.OnlyNewFiles;
            chkStartWithWindows.Checked = ConfigManager.Current.StartWithWindows;
            chkSyncExisting.Checked = ConfigManager.Current.SyncExistingFiles;
        }

        void SaveSettings()
        {
            ConfigManager.Current.WatchFolder = txtWatch.Text.Trim();
            ConfigManager.Current.DestFolder = txtDest.Text.Trim();
            ConfigManager.Current.CopyMode = chkCopyMode.Checked;
            ConfigManager.Current.OnlyNewFiles = chkOnlyNew.Checked;
            ConfigManager.Current.StartWithWindows = chkStartWithWindows.Checked;
            ConfigManager.Current.SyncExistingFiles = chkSyncExisting.Checked;
            ConfigManager.Save();
        }

        void SetupTray()
        {
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Show", null, (s, e) => { ShowWindow(); });
            trayMenu.Items.Add("Pause/Resume", null, (s, e) => { TogglePause(); });
            trayMenu.Items.Add("Open Logs", null, (s, e) => { try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SadaFileSyncer", "logs", "activity.log"), UseShellExecute = true }); } catch { } });
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("About", null, (s, e) => { ShowAboutDialog(); });
            trayMenu.Items.Add("Exit", null, (s, e) => { Application.Exit(); });

            trayIcon = new NotifyIcon();
            
            // Set tray icon - try multiple methods
            try
            {
                // Method 1: Try loading from file
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
                if (File.Exists(iconPath))
                {
                    using (var iconStream = new FileStream(iconPath, FileMode.Open, FileAccess.Read))
                    {
                        trayIcon.Icon = new Icon(iconStream);
                    }
                }
                else
                {
                    // Method 2: Extract from executable
                    var exeIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                    if (exeIcon != null)
                    {
                        trayIcon.Icon = exeIcon;
                    }
                    else
                    {
                        trayIcon.Icon = SystemIcons.Application;
                    }
                }
            }
            catch
            {
                // Fallback: Use form icon if available, otherwise system icon
                if (this.Icon != null)
                {
                    trayIcon.Icon = this.Icon;
                }
                else
                {
                    trayIcon.Icon = SystemIcons.Application;
                }
            }
            
            trayIcon.Visible = true;
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.DoubleClick += (s, e) => ShowWindow();
            trayIcon.BalloonTipTitle = "SadaFileSyncer";
            trayIcon.BalloonTipIcon = ToolTipIcon.Info;
        }

        void ShowWindow()
        {
            Show();
            WindowState = FormWindowState.Normal;
            BringToFront();
        }

        void TogglePause()
        {
            if (!running) return;
            paused = !paused;
            if (watcher != null) watcher.EnableRaisingEvents = !paused;
            btnPauseResume.Text = paused ? "Resume" : "Pause";
            Log(paused ? "Paused" : "Resumed");
        }

        void StartWatcher()
        {
            SaveSettings();

            if (string.IsNullOrWhiteSpace(ConfigManager.Current.WatchFolder) || string.IsNullOrWhiteSpace(ConfigManager.Current.DestFolder))
            {
                MessageBox.Show("Please set both Watch and Destination folders.", "Missing folders", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!Directory.Exists(ConfigManager.Current.WatchFolder) || !Directory.Exists(ConfigManager.Current.DestFolder))
            {
                MessageBox.Show("One of the folders does not exist.", "Folder missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Sync existing files if checkbox is checked
            if (ConfigManager.Current.SyncExistingFiles)
            {
                Task.Run(() => SyncExistingFiles());
            }

            watcher = new FileSystemWatcher(ConfigManager.Current.WatchFolder);
            watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
            watcher.IncludeSubdirectories = false;
            watcher.Created += async (s, e) => await HandleFileEvent(e.FullPath);
            watcher.Changed += async (s, e) => await HandleFileEvent(e.FullPath);
            watcher.EnableRaisingEvents = true;

            running = true;
            paused = false;
            btnStartStop.Text = "Stop";
            btnPauseResume.Enabled = true;
            btnPauseResume.Text = "Pause";
            Log($"Started watching {ConfigManager.Current.WatchFolder}");
            trayIcon.ShowBalloonTip(1000, "SadaFileSyncer", "Started watching.", ToolTipIcon.Info);
        }

        void StopWatcher()
        {
            try
            {
                if (watcher != null)
                {
                    watcher.EnableRaisingEvents = false;
                    watcher.Dispose();
                    watcher = null;
                }
            }
            catch { }
            running = false;
            btnStartStop.Text = "Start";
            btnPauseResume.Enabled = false;
            Log("Stopped");
            trayIcon.ShowBalloonTip(800, "SadaFileSyncer", "Stopped.", ToolTipIcon.Info);
        }

        async Task HandleFileEvent(string path)
        {
            if (Directory.Exists(path)) return;
            if (ConfigManager.Current.OnlyNewFiles)
            {
                lock (processed)
                {
                    if (processed.Contains(path)) return;
                    processed.Add(path);
                }
            }

            string fileName = Path.GetFileName(path);
            string dest = Path.Combine(ConfigManager.Current.DestFolder, fileName);

            for (int i = 0; i < 8; i++)
            {
                try
                {
                    if (ConfigManager.Current.CopyMode)
                    {
                        File.Copy(path, dest, true);
                        Log($"Copied {fileName}");
                    }
                    else
                    {
                        if (File.Exists(dest)) File.Delete(dest);
                        File.Move(path, dest);
                        Log($"Moved {fileName}");
                    }
                    return;
                }
                catch (IOException ex)
                {
                    Log($"Retry {i+1} for {fileName}: {ex.Message}");
                    await Task.Delay(500);
                }
                catch (Exception ex)
                {
                    Log($"Error {fileName}: {ex.Message}");
                    return;
                }
            }
            Log($"Failed after retries: {fileName}");
        }

        async Task SyncExistingFiles()
        {
            try
            {
                var files = Directory.GetFiles(ConfigManager.Current.WatchFolder);
                Log($"Syncing {files.Length} existing files...");
                
                foreach (var file in files)
                {
                    await HandleFileEvent(file);
                    await Task.Delay(100); // Small delay between files
                }
                
                Log("Existing files sync completed.");
            }
            catch (Exception ex)
            {
                Log($"Error syncing existing files: {ex.Message}");
            }
        }

        void Log(string msg)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => Log(msg))); return; }
            lstLog.Items.Insert(0, $"{DateTime.Now:HH:mm:ss} - {msg}");
            if (lstLog.Items.Count > 500) lstLog.Items.RemoveAt(lstLog.Items.Count - 1);
            Logger.Log(msg);
        }

        void ApplyStartup(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                if (enable)
                    key.SetValue("SadaFileSyncer", Application.ExecutablePath);
                else
                    key.DeleteValue("SadaFileSyncer", false);
            }
            catch (Exception ex)
            {
                Log("Startup registry change failed: " + ex.Message);
            }
        }

        void ShowAboutDialog()
        {
            using var aboutForm = new Form();
            aboutForm.Text = "About SadaFileSyncer";
            aboutForm.Size = new Size(450, 320);
            aboutForm.StartPosition = FormStartPosition.CenterParent;
            aboutForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            aboutForm.MaximizeBox = false;
            aboutForm.MinimizeBox = false;
            aboutForm.Font = new Font("Segoe UI", 9F);
            
            // Set icon for About dialog
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
                if (File.Exists(iconPath))
                {
                    aboutForm.Icon = new Icon(iconPath);
                }
                else
                {
                    aboutForm.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                }
            }
            catch { }

            var lblTitle = new Label
            {
                Text = "SadaFileSyncer",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                Left = 20,
                Top = 20,
                AutoSize = true
            };
            aboutForm.Controls.Add(lblTitle);

            var lblVersion = new Label
            {
                Text = "Version 1.0",
                Font = new Font("Segoe UI", 9F),
                Left = 20,
                Top = 55,
                AutoSize = true,
                ForeColor = Color.Gray
            };
            aboutForm.Controls.Add(lblVersion);

            var lblDescription = new Label
            {
                Text = "Automatically sync files between folders",
                Left = 20,
                Top = 80,
                Width = 400,
                AutoSize = false,
                Height = 20
            };
            aboutForm.Controls.Add(lblDescription);

            var separator = new Label
            {
                BorderStyle = BorderStyle.Fixed3D,
                Left = 20,
                Top = 110,
                Width = 390,
                Height = 2
            };
            aboutForm.Controls.Add(separator);

            var lblDeveloper = new Label
            {
                Text = "Developer Information",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Left = 20,
                Top = 125,
                AutoSize = true
            };
            aboutForm.Controls.Add(lblDeveloper);

            var lblName = new Label
            {
                Text = "Name: Md Mehedi Hasan",
                Left = 20,
                Top = 155,
                Width = 400,
                AutoSize = false
            };
            aboutForm.Controls.Add(lblName);

            var lblEmail = new Label
            {
                Text = "Email: mehedihasanmondol.online@gmail.com",
                Left = 20,
                Top = 175,
                Width = 400,
                AutoSize = false,
                Cursor = Cursors.Hand,
                ForeColor = Color.Blue
            };
            lblEmail.Click += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "mailto:mehedihasanmondol.online@gmail.com",
                        UseShellExecute = true
                    });
                }
                catch { }
            };
            aboutForm.Controls.Add(lblEmail);

            var lblMobile = new Label
            {
                Text = "Mobile: +880 1912-336505",
                Left = 20,
                Top = 195,
                Width = 400,
                AutoSize = false
            };
            aboutForm.Controls.Add(lblMobile);

            var lblAddress = new Label
            {
                Text = "Address: Bagoan South, Doulatpur\nKushtia, Bangladesh",
                Left = 20,
                Top = 215,
                Width = 400,
                Height = 40,
                AutoSize = false
            };
            aboutForm.Controls.Add(lblAddress);

            var btnClose = new Button
            {
                Text = "Close",
                Left = 330,
                Top = 240,
                Width = 80,
                Height = 30,
                DialogResult = DialogResult.OK
            };
            aboutForm.Controls.Add(btnClose);
            aboutForm.AcceptButton = btnClose;

            aboutForm.ShowDialog(this);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                trayIcon.ShowBalloonTip(500, "SadaFileSyncer", "Minimized to tray.", ToolTipIcon.Info);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            trayIcon.Visible = false;
            StopWatcher();
            SaveSettings();
            base.OnFormClosing(e);
        }
    }
}
