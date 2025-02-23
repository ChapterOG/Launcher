const { contextBridge, ipcRenderer, shell } = require('electron');

contextBridge.exposeInMainWorld('electron', {
  openshell: () => ipcRenderer.send('openshell'),

  selectfolder: () => ipcRenderer.send('selectfolder'),

  downloadFile: (url, filename) => ipcRenderer.send('download-file', { url, filename }),

  openshellresponse: (callback) => ipcRenderer.on('openshell-response', callback),

  onAuthId: (callback) => ipcRenderer.on('auth-id', (event, authId) => callback(authId)),

  logoutUser: () => ipcRenderer.send('logout-user'),
  openDiscordExternal: () => ipcRenderer.send('openDiscordExternal'),

  openVbucksExternal: () => ipcRenderer.send('openVbucksExternal')
});