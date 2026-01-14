#ifndef __COSSACKS2_STDAFX__
#define __COSSACKS2_STDAFX__

#pragma		once
#define		WIN32_LEAN_AND_MEAN	

#define		WM_MOUSEWHEEL		0x020A
#define		WHEEL_DELTA			120

#include "windows.h"

#include <math.h> 
#include <direct.h>
#include <malloc.h>
#include <string.h>
#include <stdio.h>
#include <stdlib.h>
#include <math.h>
#include <float.h>


//#ifdef _DEBUG
//#define _CRTDBG_MAP_ALLOC
//#include <crtdbg.h>
//#endif // _DEBUG

#include "gmDefines.h"
#include "mUtil.h"

#include "kAssert.h"
#include "kResFile.h"
#include "kIO.h"
#include "kLog.h"
#include "kString.h"
#include "kCache.h"
#include "kArray.hpp"
#include "kTemplates.hpp"
#include "kPropertyMap.h"
#include "kInput.h"
#include "kUtilities.h"
#include "kMemorySpy.h"

#include "IRenderSystem.h"
#include "ISpriteManager.h"
#include "IPictureManager.h"

#include "kTypeTraits.h"
#include "kEnumTraits.h"

#include "mMath2D.h"
#include "mMath3D.h"
#include "mQuaternion.h"
#include "mGeom3D.h"
#include "mAlgo.h"
#include "kColorValue.h"

#include "kTypeTraits.h"
#include "kMathTypeTraits.h"
#include "kXMLParser.h"

#include "gpMesh.h"

#include "kIOHelpers.h"
#include "kStatistics.h"

#include "sg.h"
#include "sgStateBlock.h"

#include "uiWidget.h"

//{{AFX_INSERT_LOCATION}}
#endif // __COSSACKS2_STDAFX__
