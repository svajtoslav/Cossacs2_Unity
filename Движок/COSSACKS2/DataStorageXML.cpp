#include "stdheader.h"
#include "BE_HEADERS.h"
#include ".\cvi_HeroButtons.h"


MissSET	gMISS_SET;

extern	float fMapX;
extern	float fMapY;

extern	int AnimTime;

int GetCircleDif(int F1,int F2,int maxF/*=256*/){
	int tF1 = F1%maxF;
	int tF2 = F2%maxF;
	int dF  = abs(tF1-tF2);
	dF = min( dF, abs(maxF-dF) ) % 256;
	return dF;
};

void FI_2Pi(float& FI){			// round fi in [0,2*Pi)
	float Pi	= 3.1415f;
	float Pi2	= Pi*2.f;
	if (FI<0||FI>=Pi2) {
		FI += (-1)*Pi2*( (float)((int)(FI/Pi2)) );
		if (FI<0)	FI += Pi2;
	};
};

FILE*	f_aiLOG = NULL;
int __cdecl Msg( LPCSTR fileName, LPCSTR format, ... )
	{
	int	result = -1;
	if ( ( f_aiLOG = fopen(fileName,"a") ) != NULL ){
		size_t		length = strlen(format);
		LPSTR		_format = (LPSTR)malloc((length + 3)*sizeof(char));
		strcpy		(_format,format);
		strcat		(_format,"\n");

		va_list		marker;
		va_start	(marker,format);
					result = vfprintf(f_aiLOG,_format,marker);
		va_end		(marker);

		fclose(f_aiLOG);
	};

	return		(result);
}

void	ggg_WarnigMessage(const char* message){
	MessageBox(hwnd,message,"lua warning",MB_ICONWARNING|MB_OK);
};

extern	int	Shifter;

void LeaveAll(OneObject* OB);
bool CheckLeaveContainerAbility(OneObject* OB);

bool GetRomeHelp(cvi_RomeHelp* RH){
	if (RH==NULL)	return false;
	RH->MsgDialogs.Clear();
	vvMESSGES* pMess = dynamic_cast<vvMESSGES*>(vValuesMap()->GetVValueTypeID(_vvMESSGES_));
	if (pMess==NULL)	return false;

    CPhraseChunk* pPhrase=NULL;	// cur phrase
	cvi_MsgDialog* pMasDial=NULL;

	// Talks history
	int ChunkNUM = pMess->TALKS.GetAmount();
	for (int i=0; i<ChunkNUM; i++){
		pMasDial=new cvi_MsgDialog;
		if (pMasDial!=NULL) {
			pPhrase = dynamic_cast<CPhraseChunk*>(pMess->TALKS[i]);
			if (pPhrase!=NULL&&pPhrase->Visible){
				if (pPhrase!=NULL){
					if (pPhrase->TitleID.str!=NULL) pMasDial->Title = GetTextByID(pPhrase->TitleID.str);
					int NPR = pPhrase->PhraseList.GetAmount();
					for (int p=0; p<NPR; p++){
						vvTEXT* pText = pPhrase->PhraseList[p]->Get();
						if (pText!=NULL) {
							cvi_Phrase* pNewPR = new cvi_Phrase;
							pNewPR->Message = reinterpret_cast<char*>(pText->Get());
							if (pText->SpeakerID.str!=NULL){
								pNewPR->talkerName = GetTextByID(pText->SpeakerID.str);
							};
							pMasDial->Phrases.Add(pNewPR);
							pNewPR=NULL;
						};
					};
				};
				RH->MsgDialogs.Add(pMasDial);
				pMasDial=NULL;
			};
		};
	};

	// Hints History
	COneMissHint* pHint=NULL;
	int HintNUM = pMess->HINTS.GetAmount();
	for (int i=0; i<HintNUM; i++){
		pMasDial=new cvi_MsgDialog;
		if (pMasDial!=NULL) {
			pHint = dynamic_cast<COneMissHint*>(pMess->HINTS[i]);
			if (pHint!=NULL&&pHint->Visible&&pHint->TextID.str!=NULL) {
				cvi_Phrase* pNewPR = new cvi_Phrase;
				pNewPR->Message = GetTextByID(pHint->TextID.str);
				pMasDial->Phrases.Add(pNewPR);
				pNewPR=NULL;
				RH->MsgHints.Add(pMasDial);
			}else{
				delete pMasDial;
			};
		};
		pMasDial=NULL;
	};

	// Tasks List
	CSingleMessage* pTask=NULL;
	int TaskNUM = pMess->TASKS.GetAmount();
	for (int i=0; i<TaskNUM; i++){
		pMasDial=new cvi_MsgDialog;
		if (pMasDial!=NULL) {
			pTask=pMess->TASKS[i];
			if (EngSettings.MIS_SET.DonotShowComleteQuest&&pTask->Visible==false&&pTask->Deleted==true){
				pTask->Visible=true;
			};
			if (pTask!=NULL&& pTask->Visible&&pTask->TextID.str!=NULL&&pTask->TextID.str!=NULL){
				cvi_Phrase* pNewPR = new cvi_Phrase;
				pNewPR->talkerName	= GetTextByID(pTask->TalkerID.str);
				pNewPR->Message		= GetTextByID(pTask->TextID.str);
				pNewPR->Deleted		= pTask->Deleted;
				pMasDial->Phrases.Add(pNewPR);
				pNewPR=NULL;
				RH->MsgObjectives.Add(pMasDial);
			}else{
				delete pMasDial;
			};
			if (EngSettings.MIS_SET.DonotShowComleteQuest&&pTask->Visible==true&&pTask->Deleted==true){
				pTask->Visible=false;
			};
		};
		pMasDial=NULL;
	};

	return false;
};


void	RemoveDublicateInList( LinearArray<int,_int>& _list ){
	int i=0; 
	while (i<_list.GetAmount()) {
		int N = _list.GetAmount();
		while ( (--N) > i ) {
			if (_list[N]==_list[i]) {
				_list.DelElement(N);
			};
		};
		i++;
	};
};
// lvCStorage //////////////////////////////////////////////////////////////
lvCStorage::lvCStorage() {};

lvCStorage::lvCStorage(lvCStorage* pStorage){
	if (pStorage!=NULL){
		ClassName		= pStorage->ClassName.str;
		InfID			= pStorage->InfID;
		FileNameXML		= pStorage->FileNameXML.str;

		NAME			= pStorage->NAME.str;
		DESCR			= pStorage->DESCR.str;
	};
};

void	lvCStorage::vINIT (char* CLName/*=NULL*/, DWORD ID/*=0*/, char* FLName/*=NULL*/){
	ClassName	= ( (CLName==NULL) ? ("CStorage") : (CLName) );    
	InfID		= ( (ID==0) ? (_lvCStorage_) : (ID) );   
	FileNameXML	= ( (FLName==NULL) ? ("CStorage.xml") : (FLName) ); 

	NAME		= "NoName";
	DESCR		= "NoDescription";
};

void	lvCStorage::vCLEAN(){
	ClassName.Clear();
	FileNameXML.Clear();

	NAME.Clear();
	DESCR.Clear();
};

void	lvCStorage::vDeleteDynamicData(){
	return;
};

void	lvCStorage::vSetClassName		(char* name){
	if (name!=NULL) {
		ClassName = name;
	};
};

void	lvCStorage::vSetFileNameXML	(char* name){
	if (name!=NULL) {
		FileNameXML = name;
	};
};

void	lvCStorage::vSetClassID		(DWORD	_id){
	InfID = _id;
};

void	lvCStorage::vSaveXML(){
	xmlQuote	xml( ClassName.str );
	this->Save(xml,this);
	xml.WriteToFile( FileNameXML.str );
};

void	lvCStorage::vLoadXML(){
	vDeleteDynamicData();
	
	xmlQuote	xml( ClassName.str );
	xml.ReadFromFile( FileNameXML.str );
	ErrorPager	Err;
	this->Load(xml,this,&Err);
};

void	lvCStorage::vSetObjectName(char* name){
	if (name!=NULL) {
		NAME = name;
	};
};

void	lvCStorage::vSetDescription(char* description){
	if (description!=NULL) {
		DESCR = description;
	};
};

// lvCNode /////////////////////////////////////////////////////////////////
#define NODE_FONT	SmallWhiteFont1
#define pNODE_FONT	&SmallWhiteFont1
void	lvCNode::vINIT (char* CLName/*=NULL*/, DWORD ID/*=0*/, char* FLName/*=NULL*/){
	ClassName	= ( (CLName==NULL) ? ("lvCNode") : (CLName) );    
	InfID		= ( (ID==0) ? (_lvCNode_) : (ID) );   
	FileNameXML	= ( (FLName==NULL) ? ("lvCNode.xml") : (FLName) ); 

	NAME		= "NoName";
	DESCR		= "NoDescription";

	id				= 0;
	x				= 0;
	y				= 0;
};

void	lvCNode::vCLEAN(){
	x  = 0;
	y  = 0;
	id = 0;
};

void	lvCNode::vDeleteDynamicData(){
	
};

bool	lvCNode::vSetNodeXY		 (int _x, int _y){
	if (true) {
		x = _x;
		y = _y;
		return true;
	};
	return false;
};

bool	lvCNode::vSetNodeID			(DWORD _id){
	if (true) {
		id = _id;
		return true;
	};
	return	false;
};

void	lvCNode::vSetNodeName		(const char* _name){
	if (_name!=NULL) NAME = _name;
};
bool	lvCNode::vSetNode(char* name,DWORD id,int x,int y){
	bool	ret = true;
	vSetObjectName	(name);
	ret = ret && vSetNodeID		(id);
	ret = ret && vSetNodeXY		(x,y);
	return ret;
};

void	lvCNode::GetTextCoordLT(int&_x,int&_y){
	float	r	= 100.0f;
	float	dx	= 10.0f;
	float	dy	= 10.0f;

	float	mX = (float)x;
	float	mY = (float)y;
	float	mZ = (float)GetHeight(x,y);
	float	mR = (float)r;

	Vector4D	p(mX-mR+dx,mY-mR/1.5f+dy/*-260.f*/,mZ,1);
	WorldToScreenSpace(p);

	_x = (int)p.x;
	_y = (int)p.y;
};

void	lvCNode::GetTextCoordRT(int&_x,int&_y){
	float	r	= 100.0f;
	float	dx	= 10.0f;
    float	dy	= 10.0f;
	float	nR	= 20.f;
	float	dxB = 5.f;

	float	mX = (float)x;
	float	mY = (float)y;
	float	mZ = (float)GetHeight(x,y);
	if (DriveMode()->NodeStyle==1) mZ += 2.f*nR-dxB;
	float	mR = (float)r;

	Vector4D	p(mX+mR-dx,mY-mR/1.5f+dy/*-260.f*/,mZ,1);
	if (DriveMode()->NodeStyle==1) p.set(mX+dxB,mY,mZ,1);
	WorldToScreenSpace(p);

	_x = (int)p.x;
	_y = (int)p.y;
};

void	lvCNode::GetNodeRect(int&_x,int&_y,int&_x1,int&_y1){
	int		r = 100;
	
	_x	= x-r;	_y	= y-(int)((float)r/1.5f);
	_x1 = x+r;	_y1 = y+(int)((float)r/1.5f);
};

void	lvCNode::vDrawID(){
	int txX,txY;
	GetTextCoordRT(txX,txY);

	char color0[56];
	sprintf(color0,"%s%x%s","{C 0x",DriveMode()->NodeNameColor,"}");
	char color1[56];
	sprintf(color1,"%s%x%s","{C 0x",DriveMode()->NodeIDColor,"}");

	char	ID[100];
	sprintf(ID,"%s%s%s%d%s%s",color0,"ID( ",color1,id,color0," )");
	int		TextLen = GetRLen(ID,pNODE_FONT);

	extern	void ShowStringEx(int x, int y, LPCSTR lps, lpRLCFont lpf);

	if (DriveMode()->NodeStyle==0) {
		ShowStringEx(txX-TextLen,txY,ID,pNODE_FONT);
	}else if (DriveMode()->NodeStyle==1) {
		ShowStringEx(txX,txY,ID,pNODE_FONT);
	};
	
};

void	lvCNode::vDrawRect(int r/* =100 */){
	DWORD	col = DriveMode()->NodeLineColor;

	if (selected&&DriveMode()->OBJECT==7){
		if (type==0){
			col=0xFFff0000+rand()%256;
		}else if (type==1){
			col=0xFF00ff00+rand()%256;
		};
	};

	float	mX = (float)x;
	float	mY = (float)y;
	float	mZ = (float)GetHeight(x,y);
	float	mR = (float)r;

	Vector3D	p1(mX-mR,mY+mR/1.5f-10.0f,mZ);
	Vector3D	p2(mX-mR,mY+mR/1.5f,mZ);
	Vector3D	p3(mX-mR+10.0f,mY+mR/1.5f,mZ);
	GPS.DrawLine(p1,p2,col);
	GPS.DrawLine(p2,p3,col);

	p1.set(mX+mR-10.0f,mY+mR/1.5f,mZ);
	p2.set(mX+mR,mY+mR/1.5f,mZ);
	p3.set(mX+mR,mY+mR/1.5f-10.0f,mZ);
	GPS.DrawLine(p1,p2,col);
	GPS.DrawLine(p2,p3,col);

	p1.set(mX+mR,mY-mR/1.5f+10.0f,mZ);
	p2.set(mX+mR,mY-mR/1.5f,mZ);
	p3.set(mX+mR-10.0f,mY-mR/1.5f,mZ);
	GPS.DrawLine(p1,p2,col);
	GPS.DrawLine(p2,p3,col);

	p1.set(mX-mR+10.0f,mY-mR/1.5f,mZ);
	p2.set(mX-mR,mY-mR/1.5f,mZ);
	p3.set(mX-mR,mY-mR/1.5f+10.f,mZ);
	GPS.DrawLine(p1,p2,col);
	GPS.DrawLine(p2,p3,col);
};

void	lvCNode::vDrawBuoy(){
	DWORD	color = DriveMode()->NodeLineColor;

	if (selected&&DriveMode()->OBJECT==7){
		if (type==0){
			color=0xFFff0000+rand()%256;
		}else if (type==1){
			color=0xFF00ff00+rand()%256;
		};
	};

	float x0 = (float)x;
	float y0 = (float)y;
    
	float		nR = 20.f;
	Vector3D	pNn[6];
	for (int i=0; i<6; i++){
		pNn[i].set(	Shapes_MAP(nR)->regular_polygon_6D[i]->x+x0,
					Shapes_MAP(nR)->regular_polygon_6D[i]->y+y0,
					(float)GetHeight(	(int)Shapes_MAP(nR)->regular_polygon_6D[i]->x+x0,
										(int)Shapes_MAP(nR)->regular_polygon_6D[i]->y+y0	)
					);
	};	
	GPS.DrawLine(pNn[0],pNn[1],color);
	GPS.DrawLine(pNn[1],pNn[2],color);
	GPS.DrawLine(pNn[2],pNn[3],color);
	GPS.DrawLine(pNn[3],pNn[4],color);
	GPS.DrawLine(pNn[4],pNn[5],color);
	GPS.DrawLine(pNn[5],pNn[0],color);

	Vector3D	pNF[5];
	pNF[0].set(x0,y0,(float)GetHeight((int)x0,(int)y0));
	pNF[1].set(x0,y0,(float)GetHeight((int)x0,(int)y0)+2.f*nR);
	pNF[2].set(x0+3.f*nR,y0,(float)GetHeight((int)(x0+3.f*nR),(int)y0)+2.f*nR);
	pNF[3].set(x0+3.f*nR,y0,(float)GetHeight((int)(x0+3.f*nR),(int)y0)+nR);
	pNF[4].set(x0,y0,(float)GetHeight((int)x0,(int)y0)+nR);
	GPS.DrawLine(pNF[0],pNF[1],color);
	GPS.DrawLine(pNF[1],pNF[2],color);
	GPS.DrawLine(pNF[2],pNF[3],color);
	GPS.DrawLine(pNF[3],pNF[4],color);
};

bool	lvCNode::InSector(float Fi){
	float		fDir=((float)Dir/256.f)*2.f*3.1415f;
	float		fSegmFR_Half=((float)SegmFR/256.f)*3.1415f;
	FI_2Pi(fDir);
//	FI_2Pi(fSegmFR_Half);
	return	(bool)( min(abs(abs(Fi)-abs(fDir)),abs(abs(abs(Fi)-abs(fDir))-2.f*3.1415f)) <= abs(fSegmFR_Half) );
};
void	lvCNode::vDrawCircle(){
	if (R<=0)	return;
	
	DWORD	color = DriveMode()->NodeLineColor;

	float		fx=(float)x;
	float		fy=(float)y;
	float		fR=(float)R;

	float		fDir=((float)Dir/256.f)*2.f*3.1415f;
	float		fSegmFR_Half=((float)SegmFR/256.f)*3.1415f;

	float SF1 = ( (SegmFR>0) ? (fDir-fSegmFR_Half) : (0.f) );
	float SF2 = ( (SegmFR>0) ? (fDir+fSegmFR_Half) : (0.f) );

	if (SegmFR>0) {
		// Set Sector Fi in [0,2*Pi);
		FI_2Pi(SF1);
		FI_2Pi(SF2);
	};

	float SegmD = 5.f;						// pixels for one segment
	float SegmF = SegmD/fR;					// segment angle
	int	  SegmN = (int)(2.f*3.1415f/SegmF);	// N of segments

	Vector3D	p1,p2,p0,SEN;
	SEN.set((float)x,(float)y,(float)GetHeight(x,y));
	
	for (int i=0; i<SegmN; i++){
        p1.set	(	fx+fR*cosf((float)i*SegmF),
					fy+fR*sinf((float)i*SegmF),
					(float)GetHeight(	(int)(fx+fR*cosf((float)i*SegmF)),
										(int)(fy+fR*sinf((float)i*SegmF))	)	
				);
		p2.set	(	fx+fR*cosf((float)(i+1)*SegmF),
					fy+fR*sinf((float)(i+1)*SegmF),
					(float)GetHeight(	(int)(fx+fR*cosf((float)(i+1)*SegmF)),
										(int)(fy+fR*sinf((float)(i+1)*SegmF))	)	
				);

		if (SegmFR>0) {
			float F1=(float)i*SegmF;
			float F2=(float)(i+1)*SegmF;
			if ( InSector(F1) && InSector(F2) ){
				GPS.DrawLine(p1,p2,color);	
			};
			if ( InSector(F2) && !InSector(F1) ){
				GPS.DrawLine(p2,SEN,color);	
			};
			if ( InSector(F1) && !InSector(F2) ){
				GPS.DrawLine(SEN,p1,color);	
			};
		}else{		
			GPS.DrawLine(p1,p2,color);
		};

		if (i==0&&SegmFR<=0)		p0.set(p1.x,p1.y,p1.z);
		if (i==SegmD-1&&SegmFR<=0)	GPS.DrawLine(p0,p2,color);
	};

	// Draw Direction
	Vector3D ForDir(fx+(fR+50.f)*cosf(fDir),fy+(fR+50.f)*sinf(fDir),(float)GetHeight((int)(fx+50.f*cosf(fDir)),(int)(fy+50.f*sinf(fDir))));
	GPS.DrawLine(SEN,ForDir,color);
};
void	lvCNode::vDrawXY(DWORD pos/* =0 */,int dy/* =10 */,int dx/* =10 */,int r/* =100 */){
	int txX,txY;
	GetTextCoordLT(txX,txY);

	char color[56];
	sprintf(color,"%s%x%s","{C 0x",DriveMode()->NodeNameColor,"}");
	
	char	XY[100];
	sprintf(XY,"%s%s%d%s%d%s",color,"X( ",x," ) : Y( ",y," )");
	extern	void ShowStringEx(int x, int y, LPCSTR lps, lpRLCFont lpf);
	ShowStringEx(txX,txY+pos*10,XY,pNODE_FONT);
};

void	lvCNode::vDrawNAME(DWORD pos/* =1 */,int dy/* =30 */,int dx/* =10 */){
	int txX,txY;
	GetTextCoordLT(txX,txY);
	
	char color[56];
	sprintf(color,"%s%x%s","{C 0x",DriveMode()->NodeNameColor,"}");

	char	name[100];
	sprintf(name,"%s%s",color,NAME.str);
	extern	void ShowStringEx(int x, int y, LPCSTR lps, lpRLCFont lpf);
	ShowStringEx(txX,txY+pos*10,name,pNODE_FONT);
};

void	lvCNode::vDrawDESCR(DWORD pos/* =2 */,int dy/* =50 */,int dx/* =10 */){
	int txX,txY;
	GetTextCoordLT(txX,txY);

	char color[56];
	sprintf(color,"%s%x%s","{C 0x",DriveMode()->NodeNameColor,"}");

	char	descr[150];
	sprintf(descr,"%s%s",color,DESCR.str);
	extern	void ShowStringEx(int x, int y, LPCSTR lps, lpRLCFont lpf);
	ShowStringEx(txX,txY+pos*10,descr,pNODE_FONT);
};

void	lvCNode::vDraw(DWORD mode /* =1 */){
	int pos = 0;

	if (!IsOnScreen(x,y,150,150))	return;

	if (DriveMode()->NodeStyle==0) {
		if (mode&_RECT_){
			vDrawRect();
			vDrawCircle();
		};
		if (Shifter==5){
			if (mode&_XY_)	  { vDrawXY(pos++); vDrawID(); };
			if (mode&_NAME_)	vDrawNAME(pos++);
			if (mode&_DESCR_)	vDrawDESCR(pos++);
		};
	}else if (DriveMode()->NodeStyle==1) {
		if (mode&_RECT_){
			vDrawBuoy();
			vDrawCircle();
		};
		if (Shifter==5) {
			if (mode&_NAME_)	vDrawNAME(pos);
			if (mode&_XY_)	  { vDrawID(); };
		};
	};
};

extern	int ADDSH;
int GetMapSize(){
	return	( 240 << ADDSH )*32;
};

void	lvCNode::vSetX(int _x){
	int	DX = 100;
	if (DX+50<_x&&_x<(GetMapSize()-DX)) {
		x = _x;
	};
};
void	lvCNode::vSetY(int _y){
	int	DY = 150;
	if (DY+200<_y&&_y<(GetMapSize()-DY)) {
		y = _y;
	};
};
void	lvCNode::vSetXY(int _x,int _y){
	vSetX(_x);
	vSetY(_y);
};
void	lvCNode::vSetR		(int _R){
	if (0<=_R)	R=_R;
	};
void	lvCNode::vSetDir		(int _Dir){
	if (0<=_Dir&&_Dir<=255)	Dir=_Dir;
	};
void	lvCNode::vSetSegmFR	(int _SegmFR){
	if (0<=_SegmFR&&_SegmFR<=255)	SegmFR=_SegmFR;
};
void	lvCNode::vSetGParam	(int _x,int _y,int _R,int _Dir,int _SegmFR){
	vSetXY		(_x,_y);
	vSetR		(_R);
	vSetDir		(_Dir);
	vSetSegmFR	(_SegmFR);
};

void	lvCNode::vAddX(int dx/* =1 */){
	int	DX = 100;
	
	x += dx;
	if (x<DX+50&&(GetMapSize()-DX)<x) {
		x -= dx;
	};
};

void	lvCNode::vAddY(int dy/* =1 */){
	int	DY = 150;
	
	y += dy;
	if (y<DY+200&&(GetMapSize()-DY)<y) {
		y -= dy;
	};
};

void	lvCNode::vAddXY(int dx/* =1 */,int dy/* =1 */){
	vAddX(dx);
	vAddY(dy);
};

float	lvCNode::vGetNodeDist(int _x,int _y){
	return	sqrtf((float)(x-_x)*(float)(x-_x)+(float)(y-_y)*(float)(y-_y));
};


int		lvCNode::vGetX()		const{
	return x;
};
int		lvCNode::vGetY()		const{
	return y;
};
int		lvCNode::vGetR()		const{
	return R;
};
int		lvCNode::vGetDir()		const{
	return Dir;
};
int		lvCNode::vGetSegmFR()	const{
	return SegmFR;
};
char*	lvCNode::vGetName()		const{
	return NAME.str;
};
// CNodesMAP_ST /////////////////////////////////////////////////////////////
void		lvCNodesMAP_ST::vINIT (char* CLName/* =NULL */,DWORD ID/* =0 */,char* FLName/* =NULL */){
	ClassName	= ( (CLName==NULL) ? ("lvCNodesMAP_ST") : (CLName) );    
	InfID		= ( (ID==0) ? (_lvCNodesMAP_ST_) : (ID) );   
	FileNameXML	= ( (FLName==NULL) ? ("lvCNodesMAP_ST.xml") : (FLName) ); 

	NAME		= "NoName";
	DESCR		= "NoDescription";
};

void		lvCNodesMAP_ST::vCLEAN(){

};

void		lvCNodesMAP_ST::vDeleteDynamicData(){
	NodesArray.Clear();
};

lvCNode*	lvCNodesMAP_ST::vAddNode(int x,int y,char* name/* =NULL */,char* descr/* =NULL */){
	int N = NodesArray.GetAmount();
	DWORD	newID = 0;
	while (N--) {
		if (NodesArray[N]->id>newID)	newID = NodesArray[N]->id;
	};
	newID++;
	lvCNode* pND = new lvCNode();
	pND->vINIT();
	pND->vSetNodeID(newID);
	pND->vSetNodeXY(x,y);
	if (name!=NULL)		pND->vSetObjectName(name);
	if (descr!=NULL)	pND->vSetDescription(descr);
	NodesArray.Add(pND);
//	pND=NULL;
	
	return	pND;
};

bool		lvCNodesMAP_ST::vDelNodeID(DWORD _id){
	int N = NodesArray.GetAmount();
	while (N--) {
		if (NodesArray[N]->id==_id) {
			NodesArray.Del(N,1);
			N=0;
			return true;
		};
	};
	return	false;
};

bool		lvCNodesMAP_ST::vDelNodePT(lvCNode* pND){
	int N = NodesArray.GetAmount();
	while (N--) {
		if (NodesArray[N]==pND) {
			NodesArray.Del(N,1);
			N=0;
			return true;
		};
	};
	return	false;
};

bool		lvCNodesMAP_ST::vDelNode(int _x,int _y,DWORD sqID/* =0xFFFFFFFF */){
	lvCNode* pND = vGetNode(_x,_y);
	return	vDelNodePT(pND);
};

void		lvCNodesMAP_ST::vDraw(DWORD mode/* =1 */,DWORD _id/* =0xFFffFFff */){
	int N = NodesArray.GetAmount();
	while (N--) {
		if (_id==0xFFffFFff||(NodesArray[N]->id==_id)) {
			NodesArray[N]->vDraw(mode);
		};
	};
};

lvCNode*	lvCNodesMAP_ST::vGetNode(DWORD _id){
	lvCNode* pRET = NULL;
	int N = NodesArray.GetAmount();
	while (N--) {
		if (NodesArray[N]->id==_id) {
			pRET = NodesArray[N];
		};
	};
	return	pRET;
};
lvCNode*	lvCNodesMAP_ST::vGetNode(const char* name){
	lvCNode* pRET = NULL;
	if (name!=NULL){
		int N = NodesArray.GetAmount();
		while (N--&&pRET==NULL) {
			if (strcmp(NodesArray[N]->NAME.str,name)==0) {
				pRET = NodesArray[N];
			};
		};
	};
	return	pRET;
};
lvCNode*	lvCNodesMAP_ST::vGetNode(int _x,int _y,DWORD sqID/* =0xFFFFFFFF */){
	const	float MIN_DIST	= 300.f;

	lvCNode* pRET = NULL;
	int N = NodesArray.GetAmount();
	if (N>0){
		pRET			= NodesArray[0];
		float minDist	= NodesArray[0]->vGetNodeDist(_x,_y);
		float dist		= 0.f; 
		while (N--) {
			dist = NodesArray[N]->vGetNodeDist(_x,_y);
			if (minDist>dist){
				minDist = dist;
				pRET = NodesArray[N];
			};
		};
		if (minDist>MIN_DIST) {
			pRET = NULL;
		};
	};
	return	pRET;
};

lvCNode* __getNodeByName(const char* nodeName){
	return	NodesMap()->vGetNode(nodeName);
};
lvCNode* __getNodeByID(const int nodeID){
	return	NodesMap()->vGetNode(nodeID);
};
// lvCNodesOnMap ////////////////////////////////////////////////////////
DWORD lvCNodesOnMap::GetValue(const char* ID){
	lvCNode* pNODE = NodesMap()->vGetNode(ID);
	if (pNODE!=NULL)	return pNODE->id;	
	return 0;
};
char* lvCNodesOnMap::GetValue(DWORD ID){
	lvCNode* pNODE = NodesMap()->vGetNode(ID);
	if (pNODE!=NULL)	return pNODE->vGetName();	
	return NULL;
};
int   lvCNodesOnMap::GetAmount(){
	return NodesMap()->NodesArray.GetAmount();
};
char* lvCNodesOnMap::GetIndexedString(int idx){
	if (idx<0||GetAmount()<=idx)	return NULL;
	return NodesMap()->NodesArray[idx]->vGetName();
};
DWORD lvCNodesOnMap::GetIndexedValue(int idx){
	if (idx<0||GetAmount()<=idx)	return 0xFFFF;
	return NodesMap()->NodesArray[idx]->id;
};
// lvCGroup //////////////////////////////////////////////////////////////
#define vGROUP_FONT		SmallWhiteFont1
#define pvGROUP_FONT	&SmallWhiteFont1
			void CopyReIm(byte NI);
			void MakeReformation(byte NI,word BrigadeID,byte FormType);
			void AddUnitToSelected(byte NI,OneObject* OB);
			void RotUnit(OneObject* OB,char Dir,byte OrdType);
DLLEXPORT	void SelChangeNation(byte SrcNat,byte DstNat);
DLLEXPORT	void ClearSelection(byte Nat);
void ClearBMASK();
extern bool BuildMode;
lvCGroup::lvCGroup(){
	visible=true; 
	ORDER=vgNO_ORDERS; 
	ORDER_STATE=vgOR_COMPLIT; 
	newElement=false; 
	NofNE=false; 
	reSized=true; 
	NUnit=0;
	// lua
#ifdef __LUA__
    UseByLua=0; 
	lua_error=false;
#endif
};
lvCGroup::lvCGroup(const char* name){
	visible=true; 
	ORDER=vgNO_ORDERS; 
	ORDER_STATE=vgOR_COMPLIT; 
	newElement=false; 
	NofNE=false; 
	reSized=true; 
	NUnit=0;
	// lua
#ifdef __LUA__
	UseByLua=0; 
	lua_error=false;
#endif
	vINIT();
	if (name!=NULL) NAME = name;
	SetID(0xfffe);
};
lvCGroup::~lvCGroup(){
	vDeleteDynamicData();
	vCLEAN();		
};
void				lvCGroup::vINIT (char* CLName/*=NULL*/, DWORD ID/*=0*/, char* FLName/*=NULL*/){
	ClassName	= ( (CLName==NULL) ? ("lvCGroup") : (CLName) );    
	InfID		= ( (ID==0) ? (_lvCGroup_) : (ID) );   
	FileNameXML	= ( (FLName==NULL) ? ("lvCGroup.xml") : (FLName) ); 

	vCLEAN();
};

void				lvCGroup::vCLEAN(){
	NAME		= "NoName";
	DESCR		= "NoDescription";
};

void				lvCGroup::vDeleteDynamicData(){
	IDS.Clear();
	SNS.Clear();
	NUnit=0;
	Units.Clear();
};

en_vGroup_Order		lvCGroup::GetORDER(){
//	RemoveDeadUnits();
	int N = GetTotalAmount();
	int inMOVE = 0;
	int inATTACK = 0;
	int inHAVE_SOME_ORDER = 0;
	int inBRIGADE_ORDER = 0;
	OneObject* pOB = NULL;
	while (N--) {
		pOB = Group[IDS[N]];
		if (pOB&&(!pOB->Sdoxlo||pOB->Hidden)&&pOB->Serial==SNS[N]) {
			if (pOB->Attack && pOB->EnemyID<0xFFFE)						inATTACK++;
			if (pOB->DestX>=0)											inMOVE++;
			if (pOB->LocalOrder!=NULL/*&&pOB->LockType!=1*/)			inHAVE_SOME_ORDER++;
			if (pOB->BrigadeID!=0xFFFF ){
				Brigade* BR=CITY[pOB->NNUM].Brigs+pOB->BrigadeID;
				if (BR && BR->BOrder!=NULL)	inBRIGADE_ORDER++;
			};
		};
	};
	if (inATTACK>0)				return vgATTACK;
	if (inMOVE>0)				return vgMOVE;
	if (inHAVE_SOME_ORDER>0)	return vgSOME_ORDER;
	if (inBRIGADE_ORDER>0)		return vgBRIG_ORDER;
								return vgNO_ORDERS;
};
en_vGroup_Order		lvCGroup::GetUnitORDER(int uid){
	en_vGroup_Order	order=vgNO_ORDERS;
	int N = GetTotalAmount();
	if (0<=uid&&uid<N){
		OneObject* pOB=GetOneObj_lID(uid);
		if (pOB!=NULL) {
			if (pOB->Attack && pOB->EnemyID<0xFFFE)						order=vgATTACK;
			if (pOB->DestX>=0)											order=vgMOVE;
			if (pOB->LocalOrder!=NULL/*&&pOB->LockType!=1*/)			order=vgSOME_ORDER;
			if (pOB->BrigadeID!=0xFFFF ){
				Brigade* BR=CITY[pOB->NNUM].Brigs+pOB->BrigadeID;
				if (BR && BR->BOrder!=NULL)								order=vgBRIG_ORDER;
			};
		};
	};
	return	order;
};// Текущий приказ для юнита в локальном списке
void				lvCGroup::RemoveDeadUnits(){
	if (!reSized)	return;
	int N = IDS.GetAmount();
	if (N==0)	return;
	while (N--)	{
        word ids = IDS[N]; 
        if (ids<0||ids>=0xFFFF) {
		//	RemUnitID(N);
			IDS.Del(N,1);
			SNS.Del(N,1);
		}else{
			const OneObject* pOB = Group[ids];
			if (pOB==NULL || pOB->Sdoxlo || pOB->Serial!=SNS[N]) {
			//	RemUnitID(N);
				IDS.Del(N,1);
				SNS.Del(N,1);
			};
		};
	};
	reSized=false;
	gg_VGrpReSize=false;
};

void				lvCGroup::ScreenOnGroup(){
	void SetCameraPos(float x,float y);
	int	  gx=0,  gy=0;
	GetGroupCenter(gx,gy);
	if (gx!=0||gy!=0) {
		float cx=0.f,cy=0.f;
		cx = ((float)gx/32.f)-(float)smaplx/2.f;
		cy = ((float)gy/32.f)-(float)smaply/2.f;
		if (abs(cx-fMapX)>=1.f/32.f || abs(cy-fMapY)>=1.f/32.f) {
			SetCameraPos(cx,cy);
		};
	};
};

