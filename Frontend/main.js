const { app, BrowserWindow, ipcMain, shell, dialog } = require('electron');
const path = require('path');
const fs = require('fs');
const RPC = require('discord-rpc');
const https = require('https');
const os = require('os');

let mainWindow;

if (process.defaultApp) {
  if (process.argv.length >= 2) {
    app.setAsDefaultProtocolClient('chapterog', process.execPath, [path.resolve(process.argv[1])])
  }
} else {
  app.setAsDefaultProtocolClient('chapterog')
}

const logFilePath = path.join(app.getPath('userData'), 'projectchapterog.log');

const logToFile = (message) => {
  fs.appendFileSync(logFilePath, message + '\n');
};


const clientId = '1236746688060854293';
RPC.register(clientId);

const rpc = new RPC.Client({ transport: 'ipc' });

async function setActivity() {
  if (!rpc ) {
    return;
  }

  rpc.setActivity({
    details: 'Project Chapter OG Launcher',
    state: 'https://chapterog.com',
    startTimestamp: Date.now(),
    largeImageKey: 'projectchapterog',
    largeImageText: 'Large Image Text',
    smallImageKey: 'small-image-key',
    smallImageText: 'Small Image Text',
    instance: false,
  });
}

rpc.on('ready', () => {
  setActivity();
});

rpc.login({ clientId }).catch(console.error);

const createWindow = () => {
  mainWindow = new BrowserWindow({
    width: 1050,
    height: 600,
    icon: path.join(__dirname, 'images', 'PROJECTOG.png'),
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      contextIsolation: true,
      enableRemoteModule: false,
      nodeIntegration: true,
      devTools: false
    }
  });

  mainWindow.loadFile('index.html');

  const authFilePath = path.join(app.getPath('appData'), 'projectchapterogauth.txt');

  if (fs.existsSync(authFilePath)) {
    setTimeout(() => {
      mainWindow.webContents.send('auth-id');
    }, 3000);
    console.log(app.getPath('appData'));
    console.log("found it");
  } else {
    console.log("not found!");
  }  

  mainWindow.setMenuBarVisibility(false);
  setActivity();
};

app.whenReady().then(() => {
  createWindow();

  app.on('open-url', (event, url) => {
    event.preventDefault();
    dialog.showErrorBox('Thank You', `Thanks for authenticating! ${url}`);
    console.log(url);
  });

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow();
  });
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') app.quit();
});

const gotTheLock = app.requestSingleInstanceLock();

if (gotTheLock) {
  app.on('second-instance', (event, commandLine, workingDirectory) => {
      if (mainWindow.isMinimized()) mainWindow.restore();
      mainWindow.focus();

      const authUrl = commandLine.find(arg => arg.startsWith('chapterog://auth'));

      if (authUrl) {
          const authCode = authUrl.split('chapterog://auth/')[1];
          const savePath = path.join(app.getPath('appData'), 'projectchapterogauth.txt');
          fs.writeFileSync(savePath, authCode, 'utf-8');
          logToFile(`[ ${Date()} ] ` + authCode);
          mainWindow.webContents.send('auth-id', authCode);
      }
  });
} else {
  app.quit();
}

ipcMain.on('openshell', (event) => {
  const filesFolderPath = path.join(process.resourcesPath, 'files', 'ProjectChapterOG.exe');

  logToFile(`[ ${Date()} ] Files Folder Path: ${filesFolderPath}`);

  shell.openExternal(filesFolderPath).catch(err => {
    logToFile(`Failed to open file: ${err.message}`);
  });
});

ipcMain.on('logout-user', async (event) => {
  const savePath = path.join(app.getPath('appData'), 'projectchapterogauth.txt');
  try {
    await fs.promises.unlink(savePath);
    logToFile('Auth file deleted successfully.');
  } catch (err) {
    logToFile(`Failed to delete auth file: ${err.message}`);
  }
});

ipcMain.on('openDiscordExternal', (event) => {
  shell.openExternal('https://backend.chapterog.com/api/v1/discord/auth')
});

ipcMain.on('openVbucksExternal', (event) => {
  shell.openExternal('https://backend.chapterog.com/v1/launcher/claimvbucks')
});

ipcMain.on('download-file', async (event, args) => {
  const { url, filename } = args;
  const downloadsPath = path.join(os.homedir(), 'Downloads', filename);

  const file = fs.createWriteStream(downloadsPath);
  https.get(url, (response) => {
      response.pipe(file);
      file.on('finish', () => {
          file.close();
          event.reply('download-complete', 'Download Complete');
      });
  }).on('error', (err) => {
      fs.unlink(downloadsPath);
      console.error('Download failed:', err.message);
      event.reply('download-failed', err.message);
  });
});

ipcMain.on('selectfolder', async (event) => {
  const result = await dialog.showOpenDialog({
      properties: ['openDirectory']
  });

  if (!result.canceled && result.filePaths.length > 0) {
      const selectedPath = result.filePaths[0];
      const savePath = path.join(app.getPath('appData'), 'gamepath.txt');

      fs.writeFileSync(savePath, selectedPath, 'utf-8');
      logToFile(selectedPath);

      return { canceled: false, filePaths: result.filePaths };
  }

  return { canceled: true };
});