using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Media;

using GIDOO_space;

namespace GNPZ_sdk{

	//▼ 開発中 ▼
    public partial class GroupedLinkGen: AnalyzerBaseV2{
		public static DevelopWin devWin; //▼development

		public bool ForceChain_Cell( ){
			Prepare();
            pSprLKsMan.PrepareSuperLinkMan( AllF:true );	//218
			Bit81[] GLC=new Bit81[9];
			for( int k=0; k<9; k++ ) GLC[k]=new Bit81(all:true);

			foreach( var P0 in pBDL.Where(p=>(p.FreeB>0)) ){
				Bit81[] sTrue=new Bit81[9];
				for( int k=0; k<9; k++ ) sTrue[k]=new Bit81(all:true);

				foreach( var noH in P0.FreeB.IEGet_BtoNo() ){
					int noB=(1<<noH);
					USuperLink USLK=pSprLKsMan.get_L2SprLK(P0.rc,noH,CorrectF:false,DevelopB:false); //#######false
					if(!USLK.SolFond)  goto nextSearch;
					for( int k=0; k<9; k++ ) sTrue[k] &= USLK.Qtrue[k];
				}

				bool solved=false;
				for( int nox=0; nox<9; nox++ ){
					sTrue[nox].BPReset(P0.rc);
					if( !sTrue[nox].IsZero() )  solved=true;
				}

				if(solved) _ForceChainCellDisp(sTrue,P0.rc);

			  nextSearch:
				continue;
            }
            return (SolCode>0);
        }
        private  bool _ForceChainCellDisp( Bit81[] sTrue, int mX ){
			UCell P0=pBDL[mX];	//着目セル
			string dspOpt = GNPXpzl.GMthdOption["ForceLx"];
			string st0="", st2="";

			for( int nox=0; nox<9; nox++ ){
				if( sTrue[nox].IsZero() )  continue;

				foreach( var rc in sTrue[nox].IEGet_rc() ){ //確定セル
					UCell Q=pBDL[rc];
					Q.FixedNo=nox+1;
					int elm=Q.FreeB.DifSet(1<<nox);
					Q.CancelB = elm;

					SolCode=1;
					st0 = "ForceChain_Cell r"+(Q.r+1)+"c"+(Q.c+1)+"/+"+(nox+1)+" is true";
					Result = st0;

					if(SolInfoDsp){
						P0.SetNoBBgColor(P0.FreeB,Colors.Green,Colors.Yellow);
						Q.SetNoBBgColor(1<<nox, Colors.Red , Colors.LightGreen );
						Q.SetNoBColorRev(elm,Colors.Red );

						string st1="";
						foreach( var no in pBDL[mX].FreeB.IEGet_BtoNo() ){
							USuperLink USLK = pSprLKsMan.get_L2SprLK(mX,no,CorrectF:true,DevelopB:false); //正確なパス
							st1 += "\r"+pSprLKsMan._GenMessage2true(USLK,Q,nox);
						}

						if(st2!="") st2+="\r";
						st2 += st0+st1;
						ResultLong = st0;
						extRes = st2;

						if( dspOpt=="ForceL0" ){
							if( !pAnMan.SnapSaveGP(true) )  return true;
							st2="";
						}
					}
				}
				if( SolInfoDsp && dspOpt=="ForceL1" && st2!="" ){
					st0 = "ForceChain_Cell";
					Result = st0;
					ResultLong = st0;
					extRes = st2;
					if( !pAnMan.SnapSaveGP(true) )  return true;
					st2="";
				}
			}
			if( SolInfoDsp && dspOpt=="ForceL2" && st2!="" ){	
				st0 = "ForceChain_Cell";
				Result = st0;
				ResultLong = st0;
				extRes = st2;
				if( !pAnMan.SnapSaveGP(true) )  return true;
			}
			return (SolCode>0);
        }
		