void				lvCGroup::DrawGroup(){
//	RemoveDeadUnits();
	if (visible==false)	return;
	DWORD color = DriveMode()->vGroupLineColor;
	int x,y;
	GetGroupCenter(x,y);
	static float uH = 50.f;		//+++
	static float tH = 150.f;	//+++
	static float uR = 15.f;		//+++
	static float dy = -10.f;	//+++
	if (x!=0||y!=0) {	
		int N = GetTotalAmount();
		OneObject* pOB = NULL;

		bool	onTOP = true;
		float tcX = (float)GetMinX()-100.f;
		float tcY = (float)GetMinY()-100.f;
		if (tcX-500.f<0)		tcX = (float)GetMaxX()+100.f;
		if (tcY-500.f<0)	  {	tcY = (float)GetMaxY()+100.f; onTOP = false; };
        Vector3D	p0(tcX,tcY,(float)GetHeight(tcX,tcY)+uH+tH);
		
		while (N--) {
			pOB = Group[IDS[N]];
			if (pOB!=NULL&&(!pOB->Sdoxlo||pOB->Hidden)&&pOB->Serial==SNS[N]) {
				float x0 = (float)(pOB->RealX>>4);
				float y0 = (float)(pOB->RealY>>4)+dy;
				//Shapes_MAP(20.f)->regular_polygon_6D
				Vector3D	pNn[6];
				for (int i=0; i<6; i++){
					pNn[i].set(	Shapes_MAP(uR)->regular_polygon_6D[i]->x+x0,
								Shapes_MAP(uR)->regular_polygon_6D[i]->y+y0,
								(float)GetHeight(	(int)Shapes_MAP(uR)->regular_polygon_6D[i]->x+x0,
													(int)Shapes_MAP(uR)->regular_polygon_6D[i]->y+y0	)
								);
				};
				for (int i=0; i<6; i++) pNn[i]=SkewPt(pNn[i].x,pNn[i].y,pNn[i].z);
				GPS.DrawLine(pNn[5],pNn[0],color);
				GPS.DrawLine(pNn[0],pNn[1],color);
				GPS.DrawLine(pNn[1],pNn[2],color);
				GPS.DrawLine(pNn[2],pNn[3],color);
				GPS.DrawLine(pNn[3],pNn[4],color);
				
				Vector3D	pHP(pNn[0].x,pNn[0].y,pNn[0].z+uH);
				GPS.DrawLine(pNn[0],pHP,color);

				GPS.DrawLine(p0,pHP,color);
			};
		};
		Vector3D	pT;
		if (onTOP) {
			pT.set(p0.x,p0.y+25,p0.z+25.f);
		}else{
			pT.set(p0.x,p0.y-25,p0.z-25.f);
		};
		GPS.DrawLine(p0,pT,color);

		Vector3D	pL(pT.x-50.f,pT.y,pT.z);
		Vector3D	pR(pT.x+50.f,pT.y,pT.z);
		GPS.DrawLine(pL,pR,color);

		Vector4D	pSTR;
		if (onTOP) {
			pSTR.set(pL.x+10.f,pL.y/*-260.f*/-20.f,pL.z,1);
		}else{
			pSTR.set(pL.x+10.f,pL.y/*-260.f*/+20.f,pL.z,1);
		};
		WorldToScreenSpace(pSTR);
		
		char titleSTR[256];
		char color[56];
		sprintf(color,"%s%x%s","{C 0x",DriveMode()->vGroupNameColor,"}");
		sprintf(titleSTR,"%s%s%s%d%s",color,NAME.str," [",GetTotalAmount(),"]");
		void ShowStringEx(int x, int y, LPCSTR lps, lpRLCFont lpf);
		ShowStringEx(pSTR.x,pSTR.y,titleSTR,pvGROUP_FONT);
	};
};

void				lvCGroup::SetGroupName(char* name){
	lvCStorage::vSetObjectName(name);
};

char*				lvCGroup::GetGroupName(){
	return	NAME.str;
};

void				lvCGroup::SetGroupDescription(char* descr){
	lvCStorage::vSetDescription(descr);
};

char*				lvCGroup::GetGroupDescription(){
	return	DESCR.str;
};

void				lvCGroup::SetID(int _id){
	id = _id;
};

word				lvCGroup::_CheckGID(word id){
	word	present = 0xFFFF;
	int N = GetTotalAmount();
	while (present==0xFFFF&&N--) {
		if (IDS[N]==id) {
			present = N;
		};
	};
	return present;
};

word				lvCGroup::_CheckGPT(OneObject* pOB){
	word present = 0xFFFF;
	if (pOB!=NULL){
		present = _CheckGID(pOB->Index);
		if (present!=0xFFFF) {
			if (SNS[present]!=pOB->Serial) {
				IDS.Del(present,1);
				SNS.Del(present,1);
				present=0xFFFF;
			};
		};
	};
	reSized=true;
	GetTotalAmount();
	return present;
};

OneObject*			lvCGroup::GetOneObj_gID(word gID){
	OneObject* pOB = NULL;
	int N = GetTotalAmount();
	bool myObj = false;
	while (!myObj&&N--) {
		if (IDS[N]==gID)	myObj = true;
	};
	if (myObj && gID!=0xFFFF){
		pOB = Group[gID];
		if ((pOB && (!pOB->Sdoxlo||pOB->Hidden) && pOB->Serial==SNS[N])==false) {
			pOB = NULL;
		};
	}
	return pOB;
};

OneObject*			lvCGroup::GetOneObj_lID(word lID){
	OneObject* pOB = NULL;
	int N = GetTotalAmount();
	if (0<=lID&&lID<N&&IDS[lID]!=0xFFFF){
		pOB = Group[IDS[lID]];
		if ( (pOB&&(!pOB->Sdoxlo||pOB->Hidden)&&pOB->Serial==SNS[lID]) == false ) {
			pOB = NULL;
		};
	};
	return pOB;
};

void				lvCGroup::AddUnitGID(word id){
	if (id<0||id>=0xFFFF)	return;
	AddUnitGPT(Group[id]);
};

void				lvCGroup::AddUnitGPT(OneObject* pOB){
	if (pOB!=NULL && (!pOB->Sdoxlo||pOB->Hidden)) {
		if (_CheckGPT(pOB)==0xFFFF) {
			IDS.Add(pOB->Index);
			SNS.Add(pOB->Serial);
			newElement=true;
			reSized=true;
			GetTotalAmount();
			NofNE++;
		};
	};
};

void				lvCGroup::AddUnitsFromAGroupID(word id){
	int AGroupsN = AGroups.GetAmount();
	if (id<0||AGroupsN<=id)	return;

	int UnitsN = AGroups[id]->Units.GetAmount();
	OneObject* pOB = NULL;
    while (UnitsN--) {
		AddUnitGID(AGroups[id]->Units[UnitsN].ID);
    };
};

void				lvCGroup::AddUnitsFromAGroupNM(char* name){
	int N = AGroups.GetAmount();
	int AGrpID = -1;
	while (AGrpID==-1&&N--) {
		if (strcmp(AGroups[N]->Name.str,name)==0) {
			AGrpID = N;
		};
	};
	AddUnitsFromAGroupID(AGrpID);
};

void				lvCGroup::AddSelectedUnits(byte NI/*=0xFF*/){
//	RemoveDeadUnits();
	byte curNI = MyNation;
	if (NI!=0xFF)	curNI=NI;
	int selN = ImNSL[curNI];
	if (selN==0)	return;
	word* pSelIDS = ImSelm[curNI];
	for (int i=0; i<selN; i++){
		if (_CheckGID(pSelIDS[i])==0xFFFF){
			AddUnitGID(pSelIDS[i]);
		};
	};

	// Add comandirs to group if "COSSACS II"
	if (EngSettings.GameName==1){
		LinearArray<int,_int> BrigList;
		GetBrigadeList(true,&BrigList);
		int NB=BrigList.GetAmount();
		Brigade* pBR=NULL;
		int Nat=GetNation();

		for (int i=0; i<NB; i++){
			pBR=&CITY[Nat].Brigs[ BrigList[i] ];
			if (pBR!=NULL&&pBR->NMemb>=2) {
				if (pBR->Memb[0]!=0xFFFF) AddUnitGID(pBR->Memb[0]);
				if (pBR->Memb[1]!=0xFFFF) AddUnitGID(pBR->Memb[1]);
				if (pBR->Memb[2]!=0xFFFF) AddUnitGID(pBR->Memb[2]);
			};
		};
	};
};

void				lvCGroup::AddBrigad(int BrigID, int Nat){
	if (0<=BrigID&&BrigID<MaxBrig-11) {
		if (CITY[Nat].Brigs[BrigID].Enabled){
			for (int i=0; i<CITY[Nat].Brigs[BrigID].NMemb; i++){
				AddUnitGID(CITY[Nat].Brigs[BrigID].Memb[i]);
			};
		};
	};
};

void				lvCGroup::RemUnitGID(word id){
	word a_id = _CheckGID(id);
	if (a_id!=0xFFFF) {
		RemUnitID(a_id);
	};
};

void				lvCGroup::RemUnitGPT(OneObject* pOB){
	word a_id = _CheckGPT(pOB);
	if (a_id!=0xFFFF) {
		RemUnitID(a_id);
	};
};

void				lvCGroup::RemUnitID(word id){
	if (0<=id&&id<IDS.GetAmount()) {
		IDS.Del(id,1);
		SNS.Del(id,1);
	};
	reSized=true;
	GetTotalAmount();
};

void				lvCGroup::RemAllUnits(){
	while (GetTotalAmount()) {
		RemUnitID(0);
	};
};

int					lvCGroup::RemoveNUnitsToCGroup(lvCGroup* pGR,int N/*=0xFFFF*/){
	if (pGR==NULL)	return	0;
	int remN = GetTotalAmount();
	if (N!=0xFFFF)	remN = min(remN,N);
	int retVAL = 0;
	OneObject* pOB=NULL;
	while(remN--){
		pOB = Group[ IDS [ remN ] ];
		if (pOB!=NULL && (!pOB->Sdoxlo||pOB->Hidden)) {
			int ids = pGR->_CheckGID(pOB->Index);
			if (ids==0xFFFF || ( ids!=0xFFFF && pGR->SNS[ids]!=SNS[remN] )) {
				pGR->IDS.Add(pOB->Index);
				pGR->SNS.Add(pOB->Serial);
				retVAL++;
			};
		};
		IDS.Del(remN,1);
		SNS.Del(remN,1);
	};
	reSized=true;
	GetTotalAmount();
	pGR->reSized=true;
	pGR->GetTotalAmount();
	return retVAL;
};

int					lvCGroup::RemoveNUnitsToAGroupID(int id, int N/*=0xFFFF*/){
	return 0;
};

int					lvCGroup::RemoveNUnitsToAGroupNM(char* name, int N/*=0xFFFF*/){
	return 0;
};

int					lvCGroup::KillUnits(int N/*=0xFFFF*/){
	int killN = GetTotalAmount();
	if (N!=0xFFFF)	killN = min(killN,N);
	int retVAL = 0;
	OneObject* pOB = NULL;
	while (killN--/*&&GetTotalAmount()>0*/) {
		pOB = Group[IDS[killN]];
		if(pOB&&pOB->Serial==SNS[killN]){
			void DestructBuilding(OneObject* OB);
			DestructBuilding(pOB);
			RemUnitID(killN);
			retVAL++;
		};
		pOB = NULL;
	};

	reSized=true;
	GetTotalAmount();
	return retVAL;
};

int					lvCGroup::EraseUnits(int N/*=0xFFFF*/){
	int killN = GetTotalAmount();
	if (N!=0xFFFF)	killN = min(killN,N);
	int retVAL = 0;
	OneObject* pOB = NULL;
	while (killN--/*&&GetTotalAmount()>0*/) {
		pOB=Group[IDS[killN]];
		if(pOB){
			pOB->delay=6000;
			if(pOB->BrigadeID!=0xFFFF){
				Brigade* BR=CITY[pOB->NNUM].Brigs+pOB->BrigadeID;
				BR->vLastEnemyID=0xFFFE;
			}
			if(pOB->LockType==1)pOB->RealDir=32;
			pOB->Die();
			pOB=Group[IDS[killN]];
			if(pOB){				
				pOB->Sdoxlo=2500;
				if(pOB->NewBuilding){
					void EliminateBuilding(OneObject* OB);
					EliminateBuilding(pOB);
					Group[IDS[killN]]=NULL;
					RemUnitID(killN);
					retVAL++;
				};
			};
		};
		pOB = NULL;
	};
	reSized=true;
	GetTotalAmount();
	return retVAL;
};

void				lvCGroup::SetNation(byte NI){
	lvCGroup* pOldSel = new lvCGroup();
	pOldSel->AddSelectedUnits(GetNation());
    SelectUnits(0);
	SelChangeNation(GetNation(),NI);
	pOldSel->SelectUnits(0);
	delete pOldSel;
};

void				lvCGroup::SelectUnits(bool add/*=0*/){
	if (!add) UnSelect();
	byte NI = GetNation();
	if (0<=NI&&NI<7) {
		int N = GetTotalAmount();
		OneObject* pOB=NULL;
		while (N--) {
			pOB = Group[IDS[N]];
			if(pOB&&pOB->NNUM==NI&&(!pOB->Sdoxlo||pOB->Hidden)&&pOB->Serial==SNS[N]){
				AddUnitToSelected(NI,pOB);
			};
		};
		CopyReIm(NI);
	};
};

void				lvCGroup::SelectUnitsInZone(int x, int y, int R,bool add/*=false*/){
	if (!add) UnSelect();
	byte NI = GetNation();
	if (0<=NI&&NI<7) {
		int N = GetTotalAmount();
		OneObject* pOB=NULL;
		while (N--) {
			pOB = Group[IDS[N]];
			if(pOB&&pOB->NNUM==NI&&(!pOB->Sdoxlo||pOB->Hidden)&&pOB->Serial==SNS[N]){
				if (Norma((pOB->RealX>>4)-x,(pOB->RealY>>4)-y)<=R) {
					AddUnitToSelected(NI,pOB);
				};
			};
		};
		CopyReIm(NI);
	};
};

void				lvCGroup::UnSelect(){
	byte NI = GetNation();
	if (0<=NI&&NI<7) {
		ClearSelection(NI);
	};
};

void				lvCGroup::SendTo(int x, int y, int dir, int type/*=0*/){
	if (GetTotalAmount()==0)	return;
	int natGRP = GetNation();
	if (natGRP<0||natGRP>7) return;
	lvCGroup* pOldSel = new lvCGroup();
	pOldSel->AddSelectedUnits(natGRP);
	ClearSelection(natGRP);
	SelectUnits(0);
	void SendSelectedToXY(byte NI,int xx,int yy,short Dir,byte Prio,byte Type);
	SendSelectedToXY(natGRP,x<<4,y<<4,dir,16,type);
	ClearSelection(natGRP);
	pOldSel->SelectUnits(0);
	delete pOldSel;
};

void				lvCGroup::ChangeDirection(int dir,int type/*=0*/){
//	RemoveDeadUnits();
	int x = 0;
	int y = 0;
	GetGroupCenter(x,y);
	SendTo(x,y,dir,type);
	/*
	int N = IDS.GetAmount();
	OneObject* pOB = NULL;
	while (N--) {
		if (0<=IDS[N]&&IDS[N]<0xFFFF){
			pOB = Group[IDS[N]];
			if (pOB&&(!pOB->Sdoxlo||pOB->Hidden)&&pOB->Serial==SNS[N]) {
				RotUnit(pOB,dir,type);
			};
		};
	};
	*/
};

bool				lvCGroup::SetAgresiveST(int stID){
	if (stID<0||stID>2) return false;
//	RemoveDeadUnits();
	int N = GetTotalAmount();
	OneObject* pOB = NULL;
	int trueN = 0;
	while (N--) {
		pOB = GetOneObj_lID(N);
		if (pOB) {
			if (pOB->ActivityState!=stID){
				trueN++;
				pOB->ActivityState=stID;
				pOB->GroundState=pOB->ActivityState==2;
				if(pOB->NewState!=pOB->GroundState)pOB->NewState=pOB->GroundState;
			};
		};
	};	
	if (trueN==0)	return false;
	if(pOB&&pOB->BrigadeID!=0xFFFF){
		if(EngSettings.AutoChangeFormationType){
			Brigade* BR=CITY[pOB->NNUM].Brigs+pOB->BrigadeID;	
			OrderDescription* ODE=ElementaryOrders+BR->WarType-1;
			if(ODE->GroupID!=0xFF){
				SingleGroup* FGD=FormGrp.Grp+ODE->GroupID;
				char ids[]={1,0,2};
				MakeReformation(pOB->NNUM,pOB->BrigadeID,FGD->IDX[ids[stID]]);
			};
		};
	};
	return true;
};

void				lvCGroup::ChengeFormation(int formID){
//	RemoveDeadUnits();
	int N = GetTotalAmount();

	if (EngSettings.GameName==0){
		for(int i=0;i<N;i++){
			word MID=IDS[i];
			if(MID!=0xFFFF){
				OneObject* OB=GetOneObj_gID(MID);
				if (OB&&0<=OB->BrigadeID&&OB->BrigadeID<=0xFFFF) {
					// For Alexander
					word fID=formID;
					if(EngSettings.AutoChangeFormationType){
						if(formID==0) fID=0xFD; else 
						if(formID==1) fID=0xFE; else 
						if(formID==2) fID=0xFF;
					};
					MakeReformation(OB->NNUM,OB->BrigadeID,fID);
				};				
				break;
			};
		};
	};
	// For Cossacs II
	if (EngSettings.GameName==1){
		LinearArray<int,_int> BrigList;
		GetBrigadeList(false,&BrigList);
		int NBR=BrigList.GetAmount();
		if (NBR==0)	return;
		Brigade* pBR=NULL;
		word fID_C=-1;
		int NNUM=GetNation();
		
		for (int i=0; i<NBR; i++){
			pBR=&CITY[NNUM].Brigs[ BrigList[i] ];
			if (pBR!=NULL&&pBR->Enabled==true){
				OrderDescription* ODE=ElementaryOrders+pBR->WarType-1;
				if(ODE->GroupID!=0xFF){
					SingleGroup* FGD=FormGrp.Grp+ODE->GroupID;
					if (formID<FGD->NCommon){
						fID_C=FGD->IDX[formID];
					};
				};
				if (fID_C!=-1) MakeReformation(NNUM,BrigList[i],fID_C);
			};
			pBR=NULL;
		};
	};
};

void				lvCGroup::TakeRess(int RessID){
	int N = GetTotalAmount();
	OneObject* pOB = NULL;
	while (N--) {
		pOB = GetOneObj_lID(N);
		if (pOB!=NULL) {
			pOB->TakeResource(pOB->RealX>>4,pOB->RealY>>4,RessID,128,0);
			pOB = NULL;
		};
	};
};
void				lvCGroup::TakeFood(){
	// FOOD == 3
	TakeRess(3);
};
void				lvCGroup::TakeWood(){
	// WOOD == 0
	TakeRess(0);
};
void				lvCGroup::TakeStone(){
	// STOWN == 2
	TakeRess(2);
};
void				lvCGroup::ClearOrders(){
	int N = GetTotalAmount();
	OneObject* pOB = NULL;
	while (N--) {
		pOB = GetOneObj_lID(N);
		if (pOB!=NULL) {
			pOB->ClearOrders();
		};
	};
	LinearArray<int,_int> BrigList;
	GetBrigadeList(false,&BrigList);
	int BRN = BrigList.GetAmount();
	if (BRN>0) {
		Brigade* pBR=NULL;
		int Nat=GetNation();
		for (int i=0; i<BRN; i++){
			pBR=&CITY[Nat].Brigs[ BrigList[i] ];
			if (pBR->Enabled){
				pBR->ClearBOrders();
				pBR->ClearNewBOrders();
			};
		};
	};
};
void				lvCGroup::SetInStandGround(){
	LinearArray<int,_int> BrigList;
	GetBrigadeList(false,&BrigList);
	int N = BrigList.GetAmount();
	if (N>0) {
		int Nat=GetNation();
		// From vui_IS.cpp [ void vui_IS_BrigHoldPosit::SynchroAction(); ]
		for (int i=0; i<N; i++){
			Brigade* BR=&(CITY[Nat].Brigs[BrigList[i]]);
			if(BR->Enabled&&BR->WarType){
				bool sel=false;
				for(int bp=0;bp<BR->NMemb;bp++){
					word ID=BR->Memb[bp];
					if(ID!=0xFFFF){
						OneObject* OB=Group[ID];
						if(OB&&OB->Serial==BR->MembSN[bp]&&!OB->Sdoxlo&&OB->Selected){
							sel=true;
						};
					};
				};

				if(sel){
					BR->ClearBOrders();
					BR->BrigDelay=0;
					void MakeStandGround(Brigade* BR);
					if(!BR->BrigDelay) MakeStandGround(BR);
				};
			};
		};
		//////////////////////////////////////////////////////////////////
	};
};// Set all brigades in SG
int					lvCGroup::GetTotalAmount2(int UnitType, int Nat/*=0xff*/){
	int retVal = 0;
	int N = GetTotalAmount();
	OneObject* pOB = NULL;
	while (N--) {
		pOB = Group[IDS[N]];
		if (pOB&&(!pOB->Sdoxlo||pOB->Hidden)&&pOB->Serial==SNS[N]&&pOB->NIndex==UnitType) {
			retVal++;
			if (Nat!=0xff && pOB->NNUM!=Nat) {
				retVal--;
			};
		};
		pOB = NULL;
	};
	return	retVal;
};

void SetBrigadeFlagbearers(Brigade* BR);
void				lvCGroup::SetBrFlagbearers(){
	int retVal = 0;
	int N = GetTotalAmount();
	OneObject* pOB = NULL;
	while (N--) {
		pOB = Group[IDS[N]];
		if (pOB&&(!pOB->Sdoxlo||pOB->Hidden)&&pOB->Serial==SNS[N]) {
			if (pOB->BrigadeID!=0xFFFF){
				Brigade* BR=&CITY[pOB->NNUM].Brigs[pOB->BrigadeID];
				if (BR) {
					SetBrigadeFlagbearers(BR);
					reSized=true;
					GetTotalAmount();
					break;
				};
			};
		};
		pOB = NULL;
	};
};

void ClearBrigadeFlagbearers(Brigade* BR);
void				lvCGroup::ClearBrFlagbearers(){
	int retVal = 0;
	int N = GetTotalAmount();
	OneObject* pOB = NULL;
	while (N--) {
		pOB = Group[IDS[N]];
		if (pOB&&(!pOB->Sdoxlo||pOB->Hidden)&&pOB->Serial==SNS[N]) {
			if (pOB->BrigadeID!=0xFFFF){
				Brigade* BR=&CITY[pOB->NNUM].Brigs[pOB->BrigadeID];
				if (BR) {
					ClearBrigadeFlagbearers(BR);
					reSized=true;
					GetTotalAmount();
					break;
				};
			};
		};
		pOB = NULL;
	};
};

void				lvCGroup::SendToPosition(vvPOINT_SET* pPosArr){
	int UN = GetTotalAmount();
	int PN = pPosArr->Value.GetAmount();
	OneObject* pOB = NULL;
	while (UN--) {
		pOB = GetOneObj_lID(UN);
		if (pOB!=NULL){
			int x = pPosArr->Value[UN%PN]->Value.x;
			int y = pPosArr->Value[UN%PN]->Value.y;
			pOB->NewMonsterSendTo(x<<4,y<<4,128+0,0);
		};		
	};
};

bool				lvCGroup::ChekPosition(vvPOINT_SET* pPosArr){
	bool	retVal = true;
	int UN = GetTotalAmount();
	int PN = pPosArr->Value.GetAmount();
	OneObject* pOB = NULL;
	bool	inPos = false;
	while (retVal&&UN--) {
		inPos=false;
		pOB = GetOneObj_lID(UN);
		if (pOB!=NULL){
			int ux = pOB->RealX>>4;
			int uy = pOB->RealY>>4;
			for (int i=0;!inPos&&i<PN;i++){
				int px = pPosArr->Value[i]->Value.x;
				int py = pPosArr->Value[i]->Value.y;
				if (Norma(ux-px,uy-py)<=50)	inPos = true;
			};
			if (inPos==false)	retVal=false;
		};
	};
	return retVal;
};

int					lvCGroup::GetTotalAmount(){ 
	if (reSized||IDS.GetAmount()!=NUnit){
		reSized=true;
		RemoveDeadUnits();
		NUnit = IDS.GetAmount(); 
		reSized=false;
	};
	return NUnit;
};

int					lvCGroup::GetAmountInZone(int x, int y, int R){ 
//	RemoveDeadUnits();
	int N = GetTotalAmount();
	int retN = 0;
	OneObject* pOB = NULL;
	while (N--) {
		if (0<=IDS[N]&&IDS[N]<0xFFFF) {
			pOB = Group[IDS[N]];
			if (pOB&&(!pOB->Sdoxlo||pOB->Hidden)&&pOB->Serial==SNS[N]) {
				if (Norma((pOB->RealX>>4)-x,(pOB->RealY>>4)-y)<=R) {
					retN++;
				};
			};
		};
	};
	return retN; 
};

void				lvCGroup::GetGroupCenter(int& x,int& y){
	x=y=0;
//	RemoveDeadUnits();
	int N = GetTotalAmount();
	if (N==0)	return;
	float w = 1.f/(float)N;
	OneObject* pOB = NULL;
	float X=0.f,Y=0.f;
	while (N--) {
		pOB = Group[IDS[N]];
		if (pOB!=NULL && (!pOB->Sdoxlo||pOB->Hidden) && pOB->Serial==SNS[N]) {
			X += float(pOB->RealX>>4)*w;
			Y += float(pOB->RealY>>4)*w;
		};
	};
	x = (int)X;
	y = (int)Y;
};
int				lvCGroup::GetGroupX(){
	int x,y;
	GetGroupCenter(x,y);
	return x;
};

int				lvCGroup::GetGroupY(){
	int x,y;
	GetGroupCenter(x,y);
	return y;
};

int					lvCGroup::GetDirection(){
	int N = GetTotalAmount();
	if (N<=0)	return 0xFFFF;
	int TN = GetTotalAmount();
	int dF = 8;
	int Fi = -1;
	int trueDIR_UN = 0;
	float cosF = 0.f;
	float sinF = 0.f;
	OneObject* pOB = NULL;
	bool bSQ = false;
	while (N--) {
		pOB = GetOneObj_lID(N);
		if (pOB){
			if (pOB->BrigadeID!=0xFFFF)	bSQ=true;

			float fF = ((float)(pOB->RealDir)/256.f)*(2.f*3.1415f);
			cosF += cosf(fF);
			sinF += sinf(fF);

			//if (Fi==-1){
			//	Fi = pOB->RealDir;
			//	trueDIR_UN++;
			//}else{
			//	Fi = GetCircleDif(pOB->RealDir,Fi)/2;
			//	if ( min( abs(pOB->RealDir-Fi), abs(pOB->RealDir+256-Fi) )<=dF ){
			//		trueDIR_UN++;
			//	};
			//};
			
		};
	};
	float prc = 0.8f;
	if (bSQ==false)	prc = 0.5f;
//	if ( ((float)trueDIR_UN/(float)TN)*100.f>=prc )	return Fi;

	float DDD = sqrt( cosF*cosF + sinF*sinF );
	if (DDD!=0.f&&DDD>=(float)TN*prc) {
		float retF=lvBE_GetVecFi(cosF/DDD,sinF/DDD);
		retF = (retF/(2.f*3.1415f))*256.f;
		return (int)(retF);
	};

	return	0xFFFF;
};

byte				lvCGroup::GetNation(){
//	RemoveDeadUnits();
	int N = GetTotalAmount();
	byte retNI = 0xFF;
	OneObject* pOB = NULL;
	while (pOB==NULL&&N--) {
		if (0<=IDS[N]&&IDS[N]<0xFFFF) {
			pOB = Group[IDS[N]];
			if (!pOB||pOB->Serial!=SNS[N]){
				pOB=NULL;
			};
		};
	};
	if (pOB)	retNI = pOB->NNUM;
	return retNI;
};

int					lvCGroup::GetAgresiveState(){
	int Normal=0;		// 0 
	int	Peacefull=0;	// 1
	int	Aggressive=0;	// 2
	OneObject* pOB=NULL;
	int N = GetTotalAmount();
    while (N--) {
		pOB = GetOneObj_lID(N);
		if (pOB!=NULL) {
			if (pOB->ActivityState==0)	Normal++;
			if (pOB->ActivityState==1)	Peacefull++;
			if (pOB->ActivityState==2)	Aggressive++;
		};
	};
    if (Normal>Peacefull&&Normal>Aggressive)	return 0;
	if (Peacefull>Normal&&Peacefull>Aggressive)	return 1;
	return 2;
};

bool				lvCGroup::GetBrigadeList(bool checkCOM/*=false*/,LinearArray<int,_int>* pBrigList/*=NULL*/){
	int N = GetTotalAmount();
	if (N==0)	return false;
	if (pBrigList==NULL&&checkCOM==false) {
		bool	brigade=false;
		OneObject* pOB=NULL;
		while (!brigade&&N--) {
			pOB = GetOneObj_lID(N);
			if (pOB!=NULL && pOB->BrigadeID!=0xFFFF) {
				brigade=true;
			};
		};
		return brigade;
	}else{
		LinearArray<int,_int> BrigListTEMP;
		LinearArray<int,_int>* pBR_LIST=&BrigListTEMP;
		if (pBrigList!=NULL)	pBR_LIST=pBrigList;
		
		OneObject* pOB=NULL;
		while (N--) {
			pOB = GetOneObj_lID(N);
			if (pOB!=NULL && pOB->BrigadeID!=0xFFFF) {
				int NL = pBR_LIST->GetAmount();
				bool	newBrigade = true;
				while (newBrigade&&NL--){
					if ((word)((*pBR_LIST)[NL])==(pOB->BrigadeID))	newBrigade=false;
				};
				if (newBrigade)	pBR_LIST->Add(pOB->BrigadeID);
			};
		};
		if (checkCOM&&pBR_LIST->GetAmount()!=0){
			int BRN = pBR_LIST->GetAmount();
			Brigade* pBR=NULL;
			int Nat=GetNation();
			bool retVal=true;
			for (int i=0; i<BRN&&retVal; i++){
				pBR=&CITY[Nat].Brigs[ (*pBR_LIST)[i] ];
				if (pBR==NULL||pBR->NMemb<3||
					pBR->Memb[0]==0xFFFF||pBR->Memb[1]==0xFFFF||pBR->Memb[2]==0xFFFF) 
				{
					retVal=false;
				};
			};
			return	retVal;
		};
		return (pBR_LIST->GetAmount()!=0);
	};
	return false;
};
int					lvCGroup::GetAmountOfNewUnits(bool clearList/*=false*/){
	int ret = NofNE;
	if (clearList)	NofNE=0;
	return ret;
};
bool				lvCGroup::GetIsTired(){
	LinearArray<int,_int>	BrigList;
	GetBrigadeList(false,&BrigList); 
	bool Tired = false;
	int NB = BrigList.GetAmount();
	Brigade* pBR=NULL;
	int nat = GetNation();
	while (!Tired&&NB--) {
		pBR=CITY[nat].Brigs+BrigList[NB];
		if (pBR!=NULL&&pBR->Enabled) Tired = Tired || pBR->IsTired;
	};
	return Tired;
};
int					lvCGroup::GetNofBRLoadedGun(){
	int nofLG=0;
	LinearArray<int,_int>	BrigList;
	GetBrigadeList(false,&BrigList);
	int nBR = BrigList.GetAmount();
	if (nBR>0) {
		int Nat=GetNation();
		OneObject* pOB=NULL;
		Brigade* pBR=NULL;
		for (int i=0; i<nBR; i++){
			pBR=&CITY[Nat].Brigs[ BrigList[i] ];
			if (pBR!=NULL&&pBR->Enabled==true) {
				for(int ii=NBPERSONAL;ii<pBR->NMemb;ii++){
					pOB=Group[ pBR->Memb[i] ];
					if(pOB!=NULL&&!pOB->delay)	nofLG++;
					pOB=NULL;
				};	
			};
		};
	};
	return nofLG;
};
int					lvCGroup::GetNInside(bool Max/*=false*/){
	int retN=0;
	OneObject* pOB=NULL;
	int N = GetTotalAmount();
	while (N--) {
		pOB=GetOneObj_lID(N);
		if (pOB!=NULL && (!pOB->NewBuilding) && (pOB->Ref.General->MoreCharacter->MaxInside)) {
			if (Max) {
				retN += pOB->Ref.General->MoreCharacter->MaxInside + pOB->AddInside;
			}else{
				retN += pOB->NInside;
			};
		};
		pOB=NULL;
	};
	return retN;
};
bool CheckLeaveContainerAbility(OneObject* OB);
int					lvCGroup::GetLeaveAbility(){
	bool	retB = true;
	OneObject* pOB=NULL;
	int N = GetTotalAmount();
	while (retB&&N--) {
		pOB=GetOneObj_lID(N);
		if (pOB!=NULL) {
			if (!CheckLeaveContainerAbility(pOB)||pOB->Speed!=0)	retB = false;
		};
		pOB=NULL;
	};
	return retB;
};

void				lvCGroup::PushNUnitAway(int NU/*=0*/){
	int NUC = NU;
	OneObject* pOB=NULL;
	int N = GetTotalAmount();
	while (N--) {
		pOB=GetOneObj_lID(N);
		if (pOB){
			int NIN = pOB->NInside;
			while (NIN--) {
				pOB->LeaveMine(Group[pOB->Inside[NIN]]->NIndex);
			};
		};
	};
};

void				lvCGroup::GetSpeed(int& maxS,int& minS,int& average){
	bool	frs=true;
	OneObject* pOB=NULL;
	int	speed=0;
    int N=GetTotalAmount();
	while (N--) {
		pOB=GetOneObj_lID(N);
		if (pOB) {
			speed = pOB->newMons->MotionDist;
			if (speed>maxS)	maxS=speed;
			if (speed<minS)	minS=speed; 
			average+=speed;
			if (frs) {
				frs=false;
				maxS=speed;
				minS=speed;
				average=speed;
			};
		};
	};
	if (N>0)	average = average/N;
};
int					lvCGroup::GetMaxSpeed(){
	int maxS,minS,aver;
	GetSpeed(maxS,minS,aver);
	return maxS;
};
int					lvCGroup::GetAverageSpeed(){
	int maxS,minS,aver;
	GetSpeed(maxS,minS,aver);
	return aver;
};
int					lvCGroup::GetMinSpeed(){
	int maxS,minS,aver;
	GetSpeed(maxS,minS,aver);
	return minS;
};
void				lvCGroup::SetSpeed(int speed){
	OneObject* pOB=NULL;
	int N=GetTotalAmount();
	while (N--) {
		pOB=GetOneObj_lID(N);
		if (pOB!=NULL) pOB->GroupSpeed=speed;
	};
};
int 				lvCGroup::GetMinX(){
	int minX = 0.f;
	int N = GetTotalAmount();
	OneObject* pOB = NULL;
	while (N--) {
		pOB = Group[IDS[N]];
		if (pOB!=NULL && (!pOB->Sdoxlo||pOB->Hidden) && pOB->Serial==SNS[N]) {
			if (minX>(pOB->RealX>>4) || minX==0.f){
				minX = pOB->RealX>>4;
			};
		};
	};
	return minX;
};

int					lvCGroup::GetMaxX(){
	int maxX = 0.f;
	int N = GetTotalAmount();
	OneObject* pOB = NULL;
	while (N--) {
		pOB = Group[IDS[N]];
		if (pOB!=NULL && (!pOB->Sdoxlo||pOB->Hidden) && pOB->Serial==SNS[N]) {
			if (maxX<(pOB->RealX>>4) || maxX==0.f){
				maxX = pOB->RealX>>4;
			};
		};
	};
	return maxX;
};

int 				lvCGroup::GetMinY(){
	int minY = 0.f;
	int N = GetTotalAmount();
	OneObject* pOB = NULL;
	while (N--) {
		pOB = Group[IDS[N]];
		if (pOB!=NULL && (!pOB->Sdoxlo||pOB->Hidden) && pOB->Serial==SNS[N]) {
			if (minY>(pOB->RealY>>4) || minY==0.f){
				minY = pOB->RealY>>4;
			};
		};
	};
	return minY;
};

int					lvCGroup::GetMaxY(){
	int maxY = 0.f;
	int N = GetTotalAmount();
	OneObject* pOB = NULL;
	while (N--) {
		pOB = Group[IDS[N]];
		if (pOB!=NULL && (!pOB->Sdoxlo||pOB->Hidden) && pOB->Serial==SNS[N]) {
			if (maxY<(pOB->RealY>>4) || maxY==0.f){
				maxY = pOB->RealY>>4;
			};
		};
	};
	return maxY;
};

void				lvCGroup::BeforeSave(){
	Units.Clear();
	reSized=true;
	int N = GetTotalAmount();
	OneUS* pUS = NULL;
	while (N--) {
		pUS = new OneUS();
		pUS->ID = IDS[N];
		pUS->SN = SNS[N];
		Units.Add(*pUS);
		pUS = NULL;
	};
	IDS.Clear();
	SNS.Clear();
	NUnit=0;
};	

void				lvCGroup::AfterSave(){
	int N = Units.GetAmount();
	while (N--) {
		IDS.Add(Units[N].ID);
		SNS.Add(Units[N].SN);
	};
	Units.Clear();
	reSized=true;
	GetTotalAmount();
};

