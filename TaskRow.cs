using System;
using System.Drawing;
using System.Windows.Forms;

namespace SimpleTodo
{
    public class TaskRow : UserControl
    {
        private static readonly Font TitleFont = new Font("Microsoft YaHei UI", 10f);
        private static readonly Font TitleFontStrike = new Font("Microsoft YaHei UI", 10f, FontStyle.Strikeout);
        private static readonly Font DateFont = new Font("Microsoft YaHei UI", 7.5f);
        private static readonly Font DateFontOverdue = new Font("Microsoft YaHei UI", 7.5f, FontStyle.Bold);
        private static readonly Font BtnFont = new Font("Microsoft YaHei UI", 9f);

        private static readonly Color TextColor = Color.FromArgb(30, 30, 30);
        private static readonly Color TextMuted = Color.FromArgb(160, 160, 160);
        private static readonly Color TextCompleted = Color.FromArgb(192, 192, 192);
        private static readonly Color OverdueColor = Color.FromArgb(211, 47, 47);
        private static readonly Color BgColor = Color.FromArgb(250, 250, 250);
        private static readonly Color HoverColor = Color.FromArgb(240, 240, 240);
        private static readonly Color BorderColor = Color.FromArgb(232, 232, 232);

        private TaskItem task;
        private bool isEditing;
        private bool isEditingDdl;

        // Title row controls
        private Button btnCollapse;
        private CheckBox chkCompleted;
        private Label lblTitle;
        private TextBox txtTitle;
        private Button btnAddSub;
        private Button btnDelete;

        // Date row
        private Label lblDate;
        private TextBox txtDdlEdit;

        private bool showOverdue;

        public TaskItem Task { get { return task; } }

        public event Action<TaskItem> TaskChanged;
        public event Action<TaskItem> SubTaskAdded;
        public event Action<TaskItem> DeleteRequested;
        public event Action<TaskItem> ToggleExpand;

        public TaskRow(TaskItem task)
        {
            this.task = task;
            this.BackColor = BgColor;
            this.DoubleBuffered = true;
            this.Margin = new Padding(0);
            this.Padding = new Padding(0);

            BuildControls();
            Bind(task);
            this.Height = 52;
        }

