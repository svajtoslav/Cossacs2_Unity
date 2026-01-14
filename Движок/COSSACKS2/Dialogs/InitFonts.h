
RLCFont  BigBlackFont;
RLCFont  BigWhiteFont;
RLCFont  BigYellowFont;
RLCFont  BigRedFont;
RLCFont  BlackFont;
RLCFont  YellowFont;
RLCFont  WhiteFont;
RLCFont  RedFont;
RLCFont  OrangeFont;
RLCFont  GrayFont;

RLCFont  SmallBlackFont;
RLCFont  SmallWhiteFont;
RLCFont  SmallYellowFont;
RLCFont  SmallRedFont;
RLCFont  SmallOrangeFont;
RLCFont  SmallGrayFont;

RLCFont  SmallBlackFont1;
RLCFont  SmallWhiteFont1;
RLCFont  SmallYellowFont1;
RLCFont  SmallRedFont1;
RLCFont  SmallGrayFont1;

RLCFont  SpecialWhiteFont;
RLCFont  SpecialYellowFont;
RLCFont  SpecialRedFont;
RLCFont  SpecialBlackFont;
RLCFont  SpecialGrayFont;

RLCFont  fn10;
RLCFont  fn8;

RLCFont  fon30y1;
RLCFont  fon30y3;
RLCFont  fon30y5;
RLCFont  fon40y1;
RLCFont  fon40y5;
RLCFont  fon18w;
RLCFont  fon18y3;
RLCFont  fon18y5;
RLCFont  fon16y1;
RLCFont  fon16y3;
RLCFont  fon16y5;
RLCFont  fon13y2;
RLCFont  fonG60y5;
RLCFont  fonG60w;

RLCFont  fonG12;

// menu
const NColor=7;
RLCFont fonMenuText[NColor];
RLCFont fonMenuTitle[NColor];
RLCFont fonMenuTitle2[NColor];

RLCFont fonMenuGold;
RLCFont fonMenuGoldBlack;
RLCFont fonMenuGoldBlack2;
RLCFont fonMenuGoldBlack3;

RLCFont fonBlow_A;
RLCFont fonBlow_S;
RLCFont fonSmall_01;

#define ADDFONT(f)EF->Add(#f,(DWORD)&f)

//#define ADDFONTX(f)

enum fColor { cBlack,cRed,cYellow,cWhite,cGray,cOrange,cDisable };
const DWORD fonColor[] = 
	{ 0xFF2E2317,0xFF8A1000,0xFFD4C19C,0xFFFFF7EF,0xFF6D6862,0xFF6A3000,0xC0665F57 };

DynArray<RLCFont> vFonts;