void				lvCGroup::BeforeLoad(){
	IDS.Clear();
	SNS.Clear();
	Units.Clear();
	NUnit=0;
};

void				lvCGroup::AfterLoad(){
	IDS.Clear();
	SNS.Clear();
	NUnit=0;
	int N = Units.GetAmount();
	while (N--) {
		IDS.Add(Units[N].ID);
		SNS.Add(Units[N].SN);
	};
	Units.Clear();
	reSized=true;
	GetTotalAmount();
};
void				lvCGroup::GetStructNameList( LinearArray<int,_int>& _list ){
	int N = GetTotalAmount();
	OneObject* pOB=NULL;
	while (N--) {
		pOB = GetOneObj_lID(N);
		if (pOB!=NULL&&pOB->NewBuilding==true) {
			_list.Add(pOB->NIndex);
		};
	};
	RemoveDublicateInList( _list );
};
// for use lua script ////////////////////////////////////////////////////
#ifdef __LUA__
bool g_lua_error=false;
void				lvCGroup::LUA_call(lua_State* L){
	if (!lua_error&&L!=NULL&&lua_main_func_name.str!=NULL&&!g_lua_error){
		try{
			if (call_lua_init==true){
				int ret = luabind::call_function<int>(L,lua_init_func_name.str,NAME.str);
				call_lua_init = (ret!=0);
			}else{
				UseByLua = luabind::call_function<int>(L,lua_main_func_name.str,NAME.str);
			};
		}catch(...){
			g_lua_error=true;
		};
	/*
		catch(luabind::error& e){
			lua_error=true;
			lua_State* L = e.state();
			// L will now point to the destructed
			// lua state and be invalid
			char errog_msg[512];
			sprintf(errog_msg,"%s",lua_tostring(L,-1));
			MessageBox(hwnd,errog_msg,"LUA ERROR",MB_ICONERROR|MB_OK);
		}
		catch(luabind::cast_failed& e){
			lua_error=true;
			lua_State* L = e.state();
			// L will now point to the destructed
			// lua state and be invalid
			char errog_msg[512];
			sprintf(errog_msg,"%s",lua_tostring(L,-1));
			MessageBox(hwnd,errog_msg,"LUA CAST ERROR",MB_ICONERROR|MB_OK);
		}*/
	};
};// call function by name "int main_lua()"

void				lvCGroup::SET_LUA_SCRIPT::EvaluateFunction(){
	lvCGroup* pGR = get_parent<lvCGroup>();
	if (pGR!=NULL) {
		pGR->lua_init_func_name = init_func_name.str;
		pGR->lua_main_func_name = main_func_name.str;
		pGR->call_lua_init=true;
		pGR->lua_error=false;
		g_lua_error=false;
	};
};
#endif//__LUA__
//////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
lvCGroup*	__getGrpByName(const char* sss){
	if (sss==NULL) return NULL;
	return (lvCGroup*)GroupsMap()->GetGroupNM(sss);
};
lvCGroup*	__getGrpByID(int sss){
	return (lvCGroup*)GroupsMap()->GetGroupID(sss);
};
// lvCGroupsMAP_ST ///////////////////////////////////////////////////////
bool	gg_VGrpReSize=true;
lvCGroupsMAP_ST::lvCGroupsMAP_ST(){
};
lvCGroupsMAP_ST::~lvCGroupsMAP_ST()	{
	vDeleteDynamicData();
	vCLEAN();
};
void		lvCGroupsMAP_ST::vINIT (char* CLName/*=NULL*/, DWORD ID/*=0*/, char* FLName/*=NULL*/){
	ClassName	= ( (CLName==NULL) ? ("lvCGroupsMAP_ST") : (CLName) );    
	InfID		= ( (ID==0) ? (_lvCGroupsMAP_ST_) : (ID) );   
	FileNameXML	= ( (FLName==NULL) ? ("lvCGroupsMAP_ST.xml") : (FLName) ); 

	vCLEAN();
};

void		lvCGroupsMAP_ST::vCLEAN(){
	NAME		= "NoName";
	DESCR		= "NoDescription";
};

void		lvCGroupsMAP_ST::vDeleteDynamicData(){
	GROUPS.Clear();
};

DWORD		lvCGroupsMAP_ST::GetFreeID(){
	int N = GROUPS.GetAmount();
	DWORD freeID = 0;
	while (N--) {
		if (GROUPS[N]->id>=freeID) {
			freeID = GROUPS[N]->id;
		};
	};
	return freeID+1;
};

void		lvCGroupsMAP_ST::AddGroup(char* name){
	bool	newGRP = (GetGroupNM(name)==NULL);
	if (newGRP) {
		lvCGroup* pGRP = new lvCGroup();
		pGRP->vINIT();
		pGRP->SetGroupName(name);
		pGRP->SetID(GetFreeID());
		GROUPS.Add(pGRP);
		BattleShema()->AddPlan_vGRP(pGRP);
		pGRP = NULL;
	};
};
void		lvCGroupsMAP_ST::AddGroupSmart(char* name){
	bool	newGRP = (GetGroupNM(name)==NULL);
	if (newGRP) {
		lvCGroupSmart* pGRP = new lvCGroupSmart();
		pGRP->vINIT();
		pGRP->SetGroupName(name);
		pGRP->SetID(GetFreeID());
		GROUPS.Add(pGRP);
		BattleShema()->AddPlan_vGRP(pGRP);
		pGRP = NULL;
	};
};
void		lvCGroupsMAP_ST::DelGroupNM(char* name){
	int N = GROUPS.GetAmount();
	while (N--) {
		if (strcmp(name,GROUPS[N]->GetGroupName())==0) {
			N=0;
			BattleShema()->DelPlan_vGRP(GROUPS[N]);
			GROUPS.Del(N,1);
		};
	};
};

void		lvCGroupsMAP_ST::DelGroupID(DWORD id){
	int N = GROUPS.GetAmount();
	while (N--) {
		if (GROUPS[N]->id == id) {
			BattleShema()->DelPlan_vGRP(GROUPS[N]);
			GROUPS.Del(N,1);
			N=0;
		};
	};
};

lvCGroup*	lvCGroupsMAP_ST::GetGroupNM(const char* name){
	lvCGroup* pGRP = NULL;
	if (name!=NULL){
		int N = GROUPS.GetAmount();
		while (pGRP==NULL&&N--) {
			char* s=GROUPS[N]->GetGroupName();
			if ( s&&(strcmp(name,s)==0) ) {
				pGRP = GROUPS[N];
			};
		};
	};
	return pGRP;
};

lvCGroup*	lvCGroupsMAP_ST::GetGroupID(DWORD id){
	lvCGroup* pGRP = NULL;
	int N = GROUPS.GetAmount();
	while (pGRP==NULL&&N--) {
		if (GROUPS[N]->id == id) {
			pGRP = GROUPS[N];
		};
	};
	return pGRP;
};

int			lvCGroupsMAP_ST::GetGroupAmount(){ 
	return GROUPS.GetAmount();
};

bool		lvCGroupsMAP_ST::GetBrigList( LinearArray<int,_int>& _list, int Nat ){
    int GRN = GetGroupAmount();
	_list.Clear();
	lvCGroup*  pGRP	= NULL;
	OneObject* pOB	= NULL;
	
	while (GRN--) {
		pGRP = GROUPS[GRN];
		if (pGRP!=NULL&&pGRP->GetNation()==Nat){
			int OBN = pGRP->GetTotalAmount();
			while (OBN--) {
				pOB = pGRP->GetOneObj_lID(OBN);
				if (pOB!=NULL&&pOB->BrigadeID!=0xFFFF) {
					bool	add = true;
					for (int l=0; add&&(l<_list.GetAmount()); l++){
						if (_list[l]==pOB->BrigadeID)	add=false;
					};
					if (add) {
						_list.Add(pOB->BrigadeID);
					};
				};
			};
		};
	};
	if (_list.GetAmount()>0)	return true;
	return false;
};

void		lvCGroupsMAP_ST::GetStructNameList( LinearArray<int,_int>& _list ){
	int N = GetGroupAmount();
	LinearArray<int,_int>	TempList;
	while (N--) {
		GROUPS[N]->GetStructNameList(TempList);
		int NL=TempList.GetAmount();
		for (int i=0; i<NL; i++){
			_list.Add( TempList[i] );
		};
		TempList.Clear();
	};
	RemoveDublicateInList( _list );
};
void		lvCGroupsMAP_ST::DrawGroup(){
	int N = GROUPS.GetAmount();
	while (N--) {
		GROUPS[N]->DrawGroup();
	};
};

void		lvCGroupsMAP_ST::SetVisible(bool value){
	int N = GROUPS.GetAmount();
	while (N--) {
		GROUPS[N]->visible = value;
	};
};

void		lvCGroupsMAP_ST::BeforeSave(){
	int N = GROUPS.GetAmount();
	while (N--) {
		if (GROUPS[N]!=NULL)	GROUPS[N]->BeforeSave();
	};
};

void		lvCGroupsMAP_ST::AfterSave(){
	int N = GROUPS.GetAmount();
	while (N--) {
		if (GROUPS[N]!=NULL)	GROUPS[N]->AfterSave();
	};
};

void		lvCGroupsMAP_ST::BeforeLoad(){
	GROUPS.Clear();
};

void		lvCGroupsMAP_ST::AfterLoad(){
	int N = GROUPS.GetAmount();
	while (N--) {
		if (GROUPS[N]!=NULL)	GROUPS[N]->AfterLoad();
	};
};

void		lvCGroupsMAP_ST::ReSize(){
	if (gg_VGrpReSize==false) {
		gg_VGrpReSize=true;
		int N=GetGroupAmount();
		while (N--){
			GROUPS[N]->reSized=true;
			GROUPS[N]->GetTotalAmount();
		};
	};
};
bool		lvCGroupsMAP_ST::__CheckMove(OneObject* pOB){
	if (GRP_donotMove.GetAmount()==0)	return true;
	if (pOB!=NULL && (!pOB->Sdoxlo||pOB->Hidden)){
		int GrpN = GRP_donotMove.GetAmount();
		bool	result=true;
		while ( (GrpN--)&&(result) ) {
			result = ( GRP_donotMove[GrpN]->_CheckGID(pOB->Index)!=0xFFFF );
		};
		return ( (result) ? (false) : (true) );
	};
	return false;
};
bool		lvCGroupsMAP_ST::__AddDonMoveGrp(lvCGroup* pGrp){
	if (pGrp==NULL||pGrp->NAME.str==NULL) return false;
	int GrpN = GRP_donotMove.GetAmount();
	bool	needADD=true;
	while (GrpN--&&needADD) {
		if ( strcmp(GRP_donotMove[GrpN]->NAME.str,pGrp->NAME.str)==0 )	needADD=false;
	};
	if (needADD){
		GRP_donotMove.Add(pGrp);
		return true;
	};
	return false;
};
bool		lvCGroupsMAP_ST::__RemDonMoveGrp(lvCGroup* pGrp){
	if (pGrp==NULL||pGrp->NAME.str==NULL) return false;
	int GrpN = GRP_donotMove.GetAmount();
	bool	deleted=false;
	while (GrpN--&&!deleted) {
		if ( strcmp(GRP_donotMove[GrpN]->NAME.str,pGrp->NAME.str)==0 ){
			deleted=true;
			GRP_donotMove[GrpN]=NULL;
			GRP_donotMove.DelElement(GrpN);
		};
	};
	return deleted;
};
//////////////////////////////////////////////////////////////////////////
// lvCGroupsOnMap ///////////////////////////////////////////////////////
DWORD lvCGroupsOnMap::GetValue(const char* ID){
	lvCGroup* pGRP = GroupsMap()->GetGroupNM(ID);
	if (pGRP!=NULL)	return pGRP->id;	
	return 0;
};

char* lvCGroupsOnMap::GetValue(DWORD ID){
	lvCGroup* pGRP = GroupsMap()->GetGroupID(ID);
	if (pGRP!=NULL)	return pGRP->GetGroupName();	
	return NULL;
};

int   lvCGroupsOnMap::GetAmount(){
	return GroupsMap()->GROUPS.GetAmount();
};

char* lvCGroupsOnMap::GetIndexedString(int idx){
	if (idx<0||GetAmount()<=idx)	return NULL;
	return GroupsMap()->GROUPS[idx]->GetGroupName();
};

DWORD lvCGroupsOnMap::GetIndexedValue (int idx){
	if (idx<0||GetAmount()<=idx)	return 0xFFFF;
	return GroupsMap()->GROUPS[idx]->id;
};

//======================================================================//
//===========================   vVALUES   ==============================//
//======================================================================//
// Base Value ////////////////////////////////////////////////////////////
_str		g_vvElementView;

char*			vvBASE::GetName() const{
	g_vvElementView  = "";
	g_vvElementView += "(BS)";
	g_vvElementView += Name.str;
	return g_vvElementView.str;
};

const	char*	vvBASE::GetThisElementView(const char* LocalName){ 
	return GetName();
};

void*			vvBASE::Get(){
	return NULL;
};

void			vvBASE::Set(void* value){ 
	return;	
};

void			vvBASE::SetName(const char* _Name){ 
	if (_Name!=NULL&&CheckName(_Name)) Name=_Name; 
};

void			vvBASE::SetID(){ 
	id = vValuesMap()->GetFreeID();
};

bool			vvBASE::CheckName(const char* name){
	bool newName = true;
	int N = vValuesMap()->V_VALUES.GetAmount();
	while (newName&&N--) {
		if (strcmp(vValuesMap()->V_VALUES[N]->Name.str,name)==0) {
			newName = false;
		};
	};
	return newName;
};

void			vvBASE::Draw(){
};
// Triger ////////////////////////////////////////////////////////////////
char*			vvTRIGER::GetName(){
	g_vvElementView  = "";
	g_vvElementView += "(TG)";
	g_vvElementView += Name.str;	
	return	g_vvElementView.str;
};

const	char*	vvTRIGER::GetThisElementView(const char* LocalName){
	GetName();
	if (Value)	g_vvElementView += "[true]";
	else		g_vvElementView += "[false]";
	return		g_vvElementView.str;
};

void*			vvTRIGER::Get(){
	return	&Value;
};

void			vvTRIGER::Set(void* value){
	bool*	pGetVal = reinterpret_cast<bool*>(value);
	if (pGetVal!=NULL)	Value = *pGetVal;
};

bool			vvTRIGER::GetValue() const{
	return Value;
};
void			vvTRIGER::SetValue(const bool value){
	Value = value;
};
// Word ///////////////////////////////////////////////////////////////
char*			vvWORD::GetName(){
	g_vvElementView  = "";
	g_vvElementView += "(WD)";
	g_vvElementView += Name.str;	
	return	g_vvElementView.str;
};

const	char*	vvWORD::GetThisElementView(const char* LocalName){
	GetName();
	g_vvElementView += "[";
	g_vvElementView += Value;
	g_vvElementView += "]";
	return		g_vvElementView.str;
};

void*			vvWORD::Get(){
	return &Value;
};

void			vvWORD::Set(void* value){
	DWORD*	pGetVal = reinterpret_cast<DWORD*>(value);
	if (pGetVal!=NULL)	Value = *pGetVal;
};

DWORD			vvWORD::GetValue()	const{
	return Value;
};
void			vvWORD::SetValue(const DWORD value){
	Value = value;
};
// Integer ///////////////////////////////////////////////////////////////
char*			vvINTEGER::GetName(){
	g_vvElementView  = "";
	g_vvElementView += "(IN)";
	g_vvElementView += Name.str;	
	return	g_vvElementView.str;
};

const	char*	vvINTEGER::GetThisElementView(const char* LocalName){
	GetName();
	g_vvElementView += "[";
	g_vvElementView += Value;
	g_vvElementView += "]";
	return		g_vvElementView.str;
};

void*			vvINTEGER::Get(){
	return &Value;
};

void			vvINTEGER::Set(void* value){
	int*	pGetVal = reinterpret_cast<int*>(value);
	if (pGetVal!=NULL)	Value = *pGetVal;
};
int				vvINTEGER::GetValue()	const{
	return Value;
};
void			vvINTEGER::SetValue(const int value){
	Value = value;
};
// Text //////////////////////////////////////////////////////////////////
char*			vvTEXT::GetName(){
	g_vvElementView  = "";
	g_vvElementView += "(TX)";
	g_vvElementView += Name.str;	
	return	g_vvElementView.str;
};

const	char*	vvTEXT::GetThisElementView(const char* LocalName){
	GetName();
    g_vvElementView += "[";
	g_vvElementView += TextID;
	g_vvElementView += "]";
	return		g_vvElementView.str;
};

void*			vvTEXT::Get(){
	Text = "";
	if (strcmp(TextID.str,"")!=0){
		Text = GetTextByID(TextID.str);
	};
	return Text.str;
};

void			vvTEXT::Set(void* value){
	vvTEXT* pGetVal = reinterpret_cast<vvTEXT*>(value);
	if (pGetVal!=NULL) {
		TextID		= pGetVal->TextID.str;
		oggFile		= pGetVal->oggFile.str;
		SpeakerID	= pGetVal->SpeakerID.str; 
	};
};
char*			vvTEXT::Get_TextID		()	const{
	return TextID.str;
};
char*			vvTEXT::Get_oggFile		()	const{
	return oggFile.str;
};
char*			vvTEXT::Get_SpeakerID	()	const{
	return SpeakerID.str;
};
void			vvTEXT::Set_TextID		(const char* text){
	if (text)	TextID		=	text;
};
void			vvTEXT::Set_oggFile		(const char* text){
	if (text)	oggFile		=	text;
};
void			vvTEXT::Set_SpeakerID	(const char* text){
	if (text)	SpeakerID	=	text;
};
// Picture ///////////////////////////////////////////////////////////////
vvPICTURE::vvPICTURE(){
	InfID		= _vvPICTURE_;
	FileID		= 0xFFFF;
	SpriteID	= -1;
};
vvPICTURE::~vvPICTURE(){
};
void			vvPICTURE::SET_DEF_POS::EvaluateFunction(){
	vvPICTURE* pPR = get_parent<vvPICTURE>();
	if (pPR!=NULL) {
		pPR->dx=0;
		pPR->dy=0;
		pPR->lx=0;
		pPR->ly=0;
	};
};
char*			vvPICTURE::GetName(){
	g_vvElementView  = "";
	g_vvElementView += "(PC)";
	g_vvElementView += Name.str;	
	return	g_vvElementView.str;
};
const	char*	vvPICTURE::GetThisElementView(const char* LocalName){
	GetName();
	g_vvElementView += "[dx=";
	g_vvElementView += dx;
	g_vvElementView += ",dy=";
	g_vvElementView += dy;
	g_vvElementView += ",lx=";
	g_vvElementView += lx;
	g_vvElementView += ",ly=";
	g_vvElementView += ly;
	g_vvElementView += "]";
	return		g_vvElementView.str;
};
bool			vvPICTURE::GetAsStringForMessage(_str& text){
	if (FileID==0xFFFF||SpriteID==-1)	return false;
	//{G gpID sprID dx dy lx ly}
	text = "";
	text += "{G ";
	text += FileID;
	text += " ";
	text += SpriteID;
	if (dx!=0||dy!=0){
		text += " ";
		text += dx;
		text += " ";
		text += dy;
	};
	if (lx!=0||ly!=0){
		text += " ";
		text += lx;
		text += " ";
		text += ly;
	};
	text += "}";
	return true;
};
int				vvPICTURE::GetSpriteNUM() const{
	if (FileID!=0xFFFF) return	GPS.GPNFrames(FileID);
	return 0;
};
int				vvPICTURE::GetSpriteID() const{
	return SpriteID;
};
void			vvPICTURE::SetSpriteID(const int sid){
	if (0<=sid&&sid<GetSpriteNUM()){
		SpriteID=sid;
	};
};
// Point2D ///////////////////////////////////////////////////////////////
void			vvPOINT2D::Draw(){
	if (visible==true){
		float x0 = (float)(Value.x);
		float y0 = (float)(Value.y);

		Vector3D	p00(x0-10.f,y0-10.f,(float)GetHeight((int)x0-10,(int)y0-10));
		Vector3D	p01(x0+10.f,y0+10.f,(float)GetHeight((int)x0+10,(int)y0+10));
		Vector3D	p10(x0+10.f,y0-10.f,(float)GetHeight((int)x0+10,(int)y0-10));
		Vector3D	p11(x0-10.f,y0+10.f,(float)GetHeight((int)x0-10,(int)y0+10));

		GPS.DrawLine(p00,p01,0xFF0000ff);
		GPS.DrawLine(p10,p11,0xFF0000ff);
	};
};
bool			vvPOINT2D::CondState(){
	bool state=true;
	int N=Cond.GetAmount();
	while (state&&N--) {
		state = state && Cond[N]->GetValue(0);
	};
	return state;
};
char*			vvPOINT2D::GetName(){
	g_vvElementView  = "";
	g_vvElementView += "(P2D)";
	g_vvElementView += Name.str;	
	return	g_vvElementView.str;
};

const	char*	vvPOINT2D::GetThisElementView(const char* LocalName){
	GetName();
	g_vvElementView += "[";
	g_vvElementView += Value.x;
	g_vvElementView += ",";
	g_vvElementView += Value.y;
	g_vvElementView += "]";
	return		g_vvElementView.str;
};

void*			vvPOINT2D::Get(){
	return &Value;
};

void			vvPOINT2D::Set(void* value){
	POINT2D* pGetVal = reinterpret_cast<POINT2D*>(value);
	if (pGetVal!=NULL){
		Value.x = pGetVal->x;
		Value.y = pGetVal->y;
	};
};

void			vvPOINT2D::Set(int x, int y){
	Value.x=x;
	Value.y=y;
};
void			vvPOINT2D::SetTR(int  x, int  y){
	if (Value.x!=x||Value.y!=y)	Value.newCoord=true;
	Value.x=x;
	Value.y=y;
};
void			vvPOINT2D::GetTR(int& x, int& y){
	Value.newCoord=false;
	x=Value.x;
	y=Value.y;
};
bool			vvPOINT2D::NewCoord(){
	return Value.newCoord;
}
int				vvPOINT2D::GetX()	const{
	return Value.x;
};
int				vvPOINT2D::GetY()	const{
	return Value.y;
};
void			vvPOINT2D::SetX(const int x){
	Value.x=x;
};
void			vvPOINT2D::SetY(const int y){
	Value.y=y;
};
// PointSet //////////////////////////////////////////////////////////////
char*			vvPOINT_SET::GetName(){
	g_vvElementView  = "";
	g_vvElementView += "(P_SET)";
	g_vvElementView += Name.str;	
	return	g_vvElementView.str;
};

const	char*	vvPOINT_SET::GetThisElementView(const char* LocalName){
	GetName();
	g_vvElementView += "[";
	g_vvElementView += Value.GetAmount();
	g_vvElementView += " P2D";
	g_vvElementView += "]";
	return		g_vvElementView.str;
};

void*			vvPOINT_SET::Get(){
	return &Value;
};

void			vvPOINT_SET::Set(void* value){
	ClassArray<vvPOINT2D>* pGetVal = reinterpret_cast<ClassArray<vvPOINT2D>*>(value);
	if (pGetVal!=NULL) {
		Value.Clear();
		vvPOINT2D* pP = NULL;
		int N = pGetVal->GetAmount();
		while (N--) {
			pP = new vvPOINT2D();
			if (pP!=NULL) {
				pP->Set( (*pGetVal)[N] );
				Value.Add(pP);
				pP = NULL;
			};
		};
	};
};

bool			vvPOINT_SET::TestPoint(int x,int y){
	bool	retVal = false;
	int N = Value.GetAmount();
	POINT2D* pPoint = NULL;
	while (!retVal&&N--) {
		pPoint = reinterpret_cast<POINT2D*>(Value[N]->Get());
		if (pPoint!=NULL){
			if (pPoint->x==x&&pPoint->y==y){
				retVal=true;
			};
			pPoint=NULL;
		};
	};
	return	retVal;
};

bool			vvPOINT_SET::TestPoint(vvPOINT2D* pPoint){
	if (pPoint!=NULL) {
		POINT2D* pP2D = reinterpret_cast<POINT2D*>(pPoint->Get());
		if (pP2D!=NULL) {
			return TestPoint(pP2D->x,pP2D->y);
		};
	};
	return	false;
};

void			vvPOINT_SET::CleanARR(){
	Value.Clear();
};

void			vvPOINT_SET::AddPoint(int x,int y){
	if (TestPoint(x,y)==false) {
		vvPOINT2D* pP = new vvPOINT2D();
		if (pP!=NULL){
			pP->Set(x,y);
			Value.Add(pP);
			pP=NULL;
		}
	};
};

int				vvPOINT_SET::GetNUM()	const{
	return Value.GetAmount();
};
vvPOINT2D*		vvPOINT_SET::Get_vvPOINT2D(const int id){
	if (0<=id&&id<GetNUM()) {
		return Value[id];
	};
	return NULL;
};
void	vvPOINT_SET::SetAgreeGrp::EvaluateFunction(){
	vvPOINT_SET* pPS = get_parent<vvPOINT_SET>();
	if (pPS!=NULL){
		lvCGroup*	 pGR = GroupsMap()->GetGroupID(GrpID);
		if (pGR!=NULL) {
			int N = pGR->GetTotalAmount();
			OneObject* pOB = NULL;
			while (N--) {
				pOB = pGR->GetOneObj_lID(N);
				if (pOB!=NULL)	pPS->AddPoint(pOB->RealX>>4,pOB->RealY>>4);
			};
		};
	};
};
// vvVector3D ///////////////////////////////////////////////////////////
char*			vvVector3D::GetName(){
	g_vvElementView = "";
	g_vvElementView += "(Vec3D)";
	g_vvElementView += Name.str;
	return	g_vvElementView.str;
};

const	char*	vvVector3D::GetThisElementView(const char* LocalName){
	GetName();
	g_vvElementView += "[";
	g_vvElementView += x;
	g_vvElementView += ",";
	g_vvElementView += y;
	g_vvElementView += ",";
	g_vvElementView += z;
	g_vvElementView += "]";
	return g_vvElementView.str;
};

void*			vvVector3D::Get(){
	Vector3D	Vec(x,y,z);
	return &Vec;
};

void			vvVector3D::Set(void* value){
	Vector3D*	pVec3D = reinterpret_cast<Vector3D*>(value);
	if (pVec3D!=NULL) {
		x = pVec3D->x;
		y = pVec3D->y;
		z = pVec3D->z;
	};
};

void			vvVector3D::SetPos::EvaluateFunction(){
	vvVector3D* pvv3D = get_parent<vvVector3D>();
	if (pvv3D!=NULL) {
		Vector3D pos = ICam->GetLookAt();
		pvv3D->x	= pos.x;
		pvv3D->y	= pos.y;
		pvv3D->z	= pos.z;
		pvv3D->mX	= mapx;
		pvv3D->mY	= mapy;
	};
};

void			vvVector3D::SetDir::EvaluateFunction(){
	vvVector3D* pvv3D = get_parent<vvVector3D>();
	if (pvv3D!=NULL) {
		Vector3D pos = ICam->GetDir();
		pvv3D->x = pos.x;
		pvv3D->y = pos.y;
		pvv3D->z = pos.z;
	};
};

void			vvVector3D::CPosToLookAt::EvaluateFunction(){
	vvVector3D* pPOS = pos.Get();
	vvVector3D* pDIR = dir.Get();

	if (pPOS==NULL||pDIR==NULL)	return;

	vvVector3D* pvv3D = get_parent<vvVector3D>();
	if (pvv3D!=NULL) {
		Vector3D curDIR = ICam->GetDir();
		Vector3D curPOS = ICam->GetDir();
		
		Vector3D tempPOS(pPOS->x,pPOS->y,pPOS->z);
		Vector3D tempDIR(pDIR->x,pDIR->y,pDIR->z);

		ICam->SetPos(tempPOS);
		ICam->SetDir(tempDIR);

		Vector3D newPOS = ICam->GetLookAt();

		ICam->SetPos(curPOS);
		ICam->SetDir(curDIR);

		pvv3D->x	= newPOS.x;
		pvv3D->y	= newPOS.y;
		pvv3D->z	= newPOS.z;
	};
};
// vvDIALOG ///////////////////////////////////////////////////////////
char*			vvDIALOG::GetName(){
	g_vvElementView = "";
	g_vvElementView += "(Dialog)";
	g_vvElementView += Name.str;
	return g_vvElementView.str;
};

const	char*	vvDIALOG::GetThisElementView(const char* LocalName){
	return GetName();
};

void*			vvDIALOG::Get(){
	return &Value;
};

void			vvDIALOG::Set(void* value){
	DialogsSystem* pGetVal = reinterpret_cast<DialogsSystem*>(value);
	if (pGetVal!=NULL) {
		value = pGetVal;
	};
};
// vvFuzzyRule ///////////////////////////////////////////////////////////
vvFuzzyRule::vvFuzzyRule(){
	Init();
};
vvFuzzyRule::vvFuzzyRule(const char* name, float min0, float min100, float max100, float max0){
	Init();
	Setup(name, min0, min100, max100, max0);
};
vvFuzzyRule::~vvFuzzyRule(){
	
};
void			vvFuzzyRule::Setup(const char* name, float min0, float min100, float max100, float max0){
	Init();
	m_min0   = min0;
	m_min100 = min100;
	m_max0   = max0;
	m_max100 = max100;
	Name	 = name;
};
float			vvFuzzyRule::IsTrueToWhatDegree(float datapoint) const {
	if (datapoint < m_min0 || datapoint > m_max0)		return(0.0f);
	if (datapoint >= m_min100 && datapoint <= m_max100)	return(1.0f);
	if (datapoint >= m_min0 && datapoint <= m_min100) {
		float widthofgreyarea = (float)fabs(m_min0 - m_min100);
		float relativedatapoint = datapoint-m_min0;
		return((float)fabs(relativedatapoint / widthofgreyarea));
	};
	if (datapoint >= m_max100 && datapoint <= m_max0) {
		float widthofgreyarea = (float)fabs(m_max0 - m_max100);
		float relativedatapoint = datapoint-m_max0;
		return((float)fabs(relativedatapoint / widthofgreyarea));
	};
	assert(0);
	return(0);
};
char*			vvFuzzyRule::GetName(void) const {
	return Name.str;
};
void			vvFuzzyRule::Init(void){
	InfID = _vvFuzzyRule_;
	m_min100 = m_max100 = m_min0 = m_max0 = 0.0f; 
	Name = "NoName";
};
float			__FuzzyAND(const vvFuzzyRule* rule1, float data1,const vvFuzzyRule* rule2, float data2){
    float true1 = 0.0f;
	if (rule1) true1 = rule1->IsTrueToWhatDegree(data1);
	float true2 = 0.0f;
	if (rule2) true1 = rule2->IsTrueToWhatDegree(data2);
	return(min(true1, true2));
};
// STORAGE for VVelue ////////////////////////////////////////////////////
		vvMAP_ST::vvMAP_ST(){
};

		vvMAP_ST::~vvMAP_ST(){
	vDeleteDynamicData();
	vCLEAN();
};

void	vvMAP_ST::vINIT(char* CLName/*=NULL*/, DWORD ID/*=0*/, char* FLName/*=NULL*/){
	ClassName	= ( (CLName==NULL) ? ("vvMAP_ST") : (CLName) );    
	InfID		= ( (ID==0) ? (_vvMAP_ST_) : (ID) );   
	FileNameXML	= ( (FLName==NULL) ? ("vvMAP_ST.xml") : (FLName) ); 

	vCLEAN();
};

void	vvMAP_ST::vCLEAN(){
	NAME		= "All Values";
	DESCR		= "Values used in BE";
};

void	vvMAP_ST::vDeleteDynamicData(){
	V_VALUES.Clear();
};

DWORD	vvMAP_ST::GetFreeID(){
	DWORD	newID = 0;
	int N = V_VALUES.GetAmount();
	while (N--) {
		if (V_VALUES[N]->id>=newID) {
			newID = V_VALUES[N]->id;
		};
	};
	return	newID+1;
};

vvBASE*	vvMAP_ST::GetVValueID(DWORD _id){
	vvBASE* retVal = NULL;
	int N = V_VALUES.GetAmount();
	while (retVal==NULL&&N--) {
		if (V_VALUES[N]->id==_id) {
			retVal = V_VALUES[N];
		};
	};
	return	retVal;
};

vvBASE*	vvMAP_ST::GetVValueNM(const char* name){
	vvBASE* retVal = NULL;
	int N = V_VALUES.GetAmount();
	while (retVal==NULL&&N--) {
		if (strcmp(V_VALUES[N]->Name.str,name)==0) {
			retVal = V_VALUES[N];
		};
	};
	return	retVal;
};
vvBASE*	vvMAP_ST::GetVValue(DWORD TYPE_id, DWORD _id){
	vvBASE* pValue = NULL;
	int N = V_VALUES.GetAmount();
	while (pValue==NULL&&N--) {
		if (V_VALUES[N]->InfID==TYPE_id && V_VALUES[N]->id==_id) {
			pValue = V_VALUES[N];
		};
	};
	return pValue;
};
vvBASE*	vvMAP_ST::GetNearestValue(float x,float y){
	vvBASE* pRet=NULL;
	for (int i=0; (i<V_VALUES.GetAmount())&&(pRet==NULL); i++){
		if (Is_vvBASE(V_VALUES[i])) {
			if (V_VALUES[i]->IsInShape(x,y)) {
				pRet = V_VALUES[i];
			};
		};
	};
	return pRet;
};
vvBASE*	vvMAP_ST::GetNearestValue(int   x,int   y){
	return GetNearestValue((float)x,(float)y);
};
vvBASE* vvMAP_ST::GetVValueTypeID(DWORD _tid){
	vvBASE* retVal = NULL;
	int N = V_VALUES.GetAmount();
	while (retVal==NULL&&N--) {
		if (V_VALUES[N]->InfID==_tid) {
			retVal = V_VALUES[N];
		};
	};
	return	retVal;
};
void	vvMAP_ST::SetViewType(DWORD _type){
	int N = V_VALUES.GetAmount();
	while (N--) {
		V_VALUES[N]->viewType = _type;
	};
};

void	vvMAP_ST::AddFirstSeparator(char* _Name,ClassArray<vvBASE>* arrX){
	lvCSubSection* pSS = new lvCSubSection();
	pSS->Descr = _Name;
	arrX->Add(NULL);
	(*arrX)[0]=(vvBASE*)pSS;
	pSS=NULL;
};

void	vvMAP_ST::AddSepareteValues(ClassArray<vvBASE>* arrX){
	if (arrX->GetAmount()>1){
		for (int i=0; i<arrX->GetAmount(); i++){
			if (i==0){
				V_VALUES.Add(NULL);
				V_VALUES[V_VALUES.GetAmount()-1]=(vvBASE*)(*arrX)[0];
			}else{
				V_VALUES.Add((*arrX)[i]);
			};
				(*arrX)[i]=NULL;
		};
	};
	arrX->Clear();
};

