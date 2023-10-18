using System;
using System.Timers;

namespace DriverScanner
{
    public class ButtonChecker
    {
        public event EventHandler<bool> Click;

        private Timer _timer;
        private readonly KernelConnector _kernel;

        public ButtonChecker(KernelConnector kernelConnector) 
        {
            _kernel = kernelConnector;
            _kernel.ButtonIni();
            _timer = new Timer(200)
            {
                AutoReset = false
            };
            _timer.Elapsed += Timer_Elapsed;
            _timer.Start();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var status = _kernel.ButtonCheck();
            Click(this, status);
            _timer.Start();
        }
    }
}
