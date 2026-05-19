using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace SimpleTodo
{
    public class TaskStore
    {
        private readonly string filePath;
        private readonly JavaScriptSerializer serializer;

        public TaskStore()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SimpleTodo");
            Directory.CreateDirectory(dir);
            filePath = Path.Combine(dir, "tasks.json");
            serializer = new JavaScriptSerializer();
        }

        public List<TaskItem> LoadAll()
        {
            if (!File.Exists(filePath))
                return new List<TaskItem>();

            var json = File.ReadAllText(filePath);
            var tree = serializer.Deserialize<List<TaskItem>>(json) ?? new List<TaskItem>();
            return BuildFlatList(tree, false);
        }

        public void SaveAll(List<TaskItem> flatList)
        {
            var tree = BuildTree(flatList);
            var json = serializer.Serialize(tree);
            var tmpPath = filePath + ".tmp";

            File.WriteAllText(tmpPath, json);
            if (File.Exists(filePath))
                File.Replace(tmpPath, filePath, filePath + ".bak");
            else
                File.Move(tmpPath, filePath);
        }

        /// <summary>
        /// Depth-first flatten: tree with nested Children -> flat list with Depth values.
        /// When respectExpand is true, collapsed children are excluded (UI display).
        /// When false, all items are included regardless of expand state (data loading).
        /// </summary>
        public static List<TaskItem> BuildFlatList(List<TaskItem> tree, bool respectExpand)
        {
            var result = new List<TaskItem>();
            Flatten(tree, result, 0, respectExpand);
            return result;
        }

        private static void Flatten(List<TaskItem> items, List<TaskItem> result, int depth, bool respectExpand)
        {
            if (items == null) return;
            foreach (var item in items.OrderBy(i => i.SortOrder).ThenBy(i => i.CreatedAt))
            {
                item.Depth = depth;
                result.Add(item);
                bool descend = !respectExpand || item.IsExpanded;
                if (descend && item.Children != null && item.Children.Count > 0)
                    Flatten(item.Children, result, depth + 1, respectExpand);
            }
        }

        /// <summary>
        /// Flat list -> tree with nested Children.
        /// Uses Depth to determine parent-child relationships.
        /// </summary>
        public static List<TaskItem> BuildTree(List<TaskItem> flatList)
        {
            var roots = new List<TaskItem>();
            var stack = new Stack<TaskItem>();
            var depth = -1;

            foreach (var item in flatList)
            {
                item.Children = new List<TaskItem>();

                while (depth >= item.Depth)
                {
                    if (stack.Count == 0) break;
                    stack.Pop();
                    depth--;
                }

                if (stack.Count == 0)
                    roots.Add(item);
                else
                    stack.Peek().Children.Add(item);

                stack.Push(item);
                depth = item.Depth;
            }

            return roots;
        }
    }
}
