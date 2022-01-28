using System;
using System.Buffers;
using System.IO;
using WolvenKit.Common.Model.Arguments;
using WolvenKit.Common.Tools.DDS;
using WolvenKit.Core.Extensions;
using static WolvenKit.Common.Tools.DDS.TexconvNative;

namespace WolvenKit.Common.DDS
{
    public enum TEX_MISC_FLAG
    // Subset here matches D3D10_RESOURCE_MISC_FLAG and D3D11_RESOURCE_MISC_FLAG
    {
        TEX_MISC_TEXTURECUBE = 0x4,
    };

    public enum TEX_DIMENSION
    // Subset here matches D3D10_RESOURCE_DIMENSION and D3D11_RESOURCE_DIMENSION
    {
        TEX_DIMENSION_TEXTURE1D = 2,
        TEX_DIMENSION_TEXTURE2D = 3,
        TEX_DIMENSION_TEXTURE3D = 4,
    };

    public static class DDSUtils
    {
        #region Fields

        private const uint DDS_MAGIC = 0x20534444;

        #endregion Fields

        // "DDS "

        // constants

        #region DDS_HEADER

        // dwCaps2
        public const uint DDSCAPS2_CUBEMAP = 0x00000200;

        public const uint DDSCAPS2_CUBEMAP_ALL_FACES = DDS_CUBEMAP_POSITIVEX | DDS_CUBEMAP_NEGATIVEX |
                                                        DDS_CUBEMAP_POSITIVEY | DDS_CUBEMAP_NEGATIVEY |
                                                        DDS_CUBEMAP_POSITIVEZ | DDS_CUBEMAP_NEGATIVEZ;

        private const uint DDS_CUBEMAP_NEGATIVEX = 0x00000a00;

        private const uint DDS_CUBEMAP_NEGATIVEY = 0x00002200;

        private const uint DDS_CUBEMAP_NEGATIVEZ = 0x00008200;

        private const uint DDS_CUBEMAP_POSITIVEX = 0x00000600;

        // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_POSITIVEX
        // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_NEGATIVEX
        private const uint DDS_CUBEMAP_POSITIVEY = 0x00001200;

        // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_POSITIVEY
        // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_NEGATIVEY
        private const uint DDS_CUBEMAP_POSITIVEZ = 0x00004200;

        private const uint DDS_HEADER_FLAGS_TEXTURE = 0x00001007;

        // dwCaps
        private const uint DDSCAPS_COMPLEX = 0x00000008;

        // DDSD_CAPS | DDSD_HEIGHT | DDSD_WIDTH | DDSD_PIXELFORMAT
        private const uint DDSCAPS_MIPMAP = 0x00400000;

        private const uint DDSCAPS_TEXTURE = 0x00001000;

        // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_POSITIVEZ
        // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_NEGATIVEZ
        private const uint DDSCAPS2_VOLUME = 0x00200000;

        // dwflags
        private const uint DDSD_CAPS = 0x00000001;

        private const uint DDSD_DEPTH = 0x00800000;

        //required
        private const uint DDSD_HEIGHT = 0x00000002;

        private const uint DDSD_LINEARSIZE = 0x00080000;

        private const uint DDSD_MIPMAPCOUNT = 0x00020000;

        private const uint DDSD_PITCH = 0x00000008;

        private const uint DDSD_PIXELFORMAT = 0x00001000;

        //required
        private const uint DDSD_WIDTH = 0x00000004;

        // dwsize
        private const uint HEADER_SIZE = 124;

        //required
        //required

        #endregion DDS_HEADER

        #region DDS_HEADER_DXT10

        private const uint D3D10_RESOURCE_MISC_GENERATE_MIPS = 0x00000001;
        private const uint DDS_ALPHA_MODE_UNKNOWN = 0x00000000;
        private const uint DDS_RESOURCE_MISC_TEXTURECUBE = 0x00000004;

        #endregion DDS_HEADER_DXT10

        #region Methods

