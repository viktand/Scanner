using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Saraff.Tiff;
using Saraff.Tiff.Core;

namespace Saraff.Twain.Sample2.Core {

    internal sealed class BmpImageFactory:Component, IImageFactory<Bitmap> {

        #region IImageFactory

        public Bitmap Create(Stream stream) {
            switch(Environment.OSVersion.Platform) {
                case PlatformID.Unix:
                    //using(var _stream = File.Create(Path.ChangeExtension(Path.GetFileName(Path.GetTempFileName()),".tif"))) {
                    //    stream.CopyTo(_stream);
                    //    _stream.Flush();
                    //    stream.Seek(0L,SeekOrigin.Begin);
                    //}
                    using(var _resultStream = this.StreamProvider?.GetStream()??new MemoryStream()) {
                        using(var _reader = TiffReader.Create(stream)) {
                            _reader.ReadHeader();
                            this.Loop(_reader,x => x.ReadImageFileDirectory(),x => x!=0,count => {
                                var _header = new BITMAPINFOHEADER();
                                var _resolutionUnit = TiffResolutionUnit.NONE;
                                var _pixeltype = TiffPhotoMetric.RGB;
                                var _bitCount = new List<short>();
                                var _samplesPerPixel = 0;
                                float _xdpi = 0f, _ydpi = 0f;
                                var _colorMap = new List<ushort>();
                                var _strips = new List<TiffHandle>();
                                var _stripsLength = new List<int>();

                                #region Read TIFF Tags

                                var _ulong2float = new Func<ulong,float>(x => (float)((ulong)x&0xffffffff)/(float)((ulong)x>>32));
                                var _handlers = new Dictionary<TiffTags,Action<object>> {
                                    {TiffTags.ImageWidth, x => _header.biWidth=Convert.ToInt32(x)},
                                    {TiffTags.ImageLength, x => _header.biHeight=Convert.ToInt32(x)},
                                    {TiffTags.BitsPerSample, x => _bitCount.Add(Convert.ToInt16(x))},
                                    {TiffTags.Compression, x => _header.biCompression=(TiffCompression)x==TiffCompression.NONE?0:-1},
                                    {TiffTags.ResolutionUnit, x => _resolutionUnit=(TiffResolutionUnit)x},
                                    {TiffTags.XResolution, x => _xdpi=_ulong2float(Convert.ToUInt64(x))},
                                    {TiffTags.YResolution, x => _ydpi=_ulong2float(Convert.ToUInt64(x))},
                                    {TiffTags.PhotometricInterpretation, x => _pixeltype=(TiffPhotoMetric)x},
                                    {TiffTags.StripOffsets, x => _strips.Add(x as TiffHandle)},
                                    {TiffTags.StripByteCounts, x =>  _stripsLength.Add(Convert.ToInt32(x))},
                                    {TiffTags.ColorMap, x => _colorMap.Add(Convert.ToUInt16(x))},
                                    {TiffTags.SampleFormat, x => {}},
                                    {TiffTags.SamplesPerPixel, x => _samplesPerPixel=Convert.ToInt32(x)}
                                };

                                this.Loop(_reader,x => x.ReadTag(),x => x!=null,x => {
                                    if(_handlers.ContainsKey(x.TagId)) {
                                        this.Loop(_reader,y => x.TagId!=TiffTags.StripOffsets ? y.ReadValue() : y.ReadHandle(),y => y!=null,y => _handlers[x.TagId](y));
                                    } else {
                                        this.Loop(_reader,y => y.ReadValue(),y => y!=null,y => {});
                                    }
                                });

                                #endregion

                                _header.biBitCount=(short)(_samplesPerPixel>0&&_bitCount.Count!=_samplesPerPixel ? _bitCount[0]*_samplesPerPixel : _bitCount.Sum(x => x));
                                if(_resolutionUnit!=TiffResolutionUnit.NONE) {
                                    _header.biXPelsPerMeter=(int)(_xdpi/(_resolutionUnit==TiffResolutionUnit.INCH ? 0.0254f : 0.01f));
                                    _header.biYPelsPerMeter=(int)(_ydpi/(_resolutionUnit==TiffResolutionUnit.INCH ? 0.0254f : 0.01f));
                                }
                                if(_header.biCompression==0) {
                                    var _scan = _header.biWidth*_header.biBitCount;
                                    _scan=(_scan>>3)+((_scan&0x7)!=0 ? 1 : 0);
                                    _header.biSizeImage=((_scan&0x3)!=0 ? _scan+0x4-(_scan&0x3) : _scan)*_header.biHeight;

                                    #region Create Pallete

                                    var _pallete = new List<RgbQuad>();
                                    for(int i = 0, _len = _colorMap.Count/3; i<_len; i++) {
                                        _pallete.Add(new RgbQuad {
                                            rgbRed=(byte)_colorMap[i],
                                            rgbGreen=(byte)_colorMap[i+_len],
                                            rgbBlue=(byte)_colorMap[i+(_len<<1)]
                                        });
                                    }
                                    if((_pixeltype==TiffPhotoMetric.BlackIsZero||_pixeltype==TiffPhotoMetric.WhiteIsZero)&&_pallete.Count==0) {
                                        for(byte i = 0x00; _pallete.Count<0x01<<_header.biBitCount; i+=(byte)(0xff/((0x01<<_header.biBitCount)-1))) {
                                            if(_pixeltype==TiffPhotoMetric.BlackIsZero) {
                                                _pallete.Add(new RgbQuad { rgbRed=i,rgbGreen=i,rgbBlue=i });
                                            } else {
                                                _pallete.Insert(0,new RgbQuad { rgbRed=i,rgbGreen=i,rgbBlue=i });
                                            }
                                        }
                                    }
                                    _header.biClrImportant=_header.biClrUsed=_pallete.Count;

                                    #endregion

                                    #region Write Bitmap File

                                    var _writer = new BinaryWriter(_resultStream);

                                    #region  Write BITMAPFILEHEADER

                                    _writer.Write((ushort)0x4d42);
                                    _writer.Write(14+_header.biSize+_header.biSizeImage+(_header.ClrUsed<<2)/*DIB Size*/);
                                    _writer.Write(0);
                                    _writer.Write(14+_header.biSize+(_header.ClrUsed<<2)/*Offset to Image Data*/);

                                    #endregion

                                    #region Write BITMAPINFO

                                    var _headerPtr = Marshal.AllocHGlobal(_header.biSize);
                                    try {
                                        Marshal.StructureToPtr(_header,_headerPtr,false);
                                        var _data = new byte[_header.biSize];
                                        Marshal.Copy(_headerPtr,_data,0,_data.Length);
                                        _writer.Write(_data);
                                    } finally {
                                        Marshal.FreeHGlobal(_headerPtr);
                                    }

                                    #region Write Pallete (Array of RGBQUAD)

                                    var _quadData = new byte[Marshal.SizeOf(typeof(RgbQuad))];
                                    var _quadPtr = Marshal.AllocHGlobal(_quadData.Length);
                                    try {
                                        foreach(var _item in _pallete) {
                                            Marshal.StructureToPtr(_item,_quadPtr,false);
                                            Marshal.Copy(_quadPtr,_quadData,0,_quadData.Length);
                                            _writer.Write(_quadData);
                                        }
                                    } finally {
                                        Marshal.FreeHGlobal(_quadPtr);
                                    }

                                    #endregion

                                    #endregion

                                    #region Write Pixel Data

                                    for(var i = _strips.Count-1; i>=0; i--) {
                                        var _data = _reader.ReadData(_strips[i],_stripsLength[i]);
                                        var _extra = new byte[(_scan&0x3)!=0 ? 4-(_scan&0x3) : 0];

                                        for(var ii = (_data.Length/_scan)-1; ii>=0; ii--) {
                                            switch(_header.biBitCount) {
                                                case 24:
                                                case 32:
                                                    for(var _offset = ii*_scan; _offset<(ii+1)*_scan; _offset+=_header.biBitCount>>3) {
                                                        var _temp = _data[_offset];
                                                        _data[_offset]=_data[_offset+2];
                                                        _data[_offset+2]=_temp;
                                                    }
                                                    break;
                                                case 48:
                                                case 64:
                                                    for(var _offset = ii*_scan; _offset<(ii+1)*_scan; _offset+=_header.biBitCount>>3) {
                                                        var _temp1 = _data[_offset];
                                                        var _temp2 = _data[_offset+1];
                                                        _data[_offset]=_data[_offset+4];
                                                        _data[_offset+1]=_data[_offset+5];
                                                        _data[_offset+4]=_temp1;
                                                        _data[_offset+5]=_temp2;
                                                    }
                                                    break;
                                            }

                                            _writer.Write(_data,ii*_scan,_scan);
                                            if(_extra.Length>0) {
                                                _writer.Write(_extra);
                                            }
                                        }
                                    }

                                    #endregion

                                    #endregion
                                }
                            });
                        }
                        return new Bitmap(_resultStream);
                    }
                case PlatformID.MacOSX:
                    throw new NotSupportedException();
                default:
                    return new Bitmap(stream);
            }
        }

