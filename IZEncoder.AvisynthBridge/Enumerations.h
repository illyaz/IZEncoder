//Enumerations from avisynth.h
namespace IZEncoderNative {
	namespace Avisynth {
		public enum class PlanarChromaAlignmentMode {
			PlanarChromaAlignmentOff,
			PlanarChromaAlignmentOn,
			PlanarChromaAlignmentTest
		};

		public enum class AudioSampleType {
			SAMPLE_INT8 = 1 << 0,
			SAMPLE_INT16 = 1 << 1,
			SAMPLE_INT24 = 1 << 2,    // Int24 is a very stupid thing to code, but it's supported by some hardware.
			SAMPLE_INT32 = 1 << 3,
			SAMPLE_FLOAT = 1 << 4
		};

		public enum class YUVPlanes {
			PLANAR_Y = 1 << 0,
			PLANAR_U = 1 << 1,
			PLANAR_V = 1 << 2,
			PLANAR_ALIGNED = 1 << 3,
			PLANAR_Y_ALIGNED = PLANAR_Y | PLANAR_ALIGNED,
			PLANAR_U_ALIGNED = PLANAR_U | PLANAR_ALIGNED,
			PLANAR_V_ALIGNED = PLANAR_V | PLANAR_ALIGNED,
		};

		// Colorspace properties.
		public enum class ColorSpaces {
			CS_YUVA = 1 << 27,
			CS_BGR = 1 << 28,
			CS_YUV = 1 << 29,
			CS_INTERLEAVED = 1 << 30,
			CS_PLANAR = 1 << 31,

			CS_Shift_Sub_Width = 0,
			CS_Shift_Sub_Height = 8,
			CS_Shift_Sample_Bits = 16,

			CS_Sub_Width_Mask = 7 << CS_Shift_Sub_Width,
			CS_Sub_Width_1 = 3 << CS_Shift_Sub_Width, // YV24
			CS_Sub_Width_2 = 0 << CS_Shift_Sub_Width, // YV12, I420, YV16
			CS_Sub_Width_4 = 1 << CS_Shift_Sub_Width, // YUV9, YV411

			CS_VPlaneFirst = 1 << 3, // YV12, YV16, YV24, YV411, YUV9
			CS_UPlaneFirst = 1 << 4, // I420

			CS_Sub_Height_Mask = 7 << CS_Shift_Sub_Height,
			CS_Sub_Height_1 = 3 << CS_Shift_Sub_Height, // YV16, YV24, YV411
			CS_Sub_Height_2 = 0 << CS_Shift_Sub_Height, // YV12, I420
			CS_Sub_Height_4 = 1 << CS_Shift_Sub_Height, // YUV9

			CS_Sample_Bits_Mask = 7 << CS_Shift_Sample_Bits,
			CS_Sample_Bits_8 = 0 << CS_Shift_Sample_Bits,
			CS_Sample_Bits_10 = 5 << CS_Shift_Sample_Bits,
			CS_Sample_Bits_12 = 6 << CS_Shift_Sample_Bits,
			CS_Sample_Bits_14 = 7 << CS_Shift_Sample_Bits,
			CS_Sample_Bits_16 = 1 << CS_Shift_Sample_Bits,
			CS_Sample_Bits_32 = 2 << CS_Shift_Sample_Bits,

			CS_PLANAR_MASK = CS_PLANAR | CS_INTERLEAVED | CS_YUV | CS_BGR | CS_YUVA | CS_Sample_Bits_Mask
			| CS_Sub_Height_Mask | CS_Sub_Width_Mask,
			CS_PLANAR_FILTER = ~(CS_VPlaneFirst | CS_UPlaneFirst),

			CS_RGB_TYPE = 1 << 0,
			CS_RGBA_TYPE = 1 << 1,

			// Specific colorformats
			CS_UNKNOWN = 0,

			CS_BGR24 = CS_RGB_TYPE | CS_BGR | CS_INTERLEAVED,
			CS_BGR32 = CS_RGBA_TYPE | CS_BGR | CS_INTERLEAVED,
			CS_YUY2 = 1 << 2 | CS_YUV | CS_INTERLEAVED,
			//  CS_YV12  = 1<<3  Reserved
			//  CS_I420  = 1<<4  Reserved
			CS_RAW32 = 1 << 5 | CS_INTERLEAVED,

			//  YV12 must be 0xA000008 2.5 Baked API will see all new planar as YV12
			//  I420 must be 0xA000010

