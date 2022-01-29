#include "stdafx.h"

namespace IZEncoderNative {
	namespace Avisynth {
		// useful functions of the above
		bool AvisynthVideoInfo::HasVideo() { return (Width != 0); }
		bool AvisynthVideoInfo::HasAudio() { return (SampleRate != 0); }
		bool AvisynthVideoInfo::IsRGB() { return !!(int)(PixelType&ColorSpaces::CS_BGR); }
		bool AvisynthVideoInfo::IsRGB24() { return (PixelType&ColorSpaces::CS_BGR24) == ColorSpaces::CS_BGR24; } // Clear out additional properties
		bool AvisynthVideoInfo::IsRGB32() { return (PixelType & ColorSpaces::CS_BGR32) == ColorSpaces::CS_BGR32; }
		bool AvisynthVideoInfo::IsYUV() { return !!(int)(PixelType&ColorSpaces::CS_YUV); }
		bool AvisynthVideoInfo::IsYUY2() { return (PixelType & ColorSpaces::CS_YUY2) == ColorSpaces::CS_YUY2; }
		bool AvisynthVideoInfo::IsYV12() { return ((PixelType & ColorSpaces::CS_YV12) == ColorSpaces::CS_YV12) || ((PixelType & ColorSpaces::CS_I420) == ColorSpaces::CS_I420); }
		bool AvisynthVideoInfo::IsColorSpace(ColorSpaces c_space) { return ((PixelType & c_space) == c_space); }
		bool AvisynthVideoInfo::Is(ColorSpaces property) { return ((PixelType & property) == property); }
		bool AvisynthVideoInfo::IsPlanar() { return !!(int)(PixelType & ColorSpaces::CS_PLANAR); }
		bool AvisynthVideoInfo::IsFieldBased() { return !!(int)(ImageType & FrameType::IT_FIELDBASED); }
		bool AvisynthVideoInfo::IsParityKnown() { return (((int)(ImageType & FrameType::IT_FIELDBASED)) && ((int)(ImageType & (FrameType::IT_BFF | FrameType::IT_TFF)))); }
		bool AvisynthVideoInfo::IsBFF() { return !!(int)(ImageType & FrameType::IT_BFF); }
		bool AvisynthVideoInfo::IsTFF() { return !!(int)(ImageType & FrameType::IT_TFF); }

		bool AvisynthVideoInfo::IsVPlaneFirst() { return ((PixelType & ColorSpaces::CS_YV12) == ColorSpaces::CS_YV12); }  // Don't use this
		int AvisynthVideoInfo::BytesFromPixels(int pixels) { return pixels * (BitsPerPixel() >> 3); }   // Will not work on planar images, but will return only luma planes
		int AvisynthVideoInfo::RowSize() { return BytesFromPixels(Width); }  // Also only returns first plane on planar images
		int AvisynthVideoInfo::BMPSize() { if (IsPlanar()) { int p = Height * ((RowSize() + 3) & ~3); p += p >> 1; return p; } return Height * ((RowSize() + 3) & ~3); }
		__int64 AvisynthVideoInfo::AudioSamplesFromFrames(__int64 frames) { return (Numerator && HasVideo()) ? ((__int64)(frames)* SampleRate * Denominator / Numerator) : 0; }
		int AvisynthVideoInfo::FramesFromAudioSamples(__int64 samples) { return (Denominator && HasAudio()) ? (int)((samples * (__int64)Numerator) / ((__int64)Denominator * (__int64)SampleRate)) : 0; }
		__int64 AvisynthVideoInfo::AudioSamplesFromBytes(__int64 bytes) { return HasAudio() ? bytes / BytesPerAudioSample() : 0; }
		__int64 AvisynthVideoInfo::BytesFromAudioSamples(__int64 samples) { return samples * BytesPerAudioSample(); }
		int AvisynthVideoInfo::AudioChannels() { return HasAudio() ? Channels : 0; }
		//int AvisynthVideoInfo::SampleType() { return SampleType;}
		bool AvisynthVideoInfo::IsSampleType(AudioSampleType testtype) { return !!(int)(SampleType&testtype); }
		int AvisynthVideoInfo::SamplesPerSecond() { return SampleRate; }
		int AvisynthVideoInfo::BytesPerAudioSample() { return Channels * BytesPerChannelSample(); }

		int AvisynthVideoInfo::BitsPerPixel() {
			switch (PixelType) {
			case ColorSpaces::CS_BGR24:
				return 24;
			case ColorSpaces::CS_BGR32:
				return 32;
			case ColorSpaces::CS_YUY2:
				return 16;
			case ColorSpaces::CS_YV12:
			case ColorSpaces::CS_I420:
				return 12;
			default:
				return 0;
			}
		}

		int AvisynthVideoInfo::BytesPerChannelSample() {
			switch (SampleType) {
			case AudioSampleType::SAMPLE_INT8:
				return sizeof(signed char);
			case AudioSampleType::SAMPLE_INT16:
				return sizeof(signed short);
			case AudioSampleType::SAMPLE_INT24:
				return 3;
			case AudioSampleType::SAMPLE_INT32:
				return sizeof(signed int);
			case AudioSampleType::SAMPLE_FLOAT:
				return sizeof(SFLOAT);
			default:
				_ASSERTE("Sample type not recognized!");
				return 0;
			}
		}

		// Test for same colorspace
		bool AvisynthVideoInfo::IsSameColorspace(AvisynthVideoInfo% vi) {
			if (vi.PixelType == PixelType) return TRUE;
			if (IsYV12() && vi.IsYV12()) return TRUE;
			return FALSE;
		}

		double AvisynthVideoInfo::FrameRate()
		{
			return Numerator / (double)Denominator;
		}

		AvisynthVideoInfo AvisynthVideoInfo::FromNative(const VideoInfo* vi) {
			return (AvisynthVideoInfo)Marshal::PtrToStructure(IntPtr((void*)vi), AvisynthVideoInfo::typeid);
		}
	}
}