        private static uint MAKEFOURCC(char ch0, char ch1, char ch2, char ch3) => (uint)(ch0 | ch1 << 8 | ch2 << 16 | ch3 << 24);

        private static void SetPixelmask(Func<uint[]> pfmtfactory, ref DDS_PIXELFORMAT pfmt)
        {
            var masks = pfmtfactory.Invoke();
            pfmt.dwRGBBitCount = masks[0];
            pfmt.dwRBitMask = masks[1];
            pfmt.dwGBitMask = masks[2];
            pfmt.dwBBitMask = masks[3];
            pfmt.dwABitMask = masks[4];
        }

        #endregion Methods

        #region DDS_PIXELFORMAT

        private const uint DDPF_ALPHA = 0x00000002;

        // dwFlags
        private const uint DDPF_ALPHAPIXELS = 0x00000001;

        private const uint DDPF_FOURCC = 0x00000004;

        private const uint DDPF_LUMINANCE = 0x00020000;

        private const uint DDPF_NORMAL = 0x80000000;

        private const uint DDPF_RGB = 0x00000040;

        // dwSize
        private const uint PIXELFORMAT_SIZE = 32;

        // Custom nv flag

        private static uint[] DDSPF_A1R5G5B5() => new uint[5] { 16, 0x00007c00, 0x000003e0, 0x0000001f, 0x00000000 };

        private static uint[] DDSPF_A4R4G4B4() => new uint[5] { 16, 0x00000f00, 0x000000f0, 0x0000000f, 0x0000f000 };

        private static uint[] DDSPF_A8() => new uint[5] { 8, 0x00, 0x00, 0x00, 0xff };

        private static uint[] DDSPF_A8B8G8R8() => new uint[5] { 32, 0x000000ff, 0x0000ff00, 0x00ff0000, 0xff000000 };

        private static uint[] DDSPF_A8L8() => new uint[5] { 16, 0x00ff, 0x0000, 0x0000, 0xff00 };

        //dwRGBBitCount     dwRBitMask      dwGBitMask      dwBBitMask      dwABitMask
        private static uint[] DDSPF_A8R8G8B8() => new uint[5] { 32, 0x00ff0000, 0x0000ff00, 0x000000ff, 0xff000000 };

        private static uint[] DDSPF_G16R16() => new uint[5] { 32, 0x0000ffff, 0xffff0000, 0x00000000, 0x00000000 };

        private static uint[] DDSPF_L8() => new uint[5] { 8, 0xff, 0x00, 0x00, 0x00 };

        private static uint[] DDSPF_R5G6B5() => new uint[5] { 16, 0x0000f800, 0x000007e0, 0x0000001f, 0x00000000 };

        private static uint[] DDSPF_R8G8B8() => new uint[5] { 24, 0x00ff0000, 0x0000ff00, 0x000000ff, 0x00000000 };

        private static uint[] DDSPF_X8B8G8R8() => new uint[5] { 32, 0x000000ff, 0x0000ff00, 0x00ff0000, 0x00000000 };

        private static uint[] DDSPF_X8R8G8B8() => new uint[5] { 32, 0x00ff0000, 0x0000ff00, 0x000000ff, 0x00000000 };

        #endregion DDS_PIXELFORMAT

        #region Writing

        public static void GenerateAndWriteHeader(Stream stream, DDSMetadata metadata)
        {
            var (header, dxt10header) = GenerateHeader(metadata);
            WriteHeader(stream, header, dxt10header);
        }

