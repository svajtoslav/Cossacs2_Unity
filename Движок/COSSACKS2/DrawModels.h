#include "IMediaManager.h"
class OneModelToDraw{
public:
	int sortY;
	Matrix4D M4;
	int ModelID;
	int CFactor;
};
struct SortElm{
public:
	int Y;
	int pos;
};
class ModelsScope{
	DynArray<OneModelToDraw> MDraw;
	DynArray<SortElm> Sort;
public:
	~ModelsScope(){
		MDraw.Clear();
		Sort.GetAmount();
	}
	void Add(int ModelID,Matrix4D* M4,int sortY,int CF=256){
		SortElm SE;
		SE.pos=MDraw.GetAmount();
		SE.Y=sortY;
		Sort.Add(SE);
        OneModelToDraw MD;
		MD.sortY=sortY;
		MD.M4=*M4;
		MD.ModelID=ModelID;
		MD.CFactor=CF;
		MDraw.Add(MD);	
	}
	void Clear(){
		MDraw.Clear();
		Sort.Clear();
	}
	static int __cdecl compare(const void* el1,const void* el2){
		return ((SortElm*)el1)->Y>((SortElm*)el2)->Y?1:-1;
	}
	void Draw(){		
        qsort(Sort.GetValues(),Sort.GetAmount(),sizeof SortElm,&compare);
		//for rocks
		Matrix4D tm;
		ICamera* iCam = GetGameCamera();
		tm = iCam ? iCam->GetWorldMatrix() : Matrix4D::identity;

		Matrix4D fm=tm;
		Matrix4D m2;
		m2.rotation(Vector3D::oX,0.8f);//float(GetTickCount())/2000.0f);
		tm*=m2;
		m2.scaling(1.0f/256.0f,0.9f/256.0f,1.0f);
		tm*=m2;	
		//for fog
		m2.rotation(Vector3D::oX,0.6);//float(GetTickCount())/2000.0f);
		fm*=m2;
		m2.scaling(1.0f/456.0f,0.9f/456.0f,1.0f);
		fm*=m2;	
		float t=GetTickCount()/24000.0;
		GPS.SetCurrentDiffuse(0xFF808080);
		void ApplyGroundZBias(bool TurnOn);
		ApplyGroundZBias(true);
		static int hud2=IRS->GetShaderID("hud2");
		static int hud=IRS->GetShaderID("hud");
		GPS.SetCurrentShader(hud2);
        IRS->SetCurrentShader( hud2 );
		for(int i=0;i<Sort.GetAmount();i++){
            OneModelToDraw* OM=&MDraw[Sort[i].pos];
            int x=int(OM->M4.e30);
			int y=int(OM->M4.e31);
			if(OM->CFactor>100){
                static int ftex=IRS->GetTextureID("ffog.tga");
				IRS->SetTexture(ftex,1);
				Matrix4D m3=fm;
				Matrix4D tr=Matrix4D::identity;
				tr.translation(0,t,0);
				m3*=tr;
				IRS->SetTextureMatrix(m3,1);
				Matrix4D m4=fm;
				Matrix4D tr2=Matrix4D::identity;
				tr2.translation(t/2,t/8,0);
				m4*=tr2;
				IRS->SetTextureMatrix(m4,0);
			}else{
				DWORD GetTexAverageColor(int x,int y);			
				int GetFactTexture(int x,int y);
				DWORD C=GetTexAverageColor(x,y);
				//C=MulDWORD(C,OM->CFactor);
				IRS->SetTextureFactor(C);			
				IRS->SetTextureMatrix(tm,1);
				IRS->SetTexture(GetFactTexture(x,y),1);
			}
			//IRS->SetTexture(GetFactTexture(x,y),2);
			IMM->Render(OM->ModelID,&OM->M4);
		}
		GPS.FlushBatches();
		GPS.SetCurrentShader(hud);
		ApplyGroundZBias(false);
		Clear();
	}
};
extern ModelsScope RenderModels;