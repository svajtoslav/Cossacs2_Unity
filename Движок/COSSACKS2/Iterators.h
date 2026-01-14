#pragma once
//typedef bool tpUnitsCallback(OneObject* OB,void* param);
//int PerformActionOverUnitsInRadius(int xc,int yc,int R,tpUnitsCallback* CB,void* Param);

/*
class OneObjectIterator{
public:
	void GetUnitsInRadius(int xc,int yc,int R,byte Nmask=0xFF);
	void GetUnitsInSquare(int x,int y,int x1,int y1,byte Nmask=0xFF);
	void GetBuildingsInRadius(int xc,int yc,int R,byte Nmask=0xFF);
	void GetBuildingsInSquare(int x,int y,int x1,int y1,byte Nmask=0xFF);
	void GetUnitsOfBrigade(Brigade* BR);
	void GetNotCommandUnitsOfBrigade(Brigade* BR);
	void GetCommandUnitsOfBrigade(Brigade* BR);
	void GetUnitsOfNation(int NI);
    void GetAllUnits();
	void GetSelected(int NI);
	void GetImSelected(int NI);

	OneObject* GetNext();
};
*/
class units_iterator{
public:
	class UnitsInRadius
	{
		word ids[2048];
		int nids;
		int pos;
		int _xc,_yc,_R2;
	public:		
		void Create(int _xc,int _yc,int R);
		OneObject* Next();
	};
	class GetUnitsInSquare
	{
		word ids[2048];
		int nids;
		int pos;
		int _x,_y,_x1,_y1;
	public:
		void Create(int x,int y, int x1, int y1);
		OneObject* Next();
	};
	class GetBuildingsInRadius
	{
		word ids[2048];
		int nids;
		int pos;
		int _xc,_yc,_R2;
	public:		
		void Create(int _xc,int _yc,int R);
		OneObject* Next();
	};
	class GetBuildingsInSquare
	{
		word ids[2048];
		int nids;
		int pos;
		int _x,_y,_x1,_y1;
	public:
		void Create(int x,int y, int x1, int y1);
		OneObject* Next();
	};
	class GetUnitsOfBrigade
	{
		int pos;
		Brigade* Br;
	public:
		void Create(Brigade* BR);
		OneObject* Next();
	};
	class GetNotCommandUnitsOfBrigade
	{
		int pos;
		Brigade* Br;
	public:
		void Create(Brigade* BR);
		OneObject* Next();
	};
	class GetUnitsOfNation
	{
		int pos;
		word* id;
		int   n;
	public:
		void Create(int NI);
		OneObject* Next();
	};
	class GetAllUnits
	{
		int pos;
	public:
		void Create();
		OneObject* Next();
	};
	class GetSelected
	{
		int n;
		word* id;
		int pos;
	public:
		void Create(int NI);
		OneObject* Next();
	};
	class GetImSelected
	{
		int n;
		word* id;
		int pos;
	public:
		void Create(int NI);
		OneObject* Next();
	};
};
extern units_iterator::UnitsInRadius itr_UnitsInRadius;
extern units_iterator::GetUnitsInSquare itr_GetUnitsInSquare;
extern units_iterator::GetBuildingsInRadius itr_GetBuildingsInRadius;
extern units_iterator::GetBuildingsInSquare itr_GetBuildingsInSquare;
extern units_iterator::GetUnitsOfBrigade itr_GetUnitsOfBrigade;
extern units_iterator::GetNotCommandUnitsOfBrigade itr_GetNotCommandUnitsOfBrigade;
extern units_iterator::GetUnitsOfNation itr_GetUnitsOfNation;
extern units_iterator::GetAllUnits itr_GetAllUnits;
extern units_iterator::GetSelected itr_GetSelected;
extern units_iterator::GetImSelected itr_GetImSelected;

//units_iterator::UnitsInRadius E(...);
//example
//while(OneObject* OB=E.Next()){
//
//}
 
class OneSpriteIterator{
public:
    void GetSpritesInRadius(int x,int y,int R);
	void GetSpritesInSquare(int x,int y,int R);
	void GetAllSprites();

	OneSprite* GetNext();
};