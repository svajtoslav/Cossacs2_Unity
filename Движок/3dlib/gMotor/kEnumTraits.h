/*****************************************************************
/*  File:   kEnumTraits.h                                            
/*  Desc:   Enum values traits declarations 
/*****************************************************************/
#ifndef __KENUMTRAITS_H__
#define __KENUMTRAITS_H__

ENUM( ColorFormat,			"Color Format",
	 en_val( cfUnknown,		"Unknown"	) <<		
	 en_val( cfARGB4444,	"ARGB4444"	) <<	
	 en_val( cfXRGB1555,	"XRGB1555"	) <<	
	 en_val( cfARGB8888,	"ARGB8888"	) <<	
	 en_val( cfRGB565,		"RGB565"	) <<
	 en_val( cfA8,			"A8"		) <<	
	 en_val( cfRGB888,		"RGB888"	) <<	
	 en_val( cfXRGB8888,	"XRGB8888"	) <<
	 en_val( cfV8U8,		"V8U8"		) <<	
	 en_val( cfBackBufferCompatible, "BackBufferCompatible" ) );

ENUM( MemoryPool,		"Memory Pool",
	 en_val( mpUnknown,	"Unknown"		) <<		
	 en_val( mpSysMem,	"System Memory"	) <<	
	 en_val( mpVRAM	,	"Video Memory"	) <<	
	 en_val( mpManaged,	"Managed"		) );

ENUM( TextureUsage,				"Texture Usage",
	 en_val( tuUnknown,			"Unknown"		) <<		
	 en_val( tuLoadable,		"Loadable"		) <<	
	 en_val( tuProcedural,		"Procedural"	) <<	
	 en_val( tuRenderTarget,	"RenderTarget"	) << 
	 en_val( tuDynamic,			"Dynamic"		) <<
	 en_val( tuDepthStencil,	"DepthStencil"	) );

ENUM( DepthStencilFormat,		"DepthStencilFormat", 
	 en_val( dsfUnknown,		"Unknown"			) <<
	 en_val( dsfD16Lockable,	"D16Lockable"		) <<
	 en_val( dsfD32,			"D32"				) <<
	 en_val( dsfD15S1,			"D15S1"				) <<
	 en_val( dsfD24S8,			"D24S8"				) <<
	 en_val( dsfD16,			"D16"				) );


ENUM( ScreenResolution,		"ScreenResolution", 
	 en_val( srUnknown,		"Unknown"			) <<
	 en_val( sr640x480,		"640x480"			) <<
	 en_val( sr800x600,		"800x600"			) <<
	 en_val( sr1024x768,	"1024x768"			) <<
	 en_val( sr1280x1024,	"1280x1024"			) <<
	 en_val( sr1600x1200,	"1600x1200"			) );

ENUM( ScreenBitDepth,		"ScreenBitDepth", 
	 en_val( bdUnknown,		"Unknown"			) <<
	 en_val( bd16,			"16 bit"			) <<
	 en_val( bd32,			"32 bit"			) );

ENUM( PowerOfTwo, "PowerOfTwo", 
	 en_val( pow0,	"1"		) <<
	 en_val( pow1,	"2"		) <<
	 en_val( pow2,	"4"		) <<
	 en_val( pow3,	"8"		) <<
	 en_val( pow4,	"16"	) <<
	 en_val( pow5,	"32"	) <<
	 en_val( pow6,	"64"	) <<
	 en_val( pow7,	"128"	) <<
	 en_val( pow8,	"256"	) <<
	 en_val( pow9,	"512"	) <<
	 en_val( pow10,	"1024"	) <<
	 en_val( pow11,	"2048"	) <<
	 en_val( pow12,	"4096"	) <<
	 en_val( pow13,	"8192"	) <<
	 en_val( pow14,	"16384"	) <<
	 en_val( pow15,	"32768"	) <<
	 en_val( pow15,	"65536"	) );

ENUM( VertexFormat, "VertexFormat",
	 en_val( vfUnknown,	"Unknown"		) <<		
	 en_val( vfTnL,		"VertexTnL"		) <<	
	 en_val( vf2Tex,	"Vertex2t"		) <<
	 en_val( vfN,		"VertexN"		) <<	
	 en_val( vfTnL2,	"VertexTnL2"	) <<	
	 en_val( vfT,		"VertexT"		) <<
	 en_val( vfMP1,		"VertexMP1"		) <<	
	 en_val( vfNMP1,	"VertexNMP1"	) <<	
	 en_val( vfNMP2,	"VertexNMP2"	) <<
	 en_val( vfNMP3,	"VertexNMP3"	) <<	
	 en_val( vfNMP4,	"VertexNMP4"	) <<
	 en_val( vfN2T,		"VertexN2T"		) <<
	 en_val( vfXYZD,	"VertexXYZD"	) <<	
	 en_val( vfXYZW,	"VertexXYZW"	) <<
	 en_val( vfTS,  	"VertexTS"	)   );

#endif // __KENUMTRAITS_H__