			CS_GENERIC_YUV420 = CS_PLANAR | CS_YUV | CS_VPlaneFirst | CS_Sub_Height_2 | CS_Sub_Width_2,  // 4:2:0 planar
			CS_GENERIC_YUV422 = CS_PLANAR | CS_YUV | CS_VPlaneFirst | CS_Sub_Height_1 | CS_Sub_Width_2,  // 4:2:2 planar
			CS_GENERIC_YUV444 = CS_PLANAR | CS_YUV | CS_VPlaneFirst | CS_Sub_Height_1 | CS_Sub_Width_1,  // 4:4:4 planar
			CS_GENERIC_Y = CS_PLANAR | CS_INTERLEAVED | CS_YUV,                                     // Y only (4:0:0)
			CS_GENERIC_RGBP = CS_PLANAR | CS_BGR | CS_RGB_TYPE,                                        // planar RGB. Though name is RGB but plane order G,B,R
			CS_GENERIC_RGBAP = CS_PLANAR | CS_BGR | CS_RGBA_TYPE,                                       // planar RGBA
			CS_GENERIC_YUVA420 = CS_PLANAR | CS_YUVA | CS_VPlaneFirst | CS_Sub_Height_2 | CS_Sub_Width_2, // 4:2:0:A planar
			CS_GENERIC_YUVA422 = CS_PLANAR | CS_YUVA | CS_VPlaneFirst | CS_Sub_Height_1 | CS_Sub_Width_2, // 4:2:2:A planar
			CS_GENERIC_YUVA444 = CS_PLANAR | CS_YUVA | CS_VPlaneFirst | CS_Sub_Height_1 | CS_Sub_Width_1, // 4:4:4:A planar

			CS_YV24 = CS_GENERIC_YUV444 | CS_Sample_Bits_8,  // YVU 4:4:4 planar
			CS_YV16 = CS_GENERIC_YUV422 | CS_Sample_Bits_8,  // YVU 4:2:2 planar
			CS_YV12 = CS_GENERIC_YUV420 | CS_Sample_Bits_8,  // YVU 4:2:0 planar
			CS_I420 = CS_PLANAR | CS_YUV | CS_Sample_Bits_8 | CS_UPlaneFirst | CS_Sub_Height_2 | CS_Sub_Width_2,  // YUV 4:2:0 planar
			CS_IYUV = CS_I420,
			CS_YUV9 = CS_PLANAR | CS_YUV | CS_Sample_Bits_8 | CS_VPlaneFirst | CS_Sub_Height_4 | CS_Sub_Width_4,  // YUV 4:1:0 planar
			CS_YV411 = CS_PLANAR | CS_YUV | CS_Sample_Bits_8 | CS_VPlaneFirst | CS_Sub_Height_1 | CS_Sub_Width_4,  // YUV 4:1:1 planar

			CS_Y8 = CS_GENERIC_Y | CS_Sample_Bits_8,                                                            // Y   4:0:0 planar

																												//-------------------------
																												// AVS16: new planar constants go live! Experimental PF 160613
																												// 10-12-14 bit + planar RGB + BRG48/64 160725

			CS_YUV444P10 = CS_GENERIC_YUV444 | CS_Sample_Bits_10, // YUV 4:4:4 10bit samples
			CS_YUV422P10 = CS_GENERIC_YUV422 | CS_Sample_Bits_10, // YUV 4:2:2 10bit samples
			CS_YUV420P10 = CS_GENERIC_YUV420 | CS_Sample_Bits_10, // YUV 4:2:0 10bit samples
			CS_Y10 = CS_GENERIC_Y | CS_Sample_Bits_10,            // Y   4:0:0 10bit samples

			CS_YUV444P12 = CS_GENERIC_YUV444 | CS_Sample_Bits_12, // YUV 4:4:4 12bit samples
			CS_YUV422P12 = CS_GENERIC_YUV422 | CS_Sample_Bits_12, // YUV 4:2:2 12bit samples
			CS_YUV420P12 = CS_GENERIC_YUV420 | CS_Sample_Bits_12, // YUV 4:2:0 12bit samples
			CS_Y12 = CS_GENERIC_Y | CS_Sample_Bits_12,            // Y   4:0:0 12bit samples

			CS_YUV444P14 = CS_GENERIC_YUV444 | CS_Sample_Bits_14, // YUV 4:4:4 14bit samples
			CS_YUV422P14 = CS_GENERIC_YUV422 | CS_Sample_Bits_14, // YUV 4:2:2 14bit samples
			CS_YUV420P14 = CS_GENERIC_YUV420 | CS_Sample_Bits_14, // YUV 4:2:0 14bit samples
			CS_Y14 = CS_GENERIC_Y | CS_Sample_Bits_14,            // Y   4:0:0 14bit samples

			CS_YUV444P16 = CS_GENERIC_YUV444 | CS_Sample_Bits_16, // YUV 4:4:4 16bit samples
			CS_YUV422P16 = CS_GENERIC_YUV422 | CS_Sample_Bits_16, // YUV 4:2:2 16bit samples
			CS_YUV420P16 = CS_GENERIC_YUV420 | CS_Sample_Bits_16, // YUV 4:2:0 16bit samples
			CS_Y16 = CS_GENERIC_Y | CS_Sample_Bits_16,            // Y   4:0:0 16bit samples

