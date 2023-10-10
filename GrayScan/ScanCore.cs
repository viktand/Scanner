using Saraff.Twain;
using System.IO;
using System;
using System.Drawing.Imaging;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Threading;

namespace GrayScan
{
    internal class ScanCore
    {
        private Twain32 _scaner;
        private bool _check;

        public ScanCore()
        {
            _scaner = new Twain32();
        }

        public void GoAuto()
        {
            _scaner.ShowUI = false;
            _scaner.IsTwain2Enable = false;
            _scaner.OpenDSM();
            Console.WriteLine();
            Console.WriteLine("Select Data Source from list:");
            for (var i = 0; i < _scaner.SourcesCount; i++)
            {
                Console.WriteLine("{0}: {1}{2}", i, _scaner.GetSourceProductName(i), _scaner.IsTwain2Supported && _scaner.GetIsSourceTwain2Compatible(i) ? " (TWAIN 2.x)" : string.Empty);
            }
            Console.Write("Select: " + _scaner.GetSourceProductName(0));
            _scaner.SourceIndex = 0;         

            _scaner.OpenDataSource();
            //_scaner.DisableAfterAcquire = true;
            

            #region Select Resolution

            Console.WriteLine();
            Console.WriteLine("Select Resolution:");
            var _resolutions = _scaner.Capabilities.XResolution.Get();
            var _val = (float)_resolutions[Convert.ToInt32(_resolutions.CurrentIndex)];
            _scaner.Capabilities.XResolution.Set(_val);
            _scaner.Capabilities.YResolution.Set(_val);
         
            Console.WriteLine(string.Format("Resolution: {0}", _scaner.Capabilities.XResolution.GetCurrent()));

            #endregion

            #region Select Pixel Type

            Console.WriteLine();
            Console.WriteLine("Select Pixel Type:");
            var _pixels = _scaner.Capabilities.PixelType.Get();
            for (var i = 0; i < _pixels.Count; i++)
            {
                Console.WriteLine("{0}: {1}", i, _pixels[i]);
            }
            Console.Write("[{0}]: ", _pixels.CurrentIndex);        
            _scaner.Capabilities.PixelType.Set(0);
            Console.WriteLine(string.Format("Pixel Type: {0}", _scaner.Capabilities.PixelType.GetCurrent()));

            #endregion

            _scaner.AcquireCompleted += _scaner_AcquireCompleted;
            _scaner.EndXfer += _scaner_EndXfer;
            _scaner.AcquireError += _scaner_AcquireError;
            _scaner.TwainStateChanged += _scaner_TwainStateChanged;
            _check = true;
            Task.Run(() =>
            {
                var pc = new ProcessMG();
                while (_check)
                {
                    pc.CheckAndClose(_scaner);
                    Thread.Sleep(500);
                }
            });
            
            _scaner.Acquire();

        }

        private void _scaner_TwainStateChanged(object sender, Twain32.TwainStateEventArgs e)
        {
            Console.WriteLine("New state: " + e.TwainState);
        }

        public void Go()
        {
            _scaner.ShowUI = false;
            _scaner.IsTwain2Enable = false;
            _scaner.OpenDSM();
            Console.WriteLine();
            Console.WriteLine("Select Data Source:");
            for (var i = 0; i < _scaner.SourcesCount; i++)
            {
                Console.WriteLine("{0}: {1}{2}", i, _scaner.GetSourceProductName(i), _scaner.IsTwain2Supported && _scaner.GetIsSourceTwain2Compatible(i) ? " (TWAIN 2.x)" : string.Empty);
            }
            Console.Write("[{0}]: ", _scaner.SourceIndex);
            for (var _res = Console.ReadLine().Trim(); !string.IsNullOrEmpty(_res);)
            {
                _scaner.SourceIndex = Convert.ToInt32(_res);
                break;
            }
            Console.WriteLine(string.Format("Data Source: {0}", _scaner.GetSourceProductName(_scaner.SourceIndex)));

            _scaner.OpenDataSource();

            #region Select Resolution

            Console.WriteLine();
            Console.WriteLine("Select Resolution:");
            var _resolutions = _scaner.Capabilities.XResolution.Get();
            for (var i = 0; i < _resolutions.Count; i++)
            {
                Console.WriteLine("{0}: {1} dpi", i, _resolutions[i]);
            }
            Console.Write("[{0}]: ", _resolutions.CurrentIndex);
            for (var _res = Console.ReadLine().Trim(); !string.IsNullOrEmpty(_res);)
            {
                var _val = (float)_resolutions[Convert.ToInt32(_res)];
                _scaner.Capabilities.XResolution.Set(_val);
                _scaner.Capabilities.YResolution.Set(_val);
                break;
            }
            Console.WriteLine(string.Format("Resolution: {0}", _scaner.Capabilities.XResolution.GetCurrent()));

            #endregion

            #region Select Pixel Type

            Console.WriteLine();
            Console.WriteLine("Select Pixel Type:");
            var _pixels = _scaner.Capabilities.PixelType.Get();
            for (var i = 0; i < _pixels.Count; i++)
            {
                Console.WriteLine("{0}: {1}", i, _pixels[i]);
            }
            Console.Write("[{0}]: ", _pixels.CurrentIndex);
            for (var _res = Console.ReadLine().Trim(); !string.IsNullOrEmpty(_res);)
            {
                var _val = (TwPixelType)_pixels[Convert.ToInt32(_res)];
                _scaner.Capabilities.PixelType.Set(_val);
                break;
            }
            Console.WriteLine(string.Format("Pixel Type: {0}", _scaner.Capabilities.PixelType.GetCurrent()));

            #endregion

            _scaner.AcquireCompleted += _scaner_AcquireCompleted;
            _scaner.EndXfer += _scaner_EndXfer;
            _scaner.AcquireError += _scaner_AcquireError;

            _scaner.Acquire();           
        }

        private void _scaner_AcquireError(object sender, Twain32.AcquireErrorEventArgs e)
        {
            Console.WriteLine("AcquireError:" + e.Exception.Message);
            _scaner.CloseDataSource();
            _scaner.CloseDSM();
            _check = false;
        }

        private void _scaner_EndXfer(object sender, Twain32.EndXferEventArgs e)
        {
            try
            {
                var _file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), Path.ChangeExtension(Path.GetFileName(Path.GetTempFileName()), ".jpg"));
                e.Image.Save(_file, ImageFormat.Jpeg);
                Console.WriteLine();
                Console.WriteLine(string.Format("Saved in: {0}", _file));
                e.Image.Dispose();
                _check = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0}: {1}{2}{3}{2}", ex.GetType().Name, ex.Message, Environment.NewLine, ex.StackTrace);
            }
        }

        private void _scaner_AcquireCompleted(object sender, System.EventArgs e)
        {
            try
            {
                Console.WriteLine();
                Console.WriteLine("Acquire Completed.");
                _check = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            _scaner.CloseDataSource();
            _scaner.CloseDSM();
        }

        public void Close()
        {
            _scaner.Dispose();
        }
    }
}