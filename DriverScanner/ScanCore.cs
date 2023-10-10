using Saraff.Twain;
using System.IO;
using System;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.Threading;
using System.Drawing;

namespace DriverScanner

{
    internal class ScanCore
    {
        private Twain32 _scaner;
        private bool _check;
        private System.Timers.Timer _timer;
        private bool _go;

        public event EventHandler<int> ScanEvent;
        public event EventHandler<string> NewScan;

        public ScanCore()
        {
            _scaner = new Twain32();
        }

        public void GoAuto()
        {
            try
            {
                Log("Запуск процедуры сканирования (GpAuto function)");
                _check = true;
                Task.Run(() =>
                {
                    var pc = new ProcessMG();
                    while (_check)
                    {
                        pc.CheckAndClose();
                        Thread.Sleep(500);
                    }
                });
                ScanEvent?.Invoke(this, 0);
                _go = true;
                while (_go)
                {
                    _scaner.ShowUI = false;
                    _scaner.IsTwain2Enable = false;
                    try // перехват ошибки "сканер не включен"
                    {
                        _scaner.OpenDSM();
                        _scaner.SourceIndex = 0;
                        _scaner.OpenDataSource();
                    }
                    catch
                    {
                        _check = false;
                        Log("Ошибка при инициализации сканера. Будет нажатие на кнопку питания.");
                        ScanEvent(this, 10);
                        var mess = Kernel.PowerSwitch();
                        if (mess != "")
                        {
                            Log($"Не удалось нажать кнопку питания, получена ошибка {mess}");
                            return;
                        }
                        Log("Кнопку питания нажали.");
                        Thread.Sleep(10000);
                        ScanEvent(this, 1);
                        ScanEvent(this, 12);
                        Log("Завершение процедуры сканирования - приглашение повторить ее.");
                        _scaner.Dispose();
                        return;
                    }


                    #region Select Resolution

                    var _resolutions = _scaner.Capabilities.XResolution.Get();
                    var _val = (float)_resolutions[Convert.ToInt32(_resolutions.CurrentIndex)];
                    _scaner.Capabilities.XResolution.Set(_val);
                    _scaner.Capabilities.YResolution.Set(_val);

                    #endregion

                    #region Select Pixel Type

                    var _pixels = _scaner.Capabilities.PixelType.Get();
                    _scaner.Capabilities.PixelType.Set(0);

                    #endregion

                    _scaner.AcquireCompleted += _scaner_AcquireCompleted;
                    _scaner.EndXfer += _scaner_EndXfer;
                    _scaner.AcquireError += _scaner_AcquireError;
                    _scaner.TwainStateChanged += _scaner_TwainStateChanged;
                    _scaner.DeviceEvent += _scaner_DeviceEvent;

                    ScanEvent?.Invoke(this, 1);
                    Kernel.PaperSwitch(true, out var m);
                    _timer = new System.Timers.Timer(3000)
                    {
                        AutoReset = false
                    };
                    _timer.Elapsed += (object sender, System.Timers.ElapsedEventArgs e) =>
                    {
                        Kernel.PaperSwitch(false, out var m2);
                        _timer.Stop();
                        _timer.Dispose();
                    };
                    _timer.Start();


                    Log("Запуск сканирования");
                    _scaner.Acquire();
                    Log("Сканирование завершено");
                    _scaner.AcquireCompleted -= _scaner_AcquireCompleted;
                    _scaner.EndXfer -= _scaner_EndXfer;
                    _scaner.AcquireError -= _scaner_AcquireError;
                    _scaner.TwainStateChanged -= _scaner_TwainStateChanged;
                    _scaner.DeviceEvent -= _scaner_DeviceEvent;
                }
                _check = false;
                ScanEvent?.Invoke(this, 13);
                }catch (Exception e)
            {
                Log("Общая ошибка: " + e.Message);
                ScanEvent?.Invoke(this, 11);
            }        
        }

        public void StopLoopScan()
        {
            Log("Команда завершить циклическое сканирование");
            _go = false;
        }

        public void ContinueScann()
        {
            if (_go) { Log("Перезапуск сканирования"); }
        }

        private void _scaner_DeviceEvent(object sender, Twain32.DeviceEventEventArgs e)
        {
            ScanEvent?.Invoke(this, 6);
        }

        private void _scaner_TwainStateChanged(object sender, Twain32.TwainStateEventArgs e)
        {
            Console.WriteLine(e.TwainState);
            //ScanEvent?.Invoke(this, 5);
        }      

        private void _scaner_AcquireError(object sender, Twain32.AcquireErrorEventArgs e)
        { 
            if(_go) ScanEvent?.Invoke(this, 2);
            Console.WriteLine(e.Exception.Message);
            Log($"Ошибка сканирования: {e.Exception.Message}");
            _scaner.CloseDataSource();
            _scaner.CloseDSM();        
        }

        private void _scaner_EndXfer(object sender, Twain32.EndXferEventArgs e)
        {
            try
            {
                var _file = @"scans\" + DateTime.Now.Ticks.ToString() + ".jpg";
                Log($"Сохраниение будет в файл: {_file}");
                e.Image.Save(_file, ImageFormat.Jpeg);
                NewScan?.Invoke(this, _file);
                e.Image.Dispose();
                Log("Успешно сохранилось");
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0}: {1}{2}{3}{2}", ex.GetType().Name, ex.Message, Environment.NewLine, ex.StackTrace);
                Log($"Ошибка при сохранении: {ex.Message}");
            }
            ScanEvent?.Invoke(this, 3);
        }

        private void Log(string message)
        {
            var text = DateTime.Now.ToString() + " : " + message + "\r\n";
            File.AppendAllText("log.txt", text);
        }

        private void _scaner_AcquireCompleted(object sender, System.EventArgs e)
        {
            try
            {
                Console.WriteLine();
                Console.WriteLine("Acquire Completed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            _scaner.CloseDataSource();
            _scaner.CloseDSM();
            ScanEvent?.Invoke(this, 4);
            Log("Сканирование завершено успешно");
        }

        public void Close()
        {
            _scaner.Dispose();
        }
    }
}