        private static (DDS_HEADER, DDS_HEADER_DXT10) GenerateHeader(DDSMetadata metadata)
        {
            var height = metadata.Height;
            var width = metadata.Width;
            var mipscount = metadata.Mipscount;
            var iscubemap = metadata.IsCubeMap();
            var format = metadata.Format;
            var dxt10 = metadata.Dx10;

            var ddspf = new DDS_PIXELFORMAT()
            {
                dwSize = PIXELFORMAT_SIZE,
                dwFlags = 0,
                dwFourCC = 0,
                dwRGBBitCount = 0,
                dwRBitMask = 0,
                dwGBitMask = 0,
                dwBBitMask = 0,
                dwABitMask = 0
            };

            var header = new DDS_HEADER()
            {
                dwSize = HEADER_SIZE,
                dwFlags = DDS_HEADER_FLAGS_TEXTURE,
                dwHeight = height,
                dwWidth = width,
                dwPitchOrLinearSize = 0,
                dwDepth = 0,
                dwMipMapCount = 0,
                dwReserved1 = 0,
                dwReserved2 = 0,
                dwReserved3 = 0,
                dwReserved4 = 0,
                dwReserved5 = 0,
                dwReserved6 = 0,
                dwReserved7 = 0,
                dwReserved8 = 0,
                dwReserved9 = 0,
                dwReserved10 = 0,
                dwReserved11 = 0,
                ddspf = ddspf,
                dwCaps = DDSCAPS_TEXTURE,
                dwCaps2 = 0,
                dwCaps3 = 0,
                dwCaps4 = 0,
                dwReserved12 = 0,
            };

            var dx10header = new DDS_HEADER_DXT10()
            {
                dxgiFormat = DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM,
                resourceDimension = D3D10_RESOURCE_DIMENSION.D3D10_RESOURCE_DIMENSION_TEXTURE2D,
                miscFlag = 0,
                arraySize = metadata.Slicecount,
                miscFlags2 = 0
            };

            if (mipscount > 0)
            {
                header.dwMipMapCount = mipscount;
            }

            // pixelformat
            {
                // dwFourCC
                if (dxt10)
                {
                    ddspf.dwFourCC = MAKEFOURCC('D', 'X', '1', '0');
                }
                else
                {
                    switch (format)
                    {
                        case DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_FLOAT:
                            ddspf.dwFourCC = 116;
                            break;

                        case DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_FLOAT:
                            ddspf.dwFourCC = 113;
                            break;

                        case DXGI_FORMAT.DXGI_FORMAT_R10G10B10A2_UNORM:
                            throw new NotImplementedException();
                        case DXGI_FORMAT.DXGI_FORMAT_R32_UINT:
                            throw new NotImplementedException();
                        case DXGI_FORMAT.DXGI_FORMAT_R8G8_UNORM:
                            SetPixelmask(DDSPF_A8L8, ref ddspf);
                            break;

                        case DXGI_FORMAT.DXGI_FORMAT_R16_FLOAT:
                            ddspf.dwFourCC = 111;
                            break;

                        case DXGI_FORMAT.DXGI_FORMAT_R8_UNORM:
                        case DXGI_FORMAT.DXGI_FORMAT_R8_UINT:
                            SetPixelmask(DDSPF_L8, ref ddspf);
                            break;

                        case DXGI_FORMAT.DXGI_FORMAT_A8_UNORM:
                            SetPixelmask(DDSPF_A8, ref ddspf);
                            break;

                        case DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM:
                            SetPixelmask(DDSPF_A8R8G8B8, ref ddspf);
                            break;

                        case DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM:
                            ddspf.dwFourCC = MAKEFOURCC('D', 'X', 'T', '1');
                            break;

                        case DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM:
                            ddspf.dwFourCC = MAKEFOURCC('D', 'X', 'T', '3');
                            break;

                        case DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM:
                            ddspf.dwFourCC = MAKEFOURCC('D', 'X', 'T', '5');
                            break;

                        case DXGI_FORMAT.DXGI_FORMAT_BC4_UNORM:
                            ddspf.dwFourCC = MAKEFOURCC('B', 'C', '4', 'U');
                            break;

                        case DXGI_FORMAT.DXGI_FORMAT_BC5_UNORM:
                            ddspf.dwFourCC = MAKEFOURCC('B', 'C', '5', 'U');
                            break;

                        case DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM:
                            dxt10 = true;
                            break;

                        default:
                            throw new MissingFormatException($"Missing format: {format}");
                    }
                }

                // dwflags
                if (ddspf.dwABitMask != 0) // check this
                {
                    ddspf.dwFlags |= DDPF_ALPHAPIXELS;
                }
                /*if (ddspf.dwABitMask != 0)
   ddspf.dwFlags |= DDPF_ALPHA;*/ //old
                if (ddspf.dwFourCC != 0)
                {
                    ddspf.dwFlags |= DDPF_FOURCC;
                }

                if (ddspf.dwRGBBitCount != 0 && ddspf.dwRBitMask != 0 && ddspf.dwGBitMask != 0 && ddspf.dwBBitMask != 0)
                {
                    ddspf.dwFlags |= DDPF_RGB;
                }

                if (ddspf.dwRBitMask != 0 && ddspf.dwGBitMask == 0 && ddspf.dwBBitMask == 0 && ddspf.dwABitMask == 0)
                {
                    ddspf.dwFlags |= DDPF_LUMINANCE;
                }

                header.ddspf = ddspf;
            }

            // dwPitchOrLinearSize
            uint p = 0;
            var bpp = ddspf.dwRGBBitCount;
            switch (format)
            {
                case DXGI_FORMAT.DXGI_FORMAT_R8_UNORM:
                    header.dwPitchOrLinearSize = (width * bpp + 7) / 8;
                    break;

                case DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_FLOAT:
                case DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_FLOAT:
                case DXGI_FORMAT.DXGI_FORMAT_R10G10B10A2_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_R32_UINT:
                case DXGI_FORMAT.DXGI_FORMAT_R8_UINT:
                case DXGI_FORMAT.DXGI_FORMAT_R8G8_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_R16_FLOAT:
                case DXGI_FORMAT.DXGI_FORMAT_A8_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM:
                    header.dwPitchOrLinearSize = (width * bpp + 7) / 8;
                    header.dwFlags |= DDSD_PITCH;
                    break;

                case DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC4_UNORM:
                    p = width * height / 2; //max(1,width ?4)x max(1,height ?4)x 8 (DXT1)
                    header.dwPitchOrLinearSize = p;
                    header.dwFlags |= DDSD_LINEARSIZE;
                    break;

                case DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC5_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM:
                    p = width * height;     //max(1,width ?4)x max(1,height ?4)x 16 (DXT2-5)
                    header.dwPitchOrLinearSize = p;
                    header.dwFlags |= DDSD_LINEARSIZE;
                    break;

                default:
                    throw new MissingFormatException($"Missing format: {format}");
            }
            // unused R8G8_B8G8, G8R8_G8B8, legacy UYVY-packed, and legacy YUY2-packed formats,
            // header.dwPitchOrLinearSize = ((width + 1) >> 1) * 4;

            // depth
            //if (slicecount > 0  && !iscubemap)
            //    header.dwDepth = slicecount;

            // caps
            if (iscubemap || mipscount > 0)
            {
                header.dwCaps |= DDSCAPS_COMPLEX;
            }

            if (mipscount > 0)
            {
                header.dwCaps |= DDSCAPS_MIPMAP;
            }

            // caps2
            if (iscubemap)
            {
                header.dwCaps2 |= DDSCAPS2_CUBEMAP_ALL_FACES | DDSCAPS2_CUBEMAP;
            }
            //if (slicecount > 0)
            //    header.dwCaps2 |= DDSCAPS2_VOLUME;

            // flags
            //if (slicecount > 0)
            //    header.dwFlags |= DDSD_DEPTH;
            if (mipscount > 0)
            {
                header.dwFlags |= DDSD_MIPMAPCOUNT;
            }

            // DXT10
            if (dxt10)
            {
                // dxgiFormat
                switch (format)
                {
                    case DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_FLOAT:
                        dx10header.dxgiFormat = DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_FLOAT;
                        break;

                    case DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_FLOAT:
                        dx10header.dxgiFormat = DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_FLOAT;
                        break;

                    case DXGI_FORMAT.DXGI_FORMAT_R10G10B10A2_UNORM:
                        dx10header.dxgiFormat = DXGI_FORMAT.DXGI_FORMAT_R10G10B10A2_UNORM;
                        break;

                    case DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM:
                        dx10header.dxgiFormat = DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM;
                        break;

                    case DXGI_FORMAT.DXGI_FORMAT_R32_UINT:
                        dx10header.dxgiFormat = DXGI_FORMAT.DXGI_FORMAT_R32_UINT;
                        break;

                    case DXGI_FORMAT.DXGI_FORMAT_R8G8_UNORM:
                        dx10header.dxgiFormat = DXGI_FORMAT.DXGI_FORMAT_R8G8_UNORM;
                        break;

                    case DXGI_FORMAT.DXGI_FORMAT_R16_FLOAT:
                        dx10header.dxgiFormat = DXGI_FORMAT.DXGI_FORMAT_R16_FLOAT;
                        break;

                    case DXGI_FORMAT.DXGI_FORMAT_R8_UINT:
                        dx10header.dxgiFormat = DXGI_FORMAT.DXGI_FORMAT_R8_UINT;
                        break;

                    case DXGI_FORMAT.DXGI_FORMAT_R8_UNORM:
                        dx10header.dxgiFormat = DXGI_FORMAT.DXGI_FORMAT_R8_UNORM;
                        break;

                    case DXGI_FORMAT.DXGI_FORMAT_A8_UNORM:
                        dx10header.dxgiFormat = DXGI_FORMAT.DXGI_FORMAT_A8_UNORM;
                        break;

                    case DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM:
                        dx10header.dxgiFormat = DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM;
                        break;

                    case DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM:
                        dx10header.dxgiFormat = DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM;
                        break;

                    case DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM:
                        dx10header.dxgiFormat = DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM;
                        break;

                    case DXGI_FORMAT.DXGI_FORMAT_BC4_UNORM:
                        dx10header.dxgiFormat = DXGI_FORMAT.DXGI_FORMAT_BC4_UNORM;
                        break;

                    case DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM:
                        dx10header.dxgiFormat = DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM;
                        break;

                    case DXGI_FORMAT.DXGI_FORMAT_BC5_UNORM:
                        dx10header.dxgiFormat = DXGI_FORMAT.DXGI_FORMAT_BC5_UNORM;
                        break;

                    default:
                    {
                        throw new MissingFormatException($"Missing format: {format}");
                    }
                }
                // resourceDimension
                //if (slicecount > 0)
                //    dxt10header.resourceDimension = D3D10_RESOURCE_DIMENSION.D3D10_RESOURCE_DIMENSION_TEXTURE3D;
                //misc flag
                //if (iscubemap)
                //    dxt10header.miscFlag |= DDS_RESOURCE_MISC_TEXTURECUBE;
                //if (mipscount > 0)
                //    dxt10header.miscFlag |= D3D10_RESOURCE_MISC_GENERATE_MIPS;
                // array size
                //if (iscubemap)
                //    dxt10header.arraySize = metadata.Slicecount;
                // miscFlags2
            }

            return (header, dx10header);
        }