																  // 32 bit samples (float)
			CS_YUV444PS = CS_GENERIC_YUV444 | CS_Sample_Bits_32,  // YUV 4:4:4 32bit samples
			CS_YUV422PS = CS_GENERIC_YUV422 | CS_Sample_Bits_32,  // YUV 4:2:2 32bit samples
			CS_YUV420PS = CS_GENERIC_YUV420 | CS_Sample_Bits_32,  // YUV 4:2:0 32bit samples
			CS_Y32 = CS_GENERIC_Y | CS_Sample_Bits_32,            // Y   4:0:0 32bit samples

																  // RGB packed
			CS_BGR48 = CS_RGB_TYPE | CS_BGR | CS_INTERLEAVED | CS_Sample_Bits_16, // BGR 3x16 bit
			CS_BGR64 = CS_RGBA_TYPE | CS_BGR | CS_INTERLEAVED | CS_Sample_Bits_16, // BGR 4x16 bit
																				   // no packed 32 bit (float) support for these legacy types

																				   // RGB planar
			CS_RGBP = CS_GENERIC_RGBP | CS_Sample_Bits_8,  // Planar RGB 8 bit samples
			CS_RGBP10 = CS_GENERIC_RGBP | CS_Sample_Bits_10, // Planar RGB 10bit samples
			CS_RGBP12 = CS_GENERIC_RGBP | CS_Sample_Bits_12, // Planar RGB 12bit samples
			CS_RGBP14 = CS_GENERIC_RGBP | CS_Sample_Bits_14, // Planar RGB 14bit samples
			CS_RGBP16 = CS_GENERIC_RGBP | CS_Sample_Bits_16, // Planar RGB 16bit samples
			CS_RGBPS = CS_GENERIC_RGBP | CS_Sample_Bits_32, // Planar RGB 32bit samples

															// RGBA planar
			CS_RGBAP = CS_GENERIC_RGBAP | CS_Sample_Bits_8,  // Planar RGBA 8 bit samples
			CS_RGBAP10 = CS_GENERIC_RGBAP | CS_Sample_Bits_10, // Planar RGBA 10bit samples
			CS_RGBAP12 = CS_GENERIC_RGBAP | CS_Sample_Bits_12, // Planar RGBA 12bit samples
			CS_RGBAP14 = CS_GENERIC_RGBAP | CS_Sample_Bits_14, // Planar RGBA 14bit samples
			CS_RGBAP16 = CS_GENERIC_RGBAP | CS_Sample_Bits_16, // Planar RGBA 16bit samples
			CS_RGBAPS = CS_GENERIC_RGBAP | CS_Sample_Bits_32, // Planar RGBA 32bit samples

															  // Planar YUVA
			CS_YUVA444 = CS_GENERIC_YUVA444 | CS_Sample_Bits_8,  // YUVA 4:4:4 8bit samples
			CS_YUVA422 = CS_GENERIC_YUVA422 | CS_Sample_Bits_8,  // YUVA 4:2:2 8bit samples
			CS_YUVA420 = CS_GENERIC_YUVA420 | CS_Sample_Bits_8,  // YUVA 4:2:0 8bit samples

			CS_YUVA444P10 = CS_GENERIC_YUVA444 | CS_Sample_Bits_10, // YUVA 4:4:4 10bit samples
			CS_YUVA422P10 = CS_GENERIC_YUVA422 | CS_Sample_Bits_10, // YUVA 4:2:2 10bit samples
			CS_YUVA420P10 = CS_GENERIC_YUVA420 | CS_Sample_Bits_10, // YUVA 4:2:0 10bit samples

			CS_YUVA444P12 = CS_GENERIC_YUVA444 | CS_Sample_Bits_12, // YUVA 4:4:4 12bit samples
			CS_YUVA422P12 = CS_GENERIC_YUVA422 | CS_Sample_Bits_12, // YUVA 4:2:2 12bit samples
			CS_YUVA420P12 = CS_GENERIC_YUVA420 | CS_Sample_Bits_12, // YUVA 4:2:0 12bit samples

			CS_YUVA444P14 = CS_GENERIC_YUVA444 | CS_Sample_Bits_14, // YUVA 4:4:4 14bit samples
			CS_YUVA422P14 = CS_GENERIC_YUVA422 | CS_Sample_Bits_14, // YUVA 4:2:2 14bit samples
			CS_YUVA420P14 = CS_GENERIC_YUVA420 | CS_Sample_Bits_14, // YUVA 4:2:0 14bit samples