void	vvMAP_ST::Sort(){
	// Create arrays for all types
	ClassArray<vvTRIGER>	arrTRIGER;
	ClassArray<vvWORD>		arrWORD;
	ClassArray<vvINTEGER>	arrINTEGER;
	ClassArray<vvTEXT>		arrTEXT;
	ClassArray<vvPOINT2D>	arrPOINT2D;
	ClassArray<vvPOINT_SET>	arrPOINT_SET;
	ClassArray<vvVector3D>	arrVector3D;
	ClassArray<vvDIALOG>	arrDIALOG;
	ClassArray<vvMESSGES>	arrMESSGES;

	AddFirstSeparator(	"TRIGER"		,(ClassArray<vvBASE>*)(&arrTRIGER)		);
	AddFirstSeparator(	"WORD"			,(ClassArray<vvBASE>*)(&arrWORD)		);
	AddFirstSeparator(	"INTEGER"		,(ClassArray<vvBASE>*)(&arrINTEGER)		);
	AddFirstSeparator(	"TEXT"			,(ClassArray<vvBASE>*)(&arrTEXT)		);
	AddFirstSeparator(	"POINT2D"		,(ClassArray<vvBASE>*)(&arrPOINT2D)		);
	AddFirstSeparator(	"POINT_SET"		,(ClassArray<vvBASE>*)(&arrPOINT_SET)	);
	AddFirstSeparator(	"Vector3D"		,(ClassArray<vvBASE>*)(&arrVector3D)	);
	AddFirstSeparator(	"DIALOG"		,(ClassArray<vvBASE>*)(&arrDIALOG)		);
	AddFirstSeparator(	"MESSGES"		,(ClassArray<vvBASE>*)(&arrMESSGES)		);
	    
	int N = V_VALUES.GetAmount();
	while (N--) {
		switch(V_VALUES[N]->InfID) {
		case _vvTRIGER_:
			arrTRIGER.Add	(	(vvTRIGER*)		(V_VALUES[N])	);
			break;
		case _vvWORD_:
			arrWORD.Add		(	(vvWORD*)		(V_VALUES[N])	);
			break;
		case _vvINTEGER_:
			arrINTEGER.Add	(	(vvINTEGER*)	(V_VALUES[N])	);
			break;
		case _vvTEXT_:
			arrTEXT.Add		(	(vvTEXT*)		(V_VALUES[N])	);
			break;
		case _vvPOINT2D_:
			arrPOINT2D.Add	(	(vvPOINT2D*)	(V_VALUES[N])	);
			break;
		case _vvPOINT_SET_:
			arrPOINT_SET.Add(	(vvPOINT_SET*)	(V_VALUES[N])	);
			break;
		case _vvVector3D_:
			arrVector3D.Add	(	(vvVector3D*)	(V_VALUES[N])	);
			break;
		case _vvDIALOG_:
			arrDIALOG.Add	(	(vvDIALOG*)		(V_VALUES[N])	);
			break;
		case _vvMESSGES_:
			arrMESSGES.Add	(	(vvMESSGES*)	(V_VALUES[N])	);
			break;
//		default:
//			V_VALUES.DelElement(N);
		};
		V_VALUES[N] = NULL;
	};
	V_VALUES.Clear();

	AddSepareteValues(	(ClassArray<vvBASE>*)(&arrTRIGER)		);
	AddSepareteValues(	(ClassArray<vvBASE>*)(&arrWORD)			);
	AddSepareteValues(	(ClassArray<vvBASE>*)(&arrINTEGER)		);
	AddSepareteValues(	(ClassArray<vvBASE>*)(&arrTEXT)			);
	AddSepareteValues(	(ClassArray<vvBASE>*)(&arrPOINT2D)		);
	AddSepareteValues(	(ClassArray<vvBASE>*)(&arrPOINT_SET)	);
	AddSepareteValues(	(ClassArray<vvBASE>*)(&arrVector3D)		);
	AddSepareteValues(	(ClassArray<vvBASE>*)(&arrDIALOG)		);
	AddSepareteValues(	(ClassArray<vvBASE>*)(&arrMESSGES)		);
};

void	vvMAP_ST::DeleteAllSeparators(){
	int N = V_VALUES.GetAmount();
	while (N--) {
		if ( Is_vvBASE(V_VALUES[N]) == false ){
			V_VALUES.DelElement(N);
		};
	};
};

void	vvMAP_ST::SORT_BY_TYPE::EvaluateFunction(){
	vvMAP_ST* pMAP = get_parent<vvMAP_ST>();
	if (pMAP==NULL)	return;
	//pMAP->Sort();
};

bool	vvMAP_ST::Is_vvBASE(vvBASE* pVV){
	if (pVV==NULL)	return false;
	lvCSubSection* pSS = (lvCSubSection*)(pVV);
	if (pSS!=NULL&&pSS->InfID==_lvCSubSection_)	return false;
	return true;
};
void	vvMAP_ST::Draw(){
	for (int i=0; i<V_VALUES.GetAmount(); i++){
		vvBASE* pVV = V_VALUES[i];
		if (Is_vvBASE(pVV)) {
			pVV->Draw();
		};
	};
};

void	vvMAP_ST::DrawOnMiniMap(int x,int y,int Lx,int Ly){
	for (int i=0; i<V_VALUES.GetAmount(); i++){
		vvBASE* pVV = V_VALUES[i];
		if (Is_vvBASE(pVV)) {
			pVV->DrawOnMiniMap(x,y,Lx,Ly);
		};
	};
}
void	vvMAP_ST::VIEW_OBJS(DWORD classID,bool state){
	int N=V_VALUES.GetAmount();
	while (N--) {
		if ( V_VALUES[N]->InfID == classID ) V_VALUES[N]->visible=state;
	};
};
bool	vvMAP_ST::VIEW_OBJS(DWORD classID){
	int N=V_VALUES.GetAmount();
	while (N--) {
		if ( V_VALUES[N]->InfID == classID ) return	V_VALUES[N]->visible;
	};
	return false;
};
bool	vvMAP_ST::OnMouseHandling(int mx,int my,bool& LeftPressed,bool& RightPressed,int MapCoordX,int MapCoordY,bool OverMiniMap){
	bool intercept=false;
	int N=V_VALUES.GetAmount();
	while (!intercept&&N--) {
		if ( Is_vvBASE(V_VALUES[N]) )
			intercept=V_VALUES[N]->OnMouseHandling(mx,my,LeftPressed,RightPressed,MapCoordX,MapCoordY,OverMiniMap);
	};
	return intercept;
};
// lvCVValuesOnMap ///////////////////////////////////////////////////////
DWORD lvCVValuesOnMap::GetValue(const char* ID){
	vvBASE* pVVal = vValuesMap()->GetVValueNM(ID);
	if (pVVal!=NULL)	return pVVal->id;	
	return 0;
};

char* lvCVValuesOnMap::GetValue(DWORD ID){
	vvBASE* pVVal = vValuesMap()->GetVValueID(ID);
	if (pVVal!=NULL)	return pVVal->GetName();	
	return NULL;
};

int   lvCVValuesOnMap::GetAmount(){
	return	vValuesMap()->V_VALUES.GetAmount();
};

char* lvCVValuesOnMap::GetIndexedString(int idx){
	if (idx<0||GetAmount()<=idx)	return NULL;
	return vValuesMap()->V_VALUES[idx]->GetName();
};

DWORD lvCVValuesOnMap::GetIndexedValue (int idx){
	if (idx<0||GetAmount()<=idx)	return 0xFFFF;
	return vValuesMap()->V_VALUES[idx]->id;
};

//======================================================================//
//==========================   vMESSAGE   ==============================//
//======================================================================//
bool	CSingleMessagereCreate=true;
bool	CSingleMessage::NeedUpdatePointsCoord(){
	bool update=false;
	if (Use_Array){
		int N=Points.GetAmount();
		while (!update&&N--) {
			if (Points[N]->Get()!=NULL){
				update=Points[N]->Get()->NewCoord();
			};
		};
	};
	return update;
};
int		MessAlertFunc(void* param){
	int viev = *(reinterpret_cast<int*>(param));
	return viev;
};
bool	OnMissTaskOver(SimpleDialog* SD){
	if (SD!=NULL) {
		vvMESSGES* pMES=reinterpret_cast<vvMESSGES*>(SD->UserParam);
		if (pMES!=NULL) {
			if ( strcmp(SD->Name.str,"Cansel")==0 )		pMES->Data_Visible(false);
			else{
				pMES->ShowTask(SD->ID);
			};
			return true;
		};
	};
	return false;
};
bool	OnMissHintClick(SimpleDialog* SD){
	if (SD!=NULL) {
		vvMESSGES* pMES=reinterpret_cast<vvMESSGES*>(SD->UserParam);
		if (pMES!=NULL) {
			pMES->HideHint(SD->ID);
			return true;
		};
	};
	return false;
};
vvMESSGES::vvMESSGES(){
	InfID=_vvMESSGES_;
	ActivateAlert="NoAlert";
	StaticAlert="NoAlert";
	MSD_Visible=true;
	Desk_Load();
	ActiveteTilda=true;
	SeparVisib=false;
};
char*			vvMESSGES::GetName(){
	g_vvElementView = "MESSAGES(";
	g_vvElementView += Name.str;
	g_vvElementView += ")";
	return g_vvElementView.str;
};

const	char*	vvMESSGES::GetThisElementView(const char* LocalName){
	return GetName();
};
void			vvMESSGES::Draw(){
	RemHintInList(gMISS_SET.ShowHint);
	Desk_Create();
	Desk_Update();
	Desk_Sort();
//	Delete_Enable(EngSettings.MIS_SET.DonotShowComleteQuest);
	AddHintInList(gMISS_SET.ShowHint);
	Desk_SetHeight();
	Desk_Draw();
	Draw_Separator();

	Data_Visible(false);

	ArrowOnMapVisible(MSD_Visible);
	ArrowDS.ProcessDialogs();
};
void			vvMESSGES::DrawOnMiniMap(int x,int y,int Lx,int Ly){
	//	Alert_Update();
	Alert_AddTASKS();
	AlertDS.ProcessDialogs();
};
bool			vvMESSGES::SetMessDS_Visbility(bool val){
	bool	oldState = MSD_Visible;
	MSD_Visible = val;
	return oldState;
};
bool			vvMESSGES::GetMessDS_Visbility(){
	return MSD_Visible;
};
bool			vvMESSGES::GetTaskState(const int Task_MessUID,bool& visible,bool& deleted){
	int N = TASKS.GetAmount();
	CSingleMessage* pSM=NULL;
	while (pSM==NULL&&N--) {
		if (TASKS[N]->MessUID==Task_MessUID) {
			pSM = TASKS[N];
		}
	};
	if (pSM!=NULL) {
		visible = pSM->Visible;
		deleted = pSM->Deleted;
	};
	return (pSM!=NULL);
};
void			vvMESSGES::Desk_Load(char* FilePath/*=NULL*/){
	if (FilePath==NULL) {
		xmlQuote	xmlData( "MissGoalSystem" ); 
		if (xmlData.ReadFromFile( "Dialogs\\MGS.DialogsSystem.xml" )){
			/*
			ErrorPager Err;
			ListDesk*	pMissionGoals = new ListDesk();
			pMissionGoals->Load( xmlData, pMissionGoals, &Err );
			MessDS.AddDialog(pMissionGoals);
			pMissionGoals = NULL;
			*/
			ErrorPager	Err;
			MessDS.Load( xmlData,&MessDS,&Err );

			Data_Visible(false);

			if (GetAnimARROW()!=NULL) GetAnimARROW()->Visible=false;
		};
	};
};
void			vvMESSGES::Desk_Create(){
	ListDesk*	pMissionGoals = Desk_GetList();
	if (pMissionGoals==NULL)	return;
	
	int NTASK = TASKS.GetAmount();
	int NMESS = pMissionGoals->DSS.GetAmount();

	int TN = NTASK;
    while (!CSingleMessagereCreate&&TN--) {
		if (pMissionGoals->GetElementByUID( TASKS[TN]->MessUID )==0) CSingleMessagereCreate=true;
	};

	if (NTASK!=NMESS || CSingleMessagereCreate) {
		pMissionGoals->DSS.Clear();
		AlertState.Clear();
		for (int i=0; i<NTASK; i++){
			if (TASKS[i]->TalkerID.str!=NULL){
				TASKS[i]->MessUID = pMissionGoals->AddElement( GetTextByID(TASKS[i]->TalkerID.str) );
				VitButton* pNewTB = dynamic_cast<VitButton*>(pMissionGoals->GetElementByUID(TASKS[i]->MessUID));
				if (pNewTB!=NULL) {
					pNewTB->Name = "Task";
				//	pNewTB->OnUserClick = OnMissTaskClick;
					pNewTB->OnMouseOver = OnMissTaskOver;
					pNewTB->UserParam = reinterpret_cast<int>( this );
				};
				if (TASKS[i]->Use_Array) {
					int par2 = AL_HIDE;
					for (int ii=0; ii<TASKS[i]->Points.GetAmount(); ii++){
						int secccc = TASKS[i]->MessUID*1000+ii;
						Para<int,int>* pPara = new Para<int,int>(secccc,par2);
						AlertState.Add(pPara);

						_str FullName; 
						FullName = TASKS[i]->Name.str;
						FullName += ii;
						g_AddAlert(	ActivateAlert.str,
									TASKS[i]->Points[ii]->Get()->Value.x,
									TASKS[i]->Points[ii]->Get()->Value.y,
									MessAlertFunc,&(pPara->second_element),
									FullName.str							);
						pPara=NULL;
					};
				}else if (TASKS[i]->x!=0&&TASKS[i]->y!=0) {
					int par2 = AL_HIDE;
					Para<int,int>* pPara = new Para<int,int>(TASKS[i]->MessUID,par2);
					AlertState.Add(pPara);
					g_AddAlert(ActivateAlert.str,TASKS[i]->x,TASKS[i]->y,MessAlertFunc,&(pPara->second_element));
					pPara=NULL;
				};
			};
		};

		SimpleDialog* pCanselBT=Data_GetCanselBT();
		if (pCanselBT!=NULL){
		//	pCanselBT->OnUserClick = OnMissTaskClick;
			pCanselBT->UserParam = reinterpret_cast<int>( this );
		};

		Alert_Clear();
		Alert_AddTASKS();
	};
	CSingleMessagereCreate=false;
};
void			vvMESSGES::Desk_Update(){
	ListDesk*	pMissionGoals = Desk_GetList();
	if (pMissionGoals==NULL)	return;

	// for resizing ListDesc
	int MaxMessLength=0;
	int TotalHeight=0;

	VitButton* pTB=NULL;
	CSingleMessage* pTASK=NULL;
	for (int i=0; i<TASKS.GetAmount(); i++){
		pTASK = TASKS[i]; // pTASK
		if (pTASK->MessUID!=0) { 
			pTB = dynamic_cast<VitButton*>( pMissionGoals->GetElementByUID( pTASK->MessUID ) );
			if (pTB!=NULL) {
				pTB->Visible = pTASK->Visible;
				pTB->Enabled = !(pTASK->Deleted);
				if ( strcmp( GetTextByID(pTASK->TalkerID.str), pTB->Message )!=0 ){
					pTB->SetMessage( GetTextByID(pTASK->TalkerID.str) );
				};
				// Alert system
				if (pTASK->Use_Array || (pTASK->x!=0&&pTASK->y!=0)) {
		
					//////////////////////////////////////////////////////////////////////////
					if (pTASK->NeedUpdatePointsCoord()==true){
						// update position of alert on minimap
						for (int j=0; j<pTASK->Points.GetAmount(); j++){
							for (int ii=0; ii<pTASK->Points.GetAmount(); ii++){
								_str FullName;
								FullName = pTASK->Name.str;
								FullName += ii;
								int xcc,ycc;
								pTASK->Points[ii]->Get()->GetTR(xcc,ycc);
								g_SetNewAlertCoord(FullName.str,xcc,ycc);
							};
						};
					};
					//////////////////////////////////////////////////////////////////////////

					for (int ia=0; ia<AlertState.GetAmount(); ia++){
						if (AlertState[ia]->first_element==pTASK->MessUID 
							|| 
							(pTASK->Use_Array==true && AlertState[ia]->first_element/1000==pTASK->MessUID) )
						{
							if (pTASK->Deleted==false) {
								if (pTASK->Use_Array==true){
									int RealID=pTASK->MessUID*1000;
									bool PointCond=true;
									int NA = pTASK->Points.GetAmount();
									for (int aa=0; aa<NA; aa++){
										if (AlertState[ia]->first_element==RealID) {
											vvPOINT2D* pPointCur = pTASK->Points[aa]->Get();
											for (int cc=0; cc<pPointCur->Cond.GetAmount(); cc++){
												PointCond = PointCond&&pPointCur->Cond[cc]->GetValue(0);
											};
											if (pTB->MouseOver&&PointCond)	AlertState[ia]->second_element = AL_VIEW;
											else							AlertState[ia]->second_element = AL_HIDE;
										};;
										RealID++;
									};
								}else{
									if (pTB->MouseOver)	AlertState[ia]->second_element = AL_VIEW;
									else				AlertState[ia]->second_element = AL_HIDE;
								};
							}else{
									AlertState[ia]->second_element = AL_DELETE;
							};

						};
					};

				};
				//if (pTB->Visible) {
				//	if (pTB->GetTextWidth()>MaxMessLength)	MaxMessLength=pTB->GetTextWidth();
				//	TotalHeight += pTB->GetHeight();
				//};
			};
		};
		pTASK=NULL;
	};
	//// update size of ListDesc by button size data
	//int left,top,right,bottom;
	//pMissionGoals->GetMargin(left,top,right,bottom);
	//int gMLX = EngSettings.MIS_SET.minTASK_Lx;
	//int gDMX = EngSettings.MIS_SET.TsskMarge_x;
	//int gDMY = EngSettings.MIS_SET.TsskMarge_y;
	//if (MaxMessLength<gMLX)	MaxMessLength=gMLX;
	//pMissionGoals->SetWidth(MaxMessLength+left+right+gDMX+gDMY);
	//pMissionGoals->SetHeight(TotalHeight+top+bottom);

	Data_Update_Position();
};
void			vvMESSGES::Desk_SetHeight(){
	ListDesk*	pMissionGoals = Desk_GetList();
	if (pMissionGoals==NULL)	return;
	// for resizing ListDesc
	int MaxMessLength=0;
	int TotalHeight=0;
	for (int i=0; i<pMissionGoals->DSS.GetAmount(); i++){
		VitButton* pTB= dynamic_cast<VitButton*>(pMissionGoals->DSS[i]);
		if (pTB!=NULL&&pTB->Visible){
			if (pTB->GetTextWidth()>MaxMessLength)	MaxMessLength=pTB->GetTextWidth();
			TotalHeight += pTB->GetHeight();
		};
	};
	int left,top,right,bottom;
	pMissionGoals->GetMargin(left,top,right,bottom);
	int gMLX = EngSettings.MIS_SET.minTASK_Lx;
	int gDMX = EngSettings.MIS_SET.TsskMarge_x;
	int gDMY = EngSettings.MIS_SET.TsskMarge_y;
	if (MaxMessLength<gMLX)	MaxMessLength=gMLX;
	pMissionGoals->SetWidth(MaxMessLength+left+right+gDMX+gDMY);
	pMissionGoals->SetHeight(TotalHeight+top+bottom);
};
void			vvMESSGES::Desk_Sort(){
	// sort by deleted
	ListDesk* pMessList = Desk_GetList();
	if (pMessList!=NULL) {
		int NMess = pMessList->DSS.GetAmount();
		int key = 0;
		SimpleDialog* pTSD=NULL;
        for (int i=1; i<NMess; i++ ){
			key = i;
			pTSD=NULL;
			while ( key>=1 && ( pMessList->DSS[key]->Enabled==true && pMessList->DSS[key-1]->Enabled==false ) ) {
				pTSD=pMessList->DSS[key-1];
				pMessList->DSS[key-1]=pMessList->DSS[key];
				pMessList->DSS[key]=pTSD;
				pTSD=NULL;
				key--;
			};
		};
	};
};
void			vvMESSGES::Desk_Draw(){
	if (MSD_Visible)	MessDS.ProcessDialogs();
};
ListDesk*		vvMESSGES::Desk_GetList(){
	ListDesk* pList=NULL;
	int NDSS = MessDS.DSS.GetAmount();
	if (NDSS>0){
		while (pList==NULL && NDSS--) {
			pList = dynamic_cast<ListDesk*>(MessDS.DSS[NDSS]);
			if (pList!=NULL && strcmp(pList->Name.str,"TaskList")!=0) pList=NULL;
		};
	};
	return pList;
};
void			vvMESSGES::Delete_Enable(bool activate){
	if (activate==false)	return;
	ListDesk* pMessList = Desk_GetList();
	if (pMessList!=NULL) {
		int NMess = pMessList->DSS.GetAmount();
		while (NMess--) {
			if (pMessList->DSS[NMess]->Visible&&pMessList->DSS[NMess]->Enabled==false){
				pMessList->DSS.DelElement(NMess);
			};
		};
	};
};
DialogsDesk*	vvMESSGES::Data_GetObj(){
	DialogsDesk* pData=NULL;
	int NDSS = MessDS.DSS.GetAmount();
	if (NDSS>0){
		while (pData==NULL && NDSS--) {
			pData = dynamic_cast<DialogsDesk*>(MessDS.DSS[NDSS]);
			if (pData!=NULL && strcmp(pData->Name.str,"TaskData")!=0) pData=NULL;
		};
	};
	return pData;
};
TextButton*		vvMESSGES::Data_GetTextBT(){
	TextButton* pBTN = NULL;
	DialogsDesk* pDataDesk = Data_GetObj();
	if (pDataDesk!=NULL) {
		//int N=pDataDesk->DSS.GetAmount();
		//DialogsDesk* pSubDataDesk = NULL;
		//while (pSubDataDesk==NULL&&N--) {
		//	pSubDataDesk = dynamic_cast<DialogsDesk*>(pDataDesk->DSS[N]);
		//	if ( pSubDataDesk!=NULL && strcmp(pSubDataDesk->Name.str,"TaskData")!=0 ) pSubDataDesk=NULL;
		//};
		//if (pSubDataDesk!=NULL) {
		//	N=pSubDataDesk->DSS.GetAmount();
		//	while (pBTN==NULL&&N--) {
		//		pBTN = dynamic_cast<TextButton*>(pSubDataDesk->DSS[N]);
		//		if ( pBTN!=NULL && strcmp(pBTN->Name.str,"Text")!=0 ) pBTN=NULL;
		//	};
		//};
		pBTN = dynamic_cast<TextButton*>(pDataDesk->Find("Text"));
	};
	return pBTN;
};
SimpleDialog*	vvMESSGES::Data_GetCanselBT(){
	SimpleDialog* pBTN = NULL;
	DialogsDesk* pDataDesk = Data_GetObj();
	if (pDataDesk!=NULL) {
		pBTN = pDataDesk->Find("Cansel");
	};
	return pBTN;	
};
void			vvMESSGES::Data_Visible(bool state){
	DialogsDesk* pTaskDesk = Data_GetObj();
	if (pTaskDesk!=NULL) {
		pTaskDesk->Visible = state;
	};
};
void			vvMESSGES::Data_Update_Position(){
	ListDesk*		pTaskList = Desk_GetList();
	DialogsDesk*	pTaskDesk = Data_GetObj();
	GPPicture*		pSEP = GetSeparator();
	TextButton*		pTB  = Data_GetTextBT();
	if (pTaskList!=NULL&&pTaskDesk!=NULL&&pSEP!=NULL&&pTB!=NULL) {
		int x0,y0;
		x0 = pTaskList->Getx() /*+ pTaskList->GetWidth() + 50*/;
		y0 = pTaskList->Gety() + pTaskList->GetHeight() + pSEP->GetHeight() + 8;
		pTaskDesk->Setx(x0);
		pTaskDesk->Sety(y0);
		pTaskDesk->SetWidth( pTaskList->GetWidth() );
		int gTMX = EngSettings.MIS_SET.TextMarge_x;
		int gTMY = EngSettings.MIS_SET.TextMarge_y;
		pTB->MaxWidth = pTaskDesk->GetWidth()-gTMX-gTMY;
		pTaskDesk->SetHeight( pTB->GetHeight()+gTMX+gTMY );
	};
};
void			vvMESSGES::Data_SetMessage(char* TextID){
	if (TextID==NULL)	return;
	TextButton* pTextBT = Data_GetTextBT();
	if (pTextBT!=NULL){
		int H=pTextBT->GetHeight();
		pTextBT->SetMessage( GetTextByID( TextID ) );
		H = pTextBT->GetHeight() - H;
		
		DialogsDesk* pDD=Data_GetObj();
		if (pDD!=NULL) {
			int tH=pDD->GetHeight();
			pDD->SetHeight(tH+H);
			SimpleDialog* pSD=pDD->Find("TaskData");
			if (pSD!=NULL) {
				int tH=pSD->GetHeight();
				pSD->SetHeight(tH+H);
			};
		};
	};
};
void			vvMESSGES::Data_SetMessage(int TaskID){
	int N=TASKS.GetAmount();
	_str text;
	while (N--) {
		if (TASKS[N]->MessUID==TaskID){
			text=TASKS[N]->TextID.str;
			N=0;
		};
	};
	
	N=HINTS.GetAmount();
	while (N--) {
		if (HINTS[N]->MessUID==TaskID){
			text=HINTS[N]->TextID.str;
			N=0;
		};
	};
	Data_SetMessage(text.str);
};
void			vvMESSGES::Alert_Clear(){
	AlertDS.DSS.Clear();
	ArrowDS.DSS.Clear();
};
void			vvMESSGES::AddArrowOnMiniMap(int _TID,int _x,int _y,bool _visible){
	GPPicture* pGPP = new GPPicture;
	
	pGPP->ID = TASKS[_TID]->MessUID;
	pGPP->FileID=TASKS[_TID]->FileID;
	pGPP->SetSpriteID(TASKS[_TID]->SpriteID);

	int minimapx = GetXOnMiniMap(_x,_y);
	int minimapy = GetYOnMiniMap(_x,_y);

//	pGPP->Setx(minimapx - pGPP->GetWidth() /2);
//	pGPP->Sety(minimapy - pGPP->GetHeight());

    pGPP->Setx(minimapx - pGPP->GetWidth()/2);
	pGPP->Sety(minimapy - pGPP->GetHeight()/2);

	pGPP->Visible=_visible;

	AlertDS.DSS.Add(pGPP);

	pGPP=NULL;
};
void			vvMESSGES::AddArrowOnMap(int _TID,int _x,int _y,bool _visible){
	static	int LastTimeUpdate	= GetTickCount();
	static	int LastSpriteID	= 0;
	static	int SpriteNUM		= 64;

	if (GetTickCount()-LastTimeUpdate>=(int)(1000.f/(float)SpriteNUM)){
		LastTimeUpdate=GetTickCount();
		LastSpriteID = (LastSpriteID+1)%SpriteNUM;
	};

	GPPicture* pBAM = new GPPicture;	// Big Arrow  on Map
	GPPicture* pSRC = GetAnimARROW();

	if (pSRC==NULL)	return;

	pBAM->ID = TASKS[_TID]->MessUID;
	pBAM->FileID = pSRC->FileID;
	pBAM->SetSpriteID(LastSpriteID);

	int mapposx = _x;
	int mapposy = _y;
	float	mX = (float)mapposx;
	float	mY = (float)mapposy;
	float	mZ = (float)GetHeight(mapposx,mapposy)+100.f;

	Vector3D	p(mX,mY,mZ);
	WorldToScreenSpace(p);
	mapposx=p.x;
	mapposy=p.y;

	pBAM->Setx(mapposx - pBAM->GetWidth()/2);
	pBAM->Sety(mapposy - pBAM->GetHeight());

	pBAM->Visible = _visible;

	ArrowDS.DSS.Add(pBAM);

	pBAM=NULL;
};
void			vvMESSGES::ArrowOnMapVisible(bool _vvv){
	if (_vvv==true)	return;
	int N = ArrowDS.DSS.GetAmount();
	GPPicture* pAR=NULL;
	while (N--) {
		pAR = dynamic_cast<GPPicture*>(ArrowDS.DSS[N]);
		if (pAR!=NULL) {
			pAR->Visible=_vvv;
		};
	};
};
void			vvMESSGES::Alert_AddTASKS(){
	Alert_Clear();
	for (int i=0; i<TASKS.GetAmount(); i++){
		if (TASKS[i]->Use_Array) {
			for (int ii=0; ii<TASKS[i]->Points.GetAmount(); ii++){
				/////////////////////////////////////////////////////////////////////
				bool vvv = TASKS[i]->Visible;
				if (TASKS[i]->Deleted==true)	vvv=false;
				if (TASKS[i]->Points[ii]->Get()->CondState()==false) vvv=false;

				int xxx=TASKS[i]->Points[ii]->Get()->Value.x;
				int yyy=TASKS[i]->Points[ii]->Get()->Value.y;

				AddArrowOnMiniMap(i,xxx,yyy,vvv);
				AddArrowOnMap(i,xxx,yyy,vvv);
				/////////////////////////////////////////////////////////////////////
			};
		}else if (TASKS[i]->x!=0&&TASKS[i]->y!=0) {
			/////////////////////////////////////////////////////////////////////
			bool vvv = TASKS[i]->Visible;
			if (TASKS[i]->Deleted==true)	vvv=false;

			int xxx=TASKS[i]->x;
			int yyy=TASKS[i]->y;

			AddArrowOnMiniMap(i,xxx,yyy,vvv);
			AddArrowOnMap(i,xxx,yyy,vvv);
			/////////////////////////////////////////////////////////////////////
		};
	};
};
void			vvMESSGES::Alert_Update(){
	//GPPicture* pGPP = NULL;
	//bool vis;
	//bool del;
	//for (int i=0; i<AlertDS.DSS.GetAmount(); i++){
	//	pGPP = dynamic_cast<GPPicture*>(AlertDS.DSS[i]);
	//	if (pGPP&&GetTaskState(pGPP->ID,vis,del)){
	//		pGPP->Visible=true;
	//		if (vis==false||del==false)	pGPP->Visible=false;
	//	};
	//};
};
void			vvMESSGES::ShowTask(int TaskID){
    Data_Update_Position();
	Data_SetMessage(TaskID);
	Data_Visible(true);
};
void			vvMESSGES::Separator_Visible(bool state){
	SeparVisib=state;
	GPPicture* pSEP = GetSeparator();
	if (SeparVisib==false&&pSEP!=NULL)	pSEP->Visible=false;
};
void			vvMESSGES::Draw_Separator(){
	DialogsDesk* pTaskDesk = Data_GetObj();
	if (pTaskDesk!=NULL) {
		Separator_Visible(pTaskDesk->Visible);
	};

	if (MSD_Visible==false)	return;
    GPPicture* pSEP = GetSeparator();
	if (pSEP==NULL||SeparVisib==false)	return;
	pSEP->Visible=true;

	ListDesk*	pMissionGoals = Desk_GetList();
	if (pMissionGoals!=NULL) {
		int x0 = pMissionGoals->Getx();
		int y0 = pMissionGoals->Gety();
		int H = pMissionGoals->GetHeight();
		int W = pMissionGoals->GetWidth();

		int Sx0 = x0+3;
		int Sy0 = y0+H-2+5;
		int SW	= W-3-3;
		int w	= pSEP->GetWidth();
		if (w==0)	return;
		
		pSEP->Sety(Sy0);
		for (int i=0; i<SW/w; i++){
			pSEP->Setx(Sx0+i*w);
			GPS.FlushBatches();	
			pSEP->_Draw();
		};
		pSEP->Setx(Sx0+SW-w);
		pSEP->_Draw();
	};
};
GPPicture*		vvMESSGES::GetSeparator(){
	GPPicture* pSEP = dynamic_cast<GPPicture*>(MessDS.Find("Separator"));;
	return pSEP;
};
GPPicture*		vvMESSGES::GetAnimARROW(){
	GPPicture* pARROW = dynamic_cast<GPPicture*>(MessDS.Find("Arrow"));;
	return pARROW;
};
void			vvMESSGES::AddHintInList(bool add){
	if (add==false)	return;
	ListDesk*	pMissionGoals = Desk_GetList();
	int HN = HINTS.GetAmount();
	for (int i=0; i<HN; i++){
		if (HINTS[i]->Visible&&(dynamic_cast<COneMissHint*>(HINTS[i]))->HasRead==false){
			HINTS[i]->MessUID = pMissionGoals->AddElement( GetTextByID("#MISS_HINT_VV") );
			VitButton* pNewTB = dynamic_cast<VitButton*>(pMissionGoals->GetElementByUID(HINTS[i]->MessUID));
			if (pNewTB!=NULL) {
				pNewTB->Name = "Hint";
				pNewTB->OnUserClick = OnMissHintClick;
				pNewTB->OnMouseOver = OnMissTaskOver;
				pNewTB->UserParam = reinterpret_cast<int>( this );
				pNewTB->Visible=true;
			};
		};
	};
};
void			vvMESSGES::RemHintInList(bool rem){
	if (rem==false)	return;

	ListDesk*	pMissionGoals = Desk_GetList();
	if (pMissionGoals==NULL)	return;

	for (int i=0; i<HINTS.GetAmount(); i++){
		int ID_DEL = HINTS[i]->MessUID;
		
        int NMess = pMissionGoals->DSS.GetAmount();
		while (NMess--&&ID_DEL!=-1) {
			if (pMissionGoals->DSS[NMess]->ID==ID_DEL){
				pMissionGoals->DSS.DelElement(NMess);
				ID_DEL=-1;
			};
		};
	};
};
void			vvMESSGES::HideHint(DWORD id){
	int N=HINTS.GetAmount();
	while (N--) {
		if (HINTS[N]->MessUID==id){
			(dynamic_cast<COneMissHint*>(HINTS[N]))->HasRead=true;
			N=0;
		};
	};
};
bool			vvMESSGES::OnMouseHandling(int mx,int my,bool& LeftPressed,bool& RightPressed,int MapCoordX,int MapCoordY,bool OverMiniMap){
	bool	intercept=false;
//	static	int temp=0;
	
	ListDesk*	pMissionGoals = Desk_GetList();
	if (pMissionGoals!=NULL) {
		if (pMissionGoals->MouseOver){
			pMissionGoals->Diffuse = 0xFFffffff;
		}else{
			DWORD gTAF = EngSettings.MIS_SET.NotActiveDiffuse;
			pMissionGoals->Diffuse = gTAF;
			Data_Visible(false);
		};
	};

//	if (false&&GetKeyState(VK_OEM_3)&0x8000&&temp>5) {
//		SetMessDS_Visbility(!GetMessDS_Visbility());
//		void	UnPress();
//		UnPress();
//		temp=0;
//		intercept=true;
//	};
//	temp++;

	return intercept;
};
vvMESSGES*	g_TASK_HINT_OBJ(){
	return dynamic_cast<vvMESSGES*>(vValuesMap()->GetVValueTypeID(_vvMESSGES_));
};
// vvMISSMGR /////////////////////////////////////////////////////////////
extern bool NOPAUSE;
bool	vbRESET_RCLIC(SimpleDialog* SD){
	return true;
};
bool	vbSTPS_LCLIC(SimpleDialog* SD){
	NOPAUSE = !NOPAUSE;
	vvMISSMGR* pOB = dynamic_cast<vvMISSMGR*>(vValuesMap()->GetVValue(_vvMISSMGR_,SD->UserParam));
	if (pOB!=NULL) {
		pOB->STPS_ANIM(false);
		pOB->STPS_PROC();
		return true;
	};
	return false;
};
bool	vbREST_LCLIC(SimpleDialog* SD){
	vvMISSMGR* pOB = dynamic_cast<vvMISSMGR*>(vValuesMap()->GetVValue(_vvMISSMGR_,SD->UserParam));
	if (pOB!=NULL) {
		pOB->REST_ANIM(false);
		pOB->REST_PROC();
		return true;
	};
	return false;
};
bool	vbNEXT_LCLIC(SimpleDialog* SD){
	vvMISSMGR* pOB = dynamic_cast<vvMISSMGR*>(vValuesMap()->GetVValue(_vvMISSMGR_,SD->UserParam));
	if (pOB!=NULL) {
		pOB->NEXT_ANIM(false);
		pOB->NEXT_PROC();
		return true;
	};
	return false;
};
vvMISSMGR::vvMISSMGR(){
	InfID		= _vvMISSMGR_;
	DS_FILE		= "Dialogs\\mission\\TutorialManager.DialogsSystem.xml";
	visible		= true;

	DS_X = 30;
	DS_Y = 40;

	DS_LOAD();
	DS_PREP();
	DS_SETPOS();

	markTIME();

	AnimDelay = 500;
};
vvMISSMGR::~vvMISSMGR(){
};
void			vvMISSMGR::Draw(){
	DS_DRAW();
};
bool			vvMISSMGR::DS_LOAD(){
	if (DS_FILE.str!=NULL){
        xmlQuote	xmlData( "MissMgrSystem" );
		if (xmlData.ReadFromFile(DS_FILE.str)) {
			ErrorPager	Err;
			DS_MENU.reset_class(&DS_MENU);
			if (DS_MENU.Load( xmlData,&DS_MENU,&Err )) return true;
		};
	};
	return false;
};// load menu
bool			vvMISSMGR::DS_PREP(){
	bool res = STPS_PREP() && REST_PREP() && NEXT_PREP();
	return res;
};// prepare for work
bool			vvMISSMGR::DS_DRAW(){
	if (visible){
//		DS_SETPOS();
		ANIMATE();
		DS_MENU.ProcessDialogs();
	};
	return true;
};// draw DS object
void			vvMISSMGR::markTIME(){
	lastATime = (int)GetTickCount();
};
bool			vvMISSMGR::checkDALAY(int curTIME){
	return ( curTIME - lastATime >= AnimDelay );
};
void	g_setALPHA_(DWORD& oldCOLOR,DWORD alpha){
	DWORD oldALPHA = oldCOLOR & 0xFF000000;
	DWORD newAlpha = alpha & 0xFF000000;
	if (oldALPHA!=newAlpha) {
		oldCOLOR = ( oldCOLOR & 0x00FFFFFF ) + newAlpha;
	};
};
void			vvMISSMGR::ANIMATE_BTN(GP_TextButton* pBTN,bool curST,bool animate){
	if ( pBTN ) {
		if ( pBTN->MouseOver==false ) {
			if ( animate ) {
				if (curST) g_setALPHA_(pBTN->Diffuse,0xFF000000);
				else	   g_setALPHA_(pBTN->Diffuse,0xBB000000);
			}else{
				g_setALPHA_(pBTN->Diffuse,0xFF000000);
			};
		}else{
			g_setALPHA_(pBTN->Diffuse,0xFF000000);
		};
	};
};
void			vvMISSMGR::ANIMATE(){
	static bool curSTATE = true;
	int curTIME = GetTickCount();

	if ( checkDALAY(curTIME) ) {

		curSTATE = !curSTATE;
		markTIME();
	};
	
	GP_TextButton* pBTN = NULL;

	pBTN = DS_GetElement("StartPause");
	ANIMATE_BTN(pBTN,curSTATE,stps_ANIM);

	pBTN = DS_GetElement("Restart");
	ANIMATE_BTN(pBTN,curSTATE,rest_ANIM);

	pBTN = DS_GetElement("Next");
	ANIMATE_BTN(pBTN,curSTATE,next_ANIM);

};
bool			vvMISSMGR::DS_VISB(bool _st){
	bool old_st = visible;
	visible = _st;
	return old_st;
};// DS visibility
DialogsDesk*	vvMISSMGR::DS_GetDesck(){
	DialogsDesk* pDD=NULL;
	int NDSS = DS_MENU.DSS.GetAmount();
	if (NDSS>0){
		while (pDD==NULL && NDSS--) {
			pDD = dynamic_cast<DialogsDesk*>(DS_MENU.DSS[NDSS]);
			if (pDD!=NULL && strcmp(pDD->Name.str,"Desk")!=0) pDD=NULL;
		};
	};
	return pDD;
};	
GP_TextButton*	vvMISSMGR::DS_GetElement(char* name){
	if (name==NULL)	return NULL;
	GP_TextButton* pOB = NULL;
	DialogsDesk* pDD = DS_GetDesck();
	if (pDD!=NULL) {
		pOB = dynamic_cast<GP_TextButton*>(pDD->Find(name));
	};
	return pOB;
};// Get element from DS by name
void			vvMISSMGR::DS_SETPOS(){
	DialogsDesk* pDD = DS_GetDesck();
    if (pDD!=NULL) {
		pDD->Setx(RealLx-pDD->GetWidth()-DS_X);
		pDD->Sety(DS_Y);
	};
};
bool			vvMISSMGR::STPS_PREP(){
	GP_TextButton* pOB = DS_GetElement("StartPause");
	if (pOB!=NULL) {
		pOB->UserParam = (int)id;
		pOB->OnUserClick = vbSTPS_LCLIC;
		pOB->OnUserRightClick = vbRESET_RCLIC;
		return true;
	};
	return false;
};// Prepare Start/Pause
bool			vvMISSMGR::STPS_ENABLED(bool _st){
	bool	st = false;
	GP_TextButton* pOB = DS_GetElement("StartPause");
	if (pOB!=NULL) {
		st				= pOB->Enabled;
		pOB->Enabled	= _st;
	};
	return st;
};// Start/Pause active state
bool			vvMISSMGR::STPS_ANIM(bool _st){
	bool oldST = _st;
	stps_ANIM = _st;
	return oldST;
};
void			vvMISSMGR::STPS_PROC(){
	int N = STPS_SCRIPT.GetAmount();
	for (int i=0; i<N; i++){
		STPS_SCRIPT[i]->Process(0);
	};
};// process Start/Pause script
bool			vvMISSMGR::REST_PREP(){
	GP_TextButton* pOB = DS_GetElement("Restart");
	if (pOB!=NULL) {
		pOB->UserParam = (int)id;
		pOB->OnUserClick = vbREST_LCLIC;
		pOB->OnUserRightClick = vbRESET_RCLIC;
		return true;
	};
	return false;
};// Prepare Restart
bool			vvMISSMGR::REST_ENABLED(bool _st){
	bool	st = false;
	GP_TextButton* pOB = DS_GetElement("Restart");
	if (pOB!=NULL) {
		st				= pOB->Enabled;
		pOB->Enabled	= _st;
	};
	return st;
};// Restart active state
bool			vvMISSMGR::REST_ANIM(bool _st){
	bool oldST = _st;
	rest_ANIM = _st;
	return oldST;
};
void			vvMISSMGR::REST_PROC(){
	int N = REST_SCRIPT.GetAmount();
	for (int i=0; i<N; i++){
		REST_SCRIPT[i]->Process(0);
	};
};// process Restart script
bool			vvMISSMGR::NEXT_PREP(){
	GP_TextButton* pOB = DS_GetElement("Next");
	if (pOB!=NULL) {
		pOB->UserParam = (int)id;
		pOB->OnUserClick = vbNEXT_LCLIC;
		pOB->OnUserRightClick = vbRESET_RCLIC;
		return true;
	};
	return false;
};// Prepare Next
bool			vvMISSMGR::NEXT_ENABLED(bool _st){
	bool	st = false;
	GP_TextButton* pOB = DS_GetElement("Next");
	if (pOB!=NULL) {
		st				= pOB->Enabled;
		pOB->Enabled	= _st;
	};
	return st;
};// Next active state
bool			vvMISSMGR::NEXT_ANIM(bool _st){
	bool oldST = _st;
	next_ANIM = _st;
	return oldST;
};
void			vvMISSMGR::NEXT_PROC(){
	int N = NEXT_SCRIPT.GetAmount();
	for (int i=0; i<N; i++){
		NEXT_SCRIPT[i]->Process(0);
	};
};// process Next script
void			vvMISSMGR::MM_INIT::EvaluateFunction(){
	vvMISSMGR* pPR = get_parent<vvMISSMGR>();
	if (pPR!=NULL) {
		pPR->DS_LOAD();
		pPR->DS_PREP();
		pPR->DS_SETPOS();
		pPR->STPS_ENABLED(SP_EN);
		pPR->REST_ENABLED(RS_EN);
		pPR->NEXT_ENABLED(NX_EN);
	};
};
// vvTASKS_CII ///////////////////////////////////////////////////////////
char*	vvTEXT_EX::GetFinishedString(){ 
	if (TextID.str!=NULL){
		if (TEXT.str!=NULL) TEXT.Clear();
		
		//////////////////////////////////////////////////////////////////////////
		
		char	data[4096];
		char*	sPart = NULL;
		char	first[4096];
		char	second[4096];
		sprintf(data,"%s",GetTextByID(TextID.str));
		int NP = paramList.GetAmount();
		int cP=0;
		if (NP==0){
			TEXT = data;
			return TEXT.str;
		};

		// Have some params for insert
		bool stop=false;
		int iteration=0;
		while (!stop) {
			iteration++;
			sprintf(first,"%s","");
			sprintf(second,"%s","");
			sPart = strstr(data,"%");
			if (sPart!=NULL) {
				int nc = (int)(sPart-data+1);
				strncpy(first,data,nc+2);
				first[nc+2]=0;
				if (data[nc+2]!=0)	sprintf(second,"%s",&(data[nc+2]));
				// set params
				if (cP<NP){
					int paramType = paramList[cP]->InfID;
					switch(paramType) {
					case _vvINTEGER_:	// insetr int param
						sprintf(data,first,((vvINTEGER*)(paramList[cP]))->Value);
						strcpy(first,data);
						break;
					case _vvTEXT_:		// insert string by TextID in it
						sprintf(data,first,GetTextByID(((vvTEXT*)(paramList[cP]))->TextID.str));
						strcpy(first,data);
						break;
					case _vvPICTURE_:
						_str picture;
						if ( ((vvPICTURE*)(paramList[cP]))->GetAsStringForMessage(picture) ){
							sprintf(data,first,picture.str);
							strcpy(first,data);
						}else{
							sprintf(data,first,"NO PICTURE");
							strcpy(first,data);
						};
						break;
					};
					cP++;
				};
				// create new data string
				sprintf(data,"%s%s",first,second);
			};
			if (sPart==NULL||cP>=NP||iteration>=100) {
				stop=true;
			};
		};

		TEXT = data;

		//////////////////////////////////////////////////////////////////////////

	}else{
		TEXT = "";
	};
	return TEXT.str; 
};// Get string with all applying params
bbObjInList::bbObjInList(){
	Name="";
	DS_ID=-1;
	UN_BMP_ID.Clear();
	GP_MMP_ID.Clear();
};
bbObjInList::~bbObjInList(){
	UN_BMP_ID.Clear();
	GP_MMP_ID.Clear();
};
void	bbObjInList::Anim_MMP(bool state){
	int N=GP_MMP_ID.GetAmount();
	while (N--) {
		if (GP_MMP_ID[N]!=NULL){
			GPS.FlushBatches();
			GP_MMP_ID[N]->Animate=state;
		};
	};
};
void	bbObjInList::Draw_MMP(bool stopANIM/*=false*/){ 
	int N=GP_MMP_ID.GetAmount();
	while (N--) {
		if (GP_MMP_ID[N]!=NULL){
			GPS.FlushBatches();
			GP_MMP_ID[N]->DRAW(stopANIM);
		};
	};
};
void	bbObjInList::PreDelete(){
	int N=UN_BMP_ID.GetAmount();
	OneObject* pOB=NULL;

	while (N--) {
		DelUnit(UN_BMP_ID[N].ID);
	};

	UN_BMP_ID.Clear();
	GP_MMP_ID.Clear();
};
void	bbObjInList::DelUnit(int UID){
	// erese unit - flag
	OneObject* pOB = Group[ UID ];
	if (pOB) {
		pOB->delay=6000;
		if(pOB->LockType==1)pOB->RealDir=32;
		if(pOB->BrigadeID!=0xFFFF){
			Brigade* BR=CITY[pOB->NNUM].Brigs+pOB->BrigadeID;
			BR->vLastEnemyID=0xFFFE;
		}
		pOB->Die();
		pOB = Group[ UID ];
		if (pOB) {			
			pOB->Sdoxlo=2500;
			if(pOB->NewBuilding){
				void EliminateBuilding(OneObject* OB);
				EliminateBuilding(pOB);
				Group[ UID ]=NULL;
			};
		};
	};
	////////////////////
};
void	bbObjInList::DelPoint(int PID){
	if (PID<0||PID>=GP_MMP_ID.GetAmount()||PID>=UN_BMP_ID.GetAmount()) return;
	if (GP_MMP_ID[PID]==NULL&&(UN_BMP_ID[PID].ID==0xFFFF)) return;
	DelUnit(UN_BMP_ID[PID].ID);
	UN_BMP_ID[PID].ID=0xFFFF;
	delete ( GP_MMP_ID[PID] );
	GP_MMP_ID[PID]=NULL;
};
void	bbObjInList::UpdatePosOnMiniMap(){
	int NMM=GP_MMP_ID.GetAmount();
	if (NMM<=0)	return;
	OneObject* pOB=NULL;
	lvCAnimateGP* pMMP=NULL;
	int h,w;
	for (int i=0; i<NMM; i++){
		pMMP=GP_MMP_ID[i];
		if (pMMP!=NULL){
			pOB=Group[ UN_BMP_ID[i].ID ];
			if ( pOB!=NULL ) {
				pMMP->getPictureSize(w,h);
				int mmX = GetXOnMiniMap((pOB->RealX)>>4,(pOB->RealY)>>4)-w/2;
				int mmY = GetYOnMiniMap((pOB->RealX)>>4,(pOB->RealY)>>4)-h/2;
				pMMP->SetPosXY( mmX, mmY );
			};
			pOB=NULL;
		};
		pMMP=NULL;
	};
};
bool	vbTASK_LCLIC(SimpleDialog* SD){
	if (SD!=NULL&&SD->UserParam!=0){
		vvTASKS_CII* pPR=(vvTASKS_CII*)(SD->UserParam);
		if (pPR!=NULL) {
			pPR->vv_TASK_LCLIC();
		};
	};
	return true;
};// Click on TASK in main menu
bool	vbHINT_LCLIC(SimpleDialog* SD){
	if (SD!=NULL&&SD->UserParam!=0){
		vvTASKS_CII* pPR=(vvTASKS_CII*)(SD->UserParam);
		if (pPR!=NULL) {
			pPR->vv_HINT_LCLIC();
		};
	};
	return true;
};// Click on HINT in main menu
bool	vbELSE_LCLIC(SimpleDialog* SD){
	if (SD!=NULL&&SD->UserParam!=0){
		vvTASKS_CII* pPR=(vvTASKS_CII*)(SD->UserParam);
		if (pPR!=NULL) {
			pPR->vv_ELSE_LCLIC();
		};
	};
	return true;
};// Click on ELSE in main menu
bool	vbTASK_DELET(SimpleDialog* SD){
	if (SD==NULL)	return false;
	vvTASKS_CII* pTHE_MENU = (vvTASKS_CII*)(SD->UserParam);
	if (pTHE_MENU!=NULL) {
		pTHE_MENU->delLT_TASK( atoi(SD->AllocPtr) );
	};
	return true;
};// Click on delete button in TASK list
bool	vbHINT_DELET(SimpleDialog* SD){
	if (SD==NULL)	return false;
	vvTASKS_CII* pTHE_MENU = (vvTASKS_CII*)(SD->UserParam);
	if (pTHE_MENU!=NULL) {
		pTHE_MENU->delLT_HINT( atoi(SD->AllocPtr) );
	};
	return true;
};// Click on delete button in HINT list
bool	vbELSE_DELET(SimpleDialog* SD){
	if (SD==NULL)	return false;
	vvTASKS_CII* pTHE_MENU = (vvTASKS_CII*)(SD->UserParam);
	if (pTHE_MENU!=NULL) {
		pTHE_MENU->delLT_ELSE( atoi(SD->AllocPtr) );
	};
	return true;
};// Click on delete button in ELSE list
bool	vbTASK_MOVER(SimpleDialog* SD){
	if (SD==NULL)	return false;
	vvTASKS_CII* pTHE_MENU = (vvTASKS_CII*)(SD->UserParam);
	if (pTHE_MENU!=NULL) {
		pTHE_MENU->TAKS_START_ANIM( atoi(SD->AllocPtr) );
	};
	return true;
};
void			vvTASKS_CII::Draw(){
//	SETIN_OBJECT();
	CHECK_POINT_COND();
	DS_DRAW();
};// Function from vvBASE for draw dialogs
void			vvTASKS_CII::DrawOnMiniMap(int x,int y,int Lx,int Ly){
	TASK_POINT_DRAW();
};
vvTASKS_CII::vvTASKS_CII(){
	InfID = _vvTASKS_CII_;
	ALREADY_SETIN=true;
	NeedRestore=true;
};
vvTASKS_CII::~vvTASKS_CII(){
};
void			vvTASKS_CII::SETIN_OBJECT(){
	if (ALREADY_SETIN==false){
		DS_LOAD();
		applyMM_pos();
		applyLM_pos();
		setMM_LMClick();
		ALREADY_SETIN=true;
	};
};
bool			vvTASKS_CII::DS_LOAD(){
	bool	a_ST = false;
	if (ds_FILE.str!=NULL) {
		xmlQuote	xmlDATA( "TASK_HINT_ELSE" );
        if (xmlDATA.ReadFromFile(ds_FILE.str)) {
			ErrorPager Err;
			ds_MAIN.reset_class(&ds_MAIN);
			if (ds_MAIN.Load( xmlDATA, &ds_MAIN, &Err )) a_ST=true;
        };		
	};
	return a_ST;
};// Load DS from file
void			vvTASKS_CII::DS_DRAW(){
	if (visible){
		Restore();
		ApplyMM_state();
		ds_MAIN.ProcessDialogs();
	};
};// draw DS
DialogsDesk*	vvTASKS_CII::getMM_object(){
	DialogsDesk* pMM=NULL;
	int NDSS = ds_MAIN.DSS.GetAmount();
	if (NDSS>0){
		while (pMM==NULL && NDSS--) {
			pMM = dynamic_cast<DialogsDesk*>(ds_MAIN.DSS[NDSS]);
			if (pMM!=NULL && strcmp(pMM->Name.str,"MAIN_MENU")!=0) pMM=NULL;
		};
	};
	return pMM;
};// Get DD object for main menu
GP_Button*		vvTASKS_CII::getMM_child(char* _name){
	GP_Button* pGPP = NULL;
	if (_name!=NULL) {
		DialogsDesk* pMM = getMM_object();
		if (pMM!=NULL) {
			pGPP = dynamic_cast<GP_Button*>(pMM->Find(_name));
		};
	};
	return pGPP;
};// Get GPP object from main menu (TASK,HINT,ELSE)

