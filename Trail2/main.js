// main.js - Electron version
let tasks = [];
let expandedTasks = new Set();

const taskList = document.getElementById('task-list');
const taskInput = document.getElementById('task-input');
const stats = document.getElementById('stats');

const api = window.api;

async function init() {
  tasks = await api.getTasks();
  render();
  updateStats();
}

async function addTask() {
  const content = taskInput.value.trim();
  if (!content) return;
  await api.createTask(content, null, null);
  taskInput.value = '';
  tasks = await api.getTasks();
  render();
  updateStats();
}

async function toggleTask(id) {
  const task = tasks.find(t => t.id === id);
  if (!task) return;
  const newCompleted = !task.completed;
  await api.toggleTask(id, newCompleted);
  task.completed = newCompleted;
  render();
  updateStats();
}

async function deleteTask(id) {
  await api.deleteTask(id);
  tasks = await api.getTasks();
  render();
  updateStats();
}

function render() {
  taskList.innerHTML = '';
  const rootTasks = tasks.filter(t => !t.parentId).sort((a, b) => a.orderIndex - b.orderIndex);
  rootTasks.forEach(task => {
    const el = createTaskElement(task, false);
    taskList.appendChild(el);
  });
}

function createTaskElement(task, isSubtask) {
  const item = document.createElement('div');
  item.className = 'task-item' + (isSubtask ? ' subtask' : '') + (task.completed ? ' completed' : '');
  item.dataset.id = task.id;

  const row = document.createElement('div');
  row.className = 'task-row';

  // Expand button
  const children = tasks.filter(t => t.parentId === task.id);
  if (children.length > 0 && !isSubtask) {
    const expandBtn = document.createElement('div');
    expandBtn.className = 'expand-btn' + (expandedTasks.has(task.id) ? '' : ' collapsed');
    expandBtn.innerHTML = '<div class="arrow"></div>';
    expandBtn.addEventListener('click', (e) => {
      e.stopPropagation();
      if (expandedTasks.has(task.id)) {
        expandedTasks.delete(task.id);
      } else {
        expandedTasks.add(task.id);
      }
      render();
    });
    row.appendChild(expandBtn);
  } else if (!isSubtask) {
    const spacer = document.createElement('div');
    spacer.style.width = '10px';
    spacer.style.flexShrink = '0';
    row.appendChild(spacer);
  }

  // Custom checkbox
  const checkbox = document.createElement('div');
  checkbox.className = 'checkbox' + (task.completed ? ' checked' : '');
  checkbox.addEventListener('click', () => toggleTask(task.id));
  row.appendChild(checkbox);

  // Content
  const content = document.createElement('span');
  content.className = 'task-content' + (task.completed ? ' completed' : '');
  content.textContent = task.content;
  content.contentEditable = true;
  content.addEventListener('blur', async () => {
    const newText = content.textContent.trim();
    if (newText && newText !== task.content) {
      await api.updateTask(task.id, newText, task.dueDate);
      task.content = newText;
    }
    render();
  });
  content.addEventListener('keydown', (e) => {
    if (e.key === 'Enter') {
      e.preventDefault();
      content.blur();
    }
  });
  row.appendChild(content);

  // Date tags
  const tags = document.createElement('div');
  tags.className = 'task-tags';

  const createdTag = document.createElement('span');
  createdTag.className = 'tag';
  createdTag.textContent = task.createdAt ? task.createdAt.slice(5) : '';
  tags.appendChild(createdTag);

  if (task.dueDate) {
    const dueTag = document.createElement('span');
    const today = new Date().toISOString().split('T')[0];
    const isOverdue = task.dueDate < today && !task.completed;
    dueTag.className = 'tag' + (isOverdue ? ' overdue' : '');
    dueTag.textContent = task.dueDate.slice(5);
    tags.appendChild(dueTag);
  } else {
    const emptyTag = document.createElement('span');
    emptyTag.className = 'tag';
    emptyTag.style.visibility = 'hidden';
    tags.appendChild(emptyTag);
  }

  row.appendChild(tags);
  item.appendChild(row);

  // Subtasks
  if (children.length > 0 && expandedTasks.has(task.id) && !isSubtask) {
    const subtasksContainer = document.createElement('div');
    subtasksContainer.className = 'subtasks';
    children.sort((a, b) => a.orderIndex - b.orderIndex).forEach(child => {
      subtasksContainer.appendChild(createTaskElement(child, true));
    });
    item.appendChild(subtasksContainer);
  }

  // Context menu
  item.addEventListener('contextmenu', (e) => {
    e.preventDefault();
    showContextMenu(e, task);
  });

  return item;
}

function showContextMenu(e, task) {
  removeContextMenu();
  const menu = document.createElement('div');
  menu.className = 'context-menu';
  menu.innerHTML = `
    <div class="context-menu-item" data-action="add-sub">添加子任务</div>
    <div class="context-menu-item" data-action="set-due">设置 DDL</div>
    <div class="context-menu-item delete" data-action="delete">删除</div>
  `;
  menu.style.left = e.pageX + 'px';
  menu.style.top = e.pageY + 'px';
  document.body.appendChild(menu);

  menu.querySelectorAll('.context-menu-item').forEach(item => {
    item.addEventListener('click', async () => {
      const action = item.dataset.action;
      if (action === 'add-sub') {
        const content = prompt('子任务内容：');
        if (content) {
          await api.createTask(content, task.id, null);
          tasks = await api.getTasks();
          expandedTasks.add(task.id);
          render();
          updateStats();
        }
      } else if (action === 'set-due') {
        const due = prompt('DDL 日期（YYYY-MM-DD，留空清除）：', task.dueDate || '');
        if (due !== null) {
          await api.updateTask(task.id, task.content, due || null);
          tasks = await api.getTasks();
          render();
        }
      } else if (action === 'delete') {
        await deleteTask(task.id);
      }
      removeContextMenu();
    });
  });

  setTimeout(() => {
    document.addEventListener('click', removeContextMenu, { once: true });
  }, 10);
}

function removeContextMenu() {
  const menu = document.querySelector('.context-menu');
  if (menu) menu.remove();
}

function updateStats() {
  const active = tasks.filter(t => !t.completed && !t.parentId).length;
  stats.textContent = active + ' 待办';
}

// Window controls
document.getElementById('minimize-btn').addEventListener('click', () => {
  api.minimize();
});
document.getElementById('close-btn').addEventListener('click', () => {
  api.close();
});

// Enter key to add task
taskInput.addEventListener('keydown', (e) => {
  if (e.key === 'Enter') addTask();
});

init();
