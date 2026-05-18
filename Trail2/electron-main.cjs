const { app, BrowserWindow, Tray, Menu, ipcMain, globalShortcut } = require('electron');
const path = require('path');
const fs = require('fs');

const DB_PATH = path.join(app.getPath('userData'), 'tasks.db');

// SQLite via better-sqlite3 or use JSON file for simplicity
// Using a simple JSON-based store to avoid native compilation issues
const DATA_FILE = path.join(app.getPath('userData'), 'tasks.json');

function loadTasks() {
  try {
    if (fs.existsSync(DATA_FILE)) {
      return JSON.parse(fs.readFileSync(DATA_FILE, 'utf8'));
    }
  } catch (e) {}
  return [];
}

function saveTasks(tasks) {
  fs.writeFileSync(DATA_FILE, JSON.stringify(tasks, null, 2), 'utf8');
}

let mainWindow = null;
let tray = null;

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 360,
    height: 600,
    minWidth: 320,
    minHeight: 400,
    frame: false,
    transparent: false,
    resizable: true,
    show: false,
    skipTaskbar: true,
    webPreferences: {
      preload: path.join(__dirname, 'preload.cjs'),
      contextIsolation: true,
      nodeIntegration: false,
    }
  });

  mainWindow.loadFile('build/index.html');

  mainWindow.on('blur', () => {
    // Don't hide on blur - let user control
  });
}

function createTray() {
  const iconPath = path.join(__dirname, 'src-tauri', 'icons', 'icon.ico');
  tray = new Tray(iconPath);

  const contextMenu = Menu.buildFromTemplate([
    {
      label: '显示/隐藏',
      click: () => {
        if (mainWindow.isVisible()) {
          mainWindow.hide();
        } else {
          mainWindow.show();
          mainWindow.focus();
        }
      }
    },
    { type: 'separator' },
    {
      label: '退出',
      click: () => app.quit()
    }
  ]);

  tray.setContextMenu(contextMenu);
  tray.setToolTip('To-Do');

  tray.on('click', () => {
    if (mainWindow.isVisible()) {
      mainWindow.hide();
    } else {
      mainWindow.show();
      mainWindow.focus();
    }
  });
}

app.whenReady().then(() => {
  createWindow();
  createTray();

  // Global shortcut
  globalShortcut.register('CommandOrControl+Shift+T', () => {
    if (mainWindow.isVisible()) {
      mainWindow.hide();
    } else {
      mainWindow.show();
      mainWindow.focus();
    }
  });

  mainWindow.show();
});

app.on('window-all-closed', (e) => {
  e.preventDefault();
});

ipcMain.handle('get-tasks', () => {
  return loadTasks();
});

ipcMain.handle('create-task', (event, { content, parentId, dueDate }) => {
  const tasks = loadTasks();
  const task = {
    id: require('crypto').randomUUID(),
    content,
    completed: false,
    parentId: parentId || null,
    createdAt: new Date().toISOString().split('T')[0],
    dueDate: dueDate || null,
    orderIndex: tasks.filter(t => t.parentId === parentId).length + 1,
  };
  tasks.push(task);
  saveTasks(tasks);
  return task;
});

ipcMain.handle('update-task', (event, { id, content, dueDate }) => {
  const tasks = loadTasks();
  const task = tasks.find(t => t.id === id);
  if (task) {
    if (content !== undefined) task.content = content;
    if (dueDate !== undefined) task.dueDate = dueDate;
    saveTasks(tasks);
  }
});

ipcMain.handle('delete-task', (event, id) => {
  let tasks = loadTasks();
  tasks = tasks.filter(t => t.id !== id && t.parentId !== id);
  saveTasks(tasks);
});

ipcMain.handle('toggle-task', (event, { id, completed }) => {
  const tasks = loadTasks();
  const task = tasks.find(t => t.id === id);
  if (task) {
    task.completed = completed;
    saveTasks(tasks);
  }
});

ipcMain.handle('window-minimize', () => {
  mainWindow.minimize();
});

ipcMain.handle('window-close', () => {
  mainWindow.hide();
});

ipcMain.handle('reorder-tasks', (event, { ids }) => {
  const tasks = loadTasks();
  ids.forEach((id, index) => {
    const task = tasks.find(t => t.id === id);
    if (task) task.orderIndex = index + 1;
  });
  saveTasks(tasks);
});