		public bool ForceChain_House( ){
			Prepare();
            pSprLKsMan.PrepareSuperLinkMan( AllF:true );	//218
			Bit81[] GLC=new Bit81[9];
			for( int k=0; k<9; k++ ) GLC[k]=new Bit81(all:true);

			for( int hs=0; hs<27; hs++ ){
				int noBs=pBDL.IEGetCellInHouse(hs).Aggregate(0,(Q,P)=>Q|(P.FreeB));

				foreach( var noH in noBs.IEGet_BtoNo() ){
					int noB=(1<<noH);
					Bit81[] sTrue=new Bit81[9];
					for( int k=0; k<9; k++ ) sTrue[k]=new Bit81(all:true);

					foreach( var P in pBDL.IEGetCellInHouse(hs,noB) ){
						USuperLink USLK=pSprLKsMan.get_L2SprLK(P.rc,noH,CorrectF:false,DevelopB:false); //#######false
						if(!USLK.SolFond)  goto nextSearch;
						for( int k=0; k<9; k++ ) sTrue[k] &= USLK.Qtrue[k];
					}

					bool solved=false;
					for( int nox=0; nox<9; nox++ ){
						sTrue[nox] -= HouseCells[hs];
						if( !sTrue[nox].IsZero() )  solved=true;
					}

					if(solved)  _ForceChainHouseDisp(sTrue,hs,noH);
				}
			  nextSearch:
				continue;
            }
            return (SolCode>0);
        }	
		private  bool _ForceChainHouseDisp( Bit81[] sTrue, int hs, int noH ){
			string dspOpt = GNPXpzl.GMthdOption["ForceLx"];

			string st0="", st2="";
			UCell P0=pBDL[hs];
			for( int nox=0; nox<9; nox++ ){
				if( sTrue[nox].IsZero() )  continue;

				foreach( var rc in sTrue[nox].IEGet_rc() ){
					UCell Q=pBDL[rc];
					Q.FixedNo=nox+1;
					int elm=Q.FreeB.DifSet(1<<nox);
					Q.CancelB = elm;
					SolCode=1;
					st0 = "ForceChain_House("+HouseToString(hs)+"/#"+(noH+1)+") r"+(Q.r+1)+"c"+(Q.c+1)+"/+"+(nox+1)+" is true";
					Result = st0;

					if(SolInfoDsp){
						Q.SetNoBBgColor(1<<nox, Colors.Red , Colors.LightGreen );
						Q.SetNoBColorRev(elm,Colors.Red );

						string st1="";
						foreach( var P in pBDL.IEGetCellInHouse(hs,1<<noH) ){
							USuperLink USLK = pSprLKsMan.get_L2SprLK(P.rc,noH,CorrectF:true,DevelopB:false); //正確なパス
							st1 += "\r"+pSprLKsMan._GenMessage2true(USLK,Q,nox);
							P.SetNoBBgColor(1<<noH, Colors.Green , Colors.Yellow );
						}

						if(st2!="") st2+="\r";
						st2 += st0+st1;
						ResultLong = st0;
						extRes = st2;

						if( dspOpt=="ForceL0" ){
							if( !pAnMan.SnapSaveGP(true) )  return true;
							st2="";
						}
					}
				}
				if( SolInfoDsp && dspOpt=="ForceL1" && st2!="" ){	
					st0 = "ForceChain_House("+HouseToString(hs)+"/#"+(noH+1)+")";
					Result = st0;
					ResultLong = st0;
					extRes = st2;
					if( !pAnMan.SnapSaveGP(true) )  return true;
					st2="";
				}
			}
			if( SolInfoDsp && dspOpt=="ForceL2" && st2!="" ){	
				st0 = "ForceChain_House("+HouseToString(hs)+")";
				Result = st0;
				ResultLong = st0;
				extRes = st2;
				if( !pAnMan.SnapSaveGP(true) )  return true;
			}
			return (SolCode>0);
        }
    }  
}