        private static void WriteHeader(Stream stream, DDS_HEADER header, DDS_HEADER_DXT10 dxt10header)
        {
            stream.Write(BitConverter.GetBytes(DDS_MAGIC), 0, 4);
            stream.WriteStruct(header);
            if (header.ddspf.dwFourCC == MAKEFOURCC('D', 'X', '1', '0'))
            {
                stream.WriteStruct(dxt10header);
            }
        }

        #endregion Writing

        #region Reading

        public static DDSMetadata GetMetadataFromTGAFile(string path)
        {
            var md = TexconvNative.GetMetadataFromTGAFile(path, TGA_FLAGS.TGA_FLAGS_NONE);
            var bpp = TexconvNative.BitsPerPixel(md.format);
            //var iscube = md.is_cubemap(); //TODO

            return new DDSMetadata(md, (uint)bpp, true);
        }

        public static DDSMetadata GetMetadataFromDDSFile(string path)
        {
            var md = TexconvNative.GetMetadataFromDDSFile(path, DDSFLAGS.DDS_FLAGS_NONE);
            var bpp = TexconvNative.BitsPerPixel(md.format);
            //var iscube = md.is_cubemap(); //TODO

            return new DDSMetadata(md, (uint)bpp, true);
        }

        public static unsafe bool TryGetMetadataFromDDSMemory(Span<byte> span, out DDSMetadata metadata)
        {
            try
            {
                var len = span.Length;
                //fixed (byte* ptr = span)
                {
                    var md = TexconvNative.GetMetadataFromDDSMemory(span.ToArray(), len, DDSFLAGS.DDS_FLAGS_NONE);
                    var bpp = TexconvNative.BitsPerPixel(md.format);
                    //var iscube = md.is_cubemap(); //TODO

                    metadata = new DDSMetadata(md, (uint)bpp, true);
                }

            }
            catch (Exception)
            {
                metadata = default;
                return false;
            }

            return true;
        }