        private void BuildControls()
        {
            // Collapse button
            btnCollapse = new Button
            {
                Text = "▸",
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei UI", 9f),
                Size = new Size(20, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                ForeColor = TextMuted,
                Visible = false,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            btnCollapse.FlatAppearance.BorderSize = 0;
            btnCollapse.FlatAppearance.MouseOverBackColor = HoverColor;
            btnCollapse.Click += (s, e) => { if (ToggleExpand != null) ToggleExpand(task); };

            // Checkbox
            chkCompleted = new CheckBox
            {
                Size = new Size(16, 16),
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0),
                FlatStyle = FlatStyle.Flat
            };
            chkCompleted.FlatAppearance.BorderSize = 1;
            chkCompleted.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            chkCompleted.CheckedChanged += OnCheckChanged;

            // Title label (default view)
            lblTitle = new Label
            {
                AutoSize = false,
                Font = TitleFont,
                ForeColor = TextColor,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            lblTitle.DoubleClick += (s, e) => EnterEditMode();

            // Title textbox (edit mode, hidden by default)
            txtTitle = new TextBox
            {
                Font = TitleFont,
                ForeColor = TextColor,
                BackColor = Color.White,
                BorderStyle = BorderStyle.None,
                Visible = false,
                Margin = new Padding(0)
            };
            txtTitle.KeyDown += OnTitleKeyDown;
            txtTitle.LostFocus += (s, e) => CommitEdit();

            // Add subtask button
            btnAddSub = new Button
            {
                Text = "+",
                FlatStyle = FlatStyle.Flat,
                Font = BtnFont,
                Size = new Size(20, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                ForeColor = TextMuted,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            btnAddSub.FlatAppearance.BorderSize = 0;
            btnAddSub.FlatAppearance.MouseOverBackColor = HoverColor;
            btnAddSub.Click += (s, e) => { if (SubTaskAdded != null) SubTaskAdded(task); };

            // Delete button
            btnDelete = new Button
            {
                Text = "×",
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei UI", 10f, FontStyle.Bold),
                Size = new Size(20, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                ForeColor = TextMuted,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 230, 230);
            btnDelete.Click += (s, e) => { if (DeleteRequested != null) DeleteRequested(task); };

            // Date label
            lblDate = new Label
            {
                AutoSize = false,
                Font = DateFont,
                ForeColor = TextMuted,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Height = 18,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            lblDate.DoubleClick += (s, e) => EnterDdlEditMode();

            // DDL edit textbox (hidden, shown on double-click date)
            txtDdlEdit = new TextBox
            {
                Font = DateFont,
                ForeColor = TextColor,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false,
                Height = 18,
                Margin = new Padding(0)
            };
            txtDdlEdit.KeyDown += OnDdlKeyDown;
            txtDdlEdit.LostFocus += (s, e) => CommitDdlEdit();

            Controls.Add(btnCollapse);
            Controls.Add(chkCompleted);
            Controls.Add(lblTitle);
            Controls.Add(txtTitle);
            Controls.Add(btnAddSub);
            Controls.Add(btnDelete);
            Controls.Add(lblDate);
            Controls.Add(txtDdlEdit);
        }

        public void Bind(TaskItem t)
        {
            task = t;
            showOverdue = task.IsOverdue;

            // Collapse button
            btnCollapse.Visible = task.HasChildren;
            btnCollapse.Text = task.IsExpanded ? "▾" : "▸";

            // Checkbox
            chkCompleted.Checked = task.Completed;

            // Title
            lblTitle.Text = task.Title;
            lblTitle.Font = task.Completed ? TitleFontStrike : TitleFont;
            lblTitle.ForeColor = task.Completed ? TextCompleted : TextColor;

            // Date info
            var created = task.CreatedAt.ToString("MM/dd");
            var deadline = task.Deadline.HasValue ? task.Deadline.Value.ToString("MM/dd") : "──";
            lblDate.Text = "创建: " + created + "   截止: " + deadline;
            if (showOverdue && !task.Completed)
            {
                lblDate.Text += "  [已逾期]";
                lblDate.ForeColor = OverdueColor;
                lblDate.Font = DateFontOverdue;
            }
            else if (task.Completed)
            {
                lblDate.ForeColor = TextCompleted;
                lblDate.Font = DateFont;
            }
            else
            {
                lblDate.ForeColor = TextMuted;
                lblDate.Font = DateFont;
            }

            // Edit mode — exit if was editing
            if (isEditing)
                CancelEdit();
            if (isEditingDdl)
                CancelDdlEdit();

            LayoutControls();
        }

        private void LayoutControls()
        {
            if (lblTitle == null) return; // Not initialized yet
            int indent = task.Depth * 32 + 8;
            int w = this.Width;

            // Row 1 (y=4, h=24): controls
            int y1 = 4;
            int x = indent;

            btnCollapse.Location = new Point(x, y1 + 2);
            x += btnCollapse.Visible ? 22 : 2;

            chkCompleted.Location = new Point(x, y1 + 4);
            x += 20;

            int rightBtns = 0;
            btnDelete.Location = new Point(w - 24, y1 + 2);
            rightBtns += 24;
            btnAddSub.Location = new Point(w - 24 - rightBtns, y1 + 2);
            rightBtns += 24;

            int titleWidth = w - x - rightBtns - 4;
            lblTitle.Location = new Point(x, y1 + 2);
            lblTitle.Size = new Size(titleWidth, 22);
            txtTitle.Location = new Point(x, y1 + 1);
            txtTitle.Size = new Size(titleWidth, 22);

            // Row 2 (y=28, h=18): date
            lblDate.Location = new Point(indent + 24, 28);
            lblDate.Size = new Size(w - indent - 48, 18);

            // DDL edit box: overlay on the right portion of the date row
            int ddlEditWidth = 80;
            txtDdlEdit.Location = new Point(w - ddlEditWidth - 24, 27);
            txtDdlEdit.Size = new Size(ddlEditWidth, 18);

            // Height
            this.Height = 52;
        }

        private void EnterDdlEditMode()
        {
            if (isEditingDdl || task.Completed) return;
            isEditingDdl = true;
            lblDate.Visible = false;
            txtDdlEdit.Text = task.Deadline.HasValue ? task.Deadline.Value.ToString("MM/dd") : "";
            txtDdlEdit.Visible = true;
            txtDdlEdit.Focus();
            txtDdlEdit.SelectAll();
        }

        private void CommitDdlEdit()
        {
            if (!isEditingDdl) return;
            isEditingDdl = false;
            txtDdlEdit.Visible = false;
            lblDate.Visible = true;

            var input = txtDdlEdit.Text.Trim();
            if (string.IsNullOrEmpty(input))
            {
                // Clear deadline
                if (task.Deadline.HasValue)
                {
                    task.Deadline = null;
                    Bind(task);
                    if (TaskChanged != null) TaskChanged(task);
                }
            }
            else
            {
                DateTime ddl;
                if (TaskItem.TryParseDdl(input, out ddl))
                {
                    if (!task.Deadline.HasValue || task.Deadline.Value.Date != ddl.Date)
                    {
                        task.Deadline = ddl;
                        Bind(task);
                        if (TaskChanged != null) TaskChanged(task);
                    }
                }
                else
                {
                    // Invalid format — revert
                    Bind(task);
                }
            }
        }

        private void CancelDdlEdit()
        {
            if (!isEditingDdl) return;
            isEditingDdl = false;
            txtDdlEdit.Visible = false;
            lblDate.Visible = true;
            Bind(task);
        }

        private void OnDdlKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                CommitDdlEdit();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                CancelDdlEdit();
            }
        }

        public void EnterEditMode()
        {
            if (isEditing || task.Completed) return;
            isEditing = true;
            lblTitle.Visible = false;
            txtTitle.Text = task.Title;
            txtTitle.Visible = true;
            txtTitle.Focus();
            txtTitle.SelectAll();
        }

        private void CommitEdit()
        {
            if (!isEditing) return;
            isEditing = false;
            txtTitle.Visible = false;
            lblTitle.Visible = true;

            var newTitle = txtTitle.Text.Trim();
            if (string.IsNullOrEmpty(newTitle))
                newTitle = task.Title; // revert to old if empty

            if (newTitle != task.Title)
            {
                task.Title = newTitle;
                lblTitle.Text = newTitle;
                if (TaskChanged != null) TaskChanged(task);
            }
        }

        private void CancelEdit()
        {
            if (!isEditing) return;
            isEditing = false;
            txtTitle.Visible = false;
            lblTitle.Visible = true;
            txtTitle.Text = task.Title;
        }

        private void OnTitleKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                CommitEdit();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                CancelEdit();
            }
        }

        private void OnCheckChanged(object sender, EventArgs e)
        {
            task.ToggleComplete();
            Bind(task);
            if (TaskChanged != null) TaskChanged(task);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Red left bar for overdue
            if (showOverdue && !task.Completed)
            {
                using (var brush = new SolidBrush(OverdueColor))
                    e.Graphics.FillRectangle(brush, 0, 0, 3, this.Height);
            }

            // Bottom border
            using (var pen = new Pen(BorderColor))
                e.Graphics.DrawLine(pen, 0, this.Height - 1, this.Width, this.Height - 1);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (lblTitle == null) return;
            LayoutControls();
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            if (!showOverdue || task.Completed)
                BackColor = HoverColor;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            BackColor = BgColor;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (btnCollapse != null) btnCollapse.Dispose();
                if (chkCompleted != null) chkCompleted.Dispose();
                if (lblTitle != null) lblTitle.Dispose();
                if (txtTitle != null) txtTitle.Dispose();
                if (btnAddSub != null) btnAddSub.Dispose();
                if (btnDelete != null) btnDelete.Dispose();
                if (lblDate != null) lblDate.Dispose();
                if (txtDdlEdit != null) txtDdlEdit.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
