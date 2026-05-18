using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SimpleTodo
{
    public partial class MainForm : Form
    {
        private static readonly Color BgColor = Color.FromArgb(250, 250, 250);
        private static readonly Color TitleBgColor = Color.FromArgb(240, 240, 240);
        private static readonly Color TextColor = Color.FromArgb(30, 30, 30);
        private static readonly Color TextMuted = Color.FromArgb(160, 160, 160);
        private static readonly Color BorderColor = Color.FromArgb(220, 220, 220);
        private static readonly Color AccentColor = Color.FromArgb(0, 103, 192);
        private static readonly Color AccentHoverColor = Color.FromArgb(0, 120, 212);
        private static readonly Color InputBgColor = Color.White;

        private TaskStore store;
        private List<TaskItem> tasks;
        private System.Windows.Forms.Timer autoSaveTimer;
        private System.Windows.Forms.Timer overdueTimer;
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private bool isExiting;

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

        public MainForm()
        {
            InitializeForm();
            SetupTrayIcon();
            store = new TaskStore();
            tasks = store.LoadAll();
            BuildLayout();
            RefreshTaskList();
            SetupAutoSave();
            SetupOverdueCheck();
            CheckOverdueOnStartup();
        }

        private void InitializeForm()
        {
            this.Text = "SimpleTodo";
            this.Size = new Size(380, 540);
            this.MinimumSize = new Size(300, 360);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.BackColor = BgColor;
            this.DoubleBuffered = true;
            this.ShowInTaskbar = true;
            PositionWindow();

            // Paint outer border
            this.Paint += (s, e) =>
            {
                using (var pen = new Pen(BorderColor))
                    e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
            };
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
                cp.ExStyle |= 0x00040000; // WS_EX_APPWINDOW — force taskbar entry
                return cp;
            }
        }

        private void BuildLayout()
        {
            BuildTitleBar();
            BuildInputPanel();
            BuildTaskList();
        }

        private void BuildTitleBar()
        {
            titleBar = new Panel
            {
                Height = 30,
                Dock = DockStyle.Top,
                BackColor = TitleBgColor
            };

            lblTitle = new Label
            {
                Text = "SimpleTodo",
                Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Bold),
                ForeColor = TextColor,
                BackColor = Color.Transparent,
                Location = new Point(10, 6),
                AutoSize = true
            };

            btnMinimize = new Button
            {
                Text = "─",
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei UI", 9f),
                Size = new Size(28, 22),
                Location = new Point(this.Width - 62, 4),
                ForeColor = TextColor,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnMinimize.FlatAppearance.BorderSize = 0;
            btnMinimize.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 220, 220);
            btnMinimize.Click += (s, e) => this.WindowState = FormWindowState.Minimized;

            btnClose = new Button
            {
                Text = "×",
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei UI", 11f, FontStyle.Bold),
                Size = new Size(28, 22),
                Location = new Point(this.Width - 34, 4),
                ForeColor = TextColor,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 17, 35);
            btnClose.FlatAppearance.MouseDownBackColor = Color.FromArgb(200, 15, 30);
            btnClose.MouseEnter += (s, e) => btnClose.ForeColor = Color.White;
            btnClose.MouseLeave += (s, e) => btnClose.ForeColor = TextColor;
            btnClose.Click += (s, e) => HideToTray();

            titleBar.Controls.Add(lblTitle);
            titleBar.Controls.Add(btnMinimize);
            titleBar.Controls.Add(btnClose);

            // Drag to move
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

            this.Controls.Add(titleBar);
        }

        private void BuildInputPanel()
        {
            inputPanel = new Panel
            {
                Height = 72,
                Dock = DockStyle.Top,
                BackColor = BgColor,
                Padding = new Padding(10, 6, 10, 4)
            };

            lblTaskTitle = new Label
            {
                Text = "任务:",
                Font = new Font("Microsoft YaHei UI", 9f),
                ForeColor = TextColor,
                BackColor = Color.Transparent,
                Location = new Point(10, 8),
                AutoSize = true
            };

            txtNewTitle = new TextBox
            {
                Font = new Font("Microsoft YaHei UI", 9f),
                ForeColor = TextColor,
                BackColor = InputBgColor,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(48, 6),
                Size = new Size(210, 22)
            };
            txtNewTitle.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                    AddTask();
            };

            lblDdl = new Label
            {
                Text = "DDL:",
                Font = new Font("Microsoft YaHei UI", 8f),
                ForeColor = TextMuted,
                BackColor = Color.Transparent,
                Location = new Point(10, 34),
                AutoSize = true
            };

            txtDdl = new TextBox
            {
                Font = new Font("Microsoft YaHei UI", 8f),
                ForeColor = TextMuted,
                BackColor = InputBgColor,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(48, 32),
                Size = new Size(110, 20)
            };

            btnAdd = new Button
            {
                Text = "添加任务",
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei UI", 9f),
                Size = new Size(80, 46),
                Location = new Point(270, 6),
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

            // Separator line
            var sep = new Panel
            {
                Height = 1,
                Dock = DockStyle.Bottom,
                BackColor = BorderColor
            };
            inputPanel.Controls.Add(sep);

            this.Controls.Add(inputPanel);
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
            this.Controls.Add(taskListPanel);
        }

        // --- System Tray ---

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
            if (this.Visible)
                HideToTray();
            else
            {
                this.Show();
                NativeMethods.SetForegroundWindow(this.Handle);
                txtNewTitle.Focus();
            }
        }

        private void HideToTray()
        {
            this.Hide();
        }

        private void ExitApp()
        {
            isExiting = true;
            if (autoSaveTimer != null) autoSaveTimer.Stop();
            if (overdueTimer != null) overdueTimer.Stop();
            store.SaveAll(tasks);
            trayIcon.Visible = false;
            trayIcon.Dispose();
            Application.Exit();
        }

        // --- Auto Save ---

        private void SetupAutoSave()
        {
            autoSaveTimer = new System.Windows.Forms.Timer
            {
                Interval = 500
            };
            autoSaveTimer.Tick += (s, e) =>
            {
                autoSaveTimer.Stop();
                store.SaveAll(tasks);
            };
        }

        private void RequestAutoSave()
        {
            autoSaveTimer.Stop();
            autoSaveTimer.Start();
        }

        // --- Overdue Check ---

        private void SetupOverdueCheck()
        {
            overdueTimer = new System.Windows.Forms.Timer
            {
                Interval = 60000 // Check every 60 seconds
            };
            overdueTimer.Tick += (s, e) =>
            {
                var overdueCount = tasks.Count(t => t.IsOverdue);
                if (overdueCount > 0)
                {
                    trayIcon.BalloonTipTitle = "待办事项逾期";
                    trayIcon.BalloonTipText = "你有 " + overdueCount + " 项任务已逾期";
                    trayIcon.BalloonTipIcon = ToolTipIcon.Warning;
                    trayIcon.ShowBalloonTip(5000);
                    RefreshTaskList();
                }
            };
            overdueTimer.Start();
        }

        private void CheckOverdueOnStartup()
        {
            var overdueCount = tasks.Count(t => t.IsOverdue);
            if (overdueCount > 0)
            {
                // Delay balloon so tray icon is ready
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

        // --- Task Operations ---

        private void AddTask()
        {
            var title = txtNewTitle.Text.Trim();
            if (string.IsNullOrEmpty(title)) return;

            var item = TaskItem.CreateNew(title);

            // Parse DDL
            var ddlText = txtDdl.Text.Trim();
            if (!string.IsNullOrEmpty(ddlText))
            {
                DateTime ddl;
                if (DateTime.TryParseExact(ddlText, "yyyy/MM/dd",
                    null, System.Globalization.DateTimeStyles.None, out ddl))
                {
                    item.Deadline = ddl;
                }
            }

            // Calculate sort order: place after last root-level task
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
            RequestAutoSave();
            RefreshTaskList();
        }

        private void OnSubTaskAdded(TaskItem parent)
        {
            // Limit to 3 levels: root (0), sub (1), sub-sub (2)
            if (parent.Depth >= 2) return;

            var item = TaskItem.CreateNew("新子任务", parent.Id);
            item.Depth = parent.Depth + 1;

            // Place after parent's last child, or right after parent
            var siblings = tasks.Where(t => t.ParentId == parent.Id).ToList();
            item.SortOrder = siblings.Count > 0 ? siblings.Max(t => t.SortOrder) + 1 : 0;

            // Insert into flat list after parent's last descendant
            int insertIdx = FindLastDescendantIndex(parent);
            tasks.Insert(insertIdx + 1, item);

            // Auto-expand parent
            parent.IsExpanded = true;

            RequestAutoSave();
            RefreshTaskList();

            // Enter edit mode on the new subtask
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

        // --- Refresh ---

        private void RefreshTaskList()
        {
            var flatList = TaskStore.BuildFlatList(
                TaskStore.BuildTree(tasks));
            taskListPanel.Populate(flatList);
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

        // --- Form Events ---

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && !isExiting)
            {
                e.Cancel = true;
                HideToTray();
                return;
            }

            // Actually closing
            if (autoSaveTimer != null) autoSaveTimer.Stop();
            if (overdueTimer != null) overdueTimer.Stop();
            store.SaveAll(tasks);
            trayIcon.Visible = false;
            trayIcon.Dispose();
            base.OnFormClosing(e);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            // Reposition title bar buttons
            if (btnMinimize != null)
                btnMinimize.Location = new Point(this.Width - 62, 4);
            if (btnClose != null)
                btnClose.Location = new Point(this.Width - 34, 4);
            // Reposition add button in input
            if (btnAdd != null)
                btnAdd.Location = new Point(this.Width - 110, 6);
            if (txtNewTitle != null)
                txtNewTitle.Size = new Size(this.Width - 170, 22);
            Invalidate();
        }
    }
}