void			vvTASKS_CII::applyMM_pos(){
	DialogsDesk* pMM = getMM_object();
	if (pMM!=NULL) {
		pMM->Setx(mm_X);
		pMM->Sety(mm_Y);
	};
};// Set main menu position usin (mm_X,mm_Y)

void			vvTASKS_CII::UpdateMM_state(){
	// Set statest using fill
	// ...
	// ...
	// ...
};// update main menu states
void			vvTASKS_CII::ApplyMM_state(){
	// Filling policy
	if (LM_TASK_OBJ.GetAmount()==0) {
		task_sate=1;
		if (LM_OPEN_ID==1)	LM_OPEN_ID=0;
	};
	if (LM_HINT_OBJ.GetAmount()==0) {
		hint_sate=1;
		if (LM_OPEN_ID==2)	LM_OPEN_ID=0;
	};
	if (LM_ELSE_OBJ.GetAmount()==0) {
		else_sate=1;
		if (LM_OPEN_ID==3)	LM_OPEN_ID=0;
	};

	// LIST MENU
	ApplyLM_state();
	
	// LAST APLLY FOR VIEW
	int oldT = mm_last_anim_time;
	// TASK
	mm_last_anim_time = oldT;
	ApplyMM_state(getMM_child("TASK"),task_sate);
	int MM_oldT=mm_last_anim_time;
	// HINT
	mm_last_anim_time = oldT;
	ApplyMM_state(getMM_child("HINT"),hint_sate);
	int MM_oldH=mm_last_anim_time;
	// ELSE
	mm_last_anim_time = oldT;
	ApplyMM_state(getMM_child("ELSE"),else_sate);
	int MM_oldE=mm_last_anim_time;
	
	mm_last_anim_time = max( max(oldT,MM_oldT), max(MM_oldH,MM_oldE) );
};// apply current state for main menu
void			vvTASKS_CII::ApplyMM_state(GP_Button* pP,int _st){
	if (pP==NULL)	return;
	pP->Visible=true;
	switch(_st) {
	case 1:		// disable
		pP->Enabled=false;
		pP->Active=false;
		pP->Diffuse=anim_diffuse;
		break;
	case 2:		// enable
		pP->Enabled=true;
		pP->Active=false;
		pP->Diffuse=0xFFffffff;
		break;
	case 3:		// enable+animate
		pP->Enabled=true;
		if (GetTickCount()-mm_last_anim_time>=mm_anim_delay){
			if (pP->Diffuse==anim_diffuse)	pP->Diffuse=0xFFffffff;
			else							pP->Diffuse=anim_diffuse;
			mm_last_anim_time=GetTickCount();
		};
		break;
	case 4:		// opened
		pP->Enabled=true;
		pP->Active=true;
		pP->Diffuse=0xFFffffff;
		break;
	default:	// hide
		pP->Visible=false;
		pP->Enabled=false;
	};
};
void			vvTASKS_CII::setMM_LMClick(){
	GP_Button*	pGPB_TASK =	getMM_child("TASK");
	if (pGPB_TASK!=NULL)	setMM_LMClick(pGPB_TASK,this,vbTASK_LCLIC);
	GP_Button*	pGPB_HINT =	getMM_child("HINT");
	if (pGPB_HINT!=NULL)	setMM_LMClick(pGPB_HINT,this,vbHINT_LCLIC);
	GP_Button*	pGPB_ELSE =	getMM_child("ELSE");
	if (pGPB_ELSE!=NULL)	setMM_LMClick(pGPB_ELSE,this,vbELSE_LCLIC);
};
void			vvTASKS_CII::setMM_LMClick(GP_Button* pP,vvTASKS_CII* pPARAM,VCall* pFUNC){
	if (pP!=NULL&&pPARAM!=NULL&&pFUNC!=NULL) {
		pP->UserParam	=(int)pPARAM;
		pP->OnUserClick	=pFUNC;
	};
};
void			vvTASKS_CII::applyLM_pos(){
	setLM_TASK_POS();
	setLM_HINT_POS();
	setLM_ELSE_POS();
};// Set list menu position usin (dl_X,dl_Y)
void			vvTASKS_CII::setLM_ID(){
	int ID=0;
	int NT=TASK.GetAmount();
	for (int i=0; i<NT; i++){
		((vvTEXT_EX*)(TASK[i]))->id=ID;
		ID++;
	};
	int NH=HINT.GetAmount();
	for (int i=0; i<NH; i++){
		((vvTEXT_EX*)(HINT[i]))->id=ID;
		ID++;
	};
	int NE=ELSE.GetAmount();
	for (int i=0; i<NE; i++){
		((vvTEXT_EX*)(ELSE[i]))->id=ID;
		ID++;
	};
};// Set unique id for vvTEXT_EX elements
ListDesk*		vvTASKS_CII::getLM_child(char* _name){
    ListDesk* pLD=NULL;
	if (_name!=NULL) {
		int NDSS = ds_MAIN.DSS.GetAmount();
		if (NDSS>0){
			while (pLD==NULL && NDSS--) {
				pLD = dynamic_cast<ListDesk*>(ds_MAIN.DSS[NDSS]);
				if (pLD!=NULL && strcmp(pLD->Name.str,_name)!=0) pLD=NULL;
			};
		};
	};
	return pLD;
};// Get GPP object from list menu (TASK,HINT,ELSE)
void			vvTASKS_CII::ApplyLM_state(){
	setLM_TASK_VIS(false);
	setLM_HINT_VIS(false);
	setLM_ELSE_VIS(false);
	switch(LM_OPEN_ID) {
	case 1:	// TASK LIST
		setLM_TASK_VIS(true);
		break;
	case 2:	// HINT LIST
		setLM_HINT_VIS(true);
		break;
	case 3:	// ELSE LIST
		setLM_ELSE_VIS(true);
		break;
	};
};// apply current state for list menu
ListDesk*		vvTASKS_CII::getLM_TASK(){
	return getLM_child("TASK_LIST");
};// Get ListDesck accoding to TASK
bool			vvTASKS_CII::setLM_TASK_VIS(bool _st){
	bool oldST=false;
	ListDesk* pLDT = getLM_TASK();
	if (pLDT!=NULL) {
		oldST=pLDT->Visible;
		pLDT->Visible=_st;
	};
	return oldST;
};// Set TASK LIST visibility
void			vvTASKS_CII::setLM_TASK_POS(){
	DialogsDesk* pDD = getMM_object();
	if (pDD!=NULL) {
		int DD_W = pDD->GetWidth();
		int DD_H = pDD->GetHeight();
		ListDesk* pLDT = getLM_TASK();
		if (pLDT!=NULL) {
			pLDT->Setx(mm_X+DD_W+dl_X);
			if (dl_Y!=-1) pLDT->Sety(dl_Y);
			else		  pLDT->Sety(mm_Y);
		};
	};
};// Set list menu for TASK position
void			vvTASKS_CII::TAKS_START_ANIM(int Task_ID){
	int mlID = Assign(Task_ID,LM_TASK_OBJ);
	if (mlID==-1)	return;
	LM_TASK_OBJ[mlID]->Anim_MMP(true);
};
void			vvTASKS_CII::TASK_POINT_DRAW(){
	int N=LM_TASK_OBJ.GetAmount();
	while (N--) {
		if (LM_TASK_OBJ[N]!=NULL){
			LM_TASK_OBJ[N]->UpdatePosOnMiniMap();
			LM_TASK_OBJ[N]->Draw_MMP(true);
		};
	};
};
bool			vvTASKS_CII::addLT_TASK(vvTEXT_EX* pT,char* name/*=NULL*/,int pos/*=0*/){
	if (pT==NULL)	return false;
	ListDesk* pLD = getLM_TASK();
	if (pLD!=NULL) {
		int newN = pLD->AddElement(pos,"");
		bbObjInList* pTL=new bbObjInList();
		pTL->DS_ID = newN;
		if (name!=NULL&&name[0]!=0)		pTL->Name=name;
		else if (pT->Name.str!=NULL)	pTL->Name=pT->Name.str;
		else							pTL->Name="NONAME";
		//////////////////////////////////////////////////////////////////////////
		int xxx,yyy;
		int mxxx,myyy;
		int h,w;

		int np = pT->paramList_EX.GetAmount();
		vvPOINT2D* pPOS=NULL;
		for (int i=0; i<np; i++){
			pPOS = dynamic_cast<vvPOINT2D*>(pT->paramList_EX[i]);
			if (pPOS!=NULL&&pPOS->CondState()) {
				// Set point on bigMAP
				pPOS->GetTR(xxx,yyy);
				xxx=xxx<<4;
				yyy=yyy<<4;

				int CreateNewTerrMons2(byte NI,int x,int y,word Type);
				int ID=CreateNewTerrMons2(0,xxx,yyy,PointOnBigMap);
				OneObject* pOOB = Group[ID];
				if(ID!=-1&& pOOB!=NULL && (!pOOB->Sdoxlo||pOOB->Hidden)){
					OneUS* pUS = new OneUS();
					pUS->ID = pOOB->Index;
					pUS->SN = pOOB->Serial;
					pTL->UN_BMP_ID.Add(*pUS);
				};

				// Set point on miniMAP
				mxxx = GetXOnMiniMap(xxx>>4,yyy>>4);
				myyy = GetYOnMiniMap(xxx>>4,yyy>>4);
				lvCAnimateGP* pMMP = new lvCAnimateGP();
				*pMMP = PointOnMiniMap;
				h=0;
				w=0;
				pMMP->getPictureSize(w,h);
				pMMP->SetPosXY(mxxx-w/2,myyy-h/2);
				pMMP->SetVisible(true);
				pTL->GP_MMP_ID.Add(pMMP);
			};
		};
		//////////////////////////////////////////////////////////////////////////
		
		DialogsDesk* pTaskEL = (DialogsDesk*)(pLD->Find(newN));
		if (pTaskEL!=NULL) {
			TextButton*	 pTxtBTN = (TextButton*)(pTaskEL->Find("Mess"));
			GPPicture*	 pDEL_BTN = (GPPicture*)(pTaskEL->Find("Delete"));
			if (pDEL_BTN!=NULL&&pTxtBTN!=NULL) {
				pTxtBTN->SetMessage( pT->GetFinishedString() );

				pDEL_BTN->Visible=false;
				// Container Class
				pDEL_BTN->UserParam = (int)(this);
				pTxtBTN->UserParam = (int)(this);
				// Task index in list
				char sID[32];
				sprintf(sID,"%d",newN);
				pDEL_BTN->AllocPtr = (char *)malloc( strlen(sID)+1 );
				if (pDEL_BTN->AllocPtr!=NULL) {
					sprintf(pDEL_BTN->AllocPtr,"%s",sID);
				};
				pTxtBTN->AllocPtr = (char *)malloc( strlen(sID)+1 );
				if (pTxtBTN->AllocPtr!=NULL) {
					sprintf(pTxtBTN->AllocPtr,"%s",sID);
				};
				// CallBack Function
				pDEL_BTN->OnUserClick=vbTASK_DELET;
				pTxtBTN->OnMouseOver=vbTASK_MOVER;
			};
		};

		LM_TASK_OBJ.Add(pTL);
	};
	if (task_sate!=4) task_sate=3;
	return true;
};// Add element from TASK in list menu for TASK
bool			vvTASKS_CII::addLT_TASK(const char* TName,int pos/*=0*/,bool Dublicate/*=true*/){
	if (TName==NULL)	return false;
	vvTEXT_EX*	pTASK = dynamic_cast<vvTEXT_EX*>(getByNAME(TName,TASK));
	if (pTASK==NULL)	return false;
	if (Dublicate==false)	delLT_TASK(pTASK->Name.str);
	return addLT_TASK(pTASK,NULL,pos);
};
void			vvTASKS_CII::delLT_TASK(int Task_ID){
	int mlID = Assign(Task_ID,LM_TASK_OBJ);
	if (mlID==-1)	return;
	ListDesk* pLD = getLM_TASK();
	if (pLD!=NULL) {
		if (pLD->DelElementByUID(Task_ID)){
			LM_TASK_OBJ[mlID]->PreDelete();
			LM_TASK_OBJ.DelElement(mlID);
		};
	};
};// Delete task from menu list by it id in LM_TAKS
void			vvTASKS_CII::delLT_TASK(const char* Task_Name){
	if (Task_Name==NULL) return;
	int Task_ID=-1;
	int N=LM_TASK_OBJ.GetAmount();
	for (int i=0; (i<N)&&(Task_ID==-1); i++){
		if ( strcmp(Task_Name,LM_TASK_OBJ[i]->Name.str)==0 ) Task_ID=LM_TASK_OBJ[i]->DS_ID;
	};
	if (Task_ID!=-1) {
		delLT_TASK(Task_ID);
	};
};// Delete task from menu list by it id in LM_TAKS
void			vvTASKS_CII::TASK_RESTORE(){
	ListDesk* pLD = getLM_TASK();
	int N=LM_TASK_OBJ.GetAmount();

	for (int i=0; i<N; i++){
		DialogsDesk* pTaskEL = (DialogsDesk*)(pLD->Find(LM_TASK_OBJ[i]->DS_ID));
		if (pTaskEL!=NULL) {
			TextButton*	 pTxtBTN = (TextButton*)(pTaskEL->Find("Mess"));
			GPPicture*	 pDEL_BTN = (GPPicture*)(pTaskEL->Find("Delete"));
			if (pDEL_BTN!=NULL&&pTxtBTN!=NULL) {
				// Container Class
				pDEL_BTN->UserParam = (int)(this);
				pTxtBTN->UserParam = (int)(this);
				// Task index in list
				char sID[32];
				sprintf(sID,"%d",LM_TASK_OBJ[i]->DS_ID);
				pDEL_BTN->AllocPtr = (char *)malloc( strlen(sID)+1 );
				if (pDEL_BTN->AllocPtr!=NULL) {
					sprintf(pDEL_BTN->AllocPtr,"%s",sID);
				};
				pTxtBTN->AllocPtr = (char *)malloc( strlen(sID)+1 );
				if (pTxtBTN->AllocPtr!=NULL) {
					sprintf(pTxtBTN->AllocPtr,"%s",sID);
				};
				// CallBack Function
				pDEL_BTN->OnUserClick=vbTASK_DELET;
				pTxtBTN->OnMouseOver=vbTASK_MOVER;
			};
		};
	};
};
void			vvTASKS_CII::CHECK_POINT_COND(){
	int NinList = LM_TASK_OBJ.GetAmount();
	if (NinList<=0)	return;
	bbObjInList* pOBinList=NULL;
	vvTEXT_EX*	 pOBinTASK=NULL;
	for (int i=0; i<NinList; i++){
		pOBinList=LM_TASK_OBJ[i];
		pOBinTASK = dynamic_cast<vvTEXT_EX*>( getByNAME(pOBinList->Name.str,TASK) );
		if (pOBinList!=NULL) {
			int nPOINT=pOBinTASK->paramList_EX.GetAmount();	
			for (int p=0; p<nPOINT; p++){
				if ( ((vvPOINT2D*)(pOBinTASK->paramList_EX[p]))->CondState()==false ){
					pOBinList->DelPoint(p);
				};
			};
		};
	};
};// Check and dell if condition for point donot true
void			vvTASKS_CII::setLT_TASK_COMPLITE(const char* Task_Name){
	if (Task_Name==NULL) return;
	int Task_ID=-1;
	int mlID=-1;
	int N=LM_TASK_OBJ.GetAmount();
	for (int i=0; (i<N)&&(Task_ID==-1); i++){
		if ( strcmp(Task_Name,LM_TASK_OBJ[i]->Name.str)==0 ){
			Task_ID=LM_TASK_OBJ[i]->DS_ID;
			mlID=i;
		};
	};
	if (Task_ID!=-1&&mlID!=-1) {
		ListDesk* pLD = getLM_TASK();
		if (pLD!=NULL) {
			LM_TASK_OBJ[mlID]->PreDelete();
			DialogsDesk* pTaskEL = (DialogsDesk*)(pLD->Find(LM_TASK_OBJ[mlID]->DS_ID));
			if (pTaskEL!=NULL){
				GPPicture*	 pDEL_BTN = (GPPicture*)(pTaskEL->Find("Delete"));
				if (pDEL_BTN!=NULL) {
					pDEL_BTN->Visible=true;
				};
			};
		};
	};
};
ListDesk*		vvTASKS_CII::getLM_HINT(){
	return getLM_child("HINT_LIST");
};// Get ListDesck accoding to HINT
bool			vvTASKS_CII::setLM_HINT_VIS(bool _st){
	bool oldST=false;
	ListDesk* pLDH = getLM_HINT();
	if (pLDH!=NULL) {
		oldST=pLDH->Visible;
		pLDH->Visible=_st;
	};
	return oldST;
};// Set HINT LIST visibility
void			vvTASKS_CII::setLM_HINT_POS(){
	DialogsDesk* pDD = getMM_object();
	if (pDD!=NULL) {
		int DD_W = pDD->GetWidth();
		int DD_H = pDD->GetHeight();
		ListDesk* pLDH = getLM_HINT();
		if (pLDH!=NULL) {
			pLDH->Setx(mm_X+DD_W+dl_X);
			if (dl_Y!=-1) pLDH->Sety(dl_Y);
			else		  pLDH->Sety(mm_Y);
		};
	};
};// Set list menu for HINT position
bool			vvTASKS_CII::addLT_HINT(vvTEXT_EX* pH,char* name/*=NULL*/,int pos/*=0*/){
	if (pH==NULL)	return false;
	ListDesk* pLD = getLM_HINT();
	if (pLD!=NULL) {
		int newN = pLD->AddElement(pos,"");
		bbObjInList* pHL=new bbObjInList();
		pHL->DS_ID = newN;
		if (name!=NULL&&name[0]!=0)		pHL->Name=name;
		else if (pH->Name.str!=NULL)	pHL->Name=pH->Name.str;
		else							pHL->Name="NONAME";
		DialogsDesk* pHintEL = (DialogsDesk*)(pLD->Find(newN));
		if (pHintEL!=NULL) {
			TextButton*	 pTxtBTN = (TextButton*)(pHintEL->Find("Mess"));
			if (pTxtBTN!=NULL) {
				pTxtBTN->SetMessage( pH->GetFinishedString() );
			};
			GPPicture*	 pDEL_BTN = (GPPicture*)(pHintEL->Find("Delete"));
			if (pDEL_BTN!=NULL) {
				// Container Class
				pDEL_BTN->UserParam = (int)(this);
				// Task index in list
				char sID[32];
				sprintf(sID,"%d",newN);
				pDEL_BTN->AllocPtr = (char *)malloc( strlen(sID)+1 );
				if (pDEL_BTN->AllocPtr!=NULL) {
					sprintf(pDEL_BTN->AllocPtr,"%s",sID);
				};
				// CallBack Function
				pDEL_BTN->OnUserClick=vbHINT_DELET;
			};
		};
		LM_HINT_OBJ.Add(pHL);
	};
	if (hint_sate!=4) hint_sate=3;
	return true;
};// Add element from HINT in list menu for HINT
bool			vvTASKS_CII::addLT_HINT(const char* HName,int pos/*=0*/,bool Dublicate/*=true*/){
	if (HName==NULL)	return false;
	vvTEXT_EX*	pHINT = dynamic_cast<vvTEXT_EX*>(getByNAME(HName,HINT));
	if (pHINT==NULL)	return false;
	if (Dublicate==false)	delLT_HINT(pHINT->Name.str);
	return addLT_HINT(pHINT,NULL,pos);
};
void			vvTASKS_CII::delLT_HINT(int Hint_ID){
	int mlID = Assign(Hint_ID,LM_HINT_OBJ);
	if (mlID==-1)	return;
	ListDesk* pLD = getLM_HINT();
	if (pLD!=NULL) {
		if (pLD->DelElementByUID(Hint_ID)){
			LM_HINT_OBJ[mlID]->PreDelete();
			LM_HINT_OBJ.DelElement(mlID);
		};
	};
};// Delete hint from menu list by it id in LM_HINT
void			vvTASKS_CII::delLT_HINT(const char* Hint_Name){
	if (Hint_Name==NULL) return;
	int Hint_ID=-1;
	int N=LM_HINT_OBJ.GetAmount();
	for (int i=0; (i<N)&&(Hint_ID==-1); i++){
		if ( strcmp(Hint_Name,LM_HINT_OBJ[i]->Name.str)==0 ) Hint_ID=LM_HINT_OBJ[i]->DS_ID;
	};
	if (Hint_ID!=-1) {
		delLT_HINT(Hint_ID);
	};
};// Delete hint from menu list by it id in LM_HINT
void			vvTASKS_CII::HINT_RESTORE(){
	ListDesk* pLD = getLM_HINT();
	int N=LM_HINT_OBJ.GetAmount();

	for (int i=0; i<N; i++){
		DialogsDesk* pHintEL = (DialogsDesk*)(pLD->Find(LM_HINT_OBJ[i]->DS_ID));
		if (pHintEL!=NULL) {
			TextButton*	 pTxtBTN = (TextButton*)(pHintEL->Find("Mess"));
			GPPicture*	 pDEL_BTN = (GPPicture*)(pHintEL->Find("Delete"));
			if (pDEL_BTN!=NULL&&pTxtBTN!=NULL) {
				// Container Class
				pDEL_BTN->UserParam = (int)(this);
				// Task index in list
				char sID[32];
				sprintf(sID,"%d",LM_HINT_OBJ[i]->DS_ID);
				pDEL_BTN->AllocPtr = (char *)malloc( strlen(sID)+1 );
				if (pDEL_BTN->AllocPtr!=NULL) {
					sprintf(pDEL_BTN->AllocPtr,"%s",sID);
				};
				// CallBack Function
				pDEL_BTN->OnUserClick=vbHINT_DELET;
			};
		};
	};
};
ListDesk*		vvTASKS_CII::getLM_ELSE(){
	return getLM_child("ELSE_LIST");
};// Get ListDesck accoding to ELSE
bool			vvTASKS_CII::setLM_ELSE_VIS(bool _st){
	bool oldST=false;
	ListDesk* pLDE = getLM_ELSE();
	if (pLDE!=NULL) {
		oldST=pLDE->Visible;
		pLDE->Visible=_st;
	};
	return oldST;
};// Set ELSE LIST visibility
void			vvTASKS_CII::setLM_ELSE_POS(){
	DialogsDesk* pDD = getMM_object();
	if (pDD!=NULL) {
		int DD_W = pDD->GetWidth();
		int DD_H = pDD->GetHeight();
		ListDesk* pLDE = getLM_ELSE();
		if (pLDE!=NULL) {
			pLDE->Setx(mm_X+DD_W+dl_X);
			if (dl_Y!=-1) pLDE->Sety(dl_Y);
			else		  pLDE->Sety(mm_Y);
		};
	};
};// Set list menu for ELSE position
bool			vvTASKS_CII::addLT_ELSE(vvTEXT_EX* pE,char* name/*=NULL*/,int pos/*=0*/){
	if (pE==NULL)	return false;
	ListDesk* pLD = getLM_ELSE();
	if (pLD!=NULL) {
		int newN = pLD->AddElement(pos,"");
		bbObjInList* pEL=new bbObjInList();
		pEL->DS_ID = newN;
		if (name!=NULL&&name[0]!=0)		pEL->Name=name;
		else if (pEL->Name.str!=NULL)	pEL->Name=pE->Name.str;
		else							pEL->Name="NONAME";
		DialogsDesk* pElseEL = (DialogsDesk*)(pLD->Find(newN));
		if (pElseEL!=NULL) {
			TextButton*	 pTxtBTN = (TextButton*)(pElseEL->Find("Mess"));
			if (pTxtBTN!=NULL) {
				pTxtBTN->SetMessage( pE->GetFinishedString() );
			};
			GPPicture*	 pDEL_BTN = (GPPicture*)(pElseEL->Find("Delete"));
			if (pDEL_BTN!=NULL) {
				// Container Class
				pDEL_BTN->UserParam = (int)(this);
				// Task index in list
				char sID[32];
				sprintf(sID,"%d",newN);
				pDEL_BTN->AllocPtr = (char *)malloc( strlen(sID)+1 );
				if (pDEL_BTN->AllocPtr!=NULL) {
					sprintf(pDEL_BTN->AllocPtr,"%s",sID);
				};
				// CallBack Function
				pDEL_BTN->OnUserClick=vbELSE_DELET;
			};
		};
		LM_ELSE_OBJ.Add(pEL);
	};
	if (else_sate!=4) else_sate=3;
	return true;
};// Add element from ELSE in list menu for ELSE
bool			vvTASKS_CII::addLT_ELSE(const char* EName,int pos/*=0*/,bool Dublicate/*=true*/){
	if (EName==NULL)	return false;
	vvTEXT_EX*	pELSE = dynamic_cast<vvTEXT_EX*>(getByNAME(EName,ELSE));
	if (pELSE==NULL)	return false;
	if (Dublicate==false)	delLT_ELSE(pELSE->Name.str);
	return addLT_ELSE(pELSE,NULL,pos);
};
void			vvTASKS_CII::delLT_ELSE(int Else_ID){
	int mlID = Assign(Else_ID,LM_ELSE_OBJ);
	if (mlID==-1)	return;
	ListDesk* pLD = getLM_ELSE();
	if (pLD!=NULL) {
		if (pLD->DelElementByUID(Else_ID)){
			LM_ELSE_OBJ[mlID]->PreDelete();
			LM_ELSE_OBJ.DelElement(mlID);
		};
	};
};// Delete else from menu list by it id in LM_ELSE
void			vvTASKS_CII::delLT_ELSE(const char* Else_Name){
	if (Else_Name==NULL) return;
	int Else_ID=-1;
	int N=LM_ELSE_OBJ.GetAmount();
	for (int i=0; (i<N)&&(Else_ID==-1); i++){
		if ( strcmp(Else_Name,LM_ELSE_OBJ[i]->Name.str)==0 ) Else_ID=LM_ELSE_OBJ[i]->DS_ID;
	};
	if (Else_ID!=-1) {
		delLT_ELSE(Else_ID);
	};
};// Delete else from menu list by it id in LM_ELSE
void			vvTASKS_CII::ELSE_RESTORE(){
	ListDesk* pLD = getLM_ELSE();
	int N=LM_ELSE_OBJ.GetAmount();

	for (int i=0; i<N; i++){
		DialogsDesk* pElseEL = (DialogsDesk*)(pLD->Find(LM_ELSE_OBJ[i]->DS_ID));
		if (pElseEL!=NULL) {
			TextButton*	 pTxtBTN = (TextButton*)(pElseEL->Find("Mess"));
			GPPicture*	 pDEL_BTN = (GPPicture*)(pElseEL->Find("Delete"));
			if (pDEL_BTN!=NULL&&pTxtBTN!=NULL) {
				// Container Class
				pDEL_BTN->UserParam = (int)(this);
				// Task index in list
				char sID[32];
				sprintf(sID,"%d",LM_ELSE_OBJ[i]->DS_ID);
				pDEL_BTN->AllocPtr = (char *)malloc( strlen(sID)+1 );
				if (pDEL_BTN->AllocPtr!=NULL) {
					sprintf(pDEL_BTN->AllocPtr,"%s",sID);
				};
				// CallBack Function
				pDEL_BTN->OnUserClick=vbELSE_DELET;
			};
		};
	};
};
int				vvTASKS_CII::Assign(int el,LinearArray<int,_int>& Arr){
	int ass=-1;
	int N=Arr.GetAmount();
	for (int i=0; (i<N)&&(ass==-1); i++){
		if (Arr[i]==el)	ass=i;
	};
	return ass;
};
int				vvTASKS_CII::Assign(int el,ClassArray<bbObjInList>& Arr){
	int ass =-1;
	int N=Arr.GetAmount();
	for (int i=0; (i<N)&&(ass==-1); i++){
		if ( Arr[i]->DS_ID==el ) ass=i;
	};
	return ass;
};
bbTEXT*			vvTASKS_CII::getByNAME(const char* name,ClassArray<bbTEXT>& Arr){
	if (name==NULL)	return NULL;
	vvTEXT_EX* pOB=NULL;
	int N=Arr.GetAmount();
	for (int i=0; i<N; i++){
		pOB = dynamic_cast<vvTEXT_EX*>(Arr[i]);
		if (pOB!=NULL&&pOB->Name.str!=NULL&&strcmp(pOB->Name.str,name)==0)	return pOB;
	};
	return pOB;
};
void			vvTASKS_CII::Restore(){
	if (NeedRestore){
		NeedRestore=false;
		setMM_LMClick();	// Restore mouse click on letter
		// restore click on TASK/HINT/ELSE List
		TASK_RESTORE();
		HINT_RESTORE();
		ELSE_RESTORE();
	};
};
bool			vvTASKS_CII::addLT_TASK_lua(const char* TName,int pos,bool Dublicate){
	return addLT_TASK(TName,pos,Dublicate);
};
bool			vvTASKS_CII::addLT_HINT_lua(const char* HName,int pos,bool Dublicate){
	return addLT_HINT(HName,pos,Dublicate);
};
bool			vvTASKS_CII::addLT_ELSE_lua(const char* EName,int pos,bool Dublicate){
	return addLT_ELSE(EName,pos,Dublicate);
};
void			vvTASKS_CII::delLT_TASK_lua(const char* Task_Name){
	delLT_TASK(Task_Name);
};
void			vvTASKS_CII::delLT_HINT_lua(const char* Hint_Name){
	delLT_HINT(Hint_Name);
};
void			vvTASKS_CII::delLT_ELSE_lua(const char* Else_Name){
	delLT_ELSE(Else_Name);
};
void			vvTASKS_CII::vv_TASK_LCLIC(){
	if (LM_OPEN_ID==1){
		LM_OPEN_ID=0;
		task_sate=2;
	}else if (LM_TASK_OBJ.GetAmount()>0){
		if (hint_sate==4)	hint_sate=2;
		if (else_sate==4)	else_sate=2;
		LM_OPEN_ID=1;
		task_sate=4;
	}else{
		if (LM_OPEN_ID==2) {
			LM_OPEN_ID=0;
			hint_sate=2;
		};
		if (LM_OPEN_ID==3) {
			LM_OPEN_ID=0;
			else_sate=2;
		};
	};
};
void			vvTASKS_CII::vv_HINT_LCLIC(){
	if (LM_OPEN_ID==2){
		LM_OPEN_ID=0;
		hint_sate=2;
	}else if (LM_HINT_OBJ.GetAmount()>0){
		if (task_sate==4)	task_sate=2;
		if (else_sate==4)	else_sate=2;
		LM_OPEN_ID=2;
		hint_sate=4;
	}else{
		if (LM_OPEN_ID==1) {
			LM_OPEN_ID=0;
			task_sate=2;
		};
		if (LM_OPEN_ID==3) {
			LM_OPEN_ID=0;
			else_sate=2;
		};
	};
};
void			vvTASKS_CII::vv_ELSE_LCLIC(){
	if (LM_OPEN_ID==3){
		LM_OPEN_ID=0;
		else_sate=2;
	}else if (LM_ELSE_OBJ.GetAmount()>0){
		if (task_sate==4)	task_sate=2;
		if (hint_sate==4)	hint_sate=2;
		LM_OPEN_ID=3;
		else_sate=4;
	}else{
		if (LM_OPEN_ID==1) {
			LM_OPEN_ID=0;
			task_sate=2;
		};
		if (LM_OPEN_ID==2) {
			LM_OPEN_ID=0;
			hint_sate=2;
		};
	};
};
void	vvTASKS_CII::FCN00::EvaluateFunction(){
	vvTASKS_CII* pPR = get_parent<vvTASKS_CII>();
	if (pPR!=NULL) {
		pPR->DS_LOAD();
	};
};
void	vvTASKS_CII::FCN01::EvaluateFunction(){
	vvTASKS_CII* pPR = get_parent<vvTASKS_CII>();
	if (pPR!=NULL) {
		pPR->applyMM_pos();
	};
};
void	vvTASKS_CII::FCN02::EvaluateFunction(){
	vvTASKS_CII* pPR = get_parent<vvTASKS_CII>();
	if (pPR!=NULL) {
		pPR->applyLM_pos();
	};
};
void	vvTASKS_CII::FCN03::EvaluateFunction(){
	vvTASKS_CII* pPR = get_parent<vvTASKS_CII>();
	if (pPR!=NULL) {
		pPR->setMM_LMClick();
	};
};
void	vvTASKS_CII::FCN04::EvaluateFunction(){
	vvTASKS_CII* pPR = get_parent<vvTASKS_CII>();
	if (pPR!=NULL) {
		pPR->setLM_ID();
	};
};
void	vvTASKS_CII::FCN05::EvaluateFunction(){
	vvTASKS_CII* pPR = get_parent<vvTASKS_CII>();
	if (pPR!=NULL) {
		TASK.getELEMENT("../TASK Name");
		if (TASK.INDEX!=-1){
			vvTEXT_EX* pT = dynamic_cast<vvTEXT_EX*>(pPR->TASK[TASK.INDEX]);
			pPR->addLT_TASK(pT);
		};
	};
};
void	vvTASKS_CII::FCN06::EvaluateFunction(){
	vvTASKS_CII* pPR = get_parent<vvTASKS_CII>();
	if (pPR!=NULL) {
		HINT.getELEMENT("../HINT Name");
		if (HINT.INDEX!=-1) {
			vvTEXT_EX* pH = dynamic_cast<vvTEXT_EX*>(pPR->HINT[HINT.INDEX]);
			pPR->addLT_HINT(pH);
		};
	};
};
void	vvTASKS_CII::FCN07::EvaluateFunction(){
	vvTASKS_CII* pPR = get_parent<vvTASKS_CII>();
	if (pPR!=NULL) {
		ELSE.getELEMENT("../ELSE Name");
		if (ELSE.INDEX!=-1) {
			vvTEXT_EX* pE = dynamic_cast<vvTEXT_EX*>(pPR->ELSE[ELSE.INDEX]);
			pPR->addLT_ELSE(pE);
		};
	};
};
// vvBrigAI //////////////////////////////////////////////////////////////
void	AI_BORDER::setRaport(){
	switch(CURRENT_ORDER) {
	case 0:	// ai_STOP
		OrderName = "STOP";
		break;
	case 1:	// ai_MOVE
		OrderName = "MOVE";
		break;
	};
	CreateTime = GetTickCount();
	ImplemTime = GetTickCount();
	setTimeInHMSMs(CreateTmBC,CreateTime);
	setTimeInHMSMs(ImplemTmBC,ImplemTime);
};
void	AI_BORDER::setTimeInHMSMs(_str& strval,DWORD time){
	time_t aclock;
	struct tm *newtime;

	int msec = (int)(CreateTime-(CreateTime/1000)*1000);
	aclock = (time_t)(CreateTime/1000);
	newtime->tm_hour=0;
	newtime->tm_min=0;
	newtime->tm_sec=0;
	newtime = localtime( &aclock );
	strval = "";
	strval += newtime->tm_hour;
	strval += " : ";
	strval += newtime->tm_min;
	strval += " : ";
	strval += newtime->tm_sec;
	strval += " : ";
	strval += msec;
};
DWORD	AI_BORDER::GetClassMask(){
	DWORD MASK = 0x00000001;
	switch(CURRENT_ORDER) {
	case 0:	// ai_STOP
		MASK = 0x00000001;
		break;
	case 1:	// ai_MOVE
		MASK = 0x00000002;
		break;
	};
	return MASK;
};
void	AI_BORDER::AI_RAPORT::EvaluateFunction(){
	AI_BORDER* pPR = get_parent<AI_BORDER>();
	if (pPR!=NULL) {
		pPR->setRaport();
	};
};
vvBrigAI::vvBrigAI(){
	InfID = _vvBrigAI_;
};
vvBrigAI::~vvBrigAI(){};
void	vvBrigAI::setDefSettings(){
};// Set settings in defoult
bool	vvBrigAI::setAI_BrigID(int _id){
	return true;
};// Set AI controled brigade id
bool	vvBrigAI::addEN_BrigID(int _id){
	return true;
};// add EN brigades id
void	vvBrigAI::UpdateState(){
};// Update state param
void	vvBrigAI::CreateOrderList(){
};// Create new order if need
void	vvBrigAI::ImplementOrdes(){
};// Implament order
void	vvBrigAI::PROCESS(){
	UpdateState();
	CreateOrderList();
	ImplementOrdes();
};// General process function
// OperationMesMgr ///////////////////////////////////////////////////////
OperationMesMgr::OperationMesMgr() : SimpleMesMgr() {
	// ...
};
OperationMesMgr::OperationMesMgr(OperationMesMgr& MM) : SimpleMesMgr( *( dynamic_cast<SimpleMesMgr*>(&MM) ) ) {
	SCRIPT.Clear();
	int N = MM.SCRIPT.GetAmount();
	lvCBaseScript* pCurSCR=NULL;
	while (N--) {
		MM.SCRIPT[N]->GetCopy((lvCBaseScript**)(&pCurSCR));
		if (pCurSCR!=NULL) {
			SCRIPT.Add(pCurSCR);
		};
		pCurSCR=NULL;
	};
};
OperationMesMgr::~OperationMesMgr(){
	SCRIPT.Clear();
};
void		OperationMesMgr::ShowDialog(){
	SimpleMesMgr::ShowDialog();

    int N = SCRIPT.GetAmount();
	for (int i=0; i<N; i++){
		SCRIPT[i]->Process(0);
	};
};

