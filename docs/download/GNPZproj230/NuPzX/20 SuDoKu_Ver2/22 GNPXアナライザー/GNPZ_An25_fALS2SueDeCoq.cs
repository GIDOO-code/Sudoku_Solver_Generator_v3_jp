using System;
using System.Collections.Generic;
using System.Linq;
using static System.Console;

using GIDOO_space;

namespace GNPZ_sdk{
//  Sue de Coq
//  http://hodoku.sourceforge.net/en/tech_misc.php#sdc
//  A more formal definition of SDC is given in the original Two-Sector Disjoint Subsets thread:
//  Consider the set of unfilled cells C that lies at the intersection of Box B and Row (or Column) R.
//  Suppose |C|>=2. Let V be the set of candidate values to occur in C. Suppose |V|>=|C|+2.
//  The pattern requires that we find |V|-|C|+n cells in B and R, with at least one cell in each, 
//  with at least |V|-|C| candidates drawn from V and with n the number of candidates not drawn from V.
//  Label the sets of cells CB and CR and their candidates VB and VR. Crucially,
//  no candidate from V is allowed to appear in VB and VR. 
//  Then C must contain V\(VB U VR) [possibly empty], |VB|-|CB| elements of VB and |VR|-|CR| elements of VR.
//  The construction allows us to eliminate candidates VB U (V\VR) from B\(C U CB), 
//  and candidates VR U (V\VB) from R\(C U CR).
// (\:backslash)

    public partial class AALSTechGen: AnalyzerBaseV2{
		private int GStageMemo;
		private ALSLinkMan fALS;

		public AALSTechGen( GNPX_AnalyzerMan pAnMan ): base(pAnMan){
			fALS = new ALSLinkMan(pAnMan);
        }

        public bool SueDeCoq( ){
			if(pAnMan.GStage!=GStageMemo){
				GStageMemo=pAnMan.GStage;
				fALS.Initialize();
	            fALS.PrepareALSLinkMan(+2);
			}
			
            if( fALS.ALSLst.Count<=3 ) return false;   //+1と+2のfakeALSを生成

            foreach( var ISPB in fALS.ALSLst.Where(p=> p.tfx>=18 && p.Size>=3) ){//ブロックfALS選択
                if( ISPB.rcbRow.BitCount()<=1 || ISPB.rcbCol.BitCount()<=1 ) continue;　//ブロック枡は複数行・列

                //▼行(列)fALS選択
                foreach( var ISPR in fALS.ALSLst.Where(p=> p.tfx<18 && p.Size>=3) ){　//行fALS選択
                    if( (ISPR.rcbBlk&ISPB.rcbBlk)==0 ) continue; //ブロックb0と交差あり
                    if( ISPR.rcbBlk.BitCount()<2 )     continue; //行(列)fALSは複数ブロック

                    //交差部のセル構成は同じか
                    if( (ISPB.B81&HouseCells[ISPR.tfx]) != (ISPR.B81&HouseCells[ISPB.tfx]) ) continue;

                    Bit81 IS = ISPB.B81&ISPR.B81;                //◆交差部(Bit81表現)
                    if( IS.Count<2 ) continue; 　                //交差部は2セル以上
                    if( (ISPR.B81-IS).Count==0 ) continue;       //行(列)ALSに交差部以外の部分がある                    

                    Bit81 PB = ISPB.B81-IS;                      //(ISPBのIS外)
                    Bit81 PR = ISPR.B81-IS;                      //(ISPRのIS外)
                    int IS_FreeB = IS.AggregateFreeB(pBDL);      //(交差部数字)
                    int PB_FreeB = PB.AggregateFreeB(pBDL);      //(ISPBのIS外の数字)
                    int PR_FreeB = PR.AggregateFreeB(pBDL);      //(ISPRのIS外の数字)
                    if( (IS_FreeB&PB_FreeB&PR_FreeB)>0 ) continue;

                    //A.DifSet(B)=A-B=A&(B^0x1FF)
                    int PB_FreeBn = PB_FreeB.DifSet(IS_FreeB);   //ブロックの交差部に無い数字
                    int PR_FreeBn = PR_FreeB.DifSet(IS_FreeB);   //行(列)の交差部に無い数字

                    int sdqNC = PB_FreeBn.BitCount()+PR_FreeBn.BitCount();  //交差部外確定の数字数
                    if( (IS_FreeB.BitCount()-IS.Count) != (PB.Count+PR.Count-sdqNC) ) continue;

                    int elmB = PB_FreeB | IS_FreeB.DifSet(PR_FreeB); //ブロックの除外数字 
                    int elmR = PR_FreeB | IS_FreeB.DifSet(PB_FreeB); //行(列)の除外数字                
                    if( elmB==0 && elmR==0 ) continue;

                    foreach( var P in _GetRestCells(ISPB,elmB) ){ P.CancelB|=P.FreeB&elmB; SolCode=2; }
                    foreach( var P in _GetRestCells(ISPR,elmR) ){ P.CancelB|=P.FreeB&elmR; SolCode=2; }

                    if(SolCode>0){//--- SueDeCoq fond -----
                        SolCode=2;
                        SuDoQueEx_SolResult( ISPB, ISPR );
                        if( ISPB.Level>=3 || ISPB.Level>=3 ) WriteLine("Level-3");
                        if( !pAnMan.SnapSaveGP(true) )  return true;
                    }
                }
            }
            return false;
        }

        public IEnumerable<UCell> _GetRestCells( UALS ISP, int selB ){
            return pBDL.IEGetCellInHouse(ISP.tfx,selB).Where(P=>!ISP.B81.IsHit(P.rc));
        }
        private void SuDoQueEx_SolResult( UALS ISPB, UALS ISPR ){
            Result="SueDeCoq";

            if( SolInfoDsp ){
                ISPB.UCellLst.ForEach(P=> P.SetNoBBgColor(P.FreeB,AttCr,SolBkCr) );
                ISPR.UCellLst.ForEach(P=> P.SetNoBBgColor(P.FreeB,AttCr,SolBkCr) );

                string ptmp = "";
                ISPB.UCellLst.ForEach(p=>{ ptmp+=" r"+(p.r+1)+"c" + (p.c+1); } );

                string po = "\r Cells";
                if( ISPB.Level==1 ) po += "(block)  ";
                else{ po += "-"+ISPB.Level+"(block)"; }
                po += ": "+ISPB.ToStringRCN();

                po += "\r Cells" + ((ISPR.Level==1)? "": "-2");
                po += ((ISPR.tfx<9)? "(row)":"(col)");
                po += ((ISPR.Level==1)? "    ": "  ");
                po += ": "+ISPR.ToStringRCN();
                ResultLong = "SueDeCoq"+po;
            }
        }
    }
}