using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SimpleTodo
{
    public partial class MainForm : Form
    {
        private static readonly Color BgColor = Color.FromArgb(250, 250, 250);
        private static readonly Color SurfaceColor = Color.FromArgb(240, 240, 240);
        private static readonly Color TextColor = Color.FromArgb(28, 28, 30);
        private static readonly Color TextSecondary = Color.FromArgb(142, 142, 147);
        private static readonly Color DividerColor = Color.FromArgb(229, 229, 234);
        private static readonly Color AccentColor = Color.FromArgb(234, 88, 12);
        private static readonly Color AccentHoverColor = Color.FromArgb(208, 74, 0);
        private static readonly Color InputBgColor = Color.White;
        private static readonly Color NotepadBg = Color.FromArgb(245, 245, 247);

        private TaskStore store;
        private List<TaskItem> tasks;
        private System.Windows.Forms.Timer autoSaveTimer;
        private System.Windows.Forms.Timer overdueTimer;
        private System.Windows.Forms.Timer notepadSaveTimer;
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private bool isExiting;
        private bool overdueBalloonShown;
        private System.Windows.Forms.Timer mouseLeaveTimer;
        private System.Windows.Forms.Timer animTimer;
        private int mouseOutsideTicks;
        private bool mouseHasEntered;
        private bool isAnimating;
        private bool animatingDown;
        private int normalY;
        private int collapsedY;
        private const int ANIM_STEP = 32;
        private string notesFilePath;

        // Title bar
        private Panel titleBar;
        private Label lblTitle;
        private Button btnMinimize;
        private Button btnClose;
        private Point dragStart;

        // Input area
        private Panel inputPanel;
        private Label lblTaskTitle;
        private TextBox txtNewTitle;
        private Label lblDdl;
        private TextBox txtDdl;
        private Button btnAdd;

        // Task list
        private TaskListPanel taskListPanel;

        // Notepad
        private Panel notepadPanel;
        private Panel notepadHeader;
        private Label lblNotepadTitle;
        private Button btnNotepadToggle;
        private TextBox txtNotepad;
        private bool notepadExpanded;
        private const int NotepadExpandedHeight = 170;
        private const int NotepadCollapsedHeight = 28;

        // Footer
        private Panel footer;
        private Label lblStats;
        private Button btnClearCompleted;
        private bool showCompleted = true;
        private const int HOTKEY_ID = 1;

        public MainForm()
        {
            InitializeForm();
            SetupTrayIcon();
            store = new TaskStore();
            tasks = store.LoadAll();
            LoadNotes();
            BuildLayout();
            RefreshTaskList();
            SetupAutoSave();
            SetupOverdueCheck();
            CheckOverdueOnStartup();
            SetupMouseLeaveDetection();
            SetupAnimation();
            CreateStartupShortcut();
            this.BeginInvoke((Action)(() => this.Hide()));
        }

        private void InitializeForm()
        {
            this.Text = "SimpleTodo";
            this.Size = new Size(380, 610);
            this.MinimumSize = new Size(300, 440);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.BackColor = BgColor;
            this.DoubleBuffered = true;
            this.ShowInTaskbar = true;
            notepadExpanded = true;
            PositionWindow();
            this.Opacity = 0;

            try
            {
                var attr = NativeMethods.DWMWCP_ROUND;
                NativeMethods.DwmSetWindowAttribute(this.Handle, NativeMethods.DWMWA_WINDOW_CORNER_PREFERENCE,
                    ref attr, sizeof(int));
            }
            catch { }
        }

        private void PositionWindow()
        {
            var screen = Screen.PrimaryScreen;
            var workArea = screen.WorkingArea;
            int x = workArea.Left + 8;
            int y = workArea.Bottom - this.Height - 8;
            this.Location = new Point(x, y);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x00040000;
                cp.ClassStyle |= 0x00020000;
                return cp;
            }
        }

        private void BuildLayout()
        {
            BuildTitleBar();
            BuildInputPanel();
            BuildTaskList();
            BuildNotepad();
            BuildFooter();

            Controls.Add(taskListPanel);
            Controls.Add(notepadPanel);
            Controls.Add(footer);
            Controls.Add(inputPanel);
            Controls.Add(titleBar);
        }

        private void BuildTitleBar()
        {
            titleBar = new Panel
            {
                Height = 32,
                Dock = DockStyle.Top,
                BackColor = SurfaceColor
            };

            lblTitle = new Label
            {
                Text = "待办清单",
                Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Bold),
                ForeColor = TextColor,
                BackColor = Color.Transparent,
                Location = new Point(12, 7),
                AutoSize = true
            };

            btnMinimize = new Button
            {
                Text = "─",
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei UI", 9f),
                Size = new Size(28, 24),
                Location = new Point(this.Width - 62, 4),
                ForeColor = TextSecondary,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnMinimize.FlatAppearance.BorderSize = 0;
            btnMinimize.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 220, 224);
            btnMinimize.Click += (s, e) => this.WindowState = FormWindowState.Minimized;

            btnClose = new Button
            {
                Text = "×",
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei UI", 11f),
                Size = new Size(28, 24),
                Location = new Point(this.Width - 34, 4),
                ForeColor = TextSecondary,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 59, 48);
            btnClose.MouseEnter += (s, e) => btnClose.ForeColor = Color.White;
            btnClose.MouseLeave += (s, e) => btnClose.ForeColor = TextSecondary;
            btnClose.Click += (s, e) => HideToTray();

            titleBar.Controls.Add(lblTitle);
            titleBar.Controls.Add(btnMinimize);
            titleBar.Controls.Add(btnClose);

            titleBar.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                    dragStart = e.Location;
            };
            titleBar.MouseMove += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    this.Location = new Point(
                        this.Location.X + e.X - dragStart.X,
                        this.Location.Y + e.Y - dragStart.Y);
                }
            };
        }

        private void BuildInputPanel()
        {
            inputPanel = new Panel
            {
                Height = 78,
                Dock = DockStyle.Top,
                BackColor = BgColor,
                Padding = new Padding(12, 8, 12, 6)
            };

            lblTaskTitle = new Label
            {
                Text = "新任务",
                Font = new Font("Microsoft YaHei UI", 9f),
                ForeColor = TextColor,
                BackColor = Color.Transparent,
                Location = new Point(12, 10),
                AutoSize = true
            };

            txtNewTitle = new TextBox
            {
                Font = new Font("Microsoft YaHei UI", 9f),
                ForeColor = TextColor,
                BackColor = InputBgColor,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(62, 8),
                Size = new Size(218, 24)
            };
            txtNewTitle.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                    AddTask();
            };

            lblDdl = new Label
            {
                Text = "截止",
                Font = new Font("Microsoft YaHei UI", 8f),
                ForeColor = TextSecondary,
                BackColor = Color.Transparent,
                Location = new Point(12, 40),
                AutoSize = true
            };

            txtDdl = new TextBox
            {
                Font = new Font("Microsoft YaHei UI", 8f),
                ForeColor = TextSecondary,
                BackColor = InputBgColor,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(62, 38),
                Size = new Size(120, 22)
            };

            btnAdd = new Button
            {
                Text = "添加",
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Bold),
                Size = new Size(68, 48),
                Location = new Point(292, 8),
                BackColor = AccentColor,
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.FlatAppearance.MouseOverBackColor = AccentHoverColor;
            btnAdd.Click += (s, e) => AddTask();

            inputPanel.Controls.Add(lblTaskTitle);
            inputPanel.Controls.Add(txtNewTitle);
            inputPanel.Controls.Add(lblDdl);
            inputPanel.Controls.Add(txtDdl);
            inputPanel.Controls.Add(btnAdd);

            var sep = new Panel
            {
                Height = 1,
                Dock = DockStyle.Bottom,
                BackColor = DividerColor
            };
            inputPanel.Controls.Add(sep);
        }

        private void BuildTaskList()
        {
            taskListPanel = new TaskListPanel
            {
                Dock = DockStyle.Fill,
                BackColor = BgColor
            };
            taskListPanel.TaskChanged += OnTaskChanged;
            taskListPanel.SubTaskAdded += OnSubTaskAdded;
            taskListPanel.DeleteRequested += OnDeleteRequested;
            taskListPanel.ToggleExpand += OnToggleExpand;
            taskListPanel.CompletedToggled += () => RefreshTaskList();
        }

        private void BuildNotepad()
        {
            notepadPanel = new Panel
            {
                Height = NotepadExpandedHeight,
                Dock = DockStyle.Bottom,
                BackColor = NotepadBg
            };

            notepadPanel.Paint += (s, e) =>
            {
                using (var pen = new Pen(DividerColor, 1))
                    e.Graphics.DrawLine(pen, 0, 0, notepadPanel.Width, 0);
            };

            notepadHeader = new Panel
            {
                Height = 28,
                Dock = DockStyle.Top,
                BackColor = SurfaceColor,
                Cursor = Cursors.Hand
            };

            btnNotepadToggle = new Button
            {
                Text = notepadExpanded ? "▾" : "▸",
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei UI", 9f),
                Size = new Size(20, 20),
                Location = new Point(8, 4),
                ForeColor = TextSecondary,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnNotepadToggle.FlatAppearance.BorderSize = 0;
            btnNotepadToggle.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 220, 224);
            btnNotepadToggle.Click += (s, e) => ToggleNotepad();

            lblNotepadTitle = new Label
            {
                Text = "记事本",
                Font = new Font("Microsoft YaHei UI", 8f, FontStyle.Bold),
                ForeColor = TextSecondary,
                BackColor = Color.Transparent,
                Location = new Point(32, 6),
                AutoSize = true
            };

            notepadHeader.Controls.Add(btnNotepadToggle);
            notepadHeader.Controls.Add(lblNotepadTitle);
            notepadHeader.Click += (s, e) => ToggleNotepad();

            txtNotepad = new TextBox
            {
                Multiline = true,
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei UI", 9f),
                ForeColor = TextColor,
                BackColor = NotepadBg,
                BorderStyle = BorderStyle.None,
                ScrollBars = ScrollBars.Vertical,
                AcceptsReturn = true,
                AcceptsTab = false
            };
            txtNotepad.TextChanged += (s, e) =>
            {
                notepadSaveTimer.Stop();
                notepadSaveTimer.Start();
            };

            notepadPanel.Controls.Add(txtNotepad);
            notepadPanel.Controls.Add(notepadHeader);

            UpdateNotepadState();
        }

        private void ToggleNotepad()
        {
            notepadExpanded = !notepadExpanded;
            UpdateNotepadState();
        }

        private void UpdateNotepadState()
        {
            if (notepadExpanded)
            {
                notepadPanel.Height = NotepadExpandedHeight;
                txtNotepad.Visible = true;
                btnNotepadToggle.Text = "▾";
            }
            else
            {
                notepadPanel.Height = NotepadCollapsedHeight;
                txtNotepad.Visible = false;
                btnNotepadToggle.Text = "▸";
            }
        }

        private void LoadNotes()
        {
            notesFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SimpleTodo", "notes.txt");
        }

        private void ApplyLoadedNotes()
        {
            if (txtNotepad == null) return;
            try
            {
                if (File.Exists(notesFilePath))
                    txtNotepad.Text = File.ReadAllText(notesFilePath);
            }
            catch { }
        }

        private void SaveNotes()
        {
            if (txtNotepad == null || string.IsNullOrEmpty(notesFilePath)) return;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(notesFilePath));
                File.WriteAllText(notesFilePath, txtNotepad.Text);
            }
            catch { }
        }

        private void BuildFooter()
        {
            footer = new Panel
            {
                Height = 28,
                Dock = DockStyle.Bottom,
                BackColor = SurfaceColor
            };

            lblStats = new Label
            {
                Font = new Font("Microsoft YaHei UI", 7.5f),
                ForeColor = TextSecondary,
                BackColor = Color.Transparent,
                Location = new Point(12, 6),
                AutoSize = true
            };

            btnClearCompleted = new Button
            {
                Text = "清除已完成",
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei UI", 7.5f),
                Size = new Size(72, 20),
                Location = new Point(this.Width - 84, 4),
                BackColor = Color.Transparent,
                ForeColor = TextSecondary,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnClearCompleted.FlatAppearance.BorderSize = 0;
            btnClearCompleted.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 220, 224);
            btnClearCompleted.Click += (s, e) => ClearCompleted();

            footer.Controls.Add(lblStats);
            footer.Controls.Add(btnClearCompleted);

            footer.Paint += (s, e) =>
            {
                using (var pen = new Pen(DividerColor))
                    e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0);
            };
        }

        private void UpdateStats()
        {
            if (lblStats == null) return;
            var active = tasks.FindAll(t => !t.Completed && t.ParentId == null).Count;
            var overdue = tasks.FindAll(t => t.IsOverdue).Count;
            var completed = tasks.FindAll(t => t.Completed).Count;
            var text = active + " 待办";
            if (overdue > 0)
                text += "  ·  " + overdue + " 逾期";
            if (completed > 0)
                text += "  ·  " + completed + " 已完成";
            lblStats.Text = text;
            lblStats.Location = new Point(12, 6);

            btnClearCompleted.Location = new Point(this.Width - 84, 4);
        }

        private void ClearCompleted()
        {
            var completed = tasks.FindAll(t => t.Completed).ToList();
            if (completed.Count == 0) return;
            foreach (var t in completed)
                RemoveTaskRecursive(t);
            RequestAutoSave();
            RefreshTaskList();
        }

        // ── System Tray ──

        private void SetupTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("显示/隐藏", null, (s, e) => ToggleVisibility());
            trayMenu.Items.Add("-");
            trayMenu.Items.Add("退出", null, (s, e) => ExitApp());

            trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "SimpleTodo",
                Visible = true,
                ContextMenuStrip = trayMenu
            };

            trayIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                    ToggleVisibility();
            };
        }

        private void ToggleVisibility()
        {
            if (this.Visible && !isAnimating)
                CollapseToBottom();
            else if (!this.Visible && !isAnimating)
                PopupFromBottom();
        }

        private void HideToTray()
        {
            CollapseToBottom();
        }

        private void SetupMouseLeaveDetection()
        {
            mouseLeaveTimer = new System.Windows.Forms.Timer { Interval = 250 };
            mouseLeaveTimer.Tick += (s, e) =>
            {
                if (!this.Visible || isAnimating) return;

                var expandedBounds = new Rectangle(
                    this.Bounds.X - 10, this.Bounds.Y - 10,
                    this.Bounds.Width + 20, this.Bounds.Height + 12);

                if (expandedBounds.Contains(Cursor.Position))
                {
                    mouseOutsideTicks = 0;
                    mouseHasEntered = true;
                }
                else if (mouseHasEntered)
                {
                    mouseOutsideTicks++;
                    if (mouseOutsideTicks >= 3)
                    {
                        mouseOutsideTicks = 0;
                        mouseLeaveTimer.Stop();
                        CollapseToBottom();
                    }
                }
            };
        }

        private void SetupAnimation()
        {
            animTimer = new System.Windows.Forms.Timer { Interval = 16 };
            animTimer.Tick += AnimTimer_Tick;
            var workArea = Screen.PrimaryScreen.WorkingArea;
            normalY = workArea.Bottom - this.Height - 8;
            collapsedY = workArea.Bottom - 4;
        }

        private void PopupFromBottom()
        {
            var workArea = Screen.PrimaryScreen.WorkingArea;
            normalY = workArea.Bottom - this.Height - 8;
            collapsedY = workArea.Bottom - 4;

            this.Opacity = 1;
            this.Location = new Point(this.Location.X, collapsedY);
            this.Show();
            NativeMethods.SetForegroundWindow(this.Handle);

            animatingDown = false;
            isAnimating = true;
            animTimer.Start();
        }

        private void CollapseToBottom()
        {
            if (!this.Visible) return;
            if (isAnimating && animatingDown) return;
            mouseLeaveTimer.Stop();
            mouseOutsideTicks = 0;
            mouseHasEntered = false;
            animatingDown = true;
            isAnimating = true;
            animTimer.Start();
        }

        private void AnimTimer_Tick(object sender, EventArgs e)
        {
            int targetY = animatingDown ? collapsedY : normalY;
            int currentY = this.Location.Y;

            if (currentY == targetY)
            {
                animTimer.Stop();
                isAnimating = false;
                if (animatingDown)
                    this.Hide();
                else
                {
                    mouseLeaveTimer.Start();
                    txtNewTitle.Focus();
                }
                return;
            }

            int step = animatingDown ? ANIM_STEP : -ANIM_STEP;
            int newY = currentY + step;
            if (animatingDown)
                newY = Math.Min(newY, targetY);
            else
                newY = Math.Max(newY, targetY);

            this.Location = new Point(this.Location.X, newY);
        }

        private void CreateStartupShortcut()
        {
            try
            {
                string startupDir = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                string shortcutPath = Path.Combine(startupDir, "SimpleTodo.lnk");
                if (File.Exists(shortcutPath)) return;

                string exePath = Application.ExecutablePath;
                string dir = Path.GetDirectoryName(exePath);

                string scriptPath = Path.Combine(Path.GetTempPath(), "simpletodo_setup.ps1");
                string ps = string.Format(
                    "$s=(New-Object -COM WScript.Shell).CreateShortcut('{0}');" +
                    "$s.TargetPath='{1}';" +
                    "$s.WorkingDirectory='{2}';" +
                    "$s.Save()",
                    shortcutPath.Replace("'", "''"),
                    exePath.Replace("'", "''"),
                    dir.Replace("'", "''"));
                File.WriteAllText(scriptPath, ps);

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File \"" + scriptPath + "\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch { }
        }

        private void ExitApp()
        {
            isExiting = true;
            NativeMethods.UnregisterHotKey(this.Handle, HOTKEY_ID);
            if (autoSaveTimer != null) autoSaveTimer.Stop();
            if (overdueTimer != null) overdueTimer.Stop();
            if (notepadSaveTimer != null) { notepadSaveTimer.Stop(); SaveNotes(); }
            if (mouseLeaveTimer != null) mouseLeaveTimer.Stop();
            if (animTimer != null) animTimer.Stop();
            store.SaveAll(tasks);
            trayIcon.Visible = false;
            trayIcon.Dispose();
            Application.Exit();
        }

        // ── Auto Save ──

        private void SetupAutoSave()
        {
            autoSaveTimer = new System.Windows.Forms.Timer { Interval = 500 };
            autoSaveTimer.Tick += (s, e) =>
            {
                autoSaveTimer.Stop();
                store.SaveAll(tasks);
            };

            notepadSaveTimer = new System.Windows.Forms.Timer { Interval = 500 };
            notepadSaveTimer.Tick += (s, e) =>
            {
                notepadSaveTimer.Stop();
                SaveNotes();
            };

            this.BeginInvoke((Action)(() => ApplyLoadedNotes()));
        }

        private void RequestAutoSave()
        {
            autoSaveTimer.Stop();
            autoSaveTimer.Start();
        }

        // ── Overdue Check ──

        private void SetupOverdueCheck()
        {
            overdueTimer = new System.Windows.Forms.Timer { Interval = 60000 };
            overdueTimer.Tick += (s, e) =>
            {
                var overdueCount = tasks.Count(t => t.IsOverdue);
                if (overdueCount > 0)
                {
                    RefreshTaskList();
                    if (!overdueBalloonShown)
                    {
                        overdueBalloonShown = true;
                        trayIcon.BalloonTipTitle = "待办事项逾期";
                        trayIcon.BalloonTipText = "你有 " + overdueCount + " 项任务已逾期";
                        trayIcon.BalloonTipIcon = ToolTipIcon.Warning;
                        trayIcon.ShowBalloonTip(5000);
                    }
                }
            };
            overdueTimer.Start();
        }

        private void CheckOverdueOnStartup()
        {
            var overdueCount = tasks.Count(t => t.IsOverdue);
            if (overdueCount > 0)
            {
                overdueBalloonShown = true;
                var initTimer = new System.Windows.Forms.Timer { Interval = 2000 };
                initTimer.Tick += (s, e) =>
                {
                    initTimer.Stop();
                    initTimer.Dispose();
                    trayIcon.BalloonTipTitle = "待办事项";
                    trayIcon.BalloonTipText = "你有 " + overdueCount + " 项任务已逾期";
                    trayIcon.BalloonTipIcon = ToolTipIcon.Warning;
                    trayIcon.ShowBalloonTip(5000);
                };
                initTimer.Start();
            }
        }

        // ── Task Operations ──

        private void AddTask()
        {
            var title = txtNewTitle.Text.Trim();
            if (string.IsNullOrEmpty(title)) return;

            var item = TaskItem.CreateNew(title);

            var ddlText = txtDdl.Text.Trim();
            if (!string.IsNullOrEmpty(ddlText))
            {
                DateTime ddl;
                if (TaskItem.TryParseDdl(ddlText, out ddl))
                    item.Deadline = ddl;
            }

            var roots = tasks.Where(t => t.Depth == 0).ToList();
            item.SortOrder = roots.Count > 0 ? roots.Max(t => t.SortOrder) + 1 : 0;

            tasks.Add(item);
            txtNewTitle.Clear();
            txtDdl.Clear();
            txtNewTitle.Focus();

            RequestAutoSave();
            RefreshTaskList();
            taskListPanel.ScrollToBottom();
        }

        private void OnTaskChanged(TaskItem task)
        {
            if (task.Completed)
                CompleteDescendants(task);
            RequestAutoSave();
            RefreshTaskList();
        }

        private void CompleteDescendants(TaskItem parent)
        {
            foreach (var t in tasks)
            {
                if (t.ParentId == parent.Id && !t.Completed)
                {
                    t.Completed = true;
                    t.CompletedAt = DateTime.Now;
                    CompleteDescendants(t);
                }
            }
        }

        private void OnSubTaskAdded(TaskItem parent)
        {
            if (parent.Depth >= 2) return;

            var item = TaskItem.CreateNew("新子任务", parent.Id);
            item.Depth = parent.Depth + 1;

            var siblings = tasks.Where(t => t.ParentId == parent.Id).ToList();
            item.SortOrder = siblings.Count > 0 ? siblings.Max(t => t.SortOrder) + 1 : 0;

            int insertIdx = FindLastDescendantIndex(parent);
            tasks.Insert(insertIdx + 1, item);

            parent.IsExpanded = true;

            RequestAutoSave();
            RefreshTaskList();

            var row = FindRowForTask(item);
            if (row != null) row.EnterEditMode();
        }

        private int FindLastDescendantIndex(TaskItem parent)
        {
            int parentIdx = tasks.IndexOf(parent);
            if (parentIdx < 0) return tasks.Count - 1;

            for (int i = parentIdx + 1; i < tasks.Count; i++)
            {
                if (tasks[i].Depth <= parent.Depth)
                    return i - 1;
            }
            return tasks.Count - 1;
        }

        private void OnDeleteRequested(TaskItem task)
        {
            if (task.HasChildren)
            {
                var result = MessageBox.Show(
                    "删除「" + task.Title + "」及其所有子任务？",
                    "确认删除",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Warning);
                if (result != DialogResult.OK) return;
            }

            RemoveTaskRecursive(task);
            RequestAutoSave();
            RefreshTaskList();
        }

        private void RemoveTaskRecursive(TaskItem task)
        {
            var children = tasks.Where(t => t.ParentId == task.Id).ToList();
            foreach (var child in children)
                RemoveTaskRecursive(child);
            tasks.Remove(task);
        }

        private void OnToggleExpand(TaskItem task)
        {
            task.IsExpanded = !task.IsExpanded;
            RequestAutoSave();
            RefreshTaskList();
        }

        // ── Refresh ──

        private void RefreshTaskList()
        {
            var tree = TaskStore.BuildTree(tasks);
            var flatList = TaskStore.BuildFlatList(tree, true);
            taskListPanel.Populate(flatList, showCompleted);
            UpdateStats();
        }

        private TaskRow FindRowForTask(TaskItem task)
        {
            foreach (Control c in taskListPanel.Controls)
            {
                var row = c as TaskRow;
                if (row != null && row.Task == task)
                    return row;
            }
            return null;
        }

        // ── Form Events ──

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && !isExiting)
            {
                e.Cancel = true;
                HideToTray();
                return;
            }

            if (autoSaveTimer != null) autoSaveTimer.Stop();
            if (overdueTimer != null) overdueTimer.Stop();
            if (notepadSaveTimer != null) { notepadSaveTimer.Stop(); SaveNotes(); }
            if (mouseLeaveTimer != null) mouseLeaveTimer.Stop();
            if (animTimer != null) animTimer.Stop();
            store.SaveAll(tasks);
            trayIcon.Visible = false;
            trayIcon.Dispose();
            base.OnFormClosing(e);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            NativeMethods.RegisterHotKey(this.Handle, HOTKEY_ID,
                (uint)(NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT), (uint)Keys.T);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                ToggleVisibility();
                return;
            }
            base.WndProc(ref m);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (btnMinimize != null)
                btnMinimize.Location = new Point(this.Width - 62, 4);
            if (btnClose != null)
                btnClose.Location = new Point(this.Width - 34, 4);
            if (btnAdd != null)
                btnAdd.Location = new Point(this.Width - 80, 8);
            if (txtNewTitle != null)
                txtNewTitle.Size = new Size(this.Width - 182, 24);
            if (btnClearCompleted != null)
                btnClearCompleted.Location = new Point(this.Width - 84, 4);
            if (txtNotepad != null && notepadHeader != null)
            {
                btnNotepadToggle.Location = new Point(8, 4);
                lblNotepadTitle.Location = new Point(32, 6);
            }
            Invalidate();
        }
    }
}
