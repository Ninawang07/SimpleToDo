const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('api', {
  getTasks: () => ipcRenderer.invoke('get-tasks'),
  createTask: (content, parentId, dueDate) => ipcRenderer.invoke('create-task', { content, parentId, dueDate }),
  updateTask: (id, content, dueDate) => ipcRenderer.invoke('update-task', { id, content, dueDate }),
  deleteTask: (id) => ipcRenderer.invoke('delete-task', id),
  toggleTask: (id, completed) => ipcRenderer.invoke('toggle-task', { id, completed }),
  reorderTasks: (ids) => ipcRenderer.invoke('reorder-tasks', { ids }),
  minimize: () => ipcRenderer.invoke('window-minimize'),
  close: () => ipcRenderer.invoke('window-close'),
});
