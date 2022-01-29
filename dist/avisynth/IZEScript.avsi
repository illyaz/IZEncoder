function IZEncoder()
{
    return FindStr(LCase(VersionString), "izencoder") > 0
}

function IZECompare(clip a, clip b){
	# Fix Frame rate
	newFPS = Max(a.FrameRate, b.FrameRate)
	a = a.FrameRate == newFPS ? a : a.ChangeFPS(newFPS)
	b = b.FrameRate == newFPS ? b : b.ChangeFPS(newFPS)
	
	if(a.PixelType == b.PixelType && a.Width == b.Width && a.Height == b.Height) {
		return StackHorizontal(a, b)
	} else {
		return AudioDub(BlankClip(a, Max(a.FrameCount, b.FrameCount), b.Width * 2, b.Height, fps=newFPS).Overlay(a).Overlay(b, x=a.Width), a.KillVideo())
	}
}

function IZESubtitle(clip source, string "file", bool "mod") {
	return mod ? TextSubMod(source, file) : TextSub(source, file)
}

function IZEOverlay(clip srcClip, clip overlayClip, int "x", int "y", float "opacity", string "mode"){
	opacity = default(opacity, 1)
	mode = default(mode, "blend")
	
	return overlayClip.IsRGB32() ? srcClip.Overlay(overlayClip, mask=overlayClip.ShowAlpha(), x=x, y=y, opacity=opacity, mode=mode) : srcClip.Overlay(overlayClip, x=x, y=y, opacity=opacity, mode=mode)
}

function IZEImageLogo(clip C, string imagefile, int "x", int "y", float "scale", float "opacity", string "mode", string "align", string "uid"){
	
	x = default(x, 5)
	y = default(y, 5)
	o = default(opacity, 0.5)
	s = default(scale, 1)
	m = default(mode, "blend")
	a = LCase(default(align, "left-top"))
	# align
	#	horizontal: left, right, middle
	#	vertical: top, bottom, middle
	#	use: horizontal-vertical
	
	uid = default(uid, Undefined())
	ASSERT(C.IsClip(), "Invalid input")
	
	ASSERT(Findstr(a, "-") > 0, "Invalid align")
	
	hAlign = MidStr(a, 1, Findstr(a, "-") - 1)
	vAlign = MidStr(a, Findstr(a, "-") + 1)
	
	ASSERT(hAlign == "left" || hAlign == "right" || hAlign == "middle", """Invalid horizontal "align" value '""" + hAlign + "'")
	ASSERT(vAlign == "top" || vAlign == "bottom" || vAlign == "middle", """Invalid vertical "align" value '""" + vAlign + "'")	
		
	P = ImageSource(imagefile, start=0, end=C.FrameCount, fps=C.FrameRate, pixel_type="RGB32", use_DevIL=true)
	P = P.Lanczos4Resize(Int(P.Width * s), Int(P.Height * s))
	
	if( IZEncoder() && Defined(uid) ) {
		IZEncoderUISendMessage(uid, "position", "SizeW", P.Width)
		IZEncoderUISendMessage(uid, "position", "SizeH", P.Height)
	}
	
	if (hAlign == "left") {
		x = x
	} else if (hAlign == "right") {
		x = C.Width - P.Width - x
	} else { # Middle
		x = (C.Width / 2) - (P.Width / 2) - x
	}
		
	if (vAlign == "top") {
		y = y
	} else if (vAlign == "bottom") {
		y = C.Height - P.Height - y
	} else { # Middle
		y = (C.Height / 2) - (P.Height / 2) - y
	}
	
	return C.IZEOverlay(P, x, y, o, m) 
}

