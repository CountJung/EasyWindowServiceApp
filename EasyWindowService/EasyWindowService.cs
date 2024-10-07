using Serilog;
using Serilog.Formatting.Compact;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace EasyWindowService
{
    public class MMFSharedData
    {
        public int statusCode;
        public int messageSize;
        public string serviceMessage;
    }
    /// <summary>
    /// 1. Need Administrator Rights
    /// 2. Set Custom Log Path for Debug, etc
    /// </summary>
    partial class EasyWindowService : ServiceBase
    {
        private Task serviceTask;
        private MemoryMappedFile mmfService;
        private MemoryMappedViewAccessor mmfAccessor;
        private readonly long shareMemSize = 1024;
        private MMFSharedData sharedMemData;
        //private readonly byte[] mmfBuffer;
        private readonly string logFile = @"D:\Log\log.txt";
        
        public bool TaskRunning { get; set; }

        public EasyWindowService()
        {
            InitializeComponent();
            //mmfBuffer = new byte[shareMemSize];
        }

        protected override void OnStart(string[] args)
        {
            // TODO: 여기에 서비스를 시작하는 코드를 추가합니다.
            sharedMemData = new MMFSharedData();
            Log.Logger = new LoggerConfiguration().MinimumLevel.Information().WriteTo.File(logFile, rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true).CreateLogger();
            try
            {
                Log.Information("Service Start");

                TaskRunning = true;
                mmfService = MemoryMappedFile.CreateOrOpen(@"Global\EasyService",shareMemSize,MemoryMappedFileAccess.ReadWrite);
                mmfAccessor = mmfService.CreateViewAccessor();
                sharedMemData.statusCode = 1;
                sharedMemData.serviceMessage = "Service Started";
                sharedMemData.messageSize = sharedMemData.serviceMessage.Length;
                mmfAccessor.Write(0, sharedMemData.statusCode);
                mmfAccessor.Write(4, sharedMemData.serviceMessage.Length);
                mmfAccessor.WriteArray(8, Encoding.UTF8.GetBytes(sharedMemData.serviceMessage), 0, sharedMemData.serviceMessage.Length);
                serviceTask = Task.Run(() => TaskWorker());
                Log.Information("Service Task Starting");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "");
            }
        }

        protected override void OnStop()
        {
            // TODO: 서비스를 중지하는 데 필요한 작업을 수행하는 코드를 여기에 추가합니다.
            try
            {
                TaskRunning = false;
                serviceTask.Wait();
                sharedMemData.statusCode = 0;
                sharedMemData.serviceMessage = "Service Stopped";
                sharedMemData.messageSize = sharedMemData.serviceMessage.Length;
                mmfAccessor.Write(0, sharedMemData.statusCode);
                mmfAccessor.Write(4, sharedMemData.serviceMessage.Length);
                mmfAccessor.WriteArray(8, Encoding.UTF8.GetBytes(sharedMemData.serviceMessage), 0, sharedMemData.serviceMessage.Length);
                Log.Information("Service Stop");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "");
            }
            Log.CloseAndFlush();
        }

        public async void TaskWorker()
        {
            while(TaskRunning)
            {
                //some awful ADS using webview
                foreach (var process in Process.GetProcesses().Where(pr => pr.ProcessName.Contains("webview2")))
                {
                    Console.WriteLine(process.MainWindowTitle + process.ProcessName);
                    process.Kill();
                }
                await Task.Delay(60000);
            }
        }
        /// <summary>
        /// Test Codes Below
        /// </summary>
        protected override void OnContinue()
        {
            base.OnContinue();
            Log.Information("Continue Service");
        }
        protected override void OnPause()
        {
            base.OnPause();
            Log.Information("Pause Service");
        }
    }
}