//======================================================================//
//======================================================================//

// lvCEdge ///////////////////////////////////////////////////////////////
#define EDGE_FONT	SmallWhiteFont1
#define pEDGE_FONT	&SmallWhiteFont1
extern int LastMx;
extern int LastMy;
void	lvDrawArrow(int x,int y,int x1,int y1,int dx,int dy,DWORD color);
void	lvDrawArrow(lvCNode* nd1,lvCNode* nd2,int dx,int dy,DWORD color);

lvCEdge::lvCEdge(lvCEdge* pEdge) : lvCStorage(dynamic_cast<lvCStorage*>(pEdge)) {
	if (pEdge==NULL)	return;

	begID=pEdge->begID;
	endID=pEdge->endID;
	procTime=pEdge->procTime;
	startTime=pEdge->startTime;

	// Select Condition
	lvCCondition* _pSelectCondition = NULL;
	if (pEdge->SelectCondition.Get()!=NULL){
		pEdge->SelectCondition.Get()->GetCopy(&_pSelectCondition);
	};
	if (_pSelectCondition!=NULL) SelectCondition.Set(_pSelectCondition);
	_pSelectCondition=NULL;
	
	// Condition / Operation
	lvCCondForOper* _pCondForOper=NULL;
	for (int i=0; i<pEdge->CondForOper.GetAmount(); i++){
		pEdge->CondForOper[i]->GetCopy( (lvCBaseScript**)(&_pCondForOper) );
		if (_pCondForOper!=NULL) CondForOper.Add(_pCondForOper);
		_pCondForOper=NULL;
	};

	// Stop Condition
	lvCCondition* _pStopCondition = NULL;
	if (pEdge->StopCondition.Get()!=NULL){
		pEdge->StopCondition.Get()->GetCopy(&_pStopCondition);
	};
	if (_pStopCondition!=NULL) StopCondition.Set(_pStopCondition);
	_pStopCondition=NULL;

	id=pEdge->id;
};

void	lvCEdge::vINIT (char* CLName/* =NULL */,DWORD ID/* =0 */,char* FLName/* =NULL */){
	ClassName	= ( (CLName==NULL) ? ("lvCEdge") : (CLName) );    
	InfID		= ( (ID==0) ? (_lvCEdge_) : (ID) );   
	FileNameXML	= ( (FLName==NULL) ? ("lvCEdge.xml") : (FLName) ); 

	NAME		= "NoName";
	DESCR		= "NoDescription";

	begID		= 0;
	endID		= 0;
	procTime	= 0;
	startTime	= 0;
	id			= 0;
};

void	lvCEdge::vCLEAN(){
	begID		= 0;
	endID		= 0;
	procTime	= 0;
	startTime	= 0;
	id			= 0;
};

void	lvCEdge::vDeleteDynamicData(){
	SelectCondition.Clear();
	CondForOper.Clear();
};

void	lvCEdge::vSetID(DWORD _id){
	id = _id;
};

void	lvCEdge::vSetBegEndNodeID(int _beg,int _end){
	if (_beg!=0xFFFFFFFF) {
		vSetBeg(_beg);
	};
	if (_end!=0xFFFFFFFF) {	
		vSetEnd(_end);
	};
};

void	lvCEdge::vSetBegEndNodePT(lvCNode* _beg,lvCNode* _end){
	if (_beg!=NULL && _end!=NULL) {
		begID = _beg->id;
		endID = _end->id;
  	};	
};

void	lvCEdge::vSetBeg(int _x,int _y){
	lvCNode* pND = NodesMap()->vGetNode(_x,_y);
	(pND!=NULL) ? (begID=pND->id) : (begID=0);
};

void	lvCEdge::vSetEnd(int _x,int _y){
	lvCNode* pND = NodesMap()->vGetNode(_x,_y);
	(pND!=NULL) ? (endID=pND->id) : (endID=0);
};

void	lvCEdge::vSetBeg(int _ndID){
	lvCNode* pND = NodesMap()->vGetNode(_ndID);
	(pND!=NULL) ? (begID=pND->id) : (begID=0);
};

void	lvCEdge::vSetEnd(int _ndID){
	lvCNode* pND = NodesMap()->vGetNode(_ndID);
	(pND!=NULL) ? (endID=pND->id) : (endID=0);
};

void	lvCEdge::vSetProcTime(int _pT){
	procTime = _pT;
};

void	lvCEdge::vSetStartTime(int _sT){
	startTime = _sT;
};

void	lvCEdge::vSetMODE(DWORD _mode){
	
};

void	lvCEdge::vSetEdgeID(int _beg,int _end,int _pT,int _sT,DWORD _mode,DWORD _id/* =0 */,char* name/* =NULL */,char* descr/* =NULL */){
	vSetBegEndNodeID(_beg,_end);
	vSetProcTime(_pT);
	vSetStartTime(_sT);
	vSetMODE(_mode);
	vSetID(_id);
};

void	lvCEdge::vSetEdgePT(lvCNode* _beg,lvCNode* _end,int _pT,int _sT,DWORD _mode,DWORD _id/* =0 */,char* name/* =NULL */,char* descr/* =NULL */){
	vSetBegEndNodePT(_beg,_end);
	vSetProcTime(_pT);
	vSetStartTime(_sT);
	vSetMODE(_mode);
	vSetID(_id);
};

bool	lvCEdge::vCheckEdge(){
	return	(NodesMap()->vGetNode(begID)!=NULL)&&(NodesMap()->vGetNode(endID)!=NULL);
};

void	lvCEdge::vDrawDirection(int _x,int _y,int& maxLen,DWORD& pos,bool setLen){
	char color[56];
	sprintf(color,"%s%x%s","{C 0x",DriveMode()->EdgeNameColor,"}");
	const	int		dT	  = 4;
	const	DWORD	MAX_STR	 = 100;
	char	NDtoND[MAX_STR];
	sprintf(NDtoND,"%s%d%s%d",color,begID," -> ",endID);
	extern	int GetRLen(char* s,RLCFont* font);
	if (maxLen<GetRLen(NDtoND,pEDGE_FONT))	maxLen = GetRLen(NDtoND,pEDGE_FONT);
	if (setLen)	return;
	extern	void ShowStringEx(int x, int y, LPCSTR lps, lpRLCFont lpf);
	ShowStringEx(_x+dT,_y+dT+pos*10,NDtoND,pEDGE_FONT); pos++;
};

void	lvCEdge::vDrawProcTime(int _x,int _y,int& maxLen,DWORD& pos,bool setLen){
	char color[56];
	sprintf(color,"%s%x%s","{C 0x",DriveMode()->EdgeNameColor,"}");
	const	int		dT	  = 4;
	const	DWORD	MAX_STR	 = 100;
	char	ProcTm[MAX_STR];
	sprintf(ProcTm,"%s%s%d",color,"Proc ",procTime);
	extern	int GetRLen(char* s,RLCFont* font);
	if (maxLen<GetRLen(ProcTm,pEDGE_FONT))	maxLen = GetRLen(ProcTm,pEDGE_FONT);
	if (setLen)	return;
	extern	void ShowStringEx(int x, int y, LPCSTR lps, lpRLCFont lpf);
	ShowStringEx(_x+dT,_y+dT+pos*10,ProcTm,pEDGE_FONT); pos++;
};

void	lvCEdge::vDrawStartTime(int _x,int _y,int& maxLen,DWORD& pos,bool setLen){
	char color[56];
	sprintf(color,"%s%x%s","{C 0x",DriveMode()->EdgeNameColor,"}");
	const	int		dT	  = 4;
	const	DWORD	MAX_STR	 = 100;
	char	StarTm[MAX_STR];
	sprintf(StarTm,"%s%s%d",color,"Start ",startTime);
	extern	int GetRLen(char* s,RLCFont* font);
	if (maxLen<GetRLen(StarTm,pEDGE_FONT))	maxLen = GetRLen(StarTm,pEDGE_FONT);
	if (setLen)	return;
	extern	void ShowStringEx(int x, int y, LPCSTR lps, lpRLCFont lpf);
	ShowStringEx(_x+dT,_y+dT+pos*10,StarTm,pEDGE_FONT); pos++;
};

void	lvCEdge::vDarwMode(int _x,int _y,int& maxLen,DWORD& pos,bool setLen){
	char color[56];
	sprintf(color,"%s%x%s","{C 0x",DriveMode()->EdgeNameColor,"}");
	const	int		dT	  = 4;
	const	DWORD	MAX_STR	 = 100;
	char	ModeSt[MAX_STR];
	sprintf(ModeSt,"%s%s",color,"ATTACK");
	extern	int GetRLen(char* s,RLCFont* font);
	if (maxLen<GetRLen(ModeSt,pEDGE_FONT))	maxLen = GetRLen(ModeSt,pEDGE_FONT);
	if (setLen)	return;
	extern	void ShowStringEx(int x, int y, LPCSTR lps, lpRLCFont lpf);
	ShowStringEx(_x+dT,_y+dT+pos*10,ModeSt,pEDGE_FONT); pos++;
};

void	lvCEdge::vDarwData(int _x, int _y,float _cosF,float _sinF,DWORD mode/* =1 */){
	DWORD	color = DriveMode()->EdgeLineColor;
	const	int		dT	  = 4;

	int		MAX_STRING	= 0;
	int		curMaxLen	= 0;

	DWORD pos = 0;
	float	x = 0.f;
	float	y = 0.f;
	if (mode&_DIRECTION_)	{ MAX_STRING++; vDrawDirection((int)x,(int)y,curMaxLen,pos,true);	};
	if (mode&_PROC_TIME_)	{ MAX_STRING++; vDrawProcTime ((int)x,(int)y,curMaxLen,pos,true);	};
	if (mode&_START_TIME_)	{ MAX_STRING++; vDrawStartTime((int)x,(int)y,curMaxLen,pos,true);	};
	if (mode&_MODE_)		{ MAX_STRING++; vDarwMode	  ((int)x,(int)y,curMaxLen,pos,true);	};

	float	DELTA = 0;
	if (mode==1) {
		DELTA = 20.f;
	}else{
		DELTA = max(15.f + (float)curMaxLen/2.f + (float)dT, 10.f*(float)MAX_STRING+2.f*dT-2.f) + 5.f;
	}
	
	x = (float)_x + _sinF*DELTA;
	y = (float)_y - _cosF*2.f*DELTA;

	float	drX = (float)(curMaxLen/2+dT+1);
	float	drY = (float)(5*MAX_STRING+dT-1);

	float		rZ = (float)GetHeight((int)x,(int)y);
	Vector3D	p1(x-drX,y-drY*2.f,rZ);
	Vector3D	p2(x+drX,y-drY*2.f,rZ);
	Vector3D	p3(x+drX,y+drY*2.f,rZ);
	Vector3D	p4(x-drX,y+drY*2.f,rZ);

	if (mode&_RECT_) {
		GPS.DrawLine(p1,p2,color);
		GPS.DrawLine(p2,p3,color);
		GPS.DrawLine(p3,p4,color);
		GPS.DrawLine(p4,p1,color);
	};

	if (mode==1) {
		return;
	};
	
	Vector4D	p(p1.x,p1.y/*-260.f*/,p1.z,1);
	WorldToScreenSpace(p);
	
	pos = 0;
	if (mode&_DIRECTION_)	{ vDrawDirection(p.x,p.y,curMaxLen,pos,false); }
	if (mode&_PROC_TIME_)	{ vDrawProcTime	(p.x,p.y,curMaxLen,pos,false); }
	if (mode&_START_TIME_)	{ vDrawStartTime(p.x,p.y,curMaxLen,pos,false); }
	if (mode&_MODE_)		{ vDarwMode		(p.x,p.y,curMaxLen,pos,false); }
};

void	lvCEdge::vDraw(DWORD mode/* =1 */,DWORD _id/* =0xFFffFFff */){
	int DTX = 100;
	int DTY = 150;
	if (DriveMode()->NodeStyle==1) {
		DTX = 15;
		DTY = 25;
	};

	if (NodesMap()->vGetNode(begID)==NULL || NodesMap()->vGetNode(endID)==NULL) {
		lvCNode* pbegND = NodesMap()->vGetNode(begID);
		lvCNode* pendND = NodesMap()->vGetNode(endID);
		if (pbegND!=NULL) {
			lvDrawArrow(pbegND->x,pbegND->y,LastMx,LastMy,DTX,DTY,0xFFff0000);
			return;
		};
		if (pendND!=NULL) {
			lvDrawArrow(LastMx,LastMy,pendND->x,pendND->y,DTX,DTY,0xFFff0000);
			return;
		};
		return;
	}

	if (_id!=0xFFffFFff&&id!=_id) {
		return;
	};

	DWORD color = DriveMode()->EdgeLineColor;
	if (selected&&DriveMode()->OBJECT==7) color = 0xFFff0000+rand()%256;
	
	lvDrawArrow(NodesMap()->vGetNode(begID),NodesMap()->vGetNode(endID),DTX,DTY,color);

	
	if (Shifter!=5)	return;
	
	lvCNode*	pND0 = NodesMap()->vGetNode(begID);
	lvCNode*	pND1 = NodesMap()->vGetNode(endID);

	if (pND0==NULL || pND1==NULL) return;
		

	float	x1 = pND0->x;
	float	y1 = pND0->y;
	float	x2 = pND1->x;
	float	y2 = pND1->y;

	float	mDX = (float)DTX;
	float	mDY = (float)DTY;

	int _x0 = (int)(( x1 + x2 ) / 2.f);
	int _y0 = (int)(( y1 + y2 ) / 2.f);

	float	DDD = (float)sqrt(mDX*mDX+mDY*mDY)*0.8f;
	float	SSS = (float)sqrt((x1-x2)*(x1-x2)+(y1-y2)*(y1-y2));

	if (-2.f*DDD-70.f<=SSS&&SSS<=2.f*DDD+70.f)	return;

	float	cosF = (x2-x1)/SSS;
	float	sinF = (y2-y1)/SSS;

	vDarwData(_x0,_y0,cosF,sinF,mode);
};

float	lvCEdge::vGetEdgeDist(int _x,int _y){
	lvCNode* pbegND = NodesMap()->vGetNode(begID);
	lvCNode* pendND = NodesMap()->vGetNode(endID);
	
	float x0 = (float)(pbegND->x);
	float y0 = (float)(pbegND->y);
	float x1 = (float)(pendND->x);
	float y1 = (float)(pendND->y);

	float D		= sqrtf((x1-x0)*(x1-x0)+(y1-y0)*(y1-y0));
	float xn	= (x1-x0)/D;
	float yn	= (y1-y0)/D;
	float x		= (float)_x-x0;
	float y		= (float)_y-y0;
	float PR	= x*xn+y*yn;

	if (PR<0) {	return sqrtf(x*x+y*y); };
	if (PR>D) {	return sqrtf(((float)_x-x1)*((float)_x-x1)*((float)_y-y1)*((float)_y-y1)); };
	
	PR = x*yn-y*xn;
	if (PR<0) {	return -PR; };
	return PR;
};

float	lvCEdge::vGetEdgeDistDirected(int _x,int _y){
	lvCNode* pbegND = NodesMap()->vGetNode(begID);
	lvCNode* pendND = NodesMap()->vGetNode(endID);

	float x0 = (float)(pbegND->x);
	float y0 = (float)(pbegND->y);
	float x1 = (float)(pendND->x);
	float y1 = (float)(pendND->y);

	float D		= sqrtf((x1-x0)*(x1-x0)+(y1-y0)*(y1-y0));
	float xn	= (x1-x0)/D;
	float yn	= (y1-y0)/D;
	float x		= (float)_x-x0;
	float y		= (float)_y-y0;
	float PR	= x*xn+y*yn;

	float dir = x*yn-y*xn;

	if (PR<0) {	
		if (dir>=0)		return sqrtf(x*x+y*y); 
		else			return -sqrtf(x*x+y*y); 
	};
	if (PR>D) {	
		if (dir>=0)		return sqrtf(((float)_x-x1)*((float)_x-x1)*((float)_y-y1)*((float)_y-y1)); 
		else			return -sqrtf(((float)_x-x1)*((float)_x-x1)*((float)_y-y1)*((float)_y-y1)); 
	};

	PR = x*yn-y*xn;
	return PR;
};

void	lvCEdge::GetCopy(lvCStorage** pCopy){
	*pCopy = dynamic_cast<lvCStorage*>(new lvCEdge(this));
};

void	lvCEdge::vSendNodesXYtoOperations(){
	int x0=0,y0=0,x1=10,y1=10;
	lvCNode* pBegND = NodesMap()->vGetNode(begID);
	lvCNode* pEndND = NodesMap()->vGetNode(endID);
	if (pBegND!=NULL&&pEndND!=NULL) {
		x0 = pBegND->x;	y0 = pBegND->y;
		x1 = pEndND->x;	y1 = pEndND->y;
	};
	int N = CondForOper.GetAmount();
	lvCCondition* pCOND = NULL;
	while (N--) {
		int O = CondForOper[N]->Operation.GetAmount();
		while (O--) {
			CondForOper[N]->Operation[O]->SetEdgeXY(x0,y0,x1,y1);
		};
		int C = CondForOper[N]->Condition.GetAmount();
		while (C--) {
			pCOND = dynamic_cast<lvCCondition*>(CondForOper[N]->Condition[C]);
			if (pCOND!=NULL)	pCOND->SetEdgeXY(x0,y0,x1,y1);
			pCOND = NULL;
		};
	};

	if (SelectCondition.Get()!=NULL) {
		pCOND = dynamic_cast<lvCCondition*>(SelectCondition.Get());
		if (pCOND!=NULL) pCOND->SetEdgeXY(x0,y0,x1,y1);
		pCOND = NULL;
	};

	if (StopCondition.Get()!=NULL) {
		pCOND = dynamic_cast<lvCCondition*>(StopCondition.Get());
		if (pCOND!=NULL)  pCOND->SetEdgeXY(x0,y0,x1,y1);
		pCOND = NULL;
	};
};

