// Hi there. Thank you for checking out our DLL!
// ©? 2024 Project Chapter OG. All rights reserved.

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

class Program
{
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
    public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

    [DllImport("kernel32.dll")]
    public static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenThread(int dwDesiredAccess, bool bInheritHandle, int dwThreadId);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint SuspendThread(IntPtr hThread);

    const int PROCESS_CREATE_THREAD = 0x0002;
    const int PROCESS_QUERY_INFORMATION = 0x0400;
    const int PROCESS_VM_OPERATION = 0x0008;
    const int PROCESS_VM_WRITE = 0x0020;
    const int PROCESS_VM_READ = 0x0010;

    const uint MEM_COMMIT = 0x00001000;
    const uint MEM_RESERVE = 0x00002000;
    const uint PAGE_READWRITE = 4;

    static void Main(string[] args)
    {
        //DisplayWatermark();

        string gamePath = LoadGamePath();
        if (string.IsNullOrEmpty(gamePath))
        {
            Console.Write("Please enter the path to your game directory. Please ensure that 'FortniteGame' and 'Engine' are in the folder you select. This will be saved: ");
            gamePath = Console.ReadLine();
            SaveGamePath(gamePath);
        }

        string authFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "projectchapterogauth.txt");

        if (!File.Exists(authFilePath))
        {
            Console.WriteLine("Authentication file not found. Ensure you have started the Project Chapter OG Launcher. Please ensure the file exists at: " + authFilePath);
            Environment.Exit(1);
        }

        string[] authData = File.ReadAllText(authFilePath).Split('/');
        if (authData.Length != 2)
        {
            Console.WriteLine("Authentication file format is invalid. Expected format: email/password");
            Environment.Exit(1);
        }

        string authEmail = authData[0];
        string authPassword = authData[1];

        string dllPath = Path.Combine(gamePath, "FortniteGame\\Binaries\\Win64\\Cobalt.dll");
        string launcherPath = Path.Combine(gamePath, "FortniteGame\\Binaries\\Win64\\FortniteLauncher.exe");

        string FolderPath = Path.Combine(gamePath, "FortniteGame\\Binaries\\Win64\\EasyAntiCheat");
        string startgamepath = Path.Combine(gamePath, "FortniteGame\\Binaries\\Win64\\startgame.exe");

        string fortniteExe = Path.Combine(gamePath, "FortniteGame\\Binaries\\Win64\\FortniteClient-Win64-Shipping.exe");

        if (!File.Exists(fortniteExe))
        {
            Console.WriteLine("Your game path that you entered is invalid! Please change it with clicking on the folder icon! Press any key to exit.");
            Console.ReadKey();
            Environment.Exit(0);
        }

        if (Directory.Exists(FolderPath))
        {
            Directory.Delete(FolderPath, true);
        }

        using (var client = new WebClient())
        {
            string downloadUrl = "https://github.com/iluvjoshallen/ProjectOG/raw/main/EasyAntiCheat.zip";
            string zipFilePath = Path.Combine(gamePath, "FortniteGame\\Binaries\\Win64\\EasyAntiCheat.zip");

            client.DownloadFile(downloadUrl, zipFilePath);

            string extractPath = Path.Combine(gamePath, "FortniteGame\\Binaries\\Win64\\EasyAntiCheat");
            ZipFile.ExtractToDirectory(zipFilePath, extractPath);

            File.Delete(zipFilePath);
        }

        if (!File.Exists(startgamepath))
        {
            using (var client = new WebClient())
            {
                string downloadUrl = "https://github.com/iluvjoshallen/ProjectOG/raw/main/startgame.exe";
                string destinationPath = Path.Combine(gamePath, "FortniteGame\\Binaries\\Win64\\startgame.exe");
                client.DownloadFile(downloadUrl, destinationPath);
            }
        }

        if (File.Exists(launcherPath))
        {
            File.Delete(launcherPath);
        }

        using (var client = new WebClient())
        {
            string downloadUrl = "https://github.com/iluvjoshallen/ProjectOG/raw/main/FortniteLauncher.exe";
            client.DownloadFile(downloadUrl, launcherPath);
        }

        if (File.Exists(dllPath))
        {
            using (var client = new WebClient())
            {
                Process process = StartProcess(Path.Combine(gamePath, "FortniteGame\\Binaries\\Win64\\FortniteLauncher.exe"), true, "-NOSSLPINNING");
                Process process2 = StartProcess(Path.Combine(gamePath, "FortniteGame\\Binaries\\Win64\\FortniteClient-Win64-Shipping_BE.exe"), true, "");
                Process process3 = StartProcess(Path.Combine(gamePath, "FortniteGame\\Binaries\\Win64\\startgame.exe"), false, $"-AUTH_LOGIN={authEmail} -AUTH_PASSWORD={authPassword}");
                process3.WaitForInputIdle();

                string targetProcessName = "FortniteClient-Win64-Shipping";

                InjectDllWhenProcessStarts(targetProcessName, gamePath, "Cobalt.dll");

                process3.WaitForExit();
                process.Kill();
                process2.Kill();
            }
        }