        #endregion

        [IoC.ServiceRequired]
        public IStreamProvider StreamProvider {
            get;
            set;
        }

        private void Loop<T>(TiffReader reader,Func<TiffReader,T> loopFunc,Func<T,bool> loopExpr,Action<T> action) {
            for(var _val = loopFunc(reader); loopExpr(_val); _val=loopFunc(reader)) {
                action(_val);
            }
        }

        [StructLayout(LayoutKind.Sequential,Pack = 2)]
        private sealed class BITMAPINFOHEADER {
            public int biSize;
            public int biWidth;
            public int biHeight;
            public short biPlanes;
            public short biBitCount;
            public int biCompression;
            public int biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public int biClrUsed;
            public int biClrImportant;

            public BITMAPINFOHEADER() {
                this.biSize=Marshal.SizeOf(this);
                this.biPlanes=1;
            }

            public int ClrUsed {
                get {
                    return this.IsRequiredCreateColorTable ? Convert.ToInt32(Math.Pow(2,this.biBitCount)) : this.biClrUsed;
                }
            }

            public bool IsRequiredCreateColorTable {
                get {
                    return this.biClrUsed==0&&this.biBitCount<=8;
                }
            }
        }

        [StructLayout(LayoutKind.Sequential,Pack = 2)]
        private sealed class RgbQuad {
            public byte rgbBlue;
            public byte rgbGreen;
            public byte rgbRed;
            public byte rgbReserved;
        }
    }
}