inline void InitFonts(){
	// colors
	Enumerator* eCol=ENUM.Get("PreDefColor");	
	eCol->Add("Black",		0xFF2E2317);
	eCol->Add("Red",		0xFF8A1000);
	eCol->Add("Yellow",		0xFFD4C19C);
	eCol->Add("White",		0xFFFFF7EF);
	eCol->Add("Gray",		0xFF6D6862);
	eCol->Add("Orange",		0xFF6A3000);
	eCol->Add("Disable",	0xC0665F57);

	// fonts
	Enumerator* EF=ENUM.Get("FONTS");
	
	char* sFolder="interf3\\Fonts\\new\\";
	RLCFont mF;
	char FN[64];
	#define ADDFONTex(f) sprintf(FN,"%s%s",sFolder,#f); \
		mF.SetGPIndex(GPS.PreLoadGPImage(FN)); \
		EF->Add(#f,(DWORD)(vFonts+vFonts.Add(mF)))	
	
	ADDFONTex(small_01);
	ADDFONTex(small_02);
	ADDFONTex(tm_01_12);
	ADDFONTex(tm_01_14);
	ADDFONTex(lt_01_14);
	ADDFONTex(tr_01_18);
	ADDFONTex(tr_01_32);
	ADDFONTex(gr_01_14);
	ADDFONTex(gr_01_16);
	ADDFONTex(gr_01_18);
	ADDFONTex(gr_01_22a);
	ADDFONTex(gr_01_22s);
	ADDFONTex(gr_01_30);
	
	//	
	int GPID;
	// new
	GPID=GPS.PreLoadGPImage("interf3\\Fonts\\FontC10");
	fonG12.SetGPIndex(GPID);
	//
	GPID=GPS.PreLoadGPImage("interf3\\Fonts\\FontS01");
	fonSmall_01.SetGPIndex(GPID);
	GPID=GPS.PreLoadGPImage("interf3\\Fonts\\FontG2a");
	fonBlow_A.SetGPIndex(GPID);
	GPID=GPS.PreLoadGPImage("interf3\\Fonts\\FontG2s");
	fonBlow_S.SetGPIndex(GPID);
	// small
	GPID=GPS.PreLoadGPImage("interf3\\Fonts\\FontC12");
	SmallWhiteFont.SetGPIndex(GPID);
	//SmallWhiteFont.SetColorTable(1);
	SmallRedFont.SetGPIndex(GPID);
	//SmallRedFont.SetColorTable(2);
	SmallYellowFont.SetGPIndex(GPID);
	//SmallYellowFont.SetColorTable(3);
	SmallBlackFont.SetGPIndex(GPID);
	//SmallBlackFont.SetColorTable(6);
	SmallOrangeFont.SetGPIndex(GPID);
	SmallGrayFont.SetGPIndex(GPID);
	SmallWhiteFont1.SetGPIndex(GPID);
	//SmallWhiteFont1.SetColorTable(1);
	ADDFONT(SmallWhiteFont1);
	SmallRedFont1.SetGPIndex(GPID);
	SmallGrayFont1.SetGPIndex(GPID);
	//SmallRedFont1.SetColorTable(2);
	ADDFONT(SmallRedFont1);
	ADDFONT(SmallGrayFont1);	
	SmallBlackFont1.SetGPIndex(GPID);
	//SmallBlackFont1.SetColorTable(6);
	ADDFONT(SmallBlackFont1);
	SmallYellowFont1.SetGPIndex(GPID);
	//SmallYellowFont1.SetColorTable(3);
	// special
	GPID=GPS.PreLoadGPImage("interf3\\Fonts\\FontG12");
	SpecialWhiteFont.SetGPIndex(GPID);
	//SpecialWhiteFont.SetColorTable(1);
	SpecialYellowFont.SetGPIndex(GPID);
	//SpecialYellowFont.SetColorTable(3);
	SpecialRedFont.SetGPIndex(GPID);
	//SpecialRedFont.SetColorTable(2);
	SpecialBlackFont.SetGPIndex(GPID);
	//SpecialBlackFont.SetColorTable(6);
	SpecialGrayFont.SetGPIndex(GPID);
	// standart
	GPID=GPS.PreLoadGPImage("interf3\\Fonts\\FontC14");
	WhiteFont.SetGPIndex(GPID);	
	YellowFont.SetGPIndex(GPID);
	RedFont.SetGPIndex(GPID);	
	BlackFont.SetGPIndex(GPID);	
	OrangeFont.SetGPIndex(GPID);
	GrayFont.SetGPIndex(GPID);
	//WhiteFont.SetColorTable(1);
	//YellowFont.SetColorTable(3);
	//RedFont.SetColorTable(2);
	//BlackFont.SetColorTable(6);
	//
	//
	// very small
	int s1=GPS.PreLoadGPImage("interf3\\Fonts\\FontC10");
	int s2=GPS.PreLoadGPImage("interf3\\Fonts\\FontC10");
	fn8.SetGPIndex(s1);
	fn8.SetColorTable(1);
	fn10.SetGPIndex(s2);
	fn10.SetColorTable(1);
	// Main menu
	GPID=GPS.PreLoadGPImage("interf3\\Fonts\\FontG18");
	fonG60y5.SetGPIndex(GPID);
	fonG60y5.SetColorTable(6);
	fonG60w.SetGPIndex(GPID);
	fonG60w.SetColorTable(2);
	// Header Big 40
	GPID=GPS.PreLoadGPImage("interf3\\Fonts\\FontG18");
	fon40y1.SetGPIndex(GPID);
	fon40y1.SetColorTable(3);
	fon40y5.SetGPIndex(GPID);
	fon40y5.SetColorTable(6);
	// Header Big 30
	GPID=GPS.PreLoadGPImage("interf3\\Fonts\\FontG18");	
	fon30y1.SetGPIndex(GPID);
	fon30y1.SetColorTable(2);
	fon30y3.SetGPIndex(GPID);
	fon30y3.SetColorTable(3);
	fon30y5.SetGPIndex(GPID);
	fon30y5.SetColorTable(5);
	// Main 18
	GPID=GPS.PreLoadGPImage("interf3\\Fonts\\FontG18");
	fon18w.SetGPIndex(GPID);
	fon18w.SetColorTable(1);
	fon18y3.SetGPIndex(GPID);
	fon18y3.SetColorTable(3);
	fon18y5.SetGPIndex(GPID);
	fon18y5.SetColorTable(6);
	// Med 16
	GPID=GPS.PreLoadGPImage("interf3\\Fonts\\FontG16");
	fon16y1.SetGPIndex(GPID);
	fon16y3.SetGPIndex(GPID);
	fon16y5.SetGPIndex(GPID);
	fon16y1.SetColorTable(1);
	fon16y3.SetColorTable(3);
	fon16y5.SetColorTable(6);
	// Hint 13
	GPID=GPS.PreLoadGPImage("interf3\\Fonts\\FontC14");
	fon13y2.SetGPIndex(GPID);
	fon13y2.SetColorTable(3);
	// Old Standart
	GPID=GPS.PreLoadGPImage("interf3\\Fonts\\FontG18");
	BigWhiteFont.SetGPIndex(GPID);
	BigWhiteFont.SetColorTable(1);	
	BigYellowFont.SetGPIndex(GPID);
	BigYellowFont.SetColorTable(3);
	BigRedFont.SetGPIndex(GPID);
	BigRedFont.SetColor(0xFFFF0000);
	BigBlackFont.SetGPIndex(GPID);
	BigBlackFont.SetColorTable(6);
	//
	ADDFONT(fonSmall_01);
	ADDFONT(SmallBlackFont);
	ADDFONT(fonG12);	
	ADDFONT(SpecialBlackFont);
	ADDFONT(BlackFont);	
	ADDFONT(fonBlow_A);
	ADDFONT(fonBlow_S);	
	//
	ADDFONT(SmallWhiteFont);
	ADDFONT(SmallYellowFont);
	ADDFONT(SmallRedFont);
	ADDFONT(SmallOrangeFont);
	ADDFONT(SmallGrayFont);	
	ADDFONT(SpecialGrayFont);	
	ADDFONT(SpecialWhiteFont);
	ADDFONT(SpecialYellowFont);
	ADDFONT(SpecialRedFont);
	ADDFONT(OrangeFont);
	ADDFONT(GrayFont);	
	ADDFONT(RedFont);
	ADDFONT(YellowFont);
	ADDFONT(WhiteFont);
	//
	ADDFONT(BigWhiteFont);
	ADDFONT(BigYellowFont);
	ADDFONT(BigRedFont);
	ADDFONT(BigBlackFont);
	//
	static const DWORD c_Black	 = 0xFF2E2317;
	static const DWORD c_Red	 = 0xFF8A1000;
	static const DWORD c_Yellow  = 0xFFD4C19C;
	static const DWORD c_White	 = 0xFFFFF7EF;
	static const DWORD c_Gray	 = 0xFF6D6862;
	static const DWORD c_Orange  = 0xFF6A3000;
	static const DWORD c_Disable = 0xC0665F57;
	/* after gec change
	static const DWORD c_Black	 = 0xFF100801;
	static const DWORD c_Red	 = 0xFF8A1000;
	static const DWORD c_Yellow  = 0xFFE9C989;
	static const DWORD c_White	 = 0xFFFFF7EF;
	static const DWORD c_Gray	 = 0xFF6D6862;
	static const DWORD c_Orange  = 0xFF6A3000;
	static const DWORD c_Disable = 0xC0665F57;
	*/
	//
	DWORD col[NColor]={c_Black,c_Red,c_Yellow,c_White,c_Gray,c_Orange,c_Disable};
	char* txt[NColor]={"Black","Red","Yellow","White","Gray","Orange","Diasble"};
	int i;
	//
	SmallGrayFont.SetColor(c_Gray);
	SmallOrangeFont.SetColor(c_Orange);	
	// menu
	GPID=GPS.PreLoadGPImage("interf3\\Fonts\\FontG14");
	for(i=0;i<NColor;i++){
		fonMenuText[i].SetGPIndex(GPID);
		fonMenuText[i].SetColor(col[i]);
		DString D="MenuText"; D+=txt[i];
		EF->Add(D.str,DWORD(fonMenuText+i));
	}
	GPID=GPS.PreLoadGPImage("interf3\\Fonts\\FontG16");
	for(i=0;i<NColor;i++){
		fonMenuTitle[i].SetGPIndex(GPID);
		fonMenuTitle[i].SetColor(col[i]);
		DString D="MenuTitle"; D+=txt[i];
		EF->Add(D.str,DWORD(fonMenuTitle+i));
	}
	GPID=GPS.PreLoadGPImage("interf3\\Fonts\\FontG18");
	for(i=0;i<NColor;i++){
		fonMenuTitle2[i].SetGPIndex(GPID);
		fonMenuTitle2[i].SetColor(col[i]);
		DString D="MenuTitle2"; D+=txt[i];
		EF->Add(D.str,DWORD(fonMenuTitle2+i));
	}
	GPID=GPS.PreLoadGPImage("interf3\\Fonts\\FontG30");	
	fonMenuGold.SetGPIndex(GPID);	
	//
	fonMenuGoldBlack.SetGPIndex(GPID);
	fonMenuGoldBlack2.SetGPIndex(GPID);
	fonMenuGoldBlack3.SetGPIndex(GPID);
	//
	EF->Add("MenuGold",DWORD(&fonMenuGold));
	//
	EF->Add("MenuGoldBlack",DWORD(&fonMenuGoldBlack));
	EF->Add("MenuGoldBlack2",DWORD(&fonMenuGoldBlack2));
	EF->Add("MenuGoldBlack3",DWORD(&fonMenuGoldBlack3));
	//
	fonG12.				SetColor( c_Black  );
	fonG60y5.			SetColor( c_Black  );
	fonG60w.			SetColor( c_White  );
	fon40y1.			SetColor( c_Yellow );	
	fon40y5.			SetColor( c_Black  );
	fon30y1.			SetColor( c_Red    );	
	fon30y3.			SetColor( c_Yellow );	
	fon30y5.			SetColor( c_Yellow );	
	fon18w.				SetColor( c_White  );	
	fon18y3.			SetColor( c_Yellow );
	fon18y5.			SetColor( c_Black  );	
	fon16y1.			SetColor( c_White  );
	fon16y3.			SetColor( c_Yellow );	
	fon16y5.			SetColor( c_Black  );	
	fon13y2.			SetColor( c_Yellow );
	BigWhiteFont.		SetColor( c_White  );	
	BigYellowFont.		SetColor( c_Yellow );
	BigBlackFont.		SetColor( c_Black  );
	WhiteFont.			SetColor( c_White  );
	YellowFont.			SetColor( c_Yellow );
	RedFont.			SetColor( c_Red    );
	BlackFont.			SetColor( c_Black  );
	OrangeFont.			SetColor( c_Orange );
	GrayFont.			SetColor( c_Gray   );
	SpecialRedFont.		SetColor( c_Red    );
	SpecialGrayFont.	SetColor( c_Gray   );
	SmallWhiteFont.		SetColor( c_White  );
	SmallRedFont.		SetColor( c_Red    );
	SmallYellowFont.	SetColor( c_Yellow );
	SmallBlackFont.		SetColor( c_Black  );
	SmallWhiteFont1.	SetColor( c_White  );
	SmallRedFont1.		SetColor( c_Red    );
	SmallGrayFont1.		SetColor( c_Gray    );	
	SmallBlackFont1.	SetColor( c_Black  );
	fn8.				SetColor( c_White  );
	fn10.				SetColor( c_White  );
	SpecialBlackFont.	SetColor( c_Black );
	SmallYellowFont1.	SetColor( c_Yellow );
	SpecialWhiteFont.	SetColor( c_White  );
	SpecialYellowFont.	SetColor( c_Yellow);	
	fonMenuGoldBlack.	SetColor( c_Red ); //0xFFB1AB8D
	fonMenuGoldBlack2.	SetColor( 0xFFEAE7DE );
	fonMenuGoldBlack3.	SetColor( 0x12000000 );
	fonSmall_01.		SetColor( c_Black  );
};
