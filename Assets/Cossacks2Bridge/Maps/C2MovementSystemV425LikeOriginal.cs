// C2MovementSystemV425LikeOriginal.cs
// Unified Cossacks II 1.1 movement implementation.
// Physical ownership rule: movement/path/topology/brigade-road implementation lives here.
// Unity-facing compatibility files may keep thin entry points only; they must not own a second algorithm.
// Original sources: COSSACKS2/path.cpp, Motion.cpp, NewMon.cpp, Groups.cpp,
// BrigadeOrders.cpp, Brigade.cpp, HashTop.cpp, TopoGraf.cpp, Factures3D.cpp,
// fpath/FPathFinder.cpp and fpath/FPathFinderLines.h.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using UnityEngine;
using Debug = UnityEngine.Debug;

// ============================================================================
// MERGED FROM: C2OriginalMovementMathV352.cs
// ============================================================================
namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // Exact integer direction math copied from COSSACKS2/NewMon.cpp::GetDir
    // and COSSACKS2/Weapon.cpp tables. Do not replace with Atan2/Sin/Cos:
    // C2 command/facing logic is quantized through these tables.
    internal static class C2OriginalMovementMathV352
    {
        private static readonly short[] TAtg = new short[]
        {
            0,0,0,0,0,0,0,1,1,1,1,1,1,2,2,2,2,2,2,3,3,3,3,3,3,3,4,4,4,4,4,4,5,5,5,5,5,5,6,6,6,6,6,6,6,7,7,7,7,7,7,8,8,8,8,8,8,8,9,9,9,9,9,9,9,10,10,10,10,10,10,11,11,11,11,11,11,11,12,12,12,12,12,12,12,13,13,13,13,13,13,13,14,14,14,14,14,14,14,15,15,15,15,15,15,15,15,16,16,16,16,16,16,16,17,17,17,17,17,17,17,17,18,18,18,18,18,18,18,19,19,19,19,19,19,19,19,20,20,20,20,20,20,20,20,20,21,21,21,21,21,21,21,21,22,22,22,22,22,22,22,22,22,23,23,23,23,23,23,23,23,23,24,24,24,24,24,24,24,24,24,25,25,25,25,25,25,25,25,25,26,26,26,26,26,26,26,26,26,26,27,27,27,27,27,27,27,27,27,27,27,28,28,28,28,28,28,28,28,28,28,29,29,29,29,29,29,29,29,29,29,29,29,30,30,30,30,30,30,30,30,30,30,30,31,31,31,31,31,31,31,31,31,31,31,31,32
        };

        internal static readonly short[] TSin = new short[]
        {
            0,6,12,18,25,31,37,43,49,56,62,68,74,80,86,92,97,103,109,115,120,126,131,136,142,147,152,157,162,167,171,176,181,185,189,193,197,201,205,209,212,216,219,222,225,228,231,234,236,238,241,243,244,246,248,249,251,252,253,254,254,255,255,255,256,255,255,255,254,254,253,252,251,249,248,246,244,243,241,238,236,234,231,228,225,222,219,216,212,209,205,201,197,193,189,185,181,176,171,167,162,157,152,147,142,136,131,126,120,115,109,103,97,92,86,80,74,68,62,56,49,43,37,31,25,18,12,6,0,-6,-12,-18,-25,-31,-37,-43,-49,-56,-62,-68,-74,-80,-86,-92,-97,-103,-109,-115,-120,-126,-131,-136,-142,-147,-152,-157,-162,-167,-171,-176,-181,-185,-189,-193,-197,-201,-205,-209,-212,-216,-219,-222,-225,-228,-231,-234,-236,-238,-241,-243,-244,-246,-248,-249,-251,-252,-253,-254,-254,-255,-255,-255,-256,-255,-255,-255,-254,-254,-253,-252,-251,-249,-248,-246,-244,-243,-241,-238,-236,-234,-231,-228,-225,-222,-219,-216,-212,-209,-205,-201,-197,-193,-189,-185,-181,-176,-171,-167,-162,-157,-152,-147,-142,-136,-131,-126,-120,-115,-109,-103,-97,-92,-86,-80,-74,-68,-62,-56,-49,-43,-37,-31,-25,-18,-12,-6,0
        };

        internal static readonly short[] TCos = new short[]
        {
            256,255,255,255,254,254,253,252,251,249,248,246,244,243,241,238,236,234,231,228,225,222,219,216,212,209,205,201,197,193,189,185,181,176,171,167,162,157,152,147,142,136,131,126,120,115,109,103,97,92,86,80,74,68,62,56,49,43,37,31,25,18,12,6,0,-6,-12,-18,-25,-31,-37,-43,-49,-56,-62,-68,-74,-80,-86,-92,-97,-103,-109,-115,-120,-126,-131,-136,-142,-147,-152,-157,-162,-167,-171,-176,-181,-185,-189,-193,-197,-201,-205,-209,-212,-216,-219,-222,-225,-228,-231,-234,-236,-238,-241,-243,-244,-246,-248,-249,-251,-252,-253,-254,-254,-255,-255,-255,-256,-255,-255,-255,-254,-254,-253,-252,-251,-249,-248,-246,-244,-243,-241,-238,-236,-234,-231,-228,-225,-222,-219,-216,-212,-209,-205,-201,-197,-193,-189,-185,-181,-176,-171,-167,-162,-157,-152,-147,-142,-136,-131,-126,-120,-115,-109,-103,-97,-92,-86,-80,-74,-68,-62,-56,-49,-43,-37,-31,-25,-18,-12,-6,0,6,12,18,25,31,37,43,49,56,62,68,74,80,86,92,97,103,109,115,120,126,131,136,142,147,152,157,162,167,171,176,181,185,189,193,197,201,205,209,212,216,219,222,225,228,231,234,236,238,241,243,244,246,248,249,251,252,253,254,254,255,255,255,256
        };

        internal static byte GetDir(int dx, int dy)
        {
            int phDir;
            if (dx != 0 || dy != 0)
            {
                int adx = Math.Abs(dx);
                int ady = Math.Abs(dy);
                if (adx > ady)
                    phDir = (byte)TAtg[(ady << 8) / adx];
                else
                    phDir = 64 - (byte)TAtg[(adx << 8) / ady];
                if (dx < 0) phDir = 128 - phDir;
                if (dy < 0) phDir = 256 - phDir;
            }
            else
            {
                phDir = 64 + 128;
            }
            return (byte)((phDir + 1024) & 255);
        }

        internal static byte Quantize16(byte dir)
        {
            return (byte)((dir + 8) & 0xF0);
        }

        internal static int Norma(int dx, int dy)
        {
            // COSSACKS2/NewMon.h exact ASM:
            // (max(abs(x),abs(y)) + abs(x) + abs(y)) >> 1.
            int ax = Math.Abs(dx);
            int ay = Math.Abs(dy);
            int mx = ax > ay ? ax : ay;
            return (mx + ax + ay) >> 1;
        }

        internal static int EuclideanLengthTruncated(int dx, int dy)
        {
            // mapa.cpp StrelMode is one of the places that deliberately uses sqrt,
            // not Norma: int(sqrt(rx*rx+ry*ry)).
            return (int)Math.Sqrt((double)dx * dx + (double)dy * dy);
        }
    }
}


// ============================================================================
// MERGED FROM: C2FPathFinderV421LikeOriginal.cs
// ============================================================================
namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // CII 1.1 fpath/FPathFinder.cpp + FPathFinderLines.h, macro-expanded port.
    // The linked-point pool replaces myManager<FPoint>; branch order, integer
    // line rasterization, two-sided wall following and Bending are preserved.
    // Geometry/obstruction queries are provided by the caller's MotionField.
    internal sealed class C2FPathFinderV421LikeOriginal
    {
        internal sealed class Point { internal int x,y; internal Point Next; }
        internal sealed class PointPool
        {
            readonly Stack<Point> free = new Stack<Point>();
            internal Point Allocate() { return free.Count != 0 ? free.Pop() : new Point(); }
            internal void Free(Point p) { free.Push(p); }
        }
        internal sealed class Path
        {
            internal Point First,Last; internal int Len;
            readonly PointPool pool;
            internal Path(PointPool p) { pool=p; }
            internal void Add(int x,int y)
            {
                var p=pool.Allocate();p.x=(short)x;p.y=(short)y;p.Next=null;
                if(First==null)First=Last=p;else { Last.Next=p;Last=p; } Len++;
            }
            internal void AddUnique(int x,int y) { if(First==null||Last.x!=x||Last.y!=y)Add(x,y); }
            internal void Null() { First=Last=null;Len=0; }
            internal void Clear() { var p=First;while(p!=null){var next=p.Next;pool.Free(p);p=next;}Null(); }
        }
        readonly PointPool pool=new PointPool();
        readonly Point ptFrom=new Point(),ptTo=new Point();
        readonly Path PathL,PathR,LinePath,MainPath;
        internal readonly Path WayPoints;
        readonly Func<int,int,bool> fCheckPt;
        readonly HashSet<long> temporaryLine=new HashSet<long>();
        readonly bool isOptimize=true;
        internal C2FPathFinderV421LikeOriginal(Func<int,int,bool> free)
        {
            fCheckPt=free;PathL=new Path(pool);PathR=new Path(pool);LinePath=new Path(pool);
            MainPath=new Path(pool);WayPoints=new Path(pool);
        }
        static long Key(int x,int y) { return ((long)x<<32)|(uint)y; }
        bool fbCheckPt(int x,int y) { return temporaryLine.Contains(Key(x,y)); }
        void fbBSetPt(int x,int y) { temporaryLine.Add(Key(x,y)); }
        void fbBClrPt(int x,int y) { temporaryLine.Remove(Key(x,y)); }
        static int Norma(int x,int y) { return C2OriginalMovementMathV352.Norma(x,y); }

 int LayLine(Point _ptFrom, Point _ptTo)
{
   LinePath.Clear();
a10:;
   int xn = _ptFrom.x, yn = _ptFrom.y, xk = _ptTo.x, yk = _ptTo.y;
   int pxn = xn, pyn = yn;
   int dx, dy, s, sx, sy, kl, swap, incr1, incr2;
   sx = 0;
   dx = xk - xn;
   if(dx < 0)
   {
      dx = -dx;
      sx--;
   }
   else
   if(dx > 0) sx++;
   sy = 0;
   dy = yk - yn;
   if(dy < 0)
   {
      dy = -dy;
      sy--;
   }
   else
   if(dy > 0) sy++;
   swap = 0;
   kl = dx;
   s = dy;
   if(kl < s)
   {
      dx = s;
      dy = kl;
      kl = s;
      swap++;
   }
   incr1 = dy << 1;
   s = incr1 - dx;
   incr2 = dx << 1;
   { if(!fCheckPt(pxn, pyn) && fCheckPt(xn, yn)) { LinePath.Add(xn, yn); fbBSetPt(xn, yn); _ptFrom.x = xn; _ptFrom.y = yn; goto a10; } if(fCheckPt(pxn, pyn) && !fCheckPt(xn, yn)) { LinePath.Add(pxn, pyn); LinePath.Add(xn, yn); } pxn = xn; pyn = yn; }; 
   while(--kl >= 0)
   {
      if(s >= 0)
      {
         if(swap != 0) xn += sx;
         else yn += sy;
         s -= incr2;
      }
      if(swap != 0) yn += sy;
      else xn += sx;
      s += incr1;
      { if(!fCheckPt(pxn, pyn) && fCheckPt(xn, yn)) { LinePath.Add(xn, yn); fbBSetPt(xn, yn); _ptFrom.x = xn; _ptFrom.y = yn; goto a10; } if(fCheckPt(pxn, pyn) && !fCheckPt(xn, yn)) { LinePath.Add(pxn, pyn); LinePath.Add(xn, yn); } pxn = xn; pyn = yn; }; 
   }
   return LinePath.Len;
}
 bool CheckLine(Point _ptFrom, Point _ptTo)
{
   int xn = _ptFrom.x, yn = _ptFrom.y, xk = _ptTo.x, yk = _ptTo.y;
   int dx, dy, s, sx, sy, kl, swap, incr1, incr2;
   sx = 0;
   dx = xk - xn;
   if(dx < 0)
   {
      dx = -dx;
      sx--;
   }
   else
   if(dx > 0) sx++;
   sy = 0;
   dy = yk - yn;
   if(dy < 0)
   {
      dy = -dy;
      sy--;
   }
   else
   if(dy > 0) sy++;
   swap = 0;
   kl = dx;
   s = dy;
   if(kl < s)
   {
      dx = s;
      dy = kl;
      kl = s;
      swap++;
   }
   incr1 = dy << 1;
   s = incr1 - dx;
   incr2 = dx << 1;
   if(!fCheckPt(xn, yn)) return false;
   while(--kl >= 0)
   {
      if(s >= 0)
      {
         if(swap != 0) xn += sx;
         else yn += sy;
         s -= incr2;
      }
      if(swap != 0) yn += sy;
      else xn += sx;
      s += incr1;
      if(!fCheckPt(xn, yn)) return false;
   }
   return true;
}
 int MakeLine(Point _ptFrom, Point _ptTo, Path Path)
{
   int xn = _ptFrom.x, yn = _ptFrom.y, xk = _ptTo.x, yk = _ptTo.y;
   int dx, dy, s, sx, sy, kl, swap, incr1, incr2, Len = 0;
   sx = 0;
   dx = xk - xn;
   if(dx < 0)
   {
      dx = -dx;
      sx--;
   }
   else
   if(dx > 0) sx++;
   sy = 0;
   dy = yk - yn;
   if(dy < 0)
   {
      dy = -dy;
      sy--;
   }
   else
   if(dy > 0) sy++;
   swap = 0;
   kl = dx;
   s = dy;
   if(kl < s)
   {
      dx = s;
      dy = kl;
      kl = s;
      swap++;
   }
   incr1 = dy << 1;
   s = incr1 - dx;
   incr2 = dx << 1;
   { Path.Add(xn, yn); Len++; }; 
   while(--kl >= 0)
   {
      if(s >= 0)
      {
         if(swap != 0) xn += sx;
         else yn += sy;
         s -= incr2;
      }
      if(swap != 0) yn += sy;
      else xn += sx;
      s += incr1;
      { Path.Add(xn, yn); Len++; }; 
   }
   return Len;
}
 int VectorsOptimize()
{
   int RemovedNumber = 0;
   Point p0 = WayPoints.First;
   if(p0 == null) return 0;
   Point p1 = p0.Next;
   if(p1 == null) return 0;
   Point p2 = p1.Next;
   if(p2 == null) return 0;
   while(p0 != null && p1 != null && p2 != null)
   {
      if(CheckLine(p0, p2))
      {
         p0.Next = p2;
         pool.Free(p1);
         RemovedNumber++;
      }
      p0 = p2;
      p1 = p0.Next;
      if(p1 == null) break;
      p2 = p1.Next;
   }
   return RemovedNumber;
}
 int VectorsOptimize1()
{
   int RemovedNumber = 0;
   Point wp0 = WayPoints.First;
   if(wp0 == null) return 0;
   while(wp0 != null)
   {
      Point wp = wp0.Next;
      while(wp != null)
      {
         if(CheckLine(wp0, wp))
         {
            Point _wp = wp0.Next;
            while(_wp != wp)
            {
               Point __wp = _wp;
               wp0.Next = _wp.Next;
               _wp = _wp.Next;
               pool.Free(__wp);
               WayPoints.Len--;
               RemovedNumber++;
            }
         }
         wp = wp.Next;
      }
      wp0 = wp0.Next;
   }
   return RemovedNumber;
}
int GoAround(int x_p, int y_p, int x_w, int y_w)
{
   int x_pL = x_p, y_pL = y_p, x_wL = x_w, y_wL = y_w;
   int x_pR = x_p, y_pR = y_p, x_wR = x_w, y_wR = y_w;
   int x1L, y1L, x2L, y2L, p_lenL = 0;
   int x1R, y1R, x2R, y2R, p_lenR = 0;
   int dxL = 2, dyL = 2, dxR = 2, dyR = 2;
   int x_p_start = x_p;
   int y_p_start = y_p;
   int x_w_start = x_w;
   int y_w_start = y_w;
   bool isLeft = true;
	while(true)
   {
  		x1L = x_pL + y_wL - y_pL;
  		y1L = y_pL + x_pL - x_wL;
  		x2L = x_wL + y_wL - y_pL;
  		y2L = y_wL + x_pL - x_wL;
 		x1R = x_pR - (y_wR - y_pR);
     	y1R = y_pR - (x_pR - x_wR);
  		x2R = x_wR - (y_wR - y_pR);
     	y2R = y_wR - (x_pR - x_wR);
      isLeft = true;
      { if(!fCheckPt(x1L, y1L)) { { x_wL = x1L; y_wL = y1L; if((x_pL == x_p_start && y_pL == y_p_start && x_wL == x_w_start && y_wL == y_w_start)) return -1; }; } else { if(!fCheckPt(x2L, y2L)) { { x_wL = x2L; y_wL = y2L; if((x_pL == x_p_start && y_pL == y_p_start && x_wL == x_w_start && y_wL == y_w_start)) return -1; }; { int _dx = x_pL - x1L; int _dy = y_pL - y1L; if(dxL != _dx || dyL != _dy) { dxL = _dx; dyL = _dy; PathL.Add(x_pL, y_pL); p_lenL++; } x_pL = x1L; y_pL = y1L; if((x_pL == x_p_start && y_pL == y_p_start && x_wL == x_w_start && y_wL == y_w_start)) return -1; if(fbCheckPt(x_pL, y_pL)) { PathL.Add(x_pL, y_pL); p_lenL++; break; } }; } else { if(fbCheckPt(x1L, y1L)) { int _dx = x_pL - x1L; int _dy = y_pL - y1L; if(dxL != _dx || dyL != _dy) { dxL = _dx; dyL = _dy; PathL.Add(x_pL, y_pL); p_lenL++; } x_pL = x1L; y_pL = y1L; if((x_pL == x_p_start && y_pL == y_p_start && x_wL == x_w_start && y_wL == y_w_start)) return -1; if(fbCheckPt(x_pL, y_pL)) { PathL.Add(x_pL, y_pL); p_lenL++; break; } }; { int _dx = x_pL - x2L; int _dy = y_pL - y2L; if(dxL != _dx || dyL != _dy) { dxL = _dx; dyL = _dy; PathL.Add(x_pL, y_pL); p_lenL++; } x_pL = x2L; y_pL = y2L; if((x_pL == x_p_start && y_pL == y_p_start && x_wL == x_w_start && y_wL == y_w_start)) return -1; if(fbCheckPt(x_pL, y_pL)) { PathL.Add(x_pL, y_pL); p_lenL++; break; } }; } } };
      isLeft = false;
      { if(!fCheckPt(x1R, y1R)) { { x_wR = x1R; y_wR = y1R; if((x_pR == x_p_start && y_pR == y_p_start && x_wR == x_w_start && y_wR == y_w_start)) return -1; }; } else { if(!fCheckPt(x2R, y2R)) { { x_wR = x2R; y_wR = y2R; if((x_pR == x_p_start && y_pR == y_p_start && x_wR == x_w_start && y_wR == y_w_start)) return -1; }; { int _dx = x_pR - x1R; int _dy = y_pR - y1R; if(dxR != _dx || dyR != _dy) { dxR = _dx; dyR = _dy; PathR.Add(x_pR, y_pR); p_lenR++; } x_pR = x1R; y_pR = y1R; if((x_pR == x_p_start && y_pR == y_p_start && x_wR == x_w_start && y_wR == y_w_start)) return -1; if(fbCheckPt(x_pR, y_pR)) { PathR.Add(x_pR, y_pR); p_lenR++; break; } }; } else { if(fbCheckPt(x1R, y1R)) { int _dx = x_pR - x1R; int _dy = y_pR - y1R; if(dxR != _dx || dyR != _dy) { dxR = _dx; dyR = _dy; PathR.Add(x_pR, y_pR); p_lenR++; } x_pR = x1R; y_pR = y1R; if((x_pR == x_p_start && y_pR == y_p_start && x_wR == x_w_start && y_wR == y_w_start)) return -1; if(fbCheckPt(x_pR, y_pR)) { PathR.Add(x_pR, y_pR); p_lenR++; break; } }; { int _dx = x_pR - x2R; int _dy = y_pR - y2R; if(dxR != _dx || dyR != _dy) { dxR = _dx; dyR = _dy; PathR.Add(x_pR, y_pR); p_lenR++; } x_pR = x2R; y_pR = y2R; if((x_pR == x_p_start && y_pR == y_p_start && x_wR == x_w_start && y_wR == y_w_start)) return -1; if(fbCheckPt(x_pR, y_pR)) { PathR.Add(x_pR, y_pR); p_lenR++; break; } }; } } };
	}
   if(isLeft)
   {
      {};
      PathR.Clear();
      return 0;
   }
   {};
   PathL.Clear();
 	return 1;
}
internal Point GetPath(int startX, int startY, int endX, int endY)
{
   ptFrom.x = startX;
   ptFrom.y = startY;
   ptTo.x = endX;
   ptTo.y = endY;
   WayPoints.Clear();
   if(!fCheckPt(startX, startY) || !fCheckPt(endX, endY)) return null;
   WayPoints.Add(startX, startY);
   if(LayLine(ptFrom, ptTo) != 0)
   {
      Point p0 = LinePath.First;
      if(p0 == null) return null;
      Point p1 = p0.Next;
      {};
      Point p2 = p1.Next;
      {};
      while(p0 != null)
      {
         {};
         {};
         int x0 = p0.x;
         int y0 = p0.y;
         int x1 = p1.x;
         int y1 = p1.y;
         int x2 = p2.x;
         int y2 = p2.y;
         if(!(fCheckPt(x0, y0) && !fCheckPt(x1, y1) && fCheckPt(x2, y2)))
            {};
         WayPoints.AddUnique(x0, y0);
         if(!fCheckPt(x0, y1)) x1 = x0;
    	   else y0 = y1;
         WayPoints.AddUnique(x0, y0);
         p0.x = x0;
         p0.y = y0;
         PathL.Clear();
         PathR.Clear();
         int LenR = 0, LenL = 0;
         int Res = GoAround(x0, y0, x1, y1);
         LenL = PathL.Len;
         LenR = PathR.Len;
         if(Res == -1)
         {
            while(p0 != null)
            {
               {};
               {};
               fbBClrPt(p2.x, p2.y);
               p0 = p2.Next;
               if(p0 == null) break;
               p1 = p0.Next;
               {};
               p2 = p1.Next;
               {};
            }
            LinePath.Clear();
            MainPath.Clear();
            WayPoints.Clear();
            PathL.Clear();
            PathR.Clear();
            return null;
         }
         if(LenR == 0 && LenL == 0)
            {};
         if(LenR != 0)
         {
            WayPoints.Last.Next = PathR.First;
            WayPoints.Last = PathR.Last;
            WayPoints.Len += PathR.Len;
            PathR.Null();
            PathL.Clear();
         }
         else
         {
            WayPoints.Last.Next = PathL.First;
            WayPoints.Last = PathL.Last;
            WayPoints.Len += PathL.Len;
            PathL.Null();
            PathR.Clear();
         }
         int x = WayPoints.Last.x;
         int y = WayPoints.Last.y;
         while(p0 != null)
         {
            {};
            {};
            int _x = p2.x;
            int _y = p2.y;
            fbBClrPt(_x, _y);
            if(_x == x && _y == y) break;
            p0 = p2.Next;
            if(p0 == null) break;
            p1 = p0.Next;
            {};
            p2 = p1.Next;
            {};
         }
         if(p0 == null) break;
         {};
         {};
         p0 = p2.Next;
         if(p0 == null) break;
         p1 = p0.Next;
         {};
         p2 = p1.Next;
         {};
      }
   }
   WayPoints.AddUnique(ptTo.x, ptTo.y);
   if(isOptimize && WayPoints.Len > 2)
   {
      VectorsOptimize1();
   }
   MainPath.Clear();
   Point wp = WayPoints.First;
   while(wp != null && wp.Next != null)
   {
      MakeLine(wp, wp.Next, MainPath);
      wp = wp.Next;
   }
   return MainPath.First;
}
internal void Bending(int val)
{
   Point p0 = WayPoints.First;
   if(p0 == null) return;
   Point p1 = p0.Next;
   if(p1 == null) return;
   Point p2 = p1.Next;
   while(p0 != null && p1 != null && p2 != null)
   {
      int ax = p0.x - p1.x;
      int ay = p0.y - p1.y;
      int da = Norma(ax, ay);
		if(da == 0) da = 1;
      ax /= da;
      ay /= da;
      int bx = p2.x - p1.x;
      int by = p2.y - p1.y;
      int db = Norma(bx, by);
		if(db == 0) db = 1;
      bx /= db;
      by /= db;
		int nx = -ax - bx;
      int ny = -ay - by;
      int dn = Norma(nx, ny);
      if(dn == 0) dn = 1;
      nx /= dn;
      ny /= dn;
      Point pt = new Point();
      pt.x = p1.x + val*nx;
      pt.y = p1.y + val*ny;
      Path Path = new Path(pool);
      MakeLine(pt, p1, Path);
      Point p = Path.First;
      while(p != null)
      {
         if(fCheckPt(p.x, p.y) && CheckLine(p, p0) && CheckLine(p, p2))
            break;
         p = p.Next;
      }
      if(p != null)
      {
         p1.x = p.x;
         p1.y = p.y;
      }
      Path.Clear();
      p0 = p1;
      p1 = p0.Next;
      p2 = p1.Next;
   }
}

    }
}


// ============================================================================
// MERGED FROM: C2TopologyCoreV401LikeOriginal.cs
// ============================================================================
// C2TopologyCoreV401LikeOriginal.cs
// V401B_TOPOLOGY_CORE
// Source port: COSSACKS2/TopoGraf.cpp + TopoGraf.h + HashTop.cpp + HashTop.h
// and COSSACKS2/Brigade.cpp::GetTopology.
//
// Port scope: CII 1.1 land topology (TopType=0) without road-network/gate-specific
// dependencies (PreCreateTopLinks/GetNRoads*/CreateNationDependentLinksForGates).
// Those branches are not faked. The only Unity adaptation in the land core is the
// MotionField CheckPt/CheckBar/BSetPt backend. Initial CreateAreas follows the original
// SetClearBuildigsLock(0)->CreateAreas()->SetClearBuildigsLock(1) contract.


namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode
    {
        internal bool C2TopologyTryGetMapGeometryV401LikeOriginal(
            out int addsh,
            out int mapSx,
            out int mapSy,
            out string sourcePath)
        {
            addsh = 1;
            mapSx = 0;
            mapSy = 0;
            sourcePath = string.Empty;
            if (_map == null)
                return false;

            addsh = Mathf.Clamp(_map.Addsh, 1, 3);
            mapSx = _map.MAPSX;
            mapSy = _map.MAPSY;
            sourcePath = _map.SourcePath ?? string.Empty;
            return mapSx > 0 && mapSy > 0;
        }

        // Movement-owned primitive view of Factures3D.cpp::RN.  The parsed road structs
        // stay private terrain data; topology/GoOnRoad consume only the original fields.
        internal int C2MovementGetRoadCountV425LikeOriginal()
        {
            return _map != null && _map.HasRoadNet && _map.RoadKnots != null ? _map.RoadKnots.Length : 0;
        }

        internal bool C2MovementTryGetRoadKnotV425LikeOriginal(
            int index, out int x, out int y, out int nLinks)
        {
            x = y = nLinks = 0;
            if (_map == null || _map.RoadKnots == null || index < 0 || index >= _map.RoadKnots.Length)
                return false;
            ParsedRoadNetKnotLikeOriginal k = _map.RoadKnots[index];
            x = k.X; y = k.Y; nLinks = k.NLinks;
            return true;
        }

        internal bool C2MovementTryGetRoadLinkV425LikeOriginal(
            int knotIndex, int linkIndex, out int next, out int linkType, out int roadId, out int wayPointIndex)
        {
            next = linkType = roadId = wayPointIndex = -1;
            if (_map == null || _map.RoadKnots == null || knotIndex < 0 || knotIndex >= _map.RoadKnots.Length)
                return false;
            ParsedRoadNetKnotLikeOriginal k = _map.RoadKnots[knotIndex];
            if (linkIndex < 0 || linkIndex >= k.NLinks || linkIndex >= C2RoadMaxLinksLikeOriginal ||
                k.Links == null || linkIndex >= k.Links.Length)
                return false;
            next = k.Links[linkIndex];
            linkType = k.LinkType != null && linkIndex < k.LinkType.Length ? k.LinkType[linkIndex] : 0;
            roadId = k.RoadID != null && linkIndex < k.RoadID.Length ? k.RoadID[linkIndex] : -1;
            wayPointIndex = k.WayPointToPointIndex != null && linkIndex < k.WayPointToPointIndex.Length
                ? k.WayPointToPointIndex[linkIndex] : -1;
            return next >= 0 && next < _map.RoadKnots.Length;
        }

        private object _movementRoadPhysicsFsV433;
        private List<RoadDescLikeOriginal> _movementRoadPhysicsV433;

        internal void C2MovementGetRoadPhysicsV425LikeOriginal(
            int linkType, out int width, out int speed, out int tiring, out int priory)
        {
            width = 96; speed = 256 + 64; tiring = -64; priory = 256 + 400;
            object fs = _bootstrap != null ? _bootstrap.Fs : null;
            if (_movementRoadPhysicsV433 == null || !ReferenceEquals(fs, _movementRoadPhysicsFsV433))
            {
                _movementRoadPhysicsFsV433 = fs;
                _movementRoadPhysicsV433 = LoadRoadDescsLikeOriginal();
            }
            List<RoadDescLikeOriginal> roads = _movementRoadPhysicsV433;
            if (roads == null || linkType < 0 || linkType >= roads.Count || roads[linkType] == null) return;
            RoadDescLikeOriginal d = roads[linkType];
            width = d.RWidth; speed = d.Speed; tiring = d.Tiring; priory = Mathf.Max(1, d.Priory);
        }

        internal bool C2MovementTryGetRoadZonePhysicsV426LikeOriginal(
            int zone, out int speed, out int tiring, out int priory)
        {
            // Factures3D.cpp::RoadsNet::SetupNetParams. NetParams are the
            // arithmetic mean of all road descriptors connected to this knot.
            speed = tiring = priory = 256;
            if (_map == null || _map.RoadKnots == null || zone < 0 || zone >= _map.RoadKnots.Length)
                return false;

            ParsedRoadNetKnotLikeOriginal knot = _map.RoadKnots[zone];
            int nLinks = Mathf.Min(knot.NLinks, C2RoadMaxLinksLikeOriginal);
            if (nLinks <= 0) return true;

            long ss = 0;
            long st = 0;
            long sp = 0;
            int counted = 0;
            for (int i = 0; i < nLinks; i++)
            {
                if (knot.LinkType == null || i >= knot.LinkType.Length) continue;
                int width;
                int ls;
                int lt;
                int lp;
                C2MovementGetRoadPhysicsV425LikeOriginal(knot.LinkType[i], out width, out ls, out lt, out lp);
                ss += ls;
                st += lt;
                sp += lp;
                counted++;
            }
            if (counted <= 0) return true;
            speed = (int)(ss / counted);
            tiring = (int)(st / counted);
            priory = (int)(sp / counted);
            return true;
        }

        internal bool C2MovementHasDirectRoadLinkV426LikeOriginal(int from, int to)
        {
            // Factures3D.cpp::RoadsNet::GetNextWayPoints(From,Dest,NULL):
            // with a null output buffer it is used purely as a direct-edge test.
            if (_map == null || _map.RoadKnots == null || from < 0 || to < 0 ||
                from >= _map.RoadKnots.Length || to >= _map.RoadKnots.Length)
                return false;
            ParsedRoadNetKnotLikeOriginal k = _map.RoadKnots[from];
            int nLinks = Mathf.Min(k.NLinks, C2RoadMaxLinksLikeOriginal);
            for (int i = 0; i < nLinks; i++)
                if (k.Links != null && i < k.Links.Length && k.Links[i] == to)
                    return true;
            return false;
        }

        internal bool C2MovementTryGetRoadEdgeWaypointsV425LikeOriginal(int from, int to, out Vector2[] points)
        {
            points = null;
            if (_map == null || _map.RoadKnots == null || from < 0 || to < 0 ||
                from >= _map.RoadKnots.Length || to >= _map.RoadKnots.Length) return false;
            ParsedRoadNetKnotLikeOriginal k = _map.RoadKnots[from];
            bool linked = false;
            for (int i = 0; i < k.NLinks && i < C2RoadMaxLinksLikeOriginal; i++)
                if (k.Links != null && i < k.Links.Length && k.Links[i] == to) { linked = true; break; }
            if (!linked) return false;
            List<Vector2> outPts = new List<Vector2>(32);
            AppendRoadEdgeWaypointsV352LikeOriginal(_map.RoadKnots, from, to, outPts);
            if (outPts.Count == 0) return false;
            points = outPts.ToArray();
            return true;
        }

        // Factures3D.cpp::OneNetWayPointToPoint physical edge object.  The saved
        // WayPointToPointIndex identifies the same undirected road edge from both knots.
        // We recover the constructor orientation by taking the first occurrence in the
        // same ascending knot/link order used by RoadsNet::CreateWayPointToPoint.
        internal bool C2MovementTryBuildRoadEdgeGeometryV426LikeOriginal(
            int from, int to,
            out int edgeKey, out int startKnot, out int endKnot,
            out Vector2[] basePointsReal, out byte[] baseDirections)
        {
            edgeKey = -1; startKnot = endKnot = -1;
            basePointsReal = null; baseDirections = null;
            if (_map == null || _map.RoadKnots == null ||
                from < 0 || to < 0 || from >= _map.RoadKnots.Length || to >= _map.RoadKnots.Length)
                return false;

            int wp = -1;
            bool linked = false;
            ParsedRoadNetKnotLikeOriginal fk = _map.RoadKnots[from];
            for (int j = 0; j < fk.NLinks && j < C2RoadMaxLinksLikeOriginal; j++)
            {
                if (fk.Links == null || j >= fk.Links.Length || fk.Links[j] != to) continue;
                linked = true;
                if (fk.WayPointToPointIndex != null && j < fk.WayPointToPointIndex.Length &&
                    fk.WayPointToPointIndex[j] != 0xFFFF)
                    wp = fk.WayPointToPointIndex[j];
                break;
            }
            if (!linked) return false;

            if (wp >= 0)
            {
                for (int i = 0; i < _map.RoadKnots.Length && startKnot < 0; i++)
                {
                    ParsedRoadNetKnotLikeOriginal k = _map.RoadKnots[i];
                    if (k.Hidden != 0) continue;
                    for (int j = 0; j < k.NLinks && j < C2RoadMaxLinksLikeOriginal; j++)
                    {
                        if (k.Links == null || j >= k.Links.Length ||
                            k.WayPointToPointIndex == null || j >= k.WayPointToPointIndex.Length) continue;
                        if (k.WayPointToPointIndex[j] != wp) continue;
                        int n = k.Links[j];
                        if (n < 0 || n >= _map.RoadKnots.Length || _map.RoadKnots[n].Hidden != 0) continue;
                        startKnot = i; endKnot = n;
                        break;
                    }
                }
            }
            if (startKnot < 0)
            {
                startKnot = Math.Min(from, to);
                endKnot = Math.Max(from, to);
            }

            edgeKey = wp >= 0 ? wp : unchecked((startKnot * 65537) ^ endKnot ^ 0x40000000);
            List<Vector2> p = new List<Vector2>(64);
            AppendRoadEdgeWaypointsV352LikeOriginal(_map.RoadKnots, startKnot, endKnot, p);
            if (p.Count == 0) return false;
            basePointsReal = p.ToArray();
            baseDirections = new byte[p.Count];
            if (p.Count >= 3)
            {
                for (int i = 0; i < p.Count - 2; i++)
                    baseDirections[i] = C2OriginalMovementMathV352.GetDir(
                        Mathf.RoundToInt((p[i + 2].x - p[i].x) / 16.0f),
                        Mathf.RoundToInt((p[i + 2].y - p[i].y) / 16.0f));
                baseDirections[p.Count - 2] = baseDirections[p.Count - 3];
                baseDirections[p.Count - 1] = baseDirections[p.Count - 3];
            }
            else
            {
                byte d = C2OriginalMovementMathV352.GetDir(
                    _map.RoadKnots[endKnot].X - _map.RoadKnots[startKnot].X,
                    _map.RoadKnots[endKnot].Y - _map.RoadKnots[startKnot].Y);
                for (int i = 0; i < baseDirections.Length; i++) baseDirections[i] = d;
            }
            return true;
        }
    }

    internal sealed class C2TopologyBootstrapV401LikeOriginal : MonoBehaviour
    {
        private int _lastModeInstanceId;
        private string _lastSourcePath = string.Empty;
        private float _retryAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallV401LikeOriginal()
        {
            const string rootName = "C2_TOPOLOGY_CORE_V401";
            GameObject existing = GameObject.Find(rootName);
            if (existing != null && existing.GetComponent<C2TopologyBootstrapV401LikeOriginal>() != null)
                return;

            GameObject go = new GameObject(rootName);
            go.hideFlags = HideFlags.DontSave;
            DontDestroyOnLoad(go);
            go.AddComponent<C2TopologyBootstrapV401LikeOriginal>();
        }

        private void Update()
        {
            if (Time.unscaledTime < _retryAt)
                return;

            C2BattleTerrainMode mode = UnityEngine.Object.FindFirstObjectByType<C2BattleTerrainMode>(FindObjectsInactive.Exclude);
            if (mode == null)
                return;

            int addsh;
            int mapSx;
            int mapSy;
            string sourcePath;
            if (!mode.C2TopologyTryGetMapGeometryV401LikeOriginal(out addsh, out mapSx, out mapSy, out sourcePath))
                return;

            int modeId = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(mode);
            if (C2TopologyCoreV401LikeOriginal.ReadyLikeOriginal &&
                modeId == _lastModeInstanceId &&
                string.Equals(sourcePath, _lastSourcePath, StringComparison.OrdinalIgnoreCase))
                return;

            _retryAt = Time.unscaledTime + 1.0f;
            if (C2TopologyCoreV401LikeOriginal.RebuildFromCurrentMapV401LikeOriginal(mode))
            {
                _lastModeInstanceId = modeId;
                _lastSourcePath = sourcePath;
            }
        }
    }

    public static class C2TopologyCoreV401LikeOriginal
    {
        private const ushort TopFreeUnassignedV401 = 0xFFFE;
        private const ushort TopBlockedV401 = 0xFFFF;
        private const int RRadV401 = 127;
        private const int NKey0V401 = 1024;
        private const int NKey1V401 = 40;
        private const int NKeyV401 = NKey0V401 * NKey1V401;
        private const int MXZV401 = 16384;
        private const int SupportedLandTopTypeV401 = 0;
        private const float MotionCellRealSizeV401 = 256.0f;
        private const float MotionCellRealHalfV401 = 128.0f;

        private sealed class RadioV401
        {
            public sbyte[] Xi = Array.Empty<sbyte>();
            public sbyte[] Yi = Array.Empty<sbyte>();
            public int N;
        }

        public struct OneLinkInfoV401LikeOriginal
        {
            public ushort NextAreaID;
            public ushort NextAreaDist;
            public uint LinkNatMask;
        }

        public struct StrategyInfoV401LikeOriginal
        {
            public ushort BuildInfo;
            public byte NPeasants;
            public byte NShortRange;
            public byte NLongRange;
            public byte NMortir;
            public byte NTowers;
            public byte NPushek;
        }

        // TopoGraf.h::Area. C++ pointer-owned buffers become managed containers only.
        public sealed class AreaV401LikeOriginal
        {
            public short x;
            public short y;
            public byte Importance;
            public byte NTrees;
            public byte NStones;
            public ushort NMines;
            public ushort[] MinesIdx = Array.Empty<ushort>();
            public ushort MaxLink = 6;
            public readonly StrategyInfoV401LikeOriginal[] SINF = new StrategyInfoV401LikeOriginal[8];
            public readonly List<OneLinkInfoV401LikeOriginal> Link = new List<OneLinkInfoV401LikeOriginal>(6);
            public int NLinks { get { return Link.Count; } }
        }

        private sealed class HashTopV401
        {
            // CII bitfield widths are preserved when values are assigned.
            public ushort K1 = 0xFFFF;
            public ushort ML;
            public ushort LD;
            public byte Prio;
            public byte NI;

            public void ClearV401LikeOriginal()
            {
                K1 = 0xFFFF;
            }
        }

        private sealed class HashTopTableV401
        {
            public byte TopType;
            public ushort[] TopRef = Array.Empty<ushort>();
            public readonly List<AreaV401LikeOriginal> TopMap = new List<AreaV401LikeOriginal>(256);
            public readonly HashTopV401[] Key1 = new HashTopV401[NKeyV401];

            public int NAreas { get { return TopMap.Count; } }

            public HashTopTableV401()
            {
                for (int i = 0; i < Key1.Length; i++)
                    Key1[i] = new HashTopV401();
            }

            public void SetUpV401LikeOriginal(byte lockType, int topCellCount)
            {
                TopType = lockType;
                TopRef = new ushort[Mathf.Max(0, topCellCount)];
                ClearV401LikeOriginal();
            }

            public void EraseAreasV401LikeOriginal()
            {
                TopMap.Clear();
            }

            public void ClearV401LikeOriginal()
            {
                for (int i = 0; i < Key1.Length; i++)
                    Key1[i].ClearV401LikeOriginal();
            }

            public bool AddAreaV401LikeOriginal(short x, short y, byte sliv)
            {
                int minNorm = 1; // CII AddArea switch(ADDSH): 1 for 1/2/3.
                if (sliv != 2)
                {
                    for (int i = 0; i < TopMap.Count; i++)
                    {
                        AreaV401LikeOriginal ar = TopMap[i];
                        if (NormaV401LikeOriginal(ar.x - x, ar.y - y) <= minNorm)
                        {
                            if (sliv != 0)
                            {
                                ar.x = (short)((ar.x + x) >> 1);
                                ar.y = (short)((ar.y + y) >> 1);
                            }
                            return false;
                        }
                    }
                }

                AreaV401LikeOriginal area = new AreaV401LikeOriginal();
                area.x = x;
                area.y = y;
                TopMap.Add(area);
                return true;
            }

            public void AddLinkV401LikeOriginal(int n1, int n2, uint mask)
            {
                if (n1 < 0 || n2 < 0 || n1 >= NAreas || n2 >= NAreas)
                    return;

                // HashTop.cpp::HashTopTable::AddLink.  Two road topology zones may
                // only be neighbours when RoadsNet itself contains that direct edge.
                // Without this guard CreateAreas/ReCreateAreas can invent geometric
                // road-to-road shortcuts which never exist in the original engine.
                int nr = GetNRoadsV401LikeOriginal();
                if (n1 < nr && n2 < nr &&
                    (s_modeV401 == null || !s_modeV401.C2MovementHasDirectRoadLinkV426LikeOriginal(n1, n2)))
                    return;

                AreaV401LikeOriginal ar = TopMap[n1];
                for (int i = 0; i < ar.Link.Count; i++)
                {
                    OneLinkInfoV401LikeOriginal link = ar.Link[i];
                    if (link.NextAreaID == n2)
                    {
                        link.LinkNatMask |= mask;
                        ar.Link[i] = link;
                        return;
                    }
                }

                AreaV401LikeOriginal a2 = TopMap[n2];
                int sc = GetLinkScaleV401LikeOriginal(n1, n2, TopType);
                int d = (NormaV401LikeOriginal(ar.x - a2.x, ar.y - a2.y) * sc) >> 4;
                if (d <= 0) d = 1;

                OneLinkInfoV401LikeOriginal nl = new OneLinkInfoV401LikeOriginal();
                nl.NextAreaID = unchecked((ushort)n2);
                nl.NextAreaDist = unchecked((ushort)d);
                nl.LinkNatMask = mask;
                ar.Link.Add(nl);
            }

            public void CreateAreasV401LikeOriginal()
            {
                EraseAreasV401LikeOriginal();
                if (TopRef == null || TopRef.Length != s_topLxV401 * s_topLyV401)
                    TopRef = new ushort[s_topLxV401 * s_topLyV401];

                for (int x = 0; x < s_topLxV401; x++)
                {
                    for (int y = 0; y < s_topLyV401; y++)
                    {
                        int ofs = x + (y << s_topSHV401);
                        TopRef[ofs] = GetTCStatusV401LikeOriginal(x, y, TopType)
                            ? TopFreeUnassignedV401
                            : TopBlockedV401;
                    }
                }

                // HashTop.cpp::_USE3D -> PreCreateTopLinks(TopType): road knots are
                // the first topology areas (except water TopType==1).
                PreCreateTopLinksV401LikeOriginal();

                int hexSize = 64 * 5;
                int max = 16384 << (s_addshV401 - 1);
                int nmax = max / hexSize;
                for (int i = 0; i < nmax; i++)
                {
                    if ((i & 1) != 0)
                    {
                        for (int j = 0; j < nmax - 1; j++)
                            TryAddHexAreaV401LikeOriginal(hexSize + j * hexSize, hexSize / 2 + i * hexSize);
                    }
                    else
                    {
                        for (int j = 0; j < nmax; j++)
                            TryAddHexAreaV401LikeOriginal(hexSize / 2 + j * hexSize, hexSize / 2 + i * hexSize);
                    }
                }

                ReCreateAreasV401LikeOriginal(0, 0, 10000, 10000);
            }

            private void PreCreateTopLinksV401LikeOriginal()
            {
                // Factures3D.cpp::PreCreateTopLinks. Water topology does not seed roads.
                if (TopType == 1 || s_modeV401 == null) return;
                int nr = s_modeV401.C2MovementGetRoadCountV425LikeOriginal();
                for (int i = 0; i < nr; i++)
                {
                    int x, y, nl;
                    if (!s_modeV401.C2MovementTryGetRoadKnotV425LikeOriginal(i, out x, out y, out nl)) continue;
                    if (nl == 2)
                    {
                        int z1, lt1, rid1, wp1;
                        int z2, lt2, rid2, wp2;
                        int x1, y1, n1links, x2, y2, n2links;
                        if (s_modeV401.C2MovementTryGetRoadLinkV425LikeOriginal(i, 0, out z1, out lt1, out rid1, out wp1) &&
                            s_modeV401.C2MovementTryGetRoadLinkV425LikeOriginal(i, 1, out z2, out lt2, out rid2, out wp2) &&
                            s_modeV401.C2MovementTryGetRoadKnotV425LikeOriginal(z1, out x1, out y1, out n1links) &&
                            s_modeV401.C2MovementTryGetRoadKnotV425LikeOriginal(z2, out x2, out y2, out n2links))
                        {
                            int width, speed, tiring, priory;
                            s_modeV401.C2MovementGetRoadPhysicsV425LikeOriginal(lt1, out width, out speed, out tiring, out priory);
                            int w = width * 2;
                            int n1 = NormaV401LikeOriginal(x1 - x, y1 - y); if (n1 == 0) n1 = 1;
                            int n2 = NormaV401LikeOriginal(x2 - x, y2 - y); if (n2 == 0) n2 = 1;
                            x1 = x + ((x1 - x) * w) / n1;
                            y1 = y + ((y1 - y) * w) / n1;
                            x2 = x + ((x2 - x) * w) / n2;
                            y2 = y + ((y2 - y) * w) / n2;
                            AddAreaV401LikeOriginal((short)(((x1 + x2 + x + x + x + x) / 6) >> 6),
                                                       (short)(((y1 + y2 + y + y + y + y) / 6) >> 6), 2);
                            continue;
                        }
                    }
                    AddAreaV401LikeOriginal((short)(x >> 6), (short)(y >> 6), 2);
                }
            }

            private int GetNRoadsV401LikeOriginal()
            {
                return s_modeV401 != null ? s_modeV401.C2MovementGetRoadCountV425LikeOriginal() : 0;
            }

            private int GetLinkScaleV401LikeOriginal(int n1, int n2, int type)
            {
                // Factures3D.cpp::GetLinkScale.
                if (type != 0 && type != 2) return 16;
                int nr = GetNRoadsV401LikeOriginal();
                if (n1 < nr && n2 < nr && s_modeV401 != null)
                {
                    int x, y, nl;
                    if (s_modeV401.C2MovementTryGetRoadKnotV425LikeOriginal(n1, out x, out y, out nl))
                    {
                        for (int q = 0; q < nl; q++)
                        {
                            int next, lt, roadId, wp;
                            if (!s_modeV401.C2MovementTryGetRoadLinkV425LikeOriginal(n1, q, out next, out lt, out roadId, out wp) || next != n2) continue;
                            int width, speed, tiring, priory;
                            s_modeV401.C2MovementGetRoadPhysicsV425LikeOriginal(lt, out width, out speed, out tiring, out priory);
                            int scale = (16 * 256) / Mathf.Max(1, priory);
                            return Mathf.Max(1, scale);
                        }
                    }
                }
                return 16;
            }

            private void TryAddHexAreaV401LikeOriginal(int xx, int yy)
            {
                int x = xx >> 6;
                int y = yy >> 6;
                if (!(x > 1 && y > 1 && x < s_topLxV401 - 4 && y < s_topLxV401 - 4))
                    return;

                bool empty = true;
                for (int dx = -1; dx <= 1 && empty; dx++)
                {
                    for (int dy = -1; dy <= 1 && empty; dy++)
                    {
                        if (TopRef[(x + dx) + (y + dy) * s_topLxV401] != TopFreeUnassignedV401)
                            empty = false;
                    }
                }

                if (empty && !CheckBarV401LikeOriginal((x << 2) - 4, (y << 2) - 4, 12, 12, TopType))
                    AddAreaV401LikeOriginal((short)x, (short)y, 2);
            }

            public void ReCreateAreasV401LikeOriginal(int x0, int y0, int x1, int y1)
            {
                if (x0 < 0) x0 = 0;
                if (y0 < 0) y0 = 0;
                if (x1 >= s_topLxV401) x1 = s_topLxV401 - 1;
                if (y1 >= s_topLyV401) y1 = s_topLyV401 - 1;

                int[] linksList = new int[4096];
                int nfLinks = 0;

                for (int ix = x0; ix <= x1; ix++)
                {
                    for (int iy = y0; iy <= y1; iy++)
                    {
                        int ofs = ix + (iy << s_topSHV401);
                        TopRef[ofs] = GetTCStatusV401LikeOriginal(ix, iy, TopType)
                            ? TopFreeUnassignedV401
                            : TopBlockedV401;
                    }
                }

                x0 -= 10;
                y0 -= 10;
                x1 += 10;
                y1 += 10;
                if (x0 < 0) x0 = 0;
                if (y0 < 0) y0 = 0;
                if (x1 >= s_topLxV401) x1 = s_topLxV401 - 1;
                if (y1 >= s_topLyV401) y1 = s_topLyV401 - 1;

                for (int i = 0; i < NAreas; i++)
                {
                    AreaV401LikeOriginal ar = TopMap[i];
                    if (ar.x >= x0 && ar.y >= y0 && ar.x <= x1 && ar.y <= y1)
                    {
                        ar.Link.Clear();
                        if (nfLinks < linksList.Length)
                            linksList[nfLinks++] = i;
                    }
                }

                int mmx = s_topLxV401 - 1;
                int mmy = s_topLxV401 - 1; // CII uses TopLx here as well.
                int nr = GetNRoadsV401LikeOriginal();

                for (int i = 0; i < NAreas; i++)
                {
                    AreaV401LikeOriginal ar = TopMap[i];
                    if (GetTCStatusV401LikeOriginal(ar.x, ar.y, TopType) || i < nr)
                        TopRef[ar.x + ar.y * s_topLxV401] = unchecked((ushort)i);
                }

                if (TopType == 0 && s_modeV401 != null)
                {
                    for (int i = 0; i < nr && i < NAreas; i++)
                    {
                        int rx, ry, nl;
                        if (!s_modeV401.C2MovementTryGetRoadKnotV425LikeOriginal(i, out rx, out ry, out nl)) continue;
                        for (int j = 0; j < nl; j++)
                        {
                            int next, lt, roadId, wp;
                            if (!s_modeV401.C2MovementTryGetRoadLinkV425LikeOriginal(i, j, out next, out lt, out roadId, out wp)) continue;
                            if (next < 0 || next >= NAreas) continue;
                            AddLinkV401LikeOriginal(i, next, uint.MaxValue);
                            AddLinkV401LikeOriginal(next, i, uint.MaxValue);
                        }
                    }
                }

                for (int ix = x0; ix <= x1; ix++)
                    for (int iy = y0; iy < y1; iy++)
                        MakeCellSingledV401LikeOriginal(ix, iy, TopType);

                byte[] temp = new byte[s_topLxV401 * s_topLyV401];
                for (int ix = x0; ix <= x1; ix++)
                {
                    for (int iy = y0; iy < y1; iy++)
                    {
                        int ofs = ix + (iy << s_topSHV401);
                        temp[ofs] = GetCellLinksV401LikeOriginal(ix, iy, TopType);
                    }
                }

                bool change = true;
                for (int i = 1; i < 8 && change; i++)
                {
                    change = false;
                    RadioV401 radio = s_rarrV401[i];
                    int n = radio.N;
                    for (int k = 0; k < n; k++)
                    {
                        for (int q = 0; q < nfLinks; q++)
                        {
                            int j = linksList[q];
                            if (i < 3 || j >= nr || TopType != 0)
                            {
                                AreaV401LikeOriginal ar = TopMap[j];
                                int x = ar.x + radio.Xi[k];
                                int y = ar.y + radio.Yi[k];
                                if (x > 0 && y > 0 && x < mmx && y < mmy)
                                {
                                    int ofs = x + (y << s_topSHV401);
                                    byte t = temp[ofs];
                                    int tc = TopRef[ofs];
                                    int tl = TopRef[ofs - 1];
                                    int tr = TopRef[ofs + 1];
                                    int tu = TopRef[ofs - s_topLxV401];
                                    int td = TopRef[ofs + s_topLxV401];

                                    if (t != 0 && tc >= TopFreeUnassignedV401)
                                    {
                                        if ((t & 1) != 0 && tl == j) { TopRef[ofs] = unchecked((ushort)tl); tc = tl; change = true; }
                                        else if ((t & 2) != 0 && tr == j) { TopRef[ofs] = unchecked((ushort)tr); tc = tr; change = true; }
                                        else if ((t & 4) != 0 && tu == j) { TopRef[ofs] = unchecked((ushort)tu); tc = tu; change = true; }
                                        else if ((t & 8) != 0 && td == j) { TopRef[ofs] = unchecked((ushort)td); tc = td; change = true; }
                                    }

                                    if (tc < TopFreeUnassignedV401)
                                    {
                                        if ((t & 1) != 0 && tc != tl && tl < TopFreeUnassignedV401) { AddLinkV401LikeOriginal(tc, tl, uint.MaxValue); AddLinkV401LikeOriginal(tl, tc, uint.MaxValue); }
                                        if ((t & 2) != 0 && tc != tr && tr < TopFreeUnassignedV401) { AddLinkV401LikeOriginal(tc, tr, uint.MaxValue); AddLinkV401LikeOriginal(tr, tc, uint.MaxValue); }
                                        if ((t & 4) != 0 && tc != tu && tu < TopFreeUnassignedV401) { AddLinkV401LikeOriginal(tc, tu, uint.MaxValue); AddLinkV401LikeOriginal(tu, tc, uint.MaxValue); }
                                        if ((t & 8) != 0 && tc != td && td < TopFreeUnassignedV401) { AddLinkV401LikeOriginal(tc, td, uint.MaxValue); AddLinkV401LikeOriginal(td, tc, uint.MaxValue); }
                                    }
                                }
                            }
                        }
                    }
                }

                change = true;
                for (int i = 0; i < 8 && change; i++)
                {
                    change = false;
                    for (int ix = x0; ix <= x1; ix++)
                    {
                        for (int iy = y0; iy <= y1; iy++)
                        {
                            if (ix > 0 && iy > 0 && ix < mmx && iy < mmy)
                            {
                                int ofs = ix + (iy << s_topSHV401);
                                byte t = temp[ofs];
                                int tc = TopRef[ofs];
                                int tl = TopRef[ofs - 1];
                                int tr = TopRef[ofs + 1];
                                int tu = TopRef[ofs - s_topLxV401];
                                int td = TopRef[ofs + s_topLxV401];

                                if (t != 0 && tc >= TopFreeUnassignedV401)
                                {
                                    if ((t & 1) != 0 && tl < TopFreeUnassignedV401) { TopRef[ofs] = unchecked((ushort)tl); tc = tl; change = true; }
                                    else if ((t & 2) != 0 && tr < TopFreeUnassignedV401) { TopRef[ofs] = unchecked((ushort)tr); tc = tr; change = true; }
                                    else if ((t & 4) != 0 && tu < TopFreeUnassignedV401) { TopRef[ofs] = unchecked((ushort)tu); tc = tu; change = true; }
                                    else if ((t & 8) != 0 && td < TopFreeUnassignedV401) { TopRef[ofs] = unchecked((ushort)td); tc = td; change = true; }

                                    if (tc < TopFreeUnassignedV401)
                                    {
                                        if ((t & 1) != 0 && tc != tl && tl < TopFreeUnassignedV401) { AddLinkV401LikeOriginal(tc, tl, uint.MaxValue); AddLinkV401LikeOriginal(tl, tc, uint.MaxValue); }
                                        if ((t & 2) != 0 && tc != tr && tr < TopFreeUnassignedV401) { AddLinkV401LikeOriginal(tc, tr, uint.MaxValue); AddLinkV401LikeOriginal(tr, tc, uint.MaxValue); }
                                        if ((t & 4) != 0 && tc != tu && tu < TopFreeUnassignedV401) { AddLinkV401LikeOriginal(tc, tu, uint.MaxValue); AddLinkV401LikeOriginal(tu, tc, uint.MaxValue); }
                                        if ((t & 8) != 0 && tc != td && td < TopFreeUnassignedV401) { AddLinkV401LikeOriginal(tc, td, uint.MaxValue); AddLinkV401LikeOriginal(td, tc, uint.MaxValue); }
                                    }
                                }
                            }
                        }
                    }
                }

                // Original next calls CreateNationDependentLinksForGates(); omitted in V401.
                ClearV401LikeOriginal();
            }

            public bool CalculateWayV401LikeOriginal(int ofs, HashTopV401 ht, uint mask)
            {
                s_nwptsV401 = 0;
                int nAreas = NAreas;
                if (nAreas <= 0 || nAreas > MXZV401)
                    return false;

                int start = ofs / nAreas;
                int fin = ofs % nAreas;
                if (start < 0 || fin < 0 || start >= nAreas || fin >= nAreas)
                    return false;

                ushort[] precisePointWeight = new ushort[MXZV401];
                ushort[] precisePointPrevIndex = new ushort[MXZV401];
                ushort[] candidatPointWeight = new ushort[MXZV401];
                ushort[] candidatPointPrevIndex = new ushort[MXZV401];
                ushort[] candidatList = new ushort[MXZV401];
                for (int i = 0; i < nAreas; i++)
                {
                    precisePointWeight[i] = 0xFFFF;
                    precisePointPrevIndex[i] = 0xFFFF;
                    candidatPointWeight[i] = 0xFFFF;
                    candidatPointPrevIndex[i] = 0xFFFF;
                }

                candidatList[0] = unchecked((ushort)fin);
                candidatPointWeight[fin] = 0;
                candidatPointPrevIndex[fin] = 0xFFFF;
                int nCandidates = 1;

                do
                {
                    nCandidates--;
                    int tz = candidatList[nCandidates];
                    precisePointWeight[tz] = candidatPointWeight[tz];
                    candidatPointWeight[tz] = 0xFFFF;
                    precisePointPrevIndex[tz] = candidatPointPrevIndex[tz];

                    if (tz == start)
                    {
                        int t0 = tz;
                        s_fullWayV401[0] = unchecked((ushort)tz);
                        s_nwptsV401 = 1;
                        for (int q = 0; t0 != 0xFFFF; q++)
                        {
                            t0 = candidatPointPrevIndex[t0];
                            if (t0 != 0xFFFF && s_nwptsV401 < s_fullWayV401.Length)
                                s_fullWayV401[s_nwptsV401++] = unchecked((ushort)t0);
                        }

                        ht.LD = unchecked((ushort)(precisePointWeight[tz] & 8191));
                        ht.ML = unchecked((ushort)(precisePointPrevIndex[tz] & 8191));
                        ht.Prio = 0;
                        return true;
                    }

                    AreaV401LikeOriginal tar = TopMap[tz];
                    int w0 = precisePointWeight[tz];
                    for (int i = 0; i < tar.Link.Count; i++)
                    {
                        OneLinkInfoV401LikeOriginal link = tar.Link[i];
                        if ((link.LinkNatMask & mask) == 0)
                            continue;

                        int pi = link.NextAreaID;
                        if (pi < 0 || pi >= nAreas)
                            continue;

                        if (precisePointWeight[pi] == 0xFFFF)
                        {
                            int wiInt = link.NextAreaDist + w0;
                            ushort wi = unchecked((ushort)wiInt);
                            ushort wc = candidatPointWeight[pi];
                            bool add = true;

                            if (wc != 0xFFFF)
                            {
                                if (wc > wi)
                                {
                                    for (int j = 0; j < nCandidates; j++)
                                    {
                                        if (candidatList[j] == pi)
                                        {
                                            for (int m = j; m < nCandidates - 1; m++)
                                                candidatList[m] = candidatList[m + 1];
                                            nCandidates--;
                                            break;
                                        }
                                    }
                                    candidatPointWeight[pi] = wi;
                                }
                                else
                                {
                                    add = false;
                                }
                            }

                            if (add)
                            {
                                if (nCandidates >= MXZV401)
                                    return false;

                                if (nCandidates == 0)
                                {
                                    candidatList[0] = unchecked((ushort)pi);
                                    candidatPointWeight[pi] = wi;
                                    candidatPointPrevIndex[pi] = unchecked((ushort)tz);
                                    nCandidates++;
                                }
                                else
                                {
                                    int idxMax = 0;
                                    int idxMin = nCandidates - 1;
                                    ushort wcMax = candidatPointWeight[candidatList[idxMax]];
                                    ushort wcMin = candidatPointWeight[candidatList[idxMin]];

                                    if (wi <= wcMin)
                                    {
                                        candidatList[nCandidates] = unchecked((ushort)pi);
                                        candidatPointWeight[pi] = wi;
                                        candidatPointPrevIndex[pi] = unchecked((ushort)tz);
                                        nCandidates++;
                                    }
                                    else if (wi > wcMax)
                                    {
                                        for (int m = nCandidates; m > 0; m--)
                                            candidatList[m] = candidatList[m - 1];
                                        candidatList[0] = unchecked((ushort)pi);
                                        candidatPointWeight[pi] = wi;
                                        candidatPointPrevIndex[pi] = unchecked((ushort)tz);
                                        nCandidates++;
                                    }
                                    else
                                    {
                                        while (idxMax != idxMin - 1)
                                        {
                                            int idxMid = (idxMin + idxMax) >> 1;
                                            ushort wm = candidatPointWeight[candidatList[idxMid]];
                                            if (wm > wi)
                                            {
                                                wcMax = wm;
                                                idxMax = idxMid;
                                            }
                                            else
                                            {
                                                wcMin = wm;
                                                idxMin = idxMid;
                                            }
                                        }

                                        for (int m = nCandidates; m > idxMin; m--)
                                            candidatList[m] = candidatList[m - 1];
                                        candidatList[idxMin] = unchecked((ushort)pi);
                                        candidatPointWeight[pi] = wi;
                                        candidatPointPrevIndex[pi] = unchecked((ushort)tz);
                                        nCandidates++;
                                    }
                                }
                            }
                        }
                    }
                }
                while (nCandidates > 0);

                return false;
            }

            public HashTopV401 GetHashTopV401LikeOriginal(int ofs, byte ni)
            {
                if (ofs < 0 || NAreas <= 0)
                    return null;

                int key0 = ofs & 1023;
                int key1 = ofs >> 10;
                int startIndex = key0 * NKey1V401;
                int finalIndex = startIndex + NKey1V401;
                int curIndex = startIndex;

                for (int scan = startIndex; scan < finalIndex; scan++)
                {
                    HashTopV401 record = Key1[scan];
                    if (record.NI == (ni & 7))
                    {
                        if (record.K1 == unchecked((ushort)key1))
                        {
                            if (record.Prio < 7) record.Prio++;
                            return record;
                        }
                        if (record.K1 == 0xFFFF)
                        {
                            curIndex = scan;
                            Key1[curIndex].Prio = 0;
                        }
                        else if (Key1[curIndex].Prio > record.Prio)
                        {
                            curIndex = scan;
                        }
                    }
                }

                HashTopV401 cur = Key1[curIndex];
                if (CalculateWayV401LikeOriginal(ofs, cur, 1u << (ni & 31)))
                {
                    cur.K1 = unchecked((ushort)key1);
                    cur.Prio = 0;
                    cur.NI = (byte)(ni & 7);
                    return cur;
                }
                return null;
            }
        }

        private static readonly RadioV401[] s_rarrV401 = CreateEmptyRadioArrayV401LikeOriginal();
        private static readonly HashTopTableV401[] s_hashTablesV401 =
        {
            new HashTopTableV401(), new HashTopTableV401(), new HashTopTableV401(), new HashTopTableV401()
        };

        private static bool[][] s_motionBlockedV401 = { Array.Empty<bool>(), Array.Empty<bool>(), Array.Empty<bool>(), Array.Empty<bool>() };
        private static C2BattleTerrainMode s_modeV401;
        private static readonly ushort[] s_fullWayV401 = new ushort[128];
        private static int s_nwptsV401;
        private static bool s_buildingLocksEnabledForTopologyV401 = true;
        private static int s_motionSxV401;
        private static int s_motionSyV401;
        private static int s_addshV401 = 1;
        private static int s_topLxV401;
        private static int s_topLyV401;
        private static int s_topSHV401;
        private static bool s_radioReadyV401;
        private static bool s_readyV401;
        private static string s_sourcePathV401 = string.Empty;
        private static long s_lastBuildMsV401;

        public static bool ReadyLikeOriginal { get { return s_readyV401; } }
        public static int TopLxLikeOriginal { get { return s_topLxV401; } }
        public static int TopLyLikeOriginal { get { return s_topLyV401; } }
        public static int TopSHLikeOriginal { get { return s_topSHV401; } }
        public static string SourcePathLikeOriginal { get { return s_sourcePathV401; } }
        public static long LastBuildMillisecondsLikeOriginal { get { return s_lastBuildMsV401; } }
        public static int NWPTSLikeOriginal { get { return s_nwptsV401; } }
        public static ushort GetFULLWAYPointLikeOriginal(int index)
        {
            return index >= 0 && index < s_nwptsV401 ? s_fullWayV401[index] : TopBlockedV401;
        }

        private static RadioV401[] CreateEmptyRadioArrayV401LikeOriginal()
        {
            RadioV401[] result = new RadioV401[RRadV401];
            for (int i = 0; i < result.Length; i++)
                result[i] = new RadioV401();
            return result;
        }

        public static bool RebuildFromCurrentMapV401LikeOriginal(C2BattleTerrainMode mode)
        {
            if (mode == null)
                return false;

            int addsh;
            int mapSx;
            int mapSy;
            string sourcePath;
            if (!mode.C2TopologyTryGetMapGeometryV401LikeOriginal(out addsh, out mapSx, out mapSy, out sourcePath))
                return false;

            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                s_readyV401 = false;
                s_addshV401 = Mathf.Clamp(addsh, 1, 3);
                s_motionSxV401 = mapSx;
                s_motionSyV401 = mapSy;
                s_topLxV401 = mapSx >> 2;
                s_topLyV401 = mapSy >> 2;
                s_topSHV401 = 7 + s_addshV401;
                s_sourcePathV401 = sourcePath ?? string.Empty;
                s_modeV401 = mode;

                if (s_topLxV401 <= 0 || s_topLyV401 <= 0 || s_topLxV401 != (1 << s_topSHV401))
                {
                    UnityEngine.Debug.LogError("[C2:TOPOLOGY V401B] build_failed invalid_geometry addsh=" + s_addshV401 +
                                               " map=" + mapSx + "x" + mapSy +
                                               " top=" + s_topLxV401 + "x" + s_topLyV401 +
                                               " topSH=" + s_topSHV401);
                    return false;
                }

                CreateRadioV401LikeOriginal();

                // TopoGraf.cpp::CreateAreas(): SetClearBuildigsLock(0), build topology,
                // SetClearBuildigsLock(1). We mirror this only inside the topology snapshot;
                // the live Unity movement obstruction map is never globally modified.
                SetClearBuildigsLockV401LikeOriginal(false);
                BuildMotionFieldAdapterV401LikeOriginal();

                // TopoGraf.cpp::CreateAreas builds one HashTable for every MFIELDS entry.
                for (int i = 0; i < s_hashTablesV401.Length; i++)
                {
                    s_hashTablesV401[i].EraseAreasV401LikeOriginal();
                    s_hashTablesV401[i].SetUpV401LikeOriginal((byte)i, s_topLxV401 * s_topLyV401);
                    s_hashTablesV401[i].CreateAreasV401LikeOriginal();
                }
                SetClearBuildigsLockV401LikeOriginal(true);

                s_readyV401 = true;
                sw.Stop();
                s_lastBuildMsV401 = sw.ElapsedMilliseconds;
                LogAuditV401LikeOriginal();
                return true;
            }
            catch (Exception ex)
            {
                sw.Stop();
                s_lastBuildMsV401 = sw.ElapsedMilliseconds;
                s_readyV401 = false;
                SetClearBuildigsLockV401LikeOriginal(true);
                UnityEngine.Debug.LogError("[C2:TOPOLOGY V401B] build_failed source='" + (sourcePath ?? string.Empty) + "' ms=" +
                                           s_lastBuildMsV401 + "\n" + ex);
                return false;
            }
        }

        private static void SetClearBuildigsLockV401LikeOriginal(bool set)
        {
            // CII spelling preserved. set=false means building locks are cleared.
            s_buildingLocksEnabledForTopologyV401 = set;
        }

        private static void BuildMotionFieldAdapterV401LikeOriginal()
        {
            int total = checked(s_motionSxV401 * s_motionSyV401);
            s_motionBlockedV401 = new bool[4][];
            for (int f = 0; f < s_motionBlockedV401.Length; f++)
                s_motionBlockedV401[f] = new bool[total];

            C2BattleTerrainMode mode = s_modeV401 != null
                ? s_modeV401 : UnityEngine.Object.FindFirstObjectByType<C2BattleTerrainMode>();
            for (int x = 0; x < s_motionSxV401; x++)
            {
                int baseOfs = x * s_motionSyV401;
                for (int y = 0; y < s_motionSyV401; y++)
                {
                    for (int f = 0; f < 4; f++)
                    {
                        bool blocked = mode == null ? true :
                            (s_buildingLocksEnabledForTopologyV401
                                ? mode.C2MovementNativeFieldCheckPtV425LikeOriginal(x, y, (byte)f)
                                : mode.C2MovementNativeFieldBaseCheckPtV425LikeOriginal(x, y, (byte)f));
                        s_motionBlockedV401[f][baseOfs + y] = blocked;
                    }
                }
            }
        }

        private static bool CheckPtV401LikeOriginal(int x, int y, int topType)
        {
            if (x < 0 || y < 0 || x >= s_motionSxV401 || y >= s_motionSyV401)
                return true; // MotionField::CheckPt returns 1 outside MAPSX/MAPSY.
            if (topType < 0 || topType >= s_motionBlockedV401.Length || s_motionBlockedV401[topType] == null)
                return true;
            return s_motionBlockedV401[topType][x * s_motionSyV401 + y];
        }

        private static void BSetPtV401LikeOriginal(int x, int y, int topType)
        {
            if (x < 0 || y < 0 || x >= s_motionSxV401 || y >= s_motionSyV401) return;
            if (topType < 0 || topType >= s_motionBlockedV401.Length || s_motionBlockedV401[topType] == null) return;
            s_motionBlockedV401[topType][x * s_motionSyV401 + y] = true;
        }

        private static bool CheckBarV401LikeOriginal(int x, int y, int lx, int ly, int topType)
        {
            for (int ix = 0; ix < lx; ix++)
                for (int iy = 0; iy < ly; iy++)
                    if (CheckPtV401LikeOriginal(x + ix, y + iy, topType)) return true;
            return false;
        }

        private static bool GetTCStatusV401LikeOriginal(int x, int y, byte topType)
        {
            int xxx = x << 2;
            int yyy = y << 2;
            if (!CheckBarV401LikeOriginal(xxx, yyy, 4, 4, topType))
                return true;
            return false;
        }

        private static void MakeCellSingledV401LikeOriginal(int tx, int ty, int lockType)
        {

            byte[] tt = new byte[16];
            bool[] l = new bool[16];
            for (int i = 0; i < 16; i++) tt[i] = 0xFF;
            int x0 = tx << 2;
            int y0 = ty << 2;
            int nBlocked = 0;
            for (int ix = 0; ix < 4; ix++)
            {
                for (int iy = 0; iy < 4; iy++)
                {
                    int ofs = ix + (iy << 2);
                    l[ofs] = CheckPtV401LikeOriginal(x0 + ix, y0 + iy, lockType);
                    if (l[ofs]) nBlocked++;
                }
            }
            if (nBlocked == 0 || nBlocked == 16)
                return;

            int nTop = 0;
            bool change;
            do
            {
                change = false;
                for (int ix = 0; ix < 4; ix++)
                {
                    for (int iy = 0; iy < 4; iy++)
                    {
                        int ofs = ix + (iy << 2);
                        if (!l[ofs])
                        {
                            int tL = ix > 0 ? tt[ofs - 1] : 0xFF;
                            int tR = ix < 3 ? tt[ofs + 1] : 0xFF;
                            int tU = iy > 0 ? tt[ofs - 4] : 0xFF;
                            int tD = iy < 3 ? tt[ofs + 4] : 0xFF;
                            int tC = tt[ofs];
                            int tm = Math.Min(tC, Math.Min(Math.Min(tL, tR), Math.Min(tU, tD)));
                            if (tm == 255)
                            {
                                tt[ofs] = unchecked((byte)nTop);
                                nTop++;
                                change = true;
                            }
                            else if (tt[ofs] != tm)
                            {
                                tt[ofs] = unchecked((byte)tm);
                                change = true;
                            }
                        }
                    }
                }
            }
            while (change);

            byte[] nZon = new byte[Mathf.Max(1, nTop)];
            for (int i = 0; i < 16; i++)
            {
                int t = tt[i];
                if (t != 255 && t < nZon.Length)
                    nZon[t]++;
            }

            int maxIdx = -1;
            int cm = 0;
            for (int i = 0; i < nTop; i++)
            {
                int t = nZon[i];
                if (t > cm)
                {
                    cm = t;
                    maxIdx = i;
                }
            }
            if (maxIdx == -1)
                return;

            for (int i = 0; i < 16; i++)
            {
                if (tt[i] != maxIdx && tt[i] != 255)
                    BSetPtV401LikeOriginal(x0 + (i & 3), y0 + (i >> 2), lockType);
            }
        }

        private static byte GetCellLinksV401LikeOriginal(int tx, int ty, int lockType)
        {

            bool linkL = false;
            bool linkR = false;
            bool linkU = false;
            bool linkD = false;
            int x0 = tx << 2;
            int y0 = ty << 2;
            for (int i = 0; i < 4; i++)
            {
                linkL |= !(CheckPtV401LikeOriginal(x0 - 1, y0 + i, lockType) || CheckPtV401LikeOriginal(x0, y0 + i, lockType));
                linkR |= !(CheckPtV401LikeOriginal(x0 + 3, y0 + i, lockType) || CheckPtV401LikeOriginal(x0 + 4, y0 + i, lockType));
                linkU |= !(CheckPtV401LikeOriginal(x0 + i, y0, lockType) || CheckPtV401LikeOriginal(x0 + i, y0 - 1, lockType));
                linkD |= !(CheckPtV401LikeOriginal(x0 + i, y0 + 3, lockType) || CheckPtV401LikeOriginal(x0 + i, y0 + 4, lockType));
            }

            return unchecked((byte)((linkL ? 1 : 0) +
                                    (linkR ? 2 : 0) +
                                    (linkU ? 4 : 0) +
                                    (linkD ? 8 : 0)));
        }

        private static void CreateRadioV401LikeOriginal()
        {
            if (s_radioReadyV401)
                return;

            int[] counts = new int[RRadV401];
            for (int ix = -RRadV401; ix <= RRadV401; ix++)
            {
                for (int iy = -RRadV401; iy <= RRadV401; iy++)
                {
                    int r = (int)Math.Sqrt(ix * ix + iy * iy);
                    if (r < RRadV401) counts[r]++;
                }
            }

            for (int i = 0; i < RRadV401; i++)
            {
                s_rarrV401[i].Xi = counts[i] > 0 ? new sbyte[counts[i]] : Array.Empty<sbyte>();
                s_rarrV401[i].Yi = counts[i] > 0 ? new sbyte[counts[i]] : Array.Empty<sbyte>();
                s_rarrV401[i].N = 0;
            }

            for (int ix = -RRadV401; ix <= RRadV401; ix++)
            {
                for (int iy = -RRadV401; iy <= RRadV401; iy++)
                {
                    int r = (int)Math.Sqrt(ix * ix + iy * iy);
                    if (r < RRadV401)
                    {
                        int n = s_rarrV401[r].N;
                        s_rarrV401[r].Xi[n] = unchecked((sbyte)ix);
                        s_rarrV401[r].Yi[n] = unchecked((sbyte)iy);
                        s_rarrV401[r].N++;
                    }
                }
            }

            if (s_rarrV401[1].Xi.Length >= 8)
            {
                s_rarrV401[1].Xi[0] = -1; s_rarrV401[1].Yi[0] = 0;
                s_rarrV401[1].Xi[1] = 1;  s_rarrV401[1].Yi[1] = 0;
                s_rarrV401[1].Xi[2] = 0;  s_rarrV401[1].Yi[2] = -1;
                s_rarrV401[1].Xi[3] = 0;  s_rarrV401[1].Yi[3] = 1;
                s_rarrV401[1].Xi[4] = -1; s_rarrV401[1].Yi[4] = 1;
                s_rarrV401[1].Xi[5] = -1; s_rarrV401[1].Yi[5] = -1;
                s_rarrV401[1].Xi[6] = 1;  s_rarrV401[1].Yi[6] = -1;
                s_rarrV401[1].Xi[7] = 1;  s_rarrV401[1].Yi[7] = 1;
            }

            s_radioReadyV401 = true;
        }

        // Read-only adapter for the exact TopoGraf.cpp::Rarr table.  Morale.cpp
        // consumes this table directly; exposing it avoids reconstructing/hardcoding
        // panic radii in a second subsystem.
        public static int GetRadioCountV407LikeOriginal(int radius)
        {
            CreateRadioV401LikeOriginal();
            if (radius < 0 || radius >= s_rarrV401.Length) return 0;
            return s_rarrV401[radius] != null ? s_rarrV401[radius].N : 0;
        }

        public static bool TryGetRadioOffsetV407LikeOriginal(
            int radius, int index, out int x, out int y)
        {
            x = 0;
            y = 0;
            CreateRadioV401LikeOriginal();
            if (radius < 0 || radius >= s_rarrV401.Length) return false;
            RadioV401 radio = s_rarrV401[radius];
            if (radio == null || index < 0 || index >= radio.N ||
                index >= radio.Xi.Length || index >= radio.Yi.Length) return false;
            x = radio.Xi[index];
            y = radio.Yi[index];
            return true;
        }

        private static int NormaV401LikeOriginal(int x, int y)
        {
            int ax = Math.Abs(x);
            int ay = Math.Abs(y);
            int mx = Math.Max(ax, ay);
            return (mx + ax + ay) >> 1;
        }

        public static ushort GetTopRefV401LikeOriginal(int ofs, byte topType = 0)
        {
            if (!s_readyV401 || topType >= s_hashTablesV401.Length)
                return TopBlockedV401;
            HashTopTableV401 table = s_hashTablesV401[topType];
            if (ofs < 0 || ofs >= table.TopRef.Length)
                return TopBlockedV401;
            return table.TopRef[ofs];
        }

        public static AreaV401LikeOriginal GetTopMapV401LikeOriginal(int ofs, byte topType = 0)
        {
            if (!s_readyV401 || topType >= s_hashTablesV401.Length)
                return null;
            HashTopTableV401 table = s_hashTablesV401[topType];
            if (ofs < 0 || ofs >= table.NAreas)
                return null;
            return table.TopMap[ofs];
        }

        public static int GetNAreasV401LikeOriginal(byte topType = 0)
        {
            if (!s_readyV401 || topType >= s_hashTablesV401.Length)
                return 0;
            return s_hashTablesV401[topType].NAreas;
        }

        public static ushort GetLinksDistV401LikeOriginal(int ofs, byte topType = 0, byte ni = 0)
        {
            if (!s_readyV401 || topType >= s_hashTablesV401.Length)
                return TopBlockedV401;
            HashTopV401 ht = s_hashTablesV401[topType].GetHashTopV401LikeOriginal(ofs, ni);
            return ht != null ? ht.LD : TopBlockedV401;
        }

        public static ushort GetMotionLinksV401LikeOriginal(int ofs, byte topType = 0, byte ni = 0)
        {
            if (!s_readyV401 || topType >= s_hashTablesV401.Length)
                return TopBlockedV401;
            HashTopV401 ht = s_hashTablesV401[topType].GetHashTopV401LikeOriginal(ofs, ni);
            if (ht == null)
                return TopBlockedV401;
            int ml = ht.ML;
            if (ml == 8191) ml = TopBlockedV401;
            return unchecked((ushort)ml);
        }

        public static bool CheckIfRoadZoneV425LikeOriginal(int zone)
        {
            return s_modeV401 != null && zone >= 0 && zone < s_modeV401.C2MovementGetRoadCountV425LikeOriginal();
        }

        public static bool GetPreciseTopCenterV425LikeOriginal(int zone, byte topType, out int x, out int y)
        {
            x = y = 0;
            // Factures3D.cpp::GetPreciseTopCenter:
            // road topology zones use the exact RoadsNet knot coordinate for every
            // lock type except water (1). Using the coarse Area cell center here
            // makes HumanGlobalSendTo approach a different point than CII.
            if (topType != 1 && s_modeV401 != null &&
                zone >= 0 && zone < s_modeV401.C2MovementGetRoadCountV425LikeOriginal())
            {
                int nl;
                if (s_modeV401.C2MovementTryGetRoadKnotV425LikeOriginal(zone, out x, out y, out nl))
                    return true;
            }

            AreaV401LikeOriginal ar = GetTopMapV401LikeOriginal(zone, topType);
            if (ar == null) return false;
            x = (ar.x << 6) + 32;
            y = (ar.y << 6) + 32;
            return true;
        }

        public static int GetZoneSpeedBonusV426LikeOriginal(int zone)
        {
            int speed, tiring, priory;
            return s_modeV401 != null &&
                   s_modeV401.C2MovementTryGetRoadZonePhysicsV426LikeOriginal(zone, out speed, out tiring, out priory)
                ? speed : 256;
        }

        public static int GetZoneTiringBonusV426LikeOriginal(int zone)
        {
            int speed, tiring, priory;
            return s_modeV401 != null &&
                   s_modeV401.C2MovementTryGetRoadZonePhysicsV426LikeOriginal(zone, out speed, out tiring, out priory)
                ? tiring : 256;
        }

        public static int GetZonePrioryBonusV426LikeOriginal(int zone)
        {
            int speed, tiring, priory;
            return s_modeV401 != null &&
                   s_modeV401.C2MovementTryGetRoadZonePhysicsV426LikeOriginal(zone, out speed, out tiring, out priory)
                ? priory : 256;
        }

        public static int GetTiringBonusV426LikeOriginal(int originalPixelX, int originalPixelY, byte lockType)
        {
            // Factures3D.cpp::GetTiringBonus.
            if (lockType == 1) return 256;
            int tx = originalPixelX >> 6;
            int ty = originalPixelY >> 6;
            if (!s_readyV401 || tx < 0 || ty < 0 || tx >= s_topLxV401 || ty >= s_topLyV401)
                return 256;
            ushort zone = GetTopRefV401LikeOriginal(tx + (ty << s_topSHV401), lockType);
            return GetZoneTiringBonusV426LikeOriginal(zone);
        }

        public static ushort GetTopFastV401LikeOriginal(int x, int y, byte topType = 0)
        {
            if (!s_readyV401 || x < 0 || y < 0 || x >= s_topLxV401 || y >= s_topLyV401)
                return TopBlockedV401;
            return GetTopRefV401LikeOriginal(x + (y << s_topSHV401), topType);
        }

        public static int GetTopologyV401LikeOriginal(int x, int y, byte lockType = 0)
        {
            if (!s_readyV401 || lockType >= s_hashTablesV401.Length)
                return TopBlockedV401;

            int xc = x >> 6;
            int yc = y >> 6;
            ushort tr;
            if (xc < 0 || yc < 0 || xc >= s_topLxV401 || yc >= s_topLyV401)
                tr = TopBlockedV401;
            else
                tr = GetTopRefV401LikeOriginal(xc + (yc << s_topSHV401), lockType);
            if (tr < TopFreeUnassignedV401)
                return tr;

            for (int i = 0; i < 20; i++)
            {
                RadioV401 radio = s_rarrV401[i];
                for (int j = 0; j < radio.N; j++)
                {
                    int xx = xc + radio.Xi[j];
                    int yy = yc + radio.Yi[j];
                    if (xx >= 0 && yy >= 0 && xx < s_topLxV401 && yy < s_topLyV401)
                    {
                        tr = GetTopRefV401LikeOriginal(xx + (yy << s_topSHV401), lockType);
                        if (tr < TopFreeUnassignedV401)
                            return tr;
                    }
                }
            }
            return TopBlockedV401;
        }

        public static int GetTopologyV401LikeOriginal(ref int x, ref int y, byte lockType = 0)
        {
            if (!s_readyV401 || lockType >= s_hashTablesV401.Length)
                return TopBlockedV401;

            int xc = x >> 6;
            int yc = y >> 6;
            ushort tr;
            if (xc < 0 || yc < 0 || xc >= s_topLxV401 || yc >= s_topLyV401)
                tr = TopBlockedV401;
            else
                tr = GetTopRefV401LikeOriginal(xc + (yc << s_topSHV401), lockType);
            if (tr < TopFreeUnassignedV401)
                return tr;

            for (int i = 0; i < 20; i++)
            {
                RadioV401 radio = s_rarrV401[i];
                for (int j = 0; j < radio.N; j++)
                {
                    int xx = xc + radio.Xi[j];
                    int yy = yc + radio.Yi[j];
                    if (xx >= 0 && yy >= 0 && xx < s_topLxV401 && yy < s_topLyV401)
                    {
                        tr = GetTopRefV401LikeOriginal(xx + (yy << s_topSHV401), lockType);
                        if (tr < TopFreeUnassignedV401)
                        {
                            x = (xx << 6) + 32;
                            y = (yy << 6) + 32;
                            return tr;
                        }
                    }
                }
            }
            return TopBlockedV401;
        }

        // Megapolis.cpp::GetTopDistance(...,LT,NI). Coordinates are TopRef cells.
        public static int GetTopDistanceV428LikeOriginal(int xa, int ya, int xb, int yb, byte lockType, byte nation)
        {
            int[] tdxy = { 1,0, 0,1, -1,0, 0,-1, 1,1, 1,-1, -1,1, -1,-1 };
            int top1 = GetTopFastV401LikeOriginal(xa, ya, lockType);
            if (top1 == 0xFFFF)
                for (int i = 0; i < tdxy.Length && top1 == 0xFFFF; i += 2)
                    top1 = GetTopFastV401LikeOriginal(xa + tdxy[i], ya + tdxy[i + 1], lockType);
            int top2 = GetTopFastV401LikeOriginal(xb, yb, lockType);
            if (top2 == 0xFFFF)
                // Retail has xa/ya here as well (not xb/yb); preserve that quirk.
                for (int i = 0; i < tdxy.Length && top2 == 0xFFFF; i += 2)
                    top2 = GetTopFastV401LikeOriginal(xa + tdxy[i], ya + tdxy[i + 1], lockType);
            if (top1 >= 0xFFFE || top2 >= 0xFFFE) return 0xFFFF;
            if (top1 == top2) return C2OriginalMovementMathV352.Norma(xa - xb, ya - yb);
            int na = GetNAreasV401LikeOriginal(lockType);
            if (na <= 0) return 0xFFFF;
            int next1 = GetMotionLinksV401LikeOriginal(top1 * na + top2, lockType, nation);
            if (next1 == 0xFFFF) return 0xFFFF;
            if (next1 == top2) return C2OriginalMovementMathV352.Norma(xa - xb, ya - yb);
            int next2 = GetMotionLinksV401LikeOriginal(top2 * na + top1, lockType, nation);
            if (next2 == 0xFFFF) return 0xFFFF;
            if (next2 == top1) return C2OriginalMovementMathV352.Norma(xa - xb, ya - yb);
            AreaV401LikeOriginal ar1 = GetTopMapV401LikeOriginal(next1, lockType);
            AreaV401LikeOriginal ar2 = GetTopMapV401LikeOriginal(next2, lockType);
            if (ar1 == null || ar2 == null) return 0xFFFF;
            int d = C2OriginalMovementMathV352.Norma(xa - ar1.x, ya - ar1.y) +
                    C2OriginalMovementMathV352.Norma(xb - ar2.x, yb - ar2.y);
            if (next1 == next2) return d;
            int links = GetLinksDistV401LikeOriginal(next1 * na + next2, lockType, nation);
            return links >= 0xFFFE ? 0xFFFF : d + links;
        }

        // NewMon.cpp::GetWTopology overloads search only Rarr[0..9].
        // Keep these separate from GetTopology(), whose wider correction radius is
        // used by other engine paths. SmartSend must use the retail 10-ring variant.
        public static int GetWTopologyV428LikeOriginal(int x, int y, byte lockType = 0)
        {
            if (!s_readyV401 || lockType >= s_hashTablesV401.Length || GetNAreasV401LikeOriginal(lockType) == 0)
                return TopBlockedV401;
            int xc = x >> 6, yc = y >> 6;
            if (xc < 0 || yc < 0 || xc >= s_topLxV401 || yc >= s_topLyV401) return TopBlockedV401;
            ushort tr = GetTopRefV401LikeOriginal(xc + (yc << s_topSHV401), lockType);
            if (tr < TopFreeUnassignedV401) return tr;
            CreateRadioV401LikeOriginal();
            for (int r = 0; r < 10; r++)
            {
                RadioV401 radio = s_rarrV401[r];
                for (int q = 0; q < radio.N; q++)
                {
                    int xx = xc + radio.Xi[q], yy = yc + radio.Yi[q];
                    tr = (xx < 0 || yy < 0 || xx >= s_topLxV401 || yy >= s_topLyV401)
                        ? TopBlockedV401
                        : GetTopRefV401LikeOriginal(xx + (yy << s_topSHV401), lockType);
                    if (tr < TopFreeUnassignedV401) return tr;
                }
            }
            return TopBlockedV401;
        }

        public static int GetWTopologyV428LikeOriginal(ref int x, ref int y, byte lockType = 0)
        {
            if (!s_readyV401 || lockType >= s_hashTablesV401.Length || GetNAreasV401LikeOriginal(lockType) == 0)
                return TopBlockedV401;
            int xc = x >> 6, yc = y >> 6;
            if (xc < 0 || yc < 0 || xc >= s_topLxV401 || yc >= s_topLyV401) return TopBlockedV401;
            ushort tr = GetTopRefV401LikeOriginal(xc + (yc << s_topSHV401), lockType);
            if (tr < TopFreeUnassignedV401) return tr;
            CreateRadioV401LikeOriginal();
            for (int r = 0; r < 10; r++)
            {
                RadioV401 radio = s_rarrV401[r];
                for (int q = 0; q < radio.N; q++)
                {
                    int xx = xc + radio.Xi[q], yy = yc + radio.Yi[q];
                    tr = (xx < 0 || yy < 0 || xx >= s_topLxV401 || yy >= s_topLyV401)
                        ? TopBlockedV401
                        : GetTopRefV401LikeOriginal(xx + (yy << s_topSHV401), lockType);
                    if (tr < TopFreeUnassignedV401)
                    {
                        x = (xx << 6) + 32;
                        y = (yy << 6) + 32;
                        return tr;
                    }
                }
            }
            return TopBlockedV401;
        }

        // NewMon.cpp::GetWTopology1: no nearest-zone correction.
        public static int GetWTopology1V428LikeOriginal(int x, int y, byte lockType = 0)
        {
            // NewMon.cpp::GetWTopology1 receives MotionField-cell coordinates
            // (16 original pixels per cell), therefore topology coordinates are >>2.
            if (!s_readyV401 || lockType >= s_hashTablesV401.Length) return TopBlockedV401;
            int xc = x >> 2, yc = y >> 2;
            if (xc < 0 || yc < 0 || xc >= s_topLxV401 || yc >= s_topLyV401) return TopBlockedV401;
            ushort tr = GetTopRefV401LikeOriginal(xc + (yc << s_topSHV401), lockType);
            return tr < TopFreeUnassignedV401 ? tr : TopBlockedV401;
        }

        // NewMon.cpp::CheckTopDirectWay: raw topology-cell sampling, no correction.
        public static bool CheckTopDirectWayV428LikeOriginal(int x0, int y0, int x1, int y1, byte topType)
        {
            x0 <<= 8; y0 <<= 8; x1 <<= 8; y1 <<= 8;
            int dx = x1 - x0, dy = y1 - y0;
            int n = (C2OriginalMovementMathV352.Norma(dx, dy) >> 14) + 1;
            dx /= n; dy /= n;
            for (int i = 0; i < n; i++)
            {
                x0 += dx; y0 += dy;
                int tx = x0 >> 14, ty = y0 >> 14;
                if (tx <= 0 || ty <= 0 || tx >= s_topLxV401 || ty >= s_topLyV401) return false;
                if (GetTopRefV401LikeOriginal(tx + (ty << s_topSHV401), topType) >= TopFreeUnassignedV401) return false;
            }
            return true;
        }


        private static void LogAuditV401LikeOriginal()
        {
            HashTopTableV401 table = s_hashTablesV401[0];
            int assigned = 0;
            int freeUnassigned = 0;
            int blocked = 0;
            for (int i = 0; i < table.TopRef.Length; i++)
            {
                ushort tr = table.TopRef[i];
                if (tr < TopFreeUnassignedV401) assigned++;
                else if (tr == TopFreeUnassignedV401) freeUnassigned++;
                else blocked++;
            }

            int directedLinks = 0;
            int isolatedAreas = 0;
            for (int i = 0; i < table.NAreas; i++)
            {
                int nl = table.TopMap[i].Link.Count;
                directedLinks += nl;
                if (nl == 0) isolatedAreas++;
            }

            string sample = "none";
            if (table.NAreas >= 2)
            {
                int first = 0;
                int last = table.NAreas - 1;
                ushort d1 = GetLinksDistV401LikeOriginal(first + last * table.NAreas, 0, 0);
                ushort d2 = GetLinksDistV401LikeOriginal(first + last * table.NAreas, 0, 0);
                sample = first + "->" + last + " dist=" + d1 + " cached=" + d2;
            }

            UnityEngine.Debug.Log(
                "[C2:TOPOLOGY V401B] READY source='" + s_sourcePathV401 + "'" +
                " addsh=" + s_addshV401 +
                " motion=" + s_motionSxV401 + "x" + s_motionSyV401 +
                " top=" + s_topLxV401 + "x" + s_topLyV401 +
                " topSH=" + s_topSHV401 +
                " areas=" + table.NAreas +
                " directedLinks=" + directedLinks +
                " assignedCells=" + assigned +
                " freeUnassigned=" + freeUnassigned +
                " blockedCells=" + blocked +
                " isolatedAreas=" + isolatedAreas +
                " hashSample='" + sample + "'" +
                " buildMs=" + s_lastBuildMsV401 +
                " adapter=MFIELDS_CheckPt_CheckBar_BSetPt_to_Unity_motion_snapshot" +
                " initialBuildingLocks=CLEARED_LIKE_CII roads=DEFERRED gates=DEFERRED lockTypes=0_ONLY");
        }
    }
}


// ============================================================================
// MERGED FROM: C2OriginalOrderChainV352.cs
// ============================================================================
namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // Unity-side representation of OneObject::LocalOrder/Order1::NextOrder for move commands.
    // The insertion rules are copied from COSSACKS2/NewMon.cpp::OneObject::CreateOrder.
    internal sealed class C2OriginalOrderChainV352
    {
        // NewMon.cpp::LongProcesses processes each nation's objects in sequence:
        // LocalOrder->DoLink, then that same object's motion handler. The unit
        // owns this chain; there is no separate backwards active-order scheduler.
        private sealed class MoveOrder
        {
            internal byte PriorityV434 = 16; // NewMonsterSendTo default 128+16, stored without high bit.
            internal MoveOrder NextOrder;
            internal Vector2[] PathReal;
            internal float DestRealX;
            internal float DestRealY;
            internal bool HasPath;
            internal bool Precise;
            internal bool ParentTaskMoveV433;
            internal bool BuildCellTargetV434;
            internal bool HasFinalFacing;
            internal byte FinalFacing;
            internal float SpeedOriginalPixelsPerSecond;
            internal string Source;
            internal bool Started;
            internal bool PreserveCurrentAttackFrame;
            internal bool WaitForForeignOrder;
            internal bool IsStayForSomeTime;
            internal byte UnlimitedOrderV433; // 1=SetUnlimitedLink, 2=ClearUnlimitedLink
            internal int StayProgress256;

            // NewMon.cpp::NewMonsterSmartSendTo / NewMonsterSmartSendToLink.
            internal bool SmartSend;
            internal int SmartX;
            internal int SmartY;
            internal int SmartDx;
            internal int SmartDy;
            internal int SmartNextX = 0xFFFF;
            internal int SmartNextY = 0xFFFF;
            internal int SmartNextTop = 0xFFFF;
        }

        private C2NeutralPeasantUnitInfoV2LikeOriginal _unit;
        private MoveOrder _localOrder;

        internal static C2OriginalOrderChainV352 GetOrCreate(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null) return null;
            C2OriginalOrderChainV352 chain = unit.C2MoveOrderChainV362LikeOriginal;
            if (chain == null)
            {
                chain = new C2OriginalOrderChainV352();
                unit.C2MoveOrderChainV362LikeOriginal = chain;
            }
            chain._unit = unit;
            return chain;
        }

        internal static void TickUnitOrderV433LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null || unit.IsDeadLikeOriginal) return;
            var chain = unit.C2MoveOrderChainV362LikeOriginal;
            if (chain != null && chain._localOrder != null) chain.TickOrderNodeLikeOriginal();
        }

        internal static void ClearMoveChainForExternalOrder(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null) return;
            C2OriginalOrderChainV352 chain = unit.C2MoveOrderChainV362LikeOriginal;
            if (chain != null) chain._localOrder = null;
            // CII NewMon.cpp::ClearOrders -> DeleteLastOrder sets DestX=-1.
            // Clearing only the managed node left the previous destination alive:
            // KeepPositions then waited for the OLD move before issuing the new one.
            C2UnitOriginalRuntime runtime = unit.RuntimeLinkCachedLikeOriginal?.Runtime;
            C2OriginalMovementSystemV425LikeOriginal.DisablePathRequestV425LikeOriginal(unit);
            if (runtime != null)
            {
                runtime.FillCrewV441 = null;
                runtime.ArtilleryChargeOrderV439 = -1;
                runtime.ArtilleryPointOrderV439 = null;
                runtime.ArtilleryAutoFireV442 = false;
                runtime.OriginalPathRequestPendingV425LikeOriginal = false;
                runtime.HasMoveTargetLikeOriginal = false;
                runtime.MoveDeferredUntilNeutralStandLikeOriginal = false;
                runtime.MovePathRealWaypointsLikeOriginal = null;
                runtime.MovePathIndexLikeOriginal = 0;
                runtime.HasFinalFacingDirLikeOriginal = false;
                runtime.FinalRotUnitActiveV352LikeOriginal = false;
                runtime.SingleStepRotateAtPlaceActiveV352LikeOriginal = false;
                runtime.PreciseBornPathLikeOriginal = false;
                runtime.PreciseBornPathLastWaypointIndexLikeOriginal = -1;
                runtime.MovePathFinalAllowsUnitOverlapFinishLikeOriginal = false;
            }
            unit.C2NeutralPeasantUnitsV15SetMovingFlagLikeOriginal(false, false);
        }

        internal static byte GetLocalPriorityV434LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            return unit?.C2MoveOrderChainV362LikeOriginal?._localOrder?.PriorityV434 ?? (byte)0;
        }

        internal static bool HasLocalMoveOrderLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            // A destination waiting for an unbreakable animation is still a
            // LocalOrder. Reissuing it every tick restarts posture transitions.
            return unit != null && unit.C2MoveOrderChainV362LikeOriginal != null &&
                   unit.C2MoveOrderChainV362LikeOriginal._localOrder != null;
        }

        internal static bool HasPreciseHeadV433LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        { return unit?.C2MoveOrderChainV362LikeOriginal?._localOrder?.Precise == true; }

        internal static void StopTaskMoveV433LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if(unit?.C2MoveOrderChainV362LikeOriginal?._localOrder?.ParentTaskMoveV433 == true)
                ClearMoveChainForExternalOrder(unit);
        }

        internal static void ReleaseForUnitLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null) return;
            C2OriginalMovementSystemV425LikeOriginal.DisablePathRequestV425LikeOriginal(unit);
            C2OriginalOrderChainV352 chain = unit.C2MoveOrderChainV362LikeOriginal;
            if (chain != null)
            {
                chain._localOrder = null;
                chain._unit = null;
            }
            unit.C2MoveOrderChainV362LikeOriginal = null;
            unit.C2OrderRuntimeStateV362LikeOriginal = null;
        }

        internal static bool SubmitMove(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            float destRealX,
            float destRealY,
            bool hasFinalFacing,
            byte finalFacing,
            byte ordType,
            string source,
            bool precise = false,
            byte priority = 144)
        {
            C2OriginalOrderChainV352 chain = GetOrCreate(unit);
            if (chain == null) return false;
            MoveOrder order = new MoveOrder
            {
                PriorityV434 = (byte)(priority & 127),
                DestRealX = destRealX,
                DestRealY = destRealY,
                HasFinalFacing = hasFinalFacing,
                FinalFacing = finalFacing,
                SpeedOriginalPixelsPerSecond = C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                Source = source ?? "c2_move",
                Precise = precise,
                HasPath = false
            };
            byte createOrderType = (byte)(ordType & 127);
            // NewMonsterSmartSendTo: CreateOrder((OrdType&127)==0 ? 3 : (OrdType&127)).
            if (createOrderType == 0) createOrderType = 3;
            chain.InsertLikeCreateOrder(order, createOrderType);
            chain.TryStartHead();
            
            return true;
        }

        internal static void SubmitTaskMoveV433LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            float x,float y,float speed,bool face,byte direction,bool precise,string source)
        {
            var chain=GetOrCreate(unit);
            if(chain==null)return;
            // SetOrderedStateForComplexObjectLink is a stationary local order,
            // never the parent of a movement child. A new destination replaces it
            // even when it arrives through the task/direct-move entry point.
            if(unit.RuntimeLinkCachedLikeOriginal?.Runtime != null)
                {
                var rt = unit.RuntimeLinkCachedLikeOriginal.Runtime;
                rt.ArtilleryChargeOrderV439 = -1;
                if(source != "AttackPointByComplexObject::CreatePath"){rt.ArtilleryPointOrderV439 = null;rt.ArtilleryAutoFireV442=false;}
            }
            // These requests originate inside the active attack/work/panic task.
            // Replacing its movement child must not cancel that parent task.
            var order=chain._localOrder;
            if(order!=null && order.NextOrder==null && !order.HasPath && !order.SmartSend &&
                order.UnlimitedOrderV433==0 && !order.IsStayForSomeTime && order.Source==source)
            {
                if(order.DestRealX==x && order.DestRealY==y && order.Precise==precise &&
                    order.HasFinalFacing==face && order.FinalFacing==direction)return;
            }
            else order=new MoveOrder();
            C2OriginalMovementSystemV425LikeOriginal.DisablePathRequestV425LikeOriginal(unit);
            order.NextOrder=null;order.DestRealX=x;order.DestRealY=y;
            order.Precise=precise;order.HasPath=false;order.SmartSend=false;order.ParentTaskMoveV433=true;
            order.BuildCellTargetV434=false;
            order.HasFinalFacing=face;order.FinalFacing=direction;
            order.SpeedOriginalPixelsPerSecond=speed;order.Source=source;order.Started=false;
            chain._localOrder=order;
            chain.TryStartHead();
        }

        internal static void SubmitBuildApproachV434LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            int cellX, int cellY, float speed)
        {
            // BuildObjLink calls CreatePath(ObjX,ObjY), not NewMonsterSendTo.
            // Convert the requested object cell back to its center; ordinary
            // SendTo's StopDistance/correct-destination rules must not displace
            // this fixed BUILDPOINT or finish before BuildObjLink's dst<=1 gate.
            int halfSize = C2OriginalMovementSystemV425LikeOriginal.ResolveLxLikeOriginal(unit) << 7;
            SubmitTaskMoveV433LikeOriginal(unit, (cellX << 8) + halfSize, (cellY << 8) + halfSize,
                speed, false, 0, false, "BuildObjLink::CreatePath");
            var chain = unit?.C2MoveOrderChainV362LikeOriginal;
            if (chain?._localOrder != null) chain._localOrder.BuildCellTargetV434 = true;
        }

        internal static bool SubmitSmartMoveV428LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            float centerRealX,
            float centerRealY,
            float slotRealX,
            float slotRealY,
            bool hasFinalFacing,
            byte finalFacing,
            byte ordType,
            string source)
        {
            C2OriginalOrderChainV352 chain = GetOrCreate(unit);
            if (chain == null || unit == null) return false;

            int x = Mathf.RoundToInt(centerRealX) >> 4;
            int y = Mathf.RoundToInt(centerRealY) >> 4;
            int dx = (Mathf.RoundToInt(slotRealX) - Mathf.RoundToInt(centerRealX)) >> 4;
            int dy = (Mathf.RoundToInt(slotRealY) - Mathf.RoundToInt(centerRealY)) >> 4;

            int lx = C2OriginalMovementSystemV425LikeOriginal.ResolveLxLikeOriginal(unit);
            if (lx > 1)
            {
                x += dx;
                y += dy;
                dx = 0;
                dy = 0;
            }

            byte lockType = C2OriginalMovementSystemV425LikeOriginal.ResolveLockTypeV425LikeOriginal(unit);
            byte nation = unit.Nation;
            if (C2TopologyCoreV401LikeOriginal.GetNAreasV401LikeOriginal(lockType) == 0) return false;

            C2UnitOriginalRuntimeLinkLikeOriginal smartLink = unit.RuntimeLinkCachedLikeOriginal;
            C2UnitOriginalRuntime smartRt = smartLink != null ? smartLink.Runtime : null;
            if (smartLink != null && !smartLink.IsReadyLikeOriginal) return false;
            if (smartRt != null) smartRt.OriginalStandTimeV425LikeOriginal = 0;

            // NewMonsterSmartSendTo tests the near/topologically-close branch BEFORE
            // the RR>128 recursive clamp.  Preserve that order: a large PORD offset can
            // still finish through NewMonsterPreciseSendTo when topology says it is near.
            int rr = C2OriginalMovementMathV352.Norma(dx, dy);
            int realX = smartRt != null ? Mathf.RoundToInt(smartRt.RuntimeRealXLikeOriginal) : unit.RealX;
            int realY = smartRt != null ? Mathf.RoundToInt(smartRt.RuntimeRealYLikeOriginal) : unit.RealY;
            int startTop = C2TopologyCoreV401LikeOriginal.GetWTopology1V428LikeOriginal(
                realX >> 8, realY >> 8, lockType);
            int finTop = C2TopologyCoreV401LikeOriginal.GetWTopology1V428LikeOriginal(
                x >> 4, y >> 4, lockType);
            int topDist = startTop == finTop
                ? 0
                : C2TopologyCoreV401LikeOriginal.GetTopDistanceV428LikeOriginal(
                    x >> 6, y >> 6, realX >> 10, realY >> 10, lockType, nation);
            int nearDy = (y - (realY >> 4)) >> 1; // retail shifts only Y here
            if (topDist < 80 && C2OriginalMovementMathV352.Norma(x - (realX >> 4), nearDy) < 1100)
            {
                int destRealX = (x + dx) << 4;
                int destRealY = (y + dy) << 4;
                int cellX = (destRealX - (lx << 7)) >> 8;
                int cellY = (destRealY - (lx << 7)) >> 8;
                int originalCellX = cellX, originalCellY = cellY;
                // NewMon.cpp::NewMonsterSmartSendTo excludes its own articulated
                // footprint. Complex occupancy now lives in MFIELDS, too.
                bool locked = smartRt?.OriginalComplexObjectV430LikeOriginal?.Lockpoints == true;
                bool found;
                if(locked)C2OriginalMovementSystemV425LikeOriginal.UnlockComplexObjectV430LikeOriginal(smartRt);
                try { found=C2OriginalMovementSystemV425LikeOriginal.FindBestPositionOldV425LikeOriginal(unit,ref cellX,ref cellY,40,lockType); }
                finally { if(locked)C2OriginalMovementSystemV425LikeOriginal.LockComplexObjectV430LikeOriginal(smartRt); }
                if (found &&
                    (cellX != originalCellX || cellY != originalCellY))
                {
                    destRealX = (cellX << 8) + (lx << 7);
                    destRealY = (cellY << 8) + (lx << 7);
                }
                return SubmitMove(unit, destRealX, destRealY, hasFinalFacing, finalFacing,
                    ordType, source ?? "NewMonsterSmartSendTo_near_PreciseSend", true);
            }

            // Native recursion for a large formation offset.  One reduction is enough:
            // the new dx is <=120 and the new dy is <=90, so the recursive call cannot
            // enter this branch a second time.
            if (rr > 128)
            {
                int dx1 = dx * 120 / rr;
                int dy1 = dy * 90 / rr;
                x += dx - dx1;
                y += dy - dy1;
                dx = dx1;
                dy = dy1;
            }
            if (x < 64) x = 64;
            if (y < 64) y = 64;

            // Native validates/corrects the SmartSend centre with GetWTopology(&x,&y)
            // before CreateOrder.  Do not create an order with an invalid topology.
            int correctedTop = C2TopologyCoreV401LikeOriginal.GetWTopologyV428LikeOriginal(
                ref x, ref y, lockType);
            if (correctedTop == 0xFFFF) return false;

            MoveOrder order = new MoveOrder
            {
                SmartSend = true,
                SmartX = x,
                SmartY = y,
                SmartDx = dx,
                SmartDy = dy,
                DestRealX = (x + dx) << 4,
                DestRealY = (y + dy) << 4,
                HasFinalFacing = hasFinalFacing,
                FinalFacing = finalFacing,
                SpeedOriginalPixelsPerSecond = C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                Source = source ?? "NewMonsterSmartSendTo"
            };
            byte createOrderType = (byte)(ordType & 127);
            if (createOrderType == 0) createOrderType = 3;
            chain.InsertLikeCreateOrder(order, createOrderType);
            chain.TryStartHead();
            
            return true;
        }

        // Build.cpp: SetUnlimited -> PreciseSend(BORNPOINTS[1..N]) ->
        // ClearUnlimited -> ordinary rally/scatter. These are LocalOrder nodes,
        // not an unowned destination that SINGLESTEP is required to discard.
        internal static void SubmitBornExitV433LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit, Vector2[] points, int preciseCount,
            bool preserveParentTask = false, bool clearOutside = true)
        {
            var chain = GetOrCreate(unit);
            if (chain == null || points == null || points.Length == 0) return;
            if (preserveParentTask)
            {
                // Resource/build tasks remain the parent order. Only replace their
                // movement child, otherwise CancelForeignOrders would cancel work.
                ClearMoveChainForExternalOrder(unit);
                chain._localOrder = new MoveOrder { UnlimitedOrderV433=1,
                    Source="Mine.cpp::SetUnlimitedLink",ParentTaskMoveV433=true };
            }
            else chain.InsertLikeCreateOrder(new MoveOrder { UnlimitedOrderV433 = 1,
                Source = "Build.cpp::SetUnlimitedLink" }, 0);
            for (int i = 0; i < preciseCount; i++)
                chain.InsertLikeCreateOrder(new MoveOrder { Precise = true, ParentTaskMoveV433=preserveParentTask,
                    DestRealX = points[i].x, DestRealY = points[i].y,
                    SpeedOriginalPixelsPerSecond = C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                    Source = "Build.cpp::BornPreciseSend" }, 2);
            chain.InsertLikeCreateOrder(new MoveOrder { ParentTaskMoveV433=preserveParentTask, UnlimitedOrderV433 = (byte)(clearOutside ? 2 : 3),
                Source = clearOutside ? "NewMon.cpp::ClearUnlimitedLink" : "Mine.cpp::EnterBuilding" }, 2);
            for (int i = preciseCount; i < points.Length; i++)
                chain.InsertLikeCreateOrder(new MoveOrder {
                    DestRealX = points[i].x, DestRealY = points[i].y,
                    SpeedOriginalPixelsPerSecond = C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                    Source = "Build.cpp::RallyOrScatter" }, 2);
            chain.TryStartHead();
            
        }

        private void TickUnlimitedOrderV433LikeOriginal()
        {
            var rt = _unit?.RuntimeLinkCachedLikeOriginal?.Runtime;
            if (rt == null) return;
            if (_localOrder.UnlimitedOrderV433 == 1)
                rt.PreciseBornPathLikeOriginal = true;
            else if (_localOrder.UnlimitedOrderV433 == 3)
            {
                // Arrival inside CONCENTRATOR hands control to the resource task.
                // FINDNEAREMPTY must not drag an arriving worker back outside.
                rt.PreciseBornPathLikeOriginal=false;
                rt.PreciseBornPathLastWaypointIndexLikeOriginal=-1;
            }
            else
            {
                int ms = rt.Md.MotionStyle == "SHEEPS" ? 2 : rt.Md.MotionStyle == "NEWSHEEPS" ? 5 : -1;
                if (ms != 2 && ms != 5 && rt.OriginalStandTimeV425LikeOriginal < 5 && _localOrder.NextOrder == null) return;
                // ClearUnlimitedLink temporarily removes the articulated footprint.
                bool complex = rt.OriginalComplexObjectV430LikeOriginal != null;
                if (complex) C2OriginalMovementSystemV425LikeOriginal.UnlockComplexObjectV430LikeOriginal(rt);
                int lx = C2OriginalMovementSystemV425LikeOriginal.ResolveLxLikeOriginal(_unit);
                int x = (Mathf.RoundToInt(rt.RuntimeRealXLikeOriginal) - (lx << 7)) >> 8;
                int y = (Mathf.RoundToInt(rt.RuntimeRealYLikeOriginal) - (lx << 7)) >> 8;
                bool found = false; int fx = x, fy = y;
                if (!rt.HiddenInsideBuildingLikeOriginal && C2OriginalMovementSystemV425LikeOriginal.CheckBarV425LikeOriginal(x-1,y-1,3,3))
                {
                    // NewMon.cpp::FINDNEAREMPTY: Rarr[1..29], exact 15x15 bar.
                    for (int r = 1; r < 30 && !found; r++)
                        for (int k = 0; k < C2TopologyCoreV401LikeOriginal.GetRadioCountV407LikeOriginal(r); k++)
                        {
                            int dx,dy;
                            C2TopologyCoreV401LikeOriginal.TryGetRadioOffsetV407LikeOriginal(r,k,out dx,out dy);
                            if (C2OriginalMovementSystemV425LikeOriginal.CheckBarV425LikeOriginal(x+dx-7,y+dy-7,15,15)) continue;
                            fx=x+dx; fy=y+dy; found=true; break;
                        }
                }
                if (complex) C2OriginalMovementSystemV425LikeOriginal.LockComplexObjectV430LikeOriginal(rt);
                if (found)
                {
                    InsertLikeCreateOrder(new MoveOrder { Precise=true, ParentTaskMoveV433=_localOrder.ParentTaskMoveV433,
                        DestRealX=fx<<8, DestRealY=fy<<8,
                        SpeedOriginalPixelsPerSecond=C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                        Source="ClearUnlimitedLink::PreciseSend" },1);
                    TryStartHead();
                    return;
                }
                rt.PreciseBornPathLikeOriginal=false;
                rt.PreciseBornPathLastWaypointIndexLikeOriginal=-1;
                // The scenario's village group remains attached by the production caller.
                // Native also seeds Motion.cpp NextForceX/Y here; that field is not
                // the BoidsExtension push-force snapshot and must not overwrite it.
            }
            _unit.C2NeutralPeasantUnitsV15SetMovingFlagLikeOriginal(rt.PreciseBornPathLikeOriginal,rt.PreciseBornPathLikeOriginal);
            _localOrder=_localOrder.NextOrder;
            TryStartHead();
        }

        internal static bool SubmitStayForSomeTimeLikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            byte ordType,
            int time256,
            string source)
        {
            // NewMon.cpp::StayForSomeTime: CreateOrder(OrdType), Progress=Time,
            // DoLink=StayForSomeTimeLink, PrioryLevel=16.  The move chain already
            // owns CreateOrder insertion semantics, so the delay is a real node,
            // not a Unity coroutine/timer.
            C2OriginalOrderChainV352 chain = GetOrCreate(unit);
            if (chain == null) return false;
            MoveOrder order = new MoveOrder
            {
                IsStayForSomeTime = true,
                StayProgress256 = time256,
                Source = source ?? "StayForSomeTime"
            };
            byte createOrderType = (byte)(ordType & 127);
            chain.InsertLikeCreateOrder(order, createOrderType);
            chain.TryStartHead();
            
            return true;
        }

        internal static bool SubmitPath(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            Vector2[] pathReal,
            bool hasFinalFacing,
            byte finalFacing,
            byte ordType,
            string source,
            float speedOriginalPixelsPerSecond = 0.0f)
        {
            if (unit == null || pathReal == null || pathReal.Length == 0) return false;
            C2OriginalOrderChainV352 chain = GetOrCreate(unit);
            if (chain == null) return false;
            MoveOrder order = new MoveOrder
            {
                PathReal = pathReal,
                HasPath = true,
                HasFinalFacing = hasFinalFacing,
                FinalFacing = finalFacing,
                SpeedOriginalPixelsPerSecond = speedOriginalPixelsPerSecond > 0.0f
                    ? speedOriginalPixelsPerSecond
                    : C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                Source = source ?? "c2_path"
            };
            byte createOrderType = (byte)(ordType & 127);
            if (createOrderType == 0) createOrderType = 3;
            chain.InsertLikeCreateOrder(order, createOrderType);
            chain.TryStartHead();
            
            return true;
        }

        private void InsertLikeCreateOrder(MoveOrder order, byte type)
        {
            if (order == null) return;

            // NewMon.cpp::CreateOrder:
            // 1 -> push to LocalOrder head.
            // 2 -> append to the tail.
            // 3 -> clear, except preserve the currently executing attack order/frame.
            // default -> clear and replace.
            switch (type)
            {
                case 1:
                    order.NextOrder = _localOrder;
                    _localOrder = order;
                    break;

                case 2:
                    order.NextOrder = null;
                    if (_localOrder == null)
                    {
                        // The Unity compatibility layer also has attack/build/resource orders
                        // outside this move chain. In C2 those are LocalOrder nodes too, so a
                        // Shift move must wait behind them rather than replacing them.
                        order.WaitForForeignOrder = IsForeignOrderStillBusy();
                        _localOrder = order;
                    }
                    else
                    {
                        MoveOrder tail = _localOrder;
                        while (tail.NextOrder != null) tail = tail.NextOrder;
                        tail.NextOrder = order;
                    }
                    break;

                case 3:
                {
                    bool preserveAttack = IsCurrentAttackFrameRunning();
                    _localOrder = order;
                    order.NextOrder = null;
                    order.PreserveCurrentAttackFrame = preserveAttack;
                    if (!preserveAttack)
                        CancelForeignOrdersForReplacement(order.Source);
                    break;
                }

                default:
                    _localOrder = order;
                    order.NextOrder = null;
                    CancelForeignOrdersForReplacement(order.Source);
                    break;
            }
        }

        private bool IsCurrentAttackFrameRunning()
        {
            if (_unit == null) return false;
            C2UnitOrderRuntimeV325LikeOriginal state =
                C2UnitOrderRuntimeV325LikeOriginal.TryGetLikeOriginal(_unit);
            if (state == null) return false;
            C2UnitOrderKindV325LikeOriginal kind = state.CurrentLikeOriginal;
            bool attack = kind == C2UnitOrderKindV325LikeOriginal.Attack ||
                          kind == C2UnitOrderKindV325LikeOriginal.PreciseAttack ||
                          kind == C2UnitOrderKindV325LikeOriginal.UnitAttack ||
                          kind == C2UnitOrderKindV325LikeOriginal.RangedAttack ||
                          kind == C2UnitOrderKindV325LikeOriginal.MeleeAttack ||
                          kind == C2UnitOrderKindV325LikeOriginal.GrenadeAttack;
            if (!attack) return false;
            C2UnitOriginalRuntimeLinkLikeOriginal link = _unit.RuntimeLinkCachedLikeOriginal;
            if (link == null) return false;
            int frame;
            int frameCount;
            int activeFrame;
            bool attacking;
            if (!link.TryGetAttackTimingV335LikeOriginal(out frame, out frameCount, out activeFrame, out attacking) || !attacking)
                return false;
            // NewMon.cpp::CheckIfNowAttack: NewCurSprite>ActiveFrame means the
            // current attack order is no longer protected by CreateOrder(3).
            return frame <= activeFrame;
        }

        private bool IsForeignOrderStillBusy()
        {
            if (_unit == null) return false;
            C2UnitOriginalRuntimeLinkLikeOriginal link = _unit.RuntimeLinkCachedLikeOriginal;
            if (link != null && link.Runtime != null)
            {
                if (link.Runtime.HasMoveTargetLikeOriginal ||
                    link.Runtime.MoveDeferredUntilNeutralStandLikeOriginal ||
                    link.Runtime.MovePathRealWaypointsLikeOriginal != null)
                    return true;
            }

            C2UnitOrderRuntimeV325LikeOriginal state =
                C2UnitOrderRuntimeV325LikeOriginal.TryGetLikeOriginal(_unit);
            if (state == null) return false;
            switch (state.CurrentLikeOriginal)
            {
                case C2UnitOrderKindV325LikeOriginal.Attack:
                case C2UnitOrderKindV325LikeOriginal.PreciseAttack:
                case C2UnitOrderKindV325LikeOriginal.UnitAttack:
                case C2UnitOrderKindV325LikeOriginal.RangedAttack:
                case C2UnitOrderKindV325LikeOriginal.MeleeAttack:
                case C2UnitOrderKindV325LikeOriginal.GrenadeAttack:
                case C2UnitOrderKindV325LikeOriginal.BuildApproach:
                case C2UnitOrderKindV325LikeOriginal.BuildWork:
                case C2UnitOrderKindV325LikeOriginal.ResourceApproach:
                case C2UnitOrderKindV325LikeOriginal.ResourceWork:
                case C2UnitOrderKindV325LikeOriginal.ResourceDeposit:
                case C2UnitOrderKindV325LikeOriginal.ResourceReturn:
                    return true;
            }
            return false;
        }

        private void CancelForeignOrdersForReplacement(string source)
        {
            if (_unit == null) return;
            if (_unit.RuntimeLinkCachedLikeOriginal?.Runtime != null)
                {
                _unit.RuntimeLinkCachedLikeOriginal.Runtime.ArtilleryChargeOrderV439 = -1;
                _unit.RuntimeLinkCachedLikeOriginal.Runtime.ArtilleryPointOrderV439 = null;
            }
            C2BattleTerrainMode.C2BuildRuntimeCancelWorkerOrderForUnitLikeOriginal(
                _unit, source ?? "c2_order_replace");
            C2GameplayUnitTaskV1 task = _unit.GetComponent<C2GameplayUnitTaskV1>();
            if (task != null && task.enabled)
                task.CancelForExternalOrderLikeOriginal(source ?? "c2_order_replace");
            C2CombatRuntimeV334LikeOriginal combat = _unit.GetComponent<C2CombatRuntimeV334LikeOriginal>();
            if (combat != null && combat.enabled)
                combat.CancelForExternalOrderLikeOriginal(source ?? "c2_order_replace");
        }

        private void TryStartHead()
        {
            if (_unit == null || _localOrder == null || _localOrder.Started) return;

            if (_localOrder.PreserveCurrentAttackFrame)
            {
                if (IsCurrentAttackFrameRunning()) return;
                _localOrder.PreserveCurrentAttackFrame = false;
                CancelForeignOrdersForReplacement(_localOrder.Source);
            }
            else if (_localOrder.WaitForForeignOrder)
            {
                // Only CreateOrder(Type=2) waits behind an already existing external
                // order. Type 0/1/3 must replace/start immediately; checking the old
                // runtime move here was the V350/V351 command-eating bug.
                if (IsForeignOrderStillBusy()) return;
                _localOrder.WaitForForeignOrder = false;
            }

            MoveOrder order = _localOrder;
            order.Started = true;
            if (order.IsStayForSomeTime || order.UnlimitedOrderV433 != 0)
                return;

            if (order.SmartSend)
            {
                // SmartSend is itself the LocalOrder. It does not install DestX yet;
                // NewMonsterSmartSendToLink chooses/pushes a PreciseSend sub-order.
                C2UnitOrderRuntimeV325LikeOriginal.IssueLikeOriginal(
                    _unit, C2UnitOrderKindV325LikeOriginal.Move,
                    order.Source, "NewMonsterSmartSendToLink");
                return;
            }

            C2UnitOrderRuntimeV325LikeOriginal.IssueLikeOriginal(
                _unit,
                C2UnitOrderKindV325LikeOriginal.Move,
                order.Source,
                order.HasFinalFacing ? "c2_order_move_with_facing" : "c2_order_move");

            C2UnitOriginalRuntimeLinkLikeOriginal link = _unit.RuntimeLinkCachedLikeOriginal;
            if (link != null && link.Runtime.PreciseBornPathLikeOriginal)
            {
                link.Owner.SetRuntimeMoveTargetOnlyLikeOriginal(link.Runtime,
                    order.DestRealX, order.DestRealY, order.SpeedOriginalPixelsPerSecond, false);
            }
            else if (order.HasPath && order.PathReal != null && order.PathReal.Length > 0 && link != null)
            {
                link.SetMovePathRealLikeOriginal(
                    order.PathReal,
                    order.SpeedOriginalPixelsPerSecond,
                    order.HasFinalFacing,
                    order.FinalFacing,
                    false,
                    order.Source);
            }
            else if (link != null && order.Precise &&
                     C2OriginalMovementMathV352.Norma(
                         Mathf.RoundToInt(order.DestRealX) - Mathf.RoundToInt(link.Runtime.RuntimeRealXLikeOriginal),
                         Mathf.RoundToInt(order.DestRealY) - Mathf.RoundToInt(link.Runtime.RuntimeRealYLikeOriginal)) < 1024)
            {
                // CII NewMon.cpp::NewMonsterPreciseSendToLink: dr<1024 writes
                // DestX/DestY directly. A formation correction is not SmartSend.
                // The motion handler still owns terrain/contact checks each step.
                link.Owner.InstallValidatedOrderDestinationV433LikeOriginal(link.Runtime,
                    order.DestRealX, order.DestRealY, order.SpeedOriginalPixelsPerSecond,
                    order.HasFinalFacing, order.FinalFacing, false, order.Source);
            }
            else if (link != null)
            {
                // A destination alone has no validated path. C2 SmartSend checks from
                // this object's position, including after a queued order starts.
                link.Owner.InstallOrderDestinationV433LikeOriginal(link.Runtime,
                    order.DestRealX, order.DestRealY,
                    order.SpeedOriginalPixelsPerSecond,
                    order.HasFinalFacing, order.FinalFacing);
            }
            else
            {
                _unit.SetMoveDestinationRealLikeOriginal(
                    order.DestRealX,
                    order.DestRealY,
                    order.SpeedOriginalPixelsPerSecond,
                    order.HasFinalFacing,
                    order.FinalFacing);
            }
        }

        private bool HeadMoveFinished()
        {
            if (_unit == null || _localOrder == null || !_localOrder.Started) return false;
            C2UnitOriginalRuntimeLinkLikeOriginal link = _unit.RuntimeLinkCachedLikeOriginal;
            if (link == null || link.Runtime == null) return true;
            C2UnitOriginalRuntime rt = link.Runtime;
            return !rt.OriginalPathRequestPendingV425LikeOriginal &&
                   !rt.HasMoveTargetLikeOriginal &&
                   !rt.MoveDeferredUntilNeutralStandLikeOriginal &&
                   rt.MovePathRealWaypointsLikeOriginal == null;
        }

        private void PushSmartPreciseV428LikeOriginal(MoveOrder smart, int destRealX, int destRealY, bool final)
        {
            if (smart == null) return;
            MoveOrder precise = new MoveOrder
            {
                DestRealX = destRealX,
                DestRealY = destRealY,
                Precise = true,
                HasPath = false,
                HasFinalFacing = final && smart.HasFinalFacing,
                FinalFacing = smart.FinalFacing,
                SpeedOriginalPixelsPerSecond = smart.SpeedOriginalPixelsPerSecond,
                Source = (smart.Source ?? "NewMonsterSmartSendTo") + (final ? "::final_PreciseSend" : "::next_PreciseSend")
            };
            precise.NextOrder = final ? smart.NextOrder : smart;
            _localOrder = precise;
            TryStartHead();
        }

        private void DeleteSmartHeadV428LikeOriginal(MoveOrder smart)
        {
            if (smart == null || _localOrder != smart) return;
            _localOrder = smart.NextOrder;
            TryStartHead();
        }

        private void CorrectUnitPositionBeforeSendV428LikeOriginal(ref int realX, ref int realY, byte lockType)
        {
            if (_unit == null) return;
            int lx = C2OriginalMovementSystemV425LikeOriginal.ResolveLxLikeOriginal(_unit);
            int xx = (realX - (lx << 7)) >> 8;
            int yy = (realY - (lx << 7)) >> 8;
            int oldX = xx, oldY = yy;
            if (C2OriginalMovementSystemV425LikeOriginal.FindBestPositionOldV425LikeOriginal(
                    _unit, ref xx, ref yy, 40, lockType) && (xx != oldX || yy != oldY))
            {
                realX = (xx << 8) + (lx << 7);
                realY = (yy << 8) + (lx << 7);
            }
        }

        private bool TopFindBestPositionV428LikeOriginal(ref int xd, ref int yd, int r0, int top, byte lockType)
        {
            if (_unit == null) return false;
            int lx = C2OriginalMovementSystemV425LikeOriginal.ResolveLxLikeOriginal(_unit);
            if (!C2OriginalMovementSystemV425LikeOriginal.CheckBarV425LikeOriginal(
                    xd - 1, yd - 1, lx + 1, lx + 1, lockType)) return true;

            C2UnitOriginalRuntimeLinkLikeOriginal link = _unit.RuntimeLinkCachedLikeOriginal;
            C2UnitOriginalRuntime rt = link != null ? link.Runtime : null;
            int realX = rt != null ? Mathf.RoundToInt(rt.RuntimeRealXLikeOriginal) : _unit.RealX;
            int realY = rt != null ? Mathf.RoundToInt(rt.RuntimeRealYLikeOriginal) : _unit.RealY;
            int ux = (realX - (lx << 7)) >> 8;
            int uy = (realY - (lx << 7)) >> 8;
            int bx = xd, by = yd, best = 100000;
            int xxx = bx - 1, yyy = by - 1, ll = 2, remain = r0;

            Action<int,int> test = delegate(int cx, int cy)
            {
                if (C2OriginalMovementSystemV425LikeOriginal.CheckBarV425LikeOriginal(
                        cx - 1, cy - 1, lx, lx, lockType)) return;
                int d = C2OriginalMovementMathV352.Norma(cx - ux, cy - uy);
                if (d < best && C2TopologyCoreV401LikeOriginal.GetWTopology1V428LikeOriginal(
                        cx, cy, lockType) == top)
                {
                    bx = cx; by = cy; best = d;
                }
            };

            while (remain != 0)
            {
                for (int i = 0; i <= ll; i++) test(xxx + i, yyy);
                for (int i = 0; i <= ll; i++) test(xxx + i, yyy + ll);
                for (int i = 0; i < ll - 1; i++) test(xxx, yyy + i + 1);
                for (int i = 0; i < ll - 1; i++) test(xxx + ll, yyy + i + 1);
                if (best < 100000) { xd = bx; yd = by; return true; }
                remain--; ll += 2; xxx--; yyy--;
            }
            return false;
        }

        private int FindSuperSmartBestPositionV428LikeOriginal(
            ref int cx, ref int cy, int dx, int dy, int top, byte lockType)
        {
            if (_unit == null) return 0;
            byte unitLockType = C2OriginalMovementSystemV425LikeOriginal.ResolveLockTypeV425LikeOriginal(_unit);
            int ldx = -2, llx = 4;
            if (unitLockType != 0) { ldx = -7; llx = 14; }
            int x0 = cx << 8, y0 = cy << 8;
            int n = (C2OriginalMovementMathV352.Norma(dx, dy) >> 5) + 1;
            int ddx = (dx << 8) / n, ddy = (dy << 8) / n;
            int mx = C2TopologyCoreV401LikeOriginal.TopLxLikeOriginal;
            int i = 0;
            bool prolong = true;

            if (lockType != 1)
            {
                for (i = 0; i < n; i++)
                {
                    x0 += ddx; y0 += ddy;
                    int tx = x0 >> 14, ty = y0 >> 14;
                    if (tx <= 0 || ty <= 0 || tx >= mx || ty >= mx ||
                        C2TopologyCoreV401LikeOriginal.GetTopFastV401LikeOriginal(tx, ty, lockType) >= 0xFFFE)
                        break;
                }
                if (i != n)
                {
                    x0 -= ddx; y0 -= ddy; prolong = false;
                }
                n = i;
                if (!C2OriginalMovementSystemV425LikeOriginal.CheckBarV425LikeOriginal(
                        (x0 >> 12) + ldx, (y0 >> 12) + ldx, llx, llx, lockType))
                { cx = x0 >> 8; cy = y0 >> 8; return 1; }

                int xx = x0, yy = y0;
                for (i = 0; i < n; i++)
                {
                    x0 -= ddx; y0 -= ddy;
                    if (!C2OriginalMovementSystemV425LikeOriginal.CheckBarV425LikeOriginal(
                            (x0 >> 12) + ldx, (y0 >> 12) + ldx, llx, llx, lockType))
                    { cx = x0 >> 8; cy = y0 >> 8; return 2; }
                }
                if (prolong)
                {
                    x0 = xx; y0 = yy;
                    for (i = 0; i < 5; i++)
                    {
                        x0 += ddx; y0 += ddy;
                        int tx = x0 >> 14, ty = y0 >> 14;
                        if (tx <= 0 || ty <= 0 || tx >= mx || ty >= C2TopologyCoreV401LikeOriginal.TopLyLikeOriginal ||
                            C2TopologyCoreV401LikeOriginal.GetTopFastV401LikeOriginal(tx, ty, lockType) >= 0xFFFE)
                            break;
                        if (!C2OriginalMovementSystemV425LikeOriginal.CheckBarV425LikeOriginal(
                                (x0 >> 12) + ldx, (y0 >> 12) + ldx, llx, llx, lockType))
                        { cx = x0 >> 8; cy = y0 >> 8; return 3; }
                    }
                }
            }

            int cellX = cx >> 4, cellY = cy >> 4;
            if (TopFindBestPositionV428LikeOriginal(ref cellX, ref cellY, 60, top, lockType))
            { cx = cellX << 4; cy = cellY << 4; return 4; }
            return 0;
        }

        private bool ProcessSmartSendHeadV428LikeOriginal(MoveOrder smart)
        {
            if (smart == null || !smart.SmartSend || _unit == null) return false;
            C2UnitOriginalRuntimeLinkLikeOriginal link = _unit.RuntimeLinkCachedLikeOriginal;
            C2UnitOriginalRuntime rt = link != null ? link.Runtime : null;
            if (rt == null) { DeleteSmartHeadV428LikeOriginal(smart); return true; }

            byte lockType = C2OriginalMovementSystemV425LikeOriginal.ResolveLockTypeV425LikeOriginal(_unit);
            byte nation = _unit.Nation;
            rt.OriginalUnitSpeedLikeOriginal = 64; // COSSACKS2 NewMonsterSmartSendToLink
            int x = smart.SmartX, y = smart.SmartY;
            int dx = smart.SmartDx, dy = smart.SmartDy;
            int finalTop = C2TopologyCoreV401LikeOriginal.GetWTopologyV428LikeOriginal(ref x, ref y, lockType);
            int curX = Mathf.RoundToInt(rt.RuntimeRealXLikeOriginal) >> 4;
            int curY = Mathf.RoundToInt(rt.RuntimeRealYLikeOriginal) >> 4;

            // First COSSACKS2 branch: if the current/next topology is a road pair,
            // CreateWayNet currently returns only the next road-knot point (the rest of
            // its historical body is unreachable after retail's early return).
            if (finalTop < 0xFFFE && _unit.GroundStateV396LikeOriginal != 1)
            {
                int currentTop = C2TopologyCoreV401LikeOriginal.GetWTopologyV428LikeOriginal(curX, curY, lockType);
                if (currentTop < 0xFFFE)
                {
                    int na = C2TopologyCoreV401LikeOriginal.GetNAreasV401LikeOriginal(lockType);
                    int nextTop = currentTop;
                    if (na > 0)
                        nextTop = C2TopologyCoreV401LikeOriginal.GetMotionLinksV401LikeOriginal(
                            finalTop + na * currentTop, lockType, nation);
                    int lx = C2OriginalMovementSystemV425LikeOriginal.ResolveLxLikeOriginal(_unit);
                    if (lx > 1 && !C2FormationRuntimeV167LikeOriginal.IsUnitInRuntimeFormationV168LikeOriginal(_unit))
                    {
                        for (int q = 0; q < 2 && nextTop < 0xFFFE && nextTop != finalTop; q++)
                            nextTop = C2TopologyCoreV401LikeOriginal.GetMotionLinksV401LikeOriginal(
                                finalTop + na * nextTop, lockType, nation);
                    }
                    if (nextTop < 0xFFFE &&
                        C2TopologyCoreV401LikeOriginal.CheckIfRoadZoneV425LikeOriginal(nextTop) &&
                        C2TopologyCoreV401LikeOriginal.CheckIfRoadZoneV425LikeOriginal(currentTop))
                    {
                        int wx, wy;
                        if (C2TopologyCoreV401LikeOriginal.GetPreciseTopCenterV425LikeOriginal(nextTop, lockType, out wx, out wy))
                        {
                            int rdx = wx - curX, rdy = wy - curY;
                            int nr = C2OriginalMovementMathV352.Norma(rdx, rdy);
                            if (nr < 64 && nr > 0)
                            { rdx = rdx * 64 / nr; rdy = rdy * 64 / nr; wx = curX + rdx; wy = curY + rdy; }

                            rt.OriginalUnitSpeedLikeOriginal =
                                (64 * C2TopologyCoreV401LikeOriginal.GetZoneSpeedBonusV426LikeOriginal(currentTop)) >> 8;
                            dx = Mathf.Clamp(dx, -64, 64);
                            dy = Mathf.Clamp(dy, -64, 64);
                            int ntp;
                            do
                            {
                                dx = dx * 3 / 4;
                                dy = dy * 3 / 4;
                                int px = wx - dx, py = wy + dy;
                                int t1 = C2TopologyCoreV401LikeOriginal.GetWTopologyV428LikeOriginal(px - 40, py - 40, lockType);
                                int t2 = C2TopologyCoreV401LikeOriginal.GetWTopologyV428LikeOriginal(px + 40, py - 40, lockType);
                                int t3 = C2TopologyCoreV401LikeOriginal.GetWTopologyV428LikeOriginal(px - 40, py + 40, lockType);
                                int t4 = C2TopologyCoreV401LikeOriginal.GetWTopologyV428LikeOriginal(px + 40, py + 40, lockType);
                                ntp = (t1 == t2 && t2 == t3 && t3 == t4) ? t1 : currentTop;
                            } while (ntp == currentTop && (dx != 0 || dy != 0));
                            smart.SmartDx = dx; smart.SmartDy = dy;
                            int destRealX = (wx - dx) << 4;
                            int destRealY = (wy + dy) << 4;
                            CorrectUnitPositionBeforeSendV428LikeOriginal(ref destRealX, ref destRealY, lockType);
                            PushSmartPreciseV428LikeOriginal(smart, destRealX, destRealY, false);
                            return true;
                        }
                    }
                }
            }

            if (smart.SmartNextTop == 0xFFFF)
            {
                smart.SmartNextTop = C2TopologyCoreV401LikeOriginal.GetWTopologyV428LikeOriginal(curX, curY, lockType);
                if (smart.SmartNextTop == 0xFFFF)
                { DeleteSmartHeadV428LikeOriginal(smart); return true; }
            }

            if (C2TopologyCoreV401LikeOriginal.CheckTopDirectWayV428LikeOriginal(
                    curX, curY, x + dx, y + dy, lockType))
            {
                PushSmartPreciseV428LikeOriginal(smart, (x + dx) << 4, (y + dy) << 4, true);
                return true;
            }
            if (finalTop >= 0xFFFE)
            { DeleteSmartHeadV428LikeOriginal(smart); return true; }

            // Source stores the corrected GetWTopology(&x,&y) centre only here, after
            // the road/direct branches have returned.
            smart.SmartX = x;
            smart.SmartY = y;

            int areas = C2TopologyCoreV401LikeOriginal.GetNAreasV401LikeOriginal(lockType);
            if (areas <= 0) { DeleteSmartHeadV428LikeOriginal(smart); return true; }
            int nextNextTop = C2TopologyCoreV401LikeOriginal.GetMotionLinksV401LikeOriginal(
                finalTop + areas * smart.SmartNextTop, lockType, nation);
            if (nextNextTop == finalTop || finalTop == smart.SmartNextTop)
            {
                int fx = x, fy = y;
                if (FindSuperSmartBestPositionV428LikeOriginal(ref fx, ref fy, dx, dy, finalTop, lockType) == 0)
                { DeleteSmartHeadV428LikeOriginal(smart); return true; }
                PushSmartPreciseV428LikeOriginal(smart, fx << 4, fy << 4, true);
                return true;
            }
            if (nextNextTop != 0xFFFF)
            {
                int maxPre = 3;
                int cox = curX, coy = curY;
                do
                {
                    int next2 = C2TopologyCoreV401LikeOriginal.GetMotionLinksV401LikeOriginal(
                        finalTop + areas * nextNextTop, lockType, nation);
                    if (next2 != 0xFFFF)
                    {
                        C2TopologyCoreV401LikeOriginal.AreaV401LikeOriginal ar2 =
                            C2TopologyCoreV401LikeOriginal.GetTopMapV401LikeOriginal(next2, 0);
                        if (ar2 == null) { maxPre = 0; break; }
                        int nx = (ar2.x << 6) + 32, ny = (ar2.y << 6) + 32;
                        if (C2TopologyCoreV401LikeOriginal.CheckTopDirectWayV428LikeOriginal(
                                cox, coy, nx + dx, ny + dy, lockType))
                        { nextNextTop = next2; maxPre--; }
                        else maxPre = 0;
                    }
                    else maxPre = 0;
                } while (maxPre != 0);

                C2TopologyCoreV401LikeOriginal.AreaV401LikeOriginal area =
                    C2TopologyCoreV401LikeOriginal.GetTopMapV401LikeOriginal(nextNextTop, lockType);
                if (area == null) { DeleteSmartHeadV428LikeOriginal(smart); return true; }
                int sx = (area.x << 6) + 32, sy = (area.y << 6) + 32;
                if (FindSuperSmartBestPositionV428LikeOriginal(ref sx, ref sy, dx, dy, nextNextTop, lockType) == 0)
                { DeleteSmartHeadV428LikeOriginal(smart); return true; }
                smart.SmartNextX = sx;
                smart.SmartNextY = sy;
                smart.SmartNextTop = nextNextTop;
                PushSmartPreciseV428LikeOriginal(smart, sx << 4, sy << 4, false);
                return true;
            }

            DeleteSmartHeadV428LikeOriginal(smart);
            return true;
        }

        private void TickOrderNodeLikeOriginal()
        {
            if (_localOrder == null) return;

            if (!_localOrder.Started)
            {
                TryStartHead();
                return;
            }

            if (_localOrder.UnlimitedOrderV433 != 0)
            {
                TickUnlimitedOrderV433LikeOriginal();
                return;
            }
            if (_localOrder.IsStayForSomeTime)
            {
                // StayForSomeTimeLink: Progress -= GameSpeed (256) each original
                // simulation quantum; delete the head only after Progress < 0.
                _localOrder.StayProgress256 -= 256;
                if (_localOrder.StayProgress256 < 0)
                {
                    _localOrder = _localOrder.NextOrder;
                    TryStartHead();
                }
                return;
            }

            if (_localOrder.SmartSend)
            {
                // NewMonsterSmartSendToLink owns the topology/road step selection.
                // It either replaces the SmartSend head by a final PreciseSend or
                // pushes one intermediate PreciseSend in front of the same SmartSend.
                ProcessSmartSendHeadV428LikeOriginal(_localOrder);
                return;
            }

            // NewMon.cpp::MoveToXYLink/NewMonsterPreciseSendToLink call CreatePath
            // again on every simulation quantum while the order is still farther than
            // its completion distance.  Do the same here so ReallyCreatePath owns
            // PathX/NIPoints reuse, PathDelay and obstruction-triggered rebuilds rather
            // than a Unity timer-based route refresh.
            if (!_localOrder.HasPath && _unit != null)
            {
                C2UnitOriginalRuntimeLinkLikeOriginal refreshLink = _unit.RuntimeLinkCachedLikeOriginal;
                C2UnitOriginalRuntime refreshRt = refreshLink != null ? refreshLink.Runtime : null;
                if (refreshRt != null)
                {
                    if (refreshRt.OriginalPathDelayV425LikeOriginal != 0 && refreshRt.OriginalStandTimeV425LikeOriginal > 64)
                    {
                        refreshLink.Owner.FinishOrdinaryOrderV433LikeOriginal(refreshRt,false);
                        _localOrder=_localOrder.NextOrder;TryStartHead();return;
                    }
                    if (!_localOrder.Precise && !_localOrder.BuildCellTargetV434 && C2RetailRandomV407LikeOriginal.Rando(_unit) < 2048)
                    {
                        float x=_localOrder.DestRealX,y=_localOrder.DestRealY;
                        C2OriginalMovementSystemV425LikeOriginal.CorrectOrdinaryDestinationV433LikeOriginal(_unit,ref x,ref y);
                        _localOrder.DestRealX=x;_localOrder.DestRealY=y;
                    }
                    int ddx = Mathf.RoundToInt(_localOrder.DestRealX - refreshRt.RuntimeRealXLikeOriginal);
                    int ddy = Mathf.RoundToInt(_localOrder.DestRealY - refreshRt.RuntimeRealYLikeOriginal);
                    int dr = C2OriginalMovementMathV352.Norma(ddx, ddy);
                    // NewMonsterSendToLink completes by native object-cell DistTo,
                    // with STOPDISTANCE and the MotionStyle-specific frame gate.
                    if (!_localOrder.Precise && !_localOrder.BuildCellTargetV434)
                    {
                        int lx = C2OriginalMovementSystemV425LikeOriginal.ResolveLxLikeOriginal(_unit);
                        int cx = (Mathf.RoundToInt(refreshRt.RuntimeRealXLikeOriginal)-(lx<<7))>>8;
                        int cy = (Mathf.RoundToInt(refreshRt.RuntimeRealYLikeOriginal)-(lx<<7))>>8;
                        int tx = (Mathf.RoundToInt(_localOrder.DestRealX)-(lx<<7))>>8;
                        int ty = (Mathf.RoundToInt(_localOrder.DestRealY)-(lx<<7))>>8;
                        string ms = refreshRt.Md.MotionStyle;
                        int r = (refreshRt.Md.StopDistance>>8) + (ms=="COMPLEXOBJECT" ? 2 : 0);
                        if (Math.Max(Math.Abs(cx-tx),Math.Abs(cy-ty)) <= r &&
                            (ms=="SINGLESTEP" || ms=="SHEEPS" || refreshRt.FrameFinishedLikeOriginal))
                        {
                            refreshLink.Owner.FinishOrdinaryOrderV433LikeOriginal(refreshRt,ms!="SINGLESTEP");
                            _localOrder = _localOrder.NextOrder;
                            TryStartHead();
                            return;
                        }
                    }
                    bool preciseDirect = _localOrder.Precise && (dr < 1024 || refreshRt.PreciseBornPathLikeOriginal);
                    if (_localOrder.Precise)
                    {
                        if (refreshRt.OriginalPathDelayV425LikeOriginal != 0 &&
                            refreshRt.OriginalStandTimeV425LikeOriginal > 64)
                        {
                            _localOrder = _localOrder.NextOrder;
                            TryStartHead();
                            return;
                        }
                        int drm = refreshRt.Md != null ? refreshRt.Md.MotionDist : 0;
                        if (refreshRt.OriginalComplexObjectV430LikeOriginal != null)
                        {
                            int dirDelta = (sbyte)(_unit.RealDir - C2OriginalMovementMathV352.GetDir(ddx, ddy));
                            drm = Math.Max(65, Math.Abs(dirDelta) < 32 ? drm * 3 : drm << 3);
                        }
                        if (dr < drm)
                        {
                            refreshLink.Owner.FinishPreciseOrderV433LikeOriginal(refreshRt,
                                _localOrder.DestRealX, _localOrder.DestRealY,
                                _localOrder.HasFinalFacing, _localOrder.FinalFacing);
                            _localOrder = _localOrder.NextOrder;
                            TryStartHead();
                            return;
                        }
                        if (preciseDirect)
                        {
                            C2OriginalMovementSystemV425LikeOriginal.DisablePathRequestV425LikeOriginal(_unit);
                            refreshRt.MovePathRealWaypointsLikeOriginal = null;
                            refreshRt.MovePathIndexLikeOriginal = 0;
                            if (!refreshRt.MoveDeferredUntilNeutralStandLikeOriginal &&
                                (!refreshRt.HasMoveTargetLikeOriginal ||
                                 refreshRt.MoveTargetRealXLikeOriginal != _localOrder.DestRealX ||
                                 refreshRt.MoveTargetRealYLikeOriginal != _localOrder.DestRealY))
                                refreshLink.Owner.SetRuntimeMoveTargetOnlyLikeOriginal(refreshRt,
                                    _localOrder.DestRealX, _localOrder.DestRealY,
                                    _localOrder.SpeedOriginalPixelsPerSecond, false);
                        }
                    }
                    if (!preciseDirect && dr > 16)
                    {
                        C2OriginalMovementSystemV425LikeOriginal.AddPathRequestorV425LikeOriginal(
                            _unit,
                            _localOrder.DestRealX,
                            _localOrder.DestRealY,
                            _localOrder.SpeedOriginalPixelsPerSecond,
                            _localOrder.HasFinalFacing,
                            _localOrder.FinalFacing,
                            _localOrder.Source ?? "MoveToXYLink::CreatePath");
                    }
                }
            }

            // Native PreciseSend finishes by distance to its exact order target,
            // never just because the path's last CELL has been consumed.
            if (_localOrder.Precise && !_localOrder.HasPath) return;
            if (!HeadMoveFinished()) return;

            _localOrder = _localOrder.NextOrder;
            TryStartHead();
        }

    }
}


// ============================================================================
// MERGED FROM: C2FormationTurnV360LikeOriginal.cs
// ============================================================================
namespace Cossacks2Bridge.UnityAdapters.Maps
{
    internal static partial class C2FormationRuntimeV167LikeOriginal
    {
        // Like Brigade::Memb: vacancies keep their indices until an explicit
        // regroup/fill operation. An ordinary move or turn must not compact them.
        private static List<C2NeutralPeasantUnitInfoV2LikeOriginal> GetFormationOrderMembersV360LikeOriginal(
            RuntimeFormationV172LikeOriginal group)
        {
            var result = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>(group.Units.Count);
            for (int i = 0; i < group.Units.Count; i++)
            {
                var unit = group.Units[i];
                // Brigade::Memb is persistent state. Ordinary movement/turning must
                // not drop a live member merely because it is temporarily
                // NotSelectable or cannot currently receive player orders.
                // Death/removal paths are responsible for clearing membership.
                bool aliveMember = unit != null && !unit.IsDeadLikeOriginal;
                result.Add(aliveMember ? unit : null);
            }
            return result;
        }

        private static int ResolveOrderCommandCountV360LikeOriginal(
            RuntimeFormationV172LikeOriginal group, IList<C2NeutralPeasantUnitInfoV2LikeOriginal> members)
        {
            return group.CommandSlotCount >= 0
                ? Math.Min(group.CommandSlotCount, members.Count)
                : ResolveCommandPrefixCountV320LikeOriginal(group, members);
        }

        private static byte GetFormationPhysicalDirectionV360LikeOriginal(
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOrderTemplateV165LikeOriginal template,
            IList<C2NeutralPeasantUnitInfoV2LikeOriginal> members, int commands)
        {
            var positions = new C2FormationSymmetryLikeOriginal.MemberPosition[members.Count];
            for (int i = 0; i < members.Count; i++)
            {
                var unit = members[i];
                if (unit == null) continue;
                positions[i] = new C2FormationSymmetryLikeOriginal.MemberPosition {
                    Present = true, RealX = unit.RealX, RealY = unit.RealY
                };
            }
            return C2FormationSymmetryLikeOriginal.DirectionByPositions(template, positions, commands);
        }

        private static bool ApplyFormationSymmetricMoveV360LikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> members,
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOrderTemplateV165LikeOriginal template,
            byte requestedDirection)
        {
            int commands = ResolveOrderCommandCountV360LikeOriginal(group, members);
            if (template == null || template.SymInv == null ||
                members.Count - commands > template.UnitCount || members.Count <= commands) return false;
            byte physical = GetFormationPhysicalDirectionV360LikeOriginal(template, members, commands);
            byte next;
            int[] swap = C2FormationSymmetryLikeOriginal.SelectTurnSwap(
                template, group.Direction, physical, requestedDirection, out next);
            // Symmetric orders reach the requested direction in this step. Orders
            // without symmetry still need the full GoTo/KeepPositions sequence.
            ApplyFormationTurnSwapV360LikeOriginal(members, commands, swap);
            return swap != null;
        }

        private static void ApplyFormationTurnSwapV360LikeOriginal(
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> members, int commands, int[] swap)
        {
            if (swap == null) return;
            var soldiers = members.GetRange(commands, members.Count - commands);
            var reordered = C2FormationSymmetryLikeOriginal.ApplySoldierSwap(soldiers, swap);
            members.RemoveRange(commands, members.Count - commands);
            members.AddRange(reordered);
        }
    }
}


// ============================================================================
// MERGED FROM: C2BrigadeGlobalMoveV419LikeOriginal.cs
// ============================================================================
namespace Cossacks2Bridge.UnityAdapters.Maps
{
    internal static partial class C2FormationRuntimeV167LikeOriginal
    {
        // BrigadeOrders.cpp::BrigadeOrder_HumanGlobalSendTo owns these values.
        // Store them on the NewBOrder node so a pushed KeepPositions suspends,
        // then resumes, the same topology order. No member path arrays.
        private sealed class BrigadeGlobalMoveV419LikeOriginal
        {
            internal int X, Y, NextX = 0xFFFF, NextY = 0xFFFF, NextTop = 0xFFFF;
            internal short Direction;
            internal byte Usage;
        }

        private static void QueueBrigadeGlobalMoveV419LikeOriginal(
            RuntimeFormationV172LikeOriginal group, float realX, float realY,
            short direction, byte priority, byte ordType, byte usage, string source)
        {
            int serial = CreateBrigadeNewOrderV418LikeOriginal(group,
                BrigadeOrderHumanGlobalSendToV418LikeOriginal, ordType, priority, source);
            var node = FindBrigadeNewOrderNodeBySerialV418LikeOriginal(group, serial);
            node.GlobalMove = new BrigadeGlobalMoveV419LikeOriginal {
                X = Mathf.RoundToInt(realX) >> 4, Y = Mathf.RoundToInt(realY) >> 4,
                Direction = direction, Usage = usage
            };
        }

        private static void TickBrigadeGlobalMovesV419LikeOriginal()
        {
            foreach (var pair in _groupsByIdV172LikeOriginal)
                ProcessBrigadeGlobalMoveV419LikeOriginal(pair.Value);
        }

        private static void ProcessBrigadeGlobalMoveV419LikeOriginal(RuntimeFormationV172LikeOriginal group)
        {
            if (group == null || !_brigadeNewBOrderV418LikeOriginal.TryGetValue(group.GroupId, out var node) ||
                node.OrderId != BrigadeOrderHumanGlobalSendToV418LikeOriginal || node.GlobalMove == null)
                return;
            var order = node.GlobalMove;
            // CalcCenter: actual soldier positions, excluding NBPERSONAL.
            long sx = 0, sy = 0;
            int n = 0;
            int first = ResolveOrderCommandCountV360LikeOriginal(group, group.Units);
            for (int i = first; i < group.Units.Count; i++)
            {
                var unit = group.Units[i];
                if (unit == null || unit.IsDeadLikeOriginal) continue;
                sx += Mathf.RoundToInt(RealXV407LikeOriginal(unit));
                sy += Mathf.RoundToInt(RealYV407LikeOriginal(unit));
                n++;
            }
            if (n == 0)
            {
                DeleteBrigadeNewOrderV418LikeOriginal(group, node.OrderId, "HumanGlobalSendTo_no_members");
                return;
            }
            int cx = (int)(sx / n) >> 4, cy = (int)(sy / n) >> 4;
            int x = order.X, y = order.Y;
            if (!C2TopologyCoreV401LikeOriginal.ReadyLikeOriginal) return;
            C2NeutralPeasantUnitInfoV2LikeOriginal topologyReference = null;
            int topologyFirst = Mathf.Clamp(ResolveOrderCommandCountV360LikeOriginal(group, group.Units), 0, group.Units.Count);
            for (int i = topologyFirst; i < group.Units.Count && topologyReference == null; i++)
                if (IsUsableFormationUnitV172LikeOriginal(group.Units[i], true)) topologyReference = group.Units[i];
            if (topologyReference == null)
                for (int i = 0; i < topologyFirst && topologyReference == null; i++)
                    if (IsUsableFormationUnitV172LikeOriginal(group.Units[i], true)) topologyReference = group.Units[i];
            if (topologyReference == null) return;
            byte lockType = C2OriginalMovementSystemV425LikeOriginal.ResolveLockTypeV425LikeOriginal(topologyReference);
            int currentTop = C2TopologyCoreV401LikeOriginal.GetTopologyV401LikeOriginal(cx, cy, lockType);
            int finalTop = C2TopologyCoreV401LikeOriginal.GetTopologyV401LikeOriginal(ref x, ref y, lockType);

            // BrigadeOrders.cpp::BrigadeOrder_HumanGlobalSendTo::Process checks the
            // road topology before CheckBDirectWay.  This is where CII decides whether
            // to approach a road knot or push BrigadeOrder_GoOnRoad over this order.
            if (TryProcessHumanGlobalRoadBranchV426LikeOriginal(
                    group, node, order, cx, cy, x, y, currentTop, finalTop))
                return;

            if (CheckBrigadeDirectWayV419LikeOriginal(cx, cy, x, y, lockType))
            {
                DeleteBrigadeNewOrderV418LikeOriginal(group, node.OrderId, "HumanGlobalSendTo_direct");
                IssueBrigadeHumanLocalSendToV4183LikeOriginal(group, x << 4, y << 4,
                    order.Direction, node.Priority, 1, "HumanGlobalSendTo_CheckBDirectWay", out _, out _);
                return;
            }

            // BrigadeOrders.cpp initializes NextTop only AFTER the road and direct
            // branches. GoOnRoad suspends this same HGST node: caching the start
            // topology before that suspension sends the brigade back toward its
            // old location when the road processor finishes.
            if (order.NextTop == 0xFFFF) order.NextTop = currentTop;
            if (order.NextTop == 0xFFFF || finalTop == 0xFFFF)
            {
                DeleteBrigadeNewOrderV418LikeOriginal(group, node.OrderId, "HumanGlobalSendTo_invalid_topology");
                return;
            }
            order.X = x; order.Y = y;

            int areas = C2TopologyCoreV401LikeOriginal.GetNAreasV401LikeOriginal(lockType);
            byte nation = 0;
            for (int i = 0; i < group.Units.Count; i++)
                if (group.Units[i] != null) { nation = group.Units[i].Nation; break; }
            int next = C2TopologyCoreV401LikeOriginal.GetMotionLinksV401LikeOriginal(finalTop + areas * order.NextTop, lockType, nation);
            if (next == finalTop || finalTop == order.NextTop)
            {
                DeleteBrigadeNewOrderV418LikeOriginal(group, node.OrderId, "HumanGlobalSendTo_final_topology");
                IssueBrigadeHumanLocalSendToV4183LikeOriginal(group, x << 4, y << 4,
                    order.Direction, node.Priority, 1, "HumanGlobalSendTo_final", out _, out _);
                return;
            }
            if (next == 0xFFFF)
            {
                DeleteBrigadeNewOrderV418LikeOriginal(group, node.OrderId, "HumanGlobalSendTo_no_link");
                return;
            }
            int maxPre = 5;
            bool nSteps = false; // Source declares bool, so after ++ it stays true.
            do
            {
                int next2 = C2TopologyCoreV401LikeOriginal.GetMotionLinksV401LikeOriginal(finalTop + areas * next, lockType, nation);
                if (next2 != 0xFFFF)
                {
                    var area2 = C2TopologyCoreV401LikeOriginal.GetTopMapV401LikeOriginal(next2, lockType);
                    if (area2 == null) break;
                    int nx = (area2.x << 6) + 32, ny = (area2.y << 6) + 32;
                    if (CheckBrigadeDirectWayV419LikeOriginal(cx, cy, nx, ny, lockType) || !nSteps)
                    { next = next2; maxPre--; }
                    else maxPre = 0;
                }
                else maxPre = 0;
                nSteps = true;
            } while (maxPre != 0);
            var area = C2TopologyCoreV401LikeOriginal.GetTopMapV401LikeOriginal(next, lockType);
            if (area == null)
            {
                DeleteBrigadeNewOrderV418LikeOriginal(group, node.OrderId, "HumanGlobalSendTo_missing_area");
                return;
            }
            order.NextX = (area.x << 6) + 32;
            order.NextY = (area.y << 6) + 32;
            order.NextTop = next;
            IssueBrigadeHumanLocalSendToV4183LikeOriginal(group, order.NextX << 4, order.NextY << 4,
                512, node.Priority, 1, "HumanGlobalSendTo_next_topology", out _, out _);
        }

        private static bool TryProcessHumanGlobalRoadBranchV426LikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            BrigadeNewOrderNodeV418LikeOriginal node,
            BrigadeGlobalMoveV419LikeOriginal order,
            int cx, int cy, int x, int y, int currentTop, int finalTop)
        {
            if (group == null || node == null || order == null ||
                (order.Usage != 1 && order.Usage != 3)) return false;

            C2NeutralPeasantUnitInfoV2LikeOriginal reference = null;
            int first = Mathf.Clamp(ResolveOrderCommandCountV360LikeOriginal(group, group.Units), 0, group.Units.Count);
            for (int i = first; i < group.Units.Count && reference == null; i++)
                if (IsUsableFormationUnitV172LikeOriginal(group.Units[i], true)) reference = group.Units[i];
            if (reference == null) return false;
            byte lockType = C2OriginalMovementSystemV425LikeOriginal.ResolveLockTypeV425LikeOriginal(reference);
            byte nation = (byte)(group.Nation & 0xFF);
            int na = C2TopologyCoreV401LikeOriginal.GetNAreasV401LikeOriginal(lockType);
            if (na <= 0 || currentTop >= 0xFFFE || finalTop >= 0xFFFE) return false;

            if (!C2TopologyCoreV401LikeOriginal.CheckIfRoadZoneV425LikeOriginal(currentTop))
            {
                int ntp = currentTop;
                int attempts = 0;
                do
                {
                    int previous = ntp;
                    ntp = C2TopologyCoreV401LikeOriginal.GetMotionLinksV401LikeOriginal(
                        finalTop + na * ntp, lockType, nation);
                    if (ntp < 0xFFFE && ntp != finalTop &&
                        C2TopologyCoreV401LikeOriginal.CheckIfRoadZoneV425LikeOriginal(ntp))
                    {
                        int rx, ry;
                        if (C2TopologyCoreV401LikeOriginal.GetPreciseTopCenterV425LikeOriginal(
                                ntp, lockType, out rx, out ry))
                        {
                            QueueBrigadeGlobalMoveV419LikeOriginal(
                                group, rx << 4, ry << 4, 512,
                                (byte)(node.Priority | 128), 1, order.Usage,
                                "HumanGlobalSendTo_approach_road");
                            if (node.Priority == 128)
                                _brigadeAttackEnemyIntentV414LikeOriginal.Add(group.GroupId);
                            return true;
                        }
                    }
                    attempts++;
                    if (ntp >= 0xFFFE || ntp == finalTop || ntp == previous) break;
                } while (attempts < 10);
                return false;
            }

            int ntop = C2TopologyCoreV401LikeOriginal.GetMotionLinksV401LikeOriginal(
                finalTop + na * currentTop, lockType, nation);
            if (ntop >= 0xFFFE || !C2TopologyCoreV401LikeOriginal.CheckIfRoadZoneV425LikeOriginal(ntop))
                return false;
            int ntop1 = C2TopologyCoreV401LikeOriginal.GetMotionLinksV401LikeOriginal(
                finalTop + na * ntop, lockType, nation);
            bool b1 = ntop1 < 0xFFFE && ntop1 != finalTop &&
                      C2TopologyCoreV401LikeOriginal.CheckIfRoadZoneV425LikeOriginal(ntop1);

            int ctop3 = C2TopologyCoreV401LikeOriginal.GetTopologyV401LikeOriginal(cx, cy, 3);
            int finalTop3 = C2TopologyCoreV401LikeOriginal.GetTopologyV401LikeOriginal(x, y, 3);
            int na3 = C2TopologyCoreV401LikeOriginal.GetNAreasV401LikeOriginal(3);
            int topDistance = 0;
            if (ctop3 < 0xFFFE && finalTop3 < 0xFFFE && na3 > 0)
                topDistance = C2TopologyCoreV401LikeOriginal.GetLinksDistV401LikeOriginal(
                    finalTop3 + na3 * ctop3, 3, nation) << 6;

            int minTopDistance = 800;
            C2UnitOriginalRuntimeLinkLikeOriginal link = reference.RuntimeLinkCachedLikeOriginal;
            if (link != null && link.Runtime != null && link.Runtime.Md != null &&
                link.Runtime.Md.MinTopDistanceToEnterRoad > 0)
                minTopDistance = link.Runtime.Md.MinTopDistanceToEnterRoad;

            if (!b1 || topDistance <= minTopDistance) return false;

            if (_brigadeAttackEnemyIntentV414LikeOriginal.Contains(group.GroupId))
            {
                ClearBrigadeAttackEnemyIntentV414LikeOriginal(group);
                for (int i = first; i < group.Units.Count; i++)
                {
                    C2NeutralPeasantUnitInfoV2LikeOriginal u = group.Units[i];
                    if (u == null) continue;
                    SetStandStateV403LikeOriginal(u, 0);
                }
            }

            string audit;
            return StartBrigadeGoOnRoadFromGlobalV426LikeOriginal(
                group, finalTop, node.Priority, "BrigadeOrder_HumanGlobalSendTo::GoOnRoad", out audit) > 0;
        }

        // Brigade.cpp::CheckBDirectWay integer sampling and five CheckPt probes.
        // The existing motion-field adapter supplies live building locks + water.
        private static bool CheckBrigadeDirectWayV419LikeOriginal(int x0, int y0, int x1, int y1, byte lockType)
        {
            int bx = x0 << 10, by = y0 << 10;
            int lx = x1 - x0, ly = y1 - y0;
            int n = (C2OriginalMovementMathV352.Norma(lx, ly) >> 6) + 1;
            lx = (lx << 10) / n; ly = (ly << 10) / n;
            for (int i = 0; i < n; i++)
            {
                bx += lx; by += ly;
                int xx = bx >> 14, yy = by >> 14;
                if (BrigadeMotionPointBlockedV419LikeOriginal(xx, yy, lockType) ||
                    BrigadeMotionPointBlockedV419LikeOriginal(xx - 8, yy, lockType) ||
                    BrigadeMotionPointBlockedV419LikeOriginal(xx + 8, yy, lockType) ||
                    BrigadeMotionPointBlockedV419LikeOriginal(xx, yy - 8, lockType) ||
                    BrigadeMotionPointBlockedV419LikeOriginal(xx, yy + 8, lockType)) return false;
            }
            return true;
        }

        private static bool BrigadeMotionPointBlockedV419LikeOriginal(int x, int y, byte lockType)
        {
            return C2OriginalMovementSystemV425LikeOriginal.CheckPtV425LikeOriginal(x, y, lockType);
        }
    }
}


// ============================================================================
// MERGED FROM: C2BrigadeOrderGoOnRoadV385A.cs
// ============================================================================
namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // V385A/V385B: stateful brigade-owned road order + original-style terminal handoff.
    //
    // Source correspondence:
    //   COSSACKS2/BrigadeOrders.cpp::BrigadeOrder_GoOnRoad::Init
    //   COSSACKS2/BrigadeOrders.cpp::BrigadeOrder_GoOnRoad::Process/ProcessPre
    //   COSSACKS2/BrigadeOrders.cpp::BrigadeOrder_GoOnRoad::SetNextPosAndDest
    //   COSSACKS2/BrigadeOrders.cpp::BrigadeOrder_GoOnRoad::GetUnitCoordInColumn
    //   COSSACKS2/BrigadeOrders.cpp::BrigadeOrder_GoOnRoad::SetUnitsSpeed
    //   COSSACKS2/BrigadeOrders.cpp::BrigadeOrder_GoOnRoad::SortBrigUnits
    //
    // The road order remains one stateful brigade order. V426/V428 restore the
    // topology-streaming reservation (LockNextPoint/SetPreLock) and the native
    // GoAway/AskGoAway clearance path instead of converting the road to N unit paths.
    internal static partial class C2FormationRuntimeV167LikeOriginal
    {
        private sealed class C2BrigadeOrderGoOnRoadStateV385A
        {
            public int GroupId;
            public string OriginalShape = string.Empty;
            public string Source = string.Empty;
            public byte OrdType;
            public byte Priority;
            public byte FinalDirection;
            public int CommandPrefix;

            public Vector2[] PointsReal;
            public byte[] PointsDir;
            public int[] ShiftInColumn;
            public int HeadIndex;
            public int MaxPIndex;
            public int UnitsInLine;
            public int DistInColumn;
            public int PassNPoints;
            public int NLines;
            // routePoints[0] is the off-road start and the last point is the final
            // battlefield destination.  The interior interval is the actual road path.
            public int FirstRoadPointIndex;
            public int LastRoadPointIndex;
            public int DestTopZone = -1;
            public int HeadTopZoneIndex = -1;
            public int Owner;
            public byte LockType;
            public ushort RoadFlag;
            public int SimTick;
            public int LastMoveTick;
            public int LastStepTick;
            public bool IsEnemyOnWay;
            public bool FirstStep;
            public bool OffPlaceHead;
            public C2NeutralPeasantUnitInfoV2LikeOriginal HeadUnit;

            public readonly List<Vector2> FinalSlots = new List<Vector2>();
            public float FinalCenterX;
            public float FinalCenterY;
            public float NextRecoveryAt;
            public float NextAuditAt;
            public int LastAuditHeadIndex = -1;
        }

        private static readonly Dictionary<int, C2BrigadeOrderGoOnRoadStateV385A>
            _roadOrdersV385A = new Dictionary<int, C2BrigadeOrderGoOnRoadStateV385A>();

        private const int C2RoadSpeedV385A = 96;       // BrigadeOrders.cpp: (64+32)
        private const int C2RoadMaxSpeedV385A = 240;  // BrigadeOrders.cpp: MaxSpdLimit
        private const int C2RoadMinSpeedV385A = 32;

        private sealed class C2RoadEdgeRuntimeV426LikeOriginal
        {
            public int Key;
            public int StartKnot;
            public int EndKnot;
            public Vector2[] P = Array.Empty<Vector2>();
            public byte[] Dir = Array.Empty<byte>();
            public uint Owner = 0xFFFFFFFFu;
            public bool OwnerDirection;
            public int LockTime;
            public ushort PreLock;
            public int PreLockTime;
            public uint PreLockRequestor = 0xFFFFFFFFu;
        }

        private static readonly Dictionary<int, C2RoadEdgeRuntimeV426LikeOriginal>
            _roadEdgeRuntimeV426LikeOriginal = new Dictionary<int, C2RoadEdgeRuntimeV426LikeOriginal>();

        private static int RoadAnimTimeV426LikeOriginal
        {
            get { return unchecked(CurrentSimulationTickV403ELikeOriginal << 8); }
        }

        private static bool TryGetRoadEdgeRuntimeV426LikeOriginal(
            int from, int to, out C2RoadEdgeRuntimeV426LikeOriginal edge)
        {
            edge = null;
            C2BattleTerrainMode mode = UnityEngine.Object.FindFirstObjectByType<C2BattleTerrainMode>(FindObjectsInactive.Exclude);
            if (mode == null) return false;
            int key, start, end;
            Vector2[] p; byte[] dir;
            if (!mode.C2MovementTryBuildRoadEdgeGeometryV426LikeOriginal(
                    from, to, out key, out start, out end, out p, out dir)) return false;
            if (!_roadEdgeRuntimeV426LikeOriginal.TryGetValue(key, out edge) || edge == null ||
                edge.StartKnot != start || edge.EndKnot != end)
            {
                edge = new C2RoadEdgeRuntimeV426LikeOriginal {
                    Key = key, StartKnot = start, EndKnot = end, P = p, Dir = dir
                };
                _roadEdgeRuntimeV426LikeOriginal[key] = edge;
            }
            return true;
        }

        private static int GetRoadPreLockV426LikeOriginal(C2RoadEdgeRuntimeV426LikeOriginal edge)
        {
            if (edge == null) return 0;
            if (edge.PreLock != 0 && unchecked(edge.PreLockTime + 64 * 256) < RoadAnimTimeV426LikeOriginal)
                edge.PreLock = 0;
            return edge.PreLock;
        }

        private static int SetRoadPreLockV426LikeOriginal(int from, int to, uint requestor)
        {
            C2RoadEdgeRuntimeV426LikeOriginal edge;
            if (!TryGetRoadEdgeRuntimeV426LikeOriginal(from, to, out edge)) return 0;
            GetRoadPreLockV426LikeOriginal(edge);
            if (edge.PreLock < ushort.MaxValue) edge.PreLock++;
            edge.PreLockTime = RoadAnimTimeV426LikeOriginal;
            edge.PreLockRequestor = requestor;
            return edge.PreLock;
        }

        private static bool IsRoadOwnerOnRoadV426LikeOriginal(C2RoadEdgeRuntimeV426LikeOriginal edge)
        {
            if (edge == null || edge.Owner == 0xFFFFFFFFu) return false;
            if (unchecked(edge.LockTime + 100 * 256) > RoadAnimTimeV426LikeOriginal) return true;
            int nation = (int)((edge.Owner >> 16) & 0xFFFFu);
            int id = (int)(edge.Owner & 0xFFFFu);
            RuntimeFormationV172LikeOriginal group;
            if (nation != 0xFFFF && id != 0xFFFF &&
                _groupsByIdV172LikeOriginal.TryGetValue(id, out group) && group != null &&
                (group.Nation & 0xFFFF) == nation &&
                IsCurrentBrigadeNewOrderV418LikeOriginal(group, BrigadeOrderGoOnRoadV418LikeOriginal) &&
                edge.P != null && edge.P.Length >= 2)
            {
                Vector2 a = edge.P[Mathf.Min(1, edge.P.Length - 1)];
                Vector2 b = edge.P[Mathf.Max(0, edge.P.Length - 2)];
                for (int i = 0; i < group.Units.Count; i++)
                {
                    C2NeutralPeasantUnitInfoV2LikeOriginal u = group.Units[i];
                    if (!IsUsableFormationUnitV172LikeOriginal(u, true)) continue;
                    if (DistanceUnitToPointPxV385ALikeOriginal(u, a) <= 50 ||
                        DistanceUnitToPointPxV385ALikeOriginal(u, b) <= 50) return true;
                }
            }
            edge.Owner = 0xFFFFFFFFu;
            return false;
        }

        private static int FirstRoadSoldierMotionDistV426LikeOriginal(uint owner)
        {
            int nation = (int)((owner >> 16) & 0xFFFFu);
            int id = (int)(owner & 0xFFFFu);
            RuntimeFormationV172LikeOriginal group;
            if (nation == 0xFFFF || id == 0xFFFF ||
                !_groupsByIdV172LikeOriginal.TryGetValue(id, out group) || group == null ||
                (group.Nation & 0xFFFF) != nation) return -1;
            int first = Mathf.Clamp(ResolveOrderCommandCountV360LikeOriginal(group, group.Units), 0, group.Units.Count);
            for (int i = first; i < group.Units.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = group.Units[i];
                C2UnitOriginalRuntime rt = u != null && u.RuntimeLinkCachedLikeOriginal != null
                    ? u.RuntimeLinkCachedLikeOriginal.Runtime : null;
                if (rt != null && rt.Md != null) return rt.Md.MotionDist;
            }
            return -1;
        }

        private static void AppendRoadEdgePointsV426LikeOriginal(
            C2RoadEdgeRuntimeV426LikeOriginal edge, int from, int to, int turn, List<Vector2> output)
        {
            if (edge == null || edge.P == null || edge.P.Length == 0 || output == null) return;
            bool asc = from == edge.StartKnot && to == edge.EndKnot;
            int pi = edge.P.Length;
            int shift = turn < 0 ? 0 : 90;
            int shiftStep = turn < 0 || pi == 0 ? 0 : shift / pi;
            int baseShift = 0;
            if (turn == 0) { baseShift = shift; shiftStep = 0; }
            else if (turn == 1) baseShift = shiftStep;
            else if (turn == 2) { baseShift = shift - shiftStep; shiftStep = -shiftStep; }

            Action<int,int> addPoint = delegate(int baseIndex, int ordinal)
            {
                Vector2 p = edge.P[baseIndex];
                int px = Mathf.RoundToInt(p.x) >> 4;
                int py = Mathf.RoundToInt(p.y) >> 4;
                if (turn >= 0)
                {
                    int d = edge.Dir[Mathf.Clamp(baseIndex, 0, edge.Dir.Length - 1)];
                    if (!asc) d = (d + 128) & 255;
                    int off = baseShift + shiftStep * ordinal;
                    px -= (off * C2OriginalMovementMathV352.TSin[d]) >> 8;
                    py += (off * C2OriginalMovementMathV352.TCos[d]) >> 8;
                }
                Vector2 q = new Vector2(px << 4, py << 4);
                if (turn >= 0 && output.Count > 0 &&
                    DistanceRealPointsPxV426LikeOriginal(output[output.Count - 1], q) <= 26) return;
                output.Add(q);
            };

            if (asc)
            {
                for (int i = 0; i < pi - 1; i++) addPoint(i + 1, i + 1);
            }
            else
            {
                for (int i = 1; i < pi; i++) addPoint(pi - i - 1, i);
            }
        }

        private static int DistanceRealPointsPxV426LikeOriginal(Vector2 a, Vector2 b)
        {
            return C2OriginalMovementMathV352.Norma(
                (Mathf.RoundToInt(a.x) >> 4) - (Mathf.RoundToInt(b.x) >> 4),
                (Mathf.RoundToInt(a.y) >> 4) - (Mathf.RoundToInt(b.y) >> 4));
        }

        private static int GetNextRoadWayPointsV426LikeOriginal(
            int from, int to, ref ushort flag, uint requestor, List<Vector2> output)
        {
            C2RoadEdgeRuntimeV426LikeOriginal edge;
            if (!TryGetRoadEdgeRuntimeV426LikeOriginal(from, to, out edge)) return 0;
            bool asc = from == edge.StartKnot && to == edge.EndKnot;
            int before = output != null ? output.Count : 0;
            int pre = GetRoadPreLockV426LikeOriginal(edge);
            if (edge.Owner == 0xFFFFFFFFu && pre == 0)
            {
                edge.Owner = requestor;
                AppendRoadEdgePointsV426LikeOriginal(edge, from, to, -1, output);
            }
            else if (pre > 1 && edge.PreLockRequestor != requestor)
            {
                int turn = flag == 0 ? 1 : 0;
                flag = 2;
                if (edge.OwnerDirection == asc) { flag = 1; return 0; }
                AppendRoadEdgePointsV426LikeOriginal(edge, from, to, turn, output);
            }
            else if (requestor == edge.Owner || !IsRoadOwnerOnRoadV426LikeOriginal(edge))
            {
                edge.Owner = requestor;
                edge.OwnerDirection = asc;
                edge.LockTime = RoadAnimTimeV426LikeOriginal;
                if (flag == 2)
                {
                    flag = 0;
                    AppendRoadEdgePointsV426LikeOriginal(edge, from, to, 2, output);
                }
                else
                {
                    flag = 0;
                    AppendRoadEdgePointsV426LikeOriginal(edge, from, to, -1, output);
                }
            }
            else if (edge.OwnerDirection == asc)
            {
                int ownerDist = FirstRoadSoldierMotionDistV426LikeOriginal(edge.Owner);
                int requestDist = FirstRoadSoldierMotionDistV426LikeOriginal(requestor);
                if (ownerDist >= 0 && requestDist > ownerDist)
                {
                    int turn = flag == 0 ? 1 : 0;
                    flag = 2;
                    AppendRoadEdgePointsV426LikeOriginal(edge, from, to, turn, output);
                }
                else
                {
                    flag = 1;
                    return 0;
                }
            }
            else
            {
                int turn = flag == 0 ? 1 : 0;
                flag = 2;
                AppendRoadEdgePointsV426LikeOriginal(edge, from, to, turn, output);
            }
            return output != null ? output.Count - before : 0;
        }

        private static int StartBrigadeGoOnRoadFromGlobalV426LikeOriginal(
            RuntimeFormationV172LikeOriginal group, int destTopZone, byte priority,
            string source, out string audit)
        {
            audit = "invalid";
            if (group == null || group.Units == null || group.Units.Count == 0 ||
                !C2TopologyCoreV401LikeOriginal.ReadyLikeOriginal) return 0;

            CancelBrigadeGoOnRoadV385ALikeOriginal(group.GroupId, "replace_road_order", false);

            int commandPrefix = Mathf.Clamp(
                ResolveOrderCommandCountV360LikeOriginal(group, group.Units), 0, group.Units.Count);
            C2NeutralPeasantUnitInfoV2LikeOriginal geometryReference = null;
            for (int i = commandPrefix; i < group.Units.Count && geometryReference == null; i++)
                if (IsUsableFormationUnitV172LikeOriginal(group.Units[i], true)) geometryReference = group.Units[i];
            for (int i = 0; i < commandPrefix && geometryReference == null; i++)
                if (IsUsableFormationUnitV172LikeOriginal(group.Units[i], true)) geometryReference = group.Units[i];
            if (geometryReference == null)
            {
                audit = "missing_geometry_reference";
                return 0;
            }
            C2UnitOriginalRuntimeLinkLikeOriginal geometryLink = geometryReference.RuntimeLinkCachedLikeOriginal;
            if (geometryLink == null || geometryLink.Runtime == null || geometryLink.Runtime.Md == null ||
                geometryLink.Runtime.Md.GeometryRadius2 <= 0)
            {
                audit = "missing_md_geometry_radius2";
                return 0;
            }

            // BrigadeOrder_GoOnRoad::Init reads NewMonster::Radius2.  The MD parser
            // stores the source geometry radius, so convert it to the NewMonster unit.
            int radius2LikeOriginal = geometryLink.Runtime.Md.GeometryRadius2 << 4;
            int distInColumn = radius2LikeOriginal / 14 + 4;
            if (distInColumn <= 0)
            {
                audit = "invalid_dist_in_column";
                return 0;
            }
            int unitsInLine = 100 / distInColumn;
            if (unitsInLine < 1 || unitsInLine > 16)
            {
                audit = "units_in_line_out_of_original_range value=" +
                        unitsInLine.ToString(CultureInfo.InvariantCulture);
                return 0;
            }
            int passNPoints = 1 + ((radius2LikeOriginal / 10 + 2) / 19) * 3;
            int[] shifts = new int[unitsInLine];
            int high = distInColumn * (unitsInLine - 1) / 2;
            for (int i = 0; i < unitsInLine; i++) shifts[i] = i * distInColumn - high;
            int nLines = group.Units.Count / unitsInLine + 1;
            if ((group.Units.Count % unitsInLine) != 0) nLines++;

            byte lockType = C2OriginalMovementSystemV425LikeOriginal.ResolveLockTypeV425LikeOriginal(geometryReference);
            float centerRealX, centerRealY;
            ComputeFormationGroupCenterV172LikeOriginal(group, group.Units, out centerRealX, out centerRealY);
            int currentTop = C2TopologyCoreV401LikeOriginal.GetTopologyV401LikeOriginal(
                Mathf.RoundToInt(centerRealX) >> 4, Mathf.RoundToInt(centerRealY) >> 4, lockType);
            if (currentTop >= 0xFFFE || destTopZone >= 0xFFFE ||
                !C2TopologyCoreV401LikeOriginal.CheckIfRoadZoneV425LikeOriginal(currentTop))
            {
                audit = "not_on_original_road_zone";
                return 0;
            }

            C2BrigadeOrderGoOnRoadStateV385A state = new C2BrigadeOrderGoOnRoadStateV385A();
            state.GroupId = group.GroupId;
            state.OriginalShape = group.Shape ?? string.Empty;
            state.Source = source ?? "BrigadeOrder_GoOnRoad";
            state.Priority = priority;
            state.CommandPrefix = commandPrefix;
            state.PointsReal = Array.Empty<Vector2>();
            state.PointsDir = Array.Empty<byte>();
            state.ShiftInColumn = shifts;
            state.HeadIndex = -1;
            state.MaxPIndex = 0;
            state.DestTopZone = destTopZone;
            state.HeadTopZoneIndex = currentTop;
            state.LockType = lockType;
            state.Owner = ((group.Nation & 0xFF) << 16) | (group.GroupId & 0xFFFF);
            state.RoadFlag = 0;
            state.UnitsInLine = unitsInLine;
            state.DistInColumn = distInColumn;
            state.PassNPoints = passNPoints;
            state.NLines = nLines;
            state.FirstStep = true;
            state.OffPlaceHead = true;
            state.LastMoveTick = 0;
            state.LastStepTick = 0;
            state.HeadUnit = null;

            // BrigadeOrder_HumanGlobalSendTo::Process -> BR->CreateNewBOrder(1,Ro).
            CreateBrigadeNewOrderV418LikeOriginal(
                group, BrigadeOrderGoOnRoadV418LikeOriginal, 1, priority,
                state.Source + "::CreateNewBOrder(1,GoOnRoad)");
            group.UsesRoadMovement = true;
            group.TurnActive = false;
            group.CommandSlotCount = commandPrefix;

            int live = 0;
            for (int i = 0; i < group.Units.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal unit = group.Units[i];
                if (!IsUsableFormationUnitV172LikeOriginal(unit, true)) continue;
                live++;
                C2OriginalOrderChainV352.ClearMoveChainForExternalOrder(unit);
                C2BattleTerrainMode.C2BuildRuntimeCancelWorkerOrderForUnitLikeOriginal(unit, state.Source);
                C2RoadUnitSpeedControllerV352.DetachLikeOriginal(unit);
                C2UnitOrderRuntimeV325LikeOriginal.IssueLikeOriginal(
                    unit, C2UnitOrderKindV325LikeOriginal.FormationMove,
                    state.Source, "BrigadeOrder_GoOnRoad");
            }
            if (live == 0)
            {
                group.UsesRoadMovement = false;
                DeleteBrigadeNewOrderV418LikeOriginal(
                    group, BrigadeOrderGoOnRoadV418LikeOriginal, "GoOnRoad_no_live_members");
                audit = "no_live_members";
                return 0;
            }

            _roadOrdersV385A[group.GroupId] = state;
            audit = "ok group=" + group.GroupId.ToString(CultureInfo.InvariantCulture) +
                    " members=" + live.ToString(CultureInfo.InvariantCulture) +
                    " destTop=" + destTopZone.ToString(CultureInfo.InvariantCulture) +
                    " lockType=" + lockType.ToString(CultureInfo.InvariantCulture) +
                    " unitsInLine=" + unitsInLine.ToString(CultureInfo.InvariantCulture) +
                    " distInColumn=" + distInColumn.ToString(CultureInfo.InvariantCulture) +
                    " passNPoints=" + passNPoints.ToString(CultureInfo.InvariantCulture);
            Debug.Log("[C2:ROAD ORDER V426 START] " + audit);
            return live;
        }

        private static void RebuildRoadDirectionsV426LikeOriginal(C2BrigadeOrderGoOnRoadStateV385A state)
        {
            if (state == null || state.PointsReal == null)
                return;
            state.PointsDir = new byte[state.PointsReal.Length];
            if (state.PointsReal.Length == 0) return;
            for (int i = 0; i < state.PointsReal.Length - 1; i++)
            {
                Vector2 a = state.PointsReal[i];
                Vector2 b = state.PointsReal[i + 1];
                state.PointsDir[i] = C2OriginalMovementMathV352.GetDir(
                    (Mathf.RoundToInt(b.x) >> 4) - (Mathf.RoundToInt(a.x) >> 4),
                    (Mathf.RoundToInt(b.y) >> 4) - (Mathf.RoundToInt(a.y) >> 4));
            }
            state.PointsDir[state.PointsDir.Length - 1] = state.PointsDir.Length > 1
                ? state.PointsDir[state.PointsDir.Length - 2]
                : (byte)0;
        }

        private static void PrependRoadHistoryV426LikeOriginal(
            RuntimeFormationV172LikeOriginal group, C2BrigadeOrderGoOnRoadStateV385A state)
        {
            if (group == null || state == null || state.PointsReal == null || state.PointsReal.Length == 0 ||
                state.NLines <= state.MaxPIndex) return;
            float cxr, cyr;
            ComputeFormationGroupCenterV172LikeOriginal(group, group.Units, out cxr, out cyr);
            int cx = Mathf.RoundToInt(cxr) >> 4;
            int cy = Mathf.RoundToInt(cyr) >> 4;
            Vector2 last = state.PointsReal[state.PointsReal.Length - 1];
            byte apd = C2OriginalMovementMathV352.GetDir(
                cx - (Mathf.RoundToInt(last.x) >> 4), cy - (Mathf.RoundToInt(last.y) >> 4));
            int np = state.NLines - state.MaxPIndex;
            Vector2 first = state.PointsReal[0];
            int firstX = Mathf.RoundToInt(first.x) >> 4;
            int firstY = Mathf.RoundToInt(first.y) >> 4;
            Vector2[] combined = new Vector2[state.PointsReal.Length + np];
            Array.Copy(state.PointsReal, 0, combined, np, state.PointsReal.Length);
            const int pdd = 25;
            for (int i = 0; i < np; i++)
            {
                int nx = firstX + ((pdd * (i + 1) * C2OriginalMovementMathV352.TCos[apd]) >> 8);
                int ny = firstY + ((pdd * (i + 1) * C2OriginalMovementMathV352.TSin[apd]) >> 8);
                combined[np - 1 - i] = new Vector2(nx << 4, ny << 4);
            }
            state.PointsReal = combined;
            state.MaxPIndex = combined.Length;
        }

        private static void LockNextRoadPointV426LikeOriginal(C2BrigadeOrderGoOnRoadStateV385A state)
        {
            if (state == null || state.HeadTopZoneIndex == state.DestTopZone) return;
            int na = C2TopologyCoreV401LikeOriginal.GetNAreasV401LikeOriginal(state.LockType);
            if (na <= 0) return;
            int next = C2TopologyCoreV401LikeOriginal.GetMotionLinksV401LikeOriginal(
                state.DestTopZone + na * state.HeadTopZoneIndex,
                state.LockType, (byte)((state.Owner >> 16) & 0xFF));
            if (next < 0xFFFE && C2TopologyCoreV401LikeOriginal.CheckIfRoadZoneV425LikeOriginal(next))
                SetRoadPreLockV426LikeOriginal(state.HeadTopZoneIndex, next, unchecked((uint)state.Owner));
        }

        private static bool AppendNextRoadTopologyChunkV426LikeOriginal(
            RuntimeFormationV172LikeOriginal group, C2BrigadeOrderGoOnRoadStateV385A state)
        {
            int na = C2TopologyCoreV401LikeOriginal.GetNAreasV401LikeOriginal(state.LockType);
            if (na <= 0) return false;
            int next = C2TopologyCoreV401LikeOriginal.GetMotionLinksV401LikeOriginal(
                state.DestTopZone + na * state.HeadTopZoneIndex,
                state.LockType, (byte)((state.Owner >> 16) & 0xFF));
            if (next < 0xFFFE && state.HeadIndex == -1)
            {
                state.HeadTopZoneIndex = next;
                next = C2TopologyCoreV401LikeOriginal.GetMotionLinksV401LikeOriginal(
                    state.DestTopZone + na * state.HeadTopZoneIndex,
                    state.LockType, (byte)((state.Owner >> 16) & 0xFF));
            }
            if (next >= 0xFFFE || !C2TopologyCoreV401LikeOriginal.CheckIfRoadZoneV425LikeOriginal(next))
                return state.HeadIndex < state.MaxPIndex - 1;

            List<Vector2> points = new List<Vector2>(state.PointsReal ?? Array.Empty<Vector2>());
            int before = points.Count;
            ushort flag = state.RoadFlag;
            int add = GetNextRoadWayPointsV426LikeOriginal(
                state.HeadTopZoneIndex, next, ref flag, unchecked((uint)state.Owner), points);
            state.RoadFlag = flag;
            if (add <= 0)
                return state.MaxPIndex > 0;

            state.PointsReal = points.ToArray();
            state.MaxPIndex = state.PointsReal.Length;
            if (state.HeadIndex == -1)
            {
                PrependRoadHistoryV426LikeOriginal(group, state);
                // Retail ProcessPre places the head at the end of the first buffered
                // chunk.  The next streamed edge extends MaxPIndex before movement begins.
                state.HeadIndex = state.MaxPIndex - 1;
            }
            RebuildRoadDirectionsV426LikeOriginal(state);
            state.HeadTopZoneIndex = next;
            LockNextRoadPointV426LikeOriginal(state);

            const int memShift = 80;
            if (state.HeadIndex > state.NLines * state.PassNPoints + memShift && state.HeadIndex > memShift)
            {
                int remain = state.MaxPIndex - memShift;
                Vector2[] compact = new Vector2[remain];
                Array.Copy(state.PointsReal, memShift, compact, 0, remain);
                state.PointsReal = compact;
                state.MaxPIndex = remain;
                state.HeadIndex -= memShift;
                RebuildRoadDirectionsV426LikeOriginal(state);
            }
            return true;
        }

        private static void RangeInLineMembersV426LikeOriginal(RuntimeFormationV172LikeOriginal group)
        {
            // BrigadeOrders.cpp::BrigadeOrder_GoOnRoad::RangeInLineMemb.
            // Native Brigades keep holes (0xFFFF) in Memb[]; when one of the first
            // three soldier slots dies/stalls, GoOnRoad compacts only the soldier tail.
            if (group == null || group.Units == null || group.Units.Count <= 4) return;
            bool foundHole = false;
            int firstHole = -1;
            for (int i = 3; i < group.Units.Count - 1; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal unit = group.Units[i];
                bool hole = unit == null || unit.IsDeadLikeOriginal;
                if (hole)
                {
                    if (!foundHole)
                    {
                        firstHole = i;
                        foundHole = true;
                    }
                    continue;
                }
                if (!foundHole) continue;

                int shift = i - firstHole;
                for (int j = i; j < group.Units.Count; j++)
                {
                    int dst = firstHole + j - i;
                    group.Units[dst] = group.Units[j];
                    group.Units[j] = null;
                }
                foundHole = false;
                i = Math.Max(2, i - shift);
            }
        }

        private static bool ProcessBrigadeGoOnRoadPreV426LikeOriginal(
            RuntimeFormationV172LikeOriginal group, C2BrigadeOrderGoOnRoadStateV385A state)
        {
            int tick = CurrentSimulationTickV403ELikeOriginal;
            if (tick <= state.LastMoveTick) return true;

            if (state.HeadIndex == -1)
            {
                float cxr, cyr;
                ComputeFormationGroupCenterV172LikeOriginal(group, group.Units, out cxr, out cyr);
                state.HeadTopZoneIndex = C2TopologyCoreV401LikeOriginal.GetTopologyV401LikeOriginal(
                    Mathf.RoundToInt(cxr) >> 4, Mathf.RoundToInt(cyr) >> 4, state.LockType);
                if (state.HeadTopZoneIndex == state.DestTopZone) return false;
            }
            if (state.HeadTopZoneIndex == state.DestTopZone &&
                state.MaxPIndex - state.HeadIndex < 2) return false;

            if (state.MaxPIndex - state.HeadIndex < 8)
            {
                if (!AppendNextRoadTopologyChunkV426LikeOriginal(group, state)) return false;
            }

            C2NeutralPeasantUnitInfoV2LikeOriginal head = state.HeadUnit;
            if (head != null && !IsUsableFormationUnitV172LikeOriginal(head, true))
            {
                head = null;
                state.HeadUnit = null;
            }
            int dds = 0;
            if (head != null && state.HeadIndex >= 0 && state.HeadIndex < state.MaxPIndex)
                dds = DistanceUnitToPointPxV385ALikeOriginal(head, state.PointsReal[state.HeadIndex]);

            // GoAway/AskGoAway is installed below as the dynamic UnitsField road-clearance
            // stage.  Keep the retail call cadence: every tick except multiples of 20.
            if ((tick % 20) != 0)
                state.IsEnemyOnWay = GoAwayV426LikeOriginal(group, state);

            if (state.HeadIndex == -1)
            {
                if (!state.IsEnemyOnWay)
                {
                    SetNextPosAndDestV385ALikeOriginal(group, state, true);
                    state.LastMoveTick = tick;
                }
            }
            else if (dds < 100 || (state.HeadIndex < 30 && dds < 150))
            {
                if (state.HeadIndex < state.MaxPIndex - 1 && state.RoadFlag != 1 && !state.IsEnemyOnWay)
                {
                    SetNextPosAndDestV385ALikeOriginal(group, state, false);
                    state.LastMoveTick = tick;
                }
            }
            else
            {
                if (head != null)
                {
                    C2UnitOriginalRuntime rt = head.RuntimeLinkCachedLikeOriginal != null
                        ? head.RuntimeLinkCachedLikeOriginal.Runtime : null;
                    int stand = rt != null ? rt.OriginalStandTimeV425LikeOriginal : 0;
                    if (stand > 40 || tick - state.LastStepTick > 40)
                    {
                        state.HeadUnit = null;
                        if (group.Units.Count > 20 &&
                            ((group.Units.Count > 3 && (group.Units[3] == null || group.Units[3].IsDeadLikeOriginal)) ||
                             (group.Units.Count > 4 && (group.Units[4] == null || group.Units[4].IsDeadLikeOriginal)) ||
                             (group.Units.Count > 5 && (group.Units[5] == null || group.Units[5].IsDeadLikeOriginal))))
                        {
                            RangeInLineMembersV426LikeOriginal(group);
                            state.HeadUnit = null;
                        }
                        if (!state.IsEnemyOnWay)
                        {
                            SetNextPosAndDestV385ALikeOriginal(group, state, false);
                            state.LastMoveTick = tick;
                        }
                    }
                }
                else
                {
                    SetUnitsSpeedV385ALikeOriginal(group, state);
                }
            }
            return true;
        }

        private static int PointToLineDistanceExV426LikeOriginal(
            int x, int y, int x1, int y1, int x2, int y2)
        {
            int dx1 = x - x1, dy1 = y - y1;
            int dx12 = x2 - x1, dy12 = y2 - y1;
            int r1 = (int)Math.Sqrt((double)dx1 * dx1 + (double)dy1 * dy1);
            int r12 = (int)Math.Sqrt((double)dx12 * dx12 + (double)dy12 * dy12);
            if (r12 == 0) return r1;
            dx12 = 256 * dx12 / r12;
            dy12 = 256 * dy12 / r12;
            int sp = (dx1 * dx12 + dy1 * dy12) / r12;
            if (sp < 0) return r1;
            if (sp > 256)
            {
                int ex = x2 - x, ey = y2 - y;
                return (int)Math.Sqrt((double)ex * ex + (double)ey * ey);
            }
            return Math.Abs(dy1 * dx12 - dx1 * dy12) >> 8;
        }

        private static bool AskGoAwayV426LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            byte mask, int x1, int y1, int radius, int x2, int y2)
        {
            if (!IsUsableFormationUnitV172LikeOriginal(unit, true)) return false;
            byte unitMask = C2CombatCoreV408LikeOriginal.GetNMaskV408LikeOriginal(unit);
            if ((unitMask & mask) == 0)
            {
                RuntimeFormationV172LikeOriginal enemyBrigade;
                return TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out enemyBrigade) && enemyBrigade != null;
            }

            C2UnitOriginalRuntime rt = unit.RuntimeLinkCachedLikeOriginal != null
                ? unit.RuntimeLinkCachedLikeOriginal.Runtime : null;
            if (rt == null || rt.OriginalStandTimeV425LikeOriginal <= 1 ||
                C2OriginalOrderChainV352.HasLocalMoveOrderLikeOriginal(unit)) return false;

            int ddx = x2 - x1, ddy = y2 - y1;
            int nn = C2OriginalMovementMathV352.Norma(ddx, ddy);
            if (nn == 0) return false;
            int ux = Mathf.RoundToInt(unit.RealXFloat != 0.0f ? unit.RealXFloat : unit.RealX) >> 4;
            int uy = Mathf.RoundToInt(unit.RealYFloat != 0.0f ? unit.RealYFloat : unit.RealY) >> 4;
            int mdr = unchecked((sbyte)C2OriginalMovementMathV352.GetDir(ddx, ddy));
            int pbd = unchecked((sbyte)C2OriginalMovementMathV352.GetDir(ux - x1, uy - y1));
            rt.OriginalUnitSpeedLikeOriginal = 64;
            int dd = PointToLineDistanceExV426LikeOriginal(ux, uy, x1, y1, x2, y2);
            int raz = radius - dd;
            int delta = mdr - pbd;
            if (raz <= 0 || Math.Abs(delta) >= 65) return false;
            raz = (raz * (16 + (C2RetailRandomV407LikeOriginal.Rando(unit) & 15))) >> 5;
            int tx, ty;
            if (delta > 0)
            {
                tx = ux + ddy * raz / nn;
                ty = uy - ddx * raz / nn;
            }
            else
            {
                tx = ux - ddy * raz / nn;
                ty = uy + ddx * raz / nn;
            }
            // BrigadeOrders.cpp::AskGoAway:
            //   if BrigadeID!=FFFF StayForSomeTime(1,10*25*256);
            //   NewMonsterSendTo(...,128+16,1) is then pushed above that stay node.
            RuntimeFormationV172LikeOriginal displacedGroup;
            if (TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out displacedGroup) && displacedGroup != null)
            {
                C2OriginalOrderChainV352.SubmitStayForSomeTimeLikeOriginal(
                    unit, 1, 10 * 25 * 256, "BrigadeOrder_GoOnRoad::AskGoAway::StayForSomeTime");
            }
            C2OriginalOrderChainV352.SubmitMove(
                unit, tx << 4, ty << 4, false, 0, 128 + 16,
                "BrigadeOrder_GoOnRoad::AskGoAway", false);
            return true;
        }

        private static int ScanGoAwayRadiusV426LikeOriginal(
            int xc, int yc, int radius, byte mask, int x1, int y1, int x2, int y2)
        {
            int count = 0;
            int c0x = xc >> 7, c0y = yc >> 7;
            int r2 = radius * radius;
            for (int cy = c0y - 2; cy <= c0y + 2; cy++)
            for (int cx = c0x - 2; cx <= c0x + 2; cx++)
            {
                List<C2NeutralPeasantUnitInfoV2LikeOriginal> cell = C2LiveUnitCellIndex.GetCell(cx, cy);
                if (cell == null) continue;
                for (int i = 0; i < cell.Count; i++)
                {
                    C2NeutralPeasantUnitInfoV2LikeOriginal u = cell[i];
                    if (u == null) continue;
                    int ux = Mathf.RoundToInt(u.RealXFloat != 0.0f ? u.RealXFloat : u.RealX) >> 4;
                    int uy = Mathf.RoundToInt(u.RealYFloat != 0.0f ? u.RealYFloat : u.RealY) >> 4;
                    int dx = ux - xc, dy = uy - yc;
                    if ((long)dx * dx + (long)dy * dy >= r2) continue;
                    if (AskGoAwayV426LikeOriginal(u, mask, x1, y1, radius, x2, y2)) count++;
                }
            }
            return count;
        }

        private static bool GoAwayV426LikeOriginal(
            RuntimeFormationV172LikeOriginal group, C2BrigadeOrderGoOnRoadStateV385A state)
        {
            if (group == null || state == null || state.HeadUnit == null ||
                state.PointsReal == null || state.MaxPIndex <= 0) return false;
            C2NeutralPeasantUnitInfoV2LikeOriginal head = state.HeadUnit;
            if (!IsUsableFormationUnitV172LikeOriginal(head, true)) return false;
            int cx = Mathf.RoundToInt(head.RealXFloat != 0.0f ? head.RealXFloat : head.RealX) >> 4;
            int cy = Mathf.RoundToInt(head.RealYFloat != 0.0f ? head.RealYFloat : head.RealY) >> 4;
            Vector2 last = state.PointsReal[state.MaxPIndex - 1];
            int dx = Mathf.RoundToInt(last.x) >> 4;
            int dy = Mathf.RoundToInt(last.y) >> 4;
            byte mask = C2CombatCoreV408LikeOriginal.GetNMaskV408LikeOriginal(head);
            int enemies = ScanGoAwayRadiusV426LikeOriginal(cx, cy, 128, mask, cx, cy, dx, dy);
            enemies += ScanGoAwayRadiusV426LikeOriginal((cx + dx) / 2, (cy + dy) / 2, 128, mask, cx, cy, dx, dy);
            return enemies > 10;
        }

        private static void TickBrigadeGoOnRoadV385ALikeOriginal()
        {
            if (_roadOrdersV385A.Count == 0) return;
            List<int> groupIds = new List<int>(_roadOrdersV385A.Keys);
            for (int oi = 0; oi < groupIds.Count; oi++)
            {
                int groupId = groupIds[oi];
                C2BrigadeOrderGoOnRoadStateV385A state;
                if (!_roadOrdersV385A.TryGetValue(groupId, out state) || state == null) continue;
                RuntimeFormationV172LikeOriginal group;
                if (!_groupsByIdV172LikeOriginal.TryGetValue(groupId, out group) || group == null)
                {
                    CancelBrigadeGoOnRoadV385ALikeOriginal(groupId, "formation_deleted", false);
                    continue;
                }
                if (!IsCurrentBrigadeNewOrderV418LikeOriginal(group, BrigadeOrderGoOnRoadV418LikeOriginal))
                    continue;
                if (!ProcessBrigadeGoOnRoadPreV426LikeOriginal(group, state))
                    FinishBrigadeGoOnRoadV385ALikeOriginal(group, state, "ProcessPre_false");
            }
        }

        private static void SetNextPosAndDestV385ALikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            C2BrigadeOrderGoOnRoadStateV385A state,
            bool firstInstall)
        {
            if (group == null || state == null || state.PointsReal == null || state.PointsReal.Length == 0)
                return;
            if (state.HeadIndex >= state.MaxPIndex - 1) return;
            state.HeadIndex++;
            state.LastStepTick = CurrentSimulationTickV403ELikeOriginal;

            if (state.FirstStep)
            {
                string orderMode = SortBrigUnitsV385ALikeOriginal(group, state);
                state.FirstStep = false;
                Debug.Log("[C2:ROAD ORDER V385A FIRSTSTEP] group=" + group.GroupId.ToString(CultureInfo.InvariantCulture) +
                          " mode=" + orderMode +
                          " unitsInLine=" + state.UnitsInLine.ToString(CultureInfo.InvariantCulture) +
                          " distInColumn=" + state.DistInColumn.ToString(CultureInfo.InvariantCulture) +
                          " passNPoints=" + state.PassNPoints.ToString(CultureInfo.InvariantCulture) +
                          " commandPrefix=" + state.CommandPrefix.ToString(CultureInfo.InvariantCulture) +
                          " offPlaceHead=" + (state.OffPlaceHead ? "1" : "0"));
            }

            state.HeadUnit = null;
            int bestHeadDistance = 10000;
            C2NeutralPeasantUnitInfoV2LikeOriginal bestHead = null;
            int averageSum = 0, averageCount = 0;
            int headColumn = state.UnitsInLine / 2;

            int commandPrefix = Mathf.Clamp(state.CommandPrefix, 0, group.Units.Count);
            for (int i = 0; i < group.Units.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal unit = group.Units[i];
                if (!IsUsableFormationUnitV172LikeOriginal(unit, true)) continue;

                int pointIndex;
                int column;
                bool waitingBeforeColumn = false;
                if (i < commandPrefix)
                {
                    int officerPoint = state.OffPlaceHead ? state.HeadIndex : Mathf.Max(state.HeadIndex - 4, 0);
                    pointIndex = Mathf.Clamp(officerPoint, 0, state.MaxPIndex - 1);
                    if (commandPrefix <= 1) column = headColumn;
                    else if (i == 0) column = 0;
                    else if (i == 1) column = headColumn;
                    else if (i == 2) column = state.UnitsInLine - 1;
                    else column = headColumn;
                }
                else
                {
                    int soldierIndex = i - commandPrefix;
                    int line = soldierIndex / state.UnitsInLine;
                    column = soldierIndex % state.UnitsInLine;
                    pointIndex = state.HeadIndex - line * state.PassNPoints;
                    if (state.OffPlaceHead) pointIndex--;
                    if (pointIndex < 0)
                    {
                        waitingBeforeColumn = true;
                        pointIndex = 0;
                    }
                    if (pointIndex >= state.MaxPIndex) pointIndex = state.MaxPIndex - 1;
                }

                // Native first assigns officers and non-negative soldier rows.
                // Waiting rows get point 0 in the speed pass, only if NMemb != 0.
                if (waitingBeforeColumn) continue;
                Vector2 target = GetUnitCoordInColumnV385ALikeOriginal(state, pointIndex, column);
                InstallRoadDestinationV385ALikeOriginal(unit, target, state, false);
                if (i < group.Slots.Count) group.Slots[i] = target;
                if (i < commandPrefix)
                {
                    // SetOffPoss: slot 1 overwrites slot 0 as HeadOBJIndex.
                    if (state.OffPlaceHead && i < 2) state.HeadUnit = unit;
                    continue;
                }
                int distance = DistanceUnitToPointPxV385ALikeOriginal(unit, target);
                if (distance < 300) { averageSum += distance; averageCount++; }
                if (state.HeadUnit == null && column == headColumn)
                {
                    if (distance < 400) state.HeadUnit = unit;
                    else if (distance < bestHeadDistance) { bestHeadDistance = distance; bestHead = unit; }
                }
            }
            if (state.HeadUnit == null) state.HeadUnit = bestHead;
            if (averageCount == 0) return;
            int average = averageSum / averageCount;
            for (int i = 0; i < group.Units.Count; i++)
            {
                var unit = group.Units[i];
                if (!IsUsableFormationUnitV172LikeOriginal(unit, true)) continue;
                var rt = unit.RuntimeLinkCachedLikeOriginal?.Runtime;
                if (rt == null) continue;
                int pi = state.HeadIndex;
                if (i >= commandPrefix)
                    pi -= ((i - commandPrefix) / state.UnitsInLine) * state.PassNPoints + (state.OffPlaceHead ? 1 : 0);
                if (i >= commandPrefix && pi < 0)
                {
                    Vector2 target = GetUnitCoordInColumnV385ALikeOriginal(state, 0, (i - commandPrefix) % state.UnitsInLine);
                    InstallRoadDestinationV385ALikeOriginal(unit, target, state, true);
                    if (i < group.Slots.Count) group.Slots[i] = target;
                    rt.OriginalUnitSpeedLikeOriginal = 32;
                    continue;
                }
                int ds = DistanceUnitToPointPxV385ALikeOriginal(unit, new Vector2(rt.MoveTargetRealXLikeOriginal, rt.MoveTargetRealYLikeOriginal));
                int speed = Mathf.Clamp((96 * ds) / (i < commandPrefix ? average + 1 : Mathf.Max(1, average)), 32, 240);
                if (i < commandPrefix ? state.HeadIndex < 15 && speed < 72 : state.HeadIndex < 30 && pi < 6 && speed < 72)
                    speed = 100;
                rt.OriginalUnitSpeedLikeOriginal = speed;
            }
        }

        private static void InstallRoadDestinationV385ALikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            Vector2 target,
            C2BrigadeOrderGoOnRoadStateV385A state,
            bool waitingBeforeColumn)
        {
            if (unit == null) return;
            C2UnitOriginalRuntimeLinkLikeOriginal link = unit.RuntimeLinkCachedLikeOriginal;
            if (link == null || link.Runtime == null)
            {
                unit.SetMoveDestinationRealLikeOriginal(
                    target.x, target.y,
                    C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                    false, 0);
                return;
            }

            // BrigadeOrder_GoOnRoad writes DestX/DestY, without a PreciseSendTo
            // child. The generic task facade now creates such a child: using it
            // here snapped/stopped each soldier at intermediate road points and
            // let local-order completion erase the brigade's next destination.
            link.Owner.InstallRoadDestinationV434LikeOriginal(link.Runtime, target.x, target.y);
            if (waitingBeforeColumn)
                link.Runtime.OriginalUnitSpeedLikeOriginal = C2RoadMinSpeedV385A;
        }

        // NewMon.cpp::ApplyTiring asks GetTiringBonus only while the brigade owns
        // BRIGADEORDER_GOONROAD.  In the supplied retail Roads.dat every normal road
        // descriptor keeps the source default Tiring=-64 (there are no $PHYS overrides).
        // Do not apply it during the off-road approach/final handoff.
        internal static int GetRoadTiringMultiplierV403ELikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null) return 256;
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) || group == null ||
                !group.UsesRoadMovement) return 256;
            C2BrigadeOrderGoOnRoadStateV385A state;
            if (!_roadOrdersV385A.TryGetValue(group.GroupId, out state) || state == null) return 256;

            // NewMon.cpp::ApplyTiring -> GetTiringBonus(RealX>>4, RealY>>4, LockType).
            // The bonus belongs to the topology zone physically under THIS unit,
            // not to the streamed point index of the brigade road order.
            int realX = Mathf.RoundToInt(unit.RealXFloat != 0.0f ? unit.RealXFloat : unit.RealX);
            int realY = Mathf.RoundToInt(unit.RealYFloat != 0.0f ? unit.RealYFloat : unit.RealY);
            byte lockType = C2OriginalMovementSystemV425LikeOriginal.ResolveLockTypeV425LikeOriginal(unit);
            return C2TopologyCoreV401LikeOriginal.GetTiringBonusV426LikeOriginal(
                realX >> 4, realY >> 4, lockType);
        }

        private static Vector2 GetUnitCoordInColumnV385ALikeOriginal(
            C2BrigadeOrderGoOnRoadStateV385A state,
            int pointIndex,
            int columnIndex)
        {
            pointIndex = Mathf.Clamp(pointIndex, 0, state.MaxPIndex - 1);
            columnIndex = Mathf.Clamp(columnIndex, 0, state.ShiftInColumn.Length - 1);
            Vector2 basePoint = state.PointsReal[pointIndex];
            int shiftPixels = state.ShiftInColumn[columnIndex];
            int direction = state.PointsDir[pointIndex];
            int sin = C2OriginalMovementMathV352.TSin[direction];
            int cos = C2OriginalMovementMathV352.TCos[direction];
            // Native PointsXY are integer pixels, then DestX/Y are shifted <<4.
            int bx = Mathf.RoundToInt(basePoint.x) >> 4;
            int by = Mathf.RoundToInt(basePoint.y) >> 4;
            return new Vector2(
                (bx - ((shiftPixels * sin) >> 8)) << 4,
                (by + ((shiftPixels * cos) >> 8)) << 4);
        }

        private static string SortBrigUnitsV385ALikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            C2BrigadeOrderGoOnRoadStateV385A state)
        {
            int firstSoldier = Mathf.Clamp(state.CommandPrefix, 0, group.Units.Count);
            int soldierCount = group.Units.Count - firstSoldier;
            if (soldierCount <= 1) return "keep";

            // Original OffPlaseHead checks the first command member only.
            state.OffPlaceHead = true;
            if (firstSoldier > 0 && IsUsableFormationUnitV172LikeOriginal(group.Units[0], true))
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal officer = group.Units[0];
                float ox = officer.RealXFloat != 0.0f ? officer.RealXFloat : officer.RealX;
                float oy = officer.RealYFloat != 0.0f ? officer.RealYFloat : officer.RealY;
                Vector2 headPoint = state.PointsReal[state.HeadIndex];
                int od = C2OriginalMovementMathV352.Norma(
                    Mathf.RoundToInt((ox - headPoint.x) / 16.0f),
                    Mathf.RoundToInt((oy - headPoint.y) / 16.0f));
                if (od > 300) state.OffPlaceHead = false;
            }

            int row = Mathf.Min(state.UnitsInLine, soldierCount);
            bool inHead = false;
            bool inBack = false;
            Vector2 firstPoint = state.PointsReal[state.HeadIndex];
            for (int i = 0; i < row; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = group.Units[firstSoldier + i];
                if (DistanceUnitToPointPxV385ALikeOriginal(u, firstPoint) < 300) inHead = true;
            }
            for (int i = 0; i < row; i++)
            {
                int idx = group.Units.Count - 1 - i;
                if (idx < firstSoldier) break;
                C2NeutralPeasantUnitInfoV2LikeOriginal u = group.Units[idx];
                if (DistanceUnitToPointPxV385ALikeOriginal(u, firstPoint) < 300) inBack = true;
            }

            if (!inHead && inBack)
            {
                group.Units.Reverse(firstSoldier, soldierCount);
                return "reverse";
            }

            if (!inHead && !inBack)
            {
                List<C2NeutralPeasantUnitInfoV2LikeOriginal> soldiers =
                    group.Units.GetRange(firstSoldier, soldierCount);
                soldiers.Sort(delegate(C2NeutralPeasantUnitInfoV2LikeOriginal a,
                                       C2NeutralPeasantUnitInfoV2LikeOriginal b)
                {
                    int da = DistanceUnitToPointPxV385ALikeOriginal(a, firstPoint);
                    int db = DistanceUnitToPointPxV385ALikeOriginal(b, firstPoint);
                    return da.CompareTo(db);
                });
                for (int i = 0; i < soldiers.Count; i++)
                    group.Units[firstSoldier + i] = soldiers[i];
                return "sort_distance";
            }

            return "keep";
        }

        private static int DistanceUnitToPointPxV385ALikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            Vector2 point)
        {
            if (unit == null) return int.MaxValue / 4;
            float ux = unit.RealXFloat != 0.0f ? unit.RealXFloat : unit.RealX;
            float uy = unit.RealYFloat != 0.0f ? unit.RealYFloat : unit.RealY;
            return C2OriginalMovementMathV352.Norma(
                (Mathf.RoundToInt(ux) >> 4) - (Mathf.RoundToInt(point.x) >> 4),
                (Mathf.RoundToInt(uy) >> 4) - (Mathf.RoundToInt(point.y) >> 4));
        }

        private static C2NeutralPeasantUnitInfoV2LikeOriginal ResolveHeadUnitV385ALikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            C2BrigadeOrderGoOnRoadStateV385A state)
        {
            if (state.HeadUnit != null && IsUsableFormationUnitV172LikeOriginal(state.HeadUnit, true))
                return state.HeadUnit;
            state.HeadUnit = ResolveNearestLiveMemberToHeadV385ALikeOriginal(group, state);
            return state.HeadUnit;
        }

        private static C2NeutralPeasantUnitInfoV2LikeOriginal ResolveNearestLiveMemberToHeadV385ALikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            C2BrigadeOrderGoOnRoadStateV385A state)
        {
            if (group == null || state == null || state.PointsReal == null || state.PointsReal.Length == 0)
                return null;
            Vector2 head = state.PointsReal[Mathf.Clamp(state.HeadIndex, 0, state.PointsReal.Length - 1)];
            C2NeutralPeasantUnitInfoV2LikeOriginal best = null;
            int bestDistance = int.MaxValue;
            for (int i = Mathf.Clamp(state.CommandPrefix, 0, group.Units.Count); i < group.Units.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = group.Units[i];
                if (!IsUsableFormationUnitV172LikeOriginal(u, true)) continue;
                int d = DistanceUnitToPointPxV385ALikeOriginal(u, head);
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = u;
                }
            }
            if (best != null) return best;
            for (int i = 0; i < group.Units.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = group.Units[i];
                if (IsUsableFormationUnitV172LikeOriginal(u, true)) return u;
            }
            return null;
        }

        private static void SetUnitsSpeedV385ALikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            C2BrigadeOrderGoOnRoadStateV385A state)
        {
            if (group == null || state == null) return;
            long averageSum = 0;
            int averageCount = 0;
            int count = group.Units.Count;

            for (int i = 0; i < count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal unit = group.Units[i];
                if (!IsUsableFormationUnitV172LikeOriginal(unit, true)) continue;
                C2UnitOriginalRuntimeLinkLikeOriginal link = unit.RuntimeLinkCachedLikeOriginal;
                C2UnitOriginalRuntime rt = link != null ? link.Runtime : null;
                if (rt == null) continue;
                int ds = C2OriginalMovementMathV352.Norma(
                    (Mathf.RoundToInt(rt.RuntimeRealXLikeOriginal) >> 4) - (Mathf.RoundToInt(rt.MoveTargetRealXLikeOriginal) >> 4),
                    (Mathf.RoundToInt(rt.RuntimeRealYLikeOriginal) >> 4) - (Mathf.RoundToInt(rt.MoveTargetRealYLikeOriginal) >> 4));
                if (ds < 200)
                {
                    averageSum += ds;
                    averageCount++;
                }
            }
            if (averageCount <= 0) return;

            int average = (int)(averageSum / averageCount);
            
            int commandPrefix = Mathf.Clamp(state.CommandPrefix, 0, count);
            for (int i = 0; i < count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal unit = group.Units[i];
                if (!IsUsableFormationUnitV172LikeOriginal(unit, true)) continue;
                C2UnitOriginalRuntimeLinkLikeOriginal link = unit.RuntimeLinkCachedLikeOriginal;
                C2UnitOriginalRuntime rt = link != null ? link.Runtime : null;
                if (rt == null) continue;
                int ds = C2OriginalMovementMathV352.Norma(
                    (Mathf.RoundToInt(rt.RuntimeRealXLikeOriginal) >> 4) - (Mathf.RoundToInt(rt.MoveTargetRealXLikeOriginal) >> 4),
                    (Mathf.RoundToInt(rt.RuntimeRealYLikeOriginal) >> 4) - (Mathf.RoundToInt(rt.MoveTargetRealYLikeOriginal) >> 4));
                int speed = (C2RoadSpeedV385A * (ds + 1)) / (average + 1);
                speed = Mathf.Clamp(speed, C2RoadMinSpeedV385A, C2RoadMaxSpeedV385A);
                rt.OriginalUnitSpeedLikeOriginal = speed;
            }
        }

        private static void FinishBrigadeGoOnRoadV385ALikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            C2BrigadeOrderGoOnRoadStateV385A state,
            string reason)
        {
            if (group == null || state == null) return;

            // BrigadeOrders.cpp::BrigadeOrder_GoOnRoad::Process:
            // CreateOrderedPositions(last road point, current brigade direction),
            // ResortMembByPos(), DeleteNewBOrder(), KeepPositions(1,Prio).
            if (state.MaxPIndex > 0 && state.PointsReal != null && state.PointsReal.Length > 0)
            {
                Vector2 tail = state.PointsReal[Mathf.Min(state.MaxPIndex, state.PointsReal.Length) - 1];
                C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal record =
                    ResolveRecordForGroupV320LikeOriginal(group);
                C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal option =
                    FindFormationOptionV320LikeOriginal(record, group.Shape);
                List<Vector2> slots = BuildFormationOrderSlotsAtV359LikeOriginal(
                    group, group.Units, option, tail.x, tail.y, group.Direction);
                int commandPrefix = Mathf.Clamp(state.CommandPrefix, 0, group.Units.Count);
                ReorderSoldiersForNearestSlotsV172LikeOriginal(group.Units, slots, commandPrefix);
                group.Slots.Clear();
                for (int i = 0; i < slots.Count; i++) group.Slots.Add(slots[i]);
                group.CommandSlotCount = commandPrefix;
            }

            group.UsesRoadMovement = false;
            for (int i = 0; i < group.Units.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal unit = group.Units[i];
                if (unit == null) continue;
                C2RoadUnitSpeedControllerV352.DetachLikeOriginal(unit);
            }

            _roadOrdersV385A.Remove(group.GroupId);
            DeleteBrigadeNewOrderV418LikeOriginal(
                group, BrigadeOrderGoOnRoadV418LikeOriginal,
                "BrigadeOrder_GoOnRoad::DeleteNewBOrder");

            int kpAcceptedV418 = QueueBrigadeKeepPositionsV416LikeOriginal(
                group, state.Priority, 1, "BrigadeOrder_GoOnRoad::KeepPositions");
            ProcessQueuedBrigadeKeepPositionsNowV416LikeOriginal(group);

            Debug.Log("[C2:ROAD ORDER V426 FINISH] group=" + group.GroupId.ToString(CultureInfo.InvariantCulture) +
                      " reason=" + (reason ?? string.Empty) +
                      " kp=" + kpAcceptedV418.ToString(CultureInfo.InvariantCulture) +
                      " underlying_hgst_preserved=1");
        }

        private static void CancelBrigadeGoOnRoadV385ALikeOriginal(
            int groupId,
            string reason,
            bool stopMembers)
        {
            C2BrigadeOrderGoOnRoadStateV385A state;
            if (!_roadOrdersV385A.TryGetValue(groupId, out state) || state == null) return;
            _roadOrdersV385A.Remove(groupId);

            RuntimeFormationV172LikeOriginal group;
            if (_groupsByIdV172LikeOriginal.TryGetValue(groupId, out group) && group != null)
            {
                DeleteBrigadeNewOrderV418LikeOriginal(
                    group, BrigadeOrderGoOnRoadV418LikeOriginal,
                    reason ?? "GoOnRoad_cancel");
                group.UsesRoadMovement = false;
                for (int i = 0; i < group.Units.Count; i++)
                {
                    C2NeutralPeasantUnitInfoV2LikeOriginal unit = group.Units[i];
                    if (unit == null) continue;
                    C2RoadUnitSpeedControllerV352.DetachLikeOriginal(unit);
                    C2UnitOriginalRuntimeLinkLikeOriginal link = unit.RuntimeLinkCachedLikeOriginal;
                    if (link != null && link.Runtime != null)
                        link.Runtime.OriginalUnitSpeedLikeOriginal = 64;
                    if (stopMembers && IsUsableFormationUnitV172LikeOriginal(unit, true))
                        unit.StopMoveAndFaceDirectionLikeOriginal(group.Direction);
                }
            }

            Debug.Log("[C2:ROAD ORDER V385A CANCEL] group=" + groupId.ToString(CultureInfo.InvariantCulture) +
                      " reason=" + (reason ?? string.Empty));
        }

        private static int CountLiveRoadMembersV385ALikeOriginal(RuntimeFormationV172LikeOriginal group)
        {
            if (group == null) return 0;
            int n = 0;
            for (int i = 0; i < group.Units.Count; i++)
                if (IsUsableFormationUnitV172LikeOriginal(group.Units[i], true)) n++;
            return n;
        }
    }
}

// ============================================================================
// V425 unified path.cpp ownership
// ============================================================================
namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // Native MFIELDS owner for the Unity bridge. The four logical fields stay
    // separate exactly like CII (0 land, 1 water, 2 horse, 3 auxiliary land).
    // Static terrain/water data is built once per parsed map; dynamic building
    // LOCKPOINTS remain an overlay so construction/destruction changes immediately.
    public sealed partial class C2BattleTerrainMode
    {
        private struct PathCost
        {
            internal long Ticks, Bytes, PeakTicks, PeakBytes;
            internal int Calls, Routes, Direct, Failed;
        }
        private static readonly Dictionary<string, PathCost> PathCosts =
            new Dictionary<string, PathCost>(StringComparer.Ordinal);
        private static int PathCostWindowStart;

        private static void RecordPathCost(string source, long ticks, long bytes, bool route, bool direct)
        {
            source = string.IsNullOrEmpty(source) ? "unspecified" : source;
            PathCosts.TryGetValue(source, out PathCost cost);
            cost.Calls++;
            if (route) cost.Routes++;
            else if (direct) cost.Direct++;
            else cost.Failed++;
            cost.Ticks += ticks;
            cost.Bytes += bytes;
            cost.PeakTicks = Math.Max(cost.PeakTicks, ticks);
            cost.PeakBytes = Math.Max(cost.PeakBytes, bytes);
            PathCosts[source] = cost;
            int now = Environment.TickCount;
            if (PathCostWindowStart == 0) PathCostWindowStart = now;
            int elapsed = unchecked(now - PathCostWindowStart);
            if (elapsed < 5000) return;
            var message = new System.Text.StringBuilder(1024);
            message.Append("[C2:PATH COST] timeSec=").Append(Time.realtimeSinceStartup.ToString("0.000", CultureInfo.InvariantCulture));
            message.Append(" elapsedMs=").Append(elapsed);
            double toMs = 1000.0 / global::System.Diagnostics.Stopwatch.Frequency;
            foreach (var pair in PathCosts)
            {
                PathCost s = pair.Value;
                message.Append(" | ").Append(pair.Key).Append(" calls=").Append(s.Calls);
                message.Append(" routes=").Append(s.Routes).Append(" direct=").Append(s.Direct).Append(" failed=").Append(s.Failed);
                message.Append(" totalMs=").Append((s.Ticks * toMs).ToString("0.000", CultureInfo.InvariantCulture));
                message.Append(" peakMs=").Append((s.PeakTicks * toMs).ToString("0.000", CultureInfo.InvariantCulture));
                message.Append(" bytes=").Append(s.Bytes).Append(" peakBytes=").Append(s.PeakBytes);
            }
            Debug.Log(message.ToString());
            PathCosts.Clear();
            PathCostWindowStart = now;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPathCosts()
        {
            PathCosts.Clear();
            PathCostWindowStart = 0;
        }

        public static bool C2BuildingMotionFieldV1TryBuildPathRealLikeOriginal(
            float fromRealX, float fromRealY, float toRealX, float toRealY,
            out Vector2[] path, int maxSearchCells, string profileSourceLikeOriginal = null)
        {
            return C2BuildingMotionFieldV1TryBuildPathOrDirectRealLikeOriginal(
                fromRealX, fromRealY, toRealX, toRealY, out path, out _, maxSearchCells, profileSourceLikeOriginal);
        }

        public static bool C2BuildingMotionFieldV1TryBuildPathOrDirectRealLikeOriginal(
            float fromRealX, float fromRealY, float toRealX, float toRealY,
            out Vector2[] path, out bool directTravelClear, int maxSearchCells,
            string profileSourceLikeOriginal = null, int radiusCells = 1)
        {
            bool success;
            long ticks, bytes;
            using (C2FrameCostProbe.Measure(C2FrameCostProbe.Phase.Paths))
            {
                long allocated = C2FrameCostProbe.AllocatedBytes;
                long started = global::System.Diagnostics.Stopwatch.GetTimestamp();
                success = C2BuildingRuntimeInfoV247LikeOriginal.TryBuildPathRealV247LikeOriginal(
                    fromRealX, fromRealY, toRealX, toRealY, out path, out directTravelClear, maxSearchCells, radiusCells);
                ticks = global::System.Diagnostics.Stopwatch.GetTimestamp() - started;
                bytes = Math.Max(0, C2FrameCostProbe.AllocatedBytes - allocated);
            }
            RecordPathCost(profileSourceLikeOriginal, ticks, bytes, success, directTravelClear);
            return success;
        }
        public static bool C2BuildingMotionFieldV1IsBlockedForUnitRealLikeOriginal(
            float realX,
            float realY,
            int radiusCells)
        {
            return C2BuildingRuntimeInfoV247LikeOriginal.IsBlockedForUnitRealV247LikeOriginal(realX, realY, radiusCells);
        }

        public static bool C2BuildingMotionFieldV1CanTravelStraightRealLikeOriginal(
            float fromRealX,
            float fromRealY,
            float toRealX,
            float toRealY)
        {
            return C2BuildingRuntimeInfoV247LikeOriginal.CanTravelStraightRealV247LikeOriginal(
                fromRealX, fromRealY, toRealX, toRealY);
        }

        public static bool C2BuildingMotionFieldV1IsBlockedRealLikeOriginal(float realX, float realY)
        {
            return C2BuildingRuntimeInfoV247LikeOriginal.IsBlockedRealV247LikeOriginal(realX, realY);
        }

        public static bool C2BuildingMotionFieldV1TryFindNearestFreeRealLikeOriginal(
            float realX,
            float realY,
            out float freeRealX,
            out float freeRealY,
            int maxRadiusCells)
        {
            return C2BuildingRuntimeInfoV247LikeOriginal.TryFindNearestFreeRealV247LikeOriginal(
                realX,
                realY,
                out freeRealX,
                out freeRealY,
                maxRadiusCells);
        }


        private bool[][] _c2MovementNativeFieldsV425;
        private string _c2MovementNativeFieldsSourceV425 = string.Empty;
        private int _c2MovementNativeFieldSxV425;
        private int _c2MovementNativeFieldSyV425;

        internal bool C2MovementNativeFieldCheckPtV425LikeOriginal(int x, int y, byte lockType)
        {
            bool blocked = C2MovementNativeFieldBaseCheckPtV425LikeOriginal(x, y, lockType);
            int field = Mathf.Clamp(lockType, 0, 3);
            // NewMon.cpp::BSetBar sets MFIELDS 0,2,3. Runtime building locks must
            // therefore not be silently projected into MFIELDS[1] (water).
            if (field != 1 && C2BuildingRuntimeInfoV247LikeOriginal.IsBlockedCellV247LikeOriginal(x, y))
                blocked = true;
            if (C2OriginalMovementSystemV425LikeOriginal.DynamicGlockCheckPtV425LikeOriginal(x, y, (byte)field))
                blocked = true;
            return blocked;
        }

        internal bool C2MovementNativeFieldBaseCheckPtV425LikeOriginal(int x, int y, byte lockType)
        {
            if (_map == null) return true;
            if (!C2OriginalMovementSystemV425LikeOriginal.FieldsPreparedV433)
                EnsureMovementNativeFieldsV425LikeOriginal();
            if (_c2MovementNativeFieldsV425 == null || x < 0 || y < 0 ||
                x >= _c2MovementNativeFieldSxV425 || y >= _c2MovementNativeFieldSyV425)
                return true;
            int field = Mathf.Clamp(lockType, 0, Math.Min(3, _c2MovementNativeFieldsV425.Length - 1));
            return _c2MovementNativeFieldsV425[field][x * _c2MovementNativeFieldSyV425 + y];
        }

        internal void PrepareMovementNativeFieldsV433() => EnsureMovementNativeFieldsV425LikeOriginal();

        private void EnsureMovementNativeFieldsV425LikeOriginal()
        {
            if (_map == null) return;
            string source = _map.SourcePath ?? string.Empty;
            int sx = Math.Max(0, _map.MAPSX);
            int sy = Math.Max(0, _map.MAPSY);
            if (_c2MovementNativeFieldsV425 != null &&
                _c2MovementNativeFieldSxV425 == sx && _c2MovementNativeFieldSyV425 == sy &&
                string.Equals(_c2MovementNativeFieldsSourceV425, source, StringComparison.OrdinalIgnoreCase))
                return;
            if (sx <= 0 || sy <= 0) return;

            _c2MovementNativeFieldSxV425 = sx;
            _c2MovementNativeFieldSyV425 = sy;
            _c2MovementNativeFieldsSourceV425 = source;
            _c2MovementNativeFieldsV425 = new bool[4][];
            int total = checked(sx * sy);
            for (int f = 0; f < 4; f++) _c2MovementNativeFieldsV425[f] = new bool[total];

            // 3DMapEd.cpp::CreateLandLocking. One terrain macrocell is 64x64
            // original pixels and writes a 4x4 MotionField block (16 px cells).
            int mxx = sx >> 2;
            int myy = sy >> 2;
            for (int ix = 0; ix < mxx; ix++)
            {
                int x0 = ix << 6;
                int ppx = ix << 2;
                for (int iy = myy - 1; iy >= 0; iy--)
                {
                    int y0 = iy << 6;
                    int ppy = iy << 2;
                    int z0 = C2OriginalFogTerrainHeightV1LikeOriginal(x0, y0);
                    int z1 = C2OriginalFogTerrainHeightV1LikeOriginal(x0 + 64, y0);
                    int z2 = C2OriginalFogTerrainHeightV1LikeOriginal(x0, y0 + 64);
                    int z3 = C2OriginalFogTerrainHeightV1LikeOriginal(x0 + 64, y0 + 64);
                    int za = (z0 + z1 + z2 + z3) >> 2;
                    bool steep = Math.Abs(z0 - za) >= 23 || Math.Abs(z1 - za) >= 23 ||
                                 Math.Abs(z2 - za) >= 23 || Math.Abs(z3 - za) >= 23;
                    SetFieldBarV425(0, ppx, ppy, 4, steep || za < -10);
                    SetFieldBarV425(1, ppx, ppy, 4, za > 0);
                    SetFieldBarV425(2, ppx, ppy, 4, steep || za < 0);
                    SetFieldBarV425(3, ppx, ppy, 4, steep || za < -10);
                }
            }

            // HashTop.cpp::SmoothFields: texture flag 256*64 is the bog lock.
            TerrainTextureTablesLikeOriginal tables = GetTerrainTextureTablesLikeOriginal();
            if (tables != null && _map.TexMap != null && _map.VertInLine > 0 && _map.MaxTH > 0)
            {
                int ofs = 0;
                for (int iy = 0; iy < _map.MaxTH; iy++)
                    for (int ix = 0; ix < _map.VertInLine; ix++, ofs++)
                    {
                        if (ofs < 0 || ofs >= _map.TexMap.Length) continue;
                        int tex = _map.TexMap[ofs];
                        if (tex >= 0 && tex < tables.TexFlags.Length && (tables.TexFlags[tex] & (256 * 64)) != 0)
                        {
                            SetFieldBarV425(2, ix << 1, iy << 1, 4, true);
                            SetFieldBarV425(3, ix << 1, iy << 1, 4, true);
                        }
                    }
            }

            // HashTop.cpp::CreateMFieldLocking for saved sprites that explicitly
            // carry LockType bits. The parsed saved-map resource entries preserve
            // the packed Locking mask from TRE1/TRE2.
            if (_c2OriginalResourcesV1 != null)
            {
                for (int i = 0; i < _c2OriginalResourcesV1.Count; i++)
                {
                    C2OriginalResourceSpriteV1 sp = _c2OriginalResourcesV1[i];
                    if (sp == null || !sp.Enabled) continue;
                    for (int f = 2; f <= 3; f++)
                        if ((sp.Locking & (1 << f)) != 0)
                            SetFieldRectV425(f, (sp.X >> 4) - 4, (sp.Y >> 4) - 4, 8, 8, true);
                }
            }

            SmoothNativeFieldV425LikeOriginal(0);
            SmoothNativeFieldV425LikeOriginal(2);
            SmoothNativeFieldV425LikeOriginal(3);
            ApplyNativeWaterLockingV425LikeOriginal();
        }

        private void ApplyNativeWaterLockingV425LikeOriginal()
        {
            C2WaterData water = _map != null ? _map.Water : null;
            if (water == null || !water.HasSea2Payload) return;
            int maxX = Math.Min(water.SeaLx - 1, (_c2MovementNativeFieldSxV425 + 1) >> 1);
            int maxY = Math.Min(water.SeaLy - 1, (_c2MovementNativeFieldSyV425 + 1) >> 1);
            for (int iy = 0; iy < maxY; iy++)
                for (int ix = 0; ix < maxX; ix++)
                {
                    int z1 = water.GetWaterDeep(ix, iy);
                    int z2 = water.GetWaterDeep(ix + 1, iy);
                    int z3 = water.GetWaterDeep(ix, iy + 1);
                    int z4 = water.GetWaterDeep(ix + 1, iy + 1);
                    int d1 = WaterDepthInterpV425(z1, z2, z3, z4);
                    int d2 = WaterDepthInterpV425(z2, z4, z1, z3);
                    int d3 = WaterDepthInterpV425(z3, z4, z1, z2);
                    int d4 = WaterDepthInterpV425(z4, z3, z2, z1);
                    ApplyWaterPointV425(ix + ix,     iy + iy,     d1);
                    ApplyWaterPointV425(ix + ix + 1, iy + iy,     d2);
                    ApplyWaterPointV425(ix + ix,     iy + iy + 1, d3);
                    ApplyWaterPointV425(ix + ix + 1, iy + iy + 1, d4);
                }
        }

        private static int WaterDepthInterpV425(int z1, int z2, int z3, int z4)
        {
            // RealWater.cpp::GetZ => (9*z1 + 3*z2 + 3*z3 + z4) / 16.
            return (3 * (3 * z1 + z2 + z3) + z4) >> 4;
        }

        private void ApplyWaterPointV425(int x, int y, int depth)
        {
            if (x < 0 || y < 0 || x >= _c2MovementNativeFieldSxV425 || y >= _c2MovementNativeFieldSyV425) return;
            int ofs = x * _c2MovementNativeFieldSyV425 + y;
            if (depth > 130) _c2MovementNativeFieldsV425[1][ofs] = false;
            if (depth > 175) _c2MovementNativeFieldsV425[0][ofs] = true;
            if (depth > 190) _c2MovementNativeFieldsV425[2][ofs] = true;
        }

        private void SmoothNativeFieldV425LikeOriginal(int field)
        {
            bool[] mf = _c2MovementNativeFieldsV425[field];
            int sx = _c2MovementNativeFieldSxV425, sy = _c2MovementNativeFieldSyV425;
            int mxx = sx >> 2, myy = sy >> 2;
            for (int ix = 1; ix < mxx; ix++)
                for (int iy = 1; iy < myy; iy++)
                {
                    int x = ix << 2, y = iy << 2, n = 0;
                    if (CheckFieldBarV425(mf, sx, sy, x, y, 4, 4))
                    {
                        if (CheckFieldBarV425(mf, sx, sy, x, y, 2, 2)) n++;
                        if (CheckFieldBarV425(mf, sx, sy, x + 2, y, 2, 2)) n++;
                        if (CheckFieldBarV425(mf, sx, sy, x, y + 2, 2, 2)) n++;
                        if (CheckFieldBarV425(mf, sx, sy, x + 2, y + 2, 2, 2)) n++;
                    }
                    SetFieldBarV425(field, x, y, 4, n >= 3);
                }

            for (int pass = 0; pass < 2; pass++)
            {
                bool[] snap = (bool[])mf.Clone();
                for (int ix = 1; ix < mxx; ix++)
                    for (int iy = 1; iy < myy; iy++)
                    {
                        int x = ix << 2, y = iy << 2;
                        int c = 0;
                        if (CheckFieldBarV425(snap, sx, sy, x + 1, y - 3, 2, 2)) c |= 1;
                        if (CheckFieldBarV425(snap, sx, sy, x + 5, y + 1, 2, 2)) c |= 2;
                        if (CheckFieldBarV425(snap, sx, sy, x + 1, y + 5, 2, 2)) c |= 4;
                        if (CheckFieldBarV425(snap, sx, sy, x - 3, y + 1, 2, 2)) c |= 8;
                        bool centerBlocked = CheckFieldBarV425(snap, sx, sy, x + 1, y + 1, 2, 2);
                        if (pass == 0 && !centerBlocked)
                        {
                            if (c == 3) SetLockMaskV425(field, x, y, "**** ***  **   *");
                            else if (c == 6) SetLockMaskV425(field, x, y, "   *  ** *******");
                            else if (c == 12) SetLockMaskV425(field, x, y, "*   **  *** ****");
                            else if (c == 9) SetLockMaskV425(field, x, y, "******* **  *   ");
                            else if (c == 7 || c == 14 || c == 13 || c == 11) SetFieldBarV425(field, x, y, 4, true);
                        }
                        else if (pass == 1 && centerBlocked)
                        {
                            if (c == 3) SetLockMaskV425(field, x, y, "**** ***  **   *");
                            else if (c == 6) SetLockMaskV425(field, x, y, "   *  ** *******");
                            else if (c == 12) SetLockMaskV425(field, x, y, "*   **  *** ****");
                            else if (c == 9) SetLockMaskV425(field, x, y, "******* **  *   ");
                        }
                    }
            }
        }

        private void SetLockMaskV425(int field, int x, int y, string mask)
        {
            int p = 0;
            for (int iy = 0; iy < 4; iy++)
                for (int ix = 0; ix < 4; ix++, p++)
                    SetFieldPointV425(field, x + ix, y + iy, p < mask.Length && mask[p] == '*');
        }

        private void SetFieldBarV425(int field, int x, int y, int side, bool value)
        {
            SetFieldRectV425(field, x, y, side, side, value);
        }

        private void SetFieldRectV425(int field, int x, int y, int lx, int ly, bool value)
        {
            if (_c2MovementNativeFieldsV425 == null || field < 0 || field >= _c2MovementNativeFieldsV425.Length) return;
            for (int ix = 0; ix < lx; ix++)
                for (int iy = 0; iy < ly; iy++)
                    SetFieldPointV425(field, x + ix, y + iy, value);
        }

        private void SetFieldPointV425(int field, int x, int y, bool value)
        {
            if (x < 0 || y < 0 || x >= _c2MovementNativeFieldSxV425 || y >= _c2MovementNativeFieldSyV425) return;
            _c2MovementNativeFieldsV425[field][x * _c2MovementNativeFieldSyV425 + y] = value;
        }

        private static bool CheckFieldBarV425(bool[] field, int sx, int sy, int x, int y, int lx, int ly)
        {
            for (int ix = 0; ix < lx; ix++)
                for (int iy = 0; iy < ly; iy++)
                {
                    int xx = x + ix, yy = y + iy;
                    if (xx <= 0 || yy <= 0 || xx >= sx || yy >= sy) return true;
                    if (field[xx * sy + yy]) return true;
                }
            return false;
        }
    }

    internal static class C2OriginalMovementSystemV425LikeOriginal
    {
        private struct PathRequestV425
        {
            internal C2NeutralPeasantUnitInfoV2LikeOriginal Unit;
            internal ushort Serial;
            internal int ToCellX;
            internal int ToCellY;
            internal float Speed;
            internal bool HasFinalFacing;
            internal byte FinalFacing;
            internal string Source;
            internal bool Enabled;
        }

        private struct RadioPointV425
        {
            internal sbyte X;
            internal sbyte Y;
            internal RadioPointV425(int x, int y) { X = (sbyte)x; Y = (sbyte)y; }
        }

        private static readonly List<PathRequestV425> PathReqV425 = new List<PathRequestV425>(4096);
        // CII keeps two distinct dynamic occupancy systems:
        //  * MFIELDS[LockType] GLock rounds: standing/blocked units obstruct path/motion.
        //  * UnitsField: all ordinary units occupy it even while moving.
        // UnitsField preserves native set/clear-bit semantics across overlapping units.
        // The GLock overlay tracks its independent owners above static terrain.
        private static int[][] DynamicGlockV425;
        private static byte[] UnitsFieldV425;
        private static readonly List<C2UnitOriginalRuntime> UnitsFieldRegistrationsV433 = new List<C2UnitOriginalRuntime>();
        private static readonly List<int>[] DynamicDirtyV433 = {
            new List<int>(), new List<int>(), new List<int>(), new List<int>() };
        private static C2BattleTerrainMode DynamicOwnerV433;
        private static string DynamicSourceV433;
        private static int UnitsFieldGenerationV433;
        private static int DynamicFieldSxV425;
        private static int DynamicFieldSyV425;
        private static readonly List<RadioPointV425>[] RarrV425 = BuildRadioV425(64);
        private static int[] SqListWidthV425(int d)
        {
            int[] a = new int[d];
            for (int j = 0; j < d; j++)
            {
                int r = (d * 2) - 1;
                int x = j * 4 - (d - 1) * 2;
                float v = Mathf.Sqrt(Mathf.Max(0, r * r - x * x));
                a[j] = ((d - 1) & 1) != 0 ? 2 * Mathf.FloorToInt((v + 2.0f) / 4.0f) : 2 * Mathf.FloorToInt(v / 4.0f) + 1;
            }
            return a;
        }
        private static readonly Dictionary<int, int[]> SqListV425 = new Dictionary<int, int[]>();

        private static int[] GetSqWidthsV425(int d)
        {
            int[] a;
            if (!SqListV425.TryGetValue(d, out a))
            {
                a = SqListWidthV425(d);
                SqListV425[d] = a;
            }
            return a;
        }

        private static List<RadioPointV425>[] BuildRadioV425(int radius)
        {
            var result = new List<RadioPointV425>[radius];
            for (int i = 0; i < radius; i++) result[i] = new List<RadioPointV425>(Mathf.Max(8, i * 8));
            // TopoGraf.cpp::CreateRadio iteration order is ix outer, iy inner.
            for (int ix = -radius; ix <= radius; ix++)
                for (int iy = -radius; iy <= radius; iy++)
                {
                    int r = (int)Math.Sqrt((long)ix * ix + (long)iy * iy);
                    if (r < radius) result[r].Add(new RadioPointV425(ix, iy));
                }
            if (radius > 1 && result[1].Count >= 8)
            {
                result[1][0] = new RadioPointV425(-1, 0);
                result[1][1] = new RadioPointV425( 1, 0);
                result[1][2] = new RadioPointV425( 0,-1);
                result[1][3] = new RadioPointV425( 0, 1);
                result[1][4] = new RadioPointV425(-1, 1);
                result[1][5] = new RadioPointV425(-1,-1);
                result[1][6] = new RadioPointV425( 1,-1);
                result[1][7] = new RadioPointV425( 1, 1);
            }
            return result;
        }

        internal static bool FieldsPreparedV433 { get; private set; }
        internal readonly struct FieldBatchV433 : IDisposable
        {
            private readonly bool previous;
            internal FieldBatchV433(bool prepare)
            {
                previous = FieldsPreparedV433;
                if (!previous && EnsureDynamicFieldsV425LikeOriginal())
                {
                    DynamicOwnerV433.PrepareMovementNativeFieldsV433();
                    FieldsPreparedV433 = true;
                }
            }
            public void Dispose() { FieldsPreparedV433 = previous; }
        }
        internal static FieldBatchV433 PrepareFieldBatchV433() => new FieldBatchV433(true);

        private static bool EnsureDynamicFieldsV425LikeOriginal()
        {
            if (FieldsPreparedV433) return true;
            C2BattleTerrainMode mode = ResolveMotionFieldModeV425LikeOriginal();
            int addsh, sx, sy; string source;
            if (mode == null || !mode.C2TopologyTryGetMapGeometryV401LikeOriginal(out addsh, out sx, out sy, out source) || sx <= 0 || sy <= 0)
                return false;
            if (DynamicGlockV425 != null && DynamicFieldSxV425 == sx && DynamicFieldSyV425 == sy &&
                ReferenceEquals(DynamicOwnerV433, mode) && DynamicSourceV433 == source)
                return true;
            foreach (var registered in UnitsFieldRegistrationsV433)
                registered.UnitsFieldRegisteredV433 = false;
            UnitsFieldRegistrationsV433.Clear();
            DynamicOwnerV433 = mode;
            DynamicSourceV433 = source;
            for (int f = 0; f < 4; f++) DynamicDirtyV433[f].Clear();
            DynamicFieldSxV425 = sx;
            DynamicFieldSyV425 = sy;
            DynamicGlockV425 = new int[4][];
            int n = checked(sx * sy);
            for (int i = 0; i < 4; i++) DynamicGlockV425[i] = new int[n];
            UnitsFieldV425 = new byte[n];
            return true;
        }

        internal static bool DynamicGlockCheckPtV425LikeOriginal(int x, int y, byte lockType)
        {
            if (!EnsureDynamicFieldsV425LikeOriginal()) return false;
            if (x < 0 || y < 0 || x >= DynamicFieldSxV425 || y >= DynamicFieldSyV425) return false;
            int f = Mathf.Clamp(lockType, 0, 3);
            return DynamicGlockV425[f][x * DynamicFieldSyV425 + y] > 0;
        }

        private static void AddRoundCountV425LikeOriginal(int[] field, int x, int y, int d, int delta)
        {
            if (field == null || d < 1) return;
            int[] widths = GetSqWidthsV425(d);
            for (int ix = 0; ix < d; ix++)
            {
                int len = widths[ix];
                int yy0 = y + (d - len) / 2;
                int xx = x + ix;
                if (xx < 0 || xx >= DynamicFieldSxV425) continue;
                for (int j = 0; j < len; j++)
                {
                    int yy = yy0 + j;
                    if (yy < 0 || yy >= DynamicFieldSyV425) continue;
                    int ofs = xx * DynamicFieldSyV425 + yy;
                    int nv = field[ofs] + delta;
                    field[ofs] = nv < 0 ? 0 : nv;
                }
            }
        }

        private static void AddGlockRoundV425LikeOriginal(int x, int y, int d, byte lockType, int delta)
        {
            if (!EnsureDynamicFieldsV425LikeOriginal()) return;
            int f = Mathf.Clamp(lockType, 0, 3);
            int[] widths = GetSqWidthsV425(d);
            for (int ix = 0; ix < d; ix++)
            for (int j = 0; j < widths[ix]; j++)
            {
                int xx = x + ix, yy = y + (d - widths[ix]) / 2 + j;
                if (xx < 0 || yy < 0 || xx >= DynamicFieldSxV425 || yy >= DynamicFieldSyV425) continue;
                int ofs = xx * DynamicFieldSyV425 + yy;
                if (DynamicGlockV425[f][ofs] == 0 && delta > 0) DynamicDirtyV433[f].Add(ofs);
                DynamicGlockV425[f][ofs] = Math.Max(0, DynamicGlockV425[f][ofs] + delta);
            }
        }

        // Mechanics.cpp::XorBlockComplexUnit uses BXorBar on MFIELDS 0,2,3
        // (water field 1 is deliberately skipped).  Complex objects are therefore
        // represented by a square dynamic MFIELDS overlay, not the ordinary round
        // GLock/UnitsField footprint used by SINGLESTEP/NEWSHEEPS.
        private static void AddGlockBarV430LikeOriginal(int x, int y, int lx, int field, int delta)
        {
            if (!EnsureDynamicFieldsV425LikeOriginal() || lx < 1) return;
            int f = Mathf.Clamp(field, 0, 3);
            int[] dst = DynamicGlockV425[f];
            for (int ix = x; ix < x + lx; ix++)
            {
                if (ix < 0 || ix >= DynamicFieldSxV425) continue;
                for (int iy = y; iy < y + lx; iy++)
                {
                    if (iy < 0 || iy >= DynamicFieldSyV425) continue;
                    int ofs = ix * DynamicFieldSyV425 + iy;
                    if (dst[ofs] == 0 && delta > 0) DynamicDirtyV433[f].Add(ofs);
                    int nv = dst[ofs] + delta;
                    dst[ofs] = nv < 0 ? 0 : nv;
                }
            }
        }

        private static void ApplyComplexObjectLockDeltaV430LikeOriginal(
            C2UnitOriginalRuntime rt, int delta)
        {
            if (rt == null || rt.Info == null || rt.OriginalComplexObjectV430LikeOriginal == null) return;
            C2ComplexObjectRuntimeV430LikeOriginal cob = rt.OriginalComplexObjectV430LikeOriginal;
            int nq = cob.Quants != null ? cob.Quants.Length : 0;
            if (nq <= 0) return;

            // Retail Mechanics.cpp loops NQuants times over the same BXorBar.  With
            // XOR storage that means an even-length chain cancels itself and an odd
            // chain leaves one bar. Preserve that retail behavior instead of silently
            // 'fixing' the source typo.
            if ((nq & 1) == 0) return;

            int lx = ResolveLxLikeOriginal(rt.Info);
            int x = (Mathf.RoundToInt(cob.RealX) - (lx << 7)) >> 8;
            int y = (Mathf.RoundToInt(cob.RealY) - (lx << 7)) >> 8;
            AddGlockBarV430LikeOriginal(x, y, lx, 0, delta);
            AddGlockBarV430LikeOriginal(x, y, lx, 2, delta);
            AddGlockBarV430LikeOriginal(x, y, lx, 3, delta);
        }

        internal static void BeginComplexObjectOccupancyV430LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit, C2UnitOriginalRuntime rt)
        {
            if (unit == null || rt == null || !EnsureDynamicFieldsV425LikeOriginal()) return;
            int lx = ResolveLxLikeOriginal(unit);
            int x = RealCenterToObjectCellV425(rt.RuntimeRealXLikeOriginal, lx);
            int y = RealCenterToObjectCellV425(rt.RuntimeRealYLikeOriginal, lx);
            if (rt.OriginalGLockV425LikeOriginal)
                AddGlockRoundV425LikeOriginal(x, y, lx, ResolveLockTypeV425LikeOriginal(unit), -1);
            C2UnitOriginalRuntimeLinkLikeOriginal link = unit.RuntimeLinkCachedLikeOriginal;
            bool unlimited = link != null && link.IsUnlimitedMotionV415LikeOriginal;
            if (!unlimited)
                AddUnitsFieldBarV427LikeOriginal(x, y, lx, -1);
            rt.OriginalGLockV425LikeOriginal = false;
            rt.OriginalLockStateInitializedV425LikeOriginal = true;
        }

        internal static void UnlockComplexObjectV430LikeOriginal(C2UnitOriginalRuntime rt)
        {
            if (rt == null || rt.OriginalComplexObjectV430LikeOriginal == null) return;
            C2ComplexObjectRuntimeV430LikeOriginal cob = rt.OriginalComplexObjectV430LikeOriginal;
            if (!cob.Lockpoints) return;
            ApplyComplexObjectLockDeltaV430LikeOriginal(rt, -1);
            cob.Lockpoints = false;
        }

        internal static void LockComplexObjectV430LikeOriginal(C2UnitOriginalRuntime rt)
        {
            if (rt == null || rt.OriginalComplexObjectV430LikeOriginal == null) return;
            C2ComplexObjectRuntimeV430LikeOriginal cob = rt.OriginalComplexObjectV430LikeOriginal;
            if (cob.Lockpoints) return;
            ApplyComplexObjectLockDeltaV430LikeOriginal(rt, +1);
            cob.Lockpoints = true;
        }

        private static void ReapplyComplexObjectLockV430LikeOriginal(C2UnitOriginalRuntime rt)
        {
            if (rt == null || rt.OriginalComplexObjectV430LikeOriginal == null ||
                !rt.OriginalComplexObjectV430LikeOriginal.Lockpoints) return;
            ApplyComplexObjectLockDeltaV430LikeOriginal(rt, +1);
        }

        // path.cpp::MotionField::BSetBar/BClrBar are SQUARE footprints.
        // Do not reuse SetRound/ClearRound here: MFIELDS GLock is round, UnitsField is not.
        private static void AddUnitsFieldBarV427LikeOriginal(int x, int y, int lx, int delta)
        {
            if (!EnsureDynamicFieldsV425LikeOriginal() || lx < 1) return;
            for (int ix = x; ix < x + lx; ix++)
            {
                if (ix < 0 || ix >= DynamicFieldSxV425) continue;
                for (int iy = y; iy < y + lx; iy++)
                {
                    if (iy < 0 || iy >= DynamicFieldSyV425) continue;
                    int ofs = ix * DynamicFieldSyV425 + iy;
                    // Native BSetBar/BClrBar are bit operations, not reference
                    // counts. Crossing soldiers can overlap the same cell; a
                    // clear must clear its bit even if another soldier was there.
                    UnitsFieldV425[ofs] = delta > 0 ? (byte)1 : (byte)0;
                }
            }
        }

        // path.cpp::MotionField::CheckBar: rectangular bar, out-of-map is blocked.
        internal static bool UnitsFieldCheckBarV427LikeOriginal(int x, int y, int lx, int ly)
        {
            if (!EnsureDynamicFieldsV425LikeOriginal()) return true;
            if (lx < 1 || ly < 1) return false;
            if (x <= 0 || y <= 0 || x + lx - 1 >= DynamicFieldSxV425 || y + ly - 1 >= DynamicFieldSyV425)
                return true;
            for (int ix = x; ix < x + lx; ix++)
                for (int iy = y; iy < y + ly; iy++)
                    if (UnitsFieldV425[ix * DynamicFieldSyV425 + iy] > 0) return true;
            return false;
        }

        // Kept as a compatibility name for callers that previously asked for a round.
        // Native UnitsField itself has no round occupancy; this now checks the exact dxd bar.
        internal static bool UnitsFieldCheckRoundV425LikeOriginal(int x, int y, int d)
        {
            return UnitsFieldCheckBarV427LikeOriginal(x, y, d, d);
        }

        internal static void MoveUnitsFieldAfterSingleStepV427LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            float oldRealX, float oldRealY, float newRealX, float newRealY)
        {
            if (unit == null || !EnsureDynamicFieldsV425LikeOriginal()) return;
            C2UnitOriginalRuntimeLinkLikeOriginal link = unit.RuntimeLinkCachedLikeOriginal;
            // SINGLESTEP writes UnitsField even during UnlimitedMotion (NewMon.cpp).
            // Unlimited bypasses terrain barriers; it does not remove a live mover
            // from the occupancy bitmap. Hidden mine workers are excluded separately.
            if (link != null && link.Runtime != null && link.Runtime.HiddenInsideBuildingLikeOriginal) return;
            int lx = ResolveLxLikeOriginal(unit);
            int ox = RealCenterToObjectCellV425(oldRealX, lx);
            int oy = RealCenterToObjectCellV425(oldRealY, lx);
            int nx = RealCenterToObjectCellV425(newRealX, lx);
            int ny = RealCenterToObjectCellV425(newRealY, lx);
            // NewMon.cpp clears and sets even when the cell is unchanged.
            AddUnitsFieldBarV427LikeOriginal(ox, oy, lx, -1);
            AddUnitsFieldBarV427LikeOriginal(nx, ny, lx, +1);
            if (link != null && link.Runtime != null && link.Runtime.UnitsFieldRegisteredV433)
            {
                link.Runtime.UnitsFieldCellXV433 = nx;
                link.Runtime.UnitsFieldCellYV433 = ny;
            }
        }

        internal static void RebuildDynamicUnitFieldsV425LikeOriginal()
        {
            if (!EnsureDynamicFieldsV425LikeOriginal()) return;
            // MFIELDS overlay is synchronized from GLock owners. Clear only the
            // cells written last quantum; never stream four entire map arrays.
            for (int f = 0; f < 4; f++)
            {
                foreach (int cell in DynamicDirtyV433[f]) DynamicGlockV425[f][cell] = 0;
                DynamicDirtyV433[f].Clear();
            }
            // UnitsField is a persistent native bit map. Rebuilding it each tick
            // resurrected bits cleared by soldiers crossing one another.
            UnitsFieldGenerationV433++;
            C2NeutralPeasantUnitInfoV2LikeOriginal[] all = C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
            for (int i = 0; all != null && i < all.Length; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal unit = all[i];
                if (unit == null || unit.IsDeadLikeOriginal || !unit.isActiveAndEnabled) continue;
                C2UnitOriginalRuntimeLinkLikeOriginal link = unit.RuntimeLinkCachedLikeOriginal;
                C2UnitOriginalRuntime rt = link != null ? link.Runtime : null;
                if (rt == null) continue;
                int lx = ResolveLxLikeOriginal(unit);
                int x = RealCenterToObjectCellV425(rt.RuntimeRealXLikeOriginal, lx);
                int y = RealCenterToObjectCellV425(rt.RuntimeRealYLikeOriginal, lx);

                // Mechanics.cpp complex objects own a separate BXorBar footprint on
                // MFIELDS 0/2/3 and are not inserted into UnitsField by their motion
                // handler. Do not rebuild them as an ordinary GLock unit.
                bool complexObjectV430 = rt.Md != null &&
                    string.Equals(rt.Md.MotionStyle, "COMPLEXOBJECT", StringComparison.OrdinalIgnoreCase);
                if (complexObjectV430)
                {
                    rt.OriginalGLockV425LikeOriginal = false;
                    rt.OriginalLockStateInitializedV425LikeOriginal = true;
                    ReapplyComplexObjectLockV430LikeOriginal(rt);
                    continue;
                }

                if (!rt.OriginalLockStateInitializedV425LikeOriginal)
                {
                    // Motion.cpp::MotionHandlerOfNewSheeps owns GLock. The
                    // SINGLESTEP/FLY handlers in NewMon.cpp never set it on idle.
                    // Giving infantry a sheep lock makes SmartSend move formation
                    // destinations away from other soldiers on open ground.
                    bool sheep = rt.Md != null &&
                        (string.Equals(rt.Md.MotionStyle, "SHEEPS", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(rt.Md.MotionStyle, "NEWSHEEPS", StringComparison.OrdinalIgnoreCase));
                    rt.OriginalGLockV425LikeOriginal = sheep && !rt.HasMoveTargetLikeOriginal && !rt.OriginalPathRequestPendingV425LikeOriginal;
                    rt.OriginalLockStateInitializedV425LikeOriginal = true;
                }
                if (rt.OriginalGLockV425LikeOriginal)
                    AddGlockRoundV425LikeOriginal(x, y, lx, ResolveLockTypeV425LikeOriginal(unit), +1);
                if (!rt.HiddenInsideBuildingLikeOriginal)
                {
                    if (!rt.UnitsFieldRegisteredV433)
                    {
                        rt.UnitsFieldRegisteredV433 = true;
                        UnitsFieldRegistrationsV433.Add(rt);
                        AddUnitsFieldBarV427LikeOriginal(x, y, lx, +1);
                    }
                    else if (rt.UnitsFieldCellXV433 != x || rt.UnitsFieldCellYV433 != y || rt.UnitsFieldLxV433 != lx)
                    {
                        // Editor placement/transport changes that bypass a motion step.
                        AddUnitsFieldBarV427LikeOriginal(rt.UnitsFieldCellXV433, rt.UnitsFieldCellYV433, rt.UnitsFieldLxV433, -1);
                        AddUnitsFieldBarV427LikeOriginal(x, y, lx, +1);
                    }
                    rt.UnitsFieldCellXV433 = x;
                    rt.UnitsFieldCellYV433 = y;
                    rt.UnitsFieldLxV433 = lx;
                    rt.UnitsFieldGenerationV433 = UnitsFieldGenerationV433;
                }
            }
            for (int i = UnitsFieldRegistrationsV433.Count - 1; i >= 0; i--)
            {
                var rt = UnitsFieldRegistrationsV433[i];
                if (rt.UnitsFieldGenerationV433 == UnitsFieldGenerationV433) continue;
                AddUnitsFieldBarV427LikeOriginal(rt.UnitsFieldCellXV433, rt.UnitsFieldCellYV433, rt.UnitsFieldLxV433, -1);
                rt.UnitsFieldRegisteredV433 = false;
                UnitsFieldRegistrationsV433.RemoveAt(i);
            }
        }

        internal static void MarkUnitStandingV425LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit, C2UnitOriginalRuntime rt)
        {
            if (unit == null || rt == null || !EnsureDynamicFieldsV425LikeOriginal()) return;
            int lx = ResolveLxLikeOriginal(unit);
            int x = RealCenterToObjectCellV425(rt.RuntimeRealXLikeOriginal, lx);
            int y = RealCenterToObjectCellV425(rt.RuntimeRealYLikeOriginal, lx);
            if (!rt.OriginalGLockV425LikeOriginal)
            {
                rt.OriginalGLockV425LikeOriginal = true;
                AddGlockRoundV425LikeOriginal(x, y, lx, ResolveLockTypeV425LikeOriginal(unit), +1);
            }
            rt.OriginalStandTimeV425LikeOriginal++;
        }

        private static void BeginUnitMotionOccupancyV425LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit, C2UnitOriginalRuntime rt, int lx, byte lockType)
        {
            if (unit == null || rt == null || !EnsureDynamicFieldsV425LikeOriginal()) return;
            int x = RealCenterToObjectCellV425(rt.RuntimeRealXLikeOriginal, lx);
            int y = RealCenterToObjectCellV425(rt.RuntimeRealYLikeOriginal, lx);
            if (rt.OriginalGLockV425LikeOriginal)
            {
                AddGlockRoundV425LikeOriginal(x, y, lx, lockType, -1);
                rt.OriginalGLockV425LikeOriginal = false;
            }
            C2UnitOriginalRuntimeLinkLikeOriginal link = unit.RuntimeLinkCachedLikeOriginal;
            bool unlimited = link != null && link.IsUnlimitedMotionV415LikeOriginal;
            if (!unlimited)
                AddUnitsFieldBarV427LikeOriginal(x, y, lx, -1);
        }

        private static void FinishUnitMotionOccupancyV425LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit, C2UnitOriginalRuntime rt,
            int lx, byte lockType, bool moved, int dx, int dy)
        {
            if (unit == null || rt == null || !EnsureDynamicFieldsV425LikeOriginal()) return;
            int realX = Mathf.RoundToInt(rt.RuntimeRealXLikeOriginal) + (moved ? dx : 0);
            int realY = Mathf.RoundToInt(rt.RuntimeRealYLikeOriginal) + (moved ? dy : 0);
            int x = RealCenterToObjectCellV425(realX, lx);
            int y = RealCenterToObjectCellV425(realY, lx);
            if (moved)
            {
                rt.OriginalGLockV425LikeOriginal = false;
                rt.OriginalStandTimeV425LikeOriginal = 0;
            }
            else
            {
                rt.OriginalGLockV425LikeOriginal = true;
                AddGlockRoundV425LikeOriginal(x, y, lx, lockType, +1);
            }
            C2UnitOriginalRuntimeLinkLikeOriginal link = unit.RuntimeLinkCachedLikeOriginal;
            bool unlimited = link != null && link.IsUnlimitedMotionV415LikeOriginal;
            if (!unlimited)
                AddUnitsFieldBarV427LikeOriginal(x, y, lx, +1);
            if (rt.UnitsFieldRegisteredV433)
            {
                rt.UnitsFieldCellXV433 = x;
                rt.UnitsFieldCellYV433 = y;
            }
        }

        internal static int ResolveLxLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            var cob = unit?.RuntimeLinkCachedLikeOriginal?.Runtime?.OriginalComplexObjectV430LikeOriginal;
            if (cob != null && cob.LxOverrideV441 > 0) return cob.LxOverrideV441;
            ResolveUnitGeometryV433(unit, out int lx, out byte lockType);
            return lx;
        }

        internal static byte ResolveLockTypeV425LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            ResolveUnitGeometryV433(unit, out int lx, out byte lockType);
            return lockType;
        }

        private static void ResolveUnitGeometryV433(C2NeutralPeasantUnitInfoV2LikeOriginal unit, out int lx, out byte lockType)
        {
            lx = 1; lockType = 0;
            if (unit == null) return;
            var rt = unit.RuntimeLinkCachedLikeOriginal?.Runtime;
            var md = rt?.Md;
            if (rt != null && rt.GeometryCachedV433 && ReferenceEquals(rt.GeometryMdV433, md) &&
                rt.GeometryNationV433 == unit.Nation && rt.GeometrySourceV433 == unit.SourceMonsterId &&
                rt.GeometryResolvedMdV433 == unit.ResolvedMd && rt.GeometryRadiusV433 == unit.GeometryRadius2Real)
            {
                lx = rt.GeometryLxV433; lockType = rt.GeometryLockTypeV433; return;
            }
            var traits = C2CombatCoreV408LikeOriginal.GetTraitsV408LikeOriginal(unit);
            lockType = traits != null ? traits.LockType : (byte)0;
            int radius = md != null ? md.GeometryRadius2 : 0;
            if (radius <= 0 && unit.GeometryRadius2Real > 0) radius = unit.GeometryRadius2Real >> 4;
            lx = Math.Max(1, ((Math.Max(0, radius) << 4) + 3) >> 7);
            bool complex = md != null && string.Equals(md.MotionStyle, "COMPLEXOBJECT", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(md.ComplexObjectIdLikeOriginal);
            if (lockType == 0 && !complex) lx = 1;
            lx = Mathf.Clamp(lx, 1, 22);
            if (rt == null) return;
            rt.GeometryCachedV433 = true; rt.GeometryMdV433 = md;
            rt.GeometryNationV433 = unit.Nation; rt.GeometrySourceV433 = unit.SourceMonsterId;
            rt.GeometryResolvedMdV433 = unit.ResolvedMd; rt.GeometryRadiusV433 = unit.GeometryRadius2Real;
            rt.GeometryLxV433 = lx; rt.GeometryLockTypeV433 = lockType;
        }

        private static int RealCenterToObjectCellV425(float real, int lx)
        {
            return (Mathf.RoundToInt(real) - (lx << 7)) >> 8;
        }

        private static float ObjectCellToRealCenterV425(int cell, int lx)
        {
            return (cell << 8) + (lx << 7);
        }

        private static C2BattleTerrainMode MotionFieldModeV425LikeOriginal;

        private static C2BattleTerrainMode ResolveMotionFieldModeV425LikeOriginal()
        {
            if (FieldsPreparedV433) return DynamicOwnerV433;
            if (MotionFieldModeV425LikeOriginal == null)
                MotionFieldModeV425LikeOriginal = UnityEngine.Object.FindFirstObjectByType<C2BattleTerrainMode>();
            return MotionFieldModeV425LikeOriginal;
        }

        // path.cpp MotionField::CheckPt semantics: true means blocked. All consumers
        // (FPathFinder, topology, unit orders and brigade orders) now resolve through
        // the same four native logical MFIELDS instead of separate Unity masks.
        internal static bool CheckPtV425LikeOriginal(int x, int y, byte lockType = 0)
        {
            C2BattleTerrainMode mode = ResolveMotionFieldModeV425LikeOriginal();
            if (mode == null) return true;
            return mode.C2MovementNativeFieldCheckPtV425LikeOriginal(x, y, lockType);
        }

        internal static bool CheckVLineV425LikeOriginal(int x, int y, int length, byte lockType = 0)
        {
            // path.cpp::MotionField::CheckVLine has an explicit domain guard; note
            // the strict x>0/y>0 and native maximum vertical span of 24 cells.
            C2BattleTerrainMode mode = ResolveMotionFieldModeV425LikeOriginal();
            int addsh, mapSx, mapSy; string sourcePath;
            if (FieldsPreparedV433) { mapSx = DynamicFieldSxV425; mapSy = DynamicFieldSyV425; }
            else if (mode == null || !mode.C2TopologyTryGetMapGeometryV401LikeOriginal(out addsh, out mapSx, out mapSy, out sourcePath))
                return true;
            if (!(x > 0 && y > 0 && y + length - 1 < mapSy && x < mapSx && length <= 24))
                return true;
            for (int i = 0; i < length; i++)
                if (CheckPtV425LikeOriginal(x, y + i, lockType)) return true;
            return false;
        }

        internal static bool CheckBarV425LikeOriginal(int x, int y, int lx, int ly, byte lockType = 0)
        {
            for (int ix = 0; ix < lx; ix++) if (CheckVLineV425LikeOriginal(x + ix, y, ly, lockType)) return true;
            return false;
        }

        internal static bool CheckRoundV425LikeOriginal(int x, int y, int d, byte lockType = 0)
        {
            if (d < 1) return false;
            int[] widths = GetSqWidthsV425(d);
            for (int i = 0; i < d; i++)
            {
                int l = widths[i];
                if (CheckVLineV425LikeOriginal(x + i, y + (d - l) / 2, l, lockType)) return true;
            }
            return false;
        }

        private static bool FPathCellFreeV425(int x, int y)
        {
            int lx = FPathLxV425;
            byte lockType = FPathLockTypeV425;
            return lx == 1
                ? !CheckPtV425LikeOriginal(x, y, lockType)
                : !CheckRoundV425LikeOriginal(x - 2, y - 2, lx + 4, lockType);
        }

        private static int FPathLxV425 = 1;
        private static byte FPathLockTypeV425;
        private static readonly C2FPathFinderV421LikeOriginal FPathV425 = new C2FPathFinderV421LikeOriginal(FPathCellFreeV425);
        private static readonly List<Vector2> PathScratchV425 = new List<Vector2>(256);

        internal static bool FastFindBestPositionOldV425LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit, ref int xd, ref int yd, int r0, byte lockType = 0)
        {
            if (unit == null) return false;
            if (!CheckPtV425LikeOriginal(xd, yd, lockType)) return true;
            if (r0 > 20) r0 = 20;
            int lx = ResolveLxLikeOriginal(unit);
            float ux = unit.RealXFloat != 0.0f ? unit.RealXFloat : unit.RealX;
            float uy = unit.RealYFloat != 0.0f ? unit.RealYFloat : unit.RealY;
            int x = RealCenterToObjectCellV425(ux, lx);
            int y = RealCenterToObjectCellV425(uy, lx);
            int x0 = xd, y0 = yd;
            for (int i = 1; i < r0; i += 3)
            {
                int bd = 100000, xb = 0, yb = 0;
                List<RadioPointV425> ring = RarrV425[i];
                for (int j = 0; j < ring.Count; j++)
                {
                    int xx = x0 + ring[j].X;
                    int yy = y0 + ring[j].Y;
                    if (!CheckPtV425LikeOriginal(xx, yy, lockType))
                    {
                        int d = C2OriginalMovementMathV352.Norma(xx - x, yy - y);
                        if (d < bd) { bd = d; xb = xx; yb = yy; }
                    }
                }
                if (bd < 100000) { xd = xb; yd = yb; return true; }
            }
            return false;
        }

        internal static bool FindBestPositionOldV425LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit, ref int xd, ref int yd, int r0, byte lockType = 0)
        {
            if (unit == null) return false;
            int objLx = ResolveLxLikeOriginal(unit);
            bool peasant = unit.IsPeasantLikeOriginal();
            if (objLx == 1 && !peasant)
                return FastFindBestPositionOldV425LikeOriginal(unit, ref xd, ref yd, r0, lockType);

            int otstup = peasant ? 1 : 0;
            if (objLx > 1) otstup = 3;
            int footprint = objLx + otstup * 2;
            if (!CheckRoundV425LikeOriginal(xd - otstup, yd - otstup, footprint, lockType)) return true;

            float ux = unit.RealXFloat != 0.0f ? unit.RealXFloat : unit.RealX;
            float uy = unit.RealYFloat != 0.0f ? unit.RealYFloat : unit.RealY;
            int x = RealCenterToObjectCellV425(ux, objLx);
            int y = RealCenterToObjectCellV425(uy, objLx);
            int bx = xd, by = yd, best = 100000;
            int xxx = bx - otstup, yyy = by - otstup, ll = 2, r1 = r0;
            while (r1 != 0)
            {
                for (int i = 0; i <= ll; i++) TestFindOldCandidateV425(xxx + i, yyy, x, y, otstup, footprint, lockType, ref bx, ref by, ref best);
                for (int i = 0; i <= ll; i++) TestFindOldCandidateV425(xxx + i, yyy + ll, x, y, otstup, footprint, lockType, ref bx, ref by, ref best);
                for (int i = 0; i < ll - 1; i++) TestFindOldCandidateV425(xxx, yyy + i, x, y, otstup, footprint, lockType, ref bx, ref by, ref best);
                for (int i = 0; i < ll - 1; i++) TestFindOldCandidateV425(xxx + ll, yyy + i, x, y, otstup, footprint, lockType, ref bx, ref by, ref best);
                if (best < 100000) { xd = bx; yd = by; return true; }
                r1--; ll += 2; xxx--; yyy--;
            }
            return false;
        }

        private static void TestFindOldCandidateV425(int cx, int cy, int ux, int uy, int otstup, int footprint, byte lockType,
            ref int bx, ref int by, ref int best)
        {
            if (CheckRoundV425LikeOriginal(cx - otstup, cy - otstup, footprint, lockType)) return;
            int d = C2OriginalMovementMathV352.Norma(cx - ux, cy - uy);
            if (d < best) { bx = cx; by = cy; best = d; }
        }

        // NewMon.cpp::NewMonsterSendToLink corrects a newly obstructed
        // destination periodically. Correcting only FPath's temporary copy leaves
        // LocalOrder waiting forever for the original blocked destination cell.
        internal static void CorrectOrdinaryDestinationV433LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,ref float realX,ref float realY)
        {
            var rt=unit.RuntimeLinkCachedLikeOriginal?.Runtime;
            if(rt==null)return;
            int lx=ResolveLxLikeOriginal(unit);
            byte lt=ResolveLockTypeV425LikeOriginal(unit);
            int ux=RealCenterToObjectCellV425(rt.RuntimeRealXLikeOriginal,lx);
            int uy=RealCenterToObjectCellV425(rt.RuntimeRealYLikeOriginal,lx);
            int x=RealCenterToObjectCellV425(realX,lx),y=RealCenterToObjectCellV425(realY,lx);
            if(rt.OriginalGLockV425LikeOriginal)AddGlockRoundV425LikeOriginal(ux,uy,lx,lt,-1);
            try
            {
                if(!rt.PreciseBornPathLikeOriginal && CheckRoundV425LikeOriginal(x-1,y-1,lx+2,lt) &&
                    FindBestPositionOldV425LikeOriginal(unit,ref x,ref y,30,lt))
                {realX=ObjectCellToRealCenterV425(x,lx);realY=ObjectCellToRealCenterV425(y,lx);}
            }
            finally {if(rt.OriginalGLockV425LikeOriginal)AddGlockRoundV425LikeOriginal(ux,uy,lx,lt,1);}
        }

        internal static bool FindBestPositionV425LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit, ref int xd, ref int yd, int r0, byte lockType = 0)
        {
            if (unit == null) return false;
            if (xd < -40 || yd < -30 || xd > 1000 || yd > 1000) return false;
            int objLx = ResolveLxLikeOriginal(unit);
            float ux = unit.RealXFloat != 0.0f ? unit.RealXFloat : unit.RealX;
            float uy = unit.RealYFloat != 0.0f ? unit.RealYFloat : unit.RealY;
            int x = RealCenterToObjectCellV425(ux, objLx);
            int y = RealCenterToObjectCellV425(uy, objLx);
            if (C2OriginalMovementMathV352.Norma(x - xd, y - yd) < 30)
                return FindBestPositionOldV425LikeOriginal(unit, ref xd, ref yd, r0, lockType);

            bool peasant = unit.IsPeasantLikeOriginal();
            int otstup = peasant ? 1 : 0;
            if (objLx > 1) otstup = 3;
            int footprint = objLx + otstup * 2;
            if (!CheckRoundV425LikeOriginal(xd - otstup, yd - otstup, footprint, lockType)) return true;

            int bx = xd, by = yd;
            int n = C2OriginalMovementMathV352.Norma(bx - x, by - y) + 1;
            for (int i = 0; i < n; i += 2)
            {
                int xxx = bx + (x - bx) * i / n;
                int yyy = by + (y - by) * i / n;
                if (!CheckRoundV425LikeOriginal(xxx - otstup, yyy - otstup, footprint, lockType))
                { xd = xxx; yd = yyy; return true; }
            }
            return FindBestPositionOldV425LikeOriginal(unit, ref xd, ref yd, r0, lockType);
        }

        internal static bool TryBuildFullPathV425LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            float wantedRealX, float wantedRealY,
            out Vector2[] path,
            byte lockType = 0)
        {
            path = null;
            if (unit == null) return false;
            C2UnitOriginalRuntimeLinkLikeOriginal link = unit.RuntimeLinkCachedLikeOriginal;
            C2UnitOriginalRuntime rt = link != null ? link.Runtime : null;
            int lx = ResolveLxLikeOriginal(unit);
            int x1 = RealCenterToObjectCellV425(wantedRealX, lx);
            int y1 = RealCenterToObjectCellV425(wantedRealY, lx);
            if (x1 < 4 || y1 < 4) return false;

            // path.cpp::CreateFullPath performs this early return before touching GLock.
            if (rt != null && rt.OriginalPathDelayV425LikeOriginal > 0)
            {
                rt.OriginalPathDelayV425LikeOriginal--;
                return false;
            }

            int tx = x1, ty = y1;
            if (!FindBestPositionV425LikeOriginal(unit, ref tx, ref ty, 40, lockType)) return false;

            float ux = unit.RealXFloat != 0.0f ? unit.RealXFloat : unit.RealX;
            float uy = unit.RealYFloat != 0.0f ? unit.RealYFloat : unit.RealY;
            int sx = RealCenterToObjectCellV425(ux, lx);
            int sy = RealCenterToObjectCellV425(uy, lx);

            // Native CreateFullPath clears this object's own GLock only after the
            // destination FindBestPosition and restores it on every exit.
            bool ownGlockCleared = rt != null && rt.OriginalGLockV425LikeOriginal;
            if (ownGlockCleared)
                AddGlockRoundV425LikeOriginal(sx, sy, lx, lockType, -1);
            try
            {
                int startX = sx, startY = sy;
                if (!FindBestPositionV425LikeOriginal(unit, ref startX, ref startY, 40, lockType))
                    return false;

                FPathLxV425 = lx;
                FPathLockTypeV425 = lockType;
                if (FPathV425.GetPath(startX, startY, tx, ty) == null)
                {
                    if (rt != null) rt.OriginalPathDelayV425LikeOriginal = 40;
                    return false;
                }
                FPathV425.Bending(2 + (Mathf.Max(0, unit.C2ObjectIndexV408LikeOriginal) % 3));
                PathScratchV425.Clear();
                for (C2FPathFinderV421LikeOriginal.Point p = FPathV425.WayPoints.First; p != null; p = p.Next)
                    PathScratchV425.Add(new Vector2(ObjectCellToRealCenterV425(p.x, lx), ObjectCellToRealCenterV425(p.y, lx)));
                if (PathScratchV425.Count == 0 ||
                    Mathf.Abs(PathScratchV425[PathScratchV425.Count - 1].x - ObjectCellToRealCenterV425(tx, lx)) > 0.5f ||
                    Mathf.Abs(PathScratchV425[PathScratchV425.Count - 1].y - ObjectCellToRealCenterV425(ty, lx)) > 0.5f)
                    PathScratchV425.Add(new Vector2(ObjectCellToRealCenterV425(tx, lx), ObjectCellToRealCenterV425(ty, lx)));
                path = PathScratchV425.ToArray();
                return path.Length != 0;
            }
            finally
            {
                if (ownGlockCleared)
                    AddGlockRoundV425LikeOriginal(sx, sy, lx, lockType, +1);
            }
        }

        // Motion.cpp::MotionHandlerOfNewSheeps/TryToMoveUnitTo. The renderer
        // supplies the already calculated RealVx/RealVy step; all collision/turn
        // decisions live here so there is no Unity-side second movement algorithm.
        internal static bool TryNewSheepMotionStepV425LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            C2UnitOriginalRuntime runtime,
            int dx0,
            int dy0,
            out int committedDx,
            out int committedDy)
        {
            committedDx = 0;
            committedDy = 0;
            if (unit == null || runtime == null) return false;
            byte lockType = ResolveLockTypeV425LikeOriginal(unit);
            int lx = ResolveLxLikeOriginal(unit);
            C2UnitOriginalRuntimeLinkLikeOriginal runtimeLink = unit.RuntimeLinkCachedLikeOriginal;
            bool unlimited = runtimeLink != null && runtimeLink.IsUnlimitedMotionV415LikeOriginal;
            BeginUnitMotionOccupancyV425LikeOriginal(unit, runtime, lx, lockType);

            int dx = dx0, dy = dy0;
            if (TryToMoveUnitV425LikeOriginal(unit, runtime, dx, dy, 0, lx, lockType, unlimited))
            { committedDx = dx; committedDy = dy; FinishUnitMotionOccupancyV425LikeOriginal(unit, runtime, lx, lockType, true, committedDx, committedDy); return true; }

            RotateVecV425LikeOriginal(dx0, dy0, 8, out dx, out dy);
            if (TryToMoveUnitV425LikeOriginal(unit, runtime, dx, dy, 17 * 16, lx, lockType, unlimited))
            { committedDx = dx; committedDy = dy; FinishUnitMotionOccupancyV425LikeOriginal(unit, runtime, lx, lockType, true, committedDx, committedDy); return true; }

            RotateVecV425LikeOriginal(dx0, dy0, -8, out dx, out dy);
            if (TryToMoveUnitV425LikeOriginal(unit, runtime, dx, dy, 17 * 16, lx, lockType, unlimited))
            { committedDx = dx; committedDy = dy; FinishUnitMotionOccupancyV425LikeOriginal(unit, runtime, lx, lockType, true, committedDx, committedDy); return true; }

            // Motion.cpp deletes the current path with 1024/32768 probability here.
            if (C2RetailRandomV407LikeOriginal.Rando(unit) < 1024)
            {
                runtime.MovePathRealWaypointsLikeOriginal = null;
                runtime.MovePathIndexLikeOriginal = 0;
                runtime.HasMoveTargetLikeOriginal = false;
                runtime.OriginalCPdestXV425LikeOriginal = -1;
                runtime.OriginalCPdestYV425LikeOriginal = -1;
            }

            RotateVecV425LikeOriginal(dx0, dy0, 16, out dx, out dy);
            if (TryToMoveUnitV425LikeOriginal(unit, runtime, dx, dy, 17 * 16, lx, lockType, unlimited))
            { committedDx = dx; committedDy = dy; FinishUnitMotionOccupancyV425LikeOriginal(unit, runtime, lx, lockType, true, committedDx, committedDy); return true; }

            RotateVecV425LikeOriginal(dx0, dy0, -16, out dx, out dy);
            if (TryToMoveUnitV425LikeOriginal(unit, runtime, dx, dy, 17 * 16, lx, lockType, unlimited))
            { committedDx = dx; committedDy = dy; FinishUnitMotionOccupancyV425LikeOriginal(unit, runtime, lx, lockType, true, committedDx, committedDy); return true; }

            RotateVecV425LikeOriginal(dx0, dy0, 32, out dx, out dy);
            if (TryToMoveUnitV425LikeOriginal(unit, runtime, dx / 2, dy / 2, 17 * 16, lx, lockType, unlimited))
            { committedDx = dx / 2; committedDy = dy / 2; FinishUnitMotionOccupancyV425LikeOriginal(unit, runtime, lx, lockType, true, committedDx, committedDy); return true; }

            RotateVecV425LikeOriginal(dx0, dy0, -32, out dx, out dy);
            if (TryToMoveUnitV425LikeOriginal(unit, runtime, dx / 2, dy / 2, 17 * 16, lx, lockType, unlimited))
            { committedDx = dx / 2; committedDy = dy / 2; FinishUnitMotionOccupancyV425LikeOriginal(unit, runtime, lx, lockType, true, committedDx, committedDy); return true; }

            RotateVecV425LikeOriginal(dx0, dy0, 64, out dx, out dy);
            if (TryToMoveUnitV425LikeOriginal(unit, runtime, dx / 4, dy / 4, 17 * 16, lx, lockType, unlimited))
            { committedDx = dx / 4; committedDy = dy / 4; FinishUnitMotionOccupancyV425LikeOriginal(unit, runtime, lx, lockType, true, committedDx, committedDy); return true; }

            RotateVecV425LikeOriginal(dx0, dy0, -64, out dx, out dy);
            if (TryToMoveUnitV425LikeOriginal(unit, runtime, dx / 4, dy / 4, 17 * 16, lx, lockType, unlimited))
            { committedDx = dx / 4; committedDy = dy / 4; FinishUnitMotionOccupancyV425LikeOriginal(unit, runtime, lx, lockType, true, committedDx, committedDy); return true; }
            FinishUnitMotionOccupancyV425LikeOriginal(unit, runtime, lx, lockType, false, 0, 0);
            return false;
        }

        private static void RotateVecV425LikeOriginal(int x0, int y0, int angle, out int x, out int y)
        {
            int a = angle & 255;
            int cos = C2OriginalMovementMathV352.TCos[a];
            int sin = C2OriginalMovementMathV352.TSin[a];
            x = (x0 * cos - y0 * sin) / 256;
            y = (x0 * sin + y0 * cos) / 256;
        }

        private static bool TryToMoveUnitV425LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            C2UnitOriginalRuntime runtime,
            int dx,
            int dy,
            int norm,
            int lx,
            byte lockType,
            bool unlimited)
        {
            int cdx = dx, cdy = dy;
            if (norm != 0)
            {
                int n = C2OriginalMovementMathV352.Norma(dx, dy);
                if (n != 0)
                {
                    // Motion.cpp uses integer division in this exact order.
                    cdx = dx * norm / 16;
                    cdy = dy * norm / 16;
                }
            }
            int realX = Mathf.RoundToInt(runtime.RuntimeRealXLikeOriginal);
            int realY = Mathf.RoundToInt(runtime.RuntimeRealYLikeOriginal);
            int ulx = lx << 7;
            int lockX = (realX + cdx - ulx) >> 8;
            int lockY = (realY + cdy - ulx) >> 8;
            bool passable = !CheckRoundV425LikeOriginal(lockX, lockY, lx, lockType);
            if (passable && lockType == 1)
                passable = TestWaterUnitCollisionV425LikeOriginal(unit, realX + cdx, realY + cdy, lx);
            return passable || unlimited;
        }

        private static bool TestWaterUnitCollisionV425LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit, int candidateRealX, int candidateRealY, int lx)
        {
            C2NeutralPeasantUnitInfoV2LikeOriginal[] all =
                C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
            int x = candidateRealX / 16, y = candidateRealY / 16;
            int r = lx * 8 + 12; // Motion.cpp ExtraR=12.
            for (int i = 0; all != null && i < all.Length; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal other = all[i];
                if (other == null || other == unit || other.IsDeadLikeOriginal || !other.isActiveAndEnabled) continue;
                if (ResolveLockTypeV425LikeOriginal(other) != 1) continue;
                int ox = Mathf.RoundToInt(other.RealXFloat != 0.0f ? other.RealXFloat : other.RealX) / 16;
                int oy = Mathf.RoundToInt(other.RealYFloat != 0.0f ? other.RealYFloat : other.RealY) / 16;
                if (C2OriginalMovementMathV352.Norma(ox - x, oy - y) > 360) continue;
                int r1 = ResolveLxLikeOriginal(other) * 8 + 12;
                long ddx = ox - x, ddy = oy - y;
                if ((long)(r + r1) * (r + r1) > ddx * ddx + ddy * ddy) return false;
            }
            return true;
        }

        internal static void AddPathRequestorV425LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            float wantedRealX, float wantedRealY,
            float speed, bool hasFinalFacing, byte finalFacing, string source)
        {
            if (unit == null) return;
            C2UnitOriginalRuntimeLinkLikeOriginal link = unit.RuntimeLinkCachedLikeOriginal;
            C2UnitOriginalRuntime rt = link != null ? link.Runtime : null;
            if (rt == null) return;
            int lx = ResolveLxLikeOriginal(unit);
            // path.cpp::AddPathRequestor appends. Only explicit cancellation
            // calls DisablePathRequest; scanning the queue on every append both
            // changes its order semantics and makes a mass command quadratic.
            PathReqV425.Add(new PathRequestV425 {
                Enabled = true, Unit = unit, Serial = unit.C2ObjectSerialLikeOriginal,
                ToCellX = RealCenterToObjectCellV425(wantedRealX, lx),
                ToCellY = RealCenterToObjectCellV425(wantedRealY, lx),
                Speed = speed, HasFinalFacing = hasFinalFacing, FinalFacing = finalFacing,
                Source = source ?? "CreatePath"
            });
            rt.OriginalPathRequestPendingV425LikeOriginal = true;
            // CPdest belongs to ReallyCreatePath, not CreatePath/AddPathRequestor.
            rt.OriginalQueuedDestRealXV425LikeOriginal = wantedRealX;
            rt.OriginalQueuedDestRealYV425LikeOriginal = wantedRealY;
        }

        internal static void DisablePathRequestV425LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null) return;
            for (int i = 0; i < PathReqV425.Count; i++)
            {
                var request = PathReqV425[i];
                if (request.Unit != unit || request.Serial != unit.C2ObjectSerialLikeOriginal) continue;
                request.Enabled = false;
                PathReqV425[i] = request;
            }
            C2UnitOriginalRuntimeLinkLikeOriginal link = unit.RuntimeLinkCachedLikeOriginal;
            if (link != null && link.Runtime != null) link.Runtime.OriginalPathRequestPendingV425LikeOriginal = false;
        }

        private static void DeleteNativePathV425LikeOriginal(C2UnitOriginalRuntime rt)
        {
            if (rt == null) return;
            rt.MovePathRealWaypointsLikeOriginal = null;
            rt.MovePathIndexLikeOriginal = 0;
            rt.HasMoveTargetLikeOriginal = false;
        }

        private static int DistanceUnitToPathCellV425LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit, int cellX, int cellY, int lx)
        {
            if (unit == null) return int.MaxValue;
            float ux = unit.RealXFloat != 0.0f ? unit.RealXFloat : unit.RealX;
            float uy = unit.RealYFloat != 0.0f ? unit.RealYFloat : unit.RealY;
            int x = RealCenterToObjectCellV425(ux, lx);
            int y = RealCenterToObjectCellV425(uy, lx);
            // OneObject.h::DistTo is max(abs(dx),abs(dy)), not Norma.
            return Math.Max(Math.Abs(cellX-x),Math.Abs(cellY-y));
        }

        private static bool ReallyCreatePathV425LikeOriginal(PathRequestV425 pr)
        {
            if (pr.Unit == null) return false;
            C2UnitOriginalRuntimeLinkLikeOriginal link = pr.Unit.RuntimeLinkCachedLikeOriginal;
            C2UnitOriginalRuntime rt = link != null ? link.Runtime : null;
            if (rt == null) return false;

            int lx = ResolveLxLikeOriginal(pr.Unit);
            int x1 = pr.ToCellX;
            int y1 = pr.ToCellY;
            byte lockType = ResolveLockTypeV425LikeOriginal(pr.Unit);
            int d = lockType == 1 ? 3 : 1;

            // path.cpp::ReallyCreatePath: unchanged CPdest reuses PathX/PathY and
            // only rebuilds when the next point becomes blocked, the path ends, or
            // the current lock state asks for recovery.
            if (rt.OriginalCPdestXV425LikeOriginal == x1 && rt.OriginalCPdestYV425LikeOriginal == y1)
            {
                Vector2[] path = rt.MovePathRealWaypointsLikeOriginal;
                if (path != null && path.Length != 0)
                {
                    int pi = Mathf.Clamp(rt.MovePathIndexLikeOriginal, 0, path.Length);
                    // path.cpp::ReallyCreatePath consumes one NIPoints entry per
                    // call when DistTo <= 1 (land) / 3 (water), before movement.
                    if (pi < path.Length && DistanceUnitToPathCellV425LikeOriginal(pr.Unit,
                        RealCenterToObjectCellV425(path[pi].x,lx),RealCenterToObjectCellV425(path[pi].y,lx),lx) <= d)
                        pi++;
                    rt.MovePathIndexLikeOriginal = pi;

                    if (pi < path.Length)
                    {
                        int cx = RealCenterToObjectCellV425(path[pi].x, lx);
                        int cy = RealCenterToObjectCellV425(path[pi].y, lx);
                        int ux = RealCenterToObjectCellV425(rt.RuntimeRealXLikeOriginal, lx);
                        int uy = RealCenterToObjectCellV425(rt.RuntimeRealYLikeOriginal, lx);
                        bool currentLocked = CheckRoundV425LikeOriginal(ux, uy, lx, lockType);
                        bool nextBlocked = CheckRoundV425LikeOriginal(cx, cy, lx, lockType);
                        if (!nextBlocked && !(currentLocked && C2RetailRandomV407LikeOriginal.Rando(pr.Unit) < 1024))
                        {
                            link.Owner.SetRuntimeMoveTargetOnlyLikeOriginal(rt,
                                path[pi].x,path[pi].y,pr.Speed,false,true);
                            rt.OriginalCPdestXV425LikeOriginal = x1;
                            rt.OriginalCPdestYV425LikeOriginal = y1;
                            return true;
                        }
                        if (currentLocked) rt.OriginalPathDelayV425LikeOriginal = 5;
                        DeleteNativePathV425LikeOriginal(rt);
                    }
                    else
                    {
                        // Native path.cpp frees an exhausted PathX/PathY and immediately
                        // requests a fresh full path to the same CPdest.
                        DeleteNativePathV425LikeOriginal(rt);
                        rt.OriginalPathDelayV425LikeOriginal = 0;
                    }
                }
            }
            else
            {
                DeleteNativePathV425LikeOriginal(rt);
            }

            Vector2[] rebuilt;
            bool built = TryBuildFullPathV425LikeOriginal(pr.Unit,
                ObjectCellToRealCenterV425(x1, lx), ObjectCellToRealCenterV425(y1, lx),
                out rebuilt, lockType);
            rt.OriginalCPdestXV425LikeOriginal = x1;
            rt.OriginalCPdestYV425LikeOriginal = y1;

            if (!built || rebuilt == null || rebuilt.Length == 0)
                return false;

            link.SetMovePathRealLikeOriginal(rebuilt, pr.Speed, pr.HasFinalFacing, pr.FinalFacing, false,
                pr.Source + "::ReallyCreatePath");

            // CreateFullPath stores the FPath list reversed and NIPoints points at
            // PathX[NIPoints-1], which is the first point. The managed forward array
            // is equivalent with MovePathIndex=0. Drop the trivial start point the
            // same way repeated ReallyCreatePath consumes it at D<=1/3.
            if (rt.MovePathRealWaypointsLikeOriginal != null && rt.MovePathRealWaypointsLikeOriginal.Length != 0)
            {
                Vector2 first = rt.MovePathRealWaypointsLikeOriginal[0];
                int fx = RealCenterToObjectCellV425(first.x, lx);
                int fy = RealCenterToObjectCellV425(first.y, lx);
                if (DistanceUnitToPathCellV425LikeOriginal(pr.Unit, fx, fy, lx) <= d &&
                    rt.MovePathRealWaypointsLikeOriginal.Length > 1)
                {
                    rt.MovePathIndexLikeOriginal = 1;
                    Vector2 next = rt.MovePathRealWaypointsLikeOriginal[1];
                    link.Owner.SetRuntimeMoveTargetOnlyLikeOriginal(rt, next.x, next.y, pr.Speed, false, true);
                }
            }
            return true;
        }

        internal static void PerformPathFindingV425LikeOriginal()
        {
            // path.cpp::PerformPathFiding: one shared pass, then the request array is
            // unconditionally cleared. Active LocalOrders call CreatePath again next tick.
            var objects = C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
            foreach (var unit in objects)
            {
                var rt = unit?.RuntimeLinkCachedLikeOriginal?.Runtime;
                if (rt?.OriginalComplexObjectV430LikeOriginal != null && rt.OriginalStandTimeV425LikeOriginal < 8)
                    UnlockComplexObjectV430LikeOriginal(rt);
            }
            try
            {
            int requestCount = PathReqV425.Count;
            for (int i = 0; i < requestCount; i++)
            {
                PathRequestV425 pr = PathReqV425[i];
                if (!pr.Enabled || pr.Unit == null || pr.Unit.C2ObjectSerialLikeOriginal != pr.Serial)
                    continue;
                C2UnitOriginalRuntimeLinkLikeOriginal link = pr.Unit.RuntimeLinkCachedLikeOriginal;
                C2UnitOriginalRuntime rt = link != null ? link.Runtime : null;
                if (rt == null) continue;
                ReallyCreatePathV425LikeOriginal(pr);
                rt.OriginalPathRequestPendingV425LikeOriginal = false;
            }
            }
            finally
            {
                foreach (var unit in objects)
                {
                    var rt = unit?.RuntimeLinkCachedLikeOriginal?.Runtime;
                    if (rt?.OriginalComplexObjectV430LikeOriginal != null) LockComplexObjectV430LikeOriginal(rt);
                }
                PathReqV425.Clear();
            }
        }

    }

    // V430: movement-only representation of Mechanics.cpp complex-object state.
    // Rendering remains an integration concern; these fields preserve the source motion geometry,
    // state-transition timing and articulated quant positions used by MotionHandlerForComplexObjects.
    internal sealed class C2ComplexStateElementV432LikeOriginal
    {
        internal string AnimationId = string.Empty;
        internal int Dx;
        internal int Dy;
        internal int Dfi;
        internal int AddHeight;
        internal byte AnmDirV437;
        internal bool ReverseClockV437;
    }

    internal sealed class C2ComplexTransformElementV432LikeOriginal
    {
        internal int StartTime;
        internal int TimeAmount;
        internal string AnimationIdV437;
        internal int StartFrameV437, EndFrameV437, Fi0V437, Fi1V437;
        internal int X0;
        internal int Y0;
        internal int X1;
        internal int Y1;
    }

    internal sealed class C2ComplexTransitionV430LikeOriginal
    {
        internal bool Exists;
        internal bool Direct;
        internal int MaxTransfTime;
        // Mechanics.cpp::QuantPartTransform::QPTE/NInQPTE.  V431 only kept
        // MaxTransfTime, which was enough for articulation timing but not for
        // source-exact helper coordinates during state transitions.
        internal C2ComplexTransformElementV432LikeOriginal[][] PartsV432LikeOriginal;
    }

    internal sealed class C2ComplexHelperDescV431LikeOriginal
    {
        internal string UnitId = string.Empty;
        internal int QuantPos;
        // Mechanics.cpp ReadHelpersForComplexObjects: bit0=can attack,
        // bit1=can't move if helper is killed. Default retail helper line sets bit1.
        internal int Options = 2;
    }

    internal sealed class C2ComplexQuantDescV430LikeOriginal
    {
        internal string Id = string.Empty;
        internal int X1;
        internal int X2;
        internal int AttackXV437, AttackYV437, AttackZV437;
        internal readonly List<int> DeathStagesV441 = new List<int>();
        internal readonly int[] StateParts = new int[24];
        internal readonly C2ComplexStateElementV432LikeOriginal[][] StatesV432LikeOriginal =
            new C2ComplexStateElementV432LikeOriginal[24][];
        internal readonly C2ComplexTransitionV430LikeOriginal[] Transitions =
            new C2ComplexTransitionV430LikeOriginal[24 * 24];
        internal readonly List<C2ComplexHelperDescV431LikeOriginal> HelpersV431LikeOriginal =
            new List<C2ComplexHelperDescV431LikeOriginal>();
    }

    internal sealed class C2ComplexUnitDescV430LikeOriginal
    {
        internal string Id = string.Empty;
        internal C2ComplexQuantDescV430LikeOriginal[] Chain = Array.Empty<C2ComplexQuantDescV430LikeOriginal>();
        internal Dictionary<string, C2ComplexAnimationV437LikeOriginal> AnimationsV437;
    }

    internal sealed class C2ComplexQuantRuntimeV430LikeOriginal
    {
        internal float Xc0;
        internal float Yc0;
        internal float Fi0;
        internal float Xc;
        internal float Yc;
        internal float Fi;
        internal int ForcePoint0;
        internal int ForcePoint1;
        internal int AxeL = 64;
        internal int LeftAngle;
        internal int RightAngle;

        internal void MoveQuantLikeOriginal(int fpIndex, float dx, float dy)
        {
            if (Math.Abs(dx) <= 0.00001f && Math.Abs(dy) <= 0.00001f) return;
            float fi = Fi * (Mathf.PI / 128.0f);
            float cos = Mathf.Cos(fi);
            float sin = Mathf.Sin(fi);
            float dl = cos * dx + sin * dy;
            float dt = -sin * dx + cos * dy;
            Xc0 = Xc;
            Yc0 = Yc;
            Fi0 = Fi;
            Xc += cos * dl;
            Yc += sin * dl;
            int r = fpIndex == 0 ? ForcePoint0 : ForcePoint1;
            if (r == 0) r = r < 0 ? -1 : 1;
            Fi += dt * 407.0f / r / 10.0f / 16.0f;
            int da = (int)(dt * AxeL / r);
            // C++ int compound assignment truncates only after evaluating DL +/- DA.
            LeftAngle = (int)(LeftAngle + dl + da);
            RightAngle = (int)(RightAngle + dl - da);
        }
    }

    internal sealed class C2ComplexHelperSlotV432LikeOriginal
    {
        internal C2ComplexHelperDescV431LikeOriginal Desc;
        internal int QuantIndex;
        internal int QuantPos;
        // Native IDS=0, SNS=FFFF is an embedded visual crew member, distinct
        // from IDS=FFFF (missing/killed). A null OneObject reference alone is
        // therefore not a reason to hide the part or require replenishment.
        internal bool MissingV437;
        internal C2UnitOriginalRuntime Runtime;
    }

    internal sealed class C2ComplexObjectRuntimeV430LikeOriginal
    {
        internal C2ComplexUnitDescV430LikeOriginal Desc;
        internal C2ComplexQuantRuntimeV430LikeOriginal[] Quants = Array.Empty<C2ComplexQuantRuntimeV430LikeOriginal>();
        internal float RealX;
        internal float RealY;
        // Mechanics.cpp stores the owner's last terrain RZ and PerformRotation uses
        // that previous value before sampling the new one. Keep it with the CObj.
        internal int Rz;
        internal int StartState;
        internal int FinalState;
        internal int TransTime;
        internal int GroundMotionState = 4;
        internal int GroundStandState;
        internal int DestDir = -1;
        internal bool Charged = true;
        internal bool ResSubtracted = true;
        internal bool NoMove;
        internal bool NoAttackV441;
        internal bool CrewMaterialActiveV441 = true;
        internal int LxOverrideV441;
        internal bool DeathProcessedV441;
        internal bool BackMotion;
        internal bool Lockpoints;
        // Mechanics.cpp stores LeaderID/TaleID on OneComplexObject.  The Unity
        // runtime keeps direct owner/runtime references; serial/index validation is
        // replaced only at the integration boundary, not by a different motion rule.
        internal C2UnitOriginalRuntime OwnerRuntimeV430LikeOriginal;
        internal C2ComplexObjectRuntimeV430LikeOriginal LeaderV430LikeOriginal;
        internal C2ComplexObjectRuntimeV430LikeOriginal TaleV430LikeOriginal;
        // Mechanics.cpp HelpersIDS/HelpersSNS/HelpersPos.  Direct runtime refs replace
        // the native Group[] index+serial pair only at the integration boundary.
        internal readonly List<C2ComplexHelperSlotV432LikeOriginal> HelpersV432LikeOriginal =
            new List<C2ComplexHelperSlotV432LikeOriginal>();
        internal readonly float[] ForwardDistanceV437 = new float[4];
        internal readonly float[] ForwardDxV437 = new float[4];
        internal readonly float[] ForwardDyV437 = new float[4];
        internal bool Initialized;
    }

    public sealed partial class C2UnitOriginalRuntime
    {
        internal C2CombatCoreV408LikeOriginal.MdTraits CombatTraitsV433;
        internal C2FormationRuntimeV167LikeOriginal.MeleeMdTraitsV407LikeOriginal MeleeTraitsV433;
        private string traitsSourceV433, traitsMdV433;
        private byte traitsNationV433;
        internal static C2UnitOriginalRuntime PrepareTraitsCacheV433(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            var rt = unit?.RuntimeLinkCachedLikeOriginal?.Runtime;
            if (rt == null) return null;
            if (rt.traitsSourceV433 != unit.SourceMonsterId || rt.traitsMdV433 != unit.ResolvedMd || rt.traitsNationV433 != unit.Nation)
            {
                rt.traitsSourceV433 = unit.SourceMonsterId; rt.traitsMdV433 = unit.ResolvedMd; rt.traitsNationV433 = unit.Nation;
                rt.CombatTraitsV433 = null; rt.MeleeTraitsV433 = null;
            }
            return rt;
        }

        internal bool GeometryCachedV433;
        internal C2UnitOriginalRuntimeAndRendererV1.MdModel GeometryMdV433;
        internal string GeometrySourceV433, GeometryResolvedMdV433;
        internal int GeometryRadiusV433, GeometryLxV433;
        internal byte GeometryNationV433, GeometryLockTypeV433;
        internal bool UnitsFieldRegisteredV433;
        internal int UnitsFieldCellXV433, UnitsFieldCellYV433, UnitsFieldLxV433, UnitsFieldGenerationV433;
        internal int OriginalCurUnitSpeedV433LikeOriginal;
        internal int OriginalPathDelayV425LikeOriginal;
        internal bool OriginalGLockV425LikeOriginal;
        internal bool OriginalLockStateInitializedV425LikeOriginal;
        internal int OriginalStandTimeV425LikeOriginal;
        internal int OriginalCPdestXV425LikeOriginal = -1;
        internal int OriginalCPdestYV425LikeOriginal = -1;
        internal bool OriginalPathRequestPendingV425LikeOriginal;
        internal float OriginalQueuedDestRealXV425LikeOriginal;
        internal float OriginalQueuedDestRealYV425LikeOriginal;

        // V430: state owned by the original MotionStyle-specific handlers.
        internal int OriginalSheepSpeedV430LikeOriginal;
        internal int OriginalRealVxV430LikeOriginal;
        internal int OriginalRealVyV430LikeOriginal;
        internal int OriginalKineticPowerV430LikeOriginal;
        internal int OriginalFlyForceXV430LikeOriginal;
        internal int OriginalFlyForceYV430LikeOriginal;
        internal int OriginalOverEarthV430LikeOriginal;
        internal int OriginalAnimationSpeedPercentV430LikeOriginal = 100;
        // V431 source-fidelity state owned by native MotionStyle handlers.
        internal int OriginalRzV431LikeOriginal;
        internal int OriginalPhaseV431LikeOriginal;
        internal bool OriginalBackMotionV431LikeOriginal;
        internal bool OriginalSkipGenericAnimationAdvanceV431LikeOriginal;
        internal bool OriginalSheepPostAnimationFinalizeV431LikeOriginal;
        internal C2ComplexObjectRuntimeV430LikeOriginal OriginalComplexObjectV430LikeOriginal;
    }
}


// ============================================================================
// Compatibility facade retained in the unified movement owner.
// No per-frame road-speed algorithm is allowed outside BrigadeOrder_GoOnRoad.
// ============================================================================
namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // Compatibility facade only. Original BrigadeOrders.cpp owns road-speed calculation
    // inside BrigadeOrder_GoOnRoad::SetUnitsSpeed; no per-unit Unity Update is allowed here.
    internal sealed class C2RoadUnitSpeedControllerV352 : MonoBehaviour
    {
        internal static void AttachLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit, int groupId)
        {
            if (unit == null) return;
            C2UnitOriginalRuntimeLinkLikeOriginal link = unit.RuntimeLinkCachedLikeOriginal;
            if (link != null && link.Runtime != null)
            {
                link.Runtime.OriginalGoOnRoadLikeOriginal = true;
                if (link.Runtime.OriginalUnitSpeedLikeOriginal <= 0)
                    link.Runtime.OriginalUnitSpeedLikeOriginal = 64;
            }
        }

        internal static void DetachLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null) return;
            C2UnitOriginalRuntimeLinkLikeOriginal link = unit.RuntimeLinkCachedLikeOriginal;
            if (link != null && link.Runtime != null)
            {
                link.Runtime.OriginalUnitSpeedLikeOriginal = 64;
                link.Runtime.OriginalGoOnRoadLikeOriginal = false;
            }
        }

        internal static bool IsGoOnRoadLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            C2UnitOriginalRuntimeLinkLikeOriginal link = unit != null ? unit.RuntimeLinkCachedLikeOriginal : null;
            return link != null && link.Runtime != null && link.Runtime.OriginalGoOnRoadLikeOriginal;
        }
    }
}


// ============================================================================
// MOVED FROM C2FormationGameplayV320LikeOriginal.cs: road-net route ownership.
// ============================================================================
namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode
    {
        // V426: the old Unity Dijkstra/full-route builder was removed.  Road route
        // ownership now stays in HumanGlobalSendTo + topology + streamed GoOnRoad.

        private static int RoadXOnRoadV352LikeOriginal(ParsedRoadNetKnotLikeOriginal[] knots, int index)
        {
            if (knots == null || index < 0 || index >= knots.Length) return 0;
            // Factures3D.cpp::CreateWayPointToPoint stores xOnRoad in OneNetKnot.
            // NEW2/ENR already carries the value; the parser supplies X as fallback for
            // old TENR.  Never recompute it here from a Unity approximation.
            return knots[index].XOnRoad;
        }

        private static int RoadYOnRoadV352LikeOriginal(ParsedRoadNetKnotLikeOriginal[] knots, int index)
        {
            if (knots == null || index < 0 || index >= knots.Length) return 0;
            return knots[index].YOnRoad;
        }

        private static void AppendRoadPointV352LikeOriginal(List<Vector2> points, int x, int y)
        {
            if (points == null) return;
            // OneNetWayPointToPoint::AddNextPoint.  Keep the retail source expression
            // verbatim, including its second P[Pi-1].x term.  It looks like a typo, but
            // changing it changes the generated point stream and therefore road motion.
            const int step = 26;
            if (points.Count > 0)
            {
                int px = Mathf.RoundToInt(points[points.Count - 1].x) >> 4;
                int pyBug = px;
                if (C2OriginalMovementMathV352.Norma(px - x, pyBug - y) <= step) return;
            }
            points.Add(new Vector2(x << 4, y << 4));
        }

        private static void Calk2PV352LikeOriginal(int x1, int y1, int x2, int y2, int n, int p, out int nx, out int ny)
        {
            if (n <= 0) { nx = x2; ny = y2; return; }
            nx = (x1 * (n - p) + x2 * p) / n;
            ny = (y1 * (n - p) + y2 * p) / n;
        }

        private static void Calk3PV352LikeOriginal(int x1, int y1, int x2, int y2, int x3, int y3, int n, int p, out int nx, out int ny)
        {
            if (n <= 0) { nx = x3; ny = y3; return; }
            nx = (x1 * (n - p) + x3 * p) / n + ((4 * x2 - 2 * (x1 + x3)) * p * (n - p)) / (n * n);
            ny = (y1 * (n - p) + y3 * p) / n + ((4 * y2 - 2 * (y1 + y3)) * p * (n - p)) / (n * n);
        }

        private static void AppendRoadEdgeWaypointsV352LikeOriginal(
            ParsedRoadNetKnotLikeOriginal[] knots, int startK, int endK, List<Vector2> output)
        {
            // Factures3D.cpp::OneNetWayPointToPoint::FillWay, Step=26.
            if (knots == null || startK < 0 || endK < 0 || startK >= knots.Length || endK >= knots.Length) return;
            ParsedRoadNetKnotLikeOriginal st = knots[startK];
            ParsedRoadNetKnotLikeOriginal en = knots[endK];
            int sxr = RoadXOnRoadV352LikeOriginal(knots, startK);
            int syr = RoadYOnRoadV352LikeOriginal(knots, startK);
            int exr = RoadXOnRoadV352LikeOriginal(knots, endK);
            int eyr = RoadYOnRoadV352LikeOriginal(knots, endK);
            int mpx = (st.X + en.X) / 2;
            int mpy = (st.Y + en.Y) / 2;
            int dst1 = C2OriginalMovementMathV352.Norma(sxr - mpx, syr - mpy);
            int dst2 = C2OriginalMovementMathV352.Norma(exr - mpx, eyr - mpy);
            const int step = 26;
            int xx, yy;

            if (st.NLinks == 2 && st.Links != null && st.Links.Length >= 2)
            {
                int other = st.Links[0] == endK ? st.Links[1] : st.Links[0];
                if (other >= 0 && other < knots.Length)
                {
                    int mpprx = (st.X + knots[other].X) / 2;
                    int mppry = (st.Y + knots[other].Y) / 2;
                    int dst1pr = Math.Max(1, C2OriginalMovementMathV352.Norma(mpx - mpprx, mpy - mppry));
                    int npp = dst1pr / step;
                    if (npp > 0)
                    {
                        int sp = (npp * dst1) / dst1pr;
                        for (int pp = sp; pp < npp; pp++)
                        {
                            Calk3PV352LikeOriginal(mpprx, mppry, sxr, syr, mpx, mpy, npp, pp, out xx, out yy);
                            AppendRoadPointV352LikeOriginal(output, xx, yy);
                        }
                    }
                }
            }
            else
            {
                int npp = dst1 / step;
                for (int pp = 0; pp < npp; pp++)
                {
                    Calk2PV352LikeOriginal(sxr, syr, mpx, mpy, npp, pp, out xx, out yy);
                    AppendRoadPointV352LikeOriginal(output, xx, yy);
                }
            }

            AppendRoadPointV352LikeOriginal(output, mpx, mpy);

            if (en.NLinks == 2 && en.Links != null && en.Links.Length >= 2)
            {
                int other = en.Links[0] == startK ? en.Links[1] : en.Links[0];
                if (other >= 0 && other < knots.Length)
                {
                    int mpprx = (en.X + knots[other].X) / 2;
                    int mppry = (en.Y + knots[other].Y) / 2;
                    int dst2pr = Math.Max(1, C2OriginalMovementMathV352.Norma(mpx - mpprx, mpy - mppry));
                    int npp = dst2pr / step;
                    if (npp > 0)
                    {
                        int sp = (npp * dst2) / dst2pr;
                        for (int pp = 1; pp < sp + 1; pp++)
                        {
                            Calk3PV352LikeOriginal(mpx, mpy, exr, eyr, mpprx, mppry, npp, pp, out xx, out yy);
                            AppendRoadPointV352LikeOriginal(output, xx, yy);
                        }
                    }
                }
            }
            else
            {
                int npp = dst2 / step;
                for (int pp = 1; pp < npp + 1; pp++)
                {
                    Calk2PV352LikeOriginal(mpx, mpy, exr, eyr, npp, pp, out xx, out yy);
                    AppendRoadPointV352LikeOriginal(output, xx, yy);
                }
            }
        }


    }
}


// ============================================================================
// V427: UNIT MOVEMENT RUNTIME physically moved out of renderer.
// This partial owns unit motion/order/path/collision execution; renderer keeps visuals.
// ============================================================================
namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2UnitOriginalRuntimeAndRendererV1
    {

        // ---- V427 moved from renderer: StepUnitRuntimeLikeOriginal ----
        private void StepUnitRuntimeLikeOriginal(C2UnitOriginalRuntime u, float dt)
        {
            if (u == null || u.Md == null) return;

            u.OriginalAnimationSpeedPercentV430LikeOriginal = 100;
            u.OriginalSkipGenericAnimationAdvanceV431LikeOriginal = false;
            u.OriginalSheepPostAnimationFinalizeV431LikeOriginal = false;

            // TickPanicV404 is bridge-owned morale logic. V427 accidentally coupled it
            // to ApplyTiring; keep that bridge behavior global, but native ApplyTiring
            // itself is now invoked at the exact MotionStyle handler position below.
            if (u.Info != null)
                C2MoraleRuntimeV404LikeOriginal.TickPanicV404LikeOriginal(u.Info);

            AnimModel beforeAnim = CurrentAnim(u);
            int beforeFrame = beforeAnim != null ? FixedFrameIndexLikeOriginal(u, beforeAnim) : 0;
            // NewMon.cpp::OneObject keeps NewCurSpritePrev beside NewCurSprite and
            // AttackObjLink tests the *crossing* of ActiveFrame, not frame>=ActiveFrame.
            // Capture the previous native frame before state/animation advancement.
            u.PreviousFrameIndexV415LikeOriginal = beforeFrame;
            u.PreviousFrameAnimIndexV415LikeOriginal = u.CurrentAnimIndex;
            bool frameFinishedBefore = u.FrameFinishedLikeOriginal;

            long stateStarted = ProfileUnitRuntimePhasesLikeOriginal
                ? global::System.Diagnostics.Stopwatch.GetTimestamp()
                : 0L;
            UpdateOriginalObjectRuntimeStateLikeOriginal(u, dt);
            if (ProfileUnitRuntimePhasesLikeOriginal)
                _profileStateTicksLikeOriginal += global::System.Diagnostics.Stopwatch.GetTimestamp() - stateStarted;

            long animationStarted = ProfileUnitRuntimePhasesLikeOriginal
                ? global::System.Diagnostics.Stopwatch.GetTimestamp()
                : 0L;
            AnimModel anim = CurrentAnim(u);
            if (anim == null || anim.Frames.Count == 0)
            {
                // NewMon.cpp::SetZeroFrame: NFrames<=1 is already finished.
                // A complex object's articulation advances in Mechanics.cpp,
                // independently of this empty ordinary sprite animation. Without
                // its finished flag NewMonsterSendToLink never retires the order.
                if (u.OriginalComplexObjectV430LikeOriginal != null)
                {
                    u.CurrentFrameLong = 0;
                    u.FrameFinishedLikeOriginal = true;
                    u.FrameFinishedLatchedForOrdersLikeOriginal = true;
                }
                if (ProfileUnitRuntimePhasesLikeOriginal)
                    _profileAnimationTicksLikeOriginal += global::System.Diagnostics.Stopwatch.GetTimestamp() - animationStarted;
                return;
            }

            if (UseOriginalMotionFramesFromPath && u.State == C2UnitOriginalState.Motion)
            {
                ApplyMotionFrameFromPathLikeOriginal(u, anim);
            }
            else if (WorkAnimationExternalPhaseLikeOriginal && u.State == C2UnitOriginalState.Work)
            {
                // BuildObjLink in the original advances building progress on the worker's work-cycle boundary.
                // In Unity the construction order owns _workPhase and calls SetWorkFramePhaseLikeOriginal(),
                // so the runtime must not also advance #WORK by generic FPS or frames will drift/desync.
                u.FrameFinishedLikeOriginal = false;
            }
            else if (u.OriginalSkipGenericAnimationAdvanceV431LikeOriginal)
            {
                // MotionHandlerOfNewSheeps does not SetNextFrame when a non-zero
                // speed movement attempt is blocked.
                u.OriginalSkipGenericAnimationAdvanceV431LikeOriginal = false;
            }
            else
            {
                // Keep the original 8.8 fixed-point frame counter without
                // rounding every Unity render frame independently.  The old
                // RoundToInt/+1 path accumulated a visible drift and could
                // advance an MD animation even when dt was zero.
                double exactDelta =
                    Math.Max(0.01, u.AnimFps) * 256.0 * Math.Max(0.0, dt) *
                    Math.Max(0, u.OriginalAnimationSpeedPercentV430LikeOriginal) / 100.0 +
                    u.AnimFrameLongRemainderLikeOriginal;
                int delta = (int)Math.Floor(exactDelta);
                u.AnimFrameLongRemainderLikeOriginal = exactDelta - delta;

                int maxLong = Math.Max(1, anim.Frames.Count) << 8;
                if (anim.Frames.Count <= 1)
                {
                    u.CurrentFrameLong = 0;
                    u.FrameFinishedLikeOriginal = true;
                    u.FrameFinishedLatchedForOrdersLikeOriginal = true;
                }
                else
                {
                    u.CurrentFrameLong += delta;
                    if (u.CurrentFrameLong >= maxLong)
                    {
                        // OneObject::SetNextFrame latches FrameFinished until SetZeroFrame.
                        // The renderer keeps its existing visual loop adapter, but external
                        // order logic also receives a stable latch so a MonoBehaviour update
                        // cannot miss the one simulation quantum in which FrameFinished is true.
                        u.FrameFinishedLikeOriginal = true;
                        u.FrameFinishedLatchedForOrdersLikeOriginal = true;
                        if (ShouldLoopAnimationLikeOriginal(u, anim))
                            u.CurrentFrameLong %= maxLong;
                        else
                            u.CurrentFrameLong = maxLong - 1;
                    }
                    else
                    {
                        u.FrameFinishedLikeOriginal = false;
                    }
                }
            }

            if (u.OriginalSheepPostAnimationFinalizeV431LikeOriginal)
            {
                u.OriginalSheepPostAnimationFinalizeV431LikeOriginal = false;
                if (u.FrameFinishedLikeOriginal)
                {
                    int standV431 = ResolveRuntimeStandAnimationIndexV322LikeOriginal(u);
                    if (standV431 >= 0)
                        SelectAnimationStateLikeOriginal(
                            u, C2UnitOriginalState.Stand, standV431, true,
                            "MotionHandlerOfNewSheeps_frame_finished_v431");
                }
                anim = CurrentAnim(u);
            }

            if (u.AnimState != null)
            {
                u.AnimState.CurrentFrameLong = u.CurrentFrameLong;
                u.AnimState.FrameFinished = u.FrameFinishedLikeOriginal;
            }

            int afterFrame = FixedFrameIndexLikeOriginal(u, anim);
            if (ProfileUnitRuntimePhasesLikeOriginal)
                _profileAnimationTicksLikeOriginal += global::System.Diagnostics.Stopwatch.GetTimestamp() - animationStarted;
            bool frameChanged = afterFrame != beforeFrame ||
                                frameFinishedBefore != u.FrameFinishedLikeOriginal ||
                                string.IsNullOrEmpty(u.LastFrameKey);
            if (frameChanged || u.FrameUploadPendingLikeOriginal)
                // COSSACKS2 advances CurrentFrameLong in simulation, but resolves
                // and submits the sprite later from MiniMap4X.cpp::DrawUnits.
                // Keep the stages separate here as well.  If a slow render frame
                // contains several 40 ms simulation quanta, only the final frame
                // can be displayed; uploading every intermediate frame was pure
                // work and multiplied the slowdown under load.
                u.FrameUploadPendingLikeOriginal = true;
        }

        private void ApplyMotionTiringProfiledV430LikeOriginal(C2UnitOriginalRuntime u, float dt)
        {
            long tiringStarted = ProfileUnitRuntimePhasesLikeOriginal
                ? global::System.Diagnostics.Stopwatch.GetTimestamp()
                : 0L;
            ApplyTiringLikeOriginal(u, dt);
            if (ProfileUnitRuntimePhasesLikeOriginal)
                _profileTiringTicksLikeOriginal += global::System.Diagnostics.Stopwatch.GetTimestamp() - tiringStarted;
        }

        // ---- V427 moved from renderer: ApplyTiringLikeOriginal ----
        private void ApplyTiringLikeOriginal(C2UnitOriginalRuntime u, float dt)
        {
            if (u == null || u.Info == null || u.Md == null || dt <= 0.0f) return;
            if (!C2MoraleRuntimeV404LikeOriginal.AllowTiringLikeOriginal(u.Info)) return;
            // NewMon.cpp calls APPLY_TIRING only from MotionHandlerForSingleStepObjects
            // and MotionHandlerForFlyingObjects. The caller enforces that dispatch.
            if (u.CurrentAnimIndex < 0 || u.CurrentAnimIndex >= u.Md.Animations.Count) return;
            AnimModel anim = u.Md.Animations[u.CurrentAnimIndex];
            if (anim == null) return;

            // NewMon.cpp::ApplyTiring, GameSpeed=256.  Negative animation tiring is
            // multiplied by the current road-zone tiring only for BRIGADEORDER_GOONROAD.
            int dtiring = anim.TiringChange;
            if (dtiring < 0)
            {
                int roadTiring = C2FormationRuntimeV167LikeOriginal
                    .GetRoadTiringMultiplierV403ELikeOriginal(u.Info);
                dtiring = (dtiring * roadTiring) >> 8;
            }

            // NewMon.cpp::ApplyTiring ArcadeMode branch.
            if (OriginalArcadeModeV431LikeOriginal)
            {
                bool scaleForThisObject = OriginalPlayerCountV431LikeOriginal != 1 ||
                    u.Info.Nation == (byte)Mathf.Clamp(OriginalMyNationV431LikeOriginal, 0, 255);
                if (scaleForThisObject)
                {
                    if (dtiring < 0) dtiring >>= 1;
                    else dtiring <<= 1;
                }
            }

            int tiring = Mathf.Clamp(u.Info.GetTiredLikeOriginal, 0, 100000);
            tiring += (dtiring * OriginalGameSpeed256LikeOriginal) >> 8;
            tiring = Mathf.Clamp(tiring, 0, 100000);
            u.Info.GetTiredLikeOriginal = tiring;
            u.Info.TiringRemainingPercentLikeOriginal = tiring / 1000.0f;

            C2FormationRuntimeV167LikeOriginal.RefreshFormationTiringStateV403ELikeOriginal(u.Info);
            C2MoraleRuntimeV404LikeOriginal.ApplyTiringMoraleStepV404LikeOriginal(u.Info);
        }

        // NewMon.cpp object-loop ordering: MotionStyle handler runs first,
        // then the SlowRecharge block. The body below preserves the pre-V431 bridge
        // recharge representation; only its source-order position is changed here.
        private bool ProcessSlowRechargeAfterMotionStyleV431LikeOriginal(C2UnitOriginalRuntime u)
        {
            if (u == null || u.Md == null) return false;
            AnimModel anim = CurrentAnim(u);
            if (u.State == C2UnitOriginalState.Recharge)
            {
                if (anim == null) return true;
                if (!u.FrameFinishedLikeOriginal) return true;
                int reloadTicks = Math.Max(1, anim.Frames.Count);
                if (u.RechargeTicksRemainingLikeOriginal > reloadTicks)
                {
                    u.RechargeTicksRemainingLikeOriginal -= reloadTicks;
                    SelectAnimationStateLikeOriginal(
                        u, C2UnitOriginalState.Recharge, u.CurrentAnimIndex, true,
                        "slow_recharge_cycle_v431");
                }
                else
                {
                    u.RechargeTicksRemainingLikeOriginal = 0;
                    int stand = ResolveRuntimeStandAnimationIndexV322LikeOriginal(u);
                    if (stand < 0) stand = ResolveAnimationIndexLikeOriginal(u.Md, StandAnimationName);
                    if (stand >= 0)
                        SelectAnimationStateLikeOriginal(
                            u, C2UnitOriginalState.Stand, stand, true,
                            "slow_recharge_finished_v431");
                    ClearRuntimeSlowRechargeDelayLikeOriginal(u);
                }
                return true;
            }
            if (u.RechargeTicksRemainingLikeOriginal > 0 &&
                u.RechargeAttackModeLikeOriginal == 1 &&
                u.State == C2UnitOriginalState.Stand &&
                !HasAnyLocalOrderV431LikeOriginal(u) &&
                !u.HasMoveTargetLikeOriginal &&
                !u.MoveDeferredUntilNeutralStandLikeOriginal)
            {
                if (BeginRuntimeSlowRechargeLikeOriginal(u, 0)) return true;
            }
            return false;
        }

        private static bool HasAnyLocalOrderV431LikeOriginal(C2UnitOriginalRuntime u)
        {
            if (u == null || u.Info == null) return false;
            return u.ArtilleryChargeOrderV439 >= 0 || C2OriginalOrderChainV352.HasLocalMoveOrderLikeOriginal(u.Info) ||
                   C2CombatRuntimeV334LikeOriginal.IsAttackOrderActiveV403LikeOriginal(u.Info);
        }

        // ---- V427 moved from renderer: UpdateOriginalObjectRuntimeStateLikeOriginal ----
        private void UpdateOriginalObjectRuntimeStateLikeOriginal(C2UnitOriginalRuntime u, float dt)
        {
            using (C2FrameCostProbe.Measure(C2FrameCostProbe.Phase.MotionState))
            {
            if (u == null || u.Md == null) return;
            AnimModel anim = CurrentAnim(u);
            if (u.State == C2UnitOriginalState.Death)
            {
                // Mechanics.cpp::DieComplexObject removes the dynamic complex footprint.
                if (MotionStyleCodeV411LikeOriginal(u.Md.MotionStyle) == 6 &&
                    u.OriginalComplexObjectV430LikeOriginal != null)
                    C2OriginalMovementSystemV425LikeOriginal.UnlockComplexObjectV430LikeOriginal(u);

                // NewMon.cpp's dead-object path calls MotionHandlerForFlyingObjects,
                // whose very first operation is APPLY_TIRING, before the Sdoxlo return.
                if (MotionStyleCodeV411LikeOriginal(u.Md.MotionStyle) == 8)
                {
                    ApplyMotionTiringProfiledV430LikeOriginal(u, dt);
                    if (u.OriginalOverEarthV430LikeOriginal != 0)
                        u.OriginalOverEarthV430LikeOriginal -= 4;
                }
                // LongProcesses retains DEATH's final frame while Sdoxlo ages.
                // Switching early to DEATHLIE1 skipped that counter forever.
                TickOriginalDeathV435LikeOriginal(u);
                return;
            }

            if (u.State == C2UnitOriginalState.Transition && anim != null && u.FrameFinishedLikeOriginal)
            {
                // NewMon.cpp calls TryToStand again on following simulation passes.
                // V411 does the same decision step here when the one-shot TRANS/
                // UATTACK/PATTACK clip ends. LocalNewState was already updated at
                // the moment retail installs the transition animation.
                if (u.PendingPostureAfterNeutralLikeOriginal >= 0)
                    u.PendingPostureAfterNeutralLikeOriginal = -1;
                u.TransitionTargetLocalPostureV411LikeOriginal = int.MinValue;
                u.PendingStandAnimIndexLikeOriginal = -1;
                TryToStandRuntimeV411LikeOriginal(u, false, "posture_transition_finished_v411");
                if (u.State == C2UnitOriginalState.Transition)
                    return;
                if (u.MoveDeferredUntilNeutralStandLikeOriginal)
                {
                    u.MoveDeferredUntilNeutralStandLikeOriginal = false;
                    StartRuntimeMoveTargetNowLikeOriginal(u, "move_after_posture_transition_v411");
                }
            }

            // Completed one-shots must be consumed before the unconditional
            // MotionStyle return as well. Otherwise Attack stays at its final
            // frame forever and only the first strike can deal damage.
            if (u.State == C2UnitOriginalState.Rest && anim != null && u.FrameFinishedLikeOriginal)
            {
                int stand = ResolveAnimationIndexLikeOriginal(u.Md, StandAnimationName);
                if (stand >= 0) SelectAnimationStateLikeOriginal(u, C2UnitOriginalState.Stand, stand, true, "rest_finished");
                return;
            }

            if (u.State == C2UnitOriginalState.Attack && anim != null && u.FrameFinishedLikeOriginal)
            {
                int stand = ResolveRuntimeStandAnimationIndexV322LikeOriginal(u);
                if (stand < 0) stand = ResolveAnimationIndexLikeOriginal(u.Md, StandAnimationName);
                if (stand >= 0)
                    SelectAnimationStateLikeOriginal(u, C2UnitOriginalState.Stand, stand, true, "attack_finished");
                if (u.Info != null)
                    C2UnitOrderRuntimeV325LikeOriginal.IssueLikeOriginal(
                        u.Info, C2UnitOrderKindV325LikeOriginal.Stand, "attack_finished", string.Empty);
                return;
            }

            // NewMon.cpp::TryToMove never starts a motion frame while
            // LocalNewState != NewState.  TryToStand must finish UATTACK/TRANSxy
            // first.  Keep the destination queued until that transition reaches
            // its final stand frame instead of letting the unit slide immediately.
            if (u.MoveDeferredUntilNeutralStandLikeOriginal)
            {
                AnimModel deferredAnim = CurrentAnim(u);
                bool mayBreakNow = deferredAnim == null ||
                                   u.FrameFinishedLikeOriginal ||
                                   deferredAnim.CanBeBroken ||
                                   deferredAnim.MoveBreak;
                if (u.State != C2UnitOriginalState.Transition && mayBreakNow)
                {
                    u.MoveDeferredUntilNeutralStandLikeOriginal = false;
                    StartRuntimeMoveTargetNowLikeOriginal(u, "move_after_posture_transition");
                    anim = CurrentAnim(u);
                }
            }

            int runtimeMotionStyleV430 = MotionStyleCodeV411LikeOriginal(u.Md.MotionStyle);

            // NewMon.cpp runs MotionStyle post-handlers unconditionally after the
            // LocalOrder pass for every live non-building object. Do not gate them
            // by the bridge's common stop radius: each native handler owns its own
            // DestX threshold/coasting/stand semantics. Styles 0/1/3/4 have no
            // post-handler and intentionally continue into the common order mover.
            if (u.ActiveLikeOriginal)
            {
                switch (runtimeMotionStyleV430)
                {
                    case 2: // SHEEPS -> PerformMotion2 -> MotionHandlerOfNewSheeps
                    case 5: // NEWSHEEPS -> MotionHandlerOfNewSheeps
                        AdvanceNewSheepMotionV430LikeOriginal(u, u.HasMoveTargetLikeOriginal);
                        ProcessSlowRechargeAfterMotionStyleV431LikeOriginal(u);
                        return;
                    case 6: // COMPLEXOBJECT
                        AdvanceComplexObjectMotionV430LikeOriginal(u, u.HasMoveTargetLikeOriginal);
                        ProcessSlowRechargeAfterMotionStyleV431LikeOriginal(u);
                        return;
                    case 7: // SINGLESTEP -> MotionHandlerForSingleStepObjects
                        AdvanceSingleStepHandlerV431LikeOriginal(u, dt);
                        ProcessSlowRechargeAfterMotionStyleV431LikeOriginal(u);
                        return;
                    case 8: // FLY -> MotionHandlerForFlyingObjects
                        ApplyMotionTiringProfiledV430LikeOriginal(u, dt);
                        AdvanceFlyingMotionV430LikeOriginal(u, u.HasMoveTargetLikeOriginal);
                        ProcessSlowRechargeAfterMotionStyleV431LikeOriginal(u);
                        return;
                }
            }

            if (ProcessSlowRechargeAfterMotionStyleV431LikeOriginal(u)) return;

            if ((!u.HasMoveTargetLikeOriginal || !u.ActiveLikeOriginal) && u.Info != null)
                C2OriginalMovementSystemV425LikeOriginal.MarkUnitStandingV425LikeOriginal(u.Info, u);

            if (u.HasMoveTargetLikeOriginal && u.ActiveLikeOriginal)
            {
                float dx = u.MoveTargetRealXLikeOriginal - u.RuntimeRealXLikeOriginal;
                float dy = u.MoveTargetRealYLikeOriginal - u.RuntimeRealYLikeOriginal;
                float distReal = Mathf.Sqrt(dx * dx + dy * dy);
                float stopReal = Mathf.Max(0.01f, OriginalMotionMinStopDistanceOriginalPixels) * 16.0f;

                if (distReal > stopReal)
                {
                    if (ProfileUnitRuntimePhasesLikeOriginal) _profileMovingCallsLikeOriginal++;

                    float speedOriginalPx = ResolveRuntimeMoveSpeedOriginalPixelsPerSecondLikeOriginal(u, u.MoveSpeedOriginalPixelsPerSecondLikeOriginal);
                    // NewMon.cpp::TryToMove applies MoreCharacter::Rate[NewState-1]
                    // after the base/group speed. SINGLESTEP has its own exact branch
                    // below; ordinary motion needs the same one-time state multiplier.
                    if (u.PostureWeaponTypeLikeOriginal >= 0 && u.Md != null &&
                        u.Md.Rate != null && u.PostureWeaponTypeLikeOriginal < u.Md.Rate.Length)
                    {
                        int rateV405B = u.Md.Rate[u.PostureWeaponTypeLikeOriginal];
                        speedOriginalPx = Mathf.Max(1.0f, speedOriginalPx * rateV405B / 16.0f);
                    }

                    float speedRealPerSecond = Mathf.Max(1.0f, speedOriginalPx * 16.0f);
                    float stepReal = Mathf.Min(distReal, speedRealPerSecond * Mathf.Max(0f, dt));

                    float nx = dx / Mathf.Max(0.0001f, distReal);
                    float ny = dy / Mathf.Max(0.0001f, distReal);
                    long movingPhaseStarted = ProfileUnitRuntimePhasesLikeOriginal
                        ? global::System.Diagnostics.Stopwatch.GetTimestamp()
                        : 0L;
                    ApplyOriginalSingleStepBoidsSteeringLikeOriginal(u, ref nx, ref ny, ref stepReal);
                    stepReal = Mathf.Min(distReal, stepReal);
                    if (ProfileUnitRuntimePhasesLikeOriginal)
                        _profileBoidsTicksLikeOriginal += global::System.Diagnostics.Stopwatch.GetTimestamp() - movingPhaseStarted;

                    float beforeRealX;
                    float beforeRealY;
                    movingPhaseStarted = ProfileUnitRuntimePhasesLikeOriginal
                        ? global::System.Diagnostics.Stopwatch.GetTimestamp()
                        : 0L;
                    bool movedByMotionField = TryAdvanceRuntimeRealWithOriginalMotionFieldLikeOriginal(u, nx, ny, stepReal, out beforeRealX, out beforeRealY);
                    if (ProfileUnitRuntimePhasesLikeOriginal)
                        _profileMotionFieldTicksLikeOriginal += global::System.Diagnostics.Stopwatch.GetTimestamp() - movingPhaseStarted;

                    byte realDir = DirectionFromRealDeltaLikeOriginal(nx, ny);
                    SetRuntimeFacingLikeOriginal(u, realDir);

                    if (!movedByMotionField)
                    {
                        if (ProfileUnitRuntimePhasesLikeOriginal)
                        {
                            _profileBlockedMoveCallsLikeOriginal++;
                            movingPhaseStarted = global::System.Diagnostics.Stopwatch.GetTimestamp();
                        }
                        // SINGLESTEP only fails on terrain/building bars. Other
                        // units are a steering influence and never a hard bar.
                        if (!IsMdSingleStepPassThroughLikeOriginal(u))
                        {
                            bool yielded = TryResolveMovingUnitBlockLikeOriginal(
                                u,
                                beforeRealX + nx * stepReal,
                                beforeRealY + ny * stepReal,
                                nx,
                                ny);
                            if (!yielded)
                                TryRequestIdleBlockerYieldAsideLikeOriginal(u, beforeRealX + nx * stepReal, beforeRealY + ny * stepReal, nx, ny);
                        }

                        // Original Motion.cpp refuses a blocked step; do not let unit feet enter building bars.
                        // Keep target and animation state, next tick may slide or path may be refreshed by caller.
                        int blockedMotion = ResolveRuntimeMotionAnimationIndexV223LikeOriginal(u);
                        if (blockedMotion >= 0 && u.State != C2UnitOriginalState.Motion)
                            SelectAnimationStateLikeOriginal(u, C2UnitOriginalState.Motion, blockedMotion, false, "move_blocked_wait_original_motionfield");
                        if (ProfileUnitRuntimePhasesLikeOriginal)
                            _profileBlockedMoveTicksLikeOriginal += global::System.Diagnostics.Stopwatch.GetTimestamp() - movingPhaseStarted;
                        return;
                    }

                    long worldSyncStarted = ProfileUnitRuntimePhasesLikeOriginal
                        ? global::System.Diagnostics.Stopwatch.GetTimestamp()
                        : 0L;
                    if (UseContinuousWorldDeltaForOriginalMotion)
                        UpdateRuntimeWorldAndRealContinuousLikeOriginal(u, beforeRealX, beforeRealY);
                    else
                        UpdateRuntimeWorldAndRealLikeOriginal(u);
                    if (ProfileUnitRuntimePhasesLikeOriginal)
                        _profileWorldSyncTicksLikeOriginal += global::System.Diagnostics.Stopwatch.GetTimestamp() - worldSyncStarted;

                    int motion = ResolveRuntimeMotionAnimationIndexV223LikeOriginal(u);
                    if (motion >= 0 && u.State != C2UnitOriginalState.Motion)
                        SelectAnimationStateLikeOriginal(u, C2UnitOriginalState.Motion, motion, false, "move_start_path");

                    if (LogOriginalMotionOnce && _originalMotionLogs < 10)
                    {
                        _originalMotionLogs++;
                        AnimModel ma = CurrentAnim(u);
                        int frames = ma != null ? ma.Frames.Count : 0;
                        Debug.Log(LogPrefix + " ORIGINAL_MOTION_PATH unit='" + (u.Probe != null ? u.Probe.MonsterId : "") + "'" +
                                  " speedPxSec=" + speedOriginalPx.ToString("0.###", CultureInfo.InvariantCulture) +
                                  " speedRealSec=" + speedRealPerSecond.ToString("0.###", CultureInfo.InvariantCulture) +
                                  " stepReal=" + stepReal.ToString("0.###", CultureInfo.InvariantCulture) +
                                  " totalPath=" + u.TotalPathLikeOriginal.ToString("0.###", CultureInfo.InvariantCulture) +
                                  " rInFrame=" + GetMotionRInFrameLikeOriginal(u, ma).ToString(CultureInfo.InvariantCulture) +
                                  " continuousWorldDelta=" + UseContinuousWorldDeltaForOriginalMotion +
                                  " frames=" + frames.ToString(CultureInfo.InvariantCulture) +
                                  " real=(" + u.RuntimeRealXLikeOriginal.ToString("0.###", CultureInfo.InvariantCulture) + "," + u.RuntimeRealYLikeOriginal.ToString("0.###", CultureInfo.InvariantCulture) + ")");
                    }
                    return;
                }

                float finishBeforeRealX = u.RuntimeRealXLikeOriginal;
                float finishBeforeRealY = u.RuntimeRealYLikeOriginal;

                // FIX7:
                // NewMonsterPreciseSendTo under OrderedUnlimitedMotion must really finish the
                // current BORNPOINTS waypoint.  FIX6 still used CheckBar on the final snap;
                // for EngKaz this meant: the unit came within stopReal of the last exit point,
                // CheckBar said "still inside/too close to building radius", the code did not
                // copy RuntimeRealX/Y to the target, but still advanced/finished the path.
                // Visually the unit stopped short inside the barracks exit.
                //
                // Original Build.cpp path:
                //   SetOrderedUnlimitedMotion(0);
                //   NewMonsterPreciseSendTo(BORNPOINTS[1..N], 16, 2+128);
                // so BORNPOINTS waypoints are allowed to complete even if MFIELDS/CheckBar
                // would reject the bar.  Normal post-born rally remains CheckBar-routed.
                bool preciseBornWaypointLikeOriginal =
                    u.PreciseBornPathLikeOriginal &&
                    (u.MovePathRealWaypointsLikeOriginal == null ||
                     u.PreciseBornPathLastWaypointIndexLikeOriginal < 0 ||
                     u.MovePathIndexLikeOriginal <= u.PreciseBornPathLastWaypointIndexLikeOriginal);

                bool canFinishAtTargetLikeOriginal =
                    preciseBornWaypointLikeOriginal ||
                    CanRuntimeUnitFinishTargetRealLikeOriginal(u, u.MoveTargetRealXLikeOriginal, u.MoveTargetRealYLikeOriginal);

                if (canFinishAtTargetLikeOriginal)
                {
                    u.RuntimeRealXLikeOriginal = u.MoveTargetRealXLikeOriginal;
                    u.RuntimeRealYLikeOriginal = u.MoveTargetRealYLikeOriginal;
                    if (UseContinuousWorldDeltaForOriginalMotion)
                        UpdateRuntimeWorldAndRealContinuousLikeOriginal(u, finishBeforeRealX, finishBeforeRealY);
                    else
                        UpdateRuntimeWorldAndRealLikeOriginal(u);
                }
                else
                {
                    if (TryRetargetBlockedFinishToFreePositionLikeOriginal(u))
                        return;

                    EmitBornStopAuditLikeOriginal(u, "blocked_finish_checkbar", "normal waypoint refused by CheckBar; target kept alive");
                    // Normal movement must not silently consume a blocked target/waypoint.
                    // Keep the target alive so the unit can retry/slide/reroute next tick.
                    int blockedMotion = ResolveRuntimeMotionAnimationIndexV223LikeOriginal(u);
                    if (blockedMotion >= 0 && u.State != C2UnitOriginalState.Motion)
                        SelectAnimationStateLikeOriginal(u, C2UnitOriginalState.Motion, blockedMotion, false, "move_finish_blocked_wait_original_checkbar");
                    return;
                }

                EmitBornExitAuditLikeOriginal(u, preciseBornWaypointLikeOriginal ? "waypoint_finish_precise" : "waypoint_finish_normal", "canFinish=" + canFinishAtTargetLikeOriginal);
                if (AdvanceRuntimeMoveWaypointLikeOriginal(u))
                    return;

                if (TryStartGotoFinePositionAfterProductionLikeOriginal(u, "move_finished_after_production"))
                    return;

                EmitBornStopAuditLikeOriginal(u, "move_finished", "all waypoints consumed; selecting stand");
                // Groups.cpp::PositionOrder::SendToPosition appends RotUnit(...,2)
                // after SmartSend for a directed RMB command.  Do not snap direction:
                // execute Brigade.cpp::RotUnit/RotUnitLink until that order completes.
                if (u.HasFinalFacingDirLikeOriginal &&
                    !AdvanceFinalRotUnitV352LikeOriginal(u, u.FinalFacingDirLikeOriginal))
                    return;

                u.HasMoveTargetLikeOriginal = false;
                u.HasFinalFacingDirLikeOriginal = false;
                u.FinalRotUnitActiveV352LikeOriginal = false;
                u.MovePreservesCombatPostureV405BLikeOriginal = false;
                if (u.Info != null)
                    u.Info.C2NeutralPeasantUnitsV15SetMovingFlagLikeOriginal(false, false);

                int stand = ResolveRuntimeStandAnimationIndexV322LikeOriginal(u);
                if (stand < 0) stand = ResolveAnimationIndexLikeOriginal(u.Md, RestAnimationName);
                if (stand >= 0 && u.State != C2UnitOriginalState.Stand)
                    SelectAnimationStateLikeOriginal(u, C2UnitOriginalState.Stand, stand, true, "move_finished");

                int postureAfterMove = u.PostureAfterMoveLikeOriginal;
                u.PostureAfterMoveLikeOriginal = -1;
                if (postureAfterMove >= 0)
                {
                    SetRuntimeCombatPostureV322LikeOriginal(u, postureAfterMove, true);
                    return;
                }
            }

            if (u.State == C2UnitOriginalState.Stand &&
                u.PostureWeaponTypeLikeOriginal < 0 &&
                EnableOriginalRestRandomLikeOriginal &&
                Time.unscaledTime >= u.NextRestCheckTime)
            {
                int rest = ResolveAnimationIndexLikeOriginal(u.Md, RestAnimationName);
                bool canRest = rest >= 0 && u.Md.Animations[rest].Frames.Count > 1;
                float roll = StableUnitRandom01LikeOriginal(u.Probe, 97 + u.RestRollCounter++);
                u.NextRestCheckTime = Time.unscaledTime + StableUnitRandomRangeLikeOriginal(u.Probe, 191 + u.RestRollCounter, RestMinDelaySeconds, RestMaxDelaySeconds);
                if (canRest && roll <= Mathf.Clamp01(RestChancePerCheck))
                {
                    SelectAnimationStateLikeOriginal(u, C2UnitOriginalState.Rest, rest, true, "rest_random");
                    return;
                }
            }
            }
        }

        // ---- V427 moved from renderer: SetRuntimeMoveDestinationWorldLikeOriginal ----
        internal void SetRuntimeMoveDestinationWorldLikeOriginal(C2UnitOriginalRuntime u, Vector3 targetWorld, float speedOriginalPixelsPerSecond)
        {
            if (u == null || !u.ActiveLikeOriginal) return;
            if (u.State == C2UnitOriginalState.Death) return;

            C2BattleTerrainMode mode = u.Info != null ? u.Info.OwnerMode : _battle;
            float px;
            float py;
            if (mode != null && mode.C2NeutralPeasantUnitsV2WorldToOriginalPixelV15LikeOriginal(targetWorld, out px, out py))
            {
                SetRuntimeMoveDestinationRealLikeOriginal(u, px * 16.0f, py * 16.0f, speedOriginalPixelsPerSecond, false, 0);
                return;
            }

            targetWorld.y = u.WorldPosition.y;
            u.MoveTargetWorldLikeOriginal = targetWorld;
            u.MoveSpeedOriginalPixelsPerSecondLikeOriginal = ResolveRuntimeMoveSpeedOriginalPixelsPerSecondLikeOriginal(u, speedOriginalPixelsPerSecond);
            u.HasMoveTargetLikeOriginal = true;
            u.BlockedFinishRetargetAttemptLikeOriginal = 0;
            if (u.Info != null) u.Info.C2NeutralPeasantUnitsV15SetMovingFlagLikeOriginal(true, false);
        }

        // ---- V427 moved from renderer: SetRuntimeMoveDestinationRealLikeOriginal ----
        internal void SetRuntimeMoveDestinationRealLikeOriginal(C2UnitOriginalRuntime u, float destRealX, float destRealY, float speedOriginalPixelsPerSecond, bool hasFinalFacingDir, byte finalFacingDir)
        {
            if(u?.Info==null||u.State==C2UnitOriginalState.Death)return;
            C2OriginalOrderChainV352.SubmitTaskMoveV433LikeOriginal(u.Info,destRealX,destRealY,
                speedOriginalPixelsPerSecond,hasFinalFacingDir,finalFacingDir,false,"task_move_child");
        }

        internal void InstallOrderDestinationV433LikeOriginal(C2UnitOriginalRuntime u, float destRealX, float destRealY, float speedOriginalPixelsPerSecond, bool hasFinalFacingDir, byte finalFacingDir)
        {
            if (u == null || u.State == C2UnitOriginalState.Death) return;
            EnsureRuntimeRealFromInfoLikeOriginal(u);
            if (u.Info != null)
            {
                // path.cpp::OneObject::CreatePath does not search here; it queues the request.
                C2OriginalMovementSystemV425LikeOriginal.AddPathRequestorV425LikeOriginal(
                    u.Info, destRealX, destRealY, speedOriginalPixelsPerSecond,
                    hasFinalFacingDir, finalFacingDir, "OneObject::CreatePath");
                u.HasFinalFacingDirLikeOriginal = hasFinalFacingDir;
                u.FinalFacingDirLikeOriginal = finalFacingDir;
                u.HasMoveTargetLikeOriginal = false;
                u.MovePathRealWaypointsLikeOriginal = null;
                u.MovePathIndexLikeOriginal = 0;
                u.Info.C2NeutralPeasantUnitsV15SetMovingFlagLikeOriginal(true, false);
                return;
            }
            SetRuntimeMoveDestinationRealInternalLikeOriginal(u, destRealX, destRealY, speedOriginalPixelsPerSecond, hasFinalFacingDir, finalFacingDir, true, false, "direct_order_no_info");
        }

        internal void FinishOrdinaryOrderV433LikeOriginal(C2UnitOriginalRuntime u, bool stand)
        {
            C2OriginalMovementSystemV425LikeOriginal.DisablePathRequestV425LikeOriginal(u.Info);
            u.HasMoveTargetLikeOriginal=false;
            u.MovePathRealWaypointsLikeOriginal=null;
            u.MovePathIndexLikeOriginal=0;
            u.MoveDeferredUntilNeutralStandLikeOriginal=false;
            if(stand)TryToStandRuntimeV411LikeOriginal(u,false,"NewMonsterSendToLink_finished_v433");
            u.Info?.C2NeutralPeasantUnitsV15SetMovingFlagLikeOriginal(false,false);
        }

        // NewMon.cpp::NewMonsterPreciseSendToLink, dr < MotionDist.
        internal void FinishPreciseOrderV433LikeOriginal(C2UnitOriginalRuntime u,
            float x, float y, bool face, byte direction)
        {
            if (u == null) return;
            C2OriginalMovementSystemV425LikeOriginal.DisablePathRequestV425LikeOriginal(u.Info);
            if (u.OriginalComplexObjectV430LikeOriginal == null)
            {
                float oldX = u.RuntimeRealXLikeOriginal, oldY = u.RuntimeRealYLikeOriginal;
                u.RuntimeRealXLikeOriginal = x;
                u.RuntimeRealYLikeOriginal = y;
                int lx=C2OriginalMovementSystemV425LikeOriginal.ResolveLxLikeOriginal(u.Info);
                if (((Mathf.RoundToInt(oldX)-(lx<<7))>>8)!=((Mathf.RoundToInt(x)-(lx<<7))>>8) ||
                    ((Mathf.RoundToInt(oldY)-(lx<<7))>>8)!=((Mathf.RoundToInt(y)-(lx<<7))>>8))
                    C2OriginalMovementSystemV425LikeOriginal.MoveUnitsFieldAfterSingleStepV427LikeOriginal(u.Info, oldX, oldY, x, y);
                UpdateRuntimeWorldAndRealLikeOriginal(u);
                TryToStandRuntimeV411LikeOriginal(u, false, "NewMonsterPreciseSendToLink_finished_v433");
            }
            u.HasMoveTargetLikeOriginal = false;
            u.MovePathRealWaypointsLikeOriginal = null;
            u.MovePathIndexLikeOriginal = 0;
            u.MoveDeferredUntilNeutralStandLikeOriginal = false;
            if (u.Info != null) u.Info.C2NeutralPeasantUnitsV15SetMovingFlagLikeOriginal(u.PreciseBornPathLikeOriginal, u.PreciseBornPathLikeOriginal);
            if (face && u.Info != null && u.Info.RealDir != direction)
                BeginSingleStepRotUnitV352LikeOriginal(u, direction);
        }

        // ---- V427 moved from renderer: SetRuntimeValidatedDirectMoveDestinationRealLikeOriginal ----
        internal void SetRuntimeValidatedDirectMoveDestinationRealLikeOriginal(
            C2UnitOriginalRuntime u,
            float destRealX,
            float destRealY,
            float speedOriginalPixelsPerSecond,
            bool hasFinalFacingDir,
            byte finalFacingDir,
            bool preciseBornPath,
            string source)
        {
            if(u?.Info==null||u.State==C2UnitOriginalState.Death)return;
            if(preciseBornPath)
                C2OriginalOrderChainV352.SubmitBornExitV433LikeOriginal(u.Info,
                    new[]{new Vector2(destRealX,destRealY)},1,true);
            else
                C2OriginalOrderChainV352.SubmitTaskMoveV433LikeOriginal(u.Info,destRealX,destRealY,
                    speedOriginalPixelsPerSecond,hasFinalFacingDir,finalFacingDir,true,source??"task_precise_child");
        }

        internal void InstallValidatedOrderDestinationV433LikeOriginal(
            C2UnitOriginalRuntime u,float destRealX,float destRealY,float speedOriginalPixelsPerSecond,
            bool hasFinalFacingDir,byte finalFacingDir,bool preciseBornPath,string source)
        {
            SetRuntimeMoveDestinationRealInternalLikeOriginal(
                u,
                destRealX,
                destRealY,
                speedOriginalPixelsPerSecond,
                hasFinalFacingDir,
                finalFacingDir,
                false,
                preciseBornPath,
                source ?? "validated_direct_order");
        }

        internal void InstallRoadDestinationV434LikeOriginal(C2UnitOriginalRuntime u, float x, float y)
        {
            if (u?.Info == null || u.State == C2UnitOriginalState.Death) return;
            u.OriginalGoOnRoadLikeOriginal = true;
            u.PostureAfterMoveLikeOriginal = -1;
            u.MovePreservesCombatPostureV405BLikeOriginal = false;
            // NewState=0; TryToStand(OB,0), then DestX/Y. LocalNewState still
            // completes its existing transition through the animation handler.
            if (u.PostureWeaponTypeLikeOriginal >= 0)
            {
                u.PostureWeaponTypeLikeOriginal = -1;
                u.Info.NewStateV396LikeOriginal = 0;
                TryToStandRuntimeV411LikeOriginal(u, false, "GoOnRoad::NewState=0");
            }
            u.HasFinalFacingDirLikeOriginal = false;
            SetRuntimeMoveTargetOnlyLikeOriginal(u, x, y,
                C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                false, true);
            u.Info.C2NeutralPeasantUnitsV15SetMovingFlagLikeOriginal(true, false);
        }

        // ---- V427 moved from renderer: SetRuntimeMovePathRealLikeOriginal ----
        internal void SetRuntimeMovePathRealLikeOriginal(C2UnitOriginalRuntime u, Vector2[] pathReal, float speedOriginalPixelsPerSecond, bool hasFinalFacingDir, byte finalFacingDir, bool preciseBornPath, string source, bool finalAllowsUnitOverlapFinish = false, bool terrainPathValidated = false)
        {
            if (u == null) return;
            if (u.State == C2UnitOriginalState.Death) return;

            EnsureRuntimeRealFromInfoLikeOriginal(u);

            if (pathReal == null || pathReal.Length == 0)
                return;

            // V428: a path handed to the runtime is already owned by the original
            // movement chain. Do not run it through the old Unity LOCKPOINTS/A*
            // revalidator: that created a second pathfinder after CreatePath.
            u.PostureAfterMoveLikeOriginal = -1;
            u.MovePathRealWaypointsLikeOriginal = pathReal;
            u.MovePathIndexLikeOriginal = 0;
            u.HasFinalFacingDirLikeOriginal = hasFinalFacingDir;
            u.FinalFacingDirLikeOriginal = finalFacingDir;
            u.PreciseBornPathLikeOriginal = preciseBornPath;
            u.PreciseBornPathLastWaypointIndexLikeOriginal = preciseBornPath ? Mathf.Max(0, pathReal.Length - 1) : -1;
            u.MovePathFinalAllowsUnitOverlapFinishLikeOriginal = finalAllowsUnitOverlapFinish;
            // A blocked-finish retarget can itself produce an A* waypoint path.
            // Resetting the counter here made every such path the "first" retry
            // again, causing an endless FindUnitPosition/A* loop for hundreds of
            // units. Preserve its bounded retry counter across the replacement
            // path; only a genuinely new external order starts from zero.
            if (!string.Equals(source, "blocked_finish_find_unit_position", StringComparison.OrdinalIgnoreCase))
                u.BlockedFinishRetargetAttemptLikeOriginal = 0;

            if (u.Info != null)
                u.Info.C2NeutralPeasantUnitsV15SetMovingFlagLikeOriginal(true, preciseBornPath);

            // FIX12: Do NOT call SetRuntimeMoveDestinationRealInternalLikeOriginal here.
            // That helper is for single-target orders and deliberately clears
            // MovePathRealWaypointsLikeOriginal. BORN exit needs the whole chain:
            // BORNPOINTS[1..N] + optional post-born rally/scatter order.
            Vector2 first = pathReal[0];
            SetRuntimeMoveTargetOnlyLikeOriginal(u, first.x, first.y, speedOriginalPixelsPerSecond, finalAllowsUnitOverlapFinish && pathReal.Length == 1);

            if (LogBornExitAuditLikeOriginal && _bornExitAuditLogs < Mathf.Max(1, MaxBornExitAuditLogsLikeOriginal))
            {
                _bornExitAuditLogs++;
                Debug.Log(LogPrefix + " BORN_STOP path_set_preserved source='" + (source ?? "path_order") + "'" +
                          " pathCount=" + pathReal.Length.ToString(CultureInfo.InvariantCulture) +
                          " idx=0" +
                          " first=(" + first.x.ToString("F0", CultureInfo.InvariantCulture) + "," + first.y.ToString("F0", CultureInfo.InvariantCulture) + ")" +
                          " precise=" + preciseBornPath);
            }
        }

        // ---- V427 moved from renderer: SetRuntimeMoveDestinationRealInternalLikeOriginal ----
        private void SetRuntimeMoveDestinationRealInternalLikeOriginal(C2UnitOriginalRuntime u, float destRealX, float destRealY, float speedOriginalPixelsPerSecond, bool hasFinalFacingDir, byte finalFacingDir, bool allowBuildingPath, bool preciseBornPath, string source, bool targetAllowsUnitOverlapFinish = false)
        {
            if (u == null) return;
            if (u.State == C2UnitOriginalState.Death) return;

            EnsureRuntimeRealFromInfoLikeOriginal(u);
            u.PostureAfterMoveLikeOriginal = -1;

            // Normal CII movement has one owner: OneObject::CreatePath -> shared path queue.
            // Building/water/terrain routing is part of MFIELDS in C2MovementSystemV425, not
            // a separate Unity A*/LOCKPOINTS branch. Precise BORN clearance remains its
            // explicit OrderedUnlimitedMotion path and therefore bypasses CreatePath.
            if (allowBuildingPath && !preciseBornPath && u.Info != null)
            {
                C2OriginalMovementSystemV425LikeOriginal.AddPathRequestorV425LikeOriginal(
                    u.Info, destRealX, destRealY, speedOriginalPixelsPerSecond,
                    hasFinalFacingDir, finalFacingDir, source ?? "OneObject::CreatePath");
                u.HasFinalFacingDirLikeOriginal = hasFinalFacingDir;
                u.FinalFacingDirLikeOriginal = finalFacingDir;
                u.HasMoveTargetLikeOriginal = false;
                u.MovePathRealWaypointsLikeOriginal = null;
                u.MovePathIndexLikeOriginal = 0;
                u.Info.C2NeutralPeasantUnitsV15SetMovingFlagLikeOriginal(true, false);
                return;
            }

            // V428: no secondary Unity LOCKPOINTS/A* fallback lives here.
            // Gameplay units above enter OneObject::CreatePath; no-Info/editor
            // runtimes fall through to the explicit direct target only.

            u.MovePathRealWaypointsLikeOriginal = null;
            u.MovePathIndexLikeOriginal = 0;
            u.PreciseBornPathLastWaypointIndexLikeOriginal = preciseBornPath ? 0 : -1;
            u.MovePathFinalAllowsUnitOverlapFinishLikeOriginal = false;
            u.HasFinalFacingDirLikeOriginal = hasFinalFacingDir;
            u.FinalFacingDirLikeOriginal = finalFacingDir;
            u.PreciseBornPathLikeOriginal = preciseBornPath;
            if (!string.Equals(source, "blocked_finish_find_unit_position", StringComparison.OrdinalIgnoreCase))
                u.BlockedFinishRetargetAttemptLikeOriginal = 0;

            SetRuntimeMoveTargetOnlyLikeOriginal(u, destRealX, destRealY, speedOriginalPixelsPerSecond, targetAllowsUnitOverlapFinish);

            if (u.Info != null)
                u.Info.C2NeutralPeasantUnitsV15SetMovingFlagLikeOriginal(true, preciseBornPath);
        }

        // ---- V427 moved from renderer: SetRuntimeMoveTargetOnlyLikeOriginal ----
        internal void SetRuntimeMoveTargetOnlyLikeOriginal(C2UnitOriginalRuntime u, float destRealX, float destRealY, float speedOriginalPixelsPerSecond, bool targetAllowsUnitOverlapFinish = false, bool pathRefreshV433 = false)
        {
            if (u == null) return;

            // path.cpp::ReallyCreatePath only writes DestX/DestY on a reused
            // path. Do not restart gait, project the same destination, or cancel
            // a pending RotUnit every 40 ms. A new order still performs full setup.
            if (pathRefreshV433 && u.HasMoveTargetLikeOriginal &&
                u.State == C2UnitOriginalState.Motion &&
                u.MoveTargetRealXLikeOriginal == destRealX && u.MoveTargetRealYLikeOriginal == destRealY)
            {
                u.MoveSpeedOriginalPixelsPerSecondLikeOriginal = ResolveRuntimeMoveSpeedOriginalPixelsPerSecondLikeOriginal(u, speedOriginalPixelsPerSecond);
                u.MoveTargetAllowsUnitOverlapFinishLikeOriginal = targetAllowsUnitOverlapFinish;
                return;
            }
            u.MoveTargetRealXLikeOriginal = destRealX;
            u.MoveTargetRealYLikeOriginal = destRealY;
            // A newly installed SmartSend/PreciseSend destination replaces any transient
            // RotUnit(Type=1) generated by the previous movement direction.
            if (!pathRefreshV433)
            {
                u.SingleStepRotateAtPlaceActiveV352LikeOriginal = false;
                u.FinalRotUnitActiveV352LikeOriginal = false;
            }
            u.MoveSpeedOriginalPixelsPerSecondLikeOriginal = ResolveRuntimeMoveSpeedOriginalPixelsPerSecondLikeOriginal(u, speedOriginalPixelsPerSecond);
            u.MoveTargetAllowsUnitOverlapFinishLikeOriginal = targetAllowsUnitOverlapFinish;

            AnimModel motionAnim = null;
            int motion = ResolveRuntimeMotionAnimationIndexV223LikeOriginal(u);
            if (motion >= 0) motionAnim = u.Md.Animations[motion];

            u.MoveRInFrameLikeOriginal = GetMotionRInFrameLikeOriginal(u, motionAnim);

            float dx = destRealX - u.RuntimeRealXLikeOriginal;
            float dy = destRealY - u.RuntimeRealYLikeOriginal;
            if ((dx * dx + dy * dy) > 0.0001f)
            {
                if (u.OriginalComplexObjectV430LikeOriginal != null)
                {
                    u.MoveDeferredUntilNeutralStandLikeOriginal = false;
                    u.HasMoveTargetLikeOriginal = true;
                }
                else if (u.State == C2UnitOriginalState.Transition)
                {
                    u.HasMoveTargetLikeOriginal = false;
                    u.MoveDeferredUntilNeutralStandLikeOriginal = true;
                }
                else if (u.State != C2UnitOriginalState.Motion &&
                         CurrentAnim(u) != null &&
                         !u.FrameFinishedLikeOriginal &&
                         !CurrentAnim(u).CanBeBroken &&
                         !CurrentAnim(u).MoveBreak)
                {
                    // MotionHandlerForSingleStepObjects waits for an ordinary
                    // animation to finish. BREAKANIMATION allows interruption
                    // on any state change; MOVEBREAK allows it when DestX is set.
                    u.HasMoveTargetLikeOriginal = false;
                    u.MoveDeferredUntilNeutralStandLikeOriginal = true;
                }
                else if ((u.PostureWeaponTypeLikeOriginal >= 0 ||
                          u.LocalPostureWeaponTypeV411LikeOriginal >= 0) &&
                         !u.MovePreservesCombatPostureV405BLikeOriginal)
                {
                    // Movement changes NewState to neutral; LocalNewState must
                    // finish UATTACK first when that clip exists.
                    u.PostureWeaponTypeLikeOriginal = -1;
                    u.HasMoveTargetLikeOriginal = false;
                    u.MoveDeferredUntilNeutralStandLikeOriginal = true;
                    TryToStandRuntimeV411LikeOriginal(u, false, "move_wait_posture_off_v411");
                    if (u.State != C2UnitOriginalState.Transition)
                    {
                        u.MoveDeferredUntilNeutralStandLikeOriginal = false;
                        StartRuntimeMoveTargetNowLikeOriginal(u, "move_posture_off_no_transition_v411");
                    }
                }
                else
                {
                    StartRuntimeMoveTargetNowLikeOriginal(u, "move_order_received");
                }
            }

            C2BattleTerrainMode mode = u.Info != null ? u.Info.OwnerMode : _battle;
            if (mode != null)
            {
                Vector3 world = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(destRealX / 16.0f, destRealY / 16.0f);
                u.MoveTargetWorldLikeOriginal = world;
            }
        }

        // ---- V427 moved from renderer: StartRuntimeMoveTargetNowLikeOriginal ----
        private void StartRuntimeMoveTargetNowLikeOriginal(C2UnitOriginalRuntime u, string reason)
        {
            if (u == null || u.Md == null || u.State == C2UnitOriginalState.Death)
                return;

            float dx = u.MoveTargetRealXLikeOriginal - u.RuntimeRealXLikeOriginal;
            float dy = u.MoveTargetRealYLikeOriginal - u.RuntimeRealYLikeOriginal;
            u.HasMoveTargetLikeOriginal = (dx * dx + dy * dy) > 0.0001f;
            if (!u.HasMoveTargetLikeOriginal)
                return;

            // MotionStyle-specific handlers own their heading. Only the legacy
            // 0/1/3/4 LocalOrder path may install the immediate managed facing here.
            int motionStyleV430 = MotionStyleCodeV411LikeOriginal(u.Md.MotionStyle);
            if (motionStyleV430 == 0 || motionStyleV430 == 1 ||
                motionStyleV430 == 3 || motionStyleV430 == 4)
                SetRuntimeFacingLikeOriginal(u, DirectionFromRealDeltaLikeOriginal(dx, dy));
            int motion = ResolveRuntimeMotionAnimationIndexV223LikeOriginal(u);
            if (motion >= 0 && u.State != C2UnitOriginalState.Motion)
            {
                SelectAnimationStateLikeOriginal(
                    u,
                    C2UnitOriginalState.Motion,
                    motion,
                    true,
                    reason ?? "move_order_received");
                ApplyUnitFrameLikeOriginal(u, reason ?? "move_order_received");
            }
        }

        // ---- V427 moved from renderer: AdvanceRuntimeMoveWaypointLikeOriginal ----
        private bool AdvanceRuntimeMoveWaypointLikeOriginal(C2UnitOriginalRuntime u)
        {
            if (u == null || u.MovePathRealWaypointsLikeOriginal == null)
                return false;

            // C2 NewMon.cpp::ClearUnlimitedLink first gets an obstructed newborn to
            // a free bar, then clears UnlimitedMotion. This is a separate order after
            // the MD exit chain, not a replacement for BORNPOINTS.
            if (u.PreciseBornPathLikeOriginal &&
                u.MovePathIndexLikeOriginal == u.PreciseBornPathLastWaypointIndexLikeOriginal &&
                C2BuildingRuntimeInfoV247LikeOriginal.IsBlockedForUnitRealV247LikeOriginal(
                    u.RuntimeRealXLikeOriginal, u.RuntimeRealYLikeOriginal, ResolveRuntimeUnitRadiusCellsLikeOriginal(u)))
            {
                float freeX, freeY;
                bool free = C2BuildingRuntimeInfoV247LikeOriginal.TryFindNearestFreeRealV247LikeOriginal(
                    u.RuntimeRealXLikeOriginal, u.RuntimeRealYLikeOriginal, out freeX, out freeY, 30, 7);
                if (!free) free = C2BuildingRuntimeInfoV247LikeOriginal.TryFindNearestFreeRealV247LikeOriginal(
                    u.RuntimeRealXLikeOriginal, u.RuntimeRealYLikeOriginal, out freeX, out freeY, 96,
                    ResolveRuntimeUnitRadiusCellsLikeOriginal(u));
                if (free)
                {
                    int at = u.MovePathIndexLikeOriginal + 1;
                    Vector2[] extended = new Vector2[u.MovePathRealWaypointsLikeOriginal.Length + 1];
                    Array.Copy(u.MovePathRealWaypointsLikeOriginal, 0, extended, 0, at);
                    extended[at] = new Vector2(freeX, freeY);
                    Array.Copy(u.MovePathRealWaypointsLikeOriginal, at, extended, at + 1,
                        u.MovePathRealWaypointsLikeOriginal.Length - at);
                    u.MovePathRealWaypointsLikeOriginal = extended;
                    u.PreciseBornPathLastWaypointIndexLikeOriginal = at;
                }
                else
                {
                    // An enclosed/invalid map must not leave a permanent uncommandable unit.
                    u.PreciseBornPathLikeOriginal = false;
                    u.PreciseBornPathLastWaypointIndexLikeOriginal = -1;
                    if (u.Info != null) u.Info.C2NeutralPeasantUnitsV15SetMovingFlagLikeOriginal(true, false);
                }
            }
            u.MovePathIndexLikeOriginal++;
            if (u.MovePathIndexLikeOriginal < 0 || u.MovePathIndexLikeOriginal >= u.MovePathRealWaypointsLikeOriginal.Length)
            {
                EmitBornExitAuditLikeOriginal(u, "advance_end", "pathEnded=1");
                EmitBornStopAuditLikeOriginal(u, "advance_end", "pathEnded=1 before clearing path state");
                u.MovePathRealWaypointsLikeOriginal = null;
                u.MovePathIndexLikeOriginal = 0;
                u.PreciseBornPathLikeOriginal = false;
                u.PreciseBornPathLastWaypointIndexLikeOriginal = -1;
                u.MovePathFinalAllowsUnitOverlapFinishLikeOriginal = false;
                u.MoveTargetAllowsUnitOverlapFinishLikeOriginal = false;
                return false;
            }

            Vector2 next = u.MovePathRealWaypointsLikeOriginal[u.MovePathIndexLikeOriginal];

            // FIX6: OrderedUnlimitedMotion is only for the original BORNPOINTS exit chain.
            // Once BORNPOINTS[1..N] are done, any appended rally/normal target must return
            // to normal building CheckBar routing.  Otherwise produced units can either keep
            // ghost-walking through LOCKPOINTS or fail to escape cleanly when the path changes.
            if (u.PreciseBornPathLikeOriginal &&
                u.PreciseBornPathLastWaypointIndexLikeOriginal >= 0 &&
                u.MovePathIndexLikeOriginal > u.PreciseBornPathLastWaypointIndexLikeOriginal)
            {
                u.MovePathRealWaypointsLikeOriginal = null;
                u.MovePathIndexLikeOriginal = 0;
                u.PreciseBornPathLikeOriginal = false;
                u.PreciseBornPathLastWaypointIndexLikeOriginal = -1;
                EmitBornExitAuditLikeOriginal(u, "post_born_route", "next=(" + next.x.ToString("0.0", CultureInfo.InvariantCulture) + "," + next.y.ToString("0.0", CultureInfo.InvariantCulture) + ") returningToCheckBar=1");
                SetRuntimeMoveDestinationRealInternalLikeOriginal(u, next.x, next.y, u.MoveSpeedOriginalPixelsPerSecondLikeOriginal, u.HasFinalFacingDirLikeOriginal, u.FinalFacingDirLikeOriginal, true, false, "post_born_rally_checkbar_route", false);
                return true;
            }

            EmitBornExitAuditLikeOriginal(u, "advance_next", "next=(" + next.x.ToString("0.0", CultureInfo.InvariantCulture) + "," + next.y.ToString("0.0", CultureInfo.InvariantCulture) + ")");
            bool finalAllowsUnitOverlapFinish = u.MovePathFinalAllowsUnitOverlapFinishLikeOriginal &&
                u.MovePathRealWaypointsLikeOriginal != null &&
                u.MovePathIndexLikeOriginal == u.MovePathRealWaypointsLikeOriginal.Length - 1;
            SetRuntimeMoveTargetOnlyLikeOriginal(u, next.x, next.y, u.MoveSpeedOriginalPixelsPerSecondLikeOriginal, finalAllowsUnitOverlapFinish);
            if (u.Info != null)
                u.Info.C2NeutralPeasantUnitsV15SetMovingFlagLikeOriginal(true, u.PreciseBornPathLikeOriginal);
            return true;
        }

        // ---- V427 moved from renderer: C2UnitOriginalRuntimeUseRawBornExitPathLikeOriginal ----
        private static Vector2[] C2UnitOriginalRuntimeUseRawBornExitPathLikeOriginal(
            Vector2[] rawBornPath,
            out string audit)
        {
            // Original Build.cpp uses BORNPOINTS exactly:
            // BORNPOINTS[0] = spawn; BORNPOINTS[1..N] = precise exit.
            // No synthetic clearance point and no segment stretching.
            if (rawBornPath == null || rawBornPath.Length == 0)
            {
                audit = "original_raw_bornpoints rawCount=0 adjustedCount=0 stretch=disabled clearance=disabled";
                return rawBornPath;
            }

            Vector2[] points = new Vector2[rawBornPath.Length];
            Array.Copy(rawBornPath, points, rawBornPath.Length);

            audit = "original_raw_bornpoints" +
                    " rawCount=" + rawBornPath.Length.ToString(CultureInfo.InvariantCulture) +
                    " adjustedCount=" + points.Length.ToString(CultureInfo.InvariantCulture) +
                    " stretch=disabled" +
                    " clearance=disabled rallyMarker=DstX_DstY_only";
            return points;
        }

        // ---- V427 moved from renderer: C2UnitOriginalRuntimeAppendBornExitClearancePointV10LikeOriginal ----
        private static void C2UnitOriginalRuntimeAppendBornExitClearancePointV10LikeOriginal(
            List<Vector2> realBornPoints,
            out string audit)
        {
            audit = "clearance_skip";
            if (realBornPoints == null || realBornPoints.Count < 2)
                return;

            Vector2 prev = realBornPoints[realBornPoints.Count - 2];
            Vector2 last = realBornPoints[realBornPoints.Count - 1];
            Vector2 dir = last - prev;
            float len = dir.magnitude;
            if (len < 0.001f)
            {
                audit = "clearance_zero_dir";
                return;
            }
            dir /= len;

            // Ported from old working 13_05 project:
            //   forcedExtra = max(48, len * 0.35)
            //   p = last + dir * forcedExtra
            //   while near LOCKPOINTS: p += dir * 16
            //
            // Here the path is already in original Real coordinates, so:
            //   48 local pixels -> 48*16 real units
            //   16 local pixels -> 16*16 real units
            float forcedExtraReal = Mathf.Max(48.0f * 16.0f, len * 0.35f);
            Vector2 p = last + dir * forcedExtraReal;

            int lockPushSteps = 0;
            while (C2UnitOriginalRuntimeBornRealPointNearLockV10LikeOriginal(p, 2) && lockPushSteps < 16)
            {
                p += dir * (16.0f * 16.0f);
                lockPushSteps++;
            }

            if ((p - last).sqrMagnitude < 64.0f)
            {
                audit = "clearance_too_small";
                return;
            }

            realBornPoints.Add(p);
            audit = "clearance_forced extraReal=" + Mathf.RoundToInt(forcedExtraReal).ToString(CultureInfo.InvariantCulture) +
                    " lockPush=" + lockPushSteps.ToString(CultureInfo.InvariantCulture) +
                    " lastReal=" + Mathf.RoundToInt(last.x).ToString(CultureInfo.InvariantCulture) + "/" + Mathf.RoundToInt(last.y).ToString(CultureInfo.InvariantCulture) +
                    " clearanceReal=" + Mathf.RoundToInt(p.x).ToString(CultureInfo.InvariantCulture) + "/" + Mathf.RoundToInt(p.y).ToString(CultureInfo.InvariantCulture);
        }

        // ---- V427 moved from renderer: C2UnitOriginalRuntimeBornRealPointNearLockV10LikeOriginal ----
        private static bool C2UnitOriginalRuntimeBornRealPointNearLockV10LikeOriginal(
            Vector2 realPoint,
            int radiusCells)
        {
            return C2BattleTerrainMode.C2BuildingMotionFieldV1IsBlockedForUnitRealLikeOriginal(
                realPoint.x,
                realPoint.y,
                Mathf.Max(0, radiusCells));
        }

        // ---- V427 moved from renderer: FormatRealPathLikeOriginal ----
        private static string FormatRealPathLikeOriginal(Vector2[] path)
        {
            if (path == null) return "<null>";
            if (path.Length == 0) return "<empty>";
            var sb = new System.Text.StringBuilder(path.Length * 32);
            for (int i = 0; i < path.Length; i++)
            {
                if (i != 0) sb.Append(" -> ");
                sb.Append(i.ToString(CultureInfo.InvariantCulture)).Append(":real(")
                  .Append(path[i].x.ToString("0", CultureInfo.InvariantCulture)).Append(",")
                  .Append(path[i].y.ToString("0", CultureInfo.InvariantCulture)).Append(") pix(")
                  .Append((path[i].x / 16.0f).ToString("0.0", CultureInfo.InvariantCulture)).Append(",")
                  .Append((path[i].y / 16.0f).ToString("0.0", CultureInfo.InvariantCulture)).Append(")");
            }
            return sb.ToString();
        }

        // ---- V427 moved from renderer: EmitBornExitAuditLikeOriginal ----
        private void EmitBornExitAuditLikeOriginal(C2UnitOriginalRuntime u, string eventName, string details)
        {
            if (!LogBornExitAuditLikeOriginal) return;
            if (_bornExitAuditLogs >= Mathf.Max(1, MaxBornExitAuditLogsLikeOriginal)) return;
            _bornExitAuditLogs++;

            string unit = u != null && u.Probe != null ? (u.Probe.MonsterId ?? string.Empty) : string.Empty;
            string md = u != null && u.Md != null ? (u.Md.Name ?? string.Empty) : string.Empty;
            string idx = u != null ? u.MovePathIndexLikeOriginal.ToString(CultureInfo.InvariantCulture) : "-";
            string pathCount = (u != null && u.MovePathRealWaypointsLikeOriginal != null) ? u.MovePathRealWaypointsLikeOriginal.Length.ToString(CultureInfo.InvariantCulture) : "-";
            string lastPrecise = u != null ? u.PreciseBornPathLastWaypointIndexLikeOriginal.ToString(CultureInfo.InvariantCulture) : "-";
            string precise = u != null ? u.PreciseBornPathLikeOriginal.ToString() : "-";
            string real = u != null ? "(" + u.RuntimeRealXLikeOriginal.ToString("0.0", CultureInfo.InvariantCulture) + "," + u.RuntimeRealYLikeOriginal.ToString("0.0", CultureInfo.InvariantCulture) + ")" : "(-)";
            string target = u != null ? "(" + u.MoveTargetRealXLikeOriginal.ToString("0.0", CultureInfo.InvariantCulture) + "," + u.MoveTargetRealYLikeOriginal.ToString("0.0", CultureInfo.InvariantCulture) + ")" : "(-)";
            string dist = "-";
            if (u != null)
            {
                float dx = u.MoveTargetRealXLikeOriginal - u.RuntimeRealXLikeOriginal;
                float dy = u.MoveTargetRealYLikeOriginal - u.RuntimeRealYLikeOriginal;
                dist = Mathf.Sqrt(dx * dx + dy * dy).ToString("0.0", CultureInfo.InvariantCulture);
            }

            Debug.Log(LogPrefix + " BORN_EXIT_AUDIT event='" + (eventName ?? string.Empty) + "'" +
                      " unit='" + unit + "' md='" + md + "'" +
                      " idx=" + idx + " pathCount=" + pathCount +
                      " precise=" + precise + " lastPrecise=" + lastPrecise +
                      " real=" + real + " target=" + target + " distReal=" + dist +
                      " rule=original_Build.cpp_SetOrderedUnlimitedMotion_then_NewMonsterPreciseSendTo_BORNPOINTS_1_to_N " +
                      (details ?? string.Empty));
        }

        // ---- V427 moved from renderer: EmitBornStopAuditLikeOriginal ----
        private void EmitBornStopAuditLikeOriginal(C2UnitOriginalRuntime u, string reason, string details)
        {
            if (!LogBornExitAuditLikeOriginal) return;
            if (_bornExitAuditLogs >= Mathf.Max(1, MaxBornExitAuditLogsLikeOriginal)) return;
            _bornExitAuditLogs++;

            string unit = u != null && u.Probe != null ? (u.Probe.MonsterId ?? string.Empty) : string.Empty;
            string md = u != null && u.Md != null ? (u.Md.Name ?? string.Empty) : string.Empty;
            string real = u != null ? "(" + u.RuntimeRealXLikeOriginal.ToString("0.0", CultureInfo.InvariantCulture) + "," + u.RuntimeRealYLikeOriginal.ToString("0.0", CultureInfo.InvariantCulture) + ")" : "(-)";
            string target = u != null ? "(" + u.MoveTargetRealXLikeOriginal.ToString("0.0", CultureInfo.InvariantCulture) + "," + u.MoveTargetRealYLikeOriginal.ToString("0.0", CultureInfo.InvariantCulture) + ")" : "(-)";
            string expectedFinal = (u != null && u.BornExpectedFinalValidLikeOriginal) ? "(" + u.BornExpectedFinalRealLikeOriginal.x.ToString("0.0", CultureInfo.InvariantCulture) + "," + u.BornExpectedFinalRealLikeOriginal.y.ToString("0.0", CultureInfo.InvariantCulture) + ")" : "(-)";
            string rawFinal = (u != null && u.BornRawFinalValidLikeOriginal) ? "(" + u.BornRawFinalRealLikeOriginal.x.ToString("0.0", CultureInfo.InvariantCulture) + "," + u.BornRawFinalRealLikeOriginal.y.ToString("0.0", CultureInfo.InvariantCulture) + ")" : "(-)";
            string clearance = (u != null && u.BornClearancePointValidLikeOriginal) ? "(" + u.BornClearancePointRealLikeOriginal.x.ToString("0.0", CultureInfo.InvariantCulture) + "," + u.BornClearancePointRealLikeOriginal.y.ToString("0.0", CultureInfo.InvariantCulture) + ")" : "(-)";
            string idx = u != null ? u.MovePathIndexLikeOriginal.ToString(CultureInfo.InvariantCulture) : "-";
            string pathCount = (u != null && u.MovePathRealWaypointsLikeOriginal != null) ? u.MovePathRealWaypointsLikeOriginal.Length.ToString(CultureInfo.InvariantCulture) : "-";
            string precise = u != null ? u.PreciseBornPathLikeOriginal.ToString() : "-";
            string lastPrecise = u != null ? u.PreciseBornPathLastWaypointIndexLikeOriginal.ToString(CultureInfo.InvariantCulture) : "-";
            string distToExpected = "-";
            if (u != null && u.BornExpectedFinalValidLikeOriginal)
            {
                float dx = u.BornExpectedFinalRealLikeOriginal.x - u.RuntimeRealXLikeOriginal;
                float dy = u.BornExpectedFinalRealLikeOriginal.y - u.RuntimeRealYLikeOriginal;
                distToExpected = Mathf.Sqrt(dx * dx + dy * dy).ToString("0.0", CultureInfo.InvariantCulture);
            }

            Debug.Log(LogPrefix + " BORN_STOP" +
                      " reason='" + (reason ?? string.Empty) + "'" +
                      " unit='" + unit + "' md='" + md + "'" +
                      " real=" + real +
                      " target=" + target +
                      " rawFinalBorn=" + rawFinal +
                      " expectedFinalBorn=" + expectedFinal +
                      " clearancePoint=" + clearance +
                      " distToExpected=" + distToExpected +
                      " idx=" + idx + " pathCount=" + pathCount +
                      " precise=" + precise + " lastPrecise=" + lastPrecise +
                      " " + (details ?? string.Empty));
        }

        // ---- V427 moved from renderer: SetRuntimeFacingLikeOriginal ----
        internal void SetRuntimeFacingLikeOriginal(C2UnitOriginalRuntime u, byte realDir)
        {
            if (u == null) return;
            if (u.RealDirPrecise != realDir)
                u.FrameUploadPendingLikeOriginal = true;
            u.RealDirPrecise = realDir & 255;
            u.OriginalRealDirPrecise256LikeOriginal = (realDir & 255) << 8;
            // CII keeps OctantInfo across changes to RealDir. DrawSpriteUnit owns
            // its hysteresis; resetting it here bypassed that state on every step.
            if (u.Info != null)
            {
                u.Info.RealDir = realDir;
                u.Info.GraphDir = realDir;
                u.Info.RealDirPrecise = u.RealDirPrecise;
                u.Info.OctantInfo = (byte)u.OctantInfo;
            }
        }

        // ---- V427 moved from renderer: UpdateRuntimeRealFromWorldLikeOriginal ----
        private void UpdateRuntimeRealFromWorldLikeOriginal(C2UnitOriginalRuntime u)
        {
            if (u == null || !u.ActiveLikeOriginal) return;
            C2BattleTerrainMode mode = u.Info != null ? u.Info.OwnerMode : _battle;
            if (mode == null) return;
            float px;
            float py;
            if (!mode.C2NeutralPeasantUnitsV2WorldToOriginalPixelV15LikeOriginal(u.WorldPosition, out px, out py))
                return;
            int realX = Mathf.RoundToInt(px * 16.0f);
            int realY = Mathf.RoundToInt(py * 16.0f);
            u.RuntimeRealXLikeOriginal = realX;
            u.RuntimeRealYLikeOriginal = realY;
            if (u.Info != null)
            {
                u.Info.RealX = realX;
                u.Info.RealY = realY;
                u.Info.RealXFloat = realX;
                u.Info.RealYFloat = realY;
                C2LiveUnitCellIndex.PositionChanged(u.Info);
            }
            if (u.Probe != null)
            {
                u.Probe.RealX = realX;
                u.Probe.RealY = realY;
            }
        }

        // ---- V427 moved from renderer: EnsureRuntimeRealFromInfoLikeOriginal ----
        private void EnsureRuntimeRealFromInfoLikeOriginal(C2UnitOriginalRuntime u)
        {
            if (u == null) return;
            if (Mathf.Abs(u.RuntimeRealXLikeOriginal) > 0.001f || Mathf.Abs(u.RuntimeRealYLikeOriginal) > 0.001f) return;

            if (u.Info != null)
            {
                u.RuntimeRealXLikeOriginal = Mathf.Abs(u.Info.RealXFloat) > 0.001f ? u.Info.RealXFloat : u.Info.RealX;
                u.RuntimeRealYLikeOriginal = Mathf.Abs(u.Info.RealYFloat) > 0.001f ? u.Info.RealYFloat : u.Info.RealY;
                return;
            }

            if (u.Probe != null)
            {
                u.RuntimeRealXLikeOriginal = u.Probe.RealX;
                u.RuntimeRealYLikeOriginal = u.Probe.RealY;
            }
        }

        // ---- V427 moved from renderer: UpdateRuntimeWorldAndRealLikeOriginal ----
        private void UpdateRuntimeWorldAndRealLikeOriginal(C2UnitOriginalRuntime u)
        {
            using (C2FrameCostProbe.Measure(C2FrameCostProbe.Phase.MotionWorld))
            {
            if (u == null) return;

            if (u.Info != null)
            {
                u.Info.RealXFloat = u.RuntimeRealXLikeOriginal;
                u.Info.RealYFloat = u.RuntimeRealYLikeOriginal;
                u.Info.RealX = Mathf.RoundToInt(u.RuntimeRealXLikeOriginal);
                u.Info.RealY = Mathf.RoundToInt(u.RuntimeRealYLikeOriginal);
                C2LiveUnitCellIndex.PositionChanged(u.Info);
            }

            if (u.Probe != null)
            {
                u.Probe.RealX = Mathf.RoundToInt(u.RuntimeRealXLikeOriginal);
                u.Probe.RealY = Mathf.RoundToInt(u.RuntimeRealYLikeOriginal);
            }

            C2BattleTerrainMode mode = u.Info != null ? u.Info.OwnerMode : _battle;
            if (mode != null)
            {
                Vector3 w = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(u.RuntimeRealXLikeOriginal / 16.0f, u.RuntimeRealYLikeOriginal / 16.0f);
                u.WorldPosition = w;
                if (u.Root != null)
                    u.Root.transform.position = w;
            }
            }
        }

        // ---- V427 moved from renderer: UpdateRuntimeWorldAndRealContinuousLikeOriginal ----
        private void UpdateRuntimeWorldAndRealContinuousLikeOriginal(C2UnitOriginalRuntime u, float beforeRealX, float beforeRealY)
        {
            // Keep the legacy setting/call sites compatible, but derive position from
            // current RealX/Y exactly as placement and the preview do. Accumulating a
            // separate unscaled world delta preserved each unit's old column offset
            // and drifted at zoom scales whose backing step is not 32.
            UpdateRuntimeWorldAndRealLikeOriginal(u);
        }

        // ---- V427 moved from renderer: TryAdvanceRuntimeRealWithOriginalMotionFieldLikeOriginal ----
        private bool TryAdvanceRuntimeRealWithOriginalMotionFieldLikeOriginal(C2UnitOriginalRuntime u, float nx, float ny, float stepReal, out float beforeRealX, out float beforeRealY)
        {
            using (C2FrameCostProbe.Measure(C2FrameCostProbe.Phase.MotionTerrain))
            {
                beforeRealX = u != null ? u.RuntimeRealXLikeOriginal : 0.0f;
                beforeRealY = u != null ? u.RuntimeRealYLikeOriginal : 0.0f;
                if (u == null) return false;

                if (!UseOriginalMotionFieldPerStepBlockLikeOriginal || u.Info == null)
                {
                    u.RuntimeRealXLikeOriginal += nx * stepReal;
                    u.RuntimeRealYLikeOriginal += ny * stepReal;
                    u.TotalPathLikeOriginal += stepReal;
                    return true;
                }

                if (u.PreciseBornPathLikeOriginal)
                {
                    float nextX = beforeRealX + nx * stepReal;
                    float nextY = beforeRealY + ny * stepReal;
                    if (!CanPreciseBornUnitAdvanceWithDoorSpacingLikeOriginal(u, nextX, nextY))
                        return false;
                    u.RuntimeRealXLikeOriginal = nextX;
                    u.RuntimeRealYLikeOriginal = nextY;
                    u.TotalPathLikeOriginal += stepReal;
                    return true;
                }

                int dx0 = Mathf.RoundToInt(nx * stepReal);
                int dy0 = Mathf.RoundToInt(ny * stepReal);
                if (dx0 == 0 && dy0 == 0) return true;
                int committedDx, committedDy;
                if (!C2OriginalMovementSystemV425LikeOriginal.TryNewSheepMotionStepV425LikeOriginal(
                        u.Info, u, dx0, dy0, out committedDx, out committedDy))
                    return false;

                u.RuntimeRealXLikeOriginal = beforeRealX + committedDx;
                u.RuntimeRealYLikeOriginal = beforeRealY + committedDy;
                u.TotalPathLikeOriginal += C2OriginalMovementMathV352.Norma(committedDx, committedDy);
                return true;
            }
        }

        // ---- V427 moved from renderer: C2BuildingMotionFieldV1BlockedForTurnLikeOriginal ----
        private static bool C2BuildingMotionFieldV1BlockedForTurnLikeOriginal(C2UnitOriginalRuntime u)
        {
            // NewMon.cpp: HaveRotAnm is false inside MFIELDS.CheckBar(x-1,y-1,3,3).
            if (u == null || u.Info == null) return true;
            int lx = C2OriginalMovementSystemV425LikeOriginal.ResolveLxLikeOriginal(u.Info);
            int x = (Mathf.RoundToInt(u.RuntimeRealXLikeOriginal) - (lx << 7)) >> 8;
            int y = (Mathf.RoundToInt(u.RuntimeRealYLikeOriginal) - (lx << 7)) >> 8;
            byte lockType = C2OriginalMovementSystemV425LikeOriginal.ResolveLockTypeV425LikeOriginal(u.Info);
            return C2OriginalMovementSystemV425LikeOriginal.CheckBarV425LikeOriginal(x - 1, y - 1, 3, 3, lockType);
        }

        // ---- V430: Mechanics.cpp complex-object descriptor/runtime support ----
        private static readonly Dictionary<string, Dictionary<string, C2ComplexUnitDescV430LikeOriginal>>
            ComplexUnitsByObjectsPathV430LikeOriginal =
                new Dictionary<string, Dictionary<string, C2ComplexUnitDescV430LikeOriginal>>(StringComparer.OrdinalIgnoreCase);

        private static string CleanComplexDataLineV430LikeOriginal(string raw)
        {
            string s = (raw ?? string.Empty).Trim();
            // Mechanics.cpp Objects.dat reader treats every line beginning with '/'
            // as disabled/commented data (there are real '/#ANIM' records in retail).
            if (s.Length == 0 || s[0] == '/') return string.Empty;
            int c = s.IndexOf("//", StringComparison.Ordinal);
            if (c >= 0) s = s.Substring(0, c).Trim();
            return s;
        }

        private static string NextComplexDataLineV430LikeOriginal(string[] lines, ref int index)
        {
            while (lines != null && index < lines.Length)
            {
                string s = CleanComplexDataLineV430LikeOriginal(lines[index++]);
                if (s.Length != 0) return s;
            }
            return string.Empty;
        }

        private static int ComplexStageIndexV430LikeOriginal(string id)
        {
            string s = id ?? string.Empty;
            int idx = -1;
            if (s.IndexOf("$STAND_", StringComparison.OrdinalIgnoreCase) >= 0) idx = 0;
            if (s.IndexOf("$MOTION_", StringComparison.OrdinalIgnoreCase) >= 0) idx = 4;
            if (s.IndexOf("$ATTACK_", StringComparison.OrdinalIgnoreCase) >= 0) idx = 8;
            if (s.IndexOf("$RATTACK_", StringComparison.OrdinalIgnoreCase) >= 0) idx = 12;
            if (s.IndexOf("$RATTACK1_", StringComparison.OrdinalIgnoreCase) >= 0) idx = 16;
            if (s.IndexOf("$RATTACK2_", StringComparison.OrdinalIgnoreCase) >= 0) idx = 20;
            if (idx < 0) return -1;
            // Preserve Mechanics.cpp::GetStageIndex ordering literally.
            if (s.IndexOf("_HEAD", StringComparison.OrdinalIgnoreCase) >= 0) idx += 1;
            else if (s.IndexOf("_TALE", StringComparison.OrdinalIgnoreCase) >= 0) idx += 2;
            else if (s.IndexOf("_HEAD&TALE", StringComparison.OrdinalIgnoreCase) >= 0) idx += 3;
            else if (s.IndexOf("_ALONE", StringComparison.OrdinalIgnoreCase) < 0) return -1;
            return idx;
        }

        private static Dictionary<string, C2ComplexUnitDescV430LikeOriginal>
            ParseComplexObjectsV430LikeOriginal(string objectsPath)
        {
            var result = new Dictionary<string, C2ComplexUnitDescV430LikeOriginal>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(objectsPath) || !System.IO.File.Exists(objectsPath)) return result;

            string[] lines = System.IO.File.ReadAllLines(objectsPath, System.Text.Encoding.Default);
            var quants = new Dictionary<string, C2ComplexQuantDescV430LikeOriginal>(StringComparer.OrdinalIgnoreCase);

            // Keep the complete source animation descriptor: the movement parser
            // previously retained AddHeight alone, losing wheels, crew and recoil.
            var animationsV437 = ParseComplexAnimationsV437LikeOriginal(lines);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = CleanComplexDataLineV430LikeOriginal(lines[i]);
                if (line.Length == 0) continue;
                string[] t = SplitTokens(line);
                if (t.Length < 1) continue;

                if (string.Equals(t[0], "#MQUANT", StringComparison.OrdinalIgnoreCase) && t.Length >= 5)
                {
                    int x1, x2, directiveCount;
                    if (!int.TryParse(t[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out x1) ||
                        !int.TryParse(t[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out x2) ||
                        !int.TryParse(t[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out directiveCount))
                        continue;

                    var q = new C2ComplexQuantDescV430LikeOriginal { Id = t[1], X1 = x1, X2 = x2 };
                    int pos = i + 1;
                    for (int d = 0; d < directiveCount && pos < lines.Length; d++)
                    {
                        string directive = NextComplexDataLineV430LikeOriginal(lines, ref pos);
                        string[] dt = SplitTokens(directive);
                        if (dt.Length == 0) continue;

                        if (string.Equals(dt[0], "@DIRECT", StringComparison.OrdinalIgnoreCase) && dt.Length >= 3)
                        {
                            int a = ComplexStageIndexV430LikeOriginal(dt[1]);
                            int b = ComplexStageIndexV430LikeOriginal(dt[2]);
                            if (a >= 0 && b >= 0)
                                q.Transitions[a + b * 24] = new C2ComplexTransitionV430LikeOriginal
                                { Exists = true, Direct = true, MaxTransfTime = 0 };
                            continue;
                        }

                        if (string.Equals(dt[0], "@TRANSFORM", StringComparison.OrdinalIgnoreCase) && dt.Length >= 3)
                        {
                            int a = ComplexStageIndexV430LikeOriginal(dt[1]);
                            int b = ComplexStageIndexV430LikeOriginal(dt[2]);
                            int nParts = a >= 0 ? q.StateParts[a] : 0;
                            int maxTime = 0;
                            var partsV432 = new C2ComplexTransformElementV432LikeOriginal[nParts][];
                            for (int part = 0; part < nParts && pos < lines.Length; part++)
                            {
                                string partLine = NextComplexDataLineV430LikeOriginal(lines, ref pos);
                                string[] pt = SplitTokens(partLine);
                                int nElements = 0;
                                if (pt.Length >= 2)
                                    int.TryParse(pt[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out nElements);
                                int sum = 0;
                                var elemsV432 = new List<C2ComplexTransformElementV432LikeOriginal>(Math.Max(0, nElements));
                                for (int e = 0; e < nElements && pos < lines.Length; e++)
                                {
                                    string elemLine = NextComplexDataLineV430LikeOriginal(lines, ref pos);
                                    string[] et = SplitTokens(elemLine);
                                    int amountV432, x0V432, y0V432, x1V432, y1V432;
                                    if (et.Length >= 10 &&
                                        int.TryParse(et[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out amountV432) &&
                                        int.TryParse(et[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out x0V432) &&
                                        int.TryParse(et[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out y0V432) &&
                                        int.TryParse(et[7], NumberStyles.Integer, CultureInfo.InvariantCulture, out x1V432) &&
                                        int.TryParse(et[8], NumberStyles.Integer, CultureInfo.InvariantCulture, out y1V432))
                                    {
                                        elemsV432.Add(new C2ComplexTransformElementV432LikeOriginal
                                        {
                                            StartTime = sum, TimeAmount = amountV432,
                                            X0 = x0V432, Y0 = y0V432, X1 = x1V432, Y1 = y1V432,
                                            AnimationIdV437 = et[1], StartFrameV437 = ComplexIntV437(et[2]),
                                            EndFrameV437 = ComplexIntV437(et[3]), Fi0V437 = ComplexIntV437(et[6]), Fi1V437 = ComplexIntV437(et[9])
                                        });
                                        sum += amountV432;
                                    }
                                }
                                partsV432[part] = elemsV432.ToArray();
                                if (sum > maxTime) maxTime = sum;
                            }
                            if (a >= 0 && b >= 0)
                                q.Transitions[a + b * 24] = new C2ComplexTransitionV430LikeOriginal
                                { Exists = true, Direct = false, MaxTransfTime = maxTime, PartsV432LikeOriginal = partsV432 };
                            continue;
                        }

                        if (dt[0].StartsWith("$", StringComparison.Ordinal))
                        {
                            int state = ComplexStageIndexV430LikeOriginal(dt[0]);
                            if (state < 0) continue;
                            if (dt.Length >= 2 && dt[1].StartsWith("$", StringComparison.Ordinal))
                            {
                                int source = ComplexStageIndexV430LikeOriginal(dt[1]);
                                q.StateParts[state] = source >= 0 ? q.StateParts[source] : 0;
                                q.StatesV432LikeOriginal[state] = source >= 0 ? q.StatesV432LikeOriginal[source] : null;
                            }
                            else
                            {
                                int count = 0;
                                if (dt.Length >= 2)
                                    int.TryParse(dt[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out count);
                                count = Math.Max(0, count);
                                q.StateParts[state] = count;
                                var stateElemsV432 = new C2ComplexStateElementV432LikeOriginal[count];
                                for (int e = 0; e < count && pos < lines.Length; e++)
                                {
                                    string stateLineV432 = NextComplexDataLineV430LikeOriginal(lines, ref pos);
                                    string[] stV432 = SplitTokens(stateLineV432);
                                    if (stV432.Length < 4) continue;
                                    int dxV432 = 0, dyV432 = 0, dfiV432 = 0, ahV432 = 0;
                                    int.TryParse(stV432[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out dxV432);
                                    int.TryParse(stV432[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out dyV432);
                                    if (stV432.Length >= 5)
                                        int.TryParse(stV432[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out dfiV432);
                                    C2ComplexAnimationV437LikeOriginal animationV437;
                                    if (animationsV437.TryGetValue(stV432[0], out animationV437)) ahV432 = animationV437.AddHeight;
                                    stateElemsV432[e] = new C2ComplexStateElementV432LikeOriginal
                                    { AnimationId = stV432[0], Dx = dxV432, Dy = dyV432, Dfi = dfiV432, AddHeight = ahV432,
                                      AnmDirV437 = (byte)(stV432[3][0] == 'L' ? 0 : stV432[3][0] == 'R' ? 2 : stV432[3][0] == 'F' ? 3 : 1),
                                      ReverseClockV437 = stV432[3].Length > 1 && stV432[3][1] == 'B' };
                                }
                                q.StatesV432LikeOriginal[state] = stateElemsV432;
                            }
                        }
                    }
                    quants[q.Id] = q;
                    i = Math.Max(i, pos - 1);
                    continue;
                }
            }

            // Mechanics.cpp::ReadHelpersForComplexObjects reads sibling helpers.dat.
            string helpersPathV431 = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(objectsPath) ?? string.Empty, "helpers.dat");
            if (System.IO.File.Exists(helpersPathV431))
            {
                string[] helperLinesV431 = System.IO.File.ReadAllLines(
                    helpersPathV431, System.Text.Encoding.Default);
                for (int hiV431 = 0; hiV431 < helperLinesV431.Length; hiV431++)
                {
                    string helperLineV431 = CleanComplexDataLineV430LikeOriginal(helperLinesV431[hiV431]);
                    if (helperLineV431.StartsWith("$EXPLODE ", StringComparison.OrdinalIgnoreCase))
                    {
                        var explosion = SplitTokens(helperLineV431);
                        if (explosion.Length >= 6 && quants.TryGetValue(explosion[1], out var quant) &&
                            int.TryParse(explosion[5], out int stage)) quant.DeathStagesV441.Add(stage);
                        continue;
                    }
                    if (helperLineV431.Length == 0 || helperLineV431[0] == '$') continue;
                    string[] htV431 = SplitTokens(helperLineV431);
                    if (htV431.Length < 3) continue;
                    C2ComplexQuantDescV430LikeOriginal hqV431;
                    int qposV431;
                    if (!quants.TryGetValue(htV431[0], out hqV431) || hqV431 == null ||
                        !int.TryParse(htV431[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out qposV431))
                        continue;
                    int optV431 = 2;
                    if (htV431.Length >= 4)
                    {
                        string qoptV431 = htV431[3] ?? string.Empty;
                        if (qoptV431.IndexOf("attack", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            optV431 |= 1;
                            optV431 &= ~2;
                        }
                        if (qoptV431.IndexOf("nomove", StringComparison.OrdinalIgnoreCase) >= 0)
                            optV431 |= 2;
                    }
                    hqV431.HelpersV431LikeOriginal.Add(new C2ComplexHelperDescV431LikeOriginal
                    {
                        UnitId = htV431[1], QuantPos = qposV431, Options = optV431
                    });
                }
            }

            // #UNIT records can occur independently of the MQUANT blocks; resolve them in a second pass.
            for (int i = 0; i < lines.Length; i++)
            {
                string line = CleanComplexDataLineV430LikeOriginal(lines[i]);
                string[] t = SplitTokens(line);
                if (t.Length < 3 || !string.Equals(t[0], "#UNIT", StringComparison.OrdinalIgnoreCase)) continue;
                var chain = new List<C2ComplexQuantDescV430LikeOriginal>(Math.Max(1, t.Length - 2));
                for (int k = 2; k < t.Length; k++)
                {
                    C2ComplexQuantDescV430LikeOriginal q;
                    if (quants.TryGetValue(t[k], out q) && q != null) chain.Add(q);
                }
                if (chain.Count == 0) continue;
                result[t[1]] = new C2ComplexUnitDescV430LikeOriginal { Id = t[1], Chain = chain.ToArray(), AnimationsV437 = animationsV437 };
            }
            foreach (string raw in lines)
            {
                string[] t = SplitTokens(CleanComplexDataLineV430LikeOriginal(raw));
                C2ComplexUnitDescV430LikeOriginal unit;
                if (t.Length < 4 || t[0] != "#ATTACK" || !result.TryGetValue(t[1], out unit)) continue;
                // Mechanics.cpp: #ATTACK unit x z [y]. The y component is optional.
                unit.Chain[0].AttackXV437 = ComplexIntV437(t[2]);
                unit.Chain[0].AttackZV437 = ComplexIntV437(t[3]);
                unit.Chain[0].AttackYV437 = t.Length > 4 ? ComplexIntV437(t[4]) : 0;
            }
            return result;
        }

        private static bool TryResolveComplexUnitDescV430LikeOriginal(
            C2UnitOriginalRuntime u, out C2ComplexUnitDescV430LikeOriginal desc)
        {
            desc = null;
            if (u == null || u.Md == null || string.IsNullOrWhiteSpace(u.MdPath) ||
                string.IsNullOrWhiteSpace(u.Md.ComplexObjectIdLikeOriginal)) return false;
            string mdDir = System.IO.Path.GetDirectoryName(u.MdPath);
            System.IO.DirectoryInfo dataDir = !string.IsNullOrWhiteSpace(mdDir)
                ? System.IO.Directory.GetParent(mdDir) : null;
            if (dataDir == null) return false;
            string objectsPath = System.IO.Path.Combine(dataDir.FullName, "ComplexObjects", "Objects.dat");
            Dictionary<string, C2ComplexUnitDescV430LikeOriginal> byId;
            if (!ComplexUnitsByObjectsPathV430LikeOriginal.TryGetValue(objectsPath, out byId))
            {
                byId = ParseComplexObjectsV430LikeOriginal(objectsPath);
                ComplexUnitsByObjectsPathV430LikeOriginal[objectsPath] = byId;
            }
            return byId != null && byId.TryGetValue(u.Md.ComplexObjectIdLikeOriginal, out desc) && desc != null;
        }

        private static int ComplexTerrainHeightV430LikeOriginal(C2UnitOriginalRuntime u, float realX, float realY)
        {
            C2BattleTerrainMode mode = u != null && u.Info != null ? u.Info.OwnerMode : null;
            if (mode == null) return 0;
            return mode.C2OriginalFogTerrainHeightV1LikeOriginal(
                Mathf.RoundToInt(realX / 16.0f), Mathf.RoundToInt(realY / 16.0f));
        }

        private static float ComplexAverageTerrainHeightV430LikeOriginal(C2UnitOriginalRuntime u, float realX, float realY)
        {
            C2BattleTerrainMode mode = u != null && u.Info != null ? u.Info.OwnerMode : null;
            if (mode == null) return 0.0f;
            int rx = Mathf.RoundToInt(realX / 16.0f);
            int ry = Mathf.RoundToInt(realY / 16.0f);
            return (
                mode.C2OriginalFogTerrainHeightV1LikeOriginal(rx - 24, ry) +
                mode.C2OriginalFogTerrainHeightV1LikeOriginal(rx + 24, ry) +
                mode.C2OriginalFogTerrainHeightV1LikeOriginal(rx, ry - 24) +
                mode.C2OriginalFogTerrainHeightV1LikeOriginal(rx, ry + 24)) / 4.0f;
        }

        private static C2ComplexObjectRuntimeV430LikeOriginal EnsureComplexObjectRuntimeV430LikeOriginal(C2UnitOriginalRuntime u)
        {
            if (u == null) return null;
            if (u.OriginalComplexObjectV430LikeOriginal != null && u.OriginalComplexObjectV430LikeOriginal.Initialized)
                return u.OriginalComplexObjectV430LikeOriginal;

            C2ComplexUnitDescV430LikeOriginal desc;
            if (!TryResolveComplexUnitDescV430LikeOriginal(u, out desc) || desc.Chain == null || desc.Chain.Length == 0)
                return null;

            // NewMon.cpp creates the ordinary OneObject first, then Mechanics.cpp
            // replaces that occupancy by XorBlockComplexUnit. Remove the bridge's
            // generic GLock/UnitsField representation before constructing CObj state.
            if (u.Info != null)
                C2OriginalMovementSystemV425LikeOriginal.BeginComplexObjectOccupancyV430LikeOriginal(u.Info, u);

            var cob = new C2ComplexObjectRuntimeV430LikeOriginal();
            cob.Desc = desc;
            cob.OwnerRuntimeV430LikeOriginal = u;
            cob.Quants = new C2ComplexQuantRuntimeV430LikeOriginal[desc.Chain.Length];
            byte dir = u.Info != null ? u.Info.RealDir : (byte)((u.OriginalRealDirPrecise256LikeOriginal >> 8) & 255);
            // Mechanics.cpp CONFI(x) = ((byte(x)+2)&252) is used only for
            // the initial articulated-chain spacing; Fi itself keeps StartDir.
            byte confiDirV430 = unchecked((byte)((dir + 2) & 252));
            int cos = C2OriginalMovementMathV352.TCos[confiDirV430];
            int sin = C2OriginalMovementMathV352.TSin[confiDirV430];
            int rz = ComplexTerrainHeightV430LikeOriginal(u, u.RuntimeRealXLikeOriginal, u.RuntimeRealYLikeOriginal);

            for (int i = 0; i < cob.Quants.Length; i++)
            {
                C2ComplexQuantDescV430LikeOriginal qd = desc.Chain[i];
                var q = new C2ComplexQuantRuntimeV430LikeOriginal();
                q.Fi = dir;
                q.Fi0 = dir;
                q.ForcePoint0 = qd.X1;
                q.ForcePoint1 = qd.X2;
                if (i == 0)
                {
                    // Mechanics.cpp::CreateComplexObjectAt -> SetStartConditionsForComplexObject:
                    // x is OB.RealX>>4, y is (OB.RealY>>4)-RZ*2, then both are <<4.
                    q.Xc = u.RuntimeRealXLikeOriginal;
                    q.Yc = u.RuntimeRealYLikeOriginal - rz * 32.0f;
                }
                else
                {
                    C2ComplexQuantRuntimeV430LikeOriginal prev = cob.Quants[i - 1];
                    C2ComplexQuantDescV430LikeOriginal prevDesc = desc.Chain[i - 1];
                    q.Xc = prev.Xc + ((prevDesc.X2 * cos) >> 4) - ((qd.X1 * cos) >> 4);
                    q.Yc = prev.Yc + ((prevDesc.X2 * sin) >> 4) - ((qd.X1 * sin) >> 4);
                }
                q.Xc0 = q.Xc;
                q.Yc0 = q.Yc;
                cob.Quants[i] = q;
            }

            float headX, headY;
            ComplexGetHeadPointV430LikeOriginal(cob, out headX, out headY);
            cob.RealX = headX;
            cob.RealY = headY + rz * 32.0f;
            cob.Rz = rz;
            cob.StartState = 0;
            cob.FinalState = 0;
            cob.GroundStandState = 0;
            cob.GroundMotionState = 4;
            cob.DestDir = -1;
            cob.Charged = true;
            cob.ResSubtracted = true;
            cob.Lockpoints = false;

            // Mechanics.cpp::CreateComplexObjectAt creates HelpersPos in quant/order order.
            // Supplied retail helpers.dat has no `attack` option, so the source does NOT
            // auto-spawn helper OneObjects here; their index/serial is effectively unbound.
            // Keep every slot nevertheless because filling/death/helper positioning address
            // these entries by stable slot number.
            for (int qiV432 = 0; qiV432 < desc.Chain.Length; qiV432++)
            {
                C2ComplexQuantDescV430LikeOriginal qdV432 = desc.Chain[qiV432];
                if (qdV432 == null) continue;
                for (int hiV432 = 0; hiV432 < qdV432.HelpersV431LikeOriginal.Count; hiV432++)
                {
                    C2ComplexHelperDescV431LikeOriginal hdV432 = qdV432.HelpersV431LikeOriginal[hiV432];
                    if (hdV432 == null) continue;
                    cob.HelpersV432LikeOriginal.Add(new C2ComplexHelperSlotV432LikeOriginal
                    {
                        Desc = hdV432, QuantIndex = qiV432, QuantPos = hdV432.QuantPos, Runtime = null
                    });
                }
            }
            cob.Initialized = true;
            u.OriginalComplexObjectV430LikeOriginal = cob;

            // CreateComplexObjectAt copies COB.RealX/RealY back into OneObject and
            // immediately locks the complex square in MFIELDS 0/2/3.
            u.RuntimeRealXLikeOriginal = cob.RealX;
            u.RuntimeRealYLikeOriginal = cob.RealY;
            u.OriginalRzV431LikeOriginal = rz;
            u.OriginalBackMotionV431LikeOriginal = false;
            cob.BackMotion = false;
            C2OriginalMovementSystemV425LikeOriginal.LockComplexObjectV430LikeOriginal(u);
            return cob;
        }

        private static bool ComplexGetHelperPositionV432LikeOriginal(
            C2ComplexObjectRuntimeV430LikeOriginal cob, C2ComplexHelperSlotV432LikeOriginal slot,
            out int x, out int y, out int addHeight)
        {
            x = y = addHeight = 0;
            if (cob == null || slot == null || cob.Desc == null || cob.Quants == null) return false;
            int qi = slot.QuantIndex;
            int qp = slot.QuantPos;
            if (qi < 0 || qi >= cob.Quants.Length || qi >= cob.Desc.Chain.Length) return false;
            C2ComplexQuantRuntimeV430LikeOriginal qrt = cob.Quants[qi];
            C2ComplexQuantDescV430LikeOriginal qd = cob.Desc.Chain[qi];
            if (qrt == null || qd == null || cob.FinalState < 0 || cob.FinalState >= 24) return false;

            C2ComplexStateElementV432LikeOriginal[] finalState = qd.StatesV432LikeOriginal[cob.FinalState];
            if (finalState == null || qp < 0 || qp >= finalState.Length || finalState[qp] == null) return false;
            int px = finalState[qp].Dx;
            int py = finalState[qp].Dy;
            addHeight = finalState[qp].AddHeight;

            if (cob.StartState != cob.FinalState)
            {
                C2ComplexTransitionV430LikeOriginal tr = ComplexTransitionV430LikeOriginal(
                    qd, cob.StartState, cob.FinalState);
                if (tr != null && tr.Exists && !tr.Direct && tr.PartsV432LikeOriginal != null &&
                    qp < tr.PartsV432LikeOriginal.Length)
                {
                    C2ComplexTransformElementV432LikeOriginal[] pe = tr.PartsV432LikeOriginal[qp];
                    int tt = cob.TransTime >> 8;
                    // Mechanics.cpp: zero elements or completed transform jumps to final-state coordinates.
                    if (pe != null && pe.Length > 0 && tr.MaxTransfTime > tt)
                    {
                        bool found = false;
                        for (int i = 0; i < pe.Length; i++)
                        {
                            C2ComplexTransformElementV432LikeOriginal e = pe[i];
                            if (e == null || e.TimeAmount <= 0) continue;
                            if (tt >= e.StartTime && tt < e.StartTime + e.TimeAmount)
                            {
                                int dt = tt - e.StartTime;
                                px = e.X0 + (e.X1 - e.X0) * dt / e.TimeAmount;
                                py = e.Y0 + (e.Y1 - e.Y0) * dt / e.TimeAmount;
                                found = true;
                                break;
                            }
                        }
                        if (!found) return false;
                    }
                }
            }

            float fi = qrt.Fi * (Mathf.PI / 128.0f);
            float sn = Mathf.Sin(fi);
            float cs = Mathf.Cos(fi);
            C2UnitOriginalRuntime owner = cob.OwnerRuntimeV430LikeOriginal;
            int rz = owner != null ? owner.OriginalRzV431LikeOriginal : cob.Rz;
            x = (Mathf.RoundToInt(qrt.Xc) >> 4) + Mathf.RoundToInt(px * cs - py * sn);
            y = (Mathf.RoundToInt(qrt.Yc) >> 4) + Mathf.RoundToInt(px * sn + py * cs) + rz * 2;
            return true;
        }

        private static void ComplexSetHelpersPositionsV432LikeOriginal(C2ComplexObjectRuntimeV430LikeOriginal cob)
        {
            if (cob == null || cob.HelpersV432LikeOriginal == null) return;
            for (int i = 0; i < cob.HelpersV432LikeOriginal.Count; i++)
            {
                C2ComplexHelperSlotV432LikeOriginal slot = cob.HelpersV432LikeOriginal[i];
                C2UnitOriginalRuntime hr = slot != null ? slot.Runtime : null;
                if (hr == null || !hr.ActiveLikeOriginal || hr.Info == null || hr.Info.IsDeadLikeOriginal) continue;
                int x, y, addHeight;
                if (!ComplexGetHelperPositionV432LikeOriginal(cob, slot, out x, out y, out addHeight)) continue;
                hr.RuntimeRealXLikeOriginal = x << 4;
                hr.RuntimeRealYLikeOriginal = y << 4;
                hr.OriginalRzV431LikeOriginal = Math.Max(2, ComplexTerrainHeightV430LikeOriginal(hr, hr.RuntimeRealXLikeOriginal, hr.RuntimeRealYLikeOriginal));
                hr.OriginalOverEarthV430LikeOriginal = addHeight;
                hr.HasMoveTargetLikeOriginal = false;
                hr.MovePathRealWaypointsLikeOriginal = null;
                hr.MovePathIndexLikeOriginal = 0;
                hr.Info.RealX = Mathf.RoundToInt(hr.RuntimeRealXLikeOriginal);
                hr.Info.RealY = Mathf.RoundToInt(hr.RuntimeRealYLikeOriginal);
                hr.Info.RealXFloat = hr.RuntimeRealXLikeOriginal;
                hr.Info.RealYFloat = hr.RuntimeRealYLikeOriginal;
                C2LiveUnitCellIndex.PositionChanged(hr.Info);
            }
        }

        private static void ComplexGetHeadPointV430LikeOriginal(C2ComplexObjectRuntimeV430LikeOriginal cob, out float x, out float y)
        {
            x = cob != null ? cob.RealX : 0.0f;
            y = cob != null ? cob.RealY : 0.0f;
            if (cob == null || cob.Quants == null || cob.Quants.Length == 0) return;
            C2ComplexQuantRuntimeV430LikeOriginal q = cob.Quants[0];
            float fi = q.Fi * (Mathf.PI / 128.0f);
            x = q.Xc + q.ForcePoint0 * Mathf.Cos(fi) * 16.0f;
            y = q.Yc + q.ForcePoint0 * Mathf.Sin(fi) * 16.0f;
        }

        private static void ComplexGetTalePointV430LikeOriginal(C2ComplexObjectRuntimeV430LikeOriginal cob, out float x, out float y)
        {
            x = cob != null ? cob.RealX : 0.0f;
            y = cob != null ? cob.RealY : 0.0f;
            if (cob == null || cob.Quants == null || cob.Quants.Length == 0) return;
            C2ComplexQuantRuntimeV430LikeOriginal q = cob.Quants[cob.Quants.Length - 1];
            float fi = q.Fi * (Mathf.PI / 128.0f);
            x = q.Xc + q.ForcePoint1 * Mathf.Cos(fi) * 16.0f;
            y = q.Yc + q.ForcePoint1 * Mathf.Sin(fi) * 16.0f;
        }

        private static void ComplexPerformShiftV430LikeOriginal(C2ComplexObjectRuntimeV430LikeOriginal cob, float dx, float dy)
        {
            if (cob == null || cob.Quants == null) return;
            for (int j = 0; j < cob.Quants.Length; j++)
            {
                C2ComplexQuantRuntimeV430LikeOriginal q = cob.Quants[j];
                if (j == 0)
                {
                    q.MoveQuantLikeOriginal(0, dx, dy);
                }
                else
                {
                    C2ComplexQuantRuntimeV430LikeOriginal prev = cob.Quants[j - 1];
                    float pf = prev.Fi * (Mathf.PI / 128.0f);
                    float x0 = prev.Xc + prev.ForcePoint1 * Mathf.Cos(pf) * 16.0f;
                    float y0 = prev.Yc + prev.ForcePoint1 * Mathf.Sin(pf) * 16.0f;
                    float qf = q.Fi * (Mathf.PI / 128.0f);
                    float x1 = q.Xc + q.ForcePoint0 * Mathf.Cos(qf) * 16.0f;
                    float y1 = q.Yc + q.ForcePoint0 * Mathf.Sin(qf) * 16.0f;
                    q.MoveQuantLikeOriginal(0, x0 - x1, y0 - y1);
                }
            }

            // Mechanics.cpp propagates the articulated shift through TaleID.  Retail
            // descriptors never set InverseTaleMotion, so the active retail branch is
            // parent tale point -> child PerformBackShift(child tale point).
            C2ComplexObjectRuntimeV430LikeOriginal tale = cob.TaleV430LikeOriginal;
            if (tale != null && tale.OwnerRuntimeV430LikeOriginal != null &&
                tale.OwnerRuntimeV430LikeOriginal.Info != null &&
                !tale.OwnerRuntimeV430LikeOriginal.Info.IsDeadLikeOriginal)
            {
                float xc, yc, x, y;
                ComplexGetTalePointV430LikeOriginal(cob, out xc, out yc);
                ComplexGetTalePointV430LikeOriginal(tale, out x, out y);

                C2UnitOriginalRuntime to = tale.OwnerRuntimeV430LikeOriginal;
                C2OriginalMovementSystemV425LikeOriginal.UnlockComplexObjectV430LikeOriginal(to);
                ComplexPerformBackShiftV430LikeOriginal(tale, xc - x, yc - y);

                // Preserve the retail assignments literally (including their swapped
                // COB/OB axes) instead of "correcting" the original source.
                tale.RealX = to.RuntimeRealYLikeOriginal = to.OriginalRzV431LikeOriginal * 32.0f + y;
                tale.RealY = to.RuntimeRealXLikeOriginal = x;
                C2OriginalMovementSystemV425LikeOriginal.LockComplexObjectV430LikeOriginal(to);
            }
        }

        private static void ComplexPerformBackShiftV430LikeOriginal(C2ComplexObjectRuntimeV430LikeOriginal cob, float dx, float dy)
        {
            if (cob == null || cob.Quants == null) return;
            for (int j = cob.Quants.Length - 1; j >= 0; j--)
            {
                C2ComplexQuantRuntimeV430LikeOriginal q = cob.Quants[j];
                if (j == cob.Quants.Length - 1)
                {
                    q.MoveQuantLikeOriginal(1, dx, dy);
                }
                else
                {
                    C2ComplexQuantRuntimeV430LikeOriginal next = cob.Quants[j + 1];
                    float nf = next.Fi * (Mathf.PI / 128.0f);
                    float x0 = next.Xc + next.ForcePoint0 * Mathf.Cos(nf) * 16.0f;
                    float y0 = next.Yc + next.ForcePoint0 * Mathf.Sin(nf) * 16.0f;
                    float qf = q.Fi * (Mathf.PI / 128.0f);
                    float x1 = q.Xc + q.ForcePoint1 * Mathf.Cos(qf) * 16.0f;
                    float y1 = q.Yc + q.ForcePoint1 * Mathf.Sin(qf) * 16.0f;
                    q.MoveQuantLikeOriginal(1, x0 - x1, y0 - y1);
                }
            }

            C2ComplexObjectRuntimeV430LikeOriginal tale = cob.TaleV430LikeOriginal;
            if (tale != null && tale.OwnerRuntimeV430LikeOriginal != null &&
                tale.OwnerRuntimeV430LikeOriginal.Info != null &&
                !tale.OwnerRuntimeV430LikeOriginal.Info.IsDeadLikeOriginal)
            {
                // With retail InverseTaleMotion==0 the source takes the parent HEAD,
                // child HEAD and recursively calls PerformBackShift.
                float xc, yc, x, y;
                ComplexGetHeadPointV430LikeOriginal(cob, out xc, out yc);
                ComplexGetHeadPointV430LikeOriginal(tale, out x, out y);
                C2UnitOriginalRuntime to = tale.OwnerRuntimeV430LikeOriginal;
                C2OriginalMovementSystemV425LikeOriginal.UnlockComplexObjectV430LikeOriginal(to);
                ComplexPerformBackShiftV430LikeOriginal(tale, xc - x, yc - y);
                C2OriginalMovementSystemV425LikeOriginal.LockComplexObjectV430LikeOriginal(to);
            }
        }

        private static C2ComplexTransitionV430LikeOriginal ComplexTransitionV430LikeOriginal(
            C2ComplexQuantDescV430LikeOriginal q, int from, int to)
        {
            if (q == null || from < 0 || from >= 24 || to < 0 || to >= 24) return null;
            return q.Transitions[from + to * 24];
        }

        private static void ComplexTryTransformV430LikeOriginal(C2ComplexObjectRuntimeV430LikeOriginal cob, int finState)
        {
            if (cob == null || cob.Desc == null || cob.Desc.Chain == null || cob.Desc.Chain.Length == 0) return;
            if (cob.StartState == cob.FinalState && cob.StartState != finState)
            {
                int t01 = cob.FinalState & 3;
                int t02 = cob.FinalState >> 2;
                int t11 = finState & 3;
                int t12 = finState >> 2;
                if (t01 == t11)
                {
                    C2ComplexTransitionV430LikeOriginal qpt = ComplexTransitionV430LikeOriginal(
                        cob.Desc.Chain[0], cob.StartState, finState);
                    if (qpt != null && qpt.Exists) cob.FinalState = finState;
                    else
                    {
                        cob.FinalState = t11;
                        qpt = ComplexTransitionV430LikeOriginal(cob.Desc.Chain[0], cob.StartState, cob.FinalState);
                        if ((qpt == null || !qpt.Exists) && cob.StartState == 8) cob.FinalState = 12;
                    }
                }
                else if (t02 == 0) cob.FinalState = t11;
                else cob.FinalState = t01;
            }
        }

        private static void ComplexTryTransformToStandV430LikeOriginal(C2ComplexObjectRuntimeV430LikeOriginal cob)
        {
            if (cob == null || cob.StartState == 8) return;
            ComplexTryTransformV430LikeOriginal(cob, cob.GroundStandState);
            // Mechanics.cpp recursively propagates TryToTransformToStandState through
            // TaleID.  Keep the same chain semantics for connected limber/cannon units.
            if (cob.TaleV430LikeOriginal != null)
                ComplexTryTransformToStandV430LikeOriginal(cob.TaleV430LikeOriginal);
        }

        private static void ComplexTryTransformToMotionV430LikeOriginal(C2ComplexObjectRuntimeV430LikeOriginal cob)
        {
            if (cob == null) return;
            ComplexTryTransformV430LikeOriginal(cob, cob.GroundMotionState);
            // Mechanics.cpp does the same recursive propagation for motion state.
            if (cob.TaleV430LikeOriginal != null)
                ComplexTryTransformToMotionV430LikeOriginal(cob.TaleV430LikeOriginal);
        }

        private static bool ComplexHasLocalOrderV430LikeOriginal(C2UnitOriginalRuntime u)
        {
            if (u == null || u.Info == null) return false;
            // OneObject::LocalOrder includes both movement and AttackObj orders.  They
            // live in two bridge owners, so combine them only at this integration edge.
            return u.ArtilleryChargeOrderV439 >= 0 || u.ArtilleryPointOrderV439 != null || C2OriginalOrderChainV352.HasLocalMoveOrderLikeOriginal(u.Info) ||
                   C2CombatRuntimeV334LikeOriginal.IsAttackOrderActiveV403LikeOriginal(u.Info);
        }

        private static int ComplexLocalNewStateV430LikeOriginal(C2UnitOriginalRuntime u)
        {
            // Runtime field stores LocalNewState-1.  Neutral is -1 -> native 0.
            return u == null ? 0 : u.OriginalComplexObjectV430LikeOriginal != null
                ? u.ArtilleryAmmoV442 : Math.Max(0, u.LocalPostureWeaponTypeV411LikeOriginal + 1);
        }

        // V432: Mechanics.cpp::OneComplexObject::TestResSubtract.
        // The existing C2 combat core is the owner of NewMonster::ShotRes and nation stocks;
        // use that same transaction here instead of the V431 boolean-only bridge latch.
        private static bool ComplexTestResSubtractV432LikeOriginal(
            C2UnitOriginalRuntime u, C2ComplexObjectRuntimeV430LikeOriginal cob)
        {
            if (cob == null) return false;
            if (cob.ResSubtracted) return true;
            if (u == null || u.Info == null) return false;
            if (!C2CombatCoreV408LikeOriginal.TryConsumeShotResourcesV408LikeOriginal(u.Info, -1))
                return false;
            cob.ResSubtracted = true;
            return true;
        }

        private static void ComplexCancelRechargementV430LikeOriginal(C2ComplexObjectRuntimeV430LikeOriginal cob)
        {
            if (cob == null) return;
            // Mechanics.cpp::CancelRechargement.  The bit mask is intentional: native
            // complex states encode the weapon posture in multiples of four.
            int s0 = cob.StartState & 252;
            int f0 = cob.FinalState & 252;
            if (cob.TransTime > 0 && s0 != f0 &&
                (s0 == 8 || (s0 >= 12 && f0 >= 12)))
            {
                cob.StartState = cob.FinalState;
                cob.TransTime = 0;
                cob.Charged = false;
            }
        }

        private static void ComplexCheckConnectionV430LikeOriginal(C2ComplexObjectRuntimeV430LikeOriginal cob)
        {
            if (cob == null) return;
            // Mechanics.cpp::OneComplexObject::CheckConnection.
            if (cob.LeaderV430LikeOriginal != null)
            {
                C2UnitOriginalRuntime owner = cob.LeaderV430LikeOriginal.OwnerRuntimeV430LikeOriginal;
                if (owner == null || owner.Info == null || owner.Info.IsDeadLikeOriginal || !owner.ActiveLikeOriginal)
                    cob.LeaderV430LikeOriginal = null;
            }
            if (cob.TaleV430LikeOriginal != null)
            {
                C2UnitOriginalRuntime owner = cob.TaleV430LikeOriginal.OwnerRuntimeV430LikeOriginal;
                if (owner == null || owner.Info == null || owner.Info.IsDeadLikeOriginal || !owner.ActiveLikeOriginal)
                    cob.TaleV430LikeOriginal = null;
            }

            bool leader = cob.LeaderV430LikeOriginal != null;
            bool tale = cob.TaleV430LikeOriginal != null;
            if (!leader && !tale)
            {
                // Native source leaves GroundStandState untouched here (both assignments
                // are commented out) and forces only the motion state.
                cob.GroundMotionState = 4;
            }
            else if (leader && tale)
            {
                cob.GroundStandState = 3;
                cob.GroundMotionState = 7;
            }
            else if (leader)
            {
                cob.GroundStandState = 2;
                cob.GroundMotionState = 6;
            }
            else
            {
                cob.GroundStandState = 1;
                cob.GroundMotionState = 5;
            }
        }

        private void ComplexSetBackStateV431LikeOriginal(
            C2UnitOriginalRuntime u, C2ComplexObjectRuntimeV430LikeOriginal cob, bool back)
        {
            if (u == null || cob == null || cob.Quants == null || cob.Quants.Length == 0) return;

            C2OriginalMovementSystemV425LikeOriginal.UnlockComplexObjectV430LikeOriginal(u);
            try
            {
                if (u.OriginalBackMotionV431LikeOriginal == back) return;

                u.OriginalBackMotionV431LikeOriginal = back;
                cob.BackMotion = back; // mirror only; OneObject::BackMotion is authoritative.

                float x, y;
                if (back) ComplexGetTalePointV430LikeOriginal(cob, out x, out y);
                else ComplexGetHeadPointV430LikeOriginal(cob, out x, out y);

                // Mechanics.cpp::SetBackState uses the PREVIOUS OB->RZ to lift RealY,
                // copies the result to COB, then samples the new terrain height.
                u.RuntimeRealYLikeOriginal = u.OriginalRzV431LikeOriginal * 32.0f + y;
                u.RuntimeRealXLikeOriginal = x;
                cob.RealX = u.RuntimeRealXLikeOriginal;
                cob.RealY = u.RuntimeRealYLikeOriginal;
                u.OriginalRzV431LikeOriginal =
                    ComplexTerrainHeightV430LikeOriginal(u, cob.RealX, cob.RealY);
                cob.Rz = u.OriginalRzV431LikeOriginal;

                byte facing = back
                    ? unchecked((byte)((int)cob.Quants[cob.Quants.Length - 1].Fi + 128))
                    : unchecked((byte)((int)cob.Quants[0].Fi));
                SetRuntimeFacingLikeOriginal(u, facing);
                u.OriginalRealDirPrecise256LikeOriginal = facing << 8;
            }
            finally
            {
                C2OriginalMovementSystemV425LikeOriginal.LockComplexObjectV430LikeOriginal(u);
            }
        }

        private void ComplexPerformRotationV430LikeOriginal(
            C2UnitOriginalRuntime u, C2ComplexObjectRuntimeV430LikeOriginal cob, byte finalDir)
        {
            if (u == null || cob == null || cob.Quants == null || cob.Quants.Length == 0) return;
            float beforeRealX = u.RuntimeRealXLikeOriginal;
            float beforeRealY = u.RuntimeRealYLikeOriginal;
            C2OriginalMovementSystemV425LikeOriginal.UnlockComplexObjectV430LikeOriginal(u);
            try
            {
                C2ComplexQuantRuntimeV430LikeOriginal q0 = cob.Quants[0];
                byte fi = unchecked((byte)((int)q0.Fi));
                int dfi = unchecked((sbyte)(fi - finalDir));
                if (Math.Abs(dfi) > 8)
                {
                    if (cob.StartState == cob.GroundMotionState)
                    {
                        int gsp = OriginalGameSpeed256LikeOriginal;
                        if (gsp < 128) gsp = 128;
                        int r = q0.ForcePoint0;
                        int dx = (-gsp * r * C2OriginalMovementMathV352.TSin[fi]) >> 17;
                        int dy = ( gsp * r * C2OriginalMovementMathV352.TCos[fi]) >> 17;
                        if (dfi > 0) { dx = -dx; dy = -dy; }
                        ComplexPerformShiftV430LikeOriginal(cob, dx, dy);
                    }
                    else ComplexTryTransformToMotionV430LikeOriginal(cob);
                }
                else
                {
                    q0.Fi = finalDir;
                    q0.Fi0 = finalDir;
                }

                float x, y;
                if (u.OriginalBackMotionV431LikeOriginal) ComplexGetTalePointV430LikeOriginal(cob, out x, out y);
                else ComplexGetHeadPointV430LikeOriginal(cob, out x, out y);
                // Mechanics.cpp::PerformRotation uses OB->RZ from the previous
                // position for RealY, then samples GetHeight at the new position.
                cob.RealX = x;
                cob.RealY = cob.Rz * 32.0f + y;
                u.RuntimeRealXLikeOriginal = cob.RealX;
                u.RuntimeRealYLikeOriginal = cob.RealY;
                cob.Rz = ComplexTerrainHeightV430LikeOriginal(u, cob.RealX, cob.RealY);
                u.OriginalRzV431LikeOriginal = cob.Rz;
                byte facing = unchecked((byte)((int)q0.Fi));
                SetRuntimeFacingLikeOriginal(u, facing);
                u.OriginalRealDirPrecise256LikeOriginal = facing << 8;
            }
            finally
            {
                C2OriginalMovementSystemV425LikeOriginal.LockComplexObjectV430LikeOriginal(u);
            }
            if (UseContinuousWorldDeltaForOriginalMotion)
                UpdateRuntimeWorldAndRealContinuousLikeOriginal(u, beforeRealX, beforeRealY);
            else
                UpdateRuntimeWorldAndRealLikeOriginal(u);
        }

        private static void ComplexGetForceActOnBarV430LikeOriginal(
            C2UnitOriginalRuntime u, int xOriginalPx, int yOriginalPx, int l, out int fx, out int fy)
        {
            fx = 0;
            fy = 0;
            if (u == null || l <= 0) return;
            byte field = C2OriginalMovementSystemV425LikeOriginal.ResolveLockTypeV425LikeOriginal(u.Info);
            int cx = (xOriginalPx - (l << 3)) >> 4;
            int cy = (yOriginalPx - (l << 3)) >> 4;
            for (int dx = 0; dx < l; dx++)
            {
                for (int dy = 0; dy < l; dy++)
                {
                    if (!C2OriginalMovementSystemV425LikeOriginal.CheckPtV425LikeOriginal(cx + dx, cy + dy, field)) continue;
                    int ddx = dx + dx - l + 1;
                    int ddy = dy + dy - l + 1;
                    int d = C2OriginalMovementMathV352.Norma(ddx, ddy);
                    if (d < l)
                    {
                        if (d < 1) d = 1;
                        fx -= ddx * 256 / d / d;
                        fy -= ddy * 256 / d / d;
                    }
                }
            }
            int n = C2OriginalMovementMathV352.Norma(fx, fy);
            if (n > 256)
            {
                fx = (fx << 8) / n;
                fy = (fy << 8) / n;
            }
        }

        private static bool ComplexSquaresIntersectV430LikeOriginal(int x0, int y0, int l0, int x1, int y1, int l1)
        {
            int d = x0 + x0 + l0 - x1 - x1 - l1;
            if (Math.Abs(d) >= l0 + l1) return false;
            d = y0 + y0 + l0 - y1 - y1 - l1;
            return Math.Abs(d) < l0 + l1;
        }

        private static bool ComplexObjectIntersectsV430LikeOriginal(
            C2ComplexObjectRuntimeV430LikeOriginal cob, int x, int y, int l, int otherL)
        {
            if (cob == null || cob.Quants == null) return false;
            int l3 = otherL << 7;
            for (int i = 0; i < cob.Quants.Length; i++)
            {
                C2ComplexQuantRuntimeV430LikeOriginal q = cob.Quants[i];
                int qx = ((int)q.Xc - l3) >> 8;
                int qy = ((int)q.Yc - l3) >> 8;
                if (ComplexSquaresIntersectV430LikeOriginal(qx, qy, otherL, x, y, l)) return true;
            }
            return false;
        }

        private static bool ComplexCheckStopRuleWhenLockedV430LikeOriginal(C2UnitOriginalRuntime u)
        {
            if (u == null || u.Info == null || !u.HasMoveTargetLikeOriginal) return false;
            C2NeutralPeasantUnitInfoV2LikeOriginal[] all =
                C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
            int myL = C2OriginalMovementSystemV425LikeOriginal.ResolveLxLikeOriginal(u.Info);
            int myX = (Mathf.RoundToInt(u.RuntimeRealXLikeOriginal) - (myL << 7)) >> 8;
            int myY = (Mathf.RoundToInt(u.RuntimeRealYLikeOriginal) - (myL << 7)) >> 8;
            for (int i = 0; all != null && i < all.Length; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal other = all[i];
                if (other == null || other == u.Info || other.IsDeadLikeOriginal) continue;
                C2UnitOriginalRuntimeLinkLikeOriginal link = other.RuntimeLinkCachedLikeOriginal;
                C2UnitOriginalRuntime ort = link != null ? link.Runtime : null;
                if (ort == null || ort.OriginalComplexObjectV430LikeOriginal == null || !ort.HasMoveTargetLikeOriginal) continue;
                int ol = C2OriginalMovementSystemV425LikeOriginal.ResolveLxLikeOriginal(other);
                if (!ComplexObjectIntersectsV430LikeOriginal(ort.OriginalComplexObjectV430LikeOriginal, myX, myY, myL, ol)) continue;

                int myDx = Mathf.RoundToInt(u.MoveTargetRealXLikeOriginal - u.RuntimeRealXLikeOriginal);
                int myDy = Mathf.RoundToInt(u.MoveTargetRealYLikeOriginal - u.RuntimeRealYLikeOriginal);
                int opDx = Mathf.RoundToInt(ort.MoveTargetRealXLikeOriginal - ort.RuntimeRealXLikeOriginal);
                int opDy = Mathf.RoundToInt(ort.MoveTargetRealYLikeOriginal - ort.RuntimeRealYLikeOriginal);
                return (long)opDx * myDy - (long)opDy * myDx > 0;
            }
            return false;
        }

        private static bool ComplexTransitionStepV430LikeOriginal(C2ComplexObjectRuntimeV430LikeOriginal cob)
        {
            if (cob == null || cob.Desc == null || cob.Desc.Chain == null) return true;
            if (cob.StartState == cob.FinalState) return true;
            bool fail = false;
            for (int i = 0; i < cob.Desc.Chain.Length; i++)
            {
                C2ComplexTransitionV430LikeOriginal tr = ComplexTransitionV430LikeOriginal(
                    cob.Desc.Chain[i], cob.StartState, cob.FinalState);
                if (tr == null || !tr.Exists || (!tr.Direct && (cob.TransTime >> 8) < tr.MaxTransfTime))
                    fail = true;
            }
            if (!fail)
            {
                cob.StartState = cob.FinalState;
                cob.TransTime = 0;
                return true;
            }
            cob.TransTime += OriginalGameSpeed256LikeOriginal;
            return false;
        }

        private static bool ComplexTransitionsReadyV430LikeOriginal(C2ComplexObjectRuntimeV430LikeOriginal cob)
        {
            // Mechanics.cpp advances every connected TaleID transition in the same
            // handler pass and enters movement only when ALL are ready.
            bool allReady = true;
            C2ComplexObjectRuntimeV430LikeOriginal cur = cob;
            int guard = 0;
            while (cur != null && guard++ < 64)
            {
                ComplexCheckConnectionV430LikeOriginal(cur);
                if (!ComplexTransitionStepV430LikeOriginal(cur)) allReady = false;
                cur = cur.TaleV430LikeOriginal;
            }
            return allReady;
        }

        // Mechanics.cpp::MotionHandlerForComplexObjects, movement/state portion.
        // Leader/tale links and helper-unit rendering are separate gameplay/render integration and
        // are intentionally not replaced with a different movement algorithm here.
        private void AdvanceComplexObjectMotionV430LikeOriginal(C2UnitOriginalRuntime u, bool hasDestination)
        {
            if (u == null || u.Md == null || u.Info == null) return;
            // Mechanics.cpp::MotionHandlerForComplexObjects hard-resets PathDelay.
            u.OriginalPathDelayV425LikeOriginal = 0;
            float preEnsureX = u.RuntimeRealXLikeOriginal;
            float preEnsureY = u.RuntimeRealYLikeOriginal;
            C2ComplexObjectRuntimeV430LikeOriginal cob = EnsureComplexObjectRuntimeV430LikeOriginal(u);
            if (cob != null &&
                (Mathf.Abs(preEnsureX - u.RuntimeRealXLikeOriginal) > 0.001f ||
                 Mathf.Abs(preEnsureY - u.RuntimeRealYLikeOriginal) > 0.001f))
                UpdateRuntimeWorldAndRealLikeOriginal(u);
            if (cob == null)
            {
                // A COMPLEXOBJECT without its Objects.dat descriptor must never fall through
                // to the ordinary 0/1/3/4 managed mover: native C2 would have no valid CObjIndex.
                u.HasMoveTargetLikeOriginal = false;
                return;
            }
            StepArtilleryCaptureV441(u);
            StepFillComplexCrewV441(u);
            if (cob.NoMove)
            {
                // Mechanics.cpp: COB->NoMove -> DestX=-1; SetHelpersPositions(COB); return.
                u.HasMoveTargetLikeOriginal = false;
                ComplexSetHelpersPositionsV432LikeOriginal(cob);
                return;
            }

            ComplexCheckConnectionV430LikeOriginal(cob);
            // Connected tail objects are slaved to their head. Native C2 syncs helpers
            // before returning from the tail handler.
            if (cob.LeaderV430LikeOriginal != null)
            {
                ComplexSetHelpersPositionsV432LikeOriginal(cob);
                return;
            }

            // Mechanics.cpp charging preamble. Resource subtraction itself remains
            // owned by the existing combat/resource bridge; ResSubtracted is the
            // native latch consumed here by the movement state machine.
            int chargingF = cob.FinalState;
            int chargingS = cob.StartState;
            if ((chargingS == 8 || chargingS >= 12) && chargingS != chargingF &&
                (chargingF == 12 || chargingF == 16 || chargingF == 20) &&
                cob.TransTime > 1024 && !cob.Charged)
            {
                ComplexTestResSubtractV432LikeOriginal(u, cob);
                if (cob.ResSubtracted) cob.Charged = true;
            }

            StepArtilleryAutoFireV442(u);
            StepArtilleryChargeOrderV439(u);
            StepArtilleryPointOrderV439(u);
            hasDestination=u.HasMoveTargetLikeOriginal;
            bool hasLocalOrderV430 = ComplexHasLocalOrderV430LikeOriginal(u);
            int localNewStateV430 = ComplexLocalNewStateV430LikeOriginal(u);

            if (cob.Quants.Length > 0)
            {
                byte dir = u.OriginalBackMotionV431LikeOriginal
                    ? unchecked((byte)((int)cob.Quants[cob.Quants.Length - 1].Fi + 128))
                    : unchecked((byte)((int)cob.Quants[0].Fi));
                SetRuntimeFacingLikeOriginal(u, dir);
                u.OriginalRealDirPrecise256LikeOriginal = dir << 8;
            }

            if (hasDestination) cob.DestDir = -1;
            if (hasDestination || cob.DestDir >= 0)
                ComplexCancelRechargementV430LikeOriginal(cob);

            // Mechanics.cpp derives the complex stand state from LocalNewState only
            // when no LocalOrder is active.
            if (cob.GroundStandState == 8 && !hasLocalOrderV430)
                cob.GroundStandState = 12 + localNewStateV430 * 4;
            if (cob.StartState == 8 && cob.FinalState == 8 && !hasLocalOrderV430)
            {
                cob.GroundStandState = 12 + localNewStateV430 * 4;
                cob.FinalState = cob.GroundStandState;
            }

            if (!ComplexTransitionsReadyV430LikeOriginal(cob)) return;

            if (cob.DestDir >= 0)
            {
                ComplexPerformRotationV430LikeOriginal(u, cob, unchecked((byte)cob.DestDir));
                byte now = unchecked((byte)((int)cob.Quants[0].Fi));
                if (now == unchecked((byte)cob.DestDir)) cob.DestDir = -1;
            }

            if (!hasDestination && cob.DestDir < 0) u.OriginalStandTimeV425LikeOriginal++;
            else u.OriginalStandTimeV425LikeOriginal = 0;

            // PreCheckMotionRules starts with `return 0` in retail Mechanics.cpp.
            const int rule = 0;
            if ((!hasDestination && u.OriginalStandTimeV425LikeOriginal > 5) || rule == 1)
            {
                if (cob.DestDir < 0)
                {
                    int standS = cob.StartState;
                    int standF = cob.FinalState;
                    // Mechanics.cpp calls TestResSubtract here even when the object is
                    // already standing in the requested charged state.  This is the
                    // actual one-shot nation-resource transaction, not a synthetic latch.
                    ComplexTestResSubtractV432LikeOriginal(u, cob);
                    if (cob.ResSubtracted && standS == standF &&
                        standF == cob.GroundStandState && !cob.Charged)
                    {
                        if (localNewStateV430 == 0)
                        {
                            cob.StartState = ComplexRechargeStartV439(cob,16,12);
                            cob.FinalState = 12;
                            cob.GroundStandState = 12;
                            cob.TransTime = 0;
                        }
                        else
                        {
                            cob.StartState = 12;
                            cob.FinalState = 12 + localNewStateV430 * 4;
                            cob.GroundStandState = cob.FinalState;
                            cob.TransTime = 0;
                        }
                    }
                    else
                    {
                        ComplexTryTransformToStandV430LikeOriginal(cob);
                    }
                }
                ComplexSetHelpersPositionsV432LikeOriginal(cob);
                return;
            }

            float rx, ry;
            if (u.OriginalBackMotionV431LikeOriginal) ComplexGetTalePointV430LikeOriginal(cob, out rx, out ry);
            else { rx = cob.RealX; ry = cob.RealY; }
            float dxF = hasDestination ? u.MoveTargetRealXLikeOriginal - rx : 0.0f;
            float dyF = hasDestination ? u.MoveTargetRealYLikeOriginal - ry : 0.0f;
            int dx = Mathf.RoundToInt(dxF);
            int dy = Mathf.RoundToInt(dyF);
            int n = C2OriginalMovementMathV352.Norma(dx, dy);

            int l = C2OriginalMovementSystemV425LikeOriginal.ResolveLxLikeOriginal(u.Info);
            int fx = 0, fy = 0;
            bool unlimited = C2FormationRuntimeV167LikeOriginal.IsUnlimitedMotionV415LikeOriginal(u.Info);
            if (!unlimited)
            {
                // Mechanics.cpp: XorBlockComplexUnit(COB); GetForceActOnBar(...);
                // XorBlockComplexUnit(COB).  Remove the object's own square so it
                // cannot repel itself, then restore it immediately.
                C2OriginalMovementSystemV425LikeOriginal.UnlockComplexObjectV430LikeOriginal(u);
                try
                {
                    ComplexGetForceActOnBarV430LikeOriginal(
                        u, Mathf.RoundToInt(u.RuntimeRealXLikeOriginal) >> 4,
                        Mathf.RoundToInt(u.RuntimeRealYLikeOriginal) >> 4, l, out fx, out fy);
                }
                finally
                {
                    C2OriginalMovementSystemV425LikeOriginal.LockComplexObjectV430LikeOriginal(u);
                }
            }

            int maxD = n < 16 * 80 ? 70 : 32;
            if (unlimited) maxD = 80;
            else
            {
                maxD += OriginalGameSpeed256LikeOriginal / 32;
                if (maxD > 70) maxD = 70;
            }

            if (fx != 0 || fy != 0)
            {
                int fn = C2OriginalMovementMathV352.Norma(fx, fy);
                if (fn > 300)
                {
                    if (ComplexCheckStopRuleWhenLockedV430LikeOriginal(u))
                    {
                        u.HasMoveTargetLikeOriginal = false;
                        ComplexTryTransformToStandV430LikeOriginal(cob);
                        return;
                    }
                    if (maxD < 68) maxD = 68;
                }
                if (n > 128)
                {
                    dx = dx * 256 / n;
                    dy = dy * 256 / n;
                }
                dx += fx;
                dy += fy;
                n = C2OriginalMovementMathV352.Norma(dx, dy);
            }

            int n0 = unlimited ? 16 : 64;
            if (n > n0)
            {
                if (cob.StartState == cob.GroundMotionState)
                {
                    byte ang = C2OriginalMovementMathV352.GetDir(dx, dy);
                    int r = n >> 4;
                    if (u.OriginalBackMotionV431LikeOriginal)
                    {
                        byte a0 = unchecked((byte)((int)cob.Quants[cob.Quants.Length - 1].Fi + 128));
                        int d = unchecked((sbyte)(ang - a0));
                        if (Math.Abs(d) > 16)
                        {
                            ComplexPerformRotationV430LikeOriginal(u, cob, unchecked((byte)(ang + 128)));
                            return;
                        }
                        ang = unchecked((byte)(a0 + d));
                    }
                    else
                    {
                        byte a0 = unchecked((byte)((int)cob.Quants[0].Fi));
                        int d = unchecked((sbyte)(ang - a0));
                        if (Math.Abs(d) > maxD)
                        {
                            if (cob.Quants.Length != 1 || r < 128 || u.Md.MinRotator <= 8)
                                d = d < 0 ? -maxD : maxD;
                            else
                            {
                                ComplexPerformRotationV430LikeOriginal(u, cob, ang);
                                return;
                            }
                        }
                        ang = unchecked((byte)(a0 + d));
                    }

                    dx = C2OriginalMovementMathV352.TCos[ang];
                    dy = C2OriginalMovementMathV352.TSin[ang];
                    int dn = Math.Max(1, C2OriginalMovementMathV352.Norma(dx, dy));
                    dx = dx * n / dn;
                    dy = dy * n / dn;

                    int v = (n - n0 + 12) >> 1;
                    // Mechanics.cpp: MSP=(OB->GroupSpeed*GameSpeed)>>8.
                    int msp = (u.OriginalGroupSpeedV431LikeOriginal *
                               OriginalGameSpeed256LikeOriginal) >> 8;
                    if (u.Md.SpeedScaleOnTrees != 0)
                    {
                        int lxCell = C2OriginalMovementSystemV425LikeOriginal.ResolveLxLikeOriginal(u.Info);
                        int ox = (Mathf.RoundToInt(u.RuntimeRealXLikeOriginal) - (lxCell << 7)) >> 8;
                        int oy = (Mathf.RoundToInt(u.RuntimeRealYLikeOriginal) - (lxCell << 7)) >> 8;
                        if (C2OriginalMovementSystemV425LikeOriginal.CheckPtV425LikeOriginal(ox, oy, 2))
                            msp = (msp * u.Md.SpeedScaleOnTrees) >> 8;
                    }
                    if (v > msp) v = msp;
                    dx = dx * v / Math.Max(1, n);
                    dy = dy * v / Math.Max(1, n);

                    float beforeX = u.RuntimeRealXLikeOriginal;
                    float beforeY = u.RuntimeRealYLikeOriginal;
                    // Mechanics.cpp toggles the complex MFIELDS square off only for
                    // the RealX/RealY translation, then toggles it back at the new cell.
                    C2OriginalMovementSystemV425LikeOriginal.UnlockComplexObjectV430LikeOriginal(u);
                    cob.RealX += dx;
                    cob.RealY += dy;
                    u.RuntimeRealXLikeOriginal = cob.RealX;
                    u.RuntimeRealYLikeOriginal = cob.RealY;
                    C2OriginalMovementSystemV425LikeOriginal.LockComplexObjectV430LikeOriginal(u);
                    u.TotalPathLikeOriginal += C2OriginalMovementMathV352.Norma(dx, dy);
                    u.OriginalStandTimeV425LikeOriginal = 0;

                    // Mechanics.cpp samples GetFHeight at four points around the
                    // owner and removes z3*32 from the articulated quant plane.
                    cob.Rz = (int)ComplexAverageTerrainHeightV430LikeOriginal(u, cob.RealX, cob.RealY);
                    u.OriginalRzV431LikeOriginal = cob.Rz;
                    float zz = cob.Rz * 32.0f;
                    // PerformShift/PerformBackShift each wrap their articulated
                    // mutation in XorBlockComplexUnit in Mechanics.cpp.  The bridge
                    // uses explicit unlock/lock around that same mutation.
                    C2OriginalMovementSystemV425LikeOriginal.UnlockComplexObjectV430LikeOriginal(u);
                    try
                    {
                        if (u.OriginalBackMotionV431LikeOriginal)
                        {
                            float tx, ty;
                            ComplexGetTalePointV430LikeOriginal(cob, out tx, out ty);
                            ComplexPerformBackShiftV430LikeOriginal(cob, cob.RealX - tx, cob.RealY - zz - ty);
                        }
                        else
                        {
                            float hx, hy;
                            ComplexGetHeadPointV430LikeOriginal(cob, out hx, out hy);
                            float gdx = (cob.RealX - hx) * 0.1f;
                            float gdy = (cob.RealY - zz - hy) * 0.1f;
                            ComplexPerformShiftV430LikeOriginal(cob, gdx, gdy);
                        }
                    }
                    finally
                    {
                        C2OriginalMovementSystemV425LikeOriginal.LockComplexObjectV430LikeOriginal(u);
                    }

                    byte newDir = u.OriginalBackMotionV431LikeOriginal
                        ? unchecked((byte)((int)cob.Quants[cob.Quants.Length - 1].Fi + 128))
                        : unchecked((byte)((int)cob.Quants[0].Fi));
                    SetRuntimeFacingLikeOriginal(u, newDir);
                    u.OriginalRealDirPrecise256LikeOriginal = newDir << 8;
                    if (UseContinuousWorldDeltaForOriginalMotion)
                        UpdateRuntimeWorldAndRealContinuousLikeOriginal(u, beforeX, beforeY);
                    else
                        UpdateRuntimeWorldAndRealLikeOriginal(u);
                    return;
                }
                ComplexTryTransformToMotionV430LikeOriginal(cob);
                return;
            }

            // ONSTOP in Mechanics.cpp clears DestX/DestY without snapping the
            // articulated object to the destination. Advance bridge path ownership
            // while preserving the exact complex-object position.
            if (hasDestination) ConsumeNativeDestinationWithoutSnapV430LikeOriginal(u);
            else u.HasMoveTargetLikeOriginal = false;
            if (u.OriginalStandTimeV425LikeOriginal > 5) ComplexTryTransformToStandV430LikeOriginal(cob);
            ComplexSetHelpersPositionsV432LikeOriginal(cob);
        }

        // A native post-handler can clear DestX without teleporting the object to
        // that point. Bridge path ownership still has to advance to the next native
        // waypoint, but must preserve the post-handler's actual RealX/RealY.
        private bool ConsumeNativeDestinationWithoutSnapV430LikeOriginal(C2UnitOriginalRuntime u)
        {
            if (u == null) return false;
            if (AdvanceRuntimeMoveWaypointLikeOriginal(u)) return true;
            u.HasMoveTargetLikeOriginal = false;
            return false;
        }

        // ---- V430: Motion.cpp::CalculateMotion2 + MotionHandlerOfNewSheeps ----
        private void AdvanceNewSheepMotionV430LikeOriginal(C2UnitOriginalRuntime u, bool hasDestination)
        {
            if (u == null || u.Md == null || u.Info == null) return;

            ushort oldPreciseV431 = unchecked((ushort)u.OriginalRealDirPrecise256LikeOriginal);
            // Motion.cpp starts with if(FrameFinished) SetZeroFrame().
            if (u.FrameFinishedLikeOriginal)
            {
                u.CurrentFrameLong = 0;
                u.FrameFinishedLikeOriginal = false;
                u.FrameFinishedLatchedForOrdersLikeOriginal = false;
            }

            bool nativeDestClearedV430 = false;
            int maxV = u.Md.MotionDist;
            int rate0 = u.Md.Rate != null && u.Md.Rate.Length > 0 ? u.Md.Rate[0] : 16;
            // Retail SpeedSh is zero in the normal C2 simulation mode used by the bridge.
            maxV = (maxV * rate0) >> 4;

            int realX = Mathf.RoundToInt(u.RuntimeRealXLikeOriginal);
            int realY = Mathf.RoundToInt(u.RuntimeRealYLikeOriginal);
            int currentDir = u.Info != null ? u.Info.RealDir : (u.OriginalRealDirPrecise256LikeOriginal >> 8) & 255;
            int precise = u.OriginalRealDirPrecise256LikeOriginal;
            if (((precise >> 8) & 255) != currentDir)
                precise = currentDir << 8;

            if (hasDestination && u.HasMoveTargetLikeOriginal)
            {
                int tx = Mathf.RoundToInt(u.MoveTargetRealXLikeOriginal);
                int ty = Mathf.RoundToInt(u.MoveTargetRealYLikeOriginal);
                int ddx = tx - realX;
                int ddy = ty - realY;
                int dis = C2OriginalMovementMathV352.Norma(ddx, ddy);
                if (dis > 64)
                {
                    if (u.OriginalSheepSpeedV430LikeOriginal < maxV)
                        u.OriginalSheepSpeedV430LikeOriginal++;
                    else
                        u.OriginalSheepSpeedV430LikeOriginal = maxV;

                    byte dir1 = C2OriginalMovementMathV352.GetDir(ddx, ddy);
                    int ddir = unchecked((sbyte)(dir1 - (byte)currentDir));
                    if (Math.Abs(ddir) < 2)
                    {
                        currentDir = dir1;
                        precise = currentDir << 8;
                        int spf = u.OriginalSheepSpeedV430LikeOriginal; // GameSpeed == 256.
                        u.OriginalRealVxV430LikeOriginal = ddx * spf / Math.Max(1, dis);
                        u.OriginalRealVyV430LikeOriginal = ddy * spf / Math.Max(1, dis);
                        if (u.Md.KineticLimitLikeOriginal > 0)
                        {
                            u.OriginalKineticPowerV430LikeOriginal += OriginalGameSpeed256LikeOriginal;
                            if (u.OriginalKineticPowerV430LikeOriginal >= u.Md.KineticLimitLikeOriginal)
                            {
                                u.OriginalKineticPowerV430LikeOriginal = u.Md.KineticLimitLikeOriginal;
                                DetectStrikenEnemyV431LikeOriginal(u);
                            }
                        }
                    }
                    else
                    {
                        u.OriginalKineticPowerV430LikeOriginal = 0;
                        int turnDis = Math.Min(dis, 4000);
                        int dr = ((OriginalGameSpeed256LikeOriginal * Math.Max(0, u.Md.MinRotator)) >> 5) *
                                 (3000 + turnDis) / Math.Max(1, 300 + turnDis);
                        int add = Math.Max(Math.Abs(ddir), 20);
                        int div = Math.Max(Math.Abs(ddir), 16);
                        dr = dr * add / 20;
                        byte lockType = C2OriginalMovementSystemV425LikeOriginal.ResolveLockTypeV425LikeOriginal(u.Info);
                        dr = Math.Min(dr, lockType == 1 ? 450 : 768);

                        int sp1 = maxV * (100 + turnDis) / Math.Max(1, 1000 + turnDis);
                        sp1 = sp1 * 16 / div;
                        if (sp1 < u.OriginalSheepSpeedV430LikeOriginal)
                            u.OriginalSheepSpeedV430LikeOriginal -= 2;

                        if (ddir > 0) precise = (precise + dr) & 0xFFFF;
                        else precise = (precise - dr) & 0xFFFF;
                        currentDir = (precise >> 8) & 255;
                        int spf = u.OriginalSheepSpeedV430LikeOriginal;
                        u.OriginalRealVxV430LikeOriginal = (spf * C2OriginalMovementMathV352.TCos[currentDir]) >> 8;
                        u.OriginalRealVyV430LikeOriginal = (spf * C2OriginalMovementMathV352.TSin[currentDir]) >> 8;
                    }
                }
                else
                {
                    // Motion.cpp: DestX=-1 only. Speed/RealV are intentionally left
                    // untouched for this quantum and the object may coast past the point.
                    u.OriginalKineticPowerV430LikeOriginal = 0;
                    nativeDestClearedV430 = true;
                }
            }
            else
            {
                u.OriginalKineticPowerV430LikeOriginal = 0;
                if (u.OriginalSheepSpeedV430LikeOriginal <= 8)
                {
                    u.OriginalRealVxV430LikeOriginal = 0;
                    u.OriginalRealVyV430LikeOriginal = 0;
                    u.OriginalSheepSpeedV430LikeOriginal = 0;
                }
                else
                {
                    u.OriginalRealVxV430LikeOriginal -= u.OriginalRealVxV430LikeOriginal >> 2;
                    u.OriginalRealVyV430LikeOriginal -= u.OriginalRealVyV430LikeOriginal >> 2;
                    u.OriginalSheepSpeedV430LikeOriginal -= u.OriginalSheepSpeedV430LikeOriginal >> 2;
                }
            }

            u.OriginalRealDirPrecise256LikeOriginal = precise;
            SetRuntimeFacingLikeOriginal(u, (byte)currentDir);
            u.OriginalRealDirPrecise256LikeOriginal = precise;

            bool movementSucceededV431 = false;
            if (u.OriginalSheepSpeedV430LikeOriginal > 0)
            {
                int committedDx;
                int committedDy;
                if (C2OriginalMovementSystemV425LikeOriginal.TryNewSheepMotionStepV425LikeOriginal(
                        u.Info, u,
                        u.OriginalRealVxV430LikeOriginal,
                        u.OriginalRealVyV430LikeOriginal,
                        out committedDx, out committedDy))
                {
                    float beforeX = u.RuntimeRealXLikeOriginal;
                    float beforeY = u.RuntimeRealYLikeOriginal;
                    u.RuntimeRealXLikeOriginal += committedDx;
                    u.RuntimeRealYLikeOriginal += committedDy;
                    u.TotalPathLikeOriginal += C2OriginalMovementMathV352.Norma(committedDx, committedDy);
                    u.OriginalStandTimeV425LikeOriginal = 0;
                    if (UseContinuousWorldDeltaForOriginalMotion)
                        UpdateRuntimeWorldAndRealContinuousLikeOriginal(u, beforeX, beforeY);
                    else
                        UpdateRuntimeWorldAndRealLikeOriginal(u);

                    // Native handler does not choose MotionL here; LocalOrder owns
                    // NewAnm. Its only animation operation on success is SetNextFrame.
                    movementSucceededV431 = true;
                }
            }

            if (nativeDestClearedV430) ConsumeNativeDestinationWithoutSnapV430LikeOriginal(u);
            if (u.OriginalSheepSpeedV430LikeOriginal == 0)
            {
                u.OriginalStandTimeV425LikeOriginal++;
                C2OriginalMovementSystemV425LikeOriginal.MarkUnitStandingV425LikeOriginal(u.Info, u);
                if (!u.HasMoveTargetLikeOriginal)
                    u.Info.C2NeutralPeasantUnitsV15SetMovingFlagLikeOriginal(false, false);

                // Motion.cpp advances a zero-speed object only when NewAnm is not Stand.
                int standIndexV431 = ResolveRuntimeStandAnimationIndexV322LikeOriginal(u);
                if (standIndexV431 >= 0 && u.CurrentAnimIndex == standIndexV431)
                    u.OriginalSkipGenericAnimationAdvanceV431LikeOriginal = true;
            }
            else if (!movementSucceededV431)
            {
                // With Speed!=0 a blocked sheep does NOT call SetNextFrame.
                u.OriginalSkipGenericAnimationAdvanceV431LikeOriginal = true;
            }

            // Motion.cpp postamble is unconditional.
            u.OriginalRzV431LikeOriginal = Math.Max(
                0, ComplexTerrainHeightV430LikeOriginal(u, u.RuntimeRealXLikeOriginal, u.RuntimeRealYLikeOriginal));
            ushort nowPreciseV431 = unchecked((ushort)u.OriginalRealDirPrecise256LikeOriginal);
            u.OriginalPhaseV431LikeOriginal += unchecked((short)(nowPreciseV431 - oldPreciseV431));

            // Central animation adapter performs the native SetNextFrame operation.
            // Then this same quantum must test FrameFinished -> Stand + SetZeroFrame.
            u.OriginalSheepPostAnimationFinalizeV431LikeOriginal = true;
        }

        // Motion.cpp::DetectStrikenEnemy.
        private void DetectStrikenEnemyV431LikeOriginal(C2UnitOriginalRuntime u)
        {
            if (u == null || u.Info == null) return;
            int lx = C2OriginalMovementSystemV425LikeOriginal.ResolveLxLikeOriginal(u.Info);
            int d = lx * 8 + 140;
            byte strikeDir = u.Info.RealDir;
            int x1 = (Mathf.RoundToInt(u.RuntimeRealXLikeOriginal) >> 4) +
                     d * C2OriginalMovementMathV352.TCos[strikeDir] / 256;
            int y1 = (Mathf.RoundToInt(u.RuntimeRealYLikeOriginal) >> 4) +
                     d * C2OriginalMovementMathV352.TSin[strikeDir] / 256;
            int sourceMaskV431 = C2CombatCoreV408LikeOriginal.GetNMaskV408LikeOriginal(u.Info);
            C2NeutralPeasantUnitInfoV2LikeOriginal[] all =
                C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
            for (int i = 0; all != null && i < all.Length; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal other = all[i];
                if (other == null || other == u.Info || other.IsDeadLikeOriginal) continue;
                if ((sourceMaskV431 & C2CombatCoreV408LikeOriginal.GetNMaskV408LikeOriginal(other)) != 0)
                    continue;
                C2UnitOriginalRuntimeLinkLikeOriginal link = other.RuntimeLinkCachedLikeOriginal;
                C2UnitOriginalRuntime ort = link != null ? link.Runtime : null;
                if (ort == null) continue;
                int ox = Mathf.RoundToInt(ort.RuntimeRealXLikeOriginal) >> 4;
                int oy = Mathf.RoundToInt(ort.RuntimeRealYLikeOriginal) >> 4;
                if (C2OriginalMovementMathV352.Norma(ox - x1, oy - y1) >= 80) continue;
                int dd = unchecked((sbyte)(other.RealDir - strikeDir));
                int facing = Math.Abs(Math.Abs(dd) - 64);
                if (facing >= 20) continue;
                other.PlayDeathOneShotLikeOriginal(other.RealDir);
                break; // STRIKE_Callback changes NMask to FF after first death.
            }
        }

        // ---- V430: NewMon.cpp::MotionHandlerForFlyingObjects ----
        private void AdvanceFlyingMotionV430LikeOriginal(C2UnitOriginalRuntime u, bool hasDestination)
        {
            if (u == null || u.Md == null) return;

            // NewMon.cpp::MotionHandlerForFlyingObjects exact preamble:
            //   if(DestX<=0) StandTime++;
            //   if(BrigadeID==FFFF && !LocalOrder) DestX=-1;
            // The second condition deliberately runs AFTER the StandTime test, so a
            // destination cleared on this quantum starts incrementing StandTime only
            // on the next quantum, exactly like the native object.
            bool hadNativeDestinationV430 = hasDestination && u.HasMoveTargetLikeOriginal;
            if (!hadNativeDestinationV430)
                u.OriginalStandTimeV425LikeOriginal++;

            bool inBrigadeV430 = u.Info != null &&
                C2FormationRuntimeV167LikeOriginal.IsUnitInRuntimeFormationV168LikeOriginal(u.Info);
            bool hasLocalOrderV430 = HasAnyLocalOrderV431LikeOriginal(u);
            if (!inBrigadeV430 && !hasLocalOrderV430)
            {
                u.HasMoveTargetLikeOriginal = false;
                hasDestination = false;
            }

            // NewMon.cpp: if(PathDelay>=FrmDec) PathDelay-=FrmDec; else PathDelay=0.
            // The bridge simulation quantum is the retail FrmDec=1 cadence.
            if (u.OriginalPathDelayV425LikeOriginal > 0) u.OriginalPathDelayV425LikeOriginal--;

            // Native saves NAM before a possible FrameFinished switch to MotionL.
            AnimModel namV431 = CurrentAnim(u);

            int vx0 = u.OriginalRealVxV430LikeOriginal;
            int vy0 = u.OriginalRealVyV430LikeOriginal;
            int n = C2OriginalMovementMathV352.Norma(vx0, vy0);
            bool slowFrame = false;
            int sp0 = Math.Max(0, u.Md.MotionDist) *
                      Math.Max(0, u.OriginalMoreCharacterSpeedPercentLikeOriginal) / 100;

            if (hasDestination && u.HasMoveTargetLikeOriginal)
            {
                int ddx = Mathf.RoundToInt(u.MoveTargetRealXLikeOriginal - u.RuntimeRealXLikeOriginal);
                int ddy = Mathf.RoundToInt(u.MoveTargetRealYLikeOriginal - u.RuntimeRealYLikeOriginal);
                int range = C2OriginalMovementMathV352.Norma(ddx, ddy);
                int speedRange = Math.Min(range, 1000);
                int maxs = sp0 * speedRange / 1000;
                n += OriginalGameSpeed256LikeOriginal << 2;
                if ((n >> 8) > maxs) n = maxs << 8;

                byte desired = C2OriginalMovementMathV352.GetDir(ddx, ddy);
                byte current = u.Info != null
                    ? u.Info.RealDir
                    : (byte)((u.OriginalRealDirPrecise256LikeOriginal >> 8) & 255);
                int ddir = unchecked((sbyte)(desired - current));
                int minRotator = Math.Max(0, u.Md.MinRotator);
                if (Math.Abs(ddir) < minRotator)
                    current = desired;
                else if (ddir > 0)
                {
                    current = unchecked((byte)(current + minRotator));
                    slowFrame = true;
                }
                else
                {
                    current = unchecked((byte)(current - minRotator));
                    slowFrame = true;
                }
                SetRuntimeFacingLikeOriginal(u, current);
                u.OriginalRealDirPrecise256LikeOriginal = current << 8;
            }
            else
            {
                n = 0;
            }

            byte dir = u.Info != null
                ? u.Info.RealDir
                : (byte)((u.OriginalRealDirPrecise256LikeOriginal >> 8) & 255);
            u.OriginalRealVxV430LikeOriginal = (n * C2OriginalMovementMathV352.TCos[dir]) >> 8;
            u.OriginalRealVyV430LikeOriginal = (n * C2OriginalMovementMathV352.TSin[dir]) >> 8;
            u.OriginalFlyForceXV430LikeOriginal =
                (u.OriginalFlyForceXV430LikeOriginal * 63 +
                 (u.OriginalRealVxV430LikeOriginal - vx0) * 10) / 64;
            u.OriginalFlyForceYV430LikeOriginal =
                (u.OriginalFlyForceYV430LikeOriginal * 63 +
                 (u.OriginalRealVyV430LikeOriginal - vy0) * 10) / 64;

            float beforeX = u.RuntimeRealXLikeOriginal;
            float beforeY = u.RuntimeRealYLikeOriginal;
            u.RuntimeRealXLikeOriginal += u.OriginalRealVxV430LikeOriginal >> 8;
            u.RuntimeRealYLikeOriginal += u.OriginalRealVyV430LikeOriginal >> 8;
            if (u.OriginalRealVxV430LikeOriginal != 0 || u.OriginalRealVyV430LikeOriginal != 0)
                u.TotalPathLikeOriginal += C2OriginalMovementMathV352.Norma(
                    u.OriginalRealVxV430LikeOriginal >> 8,
                    u.OriginalRealVyV430LikeOriginal >> 8);
            if (UseContinuousWorldDeltaForOriginalMotion)
                UpdateRuntimeWorldAndRealContinuousLikeOriginal(u, beforeX, beforeY);
            else
                UpdateRuntimeWorldAndRealLikeOriginal(u);

            int cappedN = Math.Min(n, 16000);
            int h = ((cappedN >> 5) * u.Md.FlyHeightLikeOriginal) / 3200 +
                    u.Md.StartFlyHeightLikeOriginal;
            // Retail uses two independent comparisons, without clamping to H.
            // Preserve the small overshoot/oscillation when |OverEarth-H| < 10.
            if (u.OriginalOverEarthV430LikeOriginal > h)
                u.OriginalOverEarthV430LikeOriginal -= 10;
            if (u.OriginalOverEarthV430LikeOriginal < h)
                u.OriginalOverEarthV430LikeOriginal += 10;

            // NewMon.cpp switches to MotionL only when the current animation has
            // finished (or has no frames), then starts it from frame zero.
            AnimModel currentAnim = CurrentAnim(u);
            if (currentAnim == null || currentAnim.Frames.Count == 0 || u.FrameFinishedLikeOriginal)
            {
                int motion = ResolveRuntimeMotionAnimationIndexV223LikeOriginal(u);
                if (motion >= 0)
                {
                    SelectAnimationStateLikeOriginal(
                        u, C2UnitOriginalState.Motion, motion, true,
                        "MotionHandlerForFlyingObjects_v430");
                    currentAnim = CurrentAnim(u);
                }
            }
            int currentFrame = currentAnim != null ? FixedFrameIndexLikeOriginal(u, currentAnim) : 0;
            if (slowFrame && namV431 != null &&
                currentFrame > namV431.SlowFrameStartLikeOriginal &&
                currentFrame < namV431.SlowFrameEndLikeOriginal)
            {
                u.OriginalAnimationSpeedPercentV430LikeOriginal =
                    namV431.SlowFrameSpeedLikeOriginal;
            }
            else
            {
                u.OriginalAnimationSpeedPercentV430LikeOriginal = 100;
            }

            u.OriginalRzV431LikeOriginal = ComplexTerrainHeightV430LikeOriginal(
                u, u.RuntimeRealXLikeOriginal, u.RuntimeRealYLikeOriginal);
            byte finalDirV431 = u.Info != null
                ? u.Info.RealDir
                : (byte)((u.OriginalRealDirPrecise256LikeOriginal >> 8) & 255);
            u.OriginalRealDirPrecise256LikeOriginal = finalDirV431 << 8;
        }

        // V431: NewMon.cpp::MotionHandlerForSingleStepObjects wrapper.
        private void AdvanceSingleStepHandlerV431LikeOriginal(C2UnitOriginalRuntime u, float dt)
        {
            if (u == null || u.Md == null) return;

            ApplyMotionTiringProfiledV430LikeOriginal(u, dt);

            bool hasDestination = u.HasMoveTargetLikeOriginal;
            if (!hasDestination) u.OriginalStandTimeV425LikeOriginal++;

            // NewMon.cpp: OB->Attack && !FrameFinished && !RotationAtPlaceSpeed.
            if (u.Info != null &&
                C2CombatRuntimeV334LikeOriginal.IsAttackOrderActiveV403LikeOriginal(u.Info) &&
                !u.FrameFinishedLikeOriginal && u.Md.RotationAtPlaceSpeed == 0 &&
                !u.HiddenInsideBuildingLikeOriginal)
            {
                C2CombatRuntimeV334LikeOriginal combat =
                    u.Info.GetComponent<C2CombatRuntimeV334LikeOriginal>();
                bool mustShiftV431 = combat != null && combat.HasLiveBuildingTargetV431LikeOriginal();
                C2NeutralPeasantUnitInfoV2LikeOriginal enemy;
                if (!mustShiftV431 && combat != null &&
                    combat.TryGetLiveMeleeTargetV406LikeOriginal(out enemy) && enemy != null)
                {
                    C2UnitOriginalRuntimeLinkLikeOriginal enemyLink = enemy.RuntimeLinkCachedLikeOriginal;
                    C2UnitOriginalRuntime enemyRt = enemyLink != null ? enemyLink.Runtime : null;
                    if (enemyRt != null)
                    {
                        int addShot = C2CombatCoreV408LikeOriginal.GetTraitsV408LikeOriginal(enemy).AddShotRadius;
                        int rm = (addShot + 140) * 16;
                        int edx = Mathf.RoundToInt(enemyRt.RuntimeRealXLikeOriginal - u.RuntimeRealXLikeOriginal);
                        int edy = Mathf.RoundToInt(enemyRt.RuntimeRealYLikeOriginal - u.RuntimeRealYLikeOriginal);
                        mustShiftV431 = C2OriginalMovementMathV352.Norma(edx, edy) < rm;
                    }
                }
                if (mustShiftV431) ApplyUnitLitleShiftV431LikeOriginal(u);
            }

            // LocalOrder->DoLink == RotUnitLink exits the native handler immediately.
            if (u.SingleStepRotateAtPlaceActiveV352LikeOriginal)
            {
                AdvanceSingleStepRotUnitLinkV352LikeOriginal(u);
                return;
            }

            bool hasAnyLocalOrder = HasAnyLocalOrderV431LikeOriginal(u);
            bool inMotion = u.State == C2UnitOriginalState.Motion;
            // OneObject::NewState is zero only for neutral stand state in this bridge.
            bool newState = u.PostureWeaponTypeLikeOriginal >= 0;
            if (u.FrameFinishedLikeOriginal && !hasDestination &&
                !(inMotion || hasAnyLocalOrder || newState))
            {
                if ((_originalBoidsSimulationTickLikeOriginal & 15) != 9)
                    return;
            }

            byte realDir = u.Info != null
                ? u.Info.RealDir
                : (byte)((u.OriginalRealDirPrecise256LikeOriginal >> 8) & 255);
            if (((u.OriginalRealDirPrecise256LikeOriginal >> 8) & 255) != realDir)
                u.OriginalRealDirPrecise256LikeOriginal = realDir << 8;

            bool inBrigade = u.Info != null &&
                C2FormationRuntimeV167LikeOriginal.IsUnitInRuntimeFormationV168LikeOriginal(u.Info);
            if (!inBrigade && !hasAnyLocalOrder && !u.HiddenInsideBuildingLikeOriginal)
            {
                u.HasMoveTargetLikeOriginal = false;
                hasDestination = false;
            }

            if (u.OriginalPathDelayV425LikeOriginal > 0)
                u.OriginalPathDelayV425LikeOriginal--;

            // WATERROUND/SpotByUnit is a water-render disturbance in RealWater.cpp,
            // not motion state. Parse the source flag but do not synthesize a Unity
            // movement effect here; supplied clean CII data contains no WATERROUND MD.

            AnimModel anim = CurrentAnim(u);
            bool canNowMove = inMotion || u.FrameFinishedLikeOriginal;
            if (!canNowMove && anim != null)
            {
                if (anim.MoveBreak && hasDestination) canNowMove = true;
                if (anim.CanBeBroken &&
                    (hasDestination || u.PostureWeaponTypeLikeOriginal != u.LocalPostureWeaponTypeV411LikeOriginal))
                    canNowMove = true;
            }

            if (canNowMove && hasDestination && u.HasMoveTargetLikeOriginal)
            {
                u.OriginalStandTimeV425LikeOriginal = 0;
                float dx = u.MoveTargetRealXLikeOriginal - u.RuntimeRealXLikeOriginal;
                float dy = u.MoveTargetRealYLikeOriginal - u.RuntimeRealYLikeOriginal;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist > 0.0f)
                    AdvanceSingleStepMotionV352LikeOriginal(u, dx, dy, dist);
                // Native LocalOrder/ReallyCreatePath owns the next waypoint.
                // Reaching DestX exactly is not a second NIPoints decrement here.
            }
            else if (canNowMove && !hasDestination &&
                     !C2OriginalOrderChainV352.HasPreciseHeadV433LikeOriginal(u.Info))
            {
                // Native no-Dest branch sits INSIDE CANNOWMOVE and excludes
                // OrderType=12. Directly selecting Stand here broke attack/work
                // and posture-transition animations before their frame could finish.
                TryToStandRuntimeV411LikeOriginal(u,true,"MotionHandlerForSingleStepObjects_idle_v433");
                u.OriginalCurUnitSpeedV433LikeOriginal = 0;
            }
            else
            {
                u.OriginalCurUnitSpeedV433LikeOriginal = 0;
            }

            realDir = u.Info != null
                ? u.Info.RealDir
                : (byte)((u.OriginalRealDirPrecise256LikeOriginal >> 8) & 255);
            if (((u.OriginalRealDirPrecise256LikeOriginal >> 8) & 255) != realDir)
                u.OriginalRealDirPrecise256LikeOriginal = realDir << 8;

            if (u.OriginalStandTimeV425LikeOriginal < 2)
            {
                int lx = u.Info != null
                    ? C2OriginalMovementSystemV425LikeOriginal.ResolveLxLikeOriginal(u.Info)
                    : 1;
                float sampleRealX = u.RuntimeRealXLikeOriginal - (lx << 7);
                float sampleRealY = u.RuntimeRealYLikeOriginal - (lx << 7);
                u.OriginalRzV431LikeOriginal = Math.Max(
                    2, ComplexTerrainHeightV430LikeOriginal(u, sampleRealX, sampleRealY));
            }
        }

        // UnitAbility.cpp::UnitLitleShift.
        private void ApplyUnitLitleShiftV431LikeOriginal(C2UnitOriginalRuntime u)
        {
            if (u == null || u.Info == null || C2FormationRuntimeV167LikeOriginal.IsUnlimitedMotionV415LikeOriginal(u.Info)) return;

            C2FormationRuntimeV167LikeOriginal.RuntimeFormationV172LikeOriginal group;
            if (C2FormationRuntimeV167LikeOriginal.TryGetRuntimeGroupByUnitV172LikeOriginal(u.Info, out group) &&
                group != null)
            {
                int idx = group.Units.IndexOf(u.Info);
                if (idx >= 0 && idx < group.Slots.Count)
                {
                    int ux = Mathf.RoundToInt(u.RuntimeRealXLikeOriginal) >> 4;
                    int uy = Mathf.RoundToInt(u.RuntimeRealYLikeOriginal) >> 4;
                    int sx = Mathf.RoundToInt(group.Slots[idx].x) >> 4;
                    int sy = Mathf.RoundToInt(group.Slots[idx].y) >> 4;
                    if (C2OriginalMovementMathV352.Norma(ux - sx, uy - sy) < 8) return;
                }
            }

            const int pushRadiusReal = 30 << 4;
            int sumX = 0;
            int sumY = 0;
            int found = 0;
            // ActiveScenary.cpp::PerformActionOverUnitsInRadius, R=40:
            // NR=(R>>7)+1 == 1, so Rarr[0] visits only the registered >>11 cell.
            // Share the existing periodic MCount/NMSL mirror used by DrawUnits;
            // enumerating every live object here was both different and quadratic.
            int px = Mathf.RoundToInt(u.RuntimeRealXLikeOriginal)>>4;
            int py = Mathf.RoundToInt(u.RuntimeRealYLikeOriginal)>>4;
            if (!_unitDrawCellsLikeOriginal.TryGetValue(UnitCollisionBucketKeyLikeOriginal(px>>7,py>>7),out var cell)) return;
            for (int i = 0; i < cell.Count; i++)
            {
                var ort = cell[i];
                if (ort == null || ort == u || ort.State == C2UnitOriginalState.Death) continue;
                int dxPixels = Mathf.RoundToInt(ort.RuntimeRealXLikeOriginal)/16-px;
                int dyPixels = Mathf.RoundToInt(ort.RuntimeRealYLikeOriginal)/16-py;
                if (dxPixels*dxPixels+dyPixels*dyPixels>=40*40) continue;
                int ddx = Mathf.RoundToInt(u.RuntimeRealXLikeOriginal - ort.RuntimeRealXLikeOriginal);
                int ddy = Mathf.RoundToInt(u.RuntimeRealYLikeOriginal - ort.RuntimeRealYLikeOriginal);
                int ddd = C2OriginalMovementMathV352.Norma(ddx, ddy);
                if (ddd >= pushRadiusReal) continue;
                if (ddd == 0) ddd = 1;
                int ndx = ddx * 4096 / ddd;
                int ndy = ddy * 4096 / ddd;
                int rz = pushRadiusReal - ddd;
                sumX += ndx * rz / pushRadiusReal;
                sumY += ndy * rz / pushRadiusReal;
                found++;
            }
            if (found == 0) return;

            int n = C2OriginalMovementMathV352.Norma(sumX, sumY) + 1;
            if (n > 600)
            {
                sumX = sumX * 600 / n;
                sumY = sumY * 600 / n;
            }
            float beforeX = u.RuntimeRealXLikeOriginal;
            float beforeY = u.RuntimeRealYLikeOriginal;
            u.RuntimeRealXLikeOriginal += sumX / 100;
            u.RuntimeRealYLikeOriginal += sumY / 100;
            C2OriginalMovementSystemV425LikeOriginal.MoveUnitsFieldAfterSingleStepV427LikeOriginal(
                u.Info, beforeX, beforeY, u.RuntimeRealXLikeOriginal, u.RuntimeRealYLikeOriginal);
            if (UseContinuousWorldDeltaForOriginalMotion)
                UpdateRuntimeWorldAndRealContinuousLikeOriginal(u, beforeX, beforeY);
            else
                UpdateRuntimeWorldAndRealLikeOriginal(u);
        }

        // ---- V427 moved from renderer: IsMdSingleStepPassThroughLikeOriginal ----
        private bool IsMdSingleStepPassThroughLikeOriginal(C2UnitOriginalRuntime u)
        {
            return UseMdSingleStepPassThroughLikeOriginal &&
                   u != null && u.Md != null &&
                   string.Equals(u.Md.MotionStyle, "SINGLESTEP", StringComparison.OrdinalIgnoreCase);
        }

        // ---- V427 moved from renderer: AdvanceSingleStepMotionV352LikeOriginal ----
        private void AdvanceSingleStepMotionV352LikeOriginal(
            C2UnitOriginalRuntime u,
            float remainingDxReal,
            float remainingDyReal,
            float remainingDistanceReal)
        {
            using (C2FrameCostProbe.Measure(C2FrameCostProbe.Phase.MotionSingleStep))
            {
            if (u == null || u.Md == null || remainingDistanceReal <= 0.0f) return;

            // COSSACKS2/NewMon.cpp::MotionHandlerForSingleStepObjects.
            // RInFrame begins from MD MotionDist, while movement R begins from the
            // mutable OneObject::GroupSpeed under GETTIRED.
            int rInFrame = Math.Max(1, u.Md.MotionDist);
            int r = u.OriginalGroupSpeedV431LikeOriginal;
            if (u.OriginalBackMotionV431LikeOriginal) r = (r * 2) / 3;
            if (r == 0)
            {
                u.OriginalGroupSpeedV431LikeOriginal = Math.Max(1, u.Md.MotionDist);
                u.OriginalUnitSpeedLikeOriginal = 64;
                r = u.OriginalGroupSpeedV431LikeOriginal;
            }

            // Brigade::UnitsSpeedBonus precedes the tired-brigade penalty in source.
            C2FormationRuntimeV167LikeOriginal.RuntimeFormationV172LikeOriginal speedGroupV431;
            if (u.Info != null &&
                C2FormationRuntimeV167LikeOriginal.TryGetRuntimeGroupByUnitV172LikeOriginal(
                    u.Info, out speedGroupV431) && speedGroupV431 != null)
            {
                int sbV431 = speedGroupV431.UnitsSpeedBonusV431LikeOriginal;
                if (sbV431 < 10) sbV431 = 10;
                if (sbV431 != 100) r = (r * sbV431) / 100;
                if (C2FormationRuntimeV167LikeOriginal.IsFormationTiredV403ELikeOriginal(u.Info) &&
                    u.Info.GetTiredLikeOriginal < 5000)
                    r -= (r >> 2);
            }

            // CurUnitSpeed=UnitSpeed in native source.
            u.OriginalCurUnitSpeedV433LikeOriginal = u.OriginalUnitSpeedLikeOriginal;
            int unitSpeed = u.OriginalCurUnitSpeedV433LikeOriginal;
            if (unitSpeed != 64) r = (r * unitSpeed) >> 6;

            r = (r * u.Md.SpeedScale) >> 8;
            r = (r * u.OriginalMoreCharacterSpeedPercentLikeOriginal) / 100;

            // ActiveAbility->modifyMotionSpeed has no runtime owner in this checkpoint;
            // do not fabricate an effect. No supplied CII gameplay adapter currently
            // installs an ActiveAbility on OneObject-equivalent runtimes.

            if (u.Md.SpeedScaleOnTrees != 0 && u.Info != null)
            {
                int lxTreeV431 = C2OriginalMovementSystemV425LikeOriginal.ResolveLxLikeOriginal(u.Info);
                int oxTreeV431 = (Mathf.RoundToInt(u.RuntimeRealXLikeOriginal) - (lxTreeV431 << 7)) >> 8;
                int oyTreeV431 = (Mathf.RoundToInt(u.RuntimeRealYLikeOriginal) - (lxTreeV431 << 7)) >> 8;
                if (C2OriginalMovementSystemV425LikeOriginal.CheckPtV425LikeOriginal(oxTreeV431, oyTreeV431, 2) &&
                    !C2OriginalMovementSystemV425LikeOriginal.CheckPtV425LikeOriginal(oxTreeV431, oyTreeV431, 0))
                    r = (r * u.Md.SpeedScaleOnTrees) >> 8;
            }

            // NewState is represented by the active combat posture in this runtime.
            // Rate[] is parsed from the original MD and defaults to 16.
            int state = u.PostureWeaponTypeLikeOriginal >= 0 ? u.PostureWeaponTypeLikeOriginal + 1 : 0;
            int autoSpeedAnm = u.Md.Rate != null && u.Md.Rate.Length > 0 ? u.Md.Rate[0] : 16;
            if (state > 0 && u.Md.Rate != null && state - 1 < u.Md.Rate.Length)
            {
                int rate = u.Md.Rate[state - 1];
                r = (r * rate) >> 4;
                int spd = (unitSpeed * rate) >> 4;
                if (autoSpeedAnm > 16)
                {
                    if (spd > 80)
                        rInFrame = (rInFrame * autoSpeedAnm) >> 4;
                }
                else
                {
                    rInFrame = (rInFrame * rate) >> 4;
                }
            }
            else if (autoSpeedAnm > 16 && unitSpeed > 80)
            {
                rInFrame = (rInFrame * autoSpeedAnm) >> 4;
            }

            int dx = Mathf.RoundToInt(remainingDxReal);
            int dy = Mathf.RoundToInt(remainingDyReal);
            // Original computes Nrm BEFORE BoidsSingleStep2 and never recomputes it.
            int nrm = C2OriginalMovementMathV352.Norma(dx, dy);
            if (nrm <= 0) return;

            if (UseMdBoidsSteeringLikeOriginal &&
                u.Md.BoidsMoving &&
                _units.Count < Mathf.Max(1, OriginalBoidsOffLimitLikeOriginal) &&
                nrm > 16 * 16 &&
                !u.PreciseBornPathLikeOriginal)
            {
                int addSpeed = 0;
                ApplyOriginalBoidsSingleStep2V352LikeOriginal(u, ref dx, ref dy, ref addSpeed);
                r += addSpeed;
                if (r < 1) r = 1;
            }

            int rs = r;
            // GameSpeed=256 in the C2 source, therefore (R*GameSpeed)>>8 == R.
            bool fixEnd = false;
            if (nrm < rs)
            {
                rs = nrm;
                fixEnd = true;
            }

            byte bestDir = C2OriginalMovementMathV352.GetDir(dx, dy);
            if (u.OriginalBackMotionV431LikeOriginal)
                bestDir = unchecked((byte)(bestDir + 128));
            byte currentDir = u.Info != null ? u.Info.RealDir : (byte)((u.OriginalRealDirPrecise256LikeOriginal >> 8) & 255);
            int precise = u.OriginalRealDirPrecise256LikeOriginal;
            if (((precise >> 8) & 255) != currentDir)
                precise = currentDir << 8;

            int ddir = (sbyte)(bestDir - currentDir);
            int mr = Math.Max(1, u.Md.MinRotator);
            if (nrm < rs * 4) mr <<= 1;

            // COSSACKS2/NewMon.cpp::MotionHandlerForSingleStepObjects: rotation
            // animations take priority over RotationAtPlaceSpeed while moving.
            // Cavalry MDs (AusKDrg/AusKGus/AusKKir/AusKUln) provide @ROTATEL/@ROTATER
            // plus RPLACESPEED; the original does NOT insert RotUnit for every course
            // correction when those animations are available.
            // Original reads BR->NewBOrder directly. Do not run a Unity
            // GetComponent lookup for every moving unit every simulation tick.
            bool goOnRoadV352 = u.OriginalGoOnRoadLikeOriginal;
            int rotationAtPlaceSpeedV352 = goOnRoadV352 ? 0 : u.Md.RotationAtPlaceSpeed;

            // NewMonster::GetAnimation(anm_RotateL/R) is a direct table lookup.
            int rotateLIndexV357 = u.Md.RotateLAnimationIndexLikeOriginal;
            int rotateRIndexV357 = u.Md.RotateRAnimationIndexLikeOriginal;

            bool haveRotateAnimationsV357 =
                !goOnRoadV352 &&
                u.Md.HaveRotateAnimationsLikeOriginal &&
                rotateLIndexV357 >= 0 && rotateLIndexV357 < u.Md.Animations.Count &&
                u.Md.Animations[rotateLIndexV357] != null &&
                u.Md.Animations[rotateLIndexV357].Frames.Count > 0 &&
                !C2BuildingMotionFieldV1BlockedForTurnLikeOriginal(u);
            bool useRotateAnimationsV357 = haveRotateAnimationsV357 && nrm > 120 * 16;

            // C2 NewMon.cpp:17970 executes an existing RotUnitLink before motion.
            // Having ROTATEL/ROTATER assets does not cancel a pending turn, especially
            // inside the 120-pixel range where those moving-turn animations are unused.
            if (u.SingleStepRotateAtPlaceActiveV352LikeOriginal)
            {
                if (rotationAtPlaceSpeedV352 <= 0)
                    u.SingleStepRotateAtPlaceActiveV352LikeOriginal = false;
                else
                {
                    AdvanceSingleStepRotUnitLinkV352LikeOriginal(u);
                    return;
                }
            }

            int goAnimIndexV357 = -1;
            bool currentRotateLV357 = u.CurrentAnimIndex == rotateLIndexV357;
            bool currentRotateRV357 = u.CurrentAnimIndex == rotateRIndexV357;
            bool enterDoRotV357 = false;

            if (Math.Abs(ddir) < mr)
            {
                int rsFlagV357 = 0;
                if (useRotateAnimationsV357)
                {
                    if (!(currentRotateLV357 || currentRotateRV357))
                    {
                        currentDir = C2OriginalMovementMathV352.Quantize16(currentDir);
                        precise = currentDir << 8;
                    }
                    else
                    {
                        enterDoRotV357 = true;
                    }
                    rsFlagV357 = 1;
                }

                if (rsFlagV357 == 0)
                {
                    currentDir = bestDir;
                    precise = currentDir << 8;
                    if (haveRotateAnimationsV357 && nrm > 120 * 16)
                    {
                        currentDir = C2OriginalMovementMathV352.Quantize16(currentDir);
                        precise = currentDir << 8;
                    }
                }
            }
            else
            {
                enterDoRotV357 = true;
            }

            if (enterDoRotV357)
            {
                if (useRotateAnimationsV357)
                {
                    bool nowRotateV357 = currentRotateLV357 || currentRotateRV357;
                    if (!nowRotateV357)
                    {
                        currentDir = C2OriginalMovementMathV352.Quantize16(currentDir);
                        precise = currentDir << 8;

                        // Source frame-coherency guard before entering ROTATEL/ROTATER.
                        int rotateLNFramesV357 = Math.Max(1, u.Md.Animations[rotateLIndexV357].Frames.Count);
                        AnimModel currentAnimV357 = CurrentAnim(u);
                        int currentNFramesV357 = currentAnimV357 != null ? Math.Max(1, currentAnimV357.Frames.Count) : 1;
                        int currentFrameV357 = currentAnimV357 != null ? FixedFrameIndexLikeOriginal(u, currentAnimV357) : 0;
                        ushort phaseWordV357 = ddir < 0
                            ? unchecked((ushort)(64 * 256 - precise))
                            : unchecked((ushort)(precise - 64 * 256));
                        int cfV357 = ((phaseWordV357 * rotateLNFramesV357) >> 16) % currentNFramesV357;
                        if (currentNFramesV357 > 1 &&
                            (cfV357 - currentFrameV357 + (currentNFramesV357 << 4)) % currentNFramesV357 >= 2)
                            ddir = 0;
                    }

                    if (ddir < 0)
                    {
                        if (!currentRotateRV357 && rotateLIndexV357 >= 0)
                        {
                            goAnimIndexV357 = rotateLIndexV357;
                            int nfV357 = Math.Max(1, u.Md.Animations[rotateLIndexV357].Frames.Count);
                            int ddV357 = (65536 / nfV357) * rs / Math.Max(1, rInFrame);
                            precise = (precise - ddV357) & 0xFFFF;
                            currentDir = (byte)((precise >> 8) & 255);
                        }
                        else
                        {
                            goAnimIndexV357 = ResolveRuntimeMotionAnimationIndexV223LikeOriginal(u);
                            SyncMotionPhaseAfterRotateV357LikeOriginal(u, goAnimIndexV357, rInFrame);
                        }
                    }
                    else if (ddir > 0)
                    {
                        if (!currentRotateLV357 && rotateRIndexV357 >= 0)
                        {
                            goAnimIndexV357 = rotateRIndexV357;
                            int nfV357 = Math.Max(1, u.Md.Animations[rotateRIndexV357].Frames.Count);
                            int ddV357 = (65536 / nfV357) * rs / Math.Max(1, rInFrame);
                            precise = (precise + ddV357) & 0xFFFF;
                            currentDir = (byte)((precise >> 8) & 255);
                        }
                        else
                        {
                            goAnimIndexV357 = ResolveRuntimeMotionAnimationIndexV223LikeOriginal(u);
                            SyncMotionPhaseAfterRotateV357LikeOriginal(u, goAnimIndexV357, rInFrame);
                        }
                    }
                    else if (nowRotateV357)
                    {
                        goAnimIndexV357 = ResolveRuntimeMotionAnimationIndexV223LikeOriginal(u);
                        SyncMotionPhaseAfterRotateV357LikeOriginal(u, goAnimIndexV357, rInFrame);
                    }
                }
                else if (rotationAtPlaceSpeedV352 > 0)
                {
                    // Original fallback only when HaveRotAnm is false.
                    BeginSingleStepRotUnitV352LikeOriginal(u, bestDir);
                    return;
                }
                else
                {
                    if (mr > 8)
                    {
                        rs = 0;
                        if (ddir < 0) currentDir = (byte)(currentDir - mr);
                        else currentDir = (byte)(currentDir + mr);
                        precise = currentDir << 8;
                    }
                    else
                    {
                        if (ddir < 0) precise -= mr << 5;
                        else precise += mr << 5;
                        currentDir = (byte)((precise >> 8) & 255);
                    }
                }
            }

            u.OriginalRealDirPrecise256LikeOriginal = precise;
            SetRuntimeFacingLikeOriginal(u, currentDir);
            u.OriginalRealDirPrecise256LikeOriginal = precise;

            // Source translates only while LocalNewState==NewState.
            if (u.LocalPostureWeaponTypeV411LikeOriginal != u.PostureWeaponTypeLikeOriginal)
            {
                TryToStandRuntimeV411LikeOriginal(
                    u, false, "MotionHandlerForSingleStepObjects_state_mismatch_v431");
                return;
            }

            float beforeRealX = u.RuntimeRealXLikeOriginal;
            float beforeRealY = u.RuntimeRealYLikeOriginal;
            byte moveDirV431 = u.OriginalBackMotionV431LikeOriginal
                ? unchecked((byte)(currentDir + 128))
                : currentDir;
            float stepX = fixEnd ? u.MoveTargetRealXLikeOriginal - beforeRealX
                : (rs * C2OriginalMovementMathV352.TCos[moveDirV431]) >> 8;
            float stepY = fixEnd ? u.MoveTargetRealYLikeOriginal - beforeRealY
                : (rs * C2OriginalMovementMathV352.TSin[moveDirV431]) >> 8;
            float stepLength = Mathf.Sqrt(stepX * stepX + stepY * stepY);
            C2NeutralPeasantUnitInfoV2LikeOriginal contactEnemyV414 = null;
            if (stepLength > 0.0f && u.Info != null && !u.Md.DontStuckInEnemyLikeOriginal &&
                C2FormationRuntimeV167LikeOriginal.UnitsFieldCheckBarForMotionV415LikeOriginal(u.Info))
            {
                // NewMon.cpp::MotionHandlerForSingleStepObjects:
                //   if(!DontStuckInEnemy && UnitsField.CheckBar(current bar))
                //       ENMID=CheckMotionThroughEnemyAbility(OB,rx1,ry1);
                int rx1V414 = Mathf.RoundToInt(beforeRealX + stepX);
                int ry1V414 = Mathf.RoundToInt(beforeRealY + stepY);
                contactEnemyV414 = C2FormationRuntimeV167LikeOriginal.CheckMotionThroughEnemyAbilityV414LikeOriginal(
                    u.Info, rx1V414, ry1V414);
            }
            if (stepLength > 0.0f)
            {
                // NewMon.cpp::MotionHandlerForSingleStepObjects commits rx1/ry1 directly.
                // Static MFIELDS are resolved by CreatePath/ReallyCreatePath; SINGLESTEP does
                // not run a second Unity building/water slider on every simulation step.
                u.RuntimeRealXLikeOriginal = beforeRealX + stepX;
                u.RuntimeRealYLikeOriginal = beforeRealY + stepY;
                // NewMon.cpp updates UnitsField immediately inside this same object step:
                // BClrBar(old x/y,Lx) -> recalc x/y -> BSetBar(new x/y,Lx).  This
                // matters when another unit is processed later in the same 40 ms quantum.
                C2OriginalMovementSystemV425LikeOriginal.MoveUnitsFieldAfterSingleStepV427LikeOriginal(
                    u.Info, beforeRealX, beforeRealY,
                    u.RuntimeRealXLikeOriginal, u.RuntimeRealYLikeOriginal);
                // Native TotalPath is signed for BackMotion.
                u.TotalPathLikeOriginal += u.OriginalBackMotionV431LikeOriginal ? -rs : rs;
            }

            u.MoveRInFrameLikeOriginal = Math.Max(1, rInFrame);
            if (UseContinuousWorldDeltaForOriginalMotion)
                UpdateRuntimeWorldAndRealContinuousLikeOriginal(u, beforeRealX, beforeRealY);
            else
                UpdateRuntimeWorldAndRealLikeOriginal(u);

            // NewMon.cpp: after UnitsField is moved to the new cell, a returned ENMID
            // receives AttackObj(ENMID,128+15,1,1). LOWCOLLISION units outside
            // aggressive state slow down instead; ordinary infantry installs the
            // contact attack.
            if (contactEnemyV414 != null && u.Info != null &&
                !C2FormationRuntimeV167LikeOriginal.GetNoSearchVictimV403LikeOriginal(u.Info))
            {
                if (u.Md.LowCollisionLikeOriginal && u.Info.ActivityStateV413LikeOriginal != 2)
                    u.OriginalCurUnitSpeedV433LikeOriginal = 20;
                else
                    C2FormationRuntimeV167LikeOriginal.CommitSingleStepContactAttackV414LikeOriginal(
                        u.Info, contactEnemyV414);
            }
            else if (u.Md.LowCollisionLikeOriginal)
            {
                u.OriginalCurUnitSpeedV433LikeOriginal = 64;
            }

            int motion = ResolveRuntimeMotionAnimationIndexV223LikeOriginal(u);
            int desiredMotionAnimV357 = goAnimIndexV357 >= 0 ? goAnimIndexV357 : motion;
            if (desiredMotionAnimV357 >= 0 &&
                (u.State != C2UnitOriginalState.Motion || u.CurrentAnimIndex != desiredMotionAnimV357))
            {
                SelectAnimationStateLikeOriginal(
                    u, C2UnitOriginalState.Motion, desiredMotionAnimV357, false,
                    goAnimIndexV357 >= 0
                        ? "MotionHandlerForSingleStepObjects_rotate_anim_v357"
                        : "MotionHandlerForSingleStepObjects_v357");
            }
            }
        }

        // ---- V427 moved from renderer: SyncMotionPhaseAfterRotateV357LikeOriginal ----
        private void SyncMotionPhaseAfterRotateV357LikeOriginal(
            C2UnitOriginalRuntime u,
            int motionIndex,
            int rInFrame)
        {
            if (u == null || u.Md == null || motionIndex < 0 || motionIndex >= u.Md.Animations.Count)
                return;
            AnimModel motion = u.Md.Animations[motionIndex];
            int nf = motion != null ? motion.Frames.Count : 0;
            if (nf <= 0) return;
            AnimModel current = CurrentAnim(u);
            int currentFrame = current != null ? FixedFrameIndexLikeOriginal(u, current) : 0;
            int rf = Math.Max(1, rInFrame);
            int fr = (((Math.Abs(Mathf.RoundToInt(u.TotalPathLikeOriginal + 100000.0f)) << 8) / rf) % (nf << 8)) >> 8;
            int df = fr - (currentFrame % nf);
            u.TotalPathLikeOriginal -= df * rf;
        }

        // ---- V427 moved from renderer: BeginSingleStepRotUnitV352LikeOriginal ----
        private void BeginSingleStepRotUnitV352LikeOriginal(C2UnitOriginalRuntime u, byte bestDir)
        {
            if (u == null || u.Md == null) return;
            byte current = u.Info != null ? u.Info.RealDir : (byte)((u.OriginalRealDirPrecise256LikeOriginal >> 8) & 255);
            // Brigade.cpp::RotUnit: RotSpeed first quantises the current direction and
            // the requested direction to the 16-direction grid.
            current = C2OriginalMovementMathV352.Quantize16(current);
            u.OriginalRealDirPrecise256LikeOriginal = current << 8;
            SetRuntimeFacingLikeOriginal(u, current);
            u.OriginalRealDirPrecise256LikeOriginal = current << 8;

            byte target = C2OriginalMovementMathV352.Quantize16(bestDir);
            if (current == target)
            {
                // Exact RotUnit fast path: restore the unquantised requested Dir and
                // return without creating the type-1 rotate order. Movement resumes next tick.
                SetRuntimeFacingLikeOriginal(u, bestDir);
                u.OriginalRealDirPrecise256LikeOriginal = bestDir << 8;
                return;
            }

            u.SingleStepRotateAtPlaceTargetV352LikeOriginal = target;
            u.SingleStepRotateAtPlaceActiveV352LikeOriginal = true;
        }

        // ---- V427 moved from renderer: AdvanceSingleStepRotUnitLinkV352LikeOriginal ----
        private void AdvanceSingleStepRotUnitLinkV352LikeOriginal(C2UnitOriginalRuntime u)
        {
            if (u == null || u.Md == null || !u.SingleStepRotateAtPlaceActiveV352LikeOriginal) return;
            int targetPrecise = u.SingleStepRotateAtPlaceTargetV352LikeOriginal << 8;
            int precise = u.OriginalRealDirPrecise256LikeOriginal & 0xFFFF;
            short dd = unchecked((short)(precise - targetPrecise));
            int dr = Math.Max(1, u.Md.RotationAtPlaceSpeed); // GameSpeed==256 in C2.

            if (Math.Abs((int)dd) < dr)
            {
                precise = targetPrecise & 0xFFFF;
                u.SingleStepRotateAtPlaceActiveV352LikeOriginal = false;
            }
            else if (dd < 0)
                precise = (precise + dr) & 0xFFFF;
            else
                precise = (precise - dr) & 0xFFFF;

            u.OriginalRealDirPrecise256LikeOriginal = precise;
            byte dir = (byte)((precise >> 8) & 255);
            SetRuntimeFacingLikeOriginal(u, dir);
            u.OriginalRealDirPrecise256LikeOriginal = precise;
            ApplyRotateAtPlaceAnimationFrameV352LikeOriginal(u, precise);
        }

        // ---- V427 moved from renderer: AdvanceFinalRotUnitV352LikeOriginal ----
        private bool AdvanceFinalRotUnitV352LikeOriginal(C2UnitOriginalRuntime u, byte requestedDir)
        {
            if (u == null || u.Md == null) return true;

            bool goOnRoadV352 = u.OriginalGoOnRoadLikeOriginal;
            int effectiveRotationAtPlaceSpeedV352 = goOnRoadV352 ? 0 : u.Md.RotationAtPlaceSpeed;

            if (!u.FinalRotUnitActiveV352LikeOriginal)
            {
                int rotSpeed = effectiveRotationAtPlaceSpeedV352;
                byte current = u.Info != null ? u.Info.RealDir : (byte)((u.OriginalRealDirPrecise256LikeOriginal >> 8) & 255);
                if (rotSpeed > 0)
                {
                    current = C2OriginalMovementMathV352.Quantize16(current);
                    SetRuntimeFacingLikeOriginal(u, current);
                    u.OriginalRealDirPrecise256LikeOriginal = current << 8;
                    byte target16 = C2OriginalMovementMathV352.Quantize16(requestedDir);
                    if (current == target16)
                    {
                        SetRuntimeFacingLikeOriginal(u, requestedDir);
                        u.OriginalRealDirPrecise256LikeOriginal = requestedDir << 8;
                        return true;
                    }
                    u.FinalRotUnitTargetV352LikeOriginal = target16;
                }
                else
                {
                    u.FinalRotUnitTargetV352LikeOriginal = requestedDir;
                }
                u.FinalRotUnitActiveV352LikeOriginal = true;
                // RotUnit creates OrderType 12 now; RotUnitLink performs its first turn next tick.
                return false;
            }

            int rotationAtPlaceSpeed = effectiveRotationAtPlaceSpeedV352;
            if (rotationAtPlaceSpeed > 0)
            {
                int targetPrecise = u.FinalRotUnitTargetV352LikeOriginal << 8;
                int precise = u.OriginalRealDirPrecise256LikeOriginal & 0xFFFF;
                short dd = unchecked((short)(precise - targetPrecise));
                int dr = Math.Max(1, rotationAtPlaceSpeed);
                if (Math.Abs((int)dd) < dr)
                {
                    precise = targetPrecise & 0xFFFF;
                    u.OriginalRealDirPrecise256LikeOriginal = precise;
                    SetRuntimeFacingLikeOriginal(u, (byte)((precise >> 8) & 255));
                    u.OriginalRealDirPrecise256LikeOriginal = precise;
                    u.FinalRotUnitActiveV352LikeOriginal = false;
                    return true;
                }
                if (dd < 0) precise = (precise + dr) & 0xFFFF;
                else precise = (precise - dr) & 0xFFFF;
                u.OriginalRealDirPrecise256LikeOriginal = precise;
                SetRuntimeFacingLikeOriginal(u, (byte)((precise >> 8) & 255));
                u.OriginalRealDirPrecise256LikeOriginal = precise;
                ApplyRotateAtPlaceAnimationFrameV352LikeOriginal(u, precise);
                return false;
            }

            int mrot = Math.Max(1, u.Md.MinRotator);
            byte realDir = u.Info != null ? u.Info.RealDir : (byte)((u.OriginalRealDirPrecise256LikeOriginal >> 8) & 255);
            byte target = u.FinalRotUnitTargetV352LikeOriginal;
            sbyte delta = unchecked((sbyte)(realDir - target));
            if (Math.Abs((int)delta) <= mrot)
            {
                SetRuntimeFacingLikeOriginal(u, target);
                u.OriginalRealDirPrecise256LikeOriginal = target << 8;
                u.FinalRotUnitActiveV352LikeOriginal = false;
                return true;
            }

            int p = u.OriginalRealDirPrecise256LikeOriginal & 0xFFFF;
            int dd2 = mrot << 8; // Mrot*GameSpeed, GameSpeed=256.
            if (delta > 0) p = (p - dd2) & 0xFFFF;
            else p = (p + dd2) & 0xFFFF;
            u.OriginalRealDirPrecise256LikeOriginal = p;
            SetRuntimeFacingLikeOriginal(u, (byte)((p >> 8) & 255));
            u.OriginalRealDirPrecise256LikeOriginal = p;
            return false;
        }

        // ---- V427 moved from renderer: AdvanceRuntimeAttackFacingV411LikeOriginal ----
        internal bool AdvanceRuntimeAttackFacingV411LikeOriginal(
            C2UnitOriginalRuntime u,
            byte enemyDir,
            int needState)
        {
            if (u == null || u.Md == null || u.State == C2UnitOriginalState.Death)
                return false;

            byte current = u.Info != null
                ? u.Info.RealDir
                : (byte)((u.OriginalRealDirPrecise256LikeOriginal >> 8) & 255);
            sbyte signedDelta = unchecked((sbyte)(enemyDir - current));
            int ddir = signedDelta;
            int motionStyle = MotionStyleCodeV411LikeOriginal(u.Md.MotionStyle);
            bool attackMove = needState >= 0 &&
                              needState < u.Md.PostureMotionLAnimationIndicesV376LikeOriginal.Length &&
                              u.Md.PostureMotionLAnimationIndicesV376LikeOriginal[needState] >= 0;

            int mrot = Math.Max(1, u.Md.MinRotator);
            // Exact NewMon.cpp branch is MotionStyle==2 (SHEEPS), NOT SINGLESTEP.
            if (motionStyle == 2)
            {
                mrot /= 8; // GameSpeed==256 in this bridge.
                if (mrot < 1) mrot = 1;
                if (mrot > 8) mrot = 8;
            }
            int tolerance = mrot + (mrot >> 1);
            if (u.Md.RotationAtPlaceSpeed > 0)
            {
                mrot = 1;
                tolerance = 1;
            }
            if (tolerance > 16) tolerance = 16;

            // MotionStyle==3 / COMPLEXROTATE (NewArt in AttackObjLink).
            // Preserve the original temporary state 2 rotation pipeline:
            // NewState/GroundState=2 -> PMotionL1/PMotionR1 -> neutral -> attack.
            if (motionStyle == 3)
            {
                int enemy16 = ((enemyDir + 8) >> 4) & 15;
                int current16 = ((current + 8) >> 4) & 15;
                int left = u.Md.PostureMotionLAnimationIndicesV376LikeOriginal.Length > 1
                    ? u.Md.PostureMotionLAnimationIndicesV376LikeOriginal[1]
                    : -1;
                int right = u.Md.PostureMotionRAnimationIndicesV411LikeOriginal.Length > 1
                    ? u.Md.PostureMotionRAnimationIndicesV411LikeOriginal[1]
                    : -1;

                if (enemy16 == current16)
                {
                    ddir = 0;
                    // NewMon.cpp: if(NewState==2){ NewState=0; GroundState=0;
                    // TryToStand(OBJ,0); return; }
                    if (u.PostureWeaponTypeLikeOriginal == 1)
                    {
                        u.PostureWeaponTypeLikeOriginal = -1;
                        if (u.Info != null)
                        {
                            u.Info.NewStateV396LikeOriginal = 0;
                            u.Info.GroundStateV396LikeOriginal = 0;
                        }
                        TryToStandRuntimeV411LikeOriginal(
                            u, false, "AttackObjLink_NewArt_bucket_ready_to_neutral_v412");
                        return false;
                    }
                }
                else
                {
                    // NewMon.cpp first transforms the attacker into state 2.
                    if (u.LocalPostureWeaponTypeV411LikeOriginal != 1)
                    {
                        u.PostureWeaponTypeLikeOriginal = 1;
                        if (u.Info != null)
                        {
                            u.Info.NewStateV396LikeOriginal = 2;
                            u.Info.GroundStateV396LikeOriginal = 2;
                        }
                        TryToStandRuntimeV411LikeOriginal(
                            u, false, "AttackObjLink_NewArt_enter_state2_v412");
                        return false;
                    }

                    // The outer C++ angle branch is reached only after the current
                    // breakable/non-attack animation is eligible for analysis. Do
                    // not apply a PMotion step every Unity frame.
                    if (u.FrameFinishedLikeOriginal &&
                        (u.CurrentAnimIndex == left || u.CurrentAnimIndex == right))
                    {
                        if (u.CurrentAnimIndex == left)
                        {
                            // PMotionL1 applies +16 when its step completes.
                            current = unchecked((byte)(current + 16));
                            SetRuntimeFacingLikeOriginal(u, current);
                            u.OriginalRealDirPrecise256LikeOriginal = current << 8;
                        }
                        // PMotionR1 applies -16 when the clip is installed below;
                        // retail does not subtract it a second time on completion.
                        TryToStandRuntimeV411LikeOriginal(
                            u, false, "AttackObjLink_NewArt_rotation_step_finished_v412");
                        return false;
                    }

                    signedDelta = unchecked((sbyte)(enemyDir - current));
                    ddir = signedDelta;
                    int turnAnim = ddir > 0 ? left : right;
                    if (turnAnim >= 0)
                    {
                        if (u.CurrentAnimIndex != turnAnim || u.State != C2UnitOriginalState.Motion)
                        {
                            if (ddir < 0)
                            {
                                // Exact PMotionR branch: direction is decremented
                                // immediately when the right-turn clip is installed.
                                current = unchecked((byte)(current - 16));
                                SetRuntimeFacingLikeOriginal(u, current);
                                u.OriginalRealDirPrecise256LikeOriginal = current << 8;
                            }
                            SelectAnimationStateLikeOriginal(
                                u, C2UnitOriginalState.Motion, turnAnim, true,
                                ddir > 0
                                    ? "AttackObjLink_PMotionL1_v412"
                                    : "AttackObjLink_PMotionR1_v412");
                        }
                        return false;
                    }

                    // The original requires PMotion state-1 clips for NewArt. If
                    // the MD adapter cannot resolve them, do not invent a snap or a
                    // synthetic 16-degree rotation: block attack and expose the data
                    // mismatch to the audit instead.
                    return false;
                }
            }

            if (Math.Abs(ddir) <= tolerance)
            {
                SetRuntimeFacingLikeOriginal(u, enemyDir);
                u.OriginalRealDirPrecise256LikeOriginal = enemyDir << 8;
                u.AttackRotateAtPlaceActiveV410LikeOriginal = false;
                return true;
            }

            // AttackObjLink: RotationAtPlaceSpeed -> RotUnit(OBJ,EnDir,1).
            if (u.Md.RotationAtPlaceSpeed > 0)
            {
                byte target16 = C2OriginalMovementMathV352.Quantize16(enemyDir);
                if (!u.AttackRotateAtPlaceActiveV410LikeOriginal ||
                    u.AttackRotateAtPlaceTargetV410LikeOriginal != target16)
                {
                    current = C2OriginalMovementMathV352.Quantize16(current);
                    SetRuntimeFacingLikeOriginal(u, current);
                    u.OriginalRealDirPrecise256LikeOriginal = current << 8;
                    if (current == target16)
                    {
                        SetRuntimeFacingLikeOriginal(u, enemyDir);
                        u.OriginalRealDirPrecise256LikeOriginal = enemyDir << 8;
                        u.AttackRotateAtPlaceActiveV410LikeOriginal = false;
                        return true;
                    }
                    u.AttackRotateAtPlaceTargetV410LikeOriginal = target16;
                    u.AttackRotateAtPlaceActiveV410LikeOriginal = true;
                    return false;
                }

                int targetPrecise = target16 << 8;
                int precise = u.OriginalRealDirPrecise256LikeOriginal & 0xFFFF;
                short dd = unchecked((short)(precise - targetPrecise));
                int dr = Math.Max(1, u.Md.RotationAtPlaceSpeed); // *GameSpeed/256.
                if (Math.Abs((int)dd) < dr)
                {
                    precise = targetPrecise & 0xFFFF;
                    u.OriginalRealDirPrecise256LikeOriginal = precise;
                    SetRuntimeFacingLikeOriginal(u, (byte)((precise >> 8) & 255));
                    u.OriginalRealDirPrecise256LikeOriginal = precise;
                    u.AttackRotateAtPlaceActiveV410LikeOriginal = false;
                    return false; // next AttackObjLink pass applies exact EnDir.
                }
                if (dd < 0) precise = (precise + dr) & 0xFFFF;
                else precise = (precise - dr) & 0xFFFF;
                u.OriginalRealDirPrecise256LikeOriginal = precise;
                SetRuntimeFacingLikeOriginal(u, (byte)((precise >> 8) & 255));
                u.OriginalRealDirPrecise256LikeOriginal = precise;
                ApplyRotateAtPlaceAnimationFrameV352LikeOriginal(u, precise);
                return false;
            }

            u.AttackRotateAtPlaceActiveV410LikeOriginal = false;

            // NewMon.cpp rotates by MRot whether AMove exists or not; AMove only
            // changes the surrounding approach/motion branch. Preserve that exact
            // decision and never snap across the remaining angle.
            byte next = unchecked((byte)(current + (ddir > 0 ? mrot : -mrot)));
            SetRuntimeFacingLikeOriginal(u, next);
            u.OriginalRealDirPrecise256LikeOriginal = next << 8;
            if (attackMove)
            {
                // Keep the posture-motion gait available to the renderer while
                // rotating in attack range. The direction itself remains RotateMon.
                int gait = ddir > 0
                    ? u.Md.PostureMotionLAnimationIndicesV376LikeOriginal[Mathf.Clamp(needState, 0, 15)]
                    : u.Md.PostureMotionRAnimationIndicesV411LikeOriginal[Mathf.Clamp(needState, 0, 15)];
                if (gait >= 0 && u.State == C2UnitOriginalState.Motion && u.CurrentAnimIndex != gait)
                    SelectAnimationStateLikeOriginal(
                        u, C2UnitOriginalState.Motion, gait, false,
                        "AttackObjLink_AMove_rotate_v411");
            }
            return false;
        }

        // ---- V427 moved from renderer: AdvanceRuntimeAttackFacingV410LikeOriginal ----
        internal bool AdvanceRuntimeAttackFacingV410LikeOriginal(
            C2UnitOriginalRuntime u,
            byte enemyDir)
        {
            return AdvanceRuntimeAttackFacingV411LikeOriginal(u, enemyDir, 0);
        }

        // ---- V427 moved from renderer: MotionStyleCodeV411LikeOriginal ----
        private static int MotionStyleCodeV411LikeOriginal(string style)
        {
            if (string.Equals(style, "FASTROTATE&MOVE", StringComparison.OrdinalIgnoreCase)) return 0;
            if (string.Equals(style, "SLOWROTATE", StringComparison.OrdinalIgnoreCase)) return 1;
            if (string.Equals(style, "SHEEPS", StringComparison.OrdinalIgnoreCase)) return 2;
            if (string.Equals(style, "COMPLEXROTATE", StringComparison.OrdinalIgnoreCase)) return 3;
            if (string.Equals(style, "ROTATE&MOVE", StringComparison.OrdinalIgnoreCase)) return 4;
            if (string.Equals(style, "NEWSHEEPS", StringComparison.OrdinalIgnoreCase)) return 5;
            if (string.Equals(style, "COMPLEXOBJECT", StringComparison.OrdinalIgnoreCase)) return 6;
            if (string.Equals(style, "SINGLESTEP", StringComparison.OrdinalIgnoreCase)) return 7;
            if (string.Equals(style, "FLY", StringComparison.OrdinalIgnoreCase)) return 8;
            return 0;
        }

        // ---- V427 moved from renderer: ApplyRotateAtPlaceAnimationFrameV352LikeOriginal ----
        private void ApplyRotateAtPlaceAnimationFrameV352LikeOriginal(C2UnitOriginalRuntime u, int precise)
        {
            if (u == null || u.Md == null) return;
            int rotate = ResolveAnimationIndexLikeOriginal(u.Md, "@ROTATEATPLACE");
            if (rotate < 0) rotate = ResolveAnimationIndexLikeOriginal(u.Md, "#ROTATEATPLACE");
            if (rotate < 0) rotate = ResolveAnimationIndexLikeOriginal(u.Md, "ROTATEATPLACE");
            if (rotate < 0 || rotate >= u.Md.Animations.Count) return;
            AnimModel a = u.Md.Animations[rotate];
            int nf = a != null ? a.Frames.Count : 0;
            if (nf <= 0) return;
            if (u.CurrentAnimIndex != rotate)
                SelectAnimationStateLikeOriginal(u, C2UnitOriginalState.Motion, rotate, false, "RotUnitLink_RotateAtPlace_v352");
            int wordDelta = unchecked((ushort)(precise - 64 * 256));
            u.CurrentFrameLong = (wordDelta * nf) >> 8;
            u.FrameFinishedLikeOriginal = false;
        }

        // ---- V427 moved from renderer: ApplyOriginalBoidsSingleStep2V352LikeOriginal ----
        private void ApplyOriginalBoidsSingleStep2V352LikeOriginal(
            C2UnitOriginalRuntime u,
            ref int dx,
            ref int dy,
            ref int changeSpeed)
        {
            if (u == null) return;

            dx >>= 4;
            dy >>= 4;
            int idd = C2OriginalMovementMathV352.Norma(dx, dy) + 1;
            if (idd <= 16) return;

            int mainDirNorma = Math.Max(1, Mathf.RoundToInt(OriginalBoidsMainDirectionNormLikeOriginal));
            int idx = (dx * mainDirNorma) / idd;
            int idy = (dy * mainDirNorma) / idd;

            int p = u.UnitOrder << 2;
            int ndx = p >= 0 && p + 3 < _originalBoidsCoordAndForceLikeOriginal.Length
                ? Mathf.RoundToInt(_originalBoidsCoordAndForceLikeOriginal[p + 2]) : 0;
            int ndy = p >= 0 && p + 3 < _originalBoidsCoordAndForceLikeOriginal.Length
                ? Mathf.RoundToInt(_originalBoidsCoordAndForceLikeOriginal[p + 3]) : 0;
            int ndd = C2OriginalMovementMathV352.Norma(ndx, ndy) + 1;
            int densNorma = Math.Max(1, Mathf.RoundToInt(OriginalBoidsDensityNormLikeOriginal));
            if (ndd > densNorma)
            {
                ndx = (ndx * densNorma) / ndd;
                ndy = (ndy * densNorma) / ndd;
            }

            int changeSpeedW = Mathf.RoundToInt(OriginalBoidsChangeSpeedWeightLikeOriginal);
            changeSpeed = ((changeSpeedW * (idx * ndx + idy * ndy)) / mainDirNorma) / 1000;

            // MFIELDS.CheckPt(OB->x+(ndx>>8),OB->y+(ndy>>8)) is equivalent to
            // probing the original Real coordinate displaced by ndx/ndy.
            float probeX = u.RuntimeRealXLikeOriginal + ndx;
            float probeY = u.RuntimeRealYLikeOriginal + ndy;
            if (C2BattleTerrainMode.C2BuildingMotionFieldV1IsBlockedForUnitRealLikeOriginal(probeX, probeY, 1))
            {
                ndx = 0;
                ndy = 0;
            }

            dx = idx + ndx;
            dy = idy + ndy;
        }

        // ---- V427 moved from renderer: ApplyOriginalSingleStepBoidsSteeringLikeOriginal ----
        private void ApplyOriginalSingleStepBoidsSteeringLikeOriginal(
            C2UnitOriginalRuntime u,
            ref float nx,
            ref float ny,
            ref float stepReal)
        {
            if (!UseMdBoidsSteeringLikeOriginal || u == null || u.Md == null ||
                !u.Md.BoidsMoving || !IsMdSingleStepPassThroughLikeOriginal(u) ||
                _units.Count >= Mathf.Max(1, OriginalBoidsOffLimitLikeOriginal))
                return;

            float remainingX = u.MoveTargetRealXLikeOriginal - u.RuntimeRealXLikeOriginal;
            float remainingY = u.MoveTargetRealYLikeOriginal - u.RuntimeRealYLikeOriginal;
            if (OriginalNormaLikeOriginal(remainingX, remainingY) <= 16.0f * 16.0f)
                return;

            float mainNorm = Mathf.Max(1.0f, OriginalBoidsMainDirectionNormLikeOriginal);
            float directionNorm = Mathf.Max(0.0001f, OriginalNormaLikeOriginal(nx, ny));
            float desiredX = nx * mainNorm / directionNorm;
            float desiredY = ny * mainNorm / directionNorm;
            int boidsBase = u.UnitOrder << 2;
            float forceX = boidsBase >= 0 && boidsBase + 3 < _originalBoidsCoordAndForceLikeOriginal.Length
                ? _originalBoidsCoordAndForceLikeOriginal[boidsBase + 2]
                : 0.0f;
            float forceY = boidsBase >= 0 && boidsBase + 3 < _originalBoidsCoordAndForceLikeOriginal.Length
                ? _originalBoidsCoordAndForceLikeOriginal[boidsBase + 3]
                : 0.0f;
            float forceNorm = OriginalNormaLikeOriginal(forceX, forceY) + 1.0f;
            float densNorm = Mathf.Max(1.0f, OriginalBoidsDensityNormLikeOriginal);
            if (forceNorm > densNorm)
            {
                forceX = forceX * densNorm / forceNorm;
                forceY = forceY * densNorm / forceNorm;
            }

            float probeX = u.RuntimeRealXLikeOriginal + forceX / 256.0f;
            float probeY = u.RuntimeRealYLikeOriginal + forceY / 256.0f;
            if (C2BattleTerrainMode.C2BuildingMotionFieldV1IsBlockedForUnitRealLikeOriginal(probeX, probeY, 1))
            {
                forceX = 0.0f;
                forceY = 0.0f;
            }

            float addSpeed = OriginalBoidsChangeSpeedWeightLikeOriginal *
                             (desiredX * forceX + desiredY * forceY) /
                             mainNorm / 1000.0f;
            stepReal = Mathf.Max(1.0f, stepReal + addSpeed);

            float steeringX = desiredX + forceX;
            float steeringY = desiredY + forceY;
            float steeringLength = Mathf.Sqrt(steeringX * steeringX + steeringY * steeringY);
            if (steeringLength > 0.0001f)
            {
                nx = steeringX / steeringLength;
                ny = steeringY / steeringLength;
            }
        }

        // ---- V427 moved from renderer: UpdateOriginalBoidsPairForcesLikeOriginal ----
        private void UpdateOriginalBoidsPairForcesLikeOriginal()
        {
            // BoidsExtension::ProcessingGame does no coordinate fill or force
            // calculation at all once MAXOBJECT reaches BoidsOffLimit.
            if (!UseMdBoidsSteeringLikeOriginal ||
                !_unitCollisionBucketsValidLikeOriginal ||
                _units.Count >= Mathf.Max(1, OriginalBoidsOffLimitLikeOriginal))
            {
                _originalBoidsNeighborPairsLikeOriginal.Clear();
                _originalBoidsSimulationTickLikeOriginal++;
                return;
            }

            int required = _units.Count << 2;
            if (_originalBoidsCoordAndForceLikeOriginal.Length < required)
            {
                int capacity = Mathf.NextPowerOfTwo(Mathf.Max(16, required));
                _originalBoidsCoordAndForceLikeOriginal = new float[capacity];
            }

            // CPF_Stage1: write current coordinates and clear force slots.
            for (int i = 0; i < _units.Count; i++)
            {
                int p = i << 2;
                C2UnitOriginalRuntime u = _units[i];
                if (u != null)
                {
                    _originalBoidsCoordAndForceLikeOriginal[p] = u.RuntimeRealXLikeOriginal;
                    _originalBoidsCoordAndForceLikeOriginal[p + 1] = u.RuntimeRealYLikeOriginal;
                }
                else
                {
                    _originalBoidsCoordAndForceLikeOriginal[p] = 0.0f;
                    _originalBoidsCoordAndForceLikeOriginal[p + 1] = 0.0f;
                }
                _originalBoidsCoordAndForceLikeOriginal[p + 2] = 0.0f;
                _originalBoidsCoordAndForceLikeOriginal[p + 3] = 0.0f;
            }

            int refreshTicks = Mathf.Max(1, OriginalBoidsNeighborRefreshTicksLikeOriginal);
            if (_originalBoidsNeighborPairsLikeOriginal.Count == 0 ||
                (_originalBoidsSimulationTickLikeOriginal % refreshTicks) == 0)
            {
                RebuildOriginalBoidsNeighborPairsLikeOriginal();
                _originalBoidsNeighborRefreshesLikeOriginal++;
            }
            _originalBoidsSimulationTickLikeOriginal++;

            float minDistance = Mathf.Max(1.0f, OriginalBoidsMinDistanceOriginalPixelsLikeOriginal) * 16.0f;
            for (int i = 0; i < _originalBoidsNeighborPairsLikeOriginal.Count; i++)
            {
                OriginalBoidsNeighborPairLikeOriginal pair = _originalBoidsNeighborPairsLikeOriginal[i];
                if (pair.A < 0 || pair.B < 0 || pair.A >= _units.Count || pair.B >= _units.Count)
                    continue;
                C2UnitOriginalRuntime a = _units[pair.A];
                C2UnitOriginalRuntime b = _units[pair.B];
                if (!IsRuntimeUnitCollisionCandidateLikeOriginal(a, false) ||
                    !IsRuntimeUnitCollisionCandidateLikeOriginal(b, false))
                    continue;

                int pa = pair.A << 2;
                int pb = pair.B << 2;
                float dx = _originalBoidsCoordAndForceLikeOriginal[pb] -
                           _originalBoidsCoordAndForceLikeOriginal[pa];
                float dy = _originalBoidsCoordAndForceLikeOriginal[pb + 1] -
                           _originalBoidsCoordAndForceLikeOriginal[pa + 1];
                if (Mathf.Abs(dx) < 0.0001f || Mathf.Abs(dy) < 0.0001f)
                    continue;
                float norm = OriginalNormaLikeOriginal(dx, dy) + 1.0f;
                float overlap = minDistance - norm;
                if (overlap <= 0.0f)
                    continue;

                float fx = dx * overlap * minDistance / norm;
                float fy = dy * overlap * minDistance / norm;
                _originalBoidsCoordAndForceLikeOriginal[pa + 2] -= fx;
                _originalBoidsCoordAndForceLikeOriginal[pa + 3] -= fy;
                _originalBoidsCoordAndForceLikeOriginal[pb + 2] += fx;
                _originalBoidsCoordAndForceLikeOriginal[pb + 3] += fy;
            }
        }

        // ---- V427 moved from renderer: RebuildOriginalBoidsNeighborPairsLikeOriginal ----
        private void RebuildOriginalBoidsNeighborPairsLikeOriginal()
        {
            _originalBoidsNeighborPairsLikeOriginal.Clear();
            if (!_unitCollisionBucketsValidLikeOriginal)
                return;

            float cell = Mathf.Max(64.0f, OriginalUnitCollisionCellRealLikeOriginal);
            float radius = Mathf.Max(1.0f, OriginalBoidsRadiusOriginalPixelsLikeOriginal) * 16.0f;
            int range = Mathf.Max(1, Mathf.CeilToInt(radius / cell));
            for (int i = 0; i < _units.Count; i++)
            {
                C2UnitOriginalRuntime a = _units[i];
                if (!IsRuntimeUnitCollisionCandidateLikeOriginal(a, false)) continue;
                int cx = Mathf.FloorToInt(a.RuntimeRealXLikeOriginal / cell);
                int cy = Mathf.FloorToInt(a.RuntimeRealYLikeOriginal / cell);
                for (int yy = cy - range; yy <= cy + range; yy++)
                {
                    for (int xx = cx - range; xx <= cx + range; xx++)
                    {
                        List<C2UnitOriginalRuntime> bucket;
                        if (!_unitCollisionBucketsLikeOriginal.TryGetValue(UnitCollisionBucketKeyLikeOriginal(xx, yy), out bucket) ||
                            bucket == null)
                            continue;
                        for (int bi = 0; bi < bucket.Count; bi++)
                        {
                            C2UnitOriginalRuntime b = bucket[bi];
                            if (!IsRuntimeUnitCollisionCandidateLikeOriginal(b, false) || b.UnitOrder <= a.UnitOrder)
                                continue;
                            float dx = b.RuntimeRealXLikeOriginal - a.RuntimeRealXLikeOriginal;
                            float dy = b.RuntimeRealYLikeOriginal - a.RuntimeRealYLikeOriginal;
                            if (OriginalNormaLikeOriginal(dx, dy) >= radius)
                                continue;
                            _originalBoidsNeighborPairsLikeOriginal.Add(
                                new OriginalBoidsNeighborPairLikeOriginal { A = i, B = b.UnitOrder });
                        }
                    }
                }
            }
        }

        // ---- V427 moved from renderer: OriginalNormaLikeOriginal ----
        private static float OriginalNormaLikeOriginal(float x, float y)
        {
            float ax = Mathf.Abs(x);
            float ay = Mathf.Abs(y);
            return (Mathf.Max(ax, ay) + ax + ay) * 0.5f;
        }

        // ---- V427 moved from renderer: TryRetargetBlockedFinishToFreePositionLikeOriginal ----
        private bool TryRetargetBlockedFinishToFreePositionLikeOriginal(C2UnitOriginalRuntime u)
        {
            using (C2FrameCostProbe.Measure(C2FrameCostProbe.Phase.FinishRetarget))
            {
            if (u == null || u.PreciseBornPathLikeOriginal)
                return false;

            if (!UseOriginalProducedRallyFindUnitPositionLikeOriginal || !UseOriginalUnitCollisionCheckPositionLikeOriginal)
                return false;

            if (u.BlockedFinishRetargetAttemptLikeOriginal >= 3)
                return false;

            Vector2 freeReal;
            if (!TryFindProducedRallyFreePositionRealLikeOriginal(u, u.MoveTargetRealXLikeOriginal, u.MoveTargetRealYLikeOriginal, out freeReal))
                return false;

            float dxTarget = freeReal.x - u.MoveTargetRealXLikeOriginal;
            float dyTarget = freeReal.y - u.MoveTargetRealYLikeOriginal;
            if (dxTarget * dxTarget + dyTarget * dyTarget < 1.0f)
                return false;

            u.BlockedFinishRetargetAttemptLikeOriginal++;
            EmitBornStopAuditLikeOriginal(
                u,
                "blocked_finish_find_unit_position",
                "retargetAttempt=" + u.BlockedFinishRetargetAttemptLikeOriginal.ToString(CultureInfo.InvariantCulture) +
                " oldTarget=(" + u.MoveTargetRealXLikeOriginal.ToString("0", CultureInfo.InvariantCulture) +
                "," + u.MoveTargetRealYLikeOriginal.ToString("0", CultureInfo.InvariantCulture) + ")" +
                " free=(" + freeReal.x.ToString("0", CultureInfo.InvariantCulture) +
                "," + freeReal.y.ToString("0", CultureInfo.InvariantCulture) + ")");

            SetRuntimeMoveDestinationRealInternalLikeOriginal(
                u,
                freeReal.x,
                freeReal.y,
                u.MoveSpeedOriginalPixelsPerSecondLikeOriginal > 0.001f
                    ? u.MoveSpeedOriginalPixelsPerSecondLikeOriginal
                    : C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                u.HasFinalFacingDirLikeOriginal,
                u.FinalFacingDirLikeOriginal,
                true,
                false,
                "blocked_finish_find_unit_position",
                false);
            return true;
            }
        }

        // ---- V427 moved from renderer: CanRuntimeUnitOccupyRealLikeOriginal ----
        private bool CanRuntimeUnitOccupyRealLikeOriginal(C2UnitOriginalRuntime u, float realX, float realY)
        {
            if (u == null) return false;
            if (u.PreciseBornPathLikeOriginal) return true;
            if (!CanRuntimeUnitOccupyTerrainLikeOriginal(u, realX, realY))
                return false;
            return CanRuntimeUnitOccupyOtherUnitsLikeOriginal(u, realX, realY);
        }

        // ---- V427 moved from renderer: CanRuntimeUnitAdvanceRealLikeOriginal ----
        private bool CanRuntimeUnitAdvanceRealLikeOriginal(C2UnitOriginalRuntime u, float realX, float realY)
        {
            if (u == null) return false;
            if (u.PreciseBornPathLikeOriginal) return true;
            if (!CanRuntimeUnitOccupyTerrainLikeOriginal(u, realX, realY))
                return false;
            if (IsMdSingleStepPassThroughLikeOriginal(u))
                return true;
            if (!UseOriginalUnitCollisionHardStepBlockLikeOriginal)
                return true;
            return CanRuntimeUnitOccupyOtherUnitsLikeOriginal(u, realX, realY);
        }

        // ---- V427 moved from renderer: CanRuntimeUnitAdvanceSegmentRealLikeOriginal ----
        private bool CanRuntimeUnitAdvanceSegmentRealLikeOriginal(
            C2UnitOriginalRuntime u,
            float fromRealX,
            float fromRealY,
            float toRealX,
            float toRealY)
        {
            if (!CanRuntimeUnitAdvanceRealLikeOriginal(u, toRealX, toRealY))
                return false;
            if (u == null || u.PreciseBornPathLikeOriginal ||
                !UseOriginalMotionFieldPerStepBlockLikeOriginal ||
                !RouteUnitMoveThroughBuildingLockPointsLikeOriginal)
                return true;

            int radiusCells = ResolveRuntimeUnitRadiusCellsLikeOriginal(u);
            // A unit already caught by a newly-created lock uses the existing nearest-free escape
            // rule.  Segment rejection here would otherwise prevent it from leaving the building.
            if (C2BattleTerrainMode.C2BuildingMotionFieldV1IsBlockedForUnitRealLikeOriginal(
                    fromRealX, fromRealY, radiusCells))
                return true;

            return C2BuildingRuntimeInfoV247LikeOriginal.CanTravelStraightRealV247LikeOriginal(
                fromRealX, fromRealY, toRealX, toRealY, radiusCells);
        }

        // ---- V427 moved from renderer: CanRuntimeUnitFinishTargetRealLikeOriginal ----
        private bool CanRuntimeUnitFinishTargetRealLikeOriginal(C2UnitOriginalRuntime u, float realX, float realY)
        {
            if (u == null) return false;
            // CII NewMon.cpp::MotionHandlerForSingleStepObjects, FixEnd:
            // RealX/RealY become DestX/DestY without a global CheckPosition or
            // reservation test. Applying the ordinary-motion finish adapter here
            // both scattered formations and scanned the army for every arrival.
            // Keep the bridge's terrain/building lock guard, just as during steps.
            if (IsMdSingleStepPassThroughLikeOriginal(u))
                return CanRuntimeUnitOccupyTerrainLikeOriginal(u, realX, realY);
            if (u.MoveTargetAllowsUnitOverlapFinishLikeOriginal)
                return CanRuntimeUnitOccupyTerrainLikeOriginal(u, realX, realY);
            if (!CanRuntimeUnitOccupyTerrainLikeOriginal(u, realX, realY))
                return false;
            if (!UseOriginalUnitCollisionCheckPositionLikeOriginal)
                return true;
            return CanRuntimeUnitAvoidOtherUnitsAndReservedTargetsLikeOriginal(u, realX, realY);
        }

        // ---- V427 moved from renderer: ResolveProducedRallyDestinationRealLikeOriginal ----
        private Vector2 ResolveProducedRallyDestinationRealLikeOriginal(C2UnitOriginalRuntime u, int rallyRealX, int rallyRealY, out string audit)
        {
            // Original Build.cpp: if OBJ->DstX is set, produced unit is not sent to the
            // exact marker.  It first gets dx/dy = DstX/DstY + (rando()%2048)-1024,
            // then FindUnitPosition may move that candidate to a nearby free place.
            // If no free place is found, the original keeps the scattered dx/dy, not the
            // rally center.  Falling back to the exact rally center is what made the
            // 121st+ produced units visually glue into one pixel after the collision ring
            // was full.
            int order = u != null ? u.UnitOrder : 0;
            UnitProbe probe = u != null ? u.Probe : null;
            float scatterX = StableUnitRandomRangeLikeOriginal(probe, 6101 + order, -1024.0f, 1024.0f);
            float scatterY = StableUnitRandomRangeLikeOriginal(probe, 6102 + order, -1024.0f, 1024.0f);
            float desiredX = rallyRealX + scatterX;
            float desiredY = rallyRealY + scatterY;

            Vector2 finalReal;
            bool found = TryFindProducedRallyFreePositionRealLikeOriginal(u, desiredX, desiredY, out finalReal);
            if (!found)
                finalReal = new Vector2(desiredX, desiredY);

            audit = "rally=dstXDstY_original_Build_cpp_651_654_scatter_then_FindUnitPosition" +
                    " baseReal=(" + rallyRealX.ToString(CultureInfo.InvariantCulture) + "," + rallyRealY.ToString(CultureInfo.InvariantCulture) + ")" +
                    " scatterReal=(" + scatterX.ToString("0", CultureInfo.InvariantCulture) + "," + scatterY.ToString("0", CultureInfo.InvariantCulture) + ")" +
                    " desiredReal=(" + desiredX.ToString("0", CultureInfo.InvariantCulture) + "," + desiredY.ToString("0", CultureInfo.InvariantCulture) + ")" +
                    " finalReal=(" + finalReal.x.ToString("0", CultureInfo.InvariantCulture) + "," + finalReal.y.ToString("0", CultureInfo.InvariantCulture) + ")" +
                    " foundFree=" + found +
                    " fallback=scattered_not_center";
            return finalReal;
        }

        // ---- V427 moved from renderer: TryStartGotoFinePositionAfterProductionLikeOriginal ----
        private bool TryStartGotoFinePositionAfterProductionLikeOriginal(C2UnitOriginalRuntime u, string reason)
        {
            if (!UseOriginalGotoFinePositionAfterProductionLikeOriginal) return false;
            if (u == null || u.State == C2UnitOriginalState.Death) return false;
            if (!u.NeedsGotoFinePositionLikeOriginal) return false;
            // A brigade owns this destination. A stale production-exit flag must
            // not replace its slot with an unrelated free point after the first move.
            if (u.Info != null && C2FormationRuntimeV167LikeOriginal.IsUnitInRuntimeFormationV168LikeOriginal(u.Info))
            {
                u.NeedsGotoFinePositionLikeOriginal = false;
                return false;
            }
            if (u.PreciseBornPathLikeOriginal) return false;

            int maxAttempts = Mathf.Max(0, OriginalGotoFinePositionMaxAttemptsLikeOriginal);
            if (maxAttempts <= 0 || u.GotoFinePositionAttemptsLikeOriginal >= maxAttempts)
            {
                u.NeedsGotoFinePositionLikeOriginal = false;
                return false;
            }

            Vector2 fineReal;
            bool found = TryFindProducedRallyFreePositionRealLikeOriginal(
                u,
                u.RuntimeRealXLikeOriginal,
                u.RuntimeRealYLikeOriginal,
                out fineReal);

            float dx = fineReal.x - u.RuntimeRealXLikeOriginal;
            float dy = fineReal.y - u.RuntimeRealYLikeOriginal;
            float minMove = Mathf.Max(1.0f, OriginalGotoFinePositionMinMoveRealLikeOriginal);
            if (!found || dx * dx + dy * dy < minMove * minMove)
            {
                u.NeedsGotoFinePositionLikeOriginal = false;
                LogGotoFinePositionLikeOriginal(u, reason, found ? "already_free" : "no_free_position", fineReal);
                return false;
            }

            u.GotoFinePositionAttemptsLikeOriginal++;
            SetRuntimeMoveDestinationRealInternalLikeOriginal(
                u,
                fineReal.x,
                fineReal.y,
                u.MoveSpeedOriginalPixelsPerSecondLikeOriginal > 0.001f
                    ? u.MoveSpeedOriginalPixelsPerSecondLikeOriginal
                    : C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                false,
                0,
                true,
                false,
                "goto_fine_position_after_production",
                false);

            LogGotoFinePositionLikeOriginal(u, reason, "send_to_free_position", fineReal);
            return true;
        }

        // ---- V427 moved from renderer: LogGotoFinePositionLikeOriginal ----
        private void LogGotoFinePositionLikeOriginal(C2UnitOriginalRuntime u, string reason, string result, Vector2 fineReal)
        {
            if (!LogOriginalGotoFinePositionLikeOriginal) return;
            if (_gotoFinePositionLogsLikeOriginal >= Mathf.Max(1, MaxOriginalGotoFinePositionLogsLikeOriginal)) return;

            _gotoFinePositionLogsLikeOriginal++;
            Debug.Log(LogPrefix + " GOTO_FINE_POSITION original=Build.cpp_727_779" +
                      " unit='" + (u != null && u.Probe != null ? u.Probe.MonsterId : string.Empty) + "'" +
                      " order=" + (u != null ? u.UnitOrder.ToString(CultureInfo.InvariantCulture) : "0") +
                      " attempt=" + (u != null ? u.GotoFinePositionAttemptsLikeOriginal.ToString(CultureInfo.InvariantCulture) : "0") +
                      "/" + Mathf.Max(0, OriginalGotoFinePositionMaxAttemptsLikeOriginal).ToString(CultureInfo.InvariantCulture) +
                      " reason='" + (reason ?? string.Empty) + "'" +
                      " result='" + (result ?? string.Empty) + "'" +
                      " from=(" + (u != null ? u.RuntimeRealXLikeOriginal.ToString("0", CultureInfo.InvariantCulture) : "0") +
                      "," + (u != null ? u.RuntimeRealYLikeOriginal.ToString("0", CultureInfo.InvariantCulture) : "0") + ")" +
                      " to=(" + fineReal.x.ToString("0", CultureInfo.InvariantCulture) +
                      "," + fineReal.y.ToString("0", CultureInfo.InvariantCulture) + ")");
        }

        // ---- V427 moved from renderer: TryFindProducedRallyFreePositionRealLikeOriginal ----
        private bool TryFindProducedRallyFreePositionRealLikeOriginal(C2UnitOriginalRuntime u, float wantedRealX, float wantedRealY, out Vector2 finalReal)
        {
            finalReal = new Vector2(wantedRealX, wantedRealY);
            if (!UseOriginalProducedRallyFindUnitPositionLikeOriginal)
                return false;

            int rings = Mathf.Clamp(OriginalProducedRallyFindUnitPositionRingsLikeOriginal, 1, 50);
            const float ringStepReal = 512.0f; // NewMon.cpp::FindUnitPosition SH=9.
            for (int ring = 0; ring < rings; ring++)
            {
                if (ring == 0)
                {
                    if (CanProducedRallyCandidateRealLikeOriginal(u, wantedRealX, wantedRealY))
                    {
                        finalReal = new Vector2(wantedRealX, wantedRealY);
                        return true;
                    }
                    continue;
                }

                int count = ring * 8;
                int start = Mathf.Abs(((u != null ? u.UnitOrder : 0) * 37 + ring * 13)) % Mathf.Max(1, count);
                for (int i = 0; i < count; i++)
                {
                    Vector2Int off = SquareRingOffsetLikeOriginal((i + start) % count, ring);
                    float x = wantedRealX + off.x * ringStepReal;
                    float y = wantedRealY + off.y * ringStepReal;
                    if (!CanProducedRallyCandidateRealLikeOriginal(u, x, y))
                        continue;

                    finalReal = new Vector2(x, y);
                    return true;
                }
            }

            return false;
        }

        // ---- V427 moved from renderer: CanProducedRallyCandidateRealLikeOriginal ----
        private bool CanProducedRallyCandidateRealLikeOriginal(C2UnitOriginalRuntime u, float realX, float realY)
        {
            if (!CanRuntimeUnitOccupyTerrainLikeOriginal(u, realX, realY))
                return false;
            if (!UseOriginalUnitCollisionCheckPositionLikeOriginal)
                return true;
            return CanRuntimeUnitAvoidOtherUnitsAndReservedTargetsLikeOriginal(u, realX, realY);
        }

        // ---- V427 moved from renderer: CanRuntimeUnitAvoidOtherUnitsAndReservedTargetsLikeOriginal ----
        private bool CanRuntimeUnitAvoidOtherUnitsAndReservedTargetsLikeOriginal(C2UnitOriginalRuntime u, float realX, float realY)
        {
            using (C2FrameCostProbe.Measure(C2FrameCostProbe.Phase.FinishCollision))
            {
            float selfRadius = ResolveRuntimeUnitCollisionRadiusRealLikeOriginal(u);
            for (int i = 0; i < _units.Count; i++)
            {
                C2UnitOriginalRuntime other = _units[i];
                if (other == null || ReferenceEquals(other, u)) continue;
                if (!IsRuntimeUnitCollisionCandidateLikeOriginal(other, true)) continue;
                // Members of one brigade intentionally reserve close orders.lst slots.
                // Treating those reservations as ordinary unit collisions made every
                // soldier reject his own place during the final CheckPosition pass and
                // retarget to a common "free" area, visually gluing the brigade together.
                if (C2FormationRuntimeV167LikeOriginal.AreUnitsInSameFormationV321LikeOriginal(u.Info, other.Info))
                    continue;

                float minDist = selfRadius + ResolveRuntimeUnitCollisionRadiusRealLikeOriginal(other);
                if (IsPointInsideRuntimeUnitRadiusLikeOriginal(realX, realY, other.RuntimeRealXLikeOriginal, other.RuntimeRealYLikeOriginal, minDist))
                    return false;

                Vector2 reserved;
                if (TryGetRuntimeUnitReservedDestinationRealLikeOriginal(other, out reserved) &&
                    IsPointInsideRuntimeUnitRadiusLikeOriginal(realX, realY, reserved.x, reserved.y, minDist))
                    return false;
            }

            return true;
            }
        }

        // ---- V427 moved from renderer: IsPointInsideRuntimeUnitRadiusLikeOriginal ----
        private static bool IsPointInsideRuntimeUnitRadiusLikeOriginal(float x, float y, float ox, float oy, float radius)
        {
            float dx = x - ox;
            float dy = y - oy;
            return dx * dx + dy * dy < radius * radius;
        }

        // ---- V427 moved from renderer: TryGetRuntimeUnitReservedDestinationRealLikeOriginal ----
        private static bool TryGetRuntimeUnitReservedDestinationRealLikeOriginal(C2UnitOriginalRuntime other, out Vector2 reserved)
        {
            reserved = Vector2.zero;
            if (other == null)
                return false;
            if (other.MovePathRealWaypointsLikeOriginal != null && other.MovePathRealWaypointsLikeOriginal.Length > 0)
            {
                reserved = other.MovePathRealWaypointsLikeOriginal[other.MovePathRealWaypointsLikeOriginal.Length - 1];
                return true;
            }
            if (other.HasMoveTargetLikeOriginal)
            {
                reserved = new Vector2(other.MoveTargetRealXLikeOriginal, other.MoveTargetRealYLikeOriginal);
                return true;
            }
            return false;
        }

        // ---- V427 moved from renderer: SquareRingOffsetLikeOriginal ----
        private static Vector2Int SquareRingOffsetLikeOriginal(int index, int ring)
        {
            if (ring <= 0)
                return Vector2Int.zero;

            int side = ring * 2;
            int count = ring * 8;
            int idx = ((index % count) + count) % count;
            if (idx < side)
                return new Vector2Int(-ring + idx, -ring);
            idx -= side;
            if (idx < side)
                return new Vector2Int(ring, -ring + idx);
            idx -= side;
            if (idx < side)
                return new Vector2Int(ring - idx, ring);
            idx -= side;
            return new Vector2Int(-ring, ring - idx);
        }

        // ---- V427 moved from renderer: CanRuntimeUnitOccupyTerrainLikeOriginal ----
        private bool CanRuntimeUnitOccupyTerrainLikeOriginal(C2UnitOriginalRuntime u, float realX, float realY)
        {
            if (u == null) return false;
            if (!UseOriginalMotionFieldPerStepBlockLikeOriginal) return true;
            if (!RouteUnitMoveThroughBuildingLockPointsLikeOriginal) return true;

            int radiusCells = ResolveRuntimeUnitRadiusCellsLikeOriginal(u);
            if (!C2BattleTerrainMode.C2BuildingMotionFieldV1IsBlockedForUnitRealLikeOriginal(realX, realY, radiusCells))
                return true;

            // FIX3: if the unit is already inside a locked cell (bad spawn/old save/previous patch),
            // allow only steps that move it closer to the nearest free cell. This reproduces the
            // practical effect of original UnlimitedMotion/BORN escape without allowing normal
            // movement through buildings.
            if (C2BattleTerrainMode.C2BuildingMotionFieldV1IsBlockedForUnitRealLikeOriginal(u.RuntimeRealXLikeOriginal, u.RuntimeRealYLikeOriginal, radiusCells))
            {
                float freeX;
                float freeY;
                if (C2BattleTerrainMode.C2BuildingMotionFieldV1TryFindNearestFreeRealLikeOriginal(u.RuntimeRealXLikeOriginal, u.RuntimeRealYLikeOriginal, out freeX, out freeY, 24))
                {
                    float cdx = freeX - u.RuntimeRealXLikeOriginal;
                    float cdy = freeY - u.RuntimeRealYLikeOriginal;
                    float ndx = freeX - realX;
                    float ndy = freeY - realY;
                    float curD = cdx * cdx + cdy * cdy;
                    float newD = ndx * ndx + ndy * ndy;
                    if (newD < curD - 1.0f)
                        return true;
                }
            }

            return false;
        }

        // ---- V427 moved from renderer: CanPreciseBornUnitAdvanceWithDoorSpacingLikeOriginal ----
        private bool CanPreciseBornUnitAdvanceWithDoorSpacingLikeOriginal(C2UnitOriginalRuntime u, float realX, float realY)
        {
            // Original Build.cpp sends produced units through BORNPOINTS using
            // OrderedUnlimitedMotion. It does not serialize them into a Unity-side
            // FIFO queue at the doorway. Precise born movement already ignores normal
            // unit collision below, so allow every produced unit to traverse the exact
            // exit path and disperse at its rally destination.
            return true;
        }

        // ---- V427 moved from renderer: CanRuntimeUnitOccupyOtherUnitsLikeOriginal ----
        private bool CanRuntimeUnitOccupyOtherUnitsLikeOriginal(C2UnitOriginalRuntime u, float realX, float realY)
        {
            if (!UseOriginalUnitCollisionCheckPositionLikeOriginal) return true;
            if (!_unitCollisionBucketsValidLikeOriginal || _unitCollisionBucketsLikeOriginal.Count == 0) return true;
            if (u == null || u.PreciseBornPathLikeOriginal) return true;

            float cell = Mathf.Max(64.0f, OriginalUnitCollisionCellRealLikeOriginal);
            int cx = Mathf.FloorToInt(realX / cell);
            int cy = Mathf.FloorToInt(realY / cell);
            float selfRadius = ResolveRuntimeUnitCollisionRadiusRealLikeOriginal(u);

            for (int yy = cy - 1; yy <= cy + 1; yy++)
            {
                for (int xx = cx - 1; xx <= cx + 1; xx++)
                {
                    List<C2UnitOriginalRuntime> bucket;
                    if (!_unitCollisionBucketsLikeOriginal.TryGetValue(UnitCollisionBucketKeyLikeOriginal(xx, yy), out bucket) || bucket == null)
                        continue;

                    for (int i = 0; i < bucket.Count; i++)
                    {
                        C2UnitOriginalRuntime other = bucket[i];
                        if (other == null || ReferenceEquals(other, u)) continue;
                        if (!IsRuntimeUnitCollisionCandidateLikeOriginal(other, false)) continue;
                        if (C2FormationRuntimeV167LikeOriginal.AreUnitsInSameFormationV321LikeOriginal(u.Info, other.Info))
                            continue;
                        // Cossacks II moving crowds do not form an impenetrable Unity
                        // collider wall. Both orders remain valid and their sprites can
                        // cross; only a standing unit yields aside. Hard-blocking two
                        // moving brigades was the source of stalled/bent lines.
                        if (u.HasMoveTargetLikeOriginal && other.HasMoveTargetLikeOriginal)
                            continue;

                        float dx = realX - other.RuntimeRealXLikeOriginal;
                        float dy = realY - other.RuntimeRealYLikeOriginal;
                        float minDist = selfRadius + ResolveRuntimeUnitCollisionRadiusRealLikeOriginal(other);
                        if (dx * dx + dy * dy < minDist * minDist)
                            return false;
                    }
                }
            }

            return true;
        }

        // ---- V427 moved from renderer: TryRequestIdleBlockerYieldAsideLikeOriginal ----
        private bool TryRequestIdleBlockerYieldAsideLikeOriginal(C2UnitOriginalRuntime mover, float blockedRealX, float blockedRealY, float moveNx, float moveNy)
        {
            if (!UseOriginalIdleUnitYieldAsideLikeOriginal)
                return false;
            if (mover == null || mover.PreciseBornPathLikeOriginal)
                return false;
            if (!_unitCollisionBucketsValidLikeOriginal || _unitCollisionBucketsLikeOriginal.Count == 0)
                return false;

            C2UnitOriginalRuntime blocker;
            if (!TryFindRuntimeUnitBlockingPositionLikeOriginal(mover, blockedRealX, blockedRealY, out blocker))
                return false;
            if (!IsRuntimeUnitFreeToYieldAsideLikeOriginal(blocker))
                return false;

            Vector2 yieldTarget;
            if (!TryPickIdleYieldAsideTargetLikeOriginal(mover, blocker, moveNx, moveNy, out yieldTarget))
                return false;

            blocker.NextYieldAsideAllowedAtLikeOriginal =
                Time.realtimeSinceStartup + Mathf.Max(0.05f, OriginalIdleUnitYieldAsideCooldownSecondsLikeOriginal);
            SetRuntimeMoveDestinationRealInternalLikeOriginal(
                blocker,
                yieldTarget.x,
                yieldTarget.y,
                blocker.MoveSpeedOriginalPixelsPerSecondLikeOriginal > 0.001f
                    ? blocker.MoveSpeedOriginalPixelsPerSecondLikeOriginal
                    : C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                false,
                0,
                true,
                false,
                "idle_yield_aside_for_moving_unit",
                false);

            if (_idleYieldAsideLogsLikeOriginal < Mathf.Max(0, MaxOriginalIdleYieldAsideLogsLikeOriginal))
            {
                _idleYieldAsideLogsLikeOriginal++;
                Debug.Log(LogPrefix + " IDLE_YIELD_ASIDE original=free_unit_steps_aside_for_path" +
                          " mover='" + (mover.Probe != null ? mover.Probe.MonsterId : string.Empty) + "'" +
                          " blocker='" + (blocker.Probe != null ? blocker.Probe.MonsterId : string.Empty) + "'" +
                          " blocked=(" + blockedRealX.ToString("0", CultureInfo.InvariantCulture) +
                          "," + blockedRealY.ToString("0", CultureInfo.InvariantCulture) + ")" +
                          " target=(" + yieldTarget.x.ToString("0", CultureInfo.InvariantCulture) +
                          "," + yieldTarget.y.ToString("0", CultureInfo.InvariantCulture) + ")");
            }

            return true;
        }

        // ---- V427 moved from renderer: TryResolveMovingUnitBlockLikeOriginal ----
        private bool TryResolveMovingUnitBlockLikeOriginal(
            C2UnitOriginalRuntime mover,
            float blockedRealX,
            float blockedRealY,
            float moveNx,
            float moveNy)
        {
            if (!UseOriginalMovingUnitMutualYieldLikeOriginal || mover == null)
                return false;

            C2UnitOriginalRuntime blocker;
            if (!TryFindRuntimeUnitBlockingPositionLikeOriginal(mover, blockedRealX, blockedRealY, out blocker) ||
                blocker == null ||
                !blocker.HasMoveTargetLikeOriginal ||
                blocker.PreciseBornPathLikeOriginal ||
                blocker.State == C2UnitOriginalState.Death ||
                Time.realtimeSinceStartup < blocker.NextYieldAsideAllowedAtLikeOriginal)
                return false;

            // Motion.cpp accumulates NextForceX/NextForceY for dynamic neighbours.
            // It does not replace either unit's global order. Preserve both paths and
            // apply only a deterministic lateral force to the unit with lower
            // right-of-way, so two builders/cavalry columns cannot mirror-lock.
            C2UnitOriginalRuntime yielding = mover.UnitOrder <= blocker.UnitOrder
                ? blocker
                : mover;
            C2UnitOriginalRuntime counterpart = ReferenceEquals(yielding, mover)
                ? blocker
                : mover;

            float sideSign = ((mover.UnitOrder ^ blocker.UnitOrder) & 1) == 0 ? 1.0f : -1.0f;
            Vector2 side = new Vector2(-moveNy * sideSign, moveNx * sideSign);
            Vector2 away = new Vector2(
                yielding.RuntimeRealXLikeOriginal - counterpart.RuntimeRealXLikeOriginal,
                yielding.RuntimeRealYLikeOriginal - counterpart.RuntimeRealYLikeOriginal);
            Vector2 direction = side * 0.8f;
            if (away.sqrMagnitude > 0.0001f)
                direction += away.normalized * 0.6f;
            if (direction.sqrMagnitude < 0.0001f)
                return false;
            direction.Normalize();

            float step = Mathf.Max(8.0f, OriginalMovingUnitMutualYieldStepRealLikeOriginal);
            float beforeX = yielding.RuntimeRealXLikeOriginal;
            float beforeY = yielding.RuntimeRealYLikeOriginal;
            float nextX = beforeX + direction.x * step;
            float nextY = beforeY + direction.y * step;
            if (!CanRuntimeUnitOccupyTerrainLikeOriginal(yielding, nextX, nextY) ||
                !CanRuntimeUnitOccupyOtherUnitsExceptLikeOriginal(yielding, counterpart, nextX, nextY))
                return false;

            yielding.RuntimeRealXLikeOriginal = nextX;
            yielding.RuntimeRealYLikeOriginal = nextY;
            yielding.NextYieldAsideAllowedAtLikeOriginal =
                Time.realtimeSinceStartup + Mathf.Max(0.02f, OriginalMovingUnitMutualYieldCooldownSecondsLikeOriginal);
            if (UseContinuousWorldDeltaForOriginalMotion)
                UpdateRuntimeWorldAndRealContinuousLikeOriginal(yielding, beforeX, beforeY);
            else
                UpdateRuntimeWorldAndRealLikeOriginal(yielding);
            return true;
        }

        // ---- V427 moved from renderer: CanRuntimeUnitOccupyOtherUnitsExceptLikeOriginal ----
        private bool CanRuntimeUnitOccupyOtherUnitsExceptLikeOriginal(
            C2UnitOriginalRuntime unit,
            C2UnitOriginalRuntime ignored,
            float realX,
            float realY)
        {
            if (!_unitCollisionBucketsValidLikeOriginal || unit == null)
                return true;

            float cell = Mathf.Max(64.0f, OriginalUnitCollisionCellRealLikeOriginal);
            int cx = Mathf.FloorToInt(realX / cell);
            int cy = Mathf.FloorToInt(realY / cell);
            float selfRadius = ResolveRuntimeUnitCollisionRadiusRealLikeOriginal(unit);
            for (int yy = cy - 1; yy <= cy + 1; yy++)
            {
                for (int xx = cx - 1; xx <= cx + 1; xx++)
                {
                    List<C2UnitOriginalRuntime> bucket;
                    if (!_unitCollisionBucketsLikeOriginal.TryGetValue(UnitCollisionBucketKeyLikeOriginal(xx, yy), out bucket) ||
                        bucket == null)
                        continue;
                    for (int i = 0; i < bucket.Count; i++)
                    {
                        C2UnitOriginalRuntime other = bucket[i];
                        if (other == null || ReferenceEquals(other, unit) || ReferenceEquals(other, ignored))
                            continue;
                        if (!IsRuntimeUnitCollisionCandidateLikeOriginal(other, false))
                            continue;
                        if (C2FormationRuntimeV167LikeOriginal.AreUnitsInSameFormationV321LikeOriginal(unit.Info, other.Info))
                            continue;

                        float dx = realX - other.RuntimeRealXLikeOriginal;
                        float dy = realY - other.RuntimeRealYLikeOriginal;
                        float minDist = selfRadius + ResolveRuntimeUnitCollisionRadiusRealLikeOriginal(other);
                        if (dx * dx + dy * dy < minDist * minDist)
                            return false;
                    }
                }
            }
            return true;
        }

        // ---- V427 moved from renderer: TryFindRuntimeUnitBlockingPositionLikeOriginal ----
        private bool TryFindRuntimeUnitBlockingPositionLikeOriginal(C2UnitOriginalRuntime mover, float realX, float realY, out C2UnitOriginalRuntime blocker)
        {
            blocker = null;
            if (mover == null) return false;

            float cell = Mathf.Max(64.0f, OriginalUnitCollisionCellRealLikeOriginal);
            int cx = Mathf.FloorToInt(realX / cell);
            int cy = Mathf.FloorToInt(realY / cell);
            float selfRadius = ResolveRuntimeUnitCollisionRadiusRealLikeOriginal(mover);
            float bestD2 = float.MaxValue;

            for (int yy = cy - 1; yy <= cy + 1; yy++)
            {
                for (int xx = cx - 1; xx <= cx + 1; xx++)
                {
                    List<C2UnitOriginalRuntime> bucket;
                    if (!_unitCollisionBucketsLikeOriginal.TryGetValue(UnitCollisionBucketKeyLikeOriginal(xx, yy), out bucket) || bucket == null)
                        continue;

                    for (int i = 0; i < bucket.Count; i++)
                    {
                        C2UnitOriginalRuntime other = bucket[i];
                        if (other == null || ReferenceEquals(other, mover)) continue;
                        if (!IsRuntimeUnitCollisionCandidateLikeOriginal(other, false)) continue;
                        if (C2FormationRuntimeV167LikeOriginal.AreUnitsInSameFormationV321LikeOriginal(mover.Info, other.Info))
                            continue;

                        float dx = realX - other.RuntimeRealXLikeOriginal;
                        float dy = realY - other.RuntimeRealYLikeOriginal;
                        float minDist = selfRadius + ResolveRuntimeUnitCollisionRadiusRealLikeOriginal(other);
                        float d2 = dx * dx + dy * dy;
                        if (d2 >= minDist * minDist || d2 >= bestD2)
                            continue;

                        bestD2 = d2;
                        blocker = other;
                    }
                }
            }

            return blocker != null;
        }

        // ---- V427 moved from renderer: IsRuntimeUnitFreeToYieldAsideLikeOriginal ----
        private static bool IsRuntimeUnitFreeToYieldAsideLikeOriginal(C2UnitOriginalRuntime blocker)
        {
            if (blocker == null || !blocker.ActiveLikeOriginal)
                return false;
            if (blocker.State == C2UnitOriginalState.Death || blocker.PreciseBornPathLikeOriginal || blocker.HasMoveTargetLikeOriginal)
                return false;
            if (Time.realtimeSinceStartup < blocker.NextYieldAsideAllowedAtLikeOriginal)
                return false;
            if (blocker.Info != null)
            {
                if (C2FormationRuntimeV167LikeOriginal.IsUnitInRuntimeFormationV168LikeOriginal(blocker.Info))
                    return false;
                C2BuildWorkerOrderV245LikeOriginal build = blocker.Info.GetComponent<C2BuildWorkerOrderV245LikeOriginal>();
                if (build != null && build.enabled && blocker.State == C2UnitOriginalState.Work)
                    return false;
            }
            return true;
        }

        // ---- V427 moved from renderer: TryPickIdleYieldAsideTargetLikeOriginal ----
        private bool TryPickIdleYieldAsideTargetLikeOriginal(C2UnitOriginalRuntime mover, C2UnitOriginalRuntime blocker, float moveNx, float moveNy, out Vector2 target)
        {
            target = Vector2.zero;
            if (mover == null || blocker == null)
                return false;

            float step = Mathf.Max(128.0f, OriginalIdleUnitYieldAsideStepRealLikeOriginal);
            Vector2 sideA = new Vector2(-moveNy, moveNx);
            Vector2 sideB = new Vector2(moveNy, -moveNx);
            Vector2 away = new Vector2(
                blocker.RuntimeRealXLikeOriginal - mover.RuntimeRealXLikeOriginal,
                blocker.RuntimeRealYLikeOriginal - mover.RuntimeRealYLikeOriginal);
            if (away.sqrMagnitude < 1.0f)
                away = sideA;
            away.Normalize();

            Vector2[] dirs = new Vector2[]
            {
                sideA.sqrMagnitude > 0.0001f ? sideA.normalized : away,
                sideB.sqrMagnitude > 0.0001f ? sideB.normalized : -away,
                away,
                -away
            };

            for (int s = 0; s < IdleYieldStepScalesLikeOriginal.Length; s++)
            {
                for (int d = 0; d < dirs.Length; d++)
                {
                    Vector2 dir = dirs[d];
                    if (dir.sqrMagnitude < 0.0001f) continue;

                    Vector2 candidate = new Vector2(
                        blocker.RuntimeRealXLikeOriginal + dir.x * step * IdleYieldStepScalesLikeOriginal[s],
                        blocker.RuntimeRealYLikeOriginal + dir.y * step * IdleYieldStepScalesLikeOriginal[s]);
                    if (!CanRuntimeUnitOccupyRealLikeOriginal(blocker, candidate.x, candidate.y))
                        continue;

                    target = candidate;
                    return true;
                }
            }

            return false;
        }

        // ---- V427 moved from renderer: RebuildRuntimeUnitCollisionBucketsLikeOriginal ----
        private void RebuildRuntimeUnitCollisionBucketsLikeOriginal()
        {
            // Reuse the bucket lists.  Clearing the dictionary alone discarded one
            // List + backing array per occupied cell, twice per rendered frame.  On
            // maps with hundreds of units that was the dominant managed-GC stream.
            foreach (KeyValuePair<long, List<C2UnitOriginalRuntime>> pair in _unitCollisionBucketsLikeOriginal)
            {
                List<C2UnitOriginalRuntime> oldBucket = pair.Value;
                if (oldBucket == null) continue;
                oldBucket.Clear();
                _unitCollisionBucketPoolLikeOriginal.Push(oldBucket);
            }
            _unitCollisionBucketsLikeOriginal.Clear();
            _unitCollisionBucketsValidLikeOriginal = false;
            if (!UseOriginalUnitCollisionCheckPositionLikeOriginal &&
                !UseOriginalUnitSeparationForcesLikeOriginal &&
                !UseMdBoidsSteeringLikeOriginal)
                return;

            float cell = Mathf.Max(64.0f, OriginalUnitCollisionCellRealLikeOriginal);
            for (int i = 0; i < _units.Count; i++)
            {
                C2UnitOriginalRuntime u = _units[i];
                if (!IsRuntimeUnitCollisionCandidateLikeOriginal(u, false)) continue;

                int cx = Mathf.FloorToInt(u.RuntimeRealXLikeOriginal / cell);
                int cy = Mathf.FloorToInt(u.RuntimeRealYLikeOriginal / cell);
                long key = UnitCollisionBucketKeyLikeOriginal(cx, cy);
                List<C2UnitOriginalRuntime> bucket;
                if (!_unitCollisionBucketsLikeOriginal.TryGetValue(key, out bucket) || bucket == null)
                {
                    bucket = _unitCollisionBucketPoolLikeOriginal.Count > 0
                        ? _unitCollisionBucketPoolLikeOriginal.Pop()
                        : new List<C2UnitOriginalRuntime>(16);
                    _unitCollisionBucketsLikeOriginal[key] = bucket;
                }
                bucket.Add(u);
            }

            _unitCollisionBucketsValidLikeOriginal = true;
        }

        // ---- V427 moved from renderer: ApplyRuntimeUnitSeparationForcesLikeOriginal ----
        private void ApplyRuntimeUnitSeparationForcesLikeOriginal(float dt)
        {
            if (!_unitCollisionBucketsValidLikeOriginal || _unitCollisionBucketsLikeOriginal.Count == 0)
                return;

            float cell = Mathf.Max(64.0f, OriginalUnitCollisionCellRealLikeOriginal);
            float maxPush = Mathf.Max(1.0f, OriginalUnitSeparationMaxPushRealPerFrameLikeOriginal);
            int maxNeighbors = Mathf.Max(1, OriginalUnitSeparationMaxNeighborsLikeOriginal);

            for (int i = 0; i < _units.Count; i++)
            {
                C2UnitOriginalRuntime u = _units[i];
                if (!IsRuntimeUnitCollisionCandidateLikeOriginal(u, false)) continue;
                if (IsMdSingleStepPassThroughLikeOriginal(u)) continue;
                // A brigade member owns a fixed orders.lst slot. Generic pairwise
                // separation must not shove that member out of the formation; the
                // hard CheckPosition-style step block below still prevents two
                // different formations from walking through one another. Free units
                // may yield or be separated around the rigid brigade footprint.
                if (u.Info != null &&
                    C2FormationRuntimeV167LikeOriginal.IsUnitInRuntimeFormationV168LikeOriginal(u.Info))
                    continue;

                float selfRadius = ResolveRuntimeUnitCollisionRadiusRealLikeOriginal(u);
                int cx = Mathf.FloorToInt(u.RuntimeRealXLikeOriginal / cell);
                int cy = Mathf.FloorToInt(u.RuntimeRealYLikeOriginal / cell);
                float pushX = 0.0f;
                float pushY = 0.0f;
                int touched = 0;

                for (int yy = cy - 1; yy <= cy + 1; yy++)
                {
                    for (int xx = cx - 1; xx <= cx + 1; xx++)
                    {
                        List<C2UnitOriginalRuntime> bucket;
                        if (!_unitCollisionBucketsLikeOriginal.TryGetValue(UnitCollisionBucketKeyLikeOriginal(xx, yy), out bucket) || bucket == null)
                            continue;

                        for (int b = 0; b < bucket.Count; b++)
                        {
                            C2UnitOriginalRuntime other = bucket[b];
                            if (other == null || ReferenceEquals(other, u)) continue;
                            if (!IsRuntimeUnitCollisionCandidateLikeOriginal(other, false)) continue;
                            if (C2FormationRuntimeV167LikeOriginal.AreUnitsInSameFormationV321LikeOriginal(u.Info, other.Info))
                                continue;
                            if (u.HasMoveTargetLikeOriginal && other.HasMoveTargetLikeOriginal)
                                continue;

                            float dx = u.RuntimeRealXLikeOriginal - other.RuntimeRealXLikeOriginal;
                            float dy = u.RuntimeRealYLikeOriginal - other.RuntimeRealYLikeOriginal;
                            float minDist = selfRadius + ResolveRuntimeUnitCollisionRadiusRealLikeOriginal(other);
                            float d2 = dx * dx + dy * dy;
                            if (d2 >= minDist * minDist)
                                continue;

                            float d = Mathf.Sqrt(Mathf.Max(0.0001f, d2));
                            if (d < 1.0f)
                            {
                                float a = StableUnitCollisionAngleLikeOriginal(u, other);
                                dx = Mathf.Cos(a);
                                dy = Mathf.Sin(a);
                                d = 1.0f;
                            }

                            float strength = (minDist - d) * 0.5f;
                            pushX += (dx / d) * strength;
                            pushY += (dy / d) * strength;
                            touched++;
                            if (touched >= maxNeighbors)
                                break;
                        }
                        if (touched >= maxNeighbors) break;
                    }
                    if (touched >= maxNeighbors) break;
                }

                if (touched <= 0)
                    continue;

                float len = Mathf.Sqrt(pushX * pushX + pushY * pushY);
                if (len <= 0.001f)
                    continue;

                float clamp = Mathf.Min(maxPush, len);
                float beforeX = u.RuntimeRealXLikeOriginal;
                float beforeY = u.RuntimeRealYLikeOriginal;
                float nextX = beforeX + pushX / len * clamp;
                float nextY = beforeY + pushY / len * clamp;
                if (!CanRuntimeUnitOccupyTerrainLikeOriginal(u, nextX, nextY))
                    continue;

                u.RuntimeRealXLikeOriginal = nextX;
                u.RuntimeRealYLikeOriginal = nextY;
                if (UseContinuousWorldDeltaForOriginalMotion)
                    UpdateRuntimeWorldAndRealContinuousLikeOriginal(u, beforeX, beforeY);
                else
                    UpdateRuntimeWorldAndRealLikeOriginal(u);
            }
        }

        // ---- V427 moved from renderer: IsRuntimeUnitCollisionCandidateLikeOriginal ----
        private bool IsRuntimeUnitCollisionCandidateLikeOriginal(C2UnitOriginalRuntime u, bool includePreciseBorn)
        {
            if (u == null || !u.ActiveLikeOriginal) return false;
            if (u.State == C2UnitOriginalState.Death) return false;
            if (u.HiddenInsideBuildingLikeOriginal) return false;
            if (!includePreciseBorn && u.PreciseBornPathLikeOriginal) return false;
            return true;
        }

        // ---- V427 moved from renderer: SetRuntimeHiddenInsideBuildingLikeOriginal ----
        internal void SetRuntimeHiddenInsideBuildingLikeOriginal(C2UnitOriginalRuntime u, bool hidden)
        {
            if (u == null || u.HiddenInsideBuildingLikeOriginal == hidden) return;
            u.HiddenInsideBuildingLikeOriginal = hidden;
            SetUnitForceRenderingOffLikeOriginal(u, hidden);
        }

        // ---- V427 moved from renderer: ResolveRuntimeUnitCollisionRadiusRealLikeOriginal ----
        private float ResolveRuntimeUnitCollisionRadiusRealLikeOriginal(C2UnitOriginalRuntime u)
        {
            float radius = 0.0f;
            if (u != null && u.Info != null)
            {
                if (u.Info.GeometryRadius2Real > 0)
                    radius = u.Info.GeometryRadius2Real;
                else if (u.Info.UnitRadius > 0)
                    radius = u.Info.UnitRadius * 16.0f;
            }
            if (radius <= 0.0f && u != null && u.Md != null && u.Md.GeometryRadius2 > 0)
                radius = u.Md.GeometryRadius2 * 16.0f;
            if (radius <= 0.0f)
                radius = 160.0f;
            return Mathf.Clamp(radius, OriginalUnitCollisionMinRadiusRealLikeOriginal, OriginalUnitCollisionMaxRadiusRealLikeOriginal);
        }

        // ---- V427 moved from renderer: UnitCollisionBucketKeyLikeOriginal ----
        private static long UnitCollisionBucketKeyLikeOriginal(int x, int y)
        {
            unchecked
            {
                return ((long)x << 32) ^ (uint)y;
            }
        }

        // ---- V427 moved from renderer: StableUnitCollisionAngleLikeOriginal ----
        private static float StableUnitCollisionAngleLikeOriginal(C2UnitOriginalRuntime a, C2UnitOriginalRuntime b)
        {
            int seed = 17;
            seed = seed * 31 + (a != null ? a.UnitOrder : 0);
            seed = seed * 31 + (b != null ? b.UnitOrder : 0);
            seed ^= seed << 13;
            seed ^= seed >> 17;
            seed ^= seed << 5;
            return ((seed & 4095) / 4096.0f) * Mathf.PI * 2.0f;
        }

        // ---- V427 moved from renderer: ResolveRuntimeUnitRadiusCellsLikeOriginal ----
        private static int ResolveRuntimeUnitRadiusCellsLikeOriginal(C2UnitOriginalRuntime u)
        {
            int radiusCells = 1;
            if (u != null && u.Info != null)
            {
                // Radius2 is in original pixels in our probe layer when available.
                // Motion.cpp checks a bar around (Real - Lx<<7)>>8; radius 1 cell is the safe
                // minimum that prevents walking through building LOCKPOINTS without blocking all paths.
                radiusCells = Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(8.0f, u.Info.UnitRadius) / 16.0f), 1, 2);
            }
            return radiusCells;
        }

        // ---- V427 moved from renderer: DirectionFromRealDeltaLikeOriginal ----
        private static byte DirectionFromRealDeltaLikeOriginal(float dx, float dy)
        {
            int ix = Mathf.RoundToInt(dx);
            int iy = Mathf.RoundToInt(dy);
            return C2OriginalMovementMathV352.GetDir(ix, iy);
        }

        // ---- V427 moved from renderer: GetMotionRInFrameLikeOriginal ----
        private int GetMotionRInFrameLikeOriginal(C2UnitOriginalRuntime u, AnimModel motionAnim)
        {
            float speedPx = u != null && u.MoveSpeedOriginalPixelsPerSecondLikeOriginal > 0.001f
                ? u.MoveSpeedOriginalPixelsPerSecondLikeOriginal
                : OriginalMotionDefaultSpeedOriginalPixelsPerSecond;

            float fps = Mathf.Max(1.0f, u != null ? u.AnimFps : DefaultAnimFps);
            int rInFrame = Mathf.Max(1, Mathf.RoundToInt((speedPx * 16.0f) / fps));
            if (u != null) u.MoveRInFrameLikeOriginal = rInFrame;
            return rInFrame;
        }

        // ---- V427 moved from renderer: ApplyMotionFrameFromPathLikeOriginal ----
        private void ApplyMotionFrameFromPathLikeOriginal(C2UnitOriginalRuntime u, AnimModel anim)
        {
            if (u == null || anim == null || anim.Frames.Count <= 0)
            {
                if (u != null) u.CurrentFrameLong = 0;
                return;
            }

            int nf = Mathf.Max(1, anim.Frames.Count);

            // NewMon.cpp SINGLESTEP: ROTATEL/ROTATER frames are indexed directly
            // by RealDirPrecise, not by TotalPath. Driving these animations from path
            // made horses visibly spin through camera angles while following a curve.
            if (string.Equals(anim.Name, "@ROTATEL", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(anim.Name, "#ROTATEL", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(anim.Name, "ROTATEL", StringComparison.OrdinalIgnoreCase))
            {
                ushort phase = unchecked((ushort)(64 * 256 - u.OriginalRealDirPrecise256LikeOriginal));
                u.CurrentFrameLong = (phase * nf) >> 8;
                u.FrameFinishedLikeOriginal = false;
                return;
            }
            if (string.Equals(anim.Name, "@ROTATER", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(anim.Name, "#ROTATER", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(anim.Name, "ROTATER", StringComparison.OrdinalIgnoreCase))
            {
                ushort phase = unchecked((ushort)(u.OriginalRealDirPrecise256LikeOriginal - 64 * 256));
                u.CurrentFrameLong = (phase * nf) >> 8;
                u.FrameFinishedLikeOriginal = false;
                return;
            }

            int rf = Mathf.Max(1, u.MoveRInFrameLikeOriginal > 0 ? u.MoveRInFrameLikeOriginal : GetMotionRInFrameLikeOriginal(u, anim));
            int maxLong = nf << 8;

            int path = Mathf.Abs(Mathf.RoundToInt(u.TotalPathLikeOriginal + 100000.0f));
            u.CurrentFrameLong = ((path << 8) / rf) % maxLong;
            u.FrameFinishedLikeOriginal = false;
        }

        // ---- V427 moved from renderer: SetRuntimeMovingFlagLikeOriginal ----
        internal void SetRuntimeMovingFlagLikeOriginal(C2UnitOriginalRuntime u, bool moving)
        {
            if (u == null) return;
            if (u.State == C2UnitOriginalState.Death) return;

            if(!moving) C2OriginalOrderChainV352.StopTaskMoveV433LikeOriginal(u.Info);
            u.HasMoveTargetLikeOriginal = moving && u.HasMoveTargetLikeOriginal;
            if (moving && u.Md != null)
            {
                int motion = ResolveRuntimeMotionAnimationIndexV223LikeOriginal(u);
                if (motion >= 0 && u.State != C2UnitOriginalState.Motion)
                    SelectAnimationStateLikeOriginal(u, C2UnitOriginalState.Motion, motion, true, "moving_flag_true");
                return;
            }

            // InMotion=false clears movement, not NewAnm. Repeated managed
            // "already stopped" notifications must not replace ATTACK/ATTACK3,
            // WORK or a posture transition with STAND. In particular each
            // discarded ATTACK3 was reinstalled by combat and deducted another
            // NFrames from delay without playing them (rapid musket fire).
            if (u.Md != null && u.State == C2UnitOriginalState.Motion)
            {
                int stand = ResolveRuntimeStandAnimationIndexV322LikeOriginal(u);
                if (stand >= 0) SelectAnimationStateLikeOriginal(u, C2UnitOriginalState.Stand, stand, true, "moving_flag_false");
            }
        }

        // ---- V427 moved from renderer: SetRuntimeMotionStateLikeOriginal ----
        internal void SetRuntimeMotionStateLikeOriginal(C2UnitOriginalRuntime u, byte realDir, bool backMotion)
        {
            if (u == null || u.Md == null || u.State == C2UnitOriginalState.Death) return;
            SetRuntimeFacingLikeOriginal(u, realDir);
            int motion = ResolveRuntimeMotionAnimationIndexV223LikeOriginal(u);
            if (motion >= 0 && u.State != C2UnitOriginalState.Motion)
                SelectAnimationStateLikeOriginal(u, C2UnitOriginalState.Motion, motion, true, backMotion ? "compat_back_motion" : "compat_motion");
        }

        // ---- V427 moved from renderer: SetRuntimeWalkPathFrameLikeOriginal ----
        internal void SetRuntimeWalkPathFrameLikeOriginal(C2UnitOriginalRuntime u, float totalPathReal, float rInFrameReal)
        {
            if (u == null || u.Md == null || u.State == C2UnitOriginalState.Death) return;
            u.TotalPathLikeOriginal = Mathf.Max(0.0f, totalPathReal);
            u.MoveRInFrameLikeOriginal = Mathf.Max(1, Mathf.RoundToInt(rInFrameReal));
            AnimModel anim = CurrentAnim(u);
            if (u.State == C2UnitOriginalState.Motion && anim != null && anim.Frames.Count > 0)
            {
                ApplyMotionFrameFromPathLikeOriginal(u, anim);
                ApplyUnitFrameLikeOriginal(u, "compat_walk_path_frame");
            }
        }

        // ---- V427 moved from renderer: DirectionFromWorldDeltaLikeOriginal ----
        private static byte DirectionFromWorldDeltaLikeOriginal(Vector3 d)
        {
            if (d.sqrMagnitude < 0.0001f) return 0;
            float angle = Mathf.Atan2(-d.z, d.x) * Mathf.Rad2Deg;
            int raw = Mathf.RoundToInt(Mathf.Repeat(angle / 360.0f * 256.0f, 256.0f));
            int snapped = (raw + 8) & 0xF0;
            return (byte)(snapped & 255);
        }
    }
}


// ============================================================================
// V427: movement-contact / UnitsField logic moved out of brigade-combat file.
// ============================================================================
namespace Cossacks2Bridge.UnityAdapters.Maps
{
    internal static partial class C2FormationRuntimeV167LikeOriginal
    {

        // ---- V427 moved from BrigadeBattle: IsUnlimitedMotionV415LikeOriginal ----
        internal static bool IsUnlimitedMotionV415LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            C2UnitOriginalRuntimeLinkLikeOriginal link =
                unit != null ? unit.RuntimeLinkCachedLikeOriginal : null;
            return link != null && link.IsUnlimitedMotionV415LikeOriginal;
        }

        // ---- V427 moved from BrigadeBattle: UnitsFieldCheckBarForMotionV415LikeOriginal ----
        internal static bool UnitsFieldCheckBarForMotionV415LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (!IsAliveBattleUnitV407LikeOriginal(unit)) return false;
            int lx = C2OriginalMovementSystemV425LikeOriginal.ResolveLxLikeOriginal(unit);
            int ux = (Mathf.RoundToInt(RealXV407LikeOriginal(unit)) - (lx << 7)) >> 8;
            int uy = (Mathf.RoundToInt(RealYV407LikeOriginal(unit)) - (lx << 7)) >> 8;
            // NewMon.cpp::MotionHandlerForSingleStepObjects exact gate:
            // UnitsField.CheckBar(OB->x-1,OB->y-1,OB->Lx+2,OB->Lx+2).
            return C2OriginalMovementSystemV425LikeOriginal.UnitsFieldCheckBarV427LikeOriginal(
                ux - 1, uy - 1, lx + 2, lx + 2);
        }

        // ---- V427 moved from BrigadeBattle: IsKeepPositionsSlotOccupiedV415LikeOriginal ----
        private static bool IsKeepPositionsSlotOccupiedV415LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal mover, Vector2 slot)
        {
            // BrigadeOrders.cpp::KeepPositions uses the real UnitsField bitmap:
            // CheckBar(xx1,yy1,Lx,Lx).  V427 no longer reconstructs occupancy by
            // scanning live units; it reads the unified movement-owned UnitsField.
            int lx = C2OriginalMovementSystemV425LikeOriginal.ResolveLxLikeOriginal(mover);
            int targetX = (Mathf.RoundToInt(slot.x) - (lx << 7)) >> 8;
            int targetY = (Mathf.RoundToInt(slot.y) - (lx << 7)) >> 8;
            return C2OriginalMovementSystemV425LikeOriginal.UnitsFieldCheckBarV427LikeOriginal(
                targetX, targetY, lx, lx);
        }

        // ---- V427 moved from BrigadeBattle: CheckMotionThroughEnemyAbilityV414LikeOriginal ----
        internal static C2NeutralPeasantUnitInfoV2LikeOriginal CheckMotionThroughEnemyAbilityV414LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            int nextRealX,
            int nextRealY)
        {
            using (C2FrameCostProbe.Measure(C2FrameCostProbe.Phase.MotionContact))
            {
            if (!IsAliveBattleUnitV407LikeOriginal(unit)) return null;
            RuntimeFormationV172LikeOriginal ownGroup;
            bool inArmy = TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out ownGroup) && ownGroup != null;
            if (inArmy && C2CombatCoreV408LikeOriginal.TryGetAttackObjTargetV408LikeOriginal(unit, out var liveAttack) && liveAttack != null)
                return null; // OB->BrigadeID!=FFFF && OB->Attack.

            byte opt = 3;
            if (inArmy && unit.GroundStateV396LikeOriginal == 0) opt = 2;
            C2NeutralPeasantUnitInfoV2LikeOriginal currentEnemy;
            int ws = GetEnemyDensityV414LikeOriginal(unit, Mathf.RoundToInt(RealXV407LikeOriginal(unit)),
                Mathf.RoundToInt(RealYV407LikeOriginal(unit)), opt, out currentEnemy);
            ws += ws / 10;
            C2NeutralPeasantUnitInfoV2LikeOriginal enemy;
            int wd = GetEnemyDensityV414LikeOriginal(unit, nextRealX, nextRealY, opt, out enemy);
            if (ws >= wd || enemy == null) return null;

            if (!GetNoSearchVictimV403LikeOriginal(unit) && unit.ActivityStateV413LikeOriginal == 2)
            {
                bool precise;
                C2CombatCoreV408LikeOriginal.TryAttackObjV408LikeOriginal(unit, enemy, 128 + 1, out precise);
            }
            return enemy;
            }
        }

        // ---- V427 moved from BrigadeBattle: CommitSingleStepContactAttackV414LikeOriginal ----
        internal static void CommitSingleStepContactAttackV414LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            C2NeutralPeasantUnitInfoV2LikeOriginal enemy)
        {
            if (!IsAliveBattleUnitV407LikeOriginal(unit) || !IsAliveBattleUnitV407LikeOriginal(enemy) ||
                GetNoSearchVictimV403LikeOriginal(unit)) return;
            BeginOrKeepMeleeAttackObjWithPriorityV414LikeOriginal(
                unit, enemy, true, 128 + 15,
                "NewMon.cpp::MotionHandlerForSingleStepObjects_contact");
        }

        // ---- V427 moved from BrigadeBattle: GetEnemyDensityV414LikeOriginal ----
        private static int GetEnemyDensityV414LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal attacker,
            int realX,
            int realY,
            byte opt,
            out C2NeutralPeasantUnitInfoV2LikeOriginal enemyId)
        {
            enemyId = null;
            int px = realX >> 11;
            int py = realY >> 11;
            byte mask = C2CombatCoreV408LikeOriginal.GetNMaskV408LikeOriginal(attacker);
            int count = 0;
            int weight = 0;
            // Brigade.cpp::GetEnemyDensity reads one spatial cell, not all Group[].
            // Preserve this port's live-coordinate membership and object order.
            // Native NPresence[cell] & ~Mask rejects cells containing allies only.
            // Membership/allegiance changes invalidate this conservative mask;
            // the existing exact filters and ascending object order remain below.
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> all = C2LiveUnitCellIndex.GetCellWithPotentialEnemies(px, py, mask);
            for (int i = 0; all != null && i < all.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal e = all[i];
                if (e == null || e == attacker) continue;
                // Reject allies by plain unit data before querying runtime readiness
                // or positions. Filters and ascending Index traversal are unchanged.
                if ((C2CombatCoreV408LikeOriginal.GetNMaskV408LikeOriginal(e) & mask) != 0) continue;
                if (!IsAliveBattleUnitV407LikeOriginal(e)) continue;
                if ((Mathf.RoundToInt(RealXV407LikeOriginal(e)) >> 11) != px ||
                    (Mathf.RoundToInt(RealYV407LikeOriginal(e)) >> 11) != py) continue;
                MeleeMdTraitsV407LikeOriginal mt = GetMeleeMdTraitsV407LikeOriginal(e);
                // Brigade.cpp::GetEnemyDensity: OBJ->newMons->Artilery || OBJ->UnlimitedMotion.
                if (mt.Artilery || IsUnlimitedMotionV415LikeOriginal(e)) continue;
                int rr = C2OriginalMovementMathV352.Norma(
                    realX - Mathf.RoundToInt(RealXV407LikeOriginal(e)),
                    realY - Mathf.RoundToInt(RealYV407LikeOriginal(e)));
                if (rr >= 800) continue;
                int eg;
                byte br = TryGetFormationGroupIdV321LikeOriginal(e, out eg) ? (byte)2 : (byte)1;
                if ((opt & br) == 0) continue;
                count++;
                weight += 5120 / (512 + rr);
                enemyId = e; // GetEnemyDensity writes each matching MID; final one survives.
            }
            if (count <= 2) weight = 0;
            return weight;
        }
    }
}


// ============================================================================
// V428: loose-group PositionOrder/NewMonsterSmartSendTo ownership moved here.
// ============================================================================
namespace Cossacks2Bridge.UnityAdapters.Maps
{
    internal static class C2GameplayLooseGroupMoveLikeOriginal
    {
        private const float C2LooseGroupDefaultRadius2RealLikeOriginal = 160.0f;
        private const int C2LooseGroupFormDistLikeOriginal = 270;

        // Compatibility overload: ordinary command is OrdType 0.
        public static int IssueMoveLikeOriginal(
            System.Collections.Generic.IList<C2NeutralPeasantUnitInfoV2LikeOriginal> sourceUnits,
            float destRealCenterX,
            float destRealCenterY,
            bool hasFinalFacingDir,
            byte finalFacingDir,
            string cancelSource,
            out string audit)
        {
            return IssueMoveLikeOriginal(
                sourceUnits, destRealCenterX, destRealCenterY,
                hasFinalFacingDir, finalFacingDir, 0,
                cancelSource, out audit);
        }

        public static int IssueMoveLikeOriginal(
            System.Collections.Generic.IList<C2NeutralPeasantUnitInfoV2LikeOriginal> sourceUnits,
            float destRealCenterX,
            float destRealCenterY,
            bool hasFinalFacingDir,
            byte finalFacingDir,
            byte ordType,
            string cancelSource,
            out string audit)
        {
            int formationIssued;
            string formationAudit;
            if (C2FormationRuntimeV167LikeOriginal.TryIssueMoveV167LikeOriginal(
                    sourceUnits,
                    destRealCenterX,
                    destRealCenterY,
                    hasFinalFacingDir,
                    finalFacingDir,
                    ordType,
                    cancelSource,
                    out formationIssued,
                    out formationAudit))
            {
                audit = "original_formation_move " + formationAudit;
                return formationIssued;
            }

            var units = new System.Collections.Generic.List<C2NeutralPeasantUnitInfoV2LikeOriginal>(
                sourceUnits != null ? sourceUnits.Count : 0);
            float centerX = 0.0f;
            float centerY = 0.0f;
            float maxRadius2 = 0.0f;
            int type1 = -1;
            int type2 = -1;
            bool allPus = true;

            if (sourceUnits != null)
            {
                for (int i = 0; i < sourceUnits.Count; i++)
                {
                    C2NeutralPeasantUnitInfoV2LikeOriginal u = sourceUnits[i];
                    if (u == null || !u.isActiveAndEnabled || !u.CanReceivePlayerOrdersLikeOriginal()) continue;
                    units.Add(u);
                    float ux = u.RealXFloat != 0.0f ? u.RealXFloat : u.RealX;
                    float uy = u.RealYFloat != 0.0f ? u.RealYFloat : u.RealY;
                    centerX += ux;
                    centerY += uy;
                    float rr = u.GeometryRadius2Real > 0 ? u.GeometryRadius2Real : C2LooseGroupDefaultRadius2RealLikeOriginal;
                    if (rr > maxRadius2) maxRadius2 = rr;
                    if (type1 < 0) type1 = u.NIndex;
                    else if (type1 != u.NIndex) type2 = u.NIndex;

                    // Groups.cpp::ExGroupSendSelectedTo AllPus:
                    // Usage==PushkaID OR newMons->Artpodgotovka for every loose unit.
                    C2UnitOriginalRuntimeLinkLikeOriginal mdLink = u.RuntimeLinkCachedLikeOriginal;
                    C2UnitOriginalRuntime mdRuntime = mdLink != null ? mdLink.Runtime : null;
                    bool pushka = mdRuntime != null && mdRuntime.Md != null &&
                                  string.Equals(mdRuntime.Md.Usage, "PUSHKA", StringComparison.OrdinalIgnoreCase);
                    bool artpodgotovka = mdRuntime != null && mdRuntime.Md != null && mdRuntime.Md.Artpodgotovka;
                    if (!pushka && !artpodgotovka) allPus = false;
                }
            }

            int n = units.Count;
            if (n == 0)
            {
                audit = "C2 Groups.cpp ExGroupSendSelectedTo issued=0 reason=no_controllable_units";
                return 0;
            }
            centerX /= n;
            centerY /= n;

            // Groups.cpp::ExGroupSendSelectedTo always resolves LastDirection before
            // PositionOrder::SendToPosition.  A normal RMB click starts with DIRECT=512,
            // then loose-only selection resolves LastDirection=GetDir(destination-center).
            // SendToPosition appends RotUnit(...,LastDirection,2) after SmartSend for
            // OrdType 0/2.  Therefore ordinary clicks have a final facing too; V350/V351
            // incorrectly passed HasFinalFacing=false and dropped that Order1 node.
            int clickRdxV352 = Mathf.RoundToInt(centerX - destRealCenterX);
            int clickRdyV352 = Mathf.RoundToInt(centerY - destRealCenterY);
            byte pordLastDirectionV352 = hasFinalFacingDir
                ? finalFacingDir
                : C2OriginalMovementMathV352.GetDir(-clickRdxV352, -clickRdyV352);

            if (n == 1)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal single = units[0];
                bool ok = C2OriginalOrderChainV352.SubmitSmartMoveV428LikeOriginal(
                    single,
                    destRealCenterX, destRealCenterY,
                    destRealCenterX, destRealCenterY,
                    true, pordLastDirectionV352, ordType,
                    cancelSource ?? "PORD_single_NewMonsterSmartSendTo_v428");
                audit = "C2 PORD.SendToPosition single issued=" + (ok ? "1" : "0") +
                        " route=NewMonsterSmartSendTo/original_movement_owner";
                return ok ? 1 : 0;
            }

            // Groups.cpp::ExGroupSendSelectedTo. With DIRECT=512, rdx/rdy are
            // average selected RealX/Y minus the clicked RealX/Y.
            int rdx = clickRdxV352;
            int rdy = clickRdyV352;
            if (hasFinalFacingDir)
            {
                rdx = C2OriginalMovementMathV352.TCos[finalFacingDir] << 4;
                rdy = C2OriginalMovementMathV352.TSin[finalFacingDir] << 4;
            }

            // PositionOrder::CreateRotatedPositions starts by dx>>=4,dy>>=4,
            // then rotates that vector 90 degrees.
            int dx = rdx >> 4;
            int dy = rdy >> 4;
            if (dx == 0 && dy == 0) dx = 1;

            int lx = (int)Mathf.Sqrt(n);
            int ly;
            if (allPus)
            {
                // Groups.cpp::PositionOrder::CreateRotatedPositions2.
                ly = lx << 2;
                lx >>= 2;
                if (n < 10)
                {
                    lx = 1;
                    ly = n;
                }
            }
            else
            {
                // Groups.cpp::PositionOrder::CreateRotatedPositions.
                ly = lx * 5 / 3;
                lx = lx * 3 / 5;
                if (n < 4)
                {
                    lx = 1;
                    ly = n;
                }
            }
            int dd = dx;
            dx = dy;
            dy = -dd;

            // The source performs this grow block twice, not an unbounded while.
            for (int grow = 0; grow < 2; grow++)
            {
                int nn = lx * ly;
                if (nn < n)
                {
                    if (nn + lx >= n) ly++;
                    else if (nn + ly >= n) lx++;
                    else { ly++; lx++; }
                }
            }
            if (lx < 1) lx = 1;
            if (ly < 1) ly = 1;

            // UNISORT.CreateByLine(Ids,NUnits,dx>>4,dy>>4), ascending.
            int sortDx = dx >> 4;
            int sortDy = dy >> 4;
            units.Sort(delegate(C2NeutralPeasantUnitInfoV2LikeOriginal a, C2NeutralPeasantUnitInfoV2LikeOriginal b)
            {
                int ax = Mathf.RoundToInt((a.RealXFloat != 0.0f ? a.RealXFloat : a.RealX)) >> 5;
                int ay = Mathf.RoundToInt((a.RealYFloat != 0.0f ? a.RealYFloat : a.RealY)) >> 5;
                int bx = Mathf.RoundToInt((b.RealXFloat != 0.0f ? b.RealXFloat : b.RealX)) >> 5;
                int by = Mathf.RoundToInt((b.RealYFloat != 0.0f ? b.RealYFloat : b.RealY)) >> 5;
                long ap = (long)ax * sortDx + (long)ay * sortDy;
                long bp = (long)bx * sortDx + (long)by * sortDy;
                return ap.CompareTo(bp);
            });

            // Then every row is sorted by -dy>>4,dx>>4 exactly like Groups.cpp.
            int px0 = 0;
            for (int iy = 0; iy < ly; iy++)
            {
                int rowCount = n - px0;
                if (rowCount > lx) rowCount = lx;
                if (rowCount <= 0) break;
                int rowStart = px0;
                int rowSortDx = (-dy) >> 4;
                int rowSortDy = dx >> 4;
                units.Sort(rowStart, rowCount, Comparer<C2NeutralPeasantUnitInfoV2LikeOriginal>.Create(
                    delegate(C2NeutralPeasantUnitInfoV2LikeOriginal a, C2NeutralPeasantUnitInfoV2LikeOriginal b)
                    {
                        int ax = Mathf.RoundToInt((a.RealXFloat != 0.0f ? a.RealXFloat : a.RealX)) >> 5;
                        int ay = Mathf.RoundToInt((a.RealYFloat != 0.0f ? a.RealYFloat : a.RealY)) >> 5;
                        int bx = Mathf.RoundToInt((b.RealXFloat != 0.0f ? b.RealXFloat : b.RealX)) >> 5;
                        int by = Mathf.RoundToInt((b.RealYFloat != 0.0f ? b.RealYFloat : b.RealY)) >> 5;
                        long ap = (long)ax * rowSortDx + (long)ay * rowSortDy;
                        long bp = (long)bx * rowSortDx + (long)by * rowSortDy;
                        return ap.CompareTo(bp);
                    }));
                px0 += rowCount;
            }

            int maxR = Mathf.RoundToInt(maxRadius2);
            if (allPus)
            {
                // CreateRotatedPositions2: no mixed-type FORMDIST clamp and no 3/4 squeeze.
                maxR <<= 2;
            }
            else
            {
                if (type2 != -1 && maxR > C2LooseGroupFormDistLikeOriginal * 4)
                    maxR = C2LooseGroupFormDistLikeOriginal * 4;
                maxR = maxR * 3 / 4;
                maxR <<= 2;
            }

            int nr = C2OriginalMovementMathV352.Norma(dx, dy);
            if (nr <= 0) nr = 1;
            int vx = dx * maxR / nr;
            int vy = dy * maxR / nr;
            int dxx = (-(lx - 1) * vy + (ly - 1) * vx) >> 1;
            int dyy = ((lx - 1) * vx + (ly - 1) * vy) >> 1;

            var slots = new System.Collections.Generic.List<Vector2>(n);
            int pos = 0;
            for (int iy = 0; iy < ly; iy++)
            {
                for (int ix = 0; ix < lx; ix++)
                {
                    if (pos < n)
                    {
                        slots.Add(new Vector2(
                            Mathf.RoundToInt(destRealCenterX) - ix * vy + iy * vx - dxx,
                            Mathf.RoundToInt(destRealCenterY) + ix * vx + iy * vy - dyy));
                    }
                    pos++;
                }
            }

            // V428: PORD only assigns each object its rotated destination.
            // Do not prebuild a Unity centre A*/LOCKPOINTS route and translate it
            // across the crowd. Each object enters the single original movement
            // owner (NewMonsterSmartSendTo/CreatePath/topology) with its own slot.
            int issued = 0;
            for (int i = 0; i < units.Count && i < slots.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = units[i];
                Vector2 slot = slots[i];
                bool ok = C2OriginalOrderChainV352.SubmitSmartMoveV428LikeOriginal(
                    u,
                    destRealCenterX, destRealCenterY,
                    slot.x, slot.y,
                    true, pordLastDirectionV352, ordType,
                    cancelSource ?? "PORD_NewMonsterSmartSendTo_v428");
                if (ok) issued++;
            }

            audit = "C2 Groups.cpp->PORD.CreateRotatedPositions->SendToPosition->NewMonsterSmartSendTo" +
                    " issued=" + issued.ToString() +
                    " units=" + n.ToString() +
                    " grid=" + lx.ToString() + "x" + ly.ToString() +
                    " maxR=" + maxR.ToString() +
                    " allPus=" + allPus.ToString() +
                    " ordType=" + ordType.ToString() +
                    " lastDirection=" + pordLastDirectionV352.ToString() +
                    " route=per_unit_original_movement_owner";
            return issued;
        }
    }
}
