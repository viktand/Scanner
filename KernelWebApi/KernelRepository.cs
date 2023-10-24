using System.IO.Ports;

namespace KernelWebApi
{
    public class KernelRepository : IKernelRepository
    {
        private SerialPort _port;

        public bool Press { get; set; }

        private bool _notWork;

        public KernelRepository()
        {
            _notWork = true;
        }

        public void Start(string port)
        {
            Press = false;            
            _port = new SerialPort(port)
            {
                BaudRate = 9600,
                Parity = Parity.None,
                StopBits = StopBits.One,
                DataBits = 8,
                Handshake = Handshake.None,
                RtsEnable = true
            };
            _port.Open();
            _notWork = false;
        }

        public bool Stop()
        {
            try
            {
                lock (_port)
                {
                    if (_port.IsOpen)
                    {
                        _port.Close();
                        Console.WriteLine($"Порт {_port.PortName} закрыт");
                    }
                    _port.Dispose();
                    return true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Ошибка при закрытии порта {_port.PortName}: {e.Message}");
                return false;
            }
            finally
            {
                _notWork = true;
            }
        }

        /// <summary>
        /// Настроить пин 1 разъема Кернел-чипа на чтение состояния внешней кнопки
        /// 1 - нет нажатия, 0 - есть нажате
        /// </summary>
        /// <returns></returns>
        public bool ButtonIni()
        {
            if (_notWork) return false;
            lock (_port)
            {
                try
                {
                    var send = "$KE,IO,SET,1,1,S\r\n";
                    Console.WriteLine(send);
                    _port.Write(send);
                    var result = _port.ReadLine();
                    Console.WriteLine(result);
                    return result.Contains("OK");
                }
                catch(Exception ex) 
                {
                    Console.WriteLine(ex.Message);
                    return false;
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
            if (_notWork) return false;
            if (Press)
            {
                Press = false;
                return true;
            }
            lock (_port)
            {
                try
                {
                    var send = "$KE,RD,1\r\n";
                    _port.Write(send);
                    var result = _port.ReadLine();
                    return result.Contains("#RD,01,0");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Check button: {ex.Message}");
                    return false;
                }              
            }
        }

        /// <summary>
        /// Включить датчик бумаги
        /// </summary>
        /// <returns></returns>
        public bool PaperSwitch(bool state, out string message)
        {
            message = "";
            if (_notWork) return false;
            lock (_port)
            {                
                try
                {
                    _port.Write($"$KE,REL,3,{(state ? "1" : "0")}\r\n");
                    message = _port.ReadLine();
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при переключении датчика бумаги: {ex.Message}");
                    message = ex.Message;
                    return false;
                }               
            }
        }

        /// <summary>
        /// Нажать кнопку питания
        /// </summary>
        /// <returns></returns>
        public string PowerSwitch()
        {
            if (_notWork) return "not init com-port";
            lock (_port)
            {
                try
                {
                    _port.Write($"$KE,REL,1,1\r\n");
                    Thread.Sleep(500);
                    _port.Write($"$KE,REL,1,0\r\n");
                    return "";
                }
                catch (Exception e)
                {
                    return e.Message;
                }
            }
        }

        /// <summary>
        /// Нажать кнопку сканирования
        /// </summary>
        /// <returns></returns>
        public string ScannSwitch()
        {
            if (_notWork) return "not init com-port";
            lock (_port)
            {
                try
                {
                    _port.Write($"$KE,REL,2,1\r\n");
                    Thread.Sleep(500);
                    _port.Write($"$KE,REL,2,0\r\n");
                    return "";
                }
                catch (Exception e)
                {
                    return e.Message;
                }
            }
        }      
    }
}

