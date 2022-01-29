namespace IZEncoder.Launcher.Common.Helper
{
    using System;
    using System.Runtime.InteropServices;

    public static class NativeAvs
    {
        public const string library = "avisynth";
        /////////////////////////////////////////////////////////////////////
        //
        // AVS_VideoInfo
        //

        public static unsafe NativeBool has_video(AVS_VideoInfo* p)
        {
            return p->width != 0;
        }

        public static unsafe NativeBool has_audio(AVS_VideoInfo* p)
        {
            return p->audio_samples_per_second != 0;
        }

        public static unsafe NativeBool is_rgb(AVS_VideoInfo* p)
        {
            return (p->pixel_type & AVS_CS_BGR) == AVS_CS_BGR;
        }

        public static unsafe NativeBool is_rgb24(AVS_VideoInfo* p)
        {
            return (p->pixel_type & AVS_CS_BGR24) == AVS_CS_BGR24;
        } // Clear out additional properties

        public static unsafe NativeBool is_rgb32(AVS_VideoInfo* p)
        {
            return (p->pixel_type & AVS_CS_BGR32) == AVS_CS_BGR32;
        }

        public static unsafe NativeBool is_yuv(AVS_VideoInfo* p)
        {
            return (p->pixel_type & AVS_CS_YUV) == AVS_CS_YUV;
        }

        public static unsafe NativeBool is_yuy2(AVS_VideoInfo* p)
        {
            return (p->pixel_type & AVS_CS_YUY2) == AVS_CS_YUY2;
        }

        [DllImport(library, EntryPoint = "avs_is_yv24", CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe NativeBool is_yv24(AVS_VideoInfo* p);

        [DllImport(library, EntryPoint = "avs_is_yv16", CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe NativeBool is_yv16(AVS_VideoInfo* p);

        [DllImport(library, EntryPoint = "avs_is_yv12", CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe NativeBool is_yv12(AVS_VideoInfo* p);

        [DllImport(library, EntryPoint = "avs_is_yv411", CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe NativeBool is_yv411(AVS_VideoInfo* p);

        [DllImport(library, EntryPoint = "avs_is_y8", CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe NativeBool is_y8(AVS_VideoInfo* p);

        public static unsafe NativeBool is_property(AVS_VideoInfo* p, int property)
        {
            return (p->image_type & property) == property;
        }

        public static unsafe NativeBool is_planar(AVS_VideoInfo* p)
        {
            return (p->pixel_type & AVS_CS_PLANAR) == AVS_CS_PLANAR;
        }

        [DllImport(library, EntryPoint = "avs_is_color_space", CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe NativeBool is_color_space(AVS_VideoInfo* p, int c_space);

        public static unsafe NativeBool is_field_based(AVS_VideoInfo* p)
        {
            return (p->image_type & AVS_IT_FIELDBASED) == AVS_IT_FIELDBASED;
        }

        public static unsafe NativeBool is_parity_known(AVS_VideoInfo* p)
        {
            return (p->image_type & AVS_IT_FIELDBASED) == AVS_IT_FIELDBASED &&
                   ((p->image_type & AVS_IT_BFF) == AVS_IT_BFF) | ((p->image_type & AVS_IT_TFF) == AVS_IT_TFF);
        }

        public static unsafe NativeBool is_bff(AVS_VideoInfo* p)
        {
            return (p->image_type & AVS_IT_BFF) == AVS_IT_BFF;
        }

        public static unsafe NativeBool is_tff(AVS_VideoInfo* p)
        {
            return (p->image_type & AVS_IT_TFF) == AVS_IT_TFF;
        }

        [DllImport(library, EntryPoint = "avs_get_plane_width_subsampling",
            CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe int get_plane_width_subsampling(AVS_VideoInfo* p, int plane);

        [DllImport(library, EntryPoint = "avs_get_plane_height_subsampling",
            CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe int get_plane_height_subsampling(AVS_VideoInfo* p, int plane);

        [DllImport(library, EntryPoint = "avs_bits_per_pixel", CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe int bits_per_pixel(AVS_VideoInfo* p);

        [DllImport(library, EntryPoint = "avs_bytes_from_pixels", CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe int bytes_from_pixels(AVS_VideoInfo* p, int plane = 0);

        [DllImport(library, EntryPoint = "avs_row_size", CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe int row_size(AVS_VideoInfo* p, int plane = 0);

        [DllImport(library, EntryPoint = "avs_bmp_size", CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe int bmp_size(AVS_VideoInfo* vi);

        public static unsafe int samples_per_second(AVS_VideoInfo* p)
        {
            return p->audio_samples_per_second;
        }

        public static unsafe int bytes_per_channel_sample(AVS_VideoInfo* p)
        {
            switch (p->sample_type)
            {
                case AVS_SAMPLE_INT8:  return sizeof(char);
                case AVS_SAMPLE_INT16: return sizeof(short);
                case AVS_SAMPLE_INT24: return 3;
                case AVS_SAMPLE_INT32: return sizeof(int);
                case AVS_SAMPLE_FLOAT: return sizeof(float);
                default:               return 0;
            }
        }

        public static unsafe int bytes_per_audio_sample(AVS_VideoInfo* p)
        {
            return p->nchannels * bytes_per_channel_sample(p);
        }

        public static unsafe long audio_samples_from_frames(AVS_VideoInfo* p, long frames)
        {
            return frames * p->audio_samples_per_second * p->fps_denominator / p->fps_numerator;
        }

        public static unsafe int frames_from_audio_samples(AVS_VideoInfo* p, long samples)
        {
            return (int) (samples * p->fps_numerator / p->fps_denominator / p->audio_samples_per_second);
        }

        public static unsafe long audio_samples_from_bytes(AVS_VideoInfo* p, long bytes)
        {
            return bytes / bytes_per_audio_sample(p);
        }

        public static unsafe long bytes_from_audio_samples(AVS_VideoInfo* p, long samples)
        {
            return samples * bytes_per_audio_sample(p);
        }

        public static unsafe int audio_channels(AVS_VideoInfo* p)
        {
            return p->nchannels;
        }

        public static unsafe int sample_type(AVS_VideoInfo* p)
        {
            return p->sample_type;
        }

        // useful mutator
        public static unsafe void set_property(AVS_VideoInfo* p, int property)
        {
            p->image_type |= property;
        }

        public static unsafe void clear_property(AVS_VideoInfo* p, int property)
        {
            p->image_type &= ~property;
        }

        public static unsafe void set_field_based(AVS_VideoInfo* p, bool isfieldbased)
        {
            if (isfieldbased) p->image_type |= AVS_IT_FIELDBASED;
            else p->image_type &= ~AVS_IT_FIELDBASED;
        }

        public static unsafe void set_fps(AVS_VideoInfo* p, int numerator, int denominator)
        {
            int x = numerator,
                y = denominator;

            while ((NativeBool) y)
            {
                // find gcd
                var t = x % y;
                x = y;
                y = t;
            }

            p->fps_numerator = numerator / x;
            p->fps_denominator = denominator / x;
        }

        public static unsafe NativeBool is_same_colorspace(AVS_VideoInfo* x, AVS_VideoInfo* y)
        {
            return x->pixel_type == y->pixel_type || is_yv12(x) && is_yv12(y);
        }

        /////////////////////////////////////////////////////////////////////
        //
        // AVS_VideoFrame
        //

        [DllImport(library, EntryPoint = "avs_get_pitch_p", CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe int get_pitch_p(AVS_VideoFrame* p, int plane);

        public static unsafe int get_pitch(AVS_VideoFrame* p)
        {
            return get_pitch_p(p, 0);
        }


        [DllImport(library, EntryPoint = "avs_get_row_size_p", CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe int get_row_size_p(AVS_VideoFrame* p, int plane);

        public static unsafe int get_row_size(AVS_VideoFrame* p)
        {
            return p->row_size;
        }

        [DllImport(library, EntryPoint = "avs_get_height_p", CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe int get_height_p(AVS_VideoFrame* p, int plane);

        public static unsafe int get_height(AVS_VideoFrame* p)
        {
            return p->height;
        }

        [DllImport(library, EntryPoint = "avs_get_read_ptr_p", CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe IntPtr get_read_ptr_p(AVS_VideoFrame* p, int plane);

        public static unsafe IntPtr get_read_ptr(AVS_VideoFrame* p)
        {
            return get_read_ptr_p(p, 0);
        }

        [DllImport(library, EntryPoint = "avs_is_writable", CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe NativeBool is_writable(AVS_VideoFrame* p);

        [DllImport(library, EntryPoint = "avs_get_write_ptr_p", CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe IntPtr get_write_ptr_p(AVS_VideoFrame* p, int plane);

        public static unsafe IntPtr get_write_ptr(AVS_VideoFrame* p)
        {
            return get_write_ptr_p(p, 0);
        }

        [DllImport(library, EntryPoint = "avs_release_video_frame", CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe void release_video_frame(AVS_VideoFrame* p);

        [DllImport(library, EntryPoint = "avs_copy_video_frame", CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe AVS_VideoFrame* copy_video_frame(AVS_VideoFrame* p);

        public static unsafe void release_frame(AVS_VideoFrame* f)
        {
            release_video_frame(f);
        }

        public static unsafe AVS_VideoFrame* copy_frame(AVS_VideoFrame* f)
        {
            return copy_video_frame(f);
        }


        /////////////////////////////////////////////////////////////////////
        //
        // AVS_Value
        //

        [DllImport(library, EntryPoint = "avs_copy_value", CallingConvention = CallingConvention.StdCall)]
        public static extern void copy_value(ref AVS_Value dest, AVS_Value src);

        [DllImport(library, EntryPoint = "avs_release_value", CallingConvention = CallingConvention.StdCall)]
        public static extern void release_value(AVS_Value v0);

        public static NativeBool defined(AVS_Value v)
        {
            return v.type != 'v';
        }

        public static NativeBool is_clip(AVS_Value v)
        {
            return v.type == 'c';
        }

        public static NativeBool is_bool(AVS_Value v)
        {
            return v.type == 'b';
        }

        public static NativeBool is_int(AVS_Value v)
        {
            return v.type == 'i';
        }

        public static NativeBool is_float(AVS_Value v)
        {
            return v.type == 'f' || v.type == 'i';
        }

        public static NativeBool is_string(AVS_Value v)
        {
            return v.type == 's';
        }

        public static NativeBool is_array(AVS_Value v)
        {
            return v.type == 'a';
        }

        public static NativeBool is_error(AVS_Value v)
        {
            return v.type == 'e';
        }

        [DllImport(library, EntryPoint = "avs_take_clip", CallingConvention = CallingConvention.StdCall)]
        public static extern AVS_Clip take_clip(AVS_Value v0, AVS_ScriptEnvironment v1);

        [DllImport(library, EntryPoint = "avs_set_to_clip", CallingConvention = CallingConvention.StdCall)]
        public static extern void set_to_clip(ref AVS_Value v0, AVS_Clip v1);

        public static NativeBool as_bool(AVS_Value v)
        {
            return v.d.boolean;
        }

        public static int as_int(AVS_Value v)
        {
            return v.d.integer;
        }

        public static NativeString as_string(AVS_Value v)
        {
            return is_error(v) || is_string(v) ? v.d.str : null;
        }

        public static double as_float(AVS_Value v)
        {
            return is_int(v) ? v.d.integer : v.d.floating_pt;
        }

        public static NativeString as_error(AVS_Value v)
        {
            return is_error(v) ? v.d.str : null;
        }

        public static AVS_Value[] as_array(AVS_Value v)
        {
            var a = new AVS_Value[array_size(v)];
            for (var i = 0; i < a.Length; i++)
                a[i] = array_elt(v, i);
            return a;
        }

        public static int array_size(AVS_Value v)
        {
            return is_array(v) ? v.array_size : 1;
        }

        public static unsafe AVS_Value array_elt(AVS_Value v, int index)
        {
            return is_array(v) ? ((AVS_Value*) v.d.array)[index] : v;
        }

        // only use these functions on an AVS_Value that does not already have
        // an active value.  Remember, treat AVS_Value as a fat pointer.
        public static AVS_Value new_value_bool(bool v0)
        {
            var v = new AVS_Value();
            v.type = 'b';
            v.d.boolean = v0;
            return v;
        }

        public static AVS_Value new_value_int(int v0)
        {
            var v = new AVS_Value();
            v.type = 'i';
            v.d.integer = v0;
            return v;
        }

        public static AVS_Value new_value_string(NativeString v0)
        {
            var v = new AVS_Value();
            v.type = 's';
            v.d.str = v0;
            return v;
        }

        public static AVS_Value new_value_float(float v0)
        {
            var v = new AVS_Value();
            v.type = 'f';
            v.d.floating_pt = v0;
            return v;
        }

        public static AVS_Value new_value_error(NativeString v0)
        {
            var v = new AVS_Value();
            v.type = 'e';
            v.d.str = v0;
            return v;
        }

        public static AVS_Value new_value_clip(AVS_Clip v0)
        {
            var v = new AVS_Value();
            set_to_clip(ref v, v0);
            return v;
        }

        public static unsafe AVS_Value new_value_array(params AVS_Value[] v0)
        {
            if (v0 == null || v0.Length == 0)
                return new AVS_Value {type = 'a'};

            var v = new AVS_Value();
            v.type = 'a';
            v.d.array = (IntPtr) Unmanaged.NewAndInit<AVS_Value>(v0.Length);
            v.array_size = (short) v0.Length;
            for (var i = 0; i < v0.Length; i++)
                ((AVS_Value*) v.d.array)[i] = v0[i];
            return v;
        }

        /////////////////////////////////////////////////////////////////////
        //
        // AVS_Clip
        //

        [DllImport(library, EntryPoint = "avs_release_clip", CallingConvention = CallingConvention.StdCall)]
        public static extern void release_clip(AVS_Clip v0);

        [DllImport(library, EntryPoint = "avs_copy_clip", CallingConvention = CallingConvention.StdCall)]
        public static extern AVS_Clip copy_clip(AVS_Clip v0);

        [DllImport(library, EntryPoint = "avs_clip_get_error", CallingConvention = CallingConvention.StdCall)]
        public static extern NativeString clip_get_error(AVS_Clip v0); // return 0 if no error

        [DllImport(library, EntryPoint = "avs_get_video_info", CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe AVS_VideoInfo* get_video_info(AVS_Clip v0);

        [DllImport(library, EntryPoint = "avs_get_version", CallingConvention = CallingConvention.StdCall)]
        public static extern int get_version(AVS_Clip v0);

        [DllImport(library, EntryPoint = "avs_get_frame", CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe AVS_VideoFrame* get_frame(AVS_Clip v0, int n);

        // return field parity if field_based, else parity of first field in frame
        [DllImport(library, EntryPoint = "avs_get_parity", CallingConvention = CallingConvention.StdCall)]
        public static extern int get_parity(AVS_Clip v0, int n);

        // start and count are in samples
        [DllImport(library, EntryPoint = "avs_get_audio", CallingConvention = CallingConvention.StdCall)]
        public static extern int get_audio(AVS_Clip v0, IntPtr buf, long start, long count);

        // start and count are in samples
        [DllImport(library, EntryPoint = "avs_get_audio", CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe int get_audio(AVS_Clip v0, byte* buf, long start, long count);

        [DllImport(library, EntryPoint = "avs_set_cache_hints", CallingConvention = CallingConvention.StdCall)]
        public static extern int set_cache_hints(AVS_Clip v0, int cachehints, int frame_range);

        [DllImport(library, EntryPoint = "avs_get_error", CallingConvention = CallingConvention.StdCall)]
        public static extern NativeString get_error(AVS_ScriptEnvironment v0); // return 0 if no error

        [DllImport(library, EntryPoint = "avs_get_cpu_flags", CallingConvention = CallingConvention.StdCall)]
        public static extern long get_cpu_flags(AVS_ScriptEnvironment v0);

        [DllImport(library, EntryPoint = "avs_check_version", CallingConvention = CallingConvention.StdCall)]
        public static extern NativeBool check_version(AVS_ScriptEnvironment v0, int version);

        [DllImport(library, EntryPoint = "avs_save_string", CallingConvention = CallingConvention.StdCall)]
        public static extern NativeString save_string(AVS_ScriptEnvironment v0, NativeString s, int length);

        //Not Required this
        //AVSC_API(char *, avs_sprintf)(AVS_ScriptEnvironment *, const char * fmt, ...);
        //AVSC_API(char *, avs_vsprintf)(AVS_ScriptEnvironment *, const char * fmt, void* val);
        //AVSC_API(int, avs_add_function)(AVS_ScriptEnvironment*,
        //                        const char* name, const char* params,
        //                       AVS_ApplyFunc apply, void* user_data);

        [DllImport(library, EntryPoint = "avs_function_exists", CallingConvention = CallingConvention.StdCall)]
        public static extern NativeBool function_exists(AVS_ScriptEnvironment v0, NativeString name);

        // The returned value must be be released with avs_release_value
        [DllImport(library, EntryPoint = "avs_invoke", CallingConvention = CallingConvention.StdCall)]
        public static extern AVS_Value invoke(AVS_ScriptEnvironment v0, NativeString name, AVS_Value args,
            NativeString[] arg_names);

        // The returned value must be be released with avs_release_value
        [DllImport(library, EntryPoint = "avs_invoke", CallingConvention = CallingConvention.StdCall)]
        public static extern AVS_Value invoke(AVS_ScriptEnvironment v0, NativeString name, IntPtr args,
            NativeString[] arg_names);

        // The returned value must be be released with avs_release_value
        [DllImport(library, EntryPoint = "avs_get_var", CallingConvention = CallingConvention.StdCall)]
        public static extern AVS_Value get_var(AVS_ScriptEnvironment v0, NativeString name);

        [DllImport(library, EntryPoint = "avs_set_var", CallingConvention = CallingConvention.StdCall)]
        public static extern int set_var(AVS_ScriptEnvironment v0, NativeString name, AVS_Value val);

        [DllImport(library, EntryPoint = "avs_set_global_var", CallingConvention = CallingConvention.StdCall)]
        public static extern int set_global_var(AVS_ScriptEnvironment v0, NativeString name, AVS_Value val);

        //Not Required this
        //AVSC_API(AVS_VideoFrame *, avs_new_video_frame_a)(AVS_ScriptEnvironment *, 
        //                                  const AVS_VideoInfo * vi, int align);
        //#ifndef AVSC_NO_DECLSPEC
        //AVSC_INLINE 
        //AVS_VideoFrame * avs_new_video_frame(AVS_ScriptEnvironment * env, 
        //                                     const AVS_VideoInfo * vi)
        //  {return avs_new_video_frame_a(env,vi,AVS_FRAME_ALIGN);}

        //AVSC_INLINE 
        //AVS_VideoFrame * avs_new_frame(AVS_ScriptEnvironment * env, 
        //                               const AVS_VideoInfo * vi)
        //  {return avs_new_video_frame_a(env,vi,AVS_FRAME_ALIGN);}
        //#endif
        //AVSC_API(int, avs_make_writable)(AVS_ScriptEnvironment *, AVS_VideoFrame * * pvf);

        //AVSC_API(int, avs_make_writable)(AVS_ScriptEnvironment *, AVS_VideoFrame * * pvf);

        [DllImport(library, EntryPoint = "avs_bit_blt", CallingConvention = CallingConvention.StdCall)]
        public static extern void bit_blt(AVS_ScriptEnvironment v0, IntPtr dstp, int dst_pitch, IntPtr srcp,
            int src_pitch, int row_size, int height);

        //Not Required this
        //typedef void (AVSC_CC *AVS_ShutdownFunc)(void* user_data, AVS_ScriptEnvironment * env);
        //AVSC_API(void, avs_at_exit)(AVS_ScriptEnvironment *, AVS_ShutdownFunc function, void * user_data);
        //
        //AVSC_API(AVS_VideoFrame *, avs_subframe)(AVS_ScriptEnvironment *, AVS_VideoFrame * src, int rel_offset, int new_pitch, int new_row_size, int new_height);
        //// The returned video frame must be be released

        [DllImport(library, EntryPoint = "avs_set_memory_max", CallingConvention = CallingConvention.StdCall)]
        public static extern int set_set_memory_max(AVS_ScriptEnvironment v0, int mem);

        [DllImport(library, EntryPoint = "avs_set_working_dir", CallingConvention = CallingConvention.StdCall)]
        public static extern int set_set_working_dir(AVS_ScriptEnvironment v0, NativeString newdir);

        // avisynth.dll exports this; it's a way to use it as a library, without
        // writing an AVS script or without going through AVIFile.
        [DllImport(library, EntryPoint = "avs_create_script_environment",
            CallingConvention = CallingConvention.StdCall)]
        public static extern AVS_ScriptEnvironment create_script_environment(int version);

        //Not Required this
        // this symbol is the entry point for the plugin and must
        // be defined
        //AVSC_EXPORT
        //const char* AVSC_CC avisynth_c_plugin_init(AVS_ScriptEnvironment* env);

        [DllImport(library, EntryPoint = "avs_delete_script_environment",
            CallingConvention = CallingConvention.StdCall)]
        public static extern void delete_script_environment(AVS_ScriptEnvironment v0);

        #region Constants

        public const int AVISYNTH_INTERFACE_VERSION = 6;
        public const int AVS_FRAME_ALIGN = 16;

        #region AVS_SAMPLE

        public const int AVS_SAMPLE_INT8 = 1 << 0;
        public const int AVS_SAMPLE_INT16 = 1 << 1;
        public const int AVS_SAMPLE_INT24 = 1 << 2;
        public const int AVS_SAMPLE_INT32 = 1 << 3;
        public const int AVS_SAMPLE_FLOAT = 1 << 4;

        #endregion

        #region AVS_PLANAR

        public const int AVS_PLANAR_Y = 1 << 0;
        public const int AVS_PLANAR_U = 1 << 1;
        public const int AVS_PLANAR_V = 1 << 2;
        public const int AVS_PLANAR_ALIGNED = 1 << 3;
        public const int AVS_PLANAR_Y_ALIGNED = AVS_PLANAR_Y | AVS_PLANAR_ALIGNED;
        public const int AVS_PLANAR_U_ALIGNED = AVS_PLANAR_U | AVS_PLANAR_ALIGNED;
        public const int AVS_PLANAR_V_ALIGNED = AVS_PLANAR_V | AVS_PLANAR_ALIGNED;
        public const int AVS_PLANAR_A = 1 << 4;
        public const int AVS_PLANAR_R = 1 << 5;
        public const int AVS_PLANAR_G = 1 << 6;
        public const int AVS_PLANAR_B = 1 << 7;
        public const int AVS_PLANAR_A_ALIGNED = AVS_PLANAR_A | AVS_PLANAR_ALIGNED;
        public const int AVS_PLANAR_R_ALIGNED = AVS_PLANAR_R | AVS_PLANAR_ALIGNED;
        public const int AVS_PLANAR_G_ALIGNED = AVS_PLANAR_G | AVS_PLANAR_ALIGNED;
        public const int AVS_PLANAR_B_ALIGNED = AVS_PLANAR_B | AVS_PLANAR_ALIGNED;

        #endregion

        #region AVS_CS

        // Colorspace properties.
        public const int AVS_CS_BGR = 1 << 28;
        public const int AVS_CS_YUV = 1 << 29;
        public const int AVS_CS_INTERLEAVED = 1 << 30;
        public const int AVS_CS_PLANAR = 1 << 31;

        public const int AVS_CS_SHIFT_SUB_WIDTH = 0;
        public const int AVS_CS_SHIFT_SUB_HEIGHT = 8;
        public const int AVS_CS_SHIFT_SAMPLE_BITS = 16;

        public const int AVS_CS_SUB_WIDTH_MASK = 7 << AVS_CS_SHIFT_SUB_WIDTH;
        public const int AVS_CS_SUB_WIDTH_1 = 3 << AVS_CS_SHIFT_SUB_WIDTH; // YV24
        public const int AVS_CS_SUB_WIDTH_2 = 0 << AVS_CS_SHIFT_SUB_WIDTH; // YV12; I420; YV16
        public const int AVS_CS_SUB_WIDTH_4 = 1 << AVS_CS_SHIFT_SUB_WIDTH; // YUV9; YV411

        public const int AVS_CS_VPLANEFIRST = 1 << 3; // YV12; YV16; YV24; YV411; YUV9
        public const int AVS_CS_UPLANEFIRST = 1 << 4; // I420

        public const int AVS_CS_SUB_HEIGHT_MASK = 7 << AVS_CS_SHIFT_SUB_HEIGHT;
        public const int AVS_CS_SUB_HEIGHT_1 = 3 << AVS_CS_SHIFT_SUB_HEIGHT; // YV16; YV24; YV411
        public const int AVS_CS_SUB_HEIGHT_2 = 0 << AVS_CS_SHIFT_SUB_HEIGHT; // YV12; I420
        public const int AVS_CS_SUB_HEIGHT_4 = 1 << AVS_CS_SHIFT_SUB_HEIGHT; // YUV9

        public const int AVS_CS_SAMPLE_BITS_MASK = 7 << AVS_CS_SHIFT_SAMPLE_BITS;
        public const int AVS_CS_SAMPLE_BITS_8 = 0 << AVS_CS_SHIFT_SAMPLE_BITS;
        public const int AVS_CS_SAMPLE_BITS_16 = 1 << AVS_CS_SHIFT_SAMPLE_BITS;
        public const int AVS_CS_SAMPLE_BITS_32 = 2 << AVS_CS_SHIFT_SAMPLE_BITS;

        public const int AVS_CS_PLANAR_MASK = AVS_CS_PLANAR | AVS_CS_INTERLEAVED | AVS_CS_YUV | AVS_CS_BGR |
                                              AVS_CS_SAMPLE_BITS_MASK | AVS_CS_SUB_HEIGHT_MASK | AVS_CS_SUB_WIDTH_MASK;

        public const int AVS_CS_PLANAR_FILTER = ~(AVS_CS_VPLANEFIRST | AVS_CS_UPLANEFIRST);

        // Specific colorformats
        public const int AVS_CS_UNKNOWN = 0;
        public const int AVS_CS_BGR24 = (1 << 0) | AVS_CS_BGR | AVS_CS_INTERLEAVED;
        public const int AVS_CS_BGR32 = (1 << 1) | AVS_CS_BGR | AVS_CS_INTERLEAVED;

        public const int AVS_CS_YUY2 = (1 << 2) | AVS_CS_YUV | AVS_CS_INTERLEAVED;

        //  AVS_CS_YV12  = 1<<3  Reserved
        //  AVS_CS_I420  = 1<<4  Reserved
        public const int AVS_CS_RAW32 = (1 << 5) | AVS_CS_INTERLEAVED;

        public const int AVS_CS_YV24 = AVS_CS_PLANAR | AVS_CS_YUV | AVS_CS_SAMPLE_BITS_8 | AVS_CS_VPLANEFIRST |
                                       AVS_CS_SUB_HEIGHT_1 | AVS_CS_SUB_WIDTH_1; // YVU 4:4:4 planar

        public const int AVS_CS_YV16 = AVS_CS_PLANAR | AVS_CS_YUV | AVS_CS_SAMPLE_BITS_8 | AVS_CS_VPLANEFIRST |
                                       AVS_CS_SUB_HEIGHT_1 | AVS_CS_SUB_WIDTH_2; // YVU 4:2:2 planar

        public const int AVS_CS_YV12 = AVS_CS_PLANAR | AVS_CS_YUV | AVS_CS_SAMPLE_BITS_8 | AVS_CS_VPLANEFIRST |
                                       AVS_CS_SUB_HEIGHT_2 | AVS_CS_SUB_WIDTH_2; // YVU 4:2:0 planar

        public const int AVS_CS_I420 = AVS_CS_PLANAR | AVS_CS_YUV | AVS_CS_SAMPLE_BITS_8 | AVS_CS_UPLANEFIRST |
                                       AVS_CS_SUB_HEIGHT_2 | AVS_CS_SUB_WIDTH_2; // YUV 4:2:0 planar

        public const int AVS_CS_IYUV = AVS_CS_I420;

        public const int AVS_CS_YV411 = AVS_CS_PLANAR | AVS_CS_YUV | AVS_CS_SAMPLE_BITS_8 | AVS_CS_VPLANEFIRST |
                                        AVS_CS_SUB_HEIGHT_1 | AVS_CS_SUB_WIDTH_4; // YVU 4:1:1 planar

        public const int AVS_CS_YUV9 = AVS_CS_PLANAR | AVS_CS_YUV | AVS_CS_SAMPLE_BITS_8 | AVS_CS_VPLANEFIRST |
                                       AVS_CS_SUB_HEIGHT_4 | AVS_CS_SUB_WIDTH_4; // YVU 4:1:0 planar

        public const int AVS_CS_Y8 = AVS_CS_PLANAR | AVS_CS_INTERLEAVED | AVS_CS_YUV | AVS_CS_SAMPLE_BITS_8;

        #endregion

        #region AVS_IT

        public const int AVS_IT_BFF = 1 << 0;
        public const int AVS_IT_TFF = 1 << 1;
        public const int AVS_IT_FIELDBASED = 1 << 2;

        #endregion

        #region AVS_CACHE

        // New 2.6 explicitly defined cache hints.
        public const int AVS_CACHE_NOTHING = 10; // Do not cache video.

        public const int
            AVS_CACHE_WINDOW = 11; // Hard protect upto X frames within a range of X from the current frame N.

        public const int AVS_CACHE_GENERIC = 12;       // LRU cache upto X frames.
        public const int AVS_CACHE_FORCE_GENERIC = 13; // LRU cache upto X frames; override any previous CACHE_WINDOW.

        public const int AVS_CACHE_GET_POLICY = 30; // Get the current policy.
        public const int AVS_CACHE_GET_WINDOW = 31; // Get the current window h_span.
        public const int AVS_CACHE_GET_RANGE = 32;  // Get the current generic frame range.

        public const int AVS_CACHE_AUDIO = 50;         // Explicitly do cache audio; X byte cache.
        public const int AVS_CACHE_AUDIO_NOTHING = 51; // Explicitly do not cache audio.
        public const int AVS_CACHE_AUDIO_NONE = 52;    // Audio cache off (auto mode); X byte intial cache.
        public const int AVS_CACHE_AUDIO_AUTO = 53;    // Audio cache on (auto mode); X byte intial cache.

        public const int AVS_CACHE_GET_AUDIO_POLICY = 70; // Get the current audio policy.
        public const int AVS_CACHE_GET_AUDIO_SIZE = 71;   // Get the current audio cache size.

        public const int AVS_CACHE_PREFETCH_FRAME = 100; // Queue request to prefetch frame N.
        public const int AVS_CACHE_PREFETCH_GO = 101;    // Action video prefetches.

        public const int
            AVS_CACHE_PREFETCH_AUDIO_BEGIN =
                120; // Begin queue request transaction to prefetch audio (take critical section).

        public const int AVS_CACHE_PREFETCH_AUDIO_STARTLO = 121; // Set low 32 bits of start.
        public const int AVS_CACHE_PREFETCH_AUDIO_STARTHI = 122; // Set high 32 bits of start.
        public const int AVS_CACHE_PREFETCH_AUDIO_COUNT = 123;   // Set low 32 bits of length.

        public const int
            AVS_CACHE_PREFETCH_AUDIO_COMMIT =
                124; // Enqueue request transaction to prefetch audio (release critical section).

        public const int AVS_CACHE_PREFETCH_AUDIO_GO = 125; // Action audio prefetches.

        public const int AVS_CACHE_GETCHILD_CACHE_MODE = 200; // Cache ask Child for desired video cache mode.
        public const int AVS_CACHE_GETCHILD_CACHE_SIZE = 201; // Cache ask Child for desired video cache size.
        public const int AVS_CACHE_GETCHILD_AUDIO_MODE = 202; // Cache ask Child for desired audio cache mode.
        public const int AVS_CACHE_GETCHILD_AUDIO_SIZE = 203; // Cache ask Child for desired audio cache size.

        public const int AVS_CACHE_GETCHILD_COST = 220; // Cache ask Child for estimated processing cost.
        public const int AVS_CACHE_COST_ZERO = 221;     // Child response of zero cost (ptr arithmetic only).

        public const int
            AVS_CACHE_COST_UNIT = 222; // Child response of unit cost (less than or equal 1 full frame blit).

        public const int AVS_CACHE_COST_LOW = 223; // Child response of light cost. (Fast)
        public const int AVS_CACHE_COST_MED = 224; // Child response of medium cost. (Real time)
        public const int AVS_CACHE_COST_HI = 225;  // Child response of heavy cost. (Slow)

        public const int AVS_CACHE_GETCHILD_THREAD_MODE = 240; // Cache ask Child for thread safetyness.

        public const int
            AVS_CACHE_THREAD_UNSAFE = 241; // Only 1 thread allowed for all instances. 2.5 filters default!

        public const int
            AVS_CACHE_THREAD_CLASS = 242; // Only 1 thread allowed for each instance. 2.6 filters default!

        public const int AVS_CACHE_THREAD_SAFE = 243; //  Allow all threads in any instance.
        public const int AVS_CACHE_THREAD_OWN = 244;  // Safe but limit to 1 thread; internally threaded.

        public const int AVS_CACHE_GETCHILD_ACCESS_COST = 260; // Cache ask Child for preferred access pattern.
        public const int AVS_CACHE_ACCESS_RAND = 261;          // Filter is access order agnostic.
        public const int AVS_CACHE_ACCESS_SEQ0 = 262;          // Filter prefers sequential access (low cost)
        public const int AVS_CACHE_ACCESS_SEQ1 = 263;          // Filter needs sequential access (high cost)

        #endregion

        #region AVS_CPU

        // For GetCPUFlags.  These are backwards-compatible with those in VirtualDub.
        /* slowest CPU to support extension */
        public const int AVS_CPU_FORCE = 0x01;       // N/A
        public const int AVS_CPU_FPU = 0x02;         // 386/486DX
        public const int AVS_CPU_MMX = 0x04;         // P55C; K6; PII
        public const int AVS_CPU_INTEGER_SSE = 0x08; // PIII; Athlon
        public const int AVS_CPU_SSE = 0x10;         // PIII; Athlon XP/MP
        public const int AVS_CPU_SSE2 = 0x20;        // PIV; Hammer
        public const int AVS_CPU_3DNOW = 0x40;       // K6-2
        public const int AVS_CPU_3DNOW_EXT = 0x80;   // Athlon

        public const int AVS_CPU_X86_64 = 0xA0; // Hammer (note: equiv. to 3DNow + SSE2; 

        // which only Hammer will have anyway)
        public const int AVS_CPUF_SSE3 = 0x100;  //  PIV+; K8 Venice
        public const int AVS_CPUF_SSSE3 = 0x200; //  Core 2
        public const int AVS_CPUF_SSE4 = 0x400;  //  Penryn; Wolfdale; Yorkfield

        public const int AVS_CPUF_SSE4_1 = 0x400;

        //AVS_CPUF_AVX        = 0x800;   //  Sandy Bridge; Bulldozer
        public const int AVS_CPUF_SSE4_2 = 0x1000; //  Nehalem
        //AVS_CPUF_AVX2      = 0x2000;   //  Haswell
        //AVS_CPUF_AVX512    = 0x4000;   //  Knights Landing

        #endregion

        #endregion

        #region Structures

        [StructLayout(LayoutKind.Sequential)]
        public struct NativeString
        {
            private readonly IntPtr native;

            public NativeString(string s)
            {
                //GC.KeepAlive(this);
                native = Marshal.StringToHGlobalAnsi(s);
            }

            public NativeString(IntPtr ip)
            {
                native = ip;
            }

            public void Free()
            {
                Marshal.FreeHGlobal(native);
            }

            public static implicit operator NativeString(string x)
            {
                return new NativeString(x);
            }

            public static implicit operator NativeString(IntPtr x)
            {
                return new NativeString(x);
            }

            public static implicit operator string(NativeString x)
            {
                return Marshal.PtrToStringAnsi(x.native);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NativeBool
        {
            private readonly byte native;

            public NativeBool(byte b)
            {
                native = b;
            }

            public static implicit operator NativeBool(byte p)
            {
                return new NativeBool(p);
            }

            public static implicit operator NativeBool(int i)
            {
                return i != 0 ? true : false;
            }

            public static implicit operator NativeBool(bool bp)
            {
                return new NativeBool(bp ? (byte) 1 : (byte) 0);
            }

            public static implicit operator byte(NativeBool bp)
            {
                return bp.native;
            }

            public static implicit operator int(NativeBool bp)
            {
                return bp ? 1 : 0;
            }

            public static implicit operator bool(NativeBool bp)
            {
                return Convert.ToBoolean(bp.native);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct AVS_Clip
        {
            private readonly IntPtr native;

            private AVS_Clip(IntPtr native)
            {
                this.native = native;
            }

            public static implicit operator AVS_Clip(IntPtr x)
            {
                return new AVS_Clip(x);
            }

            public static implicit operator IntPtr(AVS_Clip s)
            {
                return s.native;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct AVS_ScriptEnvironment
        {
            private readonly IntPtr native;

            private AVS_ScriptEnvironment(IntPtr native)
            {
                this.native = native;
            }

            public static implicit operator AVS_ScriptEnvironment(IntPtr x)
            {
                return new AVS_ScriptEnvironment(x);
            }

            public static implicit operator IntPtr(AVS_ScriptEnvironment s)
            {
                return s.native;
            }
        }

        /////////////////////////////////////////////////////////////////////
        //
        // AVS_VideoInfo
        //

        // AVS_VideoInfo is layed out identicly to VideoInfo
        [StructLayout(LayoutKind.Sequential)]
        public struct AVS_VideoInfo
        {
            public int width, height; // width=0 means no video
            [MarshalAs(UnmanagedType.U4)] public int fps_numerator;
            [MarshalAs(UnmanagedType.U4)] public int fps_denominator;
            public int num_frames;

            public int pixel_type;

            public int audio_samples_per_second; // 0 means no audio
            public int sample_type;
            public long num_audio_samples;
            public int nchannels;

            // Imagetype properties

            public int image_type;
        }

        /////////////////////////////////////////////////////////////////////
        //
        // AVS_VideoFrame
        //

        // VideoFrameBuffer holds information about a memory block which is used
        // for video data.  For efficiency, instances of this class are not deleted
        // when the refcount reaches zero; instead they're stored in a linked list
        // to be reused.  The instances are deleted when the corresponding AVS
        // file is closed.

        // AVS_VideoFrameBuffer is layed out identicly to VideoFrameBuffer
        // DO NOT USE THIS STRUCTURE DIRECTLY
        [StructLayout(LayoutKind.Sequential)]
        public struct AVS_VideoFrameBuffer
        {
            public IntPtr data;

            public int data_size;

            // sequence_number is incremented every time the buffer is changed, so
            // that stale views can tell they're no longer valid.
            public long sequence_number;

            public long refcount;
        }

        // VideoFrame holds a "window" into a VideoFrameBuffer.

        // AVS_VideoFrame is layed out identicly to IVideoFrame
        // DO NOT USE THIS STRUCTURE DIRECTLY
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct AVS_VideoFrame
        {
            public long refcount;
            public AVS_VideoFrameBuffer* vfb;

            public int
                offset, pitch, row_size, height, offsetU, offsetV, pitchUV; // U&V offsets are from top of picture.

            public int row_sizeUV, heightUV;
        }

        /////////////////////////////////////////////////////////////////////
        //
        // AVS_Value
        //

        // Treat AVS_Value as a fat pointer.  That is use avs_copy_value
        // and avs_release_value appropiaty as you would if AVS_Value was
        // a pointer.

        // To maintain source code compatibility with future versions of the
        // avisynth_c API don't use the AVS_Value directly.  Use the helper
        // functions below.

        // AVS_Value is layed out identicly to AVSValue
        [StructLayout(LayoutKind.Sequential)]
        public struct AVS_Value
        {
            [MarshalAs(UnmanagedType.I2)]
            public char type; // 'a'rray, 'c'lip, 'b'ool, 'i'nt, 'f'loat, 's'tring, 'v'oid, or 'l'ong

            // for some function e'rror
            [MarshalAs(UnmanagedType.I2)] public short array_size;
            public AVS_Value_d d;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct AVS_Value_d
        {
            [FieldOffset(0)] public IntPtr clip; // do not use directly, use avs_take_clip
            [FieldOffset(0)] public NativeBool boolean;
            [FieldOffset(0)] public int integer;
            [FieldOffset(0)] public float floating_pt;
            [FieldOffset(0)] public NativeString str;
            [FieldOffset(0)] public IntPtr array; //AVS_Value array
        }

        #endregion
    }
}