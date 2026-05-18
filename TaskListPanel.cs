using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SimpleTodo
{
    public class TaskListPanel : Panel
    {
        private List<TaskRow> rows = new List<TaskRow>();
        private Label separator;
        private bool completedExpanded = true;
        private int activeRowCount;

        public event Action<TaskItem> TaskChanged;
        public event Action<TaskItem> SubTaskAdded;
        public event Action<TaskItem> DeleteRequested;
        public event Action<TaskItem> ToggleExpand;
        public event Action CompletedToggled;

        private static readonly Color BgColor = Color.FromArgb(250, 250, 250);
        private static readonly Color SeparatorBg = Color.FromArgb(245, 245, 245);
        private static readonly Color TextMuted = Color.FromArgb(160, 160, 160);
        private static readonly Color BorderColor = Color.FromArgb(220, 220, 220);

        public TaskListPanel()
        {
            this.AutoScroll = true;
            this.BackColor = BgColor;
            this.BorderStyle = BorderStyle.None;
            this.DoubleBuffered = true;
        }

        public void Populate(List<TaskItem> flatList, bool showCompleted)
        {
            SuspendLayout();

            // Split flat list into active and completed
            var activeList = flatList.Where(t => !t.Completed).ToList();
            var completedList = flatList.Where(t => t.Completed).ToList();

            // Clear all existing task rows
            ClearAll();

            // Build active task rows
            foreach (var task in activeList)
            {
                var row = new TaskRow(task);
                WireRow(row);
                rows.Add(row);
                Controls.Add(row);
            }
            activeRowCount = activeList.Count;

            // Completed section
            if (showCompleted && completedList.Count > 0)
            {
                EnsureSeparator();
                UpdateSeparatorState(completedList.Count);
                separator.Visible = true;

                if (completedExpanded)
                {
                    foreach (var task in completedList)
                    {
                        var row = new TaskRow(task);
                        WireRow(row);
                        rows.Add(row);
                        Controls.Add(row);
                    }
                }
            }
            else
            {
                if (separator != null)
                    separator.Visible = false;
            }

            RecalculateLayout();
            ResumeLayout();
        }

        private void EnsureSeparator()
        {
            if (separator == null)
            {
                separator = new Label
                {
                    Height = 24,
                    Font = new Font("Microsoft YaHei UI", 7.5f),
                    ForeColor = TextMuted,
                    BackColor = SeparatorBg,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Cursor = Cursors.Hand,
                    Padding = new Padding(12, 0, 0, 0)
                };
                separator.Click += (s, e) =>
                {
                    completedExpanded = !completedExpanded;
                    if (CompletedToggled != null) CompletedToggled();
                };
                Controls.Add(separator);
            }
        }

        private void UpdateSeparatorState(int completedCount)
        {
            if (separator != null)
            {
                separator.Visible = completedCount > 0;
                if (completedCount > 0)
                {
                    var arrow = completedExpanded ? "▾" : "▸";
                    separator.Text = arrow + " 已完成 (" + completedCount + ")";
                }
                separator.Tag = completedExpanded; // store state for caller
            }
        }

        public bool ToggleCompleted()
        {
            completedExpanded = !completedExpanded;
            return completedExpanded;
        }

        public void ClearAll()
        {
            SuspendLayout();
            foreach (var row in rows)
            {
                UnwireRow(row);
                row.Dispose();
            }
            rows.Clear();
            if (separator != null)
            {
                Controls.Remove(separator);
                separator.Dispose();
                separator = null;
            }
            ResumeLayout();
        }

        public void ScrollToBottom()
        {
            if (rows.Count > 0)
            {
                var last = rows[rows.Count - 1];
                this.ScrollControlIntoView(last);
            }
        }

        private void RecalculateLayout()
        {
            int y = 0;
            int w = this.ClientSize.Width;
            if (this.VerticalScroll.Visible)
                w = this.ClientSize.Width - SystemInformation.VerticalScrollBarWidth;

            // Position active rows (indices 0 to activeRowCount-1)
            for (int i = 0; i < activeRowCount && i < rows.Count; i++)
            {
                if (rows[i].Visible)
                {
                    rows[i].Width = w;
                    rows[i].Location = new Point(0, y);
                    y += rows[i].Height;
                }
            }

            // Separator between active and completed
            if (separator != null && separator.Visible)
            {
                separator.Width = w;
                separator.Location = new Point(0, y);
                y += separator.Height;
            }

            // Position completed rows (indices activeRowCount to end)
            for (int i = activeRowCount; i < rows.Count; i++)
            {
                if (rows[i].Visible)
                {
                    rows[i].Width = w;
                    rows[i].Location = new Point(0, y);
                    y += rows[i].Height;
                }
            }
        }

        private void WireRow(TaskRow row)
        {
            row.TaskChanged += OnTaskChanged;
            row.SubTaskAdded += OnSubTaskAdded;
            row.DeleteRequested += OnDeleteRequested;
            row.ToggleExpand += OnToggleExpand;
        }

        private void UnwireRow(TaskRow row)
        {
            row.TaskChanged -= OnTaskChanged;
            row.SubTaskAdded -= OnSubTaskAdded;
            row.DeleteRequested -= OnDeleteRequested;
            row.ToggleExpand -= OnToggleExpand;
        }

        private void OnTaskChanged(TaskItem task) { if (TaskChanged != null) TaskChanged(task); }
        private void OnSubTaskAdded(TaskItem task) { if (SubTaskAdded != null) SubTaskAdded(task); }
        private void OnDeleteRequested(TaskItem task) { if (DeleteRequested != null) DeleteRequested(task); }
        private void OnToggleExpand(TaskItem task) { if (ToggleExpand != null) ToggleExpand(task); }

        public void CommitSeparatorToggle()
        {
            // Called by MainForm after checking if separator was clicked
            if (separator != null && separator.Tag is bool)
            {
                bool current = (bool)separator.Tag;
                if (current != completedExpanded)
                {
                    completedExpanded = current;
                }
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            RecalculateLayout();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                ClearAll();
            base.Dispose(disposing);
        }
    }
}
