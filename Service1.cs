using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using NLog;
using Timer = System.Timers.Timer;

namespace Service1
{
    public partial class Service1 : ServiceBase
    {
        private CancellationTokenSource _cancellationTokenSource;
        private ConcurrentDictionary<Task, int> _tasks;
        private Timer _timer;
        public static Logger Log = LogManager.GetCurrentClassLogger();
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
            _timer.Interval = 10000;
            _timer.Start();
        }

        private void RunElapsed(object sender, ElapsedEventArgs e)
        {
            var childCts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token);
            childCts.CancelAfter(TimeSpan.FromSeconds(2));
            var task = Task.Run(() =>
                {
                    return RunSync(childCts.Token);
                })
                .ContinueWith(async x =>
                {
                    var result = await x;
                    foreach (var entry in _tasks.Where(y => y.Key.IsCompleted))
                        _tasks.TryRemove(entry.Key, out _);
                });
            _tasks.TryAdd(task, 1);
        }

        private int RunSync(CancellationToken cancellationToken)
        {
            var id = Guid.NewGuid();
            Thread.Sleep(10000);
            if (cancellationToken.IsCancellationRequested)
            {
                return 0;
            }

            int i;
            for (i = 0; i < 4; i++) //loop processing
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                //Do the work
                Thread.Sleep(2000); //example long-running 
            }

            return i;
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            var id = Guid.NewGuid();
            await Task.Delay(1000);
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            for (var i = 0; i < 4; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                //Do the work
                await Task.Delay(5000); //example long-running 
            }

        }

        public static void WriteToFile(string text)
        {
            Log.Info(text);
        }

        protected override void OnStop()
        {
            try
            {
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