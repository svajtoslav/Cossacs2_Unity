typedef void rce_ProcedureToCall();
typedef bool rce_InputCallback(int mouse_x,int mouse_y,bool LButtonPressed,bool RButtonPressed);
typedef bool rce_ProcessClassCallback(ClassEditor* CE,BaseClass* BC,int Options);//Options=1-init, 2-process, 3-ok pressed, 4-cancel pressed 

void AddExistingEditor(char* Name,rce_ProcedureToCall* Proc);
void AddExistingEditorEx(char* Name,rce_ProcedureToCall* Proc,word HotKey);
#define REG_EXISTING_EDITOR(Name,ProcedureToCall) AddExistingEditor(Name,ProcedureToCall);
#define REG_EXISTING_EDITOR_Ex(Name,ProcedureToCall,HotKey) AddExistingEditorEx(Name,ProcedureToCall,HotKey);

#define RCE_SHOW_GAME_BACKGROUND             1  
#define RCE_ALLOW_GAME_PROCESSING            2  
//#define RCE_ALLOW_GAME_INPUT                 4
#define RCE_CLEAR_UNITS_SELECTION_BEFORE_RUN 8
#define RCE_CENTRAL_POSITION                 16
#define RCE_RIGHT_POSITION                   32
#define RCE_FULLSCREEN                       16+32
#define RCE_AUTOPOSITION                     64
#define RCE_BOTTOM		                     64+16
#define RCE_AUTOSAVE                         128
#define RCE_AUTOSIZE                         256
#define RCE_EXITONESCAPE                     512
#define RCE_EXITONENTER                      1024

#define RCE_DOUBLEPANEL11                    2048
#define RCE_DOUBLEPANEL12                    4096
#define RCE_DOUBLEPANEL21                    2048+4096

#define RCE_HIDEOKCANCEL                     8192
#define RCE_HIDEHEADER                       16384
#define RCE_INVISIBLE                        32768


#define RCE_DEFAULT RCE_CENTRAL_POSITION|RCE_AUTOSAVE|RCE_EXITONESCAPE|RCE_EXITONENTER
#define RCE_DEFAULT1 RCE_SHOW_GAME_BACKGROUND|RCE_ALLOW_GAME_PROCESSING|RCE_CLEAR_UNITS_SELECTION_BEFORE_RUN|RCE_CENTRAL_POSITION|RCE_AUTOSAVE|RCE_EXITONESCAPE|RCE_EXITONENTER //|RCE_ALLOW_GAME_INPUT
#define RCE_DEFAULT2 RCE_FULLSCREEN|RCE_AUTOSAVE|RCE_EXITONESCAPE|RCE_EXITONENTER
#define RCE_DEFAULT3 RCE_SHOW_GAME_BACKGROUND|RCE_ALLOW_GAME_PROCESSING|RCE_RIGHT_POSITION|RCE_AUTOSAVE|RCE_EXITONESCAPE|RCE_EXITONENTER|RCE_HIDEOKCANCEL|RCE_HIDEHEADER //|RCE_ALLOW_GAME_INPUT

class ClassEditorProcessor{
	BaseClass* ClassToEdit;
	_str xmlName;
	_str EditorName;
	bool COMPLEX;	
	rce_ProcessClassCallback* Process;
	rce_InputCallback* Input;
	DialogsSystem DSS;
	SimpleDialog* HEADER;
	SimpleDialog* OK;
	SimpleDialog* CANCEL;
	SimpleDialog* DESK;
	SimpleDialog* TDESK;
	ClassEditor CE;
	ComplexClassEditor CCE;
	static bool _Lpressed;
	static bool _Rpressed;
	static bool IGN;
public:
	DWORD Options;
	ClassEditorProcessor();
	~ClassEditorProcessor();
    void PreProcess();
	static void ProcessGame();	
	int PostProcess();//returns  0-continue required 1 - ok and exit 2 - cancel and exit
	static void DrawScreen();
	void ChangeSizeAndPos(RECT R);
	RECT  GetSizeAndPos();
	void CreateFromClass(BaseClass* _BC,const char* _EditorName,const char* _XmlName="",DWORD _Options=RCE_DEFAULT,rce_ProcessClassCallback* _Process=NULL,rce_InputCallback* _Input=NULL);
	//example of use:
	//
	//ClassEditorProcessor CEP1;
	//ClassEditorProcessor CEP2;
	//
	//CEP1.CreateFromClass(...);
	//CEP2.CreateFromClass(...);
	//
	//int r;
	//do{
	//    r=0;
	//    CEP1.PreProcess();
	//    CEP2.PreProcess();
	//    ClassEditorProcessor::ProcessGame();
	//    r|=CEP1.PostProcess();
	//    r|=CEP2.PostProcess();
	//    ClassEditorProcessor::DrawScreen();
	//}while(r==0);
	//

};

void ProcessEditor(char* EditorName);
void ReplaceEditor(char* EditorName, BaseClass* pClass);
void AddStdEditor(char* Name,BaseClass* BC,const char* XmlName,DWORD Options,rce_ProcessClassCallback* Process=NULL,rce_InputCallback* Input=NULL,word HotKey=0);
void SimpleEditClass(char* Name,BaseClass* BC,const char* XmlName="",bool Autosave=false);
void EditClass(char* Name,BaseClass* BC,const char* XmlName,DWORD Options,
			   rce_ProcessClassCallback* Process,rce_InputCallback* Input);
#define REG_CLASS_EDITOR(Name,ObjectName,XML_File_Name,Options,ProcessCallbackIfNeed,InputCallBackIfNeed) AddStdEditor(Name,ObjectName,XML_File_Name,Options,ProcessCallbackIfNeed,InputCallBackIfNeed)