        public static int ComputeRowPitch(int width, int height, DXGI_FORMAT format) => (int)TexconvNative.ComputeRowPitch((DXGI_FORMAT)format, width, height);
        public static int ComputeSlicePitch(int width, int height, DXGI_FORMAT format) => (int)TexconvNative.ComputeSlicePitch((DXGI_FORMAT)format, width, height);

        public static bool TryReadDdsHeader(Stream stream, out DDS_HEADER header)
        {
            header = default;
            if (stream.Length < 128)
            {
                return false;
            }

            if (stream.ReadStruct<int>() != DDS_MAGIC)
            {
                return false;
            }

            header = stream.ReadStruct<DDS_HEADER>();


            return true;
        }

        /// <summary>
        /// Check if a stream is a dds file. Does not advance the stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        private static bool IsDdsFile(Stream stream)
        {
            var pos = stream.Position;
            var result = TryReadDdsHeader(stream, out _);
            stream.Seek(pos, SeekOrigin.Begin);
            return result;
        }

        #endregion Reading

        /// <summary>
        /// Converts a dds stream to another texture file type and writes it to file
        /// </summary>
        /// <param name="ms">The input dds stream</param>
        /// <param name="outfilename">The output filename. Extension will be overwritten with the correct filetype</param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static bool ConvertFromDdsAndSave(Stream ms, string outfilename, ExportArgs args)
        {
            // check if stream is dds
            if (!DDSUtils.IsDdsFile(ms))
            {
                throw new ArgumentException("Input stream not a dds file", nameof(ms));
            }

            // get arguments
            var uext = EUncookExtension.dds;
            var vflip = false;
            if (args is not XbmExportArgs and not MlmaskExportArgs)
            {
                return false;

            }
            switch (args)
            {
                case XbmExportArgs xbm:
                    uext = xbm.UncookExtension;
                    vflip = xbm.Flip;
                    break;
                case MlmaskExportArgs ml:
                    uext = ml.UncookExtension;
                    break;
            }

            return uext != EUncookExtension.dds && ConvertFromDdsAndSave(ms, outfilename, uext.ToSaveFormat(), vflip);
        }

