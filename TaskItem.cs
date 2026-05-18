using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace SimpleTodo
{
    public class TaskItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public bool Completed { get; set; }
        public string ParentId { get; set; }
        public List<TaskItem> Children { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? Deadline { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int SortOrder { get; set; }

        // Computed (not serialized) — must use explicit bodies for C# 5
        [ScriptIgnore] public int Depth { get; set; }

        public bool IsExpanded { get; set; }

        [ScriptIgnore]
        public bool HasChildren
        {
            get { return Children != null && Children.Count > 0; }
        }

        [ScriptIgnore]
        public bool IsOverdue
        {
            get
            {
                return Deadline.HasValue
                    && Deadline.Value.Date < DateTime.Today
                    && !Completed;
            }
        }

        public void ToggleComplete()
        {
            Completed = !Completed;
            CompletedAt = Completed ? (DateTime?)DateTime.Now : null;
        }

        public static TaskItem CreateNew(string title, string parentId = null)
        {
            return new TaskItem
            {
                Id = Guid.NewGuid().ToString("N").Substring(0, 8),
                Title = title,
                Completed = false,
                ParentId = parentId,
                Children = new List<TaskItem>(),
                CreatedAt = DateTime.Now,
                Deadline = null,
                CompletedAt = null,
                SortOrder = 0,
                IsExpanded = true
            };
        }
    }
}
