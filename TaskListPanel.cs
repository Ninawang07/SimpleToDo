using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SimpleTodo
{
    public class TaskListPanel : Panel
    {
        private List<TaskRow> rows = new List<TaskRow>();

        public event Action<TaskItem> TaskChanged;
        public event Action<TaskItem> SubTaskAdded;
        public event Action<TaskItem> DeleteRequested;
        public event Action<TaskItem> ToggleExpand;

        public TaskListPanel()
        {
            this.AutoScroll = true;
            this.BackColor = Color.FromArgb(250, 250, 250);
            this.BorderStyle = BorderStyle.None;
            this.DoubleBuffered = true;
        }

        public void Populate(List<TaskItem> flatList)
        {
            SuspendLayout();

            // Dispose excess rows
            while (rows.Count > flatList.Count)
            {
                var last = rows[rows.Count - 1];
                UnwireRow(last);
                last.Dispose();
                rows.RemoveAt(rows.Count - 1);
            }

            // Bind or create rows
            for (int i = 0; i < flatList.Count; i++)
            {
                if (i < rows.Count)
                {
                    rows[i].Bind(flatList[i]);
                }
                else
                {
                    var row = new TaskRow(flatList[i]);
                    WireRow(row);
                    rows.Add(row);
                    Controls.Add(row);
                }
            }

            RecalculateLayout();
            ResumeLayout();
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
            Controls.Clear();
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
            // Reserve space for vertical scrollbar if needed
            if (this.VerticalScroll.Visible)
                w = this.ClientSize.Width - SystemInformation.VerticalScrollBarWidth;

            foreach (var row in rows)
            {
                row.Width = w;
                row.Location = new Point(0, y);
                y += row.Height;
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