void	lvCEdge::vSendSquardID(DWORD sqID){
	int N = CondForOper.GetAmount();
	lvCCondition* pCOND = NULL;
	while (N--) {
		int O = CondForOper[N]->Operation.GetAmount();
		while (O--) {
			CondForOper[N]->Operation[O]->SetSquardID(sqID);
		};
		int C = CondForOper[N]->Condition.GetAmount();
		while (C--) {
			pCOND = dynamic_cast<lvCCondition*>(CondForOper[N]->Condition[C]);
			if (pCOND!=NULL)	pCOND->SetSquardID(sqID);
			pCOND = NULL;
		};
	};

	if (SelectCondition.Get()!=NULL) {
		pCOND = dynamic_cast<lvCCondition*>(SelectCondition.Get());
		if (pCOND!=NULL) pCOND->SetSquardID(sqID);
		pCOND = NULL;
	};

	if (StopCondition.Get()!=NULL) {
		pCOND = dynamic_cast<lvCCondition*>(StopCondition.Get());
		if (pCOND!=NULL)  pCOND->SetSquardID(sqID);
		pCOND = NULL;
	};
};

bool	lvCEdge::Process(int time){
	int N = CondForOper.GetAmount();
	for (int i=0; i<N; i++){
		if (CondForOper[i]->Process(time)!=0) i=N;
	};
	return	1;
};

int		lvCEdge::UpdateEdgeTime(int time, DWORD ND_ID/* =0xFFFFFFFF */){
	if ((ND_ID!=0xFFFFFFFF&&begID==ND_ID) || (ND_ID==0xFFFFFFFF)) {
		startTime = time;
		return startTime+procTime;
	};
	return time;
};

int		lvCEdge::Coplite(){
	int comp = 1;
	int CFO = CondForOper.GetAmount();
	while (comp==1&&CFO--) {
		for (int o=0; (comp==1)&&(o<CondForOper[CFO]->Operation.GetAmount()); o++){
			if (CondForOper[CFO]->Operation[o]->Complite()==0)	comp=0;
		};
	};
	return comp;
};

void	lvCEdge::RestartEdge(){
	int CON = CondForOper.GetAmount();
	while (CON--) {
		CondForOper[CON]->Restart();
	};
};
// lvCSquardShema //////////////////////////////////////////////////////////
lvCSquardShema::lvCSquardShema(lvCSquardShema* pSquardShema) : lvCStorage(dynamic_cast<lvCStorage*>(pSquardShema)) {
	if (pSquardShema==NULL)	return;
	SquardName	= pSquardShema->SquardName.str;
	SquardID	= pSquardShema->SquardID;
	Use_vGRP	= pSquardShema->Use_vGRP;
	lvCEdge* pEdge = NULL;
	for (int i=0; i<pSquardShema->SquardEdges.GetAmount(); i++){
		pSquardShema->SquardEdges[i]->GetCopy( (lvCStorage**)(&pEdge) );
		SquardEdges.Add(pEdge);
		pEdge=NULL;
	};
};

void		lvCSquardShema::vINIT (char* CLName/* =NULL */, DWORD ID/* =0 */, char* FLName/* =NULL */){
	ClassName	= ( (CLName==NULL) ? ("lvCSquardShema") : (CLName) );    
	InfID		= ( (ID==0) ? (_lvCSquardShema_) : (ID) );   
	FileNameXML	= ( (FLName==NULL) ? ("lvCSquardShema.xml") : (FLName) ); 

	NAME		= "NoName";
	DESCR		= "NoDescription";

	SquardName	= "NoName";
	SquardID	= 0xFFFF;
};

void		lvCSquardShema::vCLEAN(){
	SquardName.Clear();
};

void		lvCSquardShema::vDeleteDynamicData(){
	SquardEdges.Clear();
};

lvCNode*	lvCSquardShema::vAddNode(int x,int y,char* name/* =NULL */,char* descr/* =NULL */){
    return	NodesMap()->vAddNode(x,y,name,descr);
};

bool		lvCSquardShema::vDelNodeID(DWORD _id){
	bool	delComlite = false;
	while (!delComlite) {
		int N = SquardEdges.GetAmount();
		delComlite = true;
		while (N--) {
			if (SquardEdges[N]->begID==_id || SquardEdges[N]->endID==_id) {
				SquardEdges.Del(N,1);
				N=0;
				delComlite=false;
			};
		};
	};
	return	true;
};

bool		lvCSquardShema::vDelNodePT(lvCNode* pND){
	if (pND!=NULL){
		vDelNodeID(pND->id);
	};
	return	true;
};

bool		lvCSquardShema::vDelNode(int _x,int _y,DWORD sqID/* =0xFFFFFFFF */){
	lvCNode* pND = vGetNode(_x,_y);
	return	vDelNodePT(pND);
};

lvCNode*	lvCSquardShema::vGetNode(DWORD _id){
	return	NodesMap()->vGetNode(_id);
};

lvCNode*	lvCSquardShema::vGetNode(int _x,int _y,DWORD sqID/* =0xFFFFFFFF */){
	const	float MIN_DIST	= 300.f;

	lvCNode* pRET = NULL;
	int N = SquardEdges.GetAmount();
	
	if (N>0) {
		pRET			= vGetNode(SquardEdges[0]->begID);
		if (pRET!=NULL) {
			float minDist	= pRET->vGetNodeDist(_x,_y);
			float dist		= 0.f;
			lvCNode*		pBegND = NULL;
			lvCNode*		pEndND = NULL;
			while (N--) {
				pBegND = vGetNode(SquardEdges[N]->begID);
				pEndND = vGetNode(SquardEdges[N]->endID);

				if (pBegND!=NULL){
					dist = pBegND->vGetNodeDist(_x,_y);
					if (dist<minDist) {
						minDist=dist;
						pRET=pBegND;
					};
				};

				if (pEndND!=NULL) {
					dist = pEndND->vGetNodeDist(_x,_y);
					if (dist<minDist) {
						minDist=dist;
						pRET=pEndND;
					};
				};
			};
			if (minDist>MIN_DIST) {
				pRET = NULL;
			};
		};		
	};
	return	pRET;
};

DWORD		lvCSquardShema::vGetFreeEdgeID(){
	int N = SquardEdges.GetAmount();
	DWORD	newID = 0;
	while (N--) {
		if (SquardEdges[N]->id>newID)	newID = SquardEdges[N]->id;
	};
	newID++;
	return newID;
};

bool		lvCSquardShema::vAddEdgeID(int _beg,int _end,int _pT,int _sT,DWORD _mode,char* name/* =NULL */,char* descr/* =NULL */){
	int N = SquardEdges.GetAmount();
	DWORD	newID = vGetFreeEdgeID();
	lvCEdge* pEG = new lvCEdge();
	pEG->vINIT();
	pEG->vSetEdgeID(_beg,_end,_pT,_sT,_mode,newID);

	if (name!=NULL)		pEG->vSetObjectName(name);
	if (descr!=NULL)	pEG->vSetDescription(descr);
	SquardEdges.Add(pEG);
	pEG=NULL;

	return	true;
};

bool		lvCSquardShema::vAddEdgePT(lvCNode* _beg,lvCNode* _end,int _pT,int _sT,DWORD _mode,char* name/* =NULL */,char* descr/* =NULL */){
	int N = SquardEdges.GetAmount();
	DWORD	newID = vGetFreeEdgeID();
	lvCEdge* pEG = new lvCEdge();
	pEG->vINIT();
	pEG->vSetEdgePT(_beg,_end,_pT,_sT,_mode,newID);

	if (name!=NULL)		pEG->vSetObjectName(name);
	if (descr!=NULL)	pEG->vSetDescription(descr);
	SquardEdges.Add(pEG);
	pEG=NULL;

	return	true;
};

bool		lvCSquardShema::vAddEdge(lvCEdge* pED){
	if (pED!=NULL) {
		pED->id = vGetFreeEdgeID();
		SquardEdges.Add(pED);
		pED=NULL;
		return	true;
	};
	return	false;
};

bool		lvCSquardShema::vDelEdgeID(DWORD _id){
	int N = SquardEdges.GetAmount();
	while (N--) {
		if (SquardEdges[N]->id==_id) {
			SquardEdges.Del(N,1);
			N=0;
			return true;
		};
	};
	return	false;
};

bool		lvCSquardShema::vDelEdgePT(lvCEdge* pED){
	int N = SquardEdges.GetAmount();
	while (N--) {
		if (SquardEdges[N]==pED) {
			SquardEdges.Del(N,1);
			N=0;
			return true;
		};
	};
	return	false;
};

bool		lvCSquardShema::vDelEdge(int _x,int _y){
	lvCEdge* pED = vGetEdge(_x,_y);
	if (pED!=NULL) {
		vDelEdgePT(pED);
		return true;
	};
	return	false;
};

void		lvCSquardShema::vDraw(DWORD mode/* =1 */,DWORD _id/* =0xFFffFFff */){
	int N = SquardEdges.GetAmount();
	while (N--) {
		SquardEdges[N]->vDraw(mode);
	};
};

void		lvCSquardShema::vDrawMyNodes(DWORD mode/* =1 */){
	int N = SquardEdges.GetAmount();
	while (N--) {
		NodesMap()->vDraw(mode,SquardEdges[N]->begID);
		NodesMap()->vDraw(mode,SquardEdges[N]->endID);
	};
};

lvCEdge*	lvCSquardShema::vGetEdgeID(DWORD _id){
	lvCEdge* pRET = NULL;
	int N = SquardEdges.GetAmount();
	while (N--) {
		if (SquardEdges[N]->id==_id) {
			pRET = SquardEdges[N];
		};
	};
	return	pRET;
};

int			lvCSquardShema::vGetEdgePT(lvCEdge* pED){
	int RET = 0xFFFF;
	int N = SquardEdges.GetAmount();
	while (N--) {
		if (SquardEdges[N]==pED) {
			RET = N;
		};
	};
	return	RET;
};

lvCEdge*	lvCSquardShema::vGetEdge(int _x,int _y){
	const	float MIN_DIST	= 100.f;

	lvCEdge* pRET = NULL;
	int N = SquardEdges.GetAmount();
	if (N>0) {
		pRET			= SquardEdges[0];
		float	minDist = SquardEdges[0]->vGetEdgeDist(_x,_y);;
		float	dist	= 0.f;
		while (N--) {
			dist = SquardEdges[N]->vGetEdgeDist(_x,_y);
			if (dist<minDist) {
				minDist=dist;
				pRET = SquardEdges[N];
			};
		};
		if (minDist>MIN_DIST) {
			pRET=NULL;
		};
	};

	if (pRET!=NULL){	// Проверим нет ли обратного Edge
		int N = SquardEdges.GetAmount();
		lvCEdge* pINVERS = NULL;
		while (pINVERS==NULL&&N--) {
			if (SquardEdges[N]->begID==pRET->endID&&SquardEdges[N]->endID==pRET->begID) {
				pINVERS = SquardEdges[N];
			};
		};
		if (pINVERS!=NULL) {
			if (pRET->vGetEdgeDistDirected(_x,_y)<0) {
				pRET = pINVERS;
			};
		};
	};	

	return	pRET;
};

void		lvCSquardShema::vUpdateEdges(){
	bool	updateComplite = false;
	while (!updateComplite) {
		updateComplite = true;
		int N = SquardEdges.GetAmount();
		while (N--) {
			if (SquardEdges[N]->vCheckEdge() == false) {
				vDelEdgePT(SquardEdges[N]);
				N = 0;
				updateComplite = false;
			};
		};
	};
};

void		lvCSquardShema::vRestartEdges(){
	int EN = SquardEdges.GetAmount();
	while (EN--) {
		SquardEdges[EN]->RestartEdge();
	};
};
DWORD		lvCSquardShema::GetEdgeEndedIn(DWORD end){
	DWORD	retED_ID = 0xFFFFFFFF;
	int N = SquardEdges.GetAmount();
	while (retED_ID==0xFFFFFFFF&&N--) {
		if (SquardEdges[N]->endID==end) {
			retED_ID = N;
		};
	};
	return	retED_ID;
};

int			lvCSquardShema::GetEdgeBeginIn(DWORD beg){
	int M = SquardEdges.GetAmount();
	int N = 0;
	while (M--) {
		if (SquardEdges[M]->begID==beg){
			N++;
		};
	};
	return	N;
};

void		lvCSquardShema::GetEdgeBeginIn(lvCEdge** pEdges,int N,DWORD beg){
	int M = SquardEdges.GetAmount();
	int i = 0;
	while ((M--)&&(i<N)) {
		if (SquardEdges[M]->begID==beg){
			pEdges[i++]=SquardEdges[M];
		};
	};
};

DWORD		lvCSquardShema::vGetProbablyFirstNodeID(){
	// Поиск ребра в начале которого не заканчиваются другие.
	DWORD   retNodeID = -1;
	DWORD	retID = 0xFFffFFff;
	int N = SquardEdges.GetAmount();
	DWORD	end = 0xFFffFFff;
	DWORD	curED = 0xFFffFFff;
    if (N>0) {
		end = SquardEdges[0]->begID;
		retID = curED = 0;
	}else{
		return	0xFFffFFff;
	};
	while (curED!=0xFFffFFff&&N--) {
		curED = GetEdgeEndedIn(end);
		if (curED!=0xFFffFFff) {
			end = SquardEdges[curED]->begID;
			retID = curED;
		};
	};
	retNodeID = SquardEdges[retID]->begID;
	
	if (GetEdgeEndedIn(SquardEdges[retID]->begID)!=0xFFffFFff){
		// Finde nearest node.
		retNodeID = vGetNearestNode();	
	};

	return	retNodeID;
};

DWORD		lvCSquardShema::vGetNearestNode(){
	DWORD nearestNodeID = 0xFFFF;
	int x=0,y=0;
	if (Use_vGRP) {	// For vGroups
		lvCGroup* pvGRP = GroupsMap()->GetGroupID(SquardID);
		if (pvGRP!=NULL) {
			pvGRP->GetGroupCenter(x,y);
		};
	}else{			// For AGroups
		if (0<=SquardID&&SquardID<=AGroups.GetAmount()-1) {
			int N = AGroups[SquardID]->Units.GetAmount();
			int NUM = 0;
            OneObject* pOB = NULL;
			while (N--) {
				if (AGroups[SquardID]->Units[N].ID<0xFFFE){
					pOB = Group[AGroups[SquardID]->Units[N].ID];
					if (pOB&&(!pOB->Sdoxlo||pOB->Hidden)&&pOB->Serial==AGroups[SquardID]->Units[N].SN) {
						x += pOB->RealX;
						y += pOB->RealY;
						NUM++;
					};
				};
				pOB = NULL;
			};
			if (NUM!=0) {
				x = (x/NUM)>>4;
				y = (y/NUM)>>4;
			}else{
				x=y=0;
			}
		};
	};

	if (x!=0||y!=0) {
		int N = SquardEdges.GetAmount();
		if (N!=0) {
			int minDist = -1;
			int begID=0,endID=0;
			lvCNode *pbegND=NULL,*pendND=NULL;
			while (N--) {
				begID = SquardEdges[N]->begID;
				endID = SquardEdges[N]->endID;
				pbegND = NodesMap()->vGetNode(begID);
				pendND = NodesMap()->vGetNode(endID);
				if ( pbegND!=NULL && ( pbegND->vGetNodeDist(x,y)<minDist || minDist==-1 ) ){
					minDist = pbegND->vGetNodeDist(x,y);
					nearestNodeID = begID;
				};
				if ( pendND!=NULL && ( pendND->vGetNodeDist(x,y)<minDist || minDist==-1 ) ){
					minDist = pendND->vGetNodeDist(x,y);
					nearestNodeID = endID;
				};
			};
		};
	};

	return	nearestNodeID;
};

void		lvCSquardShema::GetCopy(lvCStorage** pCopy){
	*pCopy = dynamic_cast<lvCStorage*>(new lvCSquardShema(this));
};

void		lvCSquardShema::Select(bool _select){
	for (int i=0; i<SquardEdges.GetAmount(); i++){
		SquardEdges[i]->selected=_select;
		(NodesMap()->vGetNode(SquardEdges[i]->begID))->selected=_select;
		(NodesMap()->vGetNode(SquardEdges[i]->endID))->selected=_select;
	};
};

bool		lvCSquardShema::Process(int time){
	return true;
};

int			lvCSquardShema::UpdateEdgeTime(int time, DWORD ND_ID/* =0xFFFFFFFF */){
	if (ND_ID!=0xFFFFFFFF) {
		int N = SquardEdges.GetAmount();
		while (N--) {
			SquardEdges[N]->UpdateEdgeTime(time,ND_ID);
		};
	};
	return	0;
};

// lvCSquardsOnMap ///////////////////////////////////////////////////////
extern	ClassArray<ActiveGroup> AGroups;
DWORD lvCSquardsOnMap::GetValue(const char* ID){
	int N=AGroups.GetAmount();
	while (N--) {
		if (strcmp(ID,AGroups[N]->Name.str)==0) {
			return	N;
		};
	};
	return	0xFFFFFFFF;
};

char* lvCSquardsOnMap::GetValue(DWORD ID){
	if (0<=ID&&ID<AGroups.GetAmount()) {
		return	AGroups[ID]->Name.str;
	};
	return	NULL;
};

int   lvCSquardsOnMap::GetAmount(){
	return	AGroups.GetAmount();
};

char* lvCSquardsOnMap::GetIndexedString(int idx){
	return	GetValue(idx);
};

DWORD lvCSquardsOnMap::GetIndexedValue (int idx){
	if (0<=idx&&idx<AGroups.GetAmount()) return idx;
	return	0xFFFFFFFF;
};


// lvCSquardsOnMap ///////////////////////////////////////////////////////
DWORD lvCZonesOnMap::GetValue(const char* ID){
	int N=AZones.GetAmount();
	while (N--) {
		if (strcmp(ID,AZones[N]->Name.str)==0) {
			return	N;
		};
	};
	return	0xFFFFFFFF;
};

char* lvCZonesOnMap::GetValue(DWORD ID){
	if (0<=ID&&ID<AZones.GetAmount()) {
		return	AZones[ID]->Name.str;
	};
	return	NULL;
};

int   lvCZonesOnMap::GetAmount(){
	return	AZones.GetAmount();
};

char* lvCZonesOnMap::GetIndexedString(int idx){
	return	GetValue(idx);
};

DWORD lvCZonesOnMap::GetIndexedValue (int idx){
	if (0<=idx&&idx<AZones.GetAmount()) return idx;
	return	0xFFFFFFFF;
};


// lvCBattleShema_ST /////////////////////////////////////////////////////
void			lvCBattleShema_ST::vINIT (char* CLName/* =NULL */, DWORD ID/* =0 */, char* FLName/* =NULL */){
	ClassName	= ( (CLName==NULL) ? ("lvCBattleShema_ST") : (CLName) );    
	InfID		= ( (ID==0) ? (_lvCBattleShema_ST_) : (ID) );   
	FileNameXML	= ( (FLName==NULL) ? ("lvCBattleShema_ST.xml") : (FLName) ); 

	NAME		= "NoName";
	DESCR		= "NoDescription";
};

void			lvCBattleShema_ST::vCLEAN(){

};

void			lvCBattleShema_ST::vDeleteDynamicData(){
	BattlePlan.Clear();
	vGRP_Plan.Clear();
};

ClassArray<lvCSquardShema>*	lvCBattleShema_ST::GetPlan(){
	if (DriveMode()->USE_vGRP) {
		return	&vGRP_Plan;
	};
	return &BattlePlan;
};

void			lvCBattleShema_ST::AddPlan_vGRP(lvCGroup* pGRP){
	int N=vGRP_Plan.GetAmount();
	bool newSS = true;
	while (N--) {
		if (vGRP_Plan[N]->SquardID==pGRP->id) {
			newSS = false;
		};
	};
	if (newSS==false)	return;
	lvCSquardShema* pNewShema = new lvCSquardShema;
	pNewShema->vINIT();
	pNewShema->SquardName = pGRP->GetGroupName();
	pNewShema->SquardID = pGRP->id;
	pNewShema->Use_vGRP = true;
	vGRP_Plan.Add(pNewShema);
	pNewShema = NULL;	
};

void			lvCBattleShema_ST::DelPlan_vGRP(lvCGroup* pGRP){
	int N=vGRP_Plan.GetAmount();
	int SS_ID = 0xFFFF;
	while (N--) {
		if (vGRP_Plan[N]->SquardID==pGRP->id) {
			SS_ID = N;
		};
	};
	if (SS_ID!=0xFFFF) {
		vGRP_Plan.Del(SS_ID,1);
	};
};

void			lvCBattleShema_ST::vDraw(DWORD mode/* =1 */,DWORD _id/* =0xFFffFFff */){
	ClassArray<lvCSquardShema>* pPLAN = GetPlan();
	if (pPLAN==NULL)	return;

	int N = pPLAN->GetAmount();
	if (_id==0xFFffFFff) {
		while (N--) {
			(*pPLAN)[N]->vDraw(mode);
		};
	}else{
		while (N--) {
			if ((*pPLAN)[N]->SquardID==_id) {
				(*pPLAN)[N]->vDraw(mode);
				N=0;
			};
		};
	};
};

void			lvCBattleShema_ST::vDrawMyNodes(DWORD _id,DWORD mode/* =1 */){
	lvCSquardShema* pSS = vGetSqShemaID(_id);
	if (pSS!=NULL) {
		pSS->vDrawMyNodes(mode);
	};
};

bool			lvCBattleShema_ST::vDelEdge(int _GrpID,int _x,int _y){
	lvCSquardShema* pSS = vGetSqShemaID(_GrpID);
	if (pSS!=NULL) {
		return	pSS->vDelEdge(_x,_y);
	};
	return false;
};

bool			lvCBattleShema_ST::vAddEdge(int _GrpID,int _begND_ID,int _endND_ID){
	if (_begND_ID==_endND_ID) {
		return false;
	};
	vUpdateGroups();
	lvCSquardShema* pSS = vGetSqShemaID(_GrpID);
	if (pSS!=NULL) {
		pSS->vAddEdgeID(_begND_ID,_endND_ID,0,0,0);
		return	true;
	};
	return false;
};

bool			lvCBattleShema_ST::vAddEdge(int _GrpID,lvCEdge* pED){
	if (pED->begID==pED->endID&&pED!=NULL) {
		delete	pED;
		pED = NULL;
	};
	vUpdateGroups();
	lvCSquardShema* pSS = vGetSqShemaID(_GrpID);
	if (pSS!=NULL) {
		pSS->vAddEdge(pED);
		return	true;
	};
	return false;
};

lvCEdge*		lvCBattleShema_ST::vGetEdge(int _GrpID,int _x,int _y){
	lvCEdge* pED = NULL;
	lvCSquardShema* pSS = vGetSqShemaID(_GrpID);
	if (pSS!=NULL) {
		pED = pSS->vGetEdge(_x,_y);
	};
	return	pED;
};

void			lvCBattleShema_ST::SelectSquardShema(int _GrpID,bool _select){
   lvCSquardShema* pSS = vGetSqShemaID(_GrpID);
   if (pSS!=NULL) {
	   pSS->Select(_select);
   };
};


lvCNode*		lvCBattleShema_ST::vAddNode(int x,int y,char* name/* =NULL */,char* descr/* =NULL */){
	return	NodesMap()->vAddNode(x,y,name,descr);
};

bool			lvCBattleShema_ST::vDelNodeID(DWORD _id){
	ClassArray<lvCSquardShema>* pPLAN = GetPlan();
	if (pPLAN==NULL)	return false;	

	int N = pPLAN->GetAmount();
	while (N--) {
		(*pPLAN)[N]->vDelNodeID(_id);
	};
	return	true;
};

bool			lvCBattleShema_ST::vDelNodePT(lvCNode* pND){
	ClassArray<lvCSquardShema>* pPLAN = GetPlan();
	if (pPLAN==NULL)	return false;	

	int N = pPLAN->GetAmount();
	while (N--) {
		(*pPLAN)[N]->vDelNodePT(pND);
	};
	return	true;
};

bool			lvCBattleShema_ST::vDelNode(int _x,int _y,DWORD sqID/* =0xFFFFFFFF */){
    if (sqID==0xFFFFFFFF || (sqID!=0xFFFFFFFF && vGetNode(_x,_y,sqID)!=NULL)){
		ClassArray<lvCSquardShema>* pPLAN = GetPlan();
		if (pPLAN==NULL)	return false;

		int N = pPLAN->GetAmount();
		while (N--) {
			(*pPLAN)[N]->vDelNode(_x,_y);
		};
		return	true;
	};
	return false;
};

lvCNode*		lvCBattleShema_ST::vGetNode(DWORD _id){
	return	NodesMap()->vGetNode(_id);
};

lvCNode*		lvCBattleShema_ST::vGetNode(int _x,int _y,DWORD sqID/* =0xFFFFFFFF */){
	if (sqID!=0xFFFFFFFF) {
		lvCSquardShema* pSS = vGetSqShemaID(sqID);
		if (pSS) {
			return	pSS->vGetNode(_x,_y);
		}
		return	NULL;
	}
	return	NodesMap()->vGetNode(_x,_y);
};

lvCSquardShema*	lvCBattleShema_ST::vGetSqShemaID(int _ID){
	ClassArray<lvCSquardShema>* pPLAN = GetPlan();
	if (pPLAN==NULL)	return NULL;

	int N = pPLAN->GetAmount();
	while (N--) {
		if ((*pPLAN)[N]->SquardID==_ID) {
			return	(*pPLAN)[N];
		};
	};
	return	NULL;
};

lvCSquardShema*	lvCBattleShema_ST::vGetSqShemaCH(char* _nameID){
	ClassArray<lvCSquardShema>* pPLAN = GetPlan();
	if (pPLAN==NULL)	return NULL;

	int N = pPLAN->GetAmount();
	while (N--) {
		if (strcmp((*pPLAN)[N]->SquardName.str,_nameID)==0) {
			return	(*pPLAN)[N];
		};
	};
	return	NULL;
};

void			lvCBattleShema_ST::vUpdateGroups(){
	bool oldUSE = DriveMode()->USE_vGRP;
	DriveMode()->USE_vGRP = false;

	int newN = AGroups.GetAmount();
	int oldN = BattlePlan.GetAmount();

	lvCSquardShema* pBS = NULL;
	
	for (int i=0; i<newN; i++){
		pBS = vGetSqShemaCH(AGroups[i]->Name.str);
		if (pBS!=NULL) {
			pBS->SquardID = i;
		};
		pBS = NULL;
	};
	
	for (i=oldN-1;i>=0;i--){
		pBS = BattlePlan[i];
		if (pBS->SquardID<newN) {
			if (strcmp(AGroups[pBS->SquardID]->Name.str,pBS->SquardName.str)!=0){
				BattlePlan.Del(i,1);
			};
		}else{
			BattlePlan.Del(i,1);
		};
	};

	for (i=0;i<newN;i++){
		int newID = 0xFFFF;
		// Fine new group
		if (vGetSqShemaCH(AGroups[i]->Name.str)==NULL) {
			newID = i;
		};
		if (newID!=0xFFFF) {
			lvCSquardShema* pNewShema = new lvCSquardShema;
			pNewShema->vINIT();
			pNewShema->SquardName = AGroups[newID]->Name.str;
			pNewShema->SquardID = newID;
			BattlePlan.Add(pNewShema);
			pNewShema = NULL;
		};
	};

	DriveMode()->USE_vGRP = oldUSE;
};

// lvCMainScript /////////////////////////////////////////////////////////
void	lvCMainScript::vINIT (char* CLName/*=NULL*/, DWORD ID/*=0*/, char* FLName/*=NULL*/){
	ClassName	= ( (CLName==NULL) ? ("lvCMainScript") : (CLName) );    
	InfID		= ( (ID==0) ? (_lvCMainScript_) : (ID) );   
	FileNameXML	= ( (FLName==NULL) ? ("lvCMainScript.xml") : (FLName) ); 

	NAME		= "NoName";
	DESCR		= "NoDescription";
};

void	lvCMainScript::vCLEAN(){
	mastInit = true;
};

void	lvCMainScript::vDeleteDynamicData(){
	MAIN_INIT.Clear();
	MAIN_SCRIPTS.Clear();
};

bool	lvCMainScript::Process(int time){
	int N = MAIN_SCRIPTS.GetAmount();
	for (int i=0; i<N; i++){
		MAIN_SCRIPTS[i]->Process(time);
	};

	return	1;
};

bool	lvCMainScript::ProcessInit(int time){
	if (mastInit) {
		mastInit = false;
		int N = MAIN_INIT.GetAmount();
		for (int i=0; i<N; i++){
			MAIN_INIT[i]->Process(time);
		};
		return true;
	};
	return false;
};

void	lvCMainScript::Draw(){
	int N = MAIN_INIT.GetAmount();
	while (N--) {
		MAIN_INIT[N]->Draw();
	};
	N = MAIN_SCRIPTS.GetAmount();
	while (N--) {
		MAIN_SCRIPTS[N]->Draw();
	};
};
void	lvCMainScript::Prepare_lua(){
	int N=MAIN_INIT.GetAmount();
	while (N--) {
		MAIN_INIT[N]->Prepare_lua();
	};
	N=MAIN_SCRIPTS.GetAmount();
	while (N--) {
		MAIN_SCRIPTS[N]->Prepare_lua();
	};
};
// lvCFilm ///////////////////////////////////////////////////////////////
void	lvCFilm::vINIT (char* CLName/*=NULL*/, DWORD ID/*=0*/, char* FLName/*=NULL*/){
	Name	= "";    
	InfID	= ( (ID==0) ? (_lvCFilm_) : (ID) );   
		
	DESCR	= "NoDescription";
};

void	lvCFilm::vCLEAN(){
	ScriptInProcess=false;
	ScriptComplit=false;
};

void	lvCFilm::vDeleteDynamicData(){
	StartCondition.Clear();
	PrepareOperation.Clear();
	STEP_CON_OPR.Clear();
	StopCondition.Clear();
	DestroyOperation.Clear();
};

bool	lvCFilm::IsInProcess(){
	return ScriptInProcess;
};

void SetCDVolumeEx(int Vol);
CIMPORT int GetCDVolume();
bool	lvCFilm::Process(int time){
	static int	MusicVolume=GetCDVolume();
	static int	SoundVolume=GSets.SVOpt.SoundVolume*EngSettings.vInterf.VolumeSoundEvents/100;
	static bool	lastSilence=GSets.CGame.SilenceMessageEvents;

	if (ScriptComplit) return 0;
	if (ScriptInProcess==false) {
		int SCN = StartCondition.GetAmount();
		int	a_start = 1;
		while (a_start==1&&SCN--) {
			if (StartCondition[SCN]->GetValue(time)==0) a_start=0;
		};

		if (a_start){
			// Remove LMode
			void ReverseLMode();
			if (LMode==true)	ReverseLMode();

			// Set dialog volume in films
			SoundVolume=GSets.SVOpt.SoundVolume;
			GSets.SVOpt.SoundVolume=SoundVolume*EngSettings.RelativDialogSoundVolume;
			ov_SetVolume(GSets.SVOpt.SoundVolume,0);
			// Save CD MUSIC volume and set new music volume
			MusicVolume=GetCDVolume();
			SetCDVolumeEx(GSets.SVOpt.SoundVolume*EngSettings.RelativDialogMusicVolume);
			
			// Clear selection from all units
			for (int i=0; i<7; i++)	ClearSelection(i);
			ClearBMASK();
			BuildMode=false;

			// Set silence in film as default
			lastSilence=GSets.CGame.SilenceMessageEvents;
			GSets.CGame.SilenceMessageEvents=true;
			/////////////////////////////////

			void RunGameTime(bool State);
			RunGameTime(false);
			ScriptInProcess=true;
			int PON = PrepareOperation.GetAmount();
			while (PON--) {
				PrepareOperation[PON]->Process(time);
			};
			return 1;
		};
	}else{
		int N=STEP_CON_OPR.GetAmount();
		while (N--) {
			STEP_CON_OPR[N]->Process(time);
		};	

		int SCN = StopCondition.GetAmount();
		int	a_stop = 1;
		while (a_stop==1&&SCN--) {
			if (StopCondition[SCN]->GetValue(time)==0) a_stop=0;
		};

		if (a_stop){
			// Restore silence in film as default
			GSets.CGame.SilenceMessageEvents=lastSilence;
			/////////////////////////////////////

			ScriptInProcess=false;
			ScriptComplit=true;
			int DON = DestroyOperation.GetAmount();
			while (DON--) {
				DestroyOperation[DON]->Process(time);
			};
			CameraDriver()->Stop();
			void RunGameTime(bool State);
			RunGameTime(true);

			// Set dialog volume in films
			GSets.SVOpt.SoundVolume=SoundVolume;
			ov_SetVolume(100,0);
			// Save CD MUSIC volume and set new music volume
			SetCDVolumeEx(MusicVolume);

			return 0;
		};
		return 1;
	};
	return 0;
};

// lvCScriptHandler_ST ////////////////////////////////////////////////////
lvCScriptHandler_ST::~lvCScriptHandler_ST(){ 
	for (int i=0; i<DS.DSS.GetAmount(); i++) DS.DSS[i]=NULL;

	SQUARDS_SCRIPTS.Clear(); 
	vGROUPS_SCRIPTS.Clear(); 
	SCRIPT_FILMS.Clear(); 
	SCRIPT_GRAPH.Clear();

	LUA_Close();
};
bool			lvCScriptHandler_ST::PROCESS_MAIN(bool inProc/*=false*/){
	static bool change=false;
	
	GroupsMap()->ReSize();

	if (inProc==false)	return	0;
	MAIN_SCRIPT.ProcessInit(Time);

	int sfN = SCRIPT_FILMS.GetAmount();
	bool	mainProc = true;
	while (mainProc&&sfN--) {
		SCRIPT_FILMS[sfN]->Process(Time);
		if (SCRIPT_FILMS[sfN]->IsInProcess()==true){
			mainProc=false;
			change=true;
			GSets.SVOpt.RequiredMsPerFrame=EngSettings.DefaultGameSpeedForCampaign;
		};
	};
	if (mainProc==true){
		if (vHeroButtons.GetVisible()==false&&DriveMode()->PROCESS==true) vHeroButtons.SetVisible( true );
		vvMESSGES* pMess = dynamic_cast<vvMESSGES*>(vValuesMap()->GetVValueTypeID(_vvMESSGES_));
		if (pMess!=NULL){
			if (pMess->GetMessDS_Visbility()==false&&change&&DriveMode()->PROCESS==true){
				pMess->SetMessDS_Visbility(true);
				change=false;
			};
		};
		MAIN_SCRIPT.Process(Time);
	}else if (mainProc==false&&vHeroButtons.GetVisible()==true){
		if (DriveMode()->PROCESS==true) vHeroButtons.SetVisible( false );
		vvMESSGES* pMess = dynamic_cast<vvMESSGES*>(vValuesMap()->GetVValueTypeID(_vvMESSGES_));
		if (pMess!=NULL&&DriveMode()->PROCESS==true){
			if (pMess->GetMessDS_Visbility()==true){
				pMess->SetMessDS_Visbility(false);
			};
		};
	};
	return	1;	
};

void			lvCScriptHandler_ST::DRAW(){
    int N = SCRIPT_GRAPH.GetAmount();
	if (DriveMode()->PROCESS){
		while(N--){
			GPS.FlushBatches();
			SCRIPT_GRAPH[N]->DRAW();
		};
	};
	DS.ProcessDialogs();
    MAIN_SCRIPT.Draw();
};

bool			lvCScriptHandler_ST::PROCESS(bool onlySwitched/* = false */){
	GroupsMap()->ReSize();

	bool	oldUSE = DriveMode()->USE_vGRP;

	DriveMode()->USE_vGRP = false;
	int sqN = SQUARDS_SCRIPTS.GetAmount();
	bool	state;
	while (sqN--) {
		if (onlySwitched) {
			state = SQUARDS_SCRIPTS[sqN]->Active;
			SQUARDS_SCRIPTS[sqN]->Active = true;
		};
		PROCESS(SQUARDS_SCRIPTS[sqN]->SQUARD);
		if (onlySwitched) {
			SQUARDS_SCRIPTS[sqN]->Active = state;
		};
	};

	DriveMode()->USE_vGRP = true;
	sqN = vGROUPS_SCRIPTS.GetAmount();
	while (sqN--) {
		if (onlySwitched) {
			state = vGROUPS_SCRIPTS[sqN]->Active;
			vGROUPS_SCRIPTS[sqN]->Active = true;
		};
		PROCESS(vGROUPS_SCRIPTS[sqN]->SQUARD);
		if (onlySwitched) {
			vGROUPS_SCRIPTS[sqN]->Active = state;
		};
	};

	DriveMode()->USE_vGRP = oldUSE;
	return true;	
};

