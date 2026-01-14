/*****************************************************************************/
/*	File:	IWater.h
/*	Desc:	Interface for working with water
/*	Author:	Ruslan Shestopalyuk
/*	Date:	2 Sep 2004
/*****************************************************************************/
#ifndef __IWATER_H__
#define __IWATER_H__

/*****************************************************************************/
/*  Class:  IWaterscape
/*  Desc:   Interface for manipulating water mass
/*****************************************************************************/
class IWaterscape
{
public:
    virtual void                Render          () = 0;
    virtual void                SetCellSide     ( float w, float h ) = 0;

}; // class IWaterscape

extern IWaterscape*     IWater;
#endif // __IWATER_H__