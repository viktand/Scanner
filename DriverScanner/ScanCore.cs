﻿using Saraff.Twain;
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
        private readonly KernelConnectorWeb _kernel;
        private Twain32 _scaner;
        private bool _check;
        private System.Timers.Timer _timer;
        private bool _go;

        public event EventHandler<int> ScanEvent;
        public event EventHandler<string> NewScan;

        public ScanCore(KernelConnectorWeb kernelConnector)
        {
            _kernel = kernelConnector;
            _scaner = new Twain32();
        }

        public void GoAuto()
        {
            try
            {
                Logger.Log("Запуск процедуры сканирования (GpAuto function)");
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
                        Logger.Log("Ошибка при инициализации сканера. Будет нажатие на кнопку питания.");
                        ScanEvent(this, 10);
                        var mess = _kernel.PowerSwitch();
                        if (mess != "")
                        {
                            Logger.Log($"Не удалось нажать кнопку питания, получена ошибка {mess}");
                            return;
                        }
                        Logger.Log("Кнопку питания нажали.");
                        Thread.Sleep(10000);
                        ScanEvent(this, 1);
                        ScanEvent(this, 12);
                        Logger.Log("Завершение процедуры сканирования - приглашение повторить ее.");
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

                    _scaner.AcquireCompleted += Scaner_AcquireCompleted;
                    _scaner.EndXfer += Scaner_EndXfer;
                    _scaner.AcquireError += Scaner_AcquireError;
                    _scaner.TwainStateChanged += Scaner_TwainStateChanged;
                    _scaner.DeviceEvent += Scaner_DeviceEvent;

                    ScanEvent?.Invoke(this, 1);
                    // Замкнуть датчик бумаги, чтобы сканер начал затягивать документ. Ну он так будет думать.
                    _kernel.PaperSwitch(true, out var m);
                    // Таймер, который отключает датчик бумагм, чтобы остановить процесс, если документ так и не вставили.
                    _timer = new System.Timers.Timer(3000)
                    {
                        AutoReset = false
                    };
                    _timer.Elapsed += (object sender, System.Timers.ElapsedEventArgs e) =>
                    {
                        _kernel.PaperSwitch(false, out var m2);
                        _timer.Stop();
                        _timer.Dispose();
                    };
                    _timer.Start();


                    Logger.Log("Запуск сканирования");
                    _scaner.Acquire();
                    Logger.Log("Сканирование завершено.");
                    _scaner.AcquireCompleted -= Scaner_AcquireCompleted;
                    _scaner.EndXfer -= Scaner_EndXfer;
                    _scaner.AcquireError -= Scaner_AcquireError;
                    _scaner.TwainStateChanged -= Scaner_TwainStateChanged;
                    _scaner.DeviceEvent -= Scaner_DeviceEvent;
                }
                _check = false;
                ScanEvent?.Invoke(this, 13);
            }
            catch (Exception e)
            {
                Logger.Log("Общая ошибка: " + e.Message);
                ScanEvent?.Invoke(this, 11);
            }        
        }

        public void StopLoopScan()
        {
            Logger.Log("Команда завершить циклическое сканирование");
            _go = false;
        }

        public void ContinueScann()
        {
            if (_go) { Logger.Log("Перезапуск сканирования"); }
        }

        private void Scaner_DeviceEvent(object sender, Twain32.DeviceEventEventArgs e)
        {
            ScanEvent?.Invoke(this, 6);
        }

        private void Scaner_TwainStateChanged(object sender, Twain32.TwainStateEventArgs e)
        {
            Console.WriteLine(e.TwainState);
            //ScanEvent?.Invoke(this, 5);
        }      

        private void Scaner_AcquireError(object sender, Twain32.AcquireErrorEventArgs e)
        { 
            Logger.Log($"Ошибка сканирования: {e.Exception.Message}");
            if(_go) ScanEvent?.Invoke(this, 2);            
            _scaner.CloseDataSource();
            _scaner.CloseDSM();        
        }

        private void Scaner_EndXfer(object sender, Twain32.EndXferEventArgs e)
        {
            try
            {
                var _file = @"scans\" + DateTime.Now.Ticks.ToString() + ".jpg";
                Logger.Log($"Сохраниение будет в файл: {_file}");
                e.Image.Save(_file, ImageFormat.Jpeg);
                NewScan?.Invoke(this, _file);
                e.Image.Dispose();
                Logger.Log("Успешно сохранилось");
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0}: {1}{2}{3}{2}", ex.GetType().Name, ex.Message, Environment.NewLine, ex.StackTrace);
                Logger.Log($"Ошибка при сохранении: {ex.Message}");
            }
            ScanEvent?.Invoke(this, 3);
        }


        private void Scaner_AcquireCompleted(object sender, EventArgs e)
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
            Logger.Log("Сканирование завершено успешно");
        }

        public void Close()
        {
            _scaner.Dispose();
        }
    }
}