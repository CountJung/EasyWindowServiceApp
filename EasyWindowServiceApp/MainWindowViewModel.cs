using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
/// <summary>
/// Must Run As Administrator
/// </summary>
namespace EasyWindowServiceApp
{
    public class MMFSharedData
    {
        public int statusCode;
        public int messageSize;
        public string? serviceMessage;
    }

    public class MainWindowViewModel : ViewModelAddOn
    {
        /// <summary>
        /// Properties
        /// </summary>
        private Brush statusBackBrush;
        public Brush StatusBackBrush { get => statusBackBrush; set => Set(ref statusBackBrush, value, "StatusBackBrush"); }
        private Brush statusFrontBrush;
        public Brush StatusFrontBrush { get => statusFrontBrush; set => Set(ref statusFrontBrush, value, "StatusFrontBrush"); }
        private string statusText;
        public string StatusText { get=>statusText; set=>Set(ref statusText, value, "StatusText"); }
        public bool AppRunning { get; set; }

        /// <summary>
        /// Commands
        /// </summary>
        public ICommand CreateServiceButtonCmd { get; private set; }
        public ICommand DeleteServiceButtonCmd { get; private set; }
        public ICommand StartServiceButtonCmd { get; private set; }
        public ICommand StopServiceButtonCmd { get; private set; }
        public ICommand CloseAppCmd { get; set; }
        /// <summary>
        /// for display service status, etc
        /// </summary>
        private MemoryMappedViewAccessor? mmfAccessor;
        private MemoryMappedFile? MemoryMapFile;
        private Task? watcherTask;
        private readonly long shareMemSize = 1024;
        private readonly MMFSharedData sharedMemData;
        private readonly byte[] mmfBuffer;

        public MainWindowViewModel()
        {
            StatusBackBrush = statusBackBrush = Brushes.Chocolate;
            StatusFrontBrush = statusFrontBrush = Brushes.Bisque;
            StatusText = statusText = "Service Status";
            sharedMemData = new MMFSharedData();
            CreateServiceButtonCmd = new CommandAddOn(CreateServiceButtonAct);
            DeleteServiceButtonCmd = new CommandAddOn(DeleteServiceButtomAct);
            StartServiceButtonCmd = new CommandAddOn(StartServiceButtonAct);
            StopServiceButtonCmd = new CommandAddOn(StopServiceButtonAct);
            CloseAppCmd = new CommandAddOn(CloseAppAct);
            AppRunning = true;
            mmfBuffer = new byte[shareMemSize];
            Initialize();
        }
        private void Initialize()
        {
            MemoryMappedFileInit();
            watcherTask = Task.Run(WatcherAction);
        }
        private void MemoryMappedFileInit()
        {
            //To Use MMF in Service, set it with Global
            //Debugger Hanging? Check Administrator Rights
            //http://web.archive.org/web/20180221131343/http://blogs.networkingfutures.co.uk/post/2015/12/28/Windows-Services-Implementing-Non-Persisted-Memory-Mapped-Files-Exposing-IPC-Style-Communications.aspx
            MemoryMapFile = MemoryMappedFile.CreateOrOpen(@"Global\EasyService", shareMemSize, MemoryMappedFileAccess.ReadWrite);
            mmfAccessor = MemoryMapFile.CreateViewAccessor();
            
            sharedMemData.serviceMessage = "Service Status";
            mmfAccessor.Write(0, 0);
            mmfAccessor.Write(4, sharedMemData.serviceMessage.Length);
            mmfAccessor.WriteArray(8, Encoding.UTF8.GetBytes(sharedMemData.serviceMessage), 0, sharedMemData.serviceMessage.Length);
        }

        private void WatcherAction()
        {
            MainWindow.Instance?.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(async delegate
            {
                while (AppRunning)
                {
                    mmfAccessor?.ReadArray(0, mmfBuffer, 0, mmfBuffer.Length);
                    int statCode = BitConverter.ToInt32(mmfBuffer, 0);
                    int statSize = BitConverter.ToInt32(mmfBuffer, 4);
                    string statusString = Encoding.UTF8.GetString(mmfBuffer, 8, statSize);
                    StatusText = statusString;
                    StateCodeAct(statCode);
                    await Task.Delay(1);
                }
            }));
        }
        public void CloseAppAct(object param)
        {
            AppRunning = false;
            watcherTask?.Wait(); //IDE0052 prevent
        }
        public void CreateServiceButtonAct(object param)
        {
            // No space, english only - or wrap with ""
            string argumentString = "create EasyService binpath=" + Directory.GetCurrentDirectory() + @"\EasyWindowService.exe start=demand";
            CommandProcess(argumentString);
            sharedMemData.serviceMessage = "Service Created";
            mmfAccessor?.Write(0, 0);
            mmfAccessor?.Write(4, sharedMemData.serviceMessage.Length);
            mmfAccessor?.WriteArray(8, Encoding.UTF8.GetBytes(sharedMemData.serviceMessage), 0, sharedMemData.serviceMessage.Length);
        }
        public void DeleteServiceButtomAct(object param)
        {
            string argumentString = "delete EasyService";
            CommandProcess(argumentString);
            sharedMemData.serviceMessage = "Service Deleted";
            mmfAccessor?.Write(0, 0);
            mmfAccessor?.Write(4, sharedMemData.serviceMessage.Length);
            mmfAccessor?.WriteArray(8, Encoding.UTF8.GetBytes(sharedMemData.serviceMessage), 0, sharedMemData.serviceMessage.Length);
        }
        public void StartServiceButtonAct(object param)
        {
            string argumentString = "start EasyService";
            CommandProcess(argumentString);
            MemoryMapFile?.Dispose();
            mmfAccessor?.Dispose();
            MemoryMapFile = MemoryMappedFile.CreateOrOpen(@"Global\EasyService", shareMemSize, MemoryMappedFileAccess.ReadWrite);
            mmfAccessor = MemoryMapFile.CreateViewAccessor();
        }
        public void StopServiceButtonAct(object param)
        {
            string argumentString = $"stop EasyService";
            CommandProcess(argumentString);
        }
        private void CommandProcess(string argument)
        {
            Process procCmd = new()
            {
                StartInfo = { FileName = "sc", Arguments = argument, WindowStyle = ProcessWindowStyle.Hidden, CreateNoWindow = true }
            };
            procCmd.Start();
        }
        private void StateCodeAct(int statCode)
        {
            switch (statCode)
            {
                case 0:
                    StatusBackBrush = Brushes.Chocolate;
                    StatusFrontBrush = Brushes.Bisque;
                    break;
                case 1:
                    StatusBackBrush = Brushes.Tan;
                    StatusFrontBrush = Brushes.Olive;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(statCode));
            }
        }
    }
}