			CS_YUVA444P16 = CS_GENERIC_YUVA444 | CS_Sample_Bits_16, // YUVA 4:4:4 16bit samples
			CS_YUVA422P16 = CS_GENERIC_YUVA422 | CS_Sample_Bits_16, // YUVA 4:2:2 16bit samples
			CS_YUVA420P16 = CS_GENERIC_YUVA420 | CS_Sample_Bits_16, // YUVA 4:2:0 16bit samples

			CS_YUVA444PS = CS_GENERIC_YUVA444 | CS_Sample_Bits_32,  // YUVA 4:4:4 32bit samples
			CS_YUVA422PS = CS_GENERIC_YUVA422 | CS_Sample_Bits_32,  // YUVA 4:2:2 32bit samples
			CS_YUVA420PS = CS_GENERIC_YUVA420 | CS_Sample_Bits_32,  // YUVA 4:2:0 32bit samples
		};

		public enum class FrameType {
			IT_BFF = 1 << 0,
			IT_TFF = 1 << 1,
			IT_FIELDBASED = 1 << 2
		};

		public enum class CacheType {
			CACHE_NOTHING = 0,
			CACHE_RANGE = 1,
			CACHE_ALL = 2,
			CACHE_AUDIO = 3,
			CACHE_AUDIO_NONE = 4,
			CACHE_AUDIO_AUTO = 5
		};

		[System::Flags]
		public enum class CPUFlags {
            /* oldest CPU to support extension */
			CPUF_FORCE        =  0x01,   //  N/A
			CPUF_FPU          =  0x02,   //  386/486DX
			CPUF_MMX          =  0x04,   //  P55C, K6, PII
			CPUF_INTEGER_SSE  =  0x08,   //  PIII, Athlon
			CPUF_SSE          =  0x10,   //  PIII, Athlon XP/MP
			CPUF_SSE2         =  0x20,   //  PIV, K8
			CPUF_3DNOW        =  0x40,   //  K6-2
			CPUF_3DNOW_EXT    =  0x80,   //  Athlon
			CPUF_X86_64       =  0xA0,   //  Hammer (note: equiv. to 3DNow + SSE2, which
									     //          only Hammer will have anyway)
			CPUF_SSE3         = 0x100,   //  PIV+, K8 Venice
			CPUF_SSSE3        = 0x200,   //  Core 2
			CPUF_SSE4         = 0x400,
			CPUF_SSE4_1       = 0x400,   //  Penryn, Wolfdale, Yorkfield  
			CPUF_AVX          = 0x800,   //  Sandy Bridge, Bulldozer
			CPUF_SSE4_2       = 0x1000,  //  Nehalem
			// AVS+
			CPUF_AVX2         = 0x2000,   //  Haswell
			CPUF_FMA3         = 0x4000,
			CPUF_F16C         = 0x8000,
			CPUF_MOVBE        = 0x10000,  // Big Endian move
			CPUF_POPCNT       = 0x20000,
			CPUF_AES          = 0x40000,
			CPUF_FMA4         = 0x80000,

			CPUF_AVX512F      = 0x100000,  // AVX-512 Foundation.
			CPUF_AVX512DQ     = 0x200000,  // AVX-512 DQ (Double/Quad granular) Instructions
			CPUF_AVX512PF     = 0x400000,  // AVX-512 Prefetch
			CPUF_AVX512ER     = 0x800000,  // AVX-512 Exponential and Reciprocal
			CPUF_AVX512CD     = 0x1000000, // AVX-512 Conflict Detection
			CPUF_AVX512BW     = 0x2000000, // AVX-512 BW (Byte/Word granular) Instructions
			CPUF_AVX512VL     = 0x4000000, // AVX-512 VL (128/256 Vector Length) Extensions
			CPUF_AVX512IFMA   = 0x8000000, // AVX-512 IFMA integer 52 bit
			CPUF_AVX512VBMI   = 0x10000000,// AVX-512 VBMI
		};

		/*/// <summary>The multi-threading mode for AviSynth+.</summary>
		public enum class MtMode {
			/// <summary>MT mode won't be configured.</summary>
			UNKNOWN = 0,
			/// <summary>A single instance of the filter will be created and GetFrame will be called in parallel from various threads.</summary>
			NICE_FILTER = 1,
			/// <summary>An instance of the filter will be created for each thread and each instance will receive a single GetFrame request at once.</summary>
			MULTI_INSTANCE = 2,
			/// <summary>A single instance of the filter will be created and it will receive a single GetFrame request at once. Useful for source filters.</summary>
			SERIALIZED = 3,
		};*/

		public enum class AvisynthProperty {
			PHYSICAL_CPUS = 1,
			LOGICAL_CPUS = 2,
			THREADPOOL_THREADS = 3,
			FILTERCHAIN_THREADS = 4,
			THREAD_ID = 5,
			VERSION = 6
		};
	}
}