bool			lvCScriptHandler_ST::PROCESS(int squadID){

	lvCSquardShema* pSS = BattleShema()->vGetSqShemaID(squadID);
	lvCProcSquad*	pPS = vGetSquadScriptID(squadID);
	
	if (pSS==NULL) return true;

	lvCGroup* pGRP = GroupsMap()->GetGroupID(pSS->SquardID);
	if (pSS!=NULL&&pPS!=NULL&&pGRP!=NULL&&pGRP->GetTotalAmount()>0) {
		if (pPS->Active==true) {
			if (pPS->NodeID==0xFFFF&&pPS->EdgeID==0xFFFF){
				pPS->NodeID		= pSS->vGetProbablyFirstNodeID();
				pPS->EdgeID		= 0xFFFF;
				pPS->TimeInProc	= 0;
			};
			if (pPS->NodeID!=0xFFFF) {
				// Select curent handler edge.
				int N = pSS->GetEdgeBeginIn(pPS->NodeID);
				if (N==0)	return false;
                lvCEdge** ppEdges = new lvCEdge*[N];
				pSS->GetEdgeBeginIn(ppEdges,N,pPS->NodeID);
				lvCEdge* pSelEdge = NULL;
                for (int i=0; i<N; i++){
					if (pSelEdge==NULL && ppEdges[i]->SelectCondition.Get()!=NULL) {
						if ((ppEdges[i]->SelectCondition.Get())->GetValue(pPS->TimeInProc)==true) {
							pSelEdge = ppEdges[i];
						};
					}else if (pSelEdge==NULL && ppEdges[i]->SelectCondition.Get()==NULL){
						pSelEdge = ppEdges[i];
					};
					ppEdges[i]=NULL;
				};
				delete[]ppEdges;
				if (pSelEdge!=NULL) {
					pPS->NodeID=0xFFFF;
					pPS->EdgeID=pSelEdge->id;
					pPS->TimeInProc	= 0;
					pSelEdge->startTime = Time;
					pSelEdge->vSendNodesXYtoOperations();
					pSelEdge->vSendSquardID(squadID);
				};
			};
			if (pPS->EdgeID!=0xFFFF) {
				lvCEdge* pEDGE = pSS->vGetEdgeID(pPS->EdgeID);
				if (pEDGE==NULL) return false;
				// Process curent edge, and if action complit set 0xFFFFFFFF and new node
				pEDGE->Process(pPS->TimeInProc);
				pPS->TimeInProc = Time - pEDGE->startTime;

				lvCCondition* pEndCond = pEDGE->StopCondition.Get();
				if ( ( pEndCond!=NULL && pEndCond->GetValue(pPS->TimeInProc)==true )
					 ||
					 ( pEndCond==NULL &&  pEDGE->procTime!=0 && pEDGE->procTime < pPS->TimeInProc  ) ) 
				{
					if (pEDGE->Coplite()||(pEndCond!=NULL&&pEndCond->Power()==1)){
						pPS->EdgeID=0xFFFF;
						pPS->NodeID=pEDGE->endID;
					};
				};
			};
		};
	};
	return true;
};

void			lvCScriptHandler_ST::TIMER(bool	inProc){
    if (inProc==true) {
		if (LastTime!=0) {
			Time += GetTickCount()-LastTime;
		};
		LastTime = GetTickCount();
		return;
    };
	if (inProc==false) {
		LastTime = 0;
		return;
	}
};

bool			lvCScriptHandler_ST::Create(){
	Time = 0;
	LastTime = 0;

	SQUARDS_SCRIPTS.Clear();
	int N = BattleShema()->BattlePlan.GetAmount();
	lvCSquardShema* pSS = NULL;
	lvCProcSquad* pPS = NULL;
	while (N--) {
		pSS = BattleShema()->BattlePlan[N];

		pPS = new lvCProcSquad;
        pPS->SQ_NAME	= pSS->SquardName.str;
		pPS->SQUARD		= pSS->SquardID;
		pPS->Active		= true;
		pPS->NodeID		= pSS->vGetProbablyFirstNodeID();
		pPS->EdgeID		= 0xFFFF;
		pPS->TimeInProc	= 0;

		SQUARDS_SCRIPTS.Add(pPS);
		pPS = NULL;
	};

	vGROUPS_SCRIPTS.Clear();
	N = BattleShema()->vGRP_Plan.GetAmount();
	pSS = NULL;
	pPS = NULL;
	while (N--) {
		pSS = BattleShema()->vGRP_Plan[N];

		pPS = new lvCProcSquad;
		pPS->SQ_NAME	= pSS->SquardName.str;
		pPS->SQUARD		= pSS->SquardID;
		pPS->Active		= true;
		pPS->NodeID		= pSS->vGetProbablyFirstNodeID();
		pPS->EdgeID		= 0xFFFF;
		pPS->TimeInProc	= 0;

		vGROUPS_SCRIPTS.Add(pPS);
		pPS = NULL;
	};

	// add graph to DialogsSystem
	lvCDialogBased* pDB = NULL;
	int NG = SCRIPT_GRAPH.GetAmount();
    for (int i=0; i<NG; i++){
		if (SCRIPT_GRAPH[i]->InfID==_lvCDialogBased_){
			pDB = dynamic_cast<lvCDialogBased*>( SCRIPT_GRAPH[i] );
			if (pDB!=NULL) 	pDB->AddToDS(&DS);
			pDB=NULL;
		};
	};

	return true;
};

lvCProcSquad*	lvCScriptHandler_ST::vGetSquadScriptID(int squadID){
	lvCProcSquad* pPS = NULL;

	if (DriveMode()->USE_vGRP) {
		int N = vGROUPS_SCRIPTS.GetAmount();
		while (pPS==NULL&&N--) {
			if (vGROUPS_SCRIPTS[N]->SQUARD==squadID) {
				pPS = vGROUPS_SCRIPTS[N];
			};
		};
	}else{
		int N = SQUARDS_SCRIPTS.GetAmount();
		while (pPS==NULL&&N--) {
			if (SQUARDS_SCRIPTS[N]->SQUARD==squadID) {
				pPS = SQUARDS_SCRIPTS[N];
			};
		};
	};
	return pPS;
};

void			lvCScriptHandler_ST::AddNewFilm(char* scrName,char* scrDescr){
	if (scrName==NULL||scrDescr==NULL)	return;
	if (strcmp(scrName,"New Film")!=0&&strcmp(scrDescr,"New Film Descr")!=0){
		int N = SCRIPT_FILMS.GetAmount();
		bool	AlreadyPresent = false;
		while (!AlreadyPresent&&N--) {
			if (strcmp(SCRIPT_FILMS[N]->Name.str,scrName)==0){
				AlreadyPresent = true;
			};
		};
		if (!AlreadyPresent){
			lvCFilm* pNewFilm = new lvCFilm();
			if (pNewFilm!=NULL) {
				pNewFilm->Name = scrName;
				pNewFilm->DESCR	= scrDescr;
				SCRIPT_FILMS.Add(pNewFilm);
				pNewFilm = NULL;
			};
		};
	};
};

void			lvCScriptHandler_ST::DeleteFilm(char* scrName){
	if (scrName!=NULL){
		int N = SCRIPT_FILMS.GetAmount();
		while (N--) {
			if (strcmp(SCRIPT_FILMS[N]->Name.str,scrName)==0){
				SCRIPT_FILMS.DelElement(N);
				N = 0;
			};
		};
	};
};
lvCGraphObject*	lvCScriptHandler_ST::getGraphByName(const char* val){
	lvCGraphObject* ret=NULL;
	if (val!=NULL){
		int N=SCRIPT_GRAPH.GetAmount();
		while (ret==NULL&&N--) {
			if ( strcmp(val,SCRIPT_GRAPH[N]->Name.str)==0 ) ret=SCRIPT_GRAPH[N];
		};
	};
	return ret;
};
lvCGraphObject*	__getGraphByName(const char* val){
	return	BattleHandler()->getGraphByName(val);
};
bool			lvCScriptHandler_ST::LUA_Create(){
	bool state=true;
#ifdef  __LUA__
	if (LUAC.useLua){
        state = state && LUA_MISS.OPEN();
		// class
		state = state && LUA_MISS.BIND(bind_lvCNode		);
		state = state && LUA_MISS.BIND(bind_lvCGroup	);
		state = state && LUA_MISS.BIND(bind_valuesMAP	);
		state = state && LUA_MISS.BIND(bind_GraphObjMAP	);
		// function
		state = state && LUA_MISS.BIND(lua_base			);
		state = state && LUA_MISS.BIND(bind_ActiveScenary);
		state = state && LUA_MISS.BIND(bind_Condition	);
		state = state && LUA_MISS.BIND(bind_Operation	);
		if (LUAC.useLuaDEBUGER) state = state && LUA_MISS.DEBUG_PREP();
	};
#endif//__LUA__
	return state;
};
bool			lvCScriptHandler_ST::LUA_Close(){
	bool state=true;
#ifdef  __LUA__
	state = state && LUA_MISS.CLOSE();
#endif//__LUA__
	return state;
};
bool			lvCScriptHandler_ST::LUA_LoadFile(const char* FName){
#ifdef  __LUA__
	return LUA_MISS.LOAD_LUA_FILE(FName);
#endif//__LUA__
	return false;
};
void			lvCScriptHandler_ST::LUA_CallFList(){
#ifdef  __LUA__
	if ( LUA_MISS.LUA_STATE==NULL )	return;
	int N=LUAC.LUA_CALL_LIST.GetAmount();
	for ( int i=0; i<N; i++ ){
		luabind::call_function<void>( LUA_MISS.LUA_STATE, LUAC.LUA_CALL_LIST[i]->str );
	};
#endif//__LUA__
};
bool			lvCScriptHandler_ST::LUA_SAFE_OPEN_MISS(){
	if (LUA_MISS.LUA_STATE==NULL){
		if ( LUA_Create() ){
			int N=LUAC.LUA_FileList.GetAmount();
			for (int i=0; i<N; i++){
				LUA_LoadFile(LUAC.LUA_FileList[i]->FileName.str);
			};
		};
	};
	return LUA_MISS.LUA_STATE==NULL;
};
void			lvCScriptHandler_ST::LUA_SAFE_CLOSE_MISS(){
	if (LUA_MISS.LUA_STATE!=NULL){
		LUA_Close();
	};
};
void			lvCScriptHandler_ST::LUA_CREATE::EvaluateFunction(){
	lvCScriptHandler_ST* pSH = get_parent<lvCScriptHandler_ST>();
	if (pSH!=NULL){
		if (pSH->LUA_Create())	MessageBox(hwnd,"Lua open OK !!!"	,"-= LUA =-",MB_OK);
		else					MessageBox(hwnd,"Lua open ERROR !!!","-= LUA =-",MB_OK);
	};
};
void			lvCScriptHandler_ST::LUA_CLOSE::EvaluateFunction(){
	lvCScriptHandler_ST* pSH = get_parent<lvCScriptHandler_ST>();
	if (pSH!=NULL){
		if (pSH->LUA_Close())	MessageBox(hwnd,"Lua close OK !!!"	 ,"-= LUA =-",MB_OK);
		else					MessageBox(hwnd,"Lua close ERROR !!!","-= LUA =-",MB_OK);
	}
};
void			lvCScriptHandler_ST::LUA_LOAD_FILE::EvaluateFunction(){
	if	( FName.str!=NULL && FName.str[0] ){
		lvCScriptHandler_ST* pSH = get_parent<lvCScriptHandler_ST>();
		if (pSH!=NULL){
			_str mess; 
			mess= "Lua load file << ";
			mess+=FName.str;
			if ( pSH->LUA_LoadFile(FName.str) )	mess+=" >> OK !!!";
			else								mess+=" >> ERROR !!!";
			MessageBox(hwnd,mess.str,"-= LUA =-",MB_OK);
		};
	}else{
		MessageBox(hwnd,"Bad file name *.lua","-= LUA =-",MB_OK);
	};
};
// vvMissionLOG //////////////////////////////////////////////////////////
vvMissionLOG::vvMissionLOG(){ 
	InfID=_vvMissionLOG_;
	Clear();
};
vvMissionLOG::~vvMissionLOG(){
	// ...
};
char*	vvMissionLOG::GetName(){
	g_vvElementView = "";
	g_vvElementView += "(QuestLOG)";
	g_vvElementView += Name.str;
	return	g_vvElementView.str;
};
const	char*	vvMissionLOG::GetThisElementView(const char* LocalName){
	GetName();
	g_vvElementView += "QN[";
	g_vvElementView += QuestN;
	g_vvElementView += "],QTE[";
	g_vvElementView += QuestTotalExperience;
	g_vvElementView += "],KTE[";
	g_vvElementView += KillsTotalExperience;
	g_vvElementView += "],TE[";
	g_vvElementView += TimeEverage;
	g_vvElementView += "],TEE[";
	g_vvElementView += TimeEverageExperience;
	g_vvElementView += "]";
	return g_vvElementView.str;
};
void	vvMissionLOG::Clear(){
	QuestN					= 0;
	QuestTotalExperience	= 0;
	KillsTotalExperience	= 0;
	TimeEverage				= 0;
	TimeEverageExperience	= 0;

	QuestComleteN			= 0;
	QuestComleteExperience	= 0;
	KillsTotalPseudoExperience	= 0;
	KillsPlayerPseudoExperience	= 0;
	TimePlayer					= 0;
};
void	vvMissionLOG::SetQuestData(int _QuestN, int _QuestTotalExperience){
	QuestN					= _QuestN;
	QuestTotalExperience	= _QuestTotalExperience;
	QuestComleteN			= 0;
	QuestComleteExperience	= 0;
};
void	vvMissionLOG::SetKilsData(int _KillsTotalExperience){
	KillsTotalExperience = _KillsTotalExperience;
};
void	vvMissionLOG::SetTimeData(int _TimeEverage, int _TimeEverageExperience){
	TimeEverage = _TimeEverage;
	TimeEverageExperience = _TimeEverageExperience;
};
void	vvMissionLOG::AddCopmleteQuest(int Experience){
	QuestComleteN++;
	QuestComleteExperience += Experience;
};
void	vvMissionLOG::AddKillsCopmlete(){
	KillsTotalPseudoExperience=0;
	for (int i=0; i<8; i++){
		//KillsTotalPseudoExperience += CITY[i].Account/1000;
		KillsTotalPseudoExperience += NATIONS[i].GetNationlKillingExpirience();
	};
	//KillsPlayerPseudoExperience = CITY[MyNation].Account/1000;
	KillsPlayerPseudoExperience = NATIONS[MyNation].GetNationlKillingExpirience();
	if (KillsTotalPseudoExperience==0)	return;
	float korector = 1000.f/(float)KillsTotalPseudoExperience;
	KillsTotalPseudoExperience=1000;
	KillsPlayerPseudoExperience*=korector;
};
void	vvMissionLOG::AddTimeCopmlete(){
//	TimePlayer = AnimTime/(256*25);
	int GetScaledGameTime();
	TimePlayer = GetScaledGameTime()/1000;
	if (TimePlayer<0) TimePlayer=-TimePlayer;
};
void	vvRoundValue(int& val,int key=5){
	if (key<=0)	return;
	val = (val/key)*key;
};
void	vvMissionLOG::WriteToLogClass(){
	AddKillsCopmlete();
	AddTimeCopmlete();
	// CALCULATE //
	// Time
	int Tp=0;		// Player Time
	int Tt=0;		// Everage Time
	int Ptex=0;		// Player Time Exp
	int Et=0;		// Everage Time Exp
	Tp = TimePlayer;
	Tt = TimeEverage;
	Et = TimeEverageExperience;
	Ptex = GetTimeExp(Tp,Tt,Et);
	// Kils
	int Kp=0;		// Player Kills
	int Kt=0;		// Total Kills
	int Pkex=0;		// Player Kills exp
	int Ek=0;		// Total Kills exp
	Kp = KillsPlayerPseudoExperience;
	Kt = KillsTotalPseudoExperience;
	Ek = KillsTotalExperience;
	if (Kt!=0)	Pkex = (int)( (float)Ek*( (float)Kp/(float)Kt ) );
	// Quest
	int Qp=0;		// Player Quest complete
	int Qt=0;		// Total Quest 
	int Pqex=0;		// Player Quest Exp
	int Eq=0;		// Total Quest Exp
	Qp = QuestComleteN;
	Qt = QuestN;
	Eq = QuestTotalExperience;
	Pqex = ( (QuestComleteExperience<Eq) ? (QuestComleteExperience) : (Eq) );
	// Kalibrate by coof
	float GENK = gMISS_SET.GENK;
	float TK = gMISS_SET.TK*GENK;
	float KK = gMISS_SET.KK*GENK;
	float QK = gMISS_SET.QK*GENK;
	Ptex = (int)( (float)Ptex*TK );		Et = (int)( (float)Et*TK );
	Pkex = (int)( (float)Pkex*TK );		Ek = (int)( (float)Ek*TK );
	Pqex = (int)( (float)Pqex*TK );		Eq = (int)( (float)Eq*TK );
	vvRoundValue(Ptex);		vvRoundValue(Et);
	vvRoundValue(Pkex);		vvRoundValue(Ek);
	vvRoundValue(Pqex);		vvRoundValue(Eq);
    // FORMAT //
	_str*	pTimeInfo	= new _str;
	GetTimeForamt(Tp,*pTimeInfo);	*pTimeInfo += "/";	GetTimeForamt(Tt,*pTimeInfo);
	_str*	pTimeExp	= new _str;	
	*pTimeExp  = Ptex;	*pTimeExp  += "/";	*pTimeExp  += Et;
	_str*	pKillInfo	= new _str;
	*pKillInfo = Kp;	*pKillInfo += "/";	*pKillInfo += Kt;
	_str*	pKillExp	= new _str;
	*pKillExp  = Pkex;	*pKillExp  += "/";	*pKillExp  += Ek;
	_str*	pQuestInfo	= new _str;
	*pQuestInfo= Qp;	*pQuestInfo+= "/";	*pQuestInfo+= Qt;
	_str*	pQuestExp	= new _str;
	*pQuestExp = Pqex;	*pQuestExp += "/";	*pQuestExp += Eq;
	ClassArray<_str>*	pTimeCTG  = new ClassArray<_str>;
    pTimeCTG->Add(pTimeInfo);	pTimeCTG->Add(pTimeExp);
	pTimeInfo=NULL;				pTimeExp=NULL;
	ClassArray<_str>*	pKillCTG  = new ClassArray<_str>;
	pKillCTG->Add(pKillInfo);	pKillCTG->Add(pKillExp);
	pKillInfo=NULL;				pKillExp=NULL;
	ClassArray<_str>*	pQuestCTG = new ClassArray<_str>;	
	pQuestCTG->Add(pQuestInfo);	pQuestCTG->Add(pQuestExp);
	pQuestInfo=NULL;			pQuestExp=NULL;
	// SAVE //
	gExpMan()->RemoveInfo();
	gExpMan()->EXPA = Ptex+Pkex+Pqex;
	gExpMan()->DATA.Add(pTimeCTG);
	gExpMan()->DATA.Add(pKillCTG);
	gExpMan()->DATA.Add(pQuestCTG);
	pTimeCTG=NULL;
	pKillCTG=NULL;
	pQuestCTG=NULL;
};
char*	vvMissionLOG::GetTimeForamt(int msec,_str& Time){
	int L=msec;
	int D=L/(1*60*60*24);
	L-=D*(1*60*60*24);
	int H=L/(1*60*60);
	L-=H*(1*60*60);
	int M=L/(1*60);
	L-=M*(1*60);
	int S=L/(1);
	
//	_str Time;
	if (D!=0) {
		Time += D;
		Time += ":";
	};
	
	if (0<=H&&H<=9)	Time += "0";
	if (H==0)		Time += "0";
	else			Time += H;
	Time += ":";

	if (0<=M&&M<=9)	Time += "0";
	if (M==0)		Time += "0";
	else			Time += M;
	Time += ":";

	if (0<=S&&S<=9)	Time += "0";
	if (S==0)		Time += "0";
	else			Time += S;
		
	return Time.str;
};
int		vvMissionLOG::GetTimeExp(int Tp,int Tt,int Et){
	if (Tt==0)	return 0;
	// e=2.71828
	// ln(2)=0.693147
	float C = 0.693147f/Tt; 
	return (int)(expf(-C*Tp)*(float)(Et*2));
};
vvMissionLOG	vvMissionLOG::operator=(vvMissionLOG& MLOG){
	QuestN					= MLOG.QuestN;
	QuestTotalExperience	= MLOG.QuestTotalExperience;
	KillsTotalExperience	= MLOG.KillsTotalExperience;
	TimeEverage				= MLOG.TimeEverage;
	TimeEverageExperience	= MLOG.TimeEverageExperience;

	QuestComleteN			= MLOG.QuestComleteN;
	QuestComleteExperience	= MLOG.QuestComleteExperience;
	KillsTotalPseudoExperience	= MLOG.KillsTotalPseudoExperience;
	KillsPlayerPseudoExperience	= MLOG.KillsPlayerPseudoExperience;
	TimePlayer					= MLOG.TimePlayer;
	return *this;
};
// experience manager ////////////////////////////////////////////////////
int		ExperienceManager::GetExp(){
	return EXPA;
}// Total Experience
int		ExperienceManager::GetNCtg(){
	return DATA.GetAmount();
}// Categiris Nunber
int		ExperienceManager::GetNCol(int ctg){
	int ret=-1;
	if (0<=ctg&&ctg<GetNCtg()){
		ret=DATA[ctg]->GetAmount();
	};
	return ret;
}// Column Number 
bool	ExperienceManager::GetExpInfo(_str& Info,int ctg,int col){
	if (0<=ctg&&ctg<GetNCtg()&&0<=col&&col<GetNCol(ctg)){
		Info = ((*(DATA[ctg]))[col])->str;
		return true;
	};
	return false;
}// Category/Column Info
void	ExperienceManager::RemoveInfo(){
	EXPA=0;
	DATA.Clear();
};// Remove all information
// lvCScriptMapSaver /////////////////////////////////////////////////////
extern	BaseMesMgrST		gMessagesMap;

bool lvCBattleMapSaver::SaveToXML(xmlQuote& xml){
	GroupsMap()->BeforeSave();
	vValuesMap()->DeleteAllSeparators();

	xmlQuote*	pxmlValueMap	= new xmlQuote( vValuesMap()->ClassName.str );
	xmlQuote*	pxmlNodeMap		= new xmlQuote( NodesMap()->ClassName.str	);	
	xmlQuote*	pxmlGroupsMap	= new xmlQuote( GroupsMap()->ClassName.str );
	xmlQuote*	pxmlBattleShema	= new xmlQuote( BattleShema()->ClassName.str );
	xmlQuote*	pxmlMainScript	= new xmlQuote( BattleHandler()->MAIN_SCRIPT.ClassName.str );
	xmlQuote*	pxmlScriptFilms	= new xmlQuote( "MissionScriptFilms" );
	xmlQuote*	pxmlScriptGraph	= new xmlQuote( "MissionScriptGraph" );
	xmlQuote*	pxmlCameraPoss  = new xmlQuote( "CameraPossitions"   );
	xmlQuote*	pxml_gMessagesMap = new xmlQuote( "NewMessagesMap" );
	xmlQuote*	pxml_MISS_SET	= new xmlQuote( "MissionSettings" );
	xmlQuote*	pxml_LUA		= new xmlQuote( "LUA" );

	vValuesMap()->Save(*pxmlValueMap,vValuesMap());
	NodesMap()->Save(*pxmlNodeMap,NodesMap());
	GroupsMap()->Save(*pxmlGroupsMap,GroupsMap());
	BattleShema()->Save(*pxmlBattleShema,BattleShema());
	BattleHandler()->MAIN_SCRIPT.Save(*pxmlMainScript,&(BattleHandler()->MAIN_SCRIPT));
	BattleHandler()->SCRIPT_FILMS.Save(*pxmlScriptFilms,&(BattleHandler()->SCRIPT_FILMS));
	BattleHandler()->SCRIPT_GRAPH.Save(*pxmlScriptGraph,&(BattleHandler()->SCRIPT_GRAPH));
	CameraPositons()->Save(*pxmlCameraPoss,CameraPositons());
	gMessagesMap.Save(*pxml_gMessagesMap,&gMessagesMap);
	gMISS_SET.Save(*pxml_MISS_SET,&gMISS_SET);
	BattleHandler()->Save(*pxml_LUA,&(BattleHandler()->LUAC));


	xml.AddSubQuote(pxmlValueMap);
	xml.AddSubQuote(pxmlNodeMap);
	xml.AddSubQuote(pxmlGroupsMap);
	xml.AddSubQuote(pxmlBattleShema);
	xml.AddSubQuote(pxmlMainScript);
	xml.AddSubQuote(pxmlScriptFilms);
	xml.AddSubQuote(pxmlScriptGraph);
	xml.AddSubQuote(pxmlCameraPoss);
	xml.AddSubQuote(pxml_gMessagesMap);
	xml.AddSubQuote(pxml_MISS_SET);
	xml.AddSubQuote(pxml_LUA);


	GroupsMap()->AfterSave();

	return true;
};
bool lvCBattleMapSaver::LoadFromXML(xmlQuote& xml){
	REG_BE_FUNCTIONS_class();
	REG_BE_CONDITION_class();
	REG_BE_DataStorageXML_class();

	ResetAllClass();

	GroupsMap()->BeforeLoad();

	ErrorPager	Err;
	if (xml.GetSubQuote(0)!=NULL)	vValuesMap()->Load(*(xml.GetSubQuote(0)),vValuesMap(),&Err);
	if (xml.GetSubQuote(1)!=NULL)	NodesMap()->Load(*(xml.GetSubQuote(1)),NodesMap(),&Err);										
	if (xml.GetSubQuote(2)!=NULL)	GroupsMap()->Load(*(xml.GetSubQuote(2)),GroupsMap(),&Err);									
	if (xml.GetSubQuote(3)!=NULL)	BattleShema()->Load(*(xml.GetSubQuote(3)),BattleShema(),&Err);
	if (xml.GetSubQuote(4)!=NULL)	BattleHandler()->MAIN_SCRIPT.Load(*(xml.GetSubQuote(4)),&(BattleHandler()->MAIN_SCRIPT),&Err);
	if (xml.GetSubQuote(5)!=NULL)	BattleHandler()->SCRIPT_FILMS.Load(*(xml.GetSubQuote(5)),&(BattleHandler()->SCRIPT_FILMS),&Err);
	if (xml.GetSubQuote(6)!=NULL)	BattleHandler()->SCRIPT_GRAPH.Load(*(xml.GetSubQuote(6)),&(BattleHandler()->SCRIPT_GRAPH),&Err);
	if (xml.GetSubQuote(7)!=NULL)	CameraPositons()->Load(*(xml.GetSubQuote(7)),CameraPositons(),&Err);
	if (xml.GetSubQuote(8)!=NULL)	gMessagesMap.Load(*(xml.GetSubQuote(8)),&gMessagesMap,&Err);
	if (xml.GetSubQuote(9)!=NULL)	gMISS_SET.Load(*(xml.GetSubQuote(9)),&gMISS_SET,&Err);	
	if (xml.GetSubQuote(10)!=NULL)	BattleHandler()->LUAC.Load(*(xml.GetSubQuote(10)),&(BattleHandler()->LUAC),&Err);

	GroupsMap()->AfterLoad();

	return true;
};
bool lvCBattleMapSaver::ResetAllClass(){
	vValuesMap()->reset_class(vValuesMap());
	NodesMap()->reset_class(NodesMap());
	GroupsMap()->reset_class(GroupsMap());
	BattleShema()->reset_class(BattleShema());
	BattleHandler()->MAIN_SCRIPT.reset_class(&(BattleHandler()->MAIN_SCRIPT));
	BattleHandler()->SCRIPT_FILMS.reset_class(&(BattleHandler()->SCRIPT_FILMS));
	BattleHandler()->SCRIPT_GRAPH.reset_class(&(BattleHandler()->SCRIPT_GRAPH));
	CameraPositons()->reset_class(CameraPositons());
	gMessagesMap.reset_class(&gMessagesMap);
	gMISS_SET.reset_class(&gMISS_SET);
	BattleHandler()->LUAC.reset_class(&(BattleHandler()->LUAC));
	return true;
};
bool lvCBattleMapSaver::GetXmlToSaveInMap(xmlQuote& xml){
	if (DriveMode()->BE_SAVE==false){
		SaveToXML(xml);		
	}else if (DriveMode()->GetSaveSFileName()!=NULL){
		xmlQuote* xmlMSL = new xmlQuote("MISSION_SCRIPT");
		SaveToXML(*xmlMSL);
		xmlMSL->WriteToFile( DriveMode()->GetSaveSFileName() );
	};
	return true;
};

void lvCBattleMapSaver::SetXmlFromMap(xmlQuote& xml){
	ResetAllClass();
	if (DriveMode()->BE_SAVE==false){
		LoadFromXML(xml);	
	}else if (DriveMode()->GetSaveSFileName()!=NULL){
		xmlQuote	xmlMSL( "MISSION_SCRIPT" ); 
		xmlMSL.ReadFromFile( DriveMode()->GetSaveSFileName() );
		LoadFromXML(xmlMSL);		
	};
		
};
void	RegisterBattleEditorSaver(){
	REG_XML_SAVER(lvCBattleMapSaver);
};

#ifdef  __LUA__
// LUA CLASS /////////////////////////////////////////////////////////////
bool	lua_module(const char* fileName);
// 	Function for global operation with lua
LUA_MGR::LUA_MGR(){
	LUA_STATE=NULL;
	useDEBUGER=false;
//	OPEN();
//	LUA_DBG_PREPARE();
//	BIND();
};
LUA_MGR::~LUA_MGR(){
	CLOSE();
};
bool	LUA_MGR::OPEN(){
	LUA_STATE = lua_open();
	if (LUA_STATE==NULL)	return false;
	return luabind_OPEN();
};
bool	LUA_MGR::CLOSE(){
#ifdef	__LUA_DEBUGGER__
	if (useDEBUGER) g_LUA_DBG.UnPrepareLua(LUA_STATE,0);
	useDEBUGER=false;
#endif//__LUA_DEBUGGER__
	if (LUA_STATE!=NULL) lua_close(LUA_STATE);
	LUA_STATE=NULL;
	clearFList();
	return true;
};
bool	LUA_MGR::BIND( void ( *BindFunction )(lua_State*) ){
	if (LUA_STATE==NULL||BindFunction==NULL)	return false;
	BindFunction(LUA_STATE);
	return true;
};
bool	LUA_MGR::DEBUG_PREP(){
	if (LUA_STATE==NULL)	return false;
#ifdef	__LUA_DEBUGGER__
	g_LUA_DBG.PrepareLua(LUA_STATE);
	g_LUA_DBG.PrepareLuaBind();
	useDEBUGER=true;
#endif//__LUA_DEBUGGER__
	return true;
};
bool	LUA_MGR::luabind_OPEN(){
	if (LUA_STATE==NULL)	return false;
	luabind::open(LUA_STATE);
	return true;
};
//  loadin files using module(char* filename);
bool	LUA_MGR::isFNameInList(const char* filename){
	bool	IN_LIST=false;
	if (filename!=NULL&&strlen(filename)>0){
		int N=H_list.GetAmount();
		while (!IN_LIST&&N--) {
			if (strcmp(H_list[N]->str,filename)==0)	IN_LIST=true;
		};
	};
	return IN_LIST;
};
bool	LUA_MGR::addFNameToList(const char* filename){
	if (filename==NULL||strlen(filename)==0)	return false;
	_str* pstr = new _str(); 
	(*pstr) = filename;
	pCUR_LUA_MGR->H_list.Add(pstr);
	return true;
};
void	LUA_MGR::clearFList(){
	H_list.Clear();
};
bool	LUA_MGR::LOAD_LUA_FILE(const char* filename){
	bool state=false;
	if (LUA_STATE==NULL) return state;
	pCUR_LUA_MGR=this;
	state = load_lua_file(filename);
	pCUR_LUA_MGR=NULL;
	return state;
};
bool	LUA_MGR::load_lua_file(const char* filename){
	if (filename!=NULL&&strlen(filename)>0){
		if (pCUR_LUA_MGR->isFNameInList(filename)==false){
			pCUR_LUA_MGR->addFNameToList(filename);
			int stst = lua_dofile(pCUR_LUA_MGR->LUA_STATE,filename);		
			return (stst==0);
		};
	};
	return false;
};
LUA_MGR* pCUR_LUA_MGR;
bool	lua_module(const char* fileName){
	if (pCUR_LUA_MGR==NULL) return false;
	return pCUR_LUA_MGR->load_lua_file(fileName);
};
//////////////////////////////////////////////////////////////////////////
#endif//__LUA__

inline	void	REG_BE_DataStorageXML_class(){
	static mastREG = true;
	if (mastREG||true) {
		mastREG = false;
		
		REG_CLASS(lvCStorage);
        
		REG_CLASS(lvCNode);
		REG_CLASS(lvCNodesMAP_ST);
		
		REG_CLASS(lvCGroup);
		REG_CLASS(lvCGroupSmart);
		REG_CLASS(lvCGroupsMAP_ST);

		REG_CLASS(vvBASE);
		REG_CLASS(vvTRIGER);
		REG_CLASS(vvWORD);
		REG_CLASS(vvINTEGER);
		REG_CLASS(vvTEXT);
		REG_CLASS(vvPICTURE);
		REG_CLASS(POINT2D);
		REG_CLASS(vvPOINT2D);
		REG_CLASS(vvPOINT_SET);
		REG_CLASS(vvVector3D);
		REG_CLASS(vvDIALOG);
		REG_CLASS(CSingleMessage);
		REG_CLASS(CPhraseChunk);
		REG_CLASS(COneMissHint);
		REG_CLASS(vvMESSGES);
		REG_CLASS(vvMAP_ST);
		REG_CLASS(vvMissionLOG);
		REG_CLASS(vvMISSMGR);
		REG_CLASS(vvBrigAI);
        REG_CLASS(vvTASKS_CII);
		REG_CLASS(vvFuzzyRule);

		REG_CLASS(bbTEXT);
		REG_CLASS(vvTEXT_EX);

		REG_CLASS(bbObjInList);

		REG_CLASS(MissZoneBuild);
		REG_CLASS(MissSET);

		REG_CLASS(IIPara);

		REG_CLASS(OperationMesMgr);
		
		REG_CLASS(lvCEdge);
		
		REG_CLASS(lvCSquardShema);
		
		REG_CLASS(lvCMainScript);
		REG_CLASS(lvCFilm);
		REG_CLASS(ClassArray<lvCFilm>);

		REG_CLASS(lvCGraphObject);
		REG_CLASS(lvCDialogBased);
		REG_CLASS(lvCBlackScreen);
		REG_CLASS(lvCMoveGP);
		REG_CLASS(lvCAAppearGP);
		REG_CLASS(lvCAnimateGP);
		REG_CLASS(lvCDeffFilmMenu);
		REG_CLASS(lvCDeffAnimeFilmMenu);
		REG_CLASS(ClassArray<lvCGraphObject>);

		REG_CLASS(lvCTeraforming);
		
		REG_CLASS(lvCBattleShema_ST);

		REG_CLASS(lvCProcSquad);
		REG_CLASS(CFileList);
		REG_CLASS(CLUA_COORDINATOR_FOR_SH);
		REG_CLASS(lvCScriptHandler_ST);

		REG_CLASS(lvCBattleMapSaver);

		REG_CLASS(VectorPara);
		REG_CLASS(CameraPoss);

		REG_CLASS(CSkirmishTASK);

		RegClassBMST();
	};
};



















