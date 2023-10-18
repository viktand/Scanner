using System;
using System.Timers;

namespace DriverScanner
{
    public class ButtonChecker
    {
        public event EventHandler<bool> Click;

        private Timer _timer;

        public ButtonChecker() 
        {
            Kernel.ButtonIni();
            _timer = new Timer(200)
            {
                AutoReset = false
            };
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var status = Kernel.ButtonCheck();
            Click(this, status);
            _timer.Start();
        }
    }
}
