﻿using System;
using System.Configuration;
using System.IO.Ports;
using System.Threading;

namespace DriverScanner
{
    public class KernelConnector
    {
        private readonly SerialPort _port;

        public bool IsRun { get; set; }

        public KernelConnector() 
        {
            var com = ConfigurationManager.AppSettings["comport"];
            _port = new SerialPort
            {
                PortName = com
            };
            IsRun = true;
        }

        /// <summary>
        /// Настроить пин 1 разъема Кернел-чипа на чтение состояния внешней кнопки
        /// 1 - нет нажатия, 0 - есть нажате
        /// </summary>
        /// <returns></returns>
        public bool ButtonIni()
        {
            lock (_port)
            {
                try
                {
                    _port.Open();
                    var send = "$KE,IO,SET,1,1,S\r\n";
                    Logger.Log(send);
                    _port.Write(send);
                    var result = _port.ReadLine();
                    Logger.Log(result);
                    return result.Contains("OK");
                }
                catch
                {
                    return false;
                }
                finally
                {
                    _port.Close();
                }
            }
        }
        /// <summary>
        /// Проверка состояния кнопки 
        /// false - отпущена, true - нажата
        /// </summary>
        /// <returns></returns>
        public bool ButtonCheck()
        {
            lock (_port)
            {
                try
                {
                    _port.Open();
                    var send = "$KE,RD,1\r\n";
                    _port.Write(send);
                    var result = _port.ReadLine();
                    return result.Contains("#RD,01,0");
                }
                catch(Exception ex) 
                {
                    Logger.Error($"Check button: {ex.Message}");
                    return false;
                }
                finally
                {
                    _port.Close();
                }
            }
        }

        /// <summary>
        /// Включить датчик бумаги
        /// </summary>
        /// <returns></returns>
        public bool PaperSwitch(bool state, out string message)
        {
            lock (_port)
            {
                message = "";
                try
                {
                    _port.Open();
                    _port.Write($"$KE,REL,3,{(state ? "1" : "0")}\r\n");
                    message = _port.ReadLine();                    
                    return true;
                }
                catch(Exception ex ) 
                {
                    Logger.Error($"Ошибка при переключении датчика бумаги: {ex.Message}");
                    return false;
                }
                finally
                {
                    _port.Close();
                }
            }
        }

        /// <summary>
        /// Нажать кнопку питания
        /// </summary>
        /// <returns></returns>
        public string PowerSwitch()
        {
            lock (_port)
            {
                var com = ConfigurationManager.AppSettings["comport"];
                try
                {
                    _port.Open();
                    _port.Write($"$KE,REL,1,1\r\n");
                    Thread.Sleep(500);
                    _port.Write($"$KE,REL,1,0\r\n");
                    return "";
                }
                catch (Exception e)
                {
                    return e.Message;
                }
                finally
                {
                    _port.Close();
                }
            }
        }

        /// <summary>
        /// Нажать кнопку сканирования
        /// </summary>
        /// <returns></returns>
        public string ScannSwitch()
        {
            lock (_port)
            {
                try
                {
                    _port.Open();
                    _port.Write($"$KE,REL,2,1\r\n");
                    Thread.Sleep(500);
                    _port.Write($"$KE,REL,2,0\r\n");
                    return "";
                }
                catch (Exception e)
                {
                    return e.Message;
                }
                finally
                {
                    _port.Close();
                }
            }
        }

        internal void Close()
        {
            lock(_port)
            {
                IsRun = false;
                if (_port.IsOpen)
                {
                    _port.Close();
                }
                _port.Dispose();
                Logger.Log("Com-port was closed");
            }
        }
    }
}