        if (!File.Exists(dllPath))
        {
            using (var client = new WebClient())
            {
                File.Delete(dllPath);
                string downloadUrl = "https://github.com/iluvjoshallen/ProjectOG/raw/main/Cobalt.dll";
                string destinationPath = Path.Combine(gamePath, "FortniteGame\\Binaries\\Win64\\Cobalt.dll");
                client.DownloadFile(downloadUrl, destinationPath);
            }
        }
    }

    public static Process StartProcess(string path, bool shouldFreeze, string extraArgs = "")
    {
        Process process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = path,
                Arguments = $"-epicapp=Fortnite -epicenv=Prod -epiclocale=en-us -epicportal -skippatchcheck -nobe -fromfl=eac -fltoken=3db3ba5dcbd2e16703f3978d -caldera=eyJhbGciOiJFUzI1NiIsInR5cCI6IkpXVCJ9.eyJhY2NvdW50X2lkIjoiYmU5ZGE1YzJmYmVhNDQwN2IyZjQwZWJhYWQ4NTlhZDQiLCJnZW5lcmF0ZWQiOjE2Mzg3MTcyNzgsImNhbGRlcmFHdWlkIjoiMzgxMGI4NjMtMmE2NS00NDU3LTliNTgtNGRhYjNiNDgyYTg2IiwiYWNQcm92aWRlciI6IkVhc3lBbnRpQ2hlYXQiLCJub3RlcyI6IiIsImZhbGxiYWNrIjpmYWxzZX0.VAWQB67RTxhiWOxx7DBjnzDnXyyEnX7OljJm-j2d88G_WgwQ9wrE6lwMEHZHjBd1ISJdUO1UVUqkfLdU5nofBQ -AUTH_TYPE=epic {extraArgs}"
            }
        };
        process.Start();
        if (shouldFreeze)
        {
            foreach (ProcessThread thread in process.Threads)
            {
                IntPtr threadHandle = OpenThread(0x0002, false, thread.Id);
                if (threadHandle != IntPtr.Zero)
                {
                    SuspendThread(threadHandle);
                }
            }
        }
        return process;
    }

    static void InjectDllWhenProcessStarts(string processName, string currentDirectory, string dllName)
    {
        int initialSleep = 100;
        int maxSleep = 5000;
        int totalWaitTime = 0;
        int timeout = 300000;

        while (totalWaitTime < timeout)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            if (processes.Length > 0)
            {
                Process targetProcess = processes[0];
                Console.WriteLine($"Attempting to open process: {targetProcess.ProcessName} (ID: {targetProcess.Id})");

                IntPtr procHandle = OpenProcess(PROCESS_CREATE_THREAD | PROCESS_QUERY_INFORMATION | PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ, false, targetProcess.Id);

                if (procHandle == IntPtr.Zero)
                {
                    int error = Marshal.GetLastWin32Error();
                    Console.WriteLine($"OpenProcess failed for process: {targetProcess.ProcessName} (ID: {targetProcess.Id}). Error: {error}");

                    if (error == 5)
                    {
                        Console.WriteLine("Access denied. Ensure the application is running with administrator privileges.");
                    }
                    return;
                }

                IntPtr loadLibraryAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
                if (loadLibraryAddr == IntPtr.Zero)
                {
                    Console.WriteLine("GetProcAddress failed. Error: " + Marshal.GetLastWin32Error());
                    return;
                }

                IntPtr allocMemAddress = VirtualAllocEx(procHandle, IntPtr.Zero, (uint)((dllName.Length + 1) * Marshal.SizeOf(typeof(char))), MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
                if (allocMemAddress == IntPtr.Zero)
                {
                    Console.WriteLine("VirtualAllocEx failed. Error: " + Marshal.GetLastWin32Error());
                    return;
                }

                UIntPtr bytesWritten;
                bool result = WriteProcessMemory(procHandle, allocMemAddress, Encoding.Default.GetBytes(dllName), (uint)((dllName.Length + 1) * Marshal.SizeOf(typeof(char))), out bytesWritten);
                if (!result)
                {
                    Console.WriteLine("WriteProcessMemory failed. Error: " + Marshal.GetLastWin32Error());
                    return;
                }

                IntPtr remoteThread = CreateRemoteThread(procHandle, IntPtr.Zero, 0, loadLibraryAddr, allocMemAddress, 0, IntPtr.Zero);
                if (remoteThread == IntPtr.Zero)
                {
                    Console.WriteLine("CreateRemoteThread failed. Error: " + Marshal.GetLastWin32Error());
                    return;
                }

                Console.WriteLine("DLL successfully injected!");
                return;
            }

            Console.WriteLine($"Waiting for process '{processName}' to start...");
            Thread.Sleep(initialSleep);
            totalWaitTime += initialSleep;
            initialSleep = Math.Min(initialSleep * 2, maxSleep);
        }

        Console.WriteLine("DLL injection timed out after waiting for the process for " + timeout / 1000 + " seconds.");
    }

    static void SaveGamePath(string gamePath)
    {
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "gamepath.txt");
        File.WriteAllText(path, gamePath);
    }

    static string LoadGamePath()
    {
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "gamepath.txt");
        if (File.Exists(path))
        {
            return File.ReadAllText(path);
        }
        return null;
    }
}