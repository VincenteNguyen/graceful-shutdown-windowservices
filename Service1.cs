using System.Collections.Concurrent;
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

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _tasks = new ConcurrentDictionary<Task, int>();
            _cancellationTokenSource = new CancellationTokenSource();

            _timer = new Timer();
            _timer.Elapsed += RunElapsed;
            _timer.Interval = 1000;
            _timer.Start();
        }

        private void RunElapsed(object sender, ElapsedEventArgs e) => _tasks.TryAdd(Task.Run(() => RunAsync(_cancellationTokenSource.Token)).ContinueWith(x => _tasks.TryRemove(x, out _)), 1);

        private static void RunSync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            for (var i = 0; i < 4; i++) //loop processing
            {
                if (cancellationToken.IsCancellationRequested) break;

                //Do the work
                Thread.Sleep(5000); //example long-running
            }
        }

        private static async Task RunAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            for (var i = 0; i < 4; i++) //loop processing
            {
                if (cancellationToken.IsCancellationRequested) break;

                //Do the work
                await Task.Delay(5000); //example long-running
            }
        }

        protected override void OnStop()
        {
            try
            {
                _timer.Stop();
                _cancellationTokenSource.Cancel();
                Task.WaitAll(_tasks.Keys.ToArray());
            }
            finally
            {
                _timer.Dispose();
                _timer = null;
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }
    }
}