        /// <summary>
        /// Converts a dds stream to another texture file type and writes it to file
        /// </summary>
        /// <param name="ms"></param>
        /// <param name="outfilename"></param>
        /// <param name="filetype"></param>
        /// <param name="vflip"></param>
        /// <param name="hflip"></param>
        /// <returns></returns>
        private static /*unsafe*/ bool ConvertFromDdsAndSave(Stream ms, string outfilename, ESaveFileTypes filetype, bool vflip = false, bool hflip = false)
        {
            byte[] rentedBuffer = null;
            try
            {
                var offset = 0;

                var len = checked((int)ms.Length);
                rentedBuffer = ArrayPool<byte>.Shared.Rent(len);

                int readBytes;
                while (offset < len &&
                       (readBytes = ms.Read(rentedBuffer, offset, len - offset)) > 0)
                {
                    offset += readBytes;
                }

                var span = new ReadOnlySpan<byte>(rentedBuffer, 0, len);

                //fixed (byte* ptr = span)
                {
                    var outDir = new FileInfo(outfilename).Directory.FullName;
                    Directory.CreateDirectory(outDir);
                    var fileName = Path.GetFileNameWithoutExtension(outfilename);
                    var extension = filetype.ToString().ToLower();
                    var newpath = Path.Combine(outDir, $"{fileName}.{extension}");

                    TexconvNative.ConvertAndSaveDdsImage(span.ToArray(), span.Length, newpath, filetype, vflip, hflip);
                }
            }
            finally
            {
                if (rentedBuffer is object)
                {
                    ArrayPool<byte>.Shared.Return(rentedBuffer);
                }
            }

            return true;


        }

