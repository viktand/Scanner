using System;
using System.Timers;

namespace DriverScanner
{
    public class ButtonChecker
    {
        public event EventHandler<bool> Click;

        private readonly Timer _timer;
        private readonly KernelConnectorWeb _kernel;
        private bool _exit;

        public ButtonChecker(KernelConnectorWeb kernelConnector) 
        {           
            _kernel = kernelConnector;
            _kernel.ButtonIni();
            _timer = new Timer(200)
            {
                AutoReset = false
            };
            _timer.Elapsed += Timer_Elapsed;
            _timer.Start();
            _exit = false;
            Logger.Log("Чекер кнопки запущен");
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {               
                var status = _kernel.ButtonCheck();
                Click(this, status);
                _timer.Start();                
            }catch(Exception ex)
            {
                Logger.Error($"ошибка при проверке кнопки {ex.Message}");
                _timer.Start();
            }
        }
    }
}
