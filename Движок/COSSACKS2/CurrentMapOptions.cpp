#include "stdheader.h"
#include "CurrentMapOptions.h"
#include ".\supereditor.h"
MapOptions MOptions;
extern int ItemChoose;
bool MMItemChoose(SimpleDialog* SD);
void EditMapOptions(){
	xmlQuote xml;
	ItemChoose=-1;
	if(xml.ReadFromFile("Dialogs\\MapOptionsDialog.DialogsSystem.xml")){
		DialogsSystem DSS;
		ErrorPager EP;
		DSS.Load(xml,&DSS,&EP);
		SimpleDialog* Desk=DSS.Find("OptionsDesk");
		SimpleDialog* OK=DSS.Find("OK");
		SimpleDialog* CANCEL=DSS.Find("CANCEL");
		if(Desk&&OK&&CANCEL){
			int x0,y0,x1,y1;
			DSS.GetDialogsFrame(x0,y0,x1,y1);
			if(x1>x0){
				DSS.x=(RealLx-x1+x0)/2;
				DSS.y=(RealLy-y1+y0)/2;
				OK->OnUserClick=&MMItemChoose;
				OK->UserParam=1;
				CANCEL->OnUserClick=&MMItemChoose;
				CANCEL->UserParam=1;
				ClassEditor CE;
				CE.CreateFromClass(Desk,0,0,Desk->x1-Desk->x,Desk->y1-Desk->y,&MOptions,3,"EmptyBorder");
				do{
                    ProcessMessages();					
					DSS.ProcessDialogs();
					CE.Process();
					DSS.RefreshView();
				}while(ItemChoose==-1);
			}
		}
	}
}
extern char GlobalTextCommand[256];
class ScriptEd:public BaseClass{
public:
	OneScript Script;
	SAVE(ScriptEd);
	REG_AUTO(Script);
	ENDSAVE;
};
void KeyTestMem();
void EditScripts(){
	xmlQuote xml;
	ItemChoose=-1;
	if(xml.ReadFromFile("Dialogs\\ScriptEditor.DialogsSystem.xml")){
		DialogsSystem DSS;
		ErrorPager EP;
		DSS.Load(xml,&DSS,&EP);
		SimpleDialog* Desk=DSS.Find("CLASSEDITOR");
		SimpleDialog* CmlDesk=DSS.Find("SOURCEDESK");
		SimpleDialog* OK=DSS.Find("OK");
		SimpleDialog* CANCEL=DSS.Find("CANCEL");
		DString ScriptText;ScriptText.Add("");
		//Privet {CG}{R FFFF0000 Shuriku}Shuriku{C} ot\\ {CG}{R FFFF0000 druzej} druzej{C} po rabote"
		TextButton* TB=CmlDesk->addTextButton(NULL,0,0,ScriptText.str,&WhiteFont,&WhiteFont,&WhiteFont,0);

		//CmlDesk->addInputBox(NULL,0,100,GlobalTextCommand,100,100,20,&WhiteFont,&WhiteFont);
		if(Desk&&OK&&CANCEL){
			int x0,y0,x1,y1;
			DSS.GetDialogsFrame(x0,y0,x1,y1);
			if(x1>x0){
				DSS.x=(RealLx-x1+x0)/2;
				DSS.y=(RealLy-y1+y0)/2;
				OK->OnUserClick=&MMItemChoose;
				OK->UserParam=1;
				CANCEL->OnUserClick=&MMItemChoose;
				CANCEL->UserParam=1;
				ClassEditor CE;
				//OperandBlock FirstOperand;
				ScriptEd Script;
				Script.Script.ReadQppFile("AI\\test.qpp");
				//FirstOperand.Add( (Operand*) new OperandBlock());
				CE.CreateFromClass(Desk,0,0,Desk->x1-Desk->x,Desk->y1-Desk->y,&Script,3,"EmptyBorder");
				do{
					if(strlen(GlobalTextCommand)>0)
					{
						int ii=atoi(GlobalTextCommand);
						void* Adres=(void*)ii;
						if(Adres)
						{
							void SetClassEditorScrollPosTo(void* Adres,ClassEditor* CE);
							SetClassEditorScrollPosTo(Adres,&CE);
						}
						GlobalTextCommand[0]=0;
					}
					ScriptText.Clear();
					Script.Script.VAR.RefreshEnumerator();
					Script.Script.VAR.GetAssembledView(ScriptText,false);
					Script.Script.BrigadesVariables.RefreshEnumerator();
					Script.Script.BrigadesVariables.GetAssembledView(ScriptText,false);
					Script.Script.Script.GetAssembledView(ScriptText,false);
					ScriptText.WriteToFile("gec.qpp");
					//Script.Script.Script.Execute();
					TB->Message=ScriptText.str;
					ProcessMessages();					
					DSS.ProcessDialogs();
					if(ItemChoose!=-1)break;
					CE.Process();
					ItemChoose=-1;
					DSS.RefreshView();
				}while(ItemChoose==-1);
			}
		}
	}
}
//==================================================================================================================//
int PlayGameProcessList::GetExpansionRules()
{
	return 2; 
}
bool PlayGameProcess::Process()
{
	return false;
}
//------------------------------------------------------------------------------------------------------------------//
bool PlayGameProcessList::Process()
{
	bool rez=false;
	int n=GetAmount();
	for(int i=0;i<n;i++)
	{
		rez=(*this)[i]->Process()||rez;
	}
	return rez;
}
//------------------------------------------------------------------------------------------------------------------//
StartTacticalAI::StartTacticalAI()
{
	NI=0;
}
//
bool vf_CheckTackticalAIStart(byte NI);
void ActivateTacticalAI(byte NI);
//
bool StartTacticalAI::Process()
{
	/*
	CurrentGameInfo* g=&GSets.CGame;
	for(int i=NPlayers;i<8&&COMPSTART[i];i++){
		PlayerInfo* I=g->PL_INFO+i;
		ActivateTacticalAI(I->ColorID);
	}
	return true;
	*/
	if(vf_CheckTackticalAIStart(NI)){
		ActivateTacticalAI(NI);
		return true;
	}
	return false;	
}
//------------------------------------------------------------------------------------------------------------------//
bool PreviewBinkVideo::Process(){
	extern word NPlayers;
	if(NPlayers==1&&!BinkFile.isClear()){
		void PlayFullscreenVideo(char* name,float px,float py);
		PlayFullscreenVideo(BinkFile.str, 0.0f, 0.17f);
	}
	return false;
}
//
int StartResScope::GetN_Single(){
	int n=0;
	for(int i=0;i<=MaxNatColors;i++){
		if(!Player[i].DisableInSingle){
			n++;
		}
	}
	return n;
};
int StartResScope::GetN_Multi(){
	int n=0;
	for(int i=0;i<=MaxNatColors;i++){
		if(!Player[i].DisableInMultiplayer){
			n++;
		}
	}
	return n;
};

//==================================================================================================================//