        /// <summary>
        /// Converts a dds stream to another texture file type and returns an image byte array
        /// </summary>
        public static /*unsafe*/ byte[] ConvertToDdsMemory(
            Stream ms,
            EUncookExtension filetype,
            DXGI_FORMAT? format = null,
            bool vflip = false,
            bool hflip = false)
        {
            byte[] rentedBuffer = null;
            try
            {
                var offset = 0;

                var len = checked((int)ms.Length);
                rentedBuffer = ArrayPool<byte>.Shared.Rent(len);

                int readBytes;
                while (offset < len &&
                       (readBytes = ms.Read(rentedBuffer, offset, len - offset)) > 0)
                {
                    offset += readBytes;
                }

                var span = new ReadOnlySpan<byte>(rentedBuffer, 0, len);

                //fixed (byte* ptr = span)
                {
                    var fmt = format != null
                        ? (DXGI_FORMAT)format
                        : DXGI_FORMAT.DXGI_FORMAT_UNKNOWN;


                    var buffer = TexconvNative.ConvertToDdsArray(span.ToArray(), span.Length,
                        filetype.ToSaveFormat(),
                        fmt, vflip, hflip);
                    return buffer;
                }
            }
            finally
            {
                if (rentedBuffer is object)
                {
                    ArrayPool<byte>.Shared.Return(rentedBuffer);
                }
            }
        }

        /// <summary>
        /// Converts an image stream to a dds byte array
        /// </summary>
        public static /*unsafe*/ byte[] ConvertFromDdsMemory(Stream ms, EUncookExtension filetype, bool vflip = false, bool hflip = false)
        {
            byte[] rentedBuffer = null;
            try
            {
                var offset = 0;

                var len = checked((int)ms.Length);
                rentedBuffer = ArrayPool<byte>.Shared.Rent(len);

                int readBytes;
                while (offset < len &&
                       (readBytes = ms.Read(rentedBuffer, offset, len - offset)) > 0)
                {
                    offset += readBytes;
                }

                var span = new ReadOnlySpan<byte>(rentedBuffer, 0, len);

                //fixed (byte* ptr = span)
                {
                    var buffer = TexconvNative.ConvertFromDdsArray(span.ToArray(), span.Length,
                        filetype.ToSaveFormat(),
                        vflip, hflip);
                    throw new NotImplementedException();
                    //return buffer;
                }
            }
            finally
            {
                if (rentedBuffer is object)
                {
                    ArrayPool<byte>.Shared.Return(rentedBuffer);
                }
            }
        }

        private static ESaveFileTypes ToSaveFormat(this EUncookExtension extension) =>
            extension switch
            {
                EUncookExtension.bmp => ESaveFileTypes.BMP,
                EUncookExtension.jpg => ESaveFileTypes.JPEG,
                EUncookExtension.png => ESaveFileTypes.PNG,
                EUncookExtension.tga => ESaveFileTypes.TGA,
                EUncookExtension.tiff => ESaveFileTypes.TIFF,
                _ => throw new ArgumentOutOfRangeException(nameof(extension), extension, null)
            };
    }
}