function IZEImageIntro(clip C, string imagefile, int "introDuration", int "fadeDuration", \ 
	bool "ifadeIn", bool "ifadeOut", int "color"){
	
	introDuration = default(introDuration, 5000)
	fadeDuration  = default(fadeDuration, 1000)
	ifadeIn       = default(ifadeIn, true)
	ifadeOut      = default(ifadeOut, true)
	color         = default(color, $000000)
	
	ASSERT(C.IsClip(), "Invalid input")
	
	P = ImageSource(imagefile, start=0, end=Int(introDuration / 1000 * FrameRate(C)), fps=FrameRate(C), pixel_type="RGB32", use_DevIL=true)
	P = (Width(C) != Width(P) || Height(C) != Height(P)) ? P.Lanczos4Resize(Width(C), Height(C)) : P
	P = (ifadeIn == true) ? P.FadeIn(Int((fadeDuration / 1000.0) * FrameRate(C)), color) : P
	P = (ifadeOut == true) ? P.FadeOut(Int((fadeDuration / 1000.0) * FrameRate(C)), color) : P
	B = BlankClip(C, Int(introDuration / 1000 * FrameRate(C)))
	B = B.Overlay(P, mask=P.ShowAlpha())
	R = B + C
	
	return R
}

function IZEVideoIntro(clip C, clip intro, int "fadeDuration", \ 
	bool "ifadeIn", bool "ifadeOut", int "color"){
	
	fadeDuration  = default(fadeDuration, 1000)
	ifadeIn       = default(ifadeIn, true)
	ifadeOut      = default(ifadeOut, true)
	color         = default(color, $000000)
	
	ASSERT(C.IsClip(), "Invalid input")
	
	P = intro
	
	P = (Width(C) != Width(P) || Height(C) != Height(P)) ? P.Lanczos4Resize(Width(C), Height(C)) : P
	P = (FrameRate(C) != FrameRate(P)) ? P.ChangeFPS(FrameRate(C)) : P	
	
	if ( HasAudio(P) && HasAudio(C) ) {
		P = (AudioRate(C) != AudioRate(P)) ? P.SSRC(AudioRate(C)) : P
		
		if( AudioBits(C) != AudioBits(P) ) {
			if ( IsAudioInt(C) ) {
				if ( AudioBits(C) == 8) {
					P = P.ConvertAudioTo8Bit()
				} else if ( AudioBits(C) == 16 ) {
					P = P.ConvertAudioTo16Bit()
				} else if ( AudioBits(C) == 24 ) {
					P = P.ConvertAudioTo24Bit()
				} else if ( AudioBits(C) == 32 ) {
					P = P.ConvertAudioTo32Bit()
				}
			} else {
				P = P.ConvertAudioToFloat()
			}
		}
	}
	
	P = (ifadeIn == true) ? P.FadeIn(Int((fadeDuration / 1000.0) * FrameRate(C)), color) : P
	P = (ifadeOut == true) ? P.FadeOut(Int((fadeDuration / 1000.0) * FrameRate(C)), color) : P
	B = BlankClip(C, P.FrameCount)
	B = B.Overlay(P)
	
	if ( HasAudio(P) && HasAudio(C) ) {
		B = B.MixAudio(P)
	}
	
	return B + C
}

function IZESmoothFPS(clip c, int "targetfps") {
	targetfps = default(targetfps, 60000)
	super_params     = "{scale:{up:0},gpu:1,rc:true}"
	analyse_params   = "{main:{search:{coarse:{distance:-8,bad:{sad:2000,range:24}},type:2}},refine:[{thsad:250}]}"
	smoothfps_params = 0 >= targetfps ? "{gpuid:11,rate:{num:2,den:1,abs:false},algo:13,mask:{area:200},scene:{}}" : ("{gpuid:11,rate:{num:" + string(targetfps) + ",den:1001,abs:true},algo:13,mask:{area:200},scene:{}}")
	
	super=SVSuper(c, super_params)
	vectors=SVAnalyse(super, analyse_params, src=c)
	return SVSmoothFps(c, super, vectors, smoothfps_params, mt=IZE_ThreadCount, src=c)
}

# Use to repair stupid 23.976 > 29.970 fps conversion
function IZERepair24FPS(clip c){
	return c.SelectEvery(5,0,2,3,4)
}