#ifndef AVISYNTHVIDEOINFO_H
#define AVISYNTHVIDEOINFO_H

using namespace System::Runtime::InteropServices;

namespace IZEncoderNative {
	namespace Avisynth {
		[StructLayout(LayoutKind::Sequential)]
		public value struct AvisynthVideoInfo {
		public:
			property int Width;
			property int Height;    // width=0 means no video
			property unsigned Numerator;
			property unsigned Denominator;
			property int Frames;
			// This is more extensible than previous versions. More properties can be added seeminglesly.

			property ColorSpaces PixelType;                // changed to int as of 2.5

			property int SampleRate;   // 0 means no audio
			property AudioSampleType SampleType;                // as of 2.5
			property __int64 Samples;      // changed as of 2.5
			property int Channels;                  // as of 2.5

											// Imagetype properties

			property FrameType ImageType;

			// useful functions of the above
			bool HasVideo();
			bool HasAudio();
			bool IsRGB();
			bool IsRGB24(); // Clear out additional properties
			bool IsRGB32();
			bool IsYUV();
			bool IsYUY2();
			bool IsYV12();
			bool IsColorSpace(ColorSpaces c_space);
			bool Is(ColorSpaces property);
			bool IsPlanar();
			bool IsFieldBased();
			bool IsParityKnown();
			bool IsBFF();
			bool IsTFF();

			bool IsVPlaneFirst();  // Don't use this
			int BytesFromPixels(int pixels);   // Will not work on planar images, but will return only luma planes
			int RowSize();  // Also only returns first plane on planar images
			int BMPSize();
			__int64 AudioSamplesFromFrames(__int64 frames);
			int FramesFromAudioSamples(__int64 samples);
			__int64 AudioSamplesFromBytes(__int64 bytes);
			__int64 BytesFromAudioSamples(__int64 samples);
			int AudioChannels();
			//int SampleType();
			bool IsSampleType(AudioSampleType testtype);
			int SamplesPerSecond();
			int BytesPerAudioSample();

			int BitsPerPixel();

			int BytesPerChannelSample();


			// Test for same colorspace
			bool IsSameColorspace(AvisynthVideoInfo% vi);

			double FrameRate();

		internal:
			static AvisynthVideoInfo FromNative(const VideoInfo* nvi);
		};
	}
}
#endif // !AVISYNTHVIDEOINFO_H
