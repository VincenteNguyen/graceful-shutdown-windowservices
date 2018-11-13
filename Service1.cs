using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Service1
{
    public partial class Service1 : ServiceBase
    {
        private CancellationTokenSource _cancellationTokenSource;
        private ConcurrentDictionary<Task, int> _tasks;
        private Timer _timer;
        private const string Path = "E:\\Logs\\WindowsServiceLog.txt";
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            WriteToFile("Start");
            _tasks = new ConcurrentDictionary<Task, int>();
            _cancellationTokenSource = new CancellationTokenSource();

            _timer = new Timer();
            _timer.Elapsed += RunElapsed;
            _timer.Interval = 1000;
            _timer.Start();
        }

        private void RunElapsed(object sender, ElapsedEventArgs e)
        {
            var id = Guid.NewGuid();
            WriteToFile($"RunElapsed {id} start");
            _tasks.TryAdd(Task.Run(() => RunSync(_cancellationTokenSource.Token)).ContinueWith(x => _tasks.TryRemove(x, out _)), 1);
            WriteToFile($"RunElapsed {id} done");
        }

        private void RunSync(CancellationToken cancellationToken)
        {
            var id = Guid.NewGuid();
            Thread.Sleep(1000);
            if (cancellationToken.IsCancellationRequested)
            {
                WriteToFile($"{id} cancelled, return the method");
                return;
            }

            WriteToFile($"Start {id}");
            for (var i = 0; i < 4; i++) //loop processing
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    WriteToFile($"{id} cancelled, break the loop");
                    break;
                }

                //Do the work
                Thread.Sleep(5000); //example long-running 
                WriteToFile($"Wait - {id} - {i}");
            }

            WriteToFile($"Done {id}");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            var id = Guid.NewGuid();
            await Task.Delay(1000);
            if (cancellationToken.IsCancellationRequested)
            {
                WriteToFile($"{id} cancelled, return the method");
                return;
            }

            WriteToFile($"Start {id}");
            for (var i = 0; i < 4; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    WriteToFile($"{id} cancelled, break the loop");
                    break;
                }

                //Do the work
                await Task.Delay(5000); //example long-running 
                WriteToFile($"Wait - {id} - {i}");
            }

            WriteToFile($"Done {id}");
        }

        public static void WriteToFile(string text)
        {
            using (var writer = new StreamWriter(Path, true))
            {
                writer.WriteLine($"{DateTime.Now} - {text}");
                writer.Close();
            }
        }

        protected override void OnStop()
        {
            try
            {
                WriteToFile("Stop");
                _timer.Stop();
                _cancellationTokenSource.Cancel();
                WriteToFile("Graceful waiting");
                Task.WaitAll(_tasks.Keys.ToArray());
                WriteToFile("Task WaitAll done");
            }
            finally
            {
                _timer.Dispose();
                _timer = null;
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
                WriteToFile("OnStop successfully");
            }
        }
    }
}