#if !defined(DIP_SERVER_DSTRING_DEFFF)
#define DIP_SERVER_DSTRING_DEFFF
class DString;
class DIALOGS_API DString{
public:
	char* str;
	int L;
	int MaxL;
	DString(){
			str=NULL;
			L=0;
			MaxL=0;
		};
	DString(char* s){
			str=NULL;
			L=0;
			MaxL=0;
			Assign(s);
		};
	~DString(){
			if(str)free(str);
			str=NULL;
			L=0;
			MaxL=0;
		};
	void Allocate(int LN){
			if(MaxL<LN){
				MaxL=((LN+LN/2+256)&0xFFFFFFF0);
				str=(char*)realloc(str,MaxL);
				str[L]=0;
			};
		};
	void ReadFromFile(char* file){
			Clear();
			/*ResFile F=RReset(file);
			if(F!=INVALID_HANDLE_VALUE){
				int sz=RFileSize(F);
				if(sz){
					Allocate(sz+1);
					RBlockRead(F,str,sz);
					str[sz]=0;
					L=strlen(str);
				};
				RClose(F);
			};*/
			FILE* F=fopen(file,"rb");
			if(F){
				fseek(F,0,SEEK_END);
                int sz=ftell(F);
				fseek(F,0,SEEK_SET);
				if(sz){
					Allocate(sz+1);
					fread(str,1,sz,F);
					str[sz]=0;
					L=strlen(str);
				};
				fclose(F);
			};
		};
	void WriteToFile (char* file){
			ResFile F=RRewrite(file);
			if(F!=INVALID_HANDLE_VALUE){
				if(str)RBlockWrite(F,str,L);
				RClose(F);
			};
		};
	void Add(DString& Str){
			if(!Str.str)return;
			Allocate(L+Str.L+1);
			strcat(str+L,Str.str);
			L+=Str.L;
		};
	void Add(const char* Str){ 
		if(Str){
			int L1=strlen(Str);
			Allocate(L+L1+1);
			strcat(str+L,Str);
			L+=L1;
		}
	}
	void Add(char Ch){
		if(str){
			Allocate(L+1+1);
			str[L]=Ch;
			str[L+1]=0;
			L++;
		}
	}
	void Add(int v){
		char s[16];
		sprintf(s,"%d",v);
		Add(s);
	};
	void print(char* mask,...){
		va_list args;
		va_start(args,mask);
		char temp[4096];
		vsprintf(temp,mask,args);
		va_end(args);
		Add(temp);
	}
	void Replace0(char* src,char* dst){
		if(!str)return;
		char* s=str;
		int L0=strlen(src);
		int L1=strlen(dst);
		do{
			s=strstr(s,src);
			if(s){
				int pos=s-str;
				Allocate(L+L1-L0+1);
				memmove(str+pos+L1,str+pos+L0,L-pos-L0+1);
				memcpy(str+pos,dst,L1);
				L=strlen(str);
				s=str+pos+L1;
			};
		}while(s);
	};
	void Replace(char* src,DString& dst){
			if(!dst.str)return;
			Replace(src,dst.str);
		};
	void Replace(char* src,char* dst,...){
		assert(strlen(dst)<4000);
		char ccc[4096];
        va_list va;
        va_start(va,dst);
        vsprintf (ccc,dst,va);   
        va_end(va);
		assert(strlen(ccc)<4000);
		Replace0(src,ccc);
	};
	void Replace(char* src,int value){
		char cc[16];
		sprintf(cc,"%d",value);
		Replace(src,cc);
	};
	void Clear(){
			L=0;
			if(str)str[0]=0;
		};
	inline bool isClear(){ return str==NULL||str[0]==0; }
	void Free(){
			if(str)free(str);
			str=NULL;
			L=0;
			MaxL=0;
		};
	void Assign(char* Str){
			if(Str){
				Clear();
				L=strlen(Str);
				Allocate(L+1);
				strcpy(str,Str);
			}
		};
	void Assign(DString& Str){
			if(Str.str)Assign(Str.str);
			else Clear();
		};
	void Assign(const int& a){
		char s[16];
		sprintf(s,"%d",a);
		Assign(s);
		};
	DString& operator = (char* s){
			Assign(s);
			return *this;
		};
	DString& operator = (const char* s){
			Assign((char*)s);
			return *this;
		};
	DString& operator = (DString& ds1){
			Assign(ds1);
			return *this;
		};
	DString& operator = (const DString& ds1){
			Assign((DString&)ds1);
			return *this;
		};
	DString& operator = (const int& a){
			Assign(a);
			return *this;
		};
	DString& operator + (const int& a){
			Add((int)a);
			return *this;
		};
	DString& operator + (char* s){
			Add(s);
			return *this;
		};
	DString& operator + (const char* s){
			Add((char*)s);
			return *this;
		};
	DString& operator + (DString& ds1){
			Add(ds1);
			return *this;
		};
	DString& operator + (const DString& ds1){
			Add((DString&)ds1);
			return *this;
		};
	DString& operator += (const int& a){
			Add((int)a);
			return *this;
		};
	DString& operator += (char* s){
			Add(s);
			return *this;
		};
	DString& operator += (const char* s){
			Add((char*)s);
			return *this;
		};
	DString& operator += (DString& ds){
			Add(ds);
			return *this;
		};
	DString& operator += (const DString& ds){
			Add((DString&)ds);
			return *this;
		};
	bool operator == (char* s){
			if(s&&str){
				return !strcmp(s,str);
			}else return false;
		};
	bool operator == (const char* s){
			if((char*)s&&str){
				return !strcmp(s,str);
			}else return false;
		};
	bool operator == (DString& ds){
			if(ds.str&&str){
				return !strcmp(ds.str,str);
			}else return false;
		};
	bool operator == (const DString& ds){
			if(ds.str&&str){
				return !strcmp(ds.str,str);
			}else return false;
		};
	bool operator != (char* s){
		return !(this->operator ==(s));
		};
	bool operator != (const char* s){
		return !(this->operator ==(s));
		};
	bool operator != (DString& ds){
		return !(this->operator ==(ds));
		};
	bool operator != (const DString& ds){
		return !(this->operator ==(ds));
		};
	char operator [] (int index){
			if(str&&index<L)return str[index];
			else return 0;
		};
	void ExtractLine(DString& dst)
		{
			dst.Clear();
			char seps[]="\n";
			char *token;
			DString rez;
			rez.Assign(str);
			token=strtok(str,seps);
			if(token!=NULL)
			{
				dst.Add(token);
				int s=strlen(token);
				char* c;
				if(s<rez.L)
					c=(char*)(strstr(rez.str,token)+s+1);
				else 
					c=(char*)(strstr(rez.str,token)+s);
				Assign(c);
			}
		}
	void ExtractWord(DString& dst)
		{
			dst.Clear();
			char seps[]=" ,;:\t\n\r";
			char *token;
			DString rez;
			rez.Assign(str);
			token=strtok(str,seps);
			if(token!=NULL)
			{
				dst.Add(token);
				int s=strlen(token);
				char* c;
				if(s<rez.L)
					c=(char*)(strstr(rez.str,token)+s+1);
				else 
					c=(char*)(strstr(rez.str,token)+s);
				Assign(c);
			}
		}
	void ExtractWord2(DString& dst)
		{
			dst.Clear();
			char seps[]=" ,;:\t\n";
			char *token;
			DString rez;
			rez.Assign(str);
			token=strtok(str,seps);
			if(token!=NULL)
			{
				dst.Add(token);
				int s=strlen(token);
				char* c;
				if(s<rez.L)
					c=(char*)(strstr(rez.str,token)+s+1);
				else 
					c=(char*)(strstr(rez.str,token)+s);
				Assign(c);
			}
			else
			{
				Clear();
			}

		}
	void DeleteChars(int From, int N)
		{
			if((From+N)<=L)
			{
				memmove(str+From,str+From+N,L-From-N);
				L-=N;
				str[L]=0;
			}
		}
	void Limit(int maxL){
		if(str&&L>maxL){
			str[maxL]=0;
			L=maxL;
		}
	}
};
#endif
