using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Media;
using static System.Math;

using GIDOO_space;

namespace GNPZ_sdk{
    public delegate bool dSolver();

    public class TagAn{
        static private int ID0=0;
        public int        ID;
        public string     MethodName;
        public bool       Enabled;
        public int        DifLevel; 
        public int        UsedCC;
        public dSolver    Method;

        public TagAn( int ID, string MethodName, int DifLevel, dSolver Method ){
            this.ID         = (ID<<10)+(ID0++); //stableSort
            this.MethodName = MethodName;
            this.DifLevel   = DifLevel;
            this.Method     = Method;
            this.UsedCC     = 0;
            this.Enabled    = true; 
        }
        public override string ToString(){
            return (MethodName.PadRight(30)+"["+DifLevel+"]"+(Enabled? "T": "F")+" "+UsedCC);
        }
    }

    public class GNPX_AnalyzerMan{      
        public GNPZ_Engin   pENGN;
        public UProblem     pGP{        get{return pENGN.pGP;} }
		public int			GStage=0;

        public bool         Insoluble;
        public List<UCell>  pBDL{       get{return pENGN.pGP.BDL;} }
        public bool         MltSolOn{   get{return GNPXpzl.MltSolOn;} }
        public bool         SolInfoDsp{ get{return GNPZ_Engin.SolInfoDsp;} }

        public int          SolCode{    set{pGP.SolCode=value;} get{return pGP.SolCode;} }
        public string       Result{     set{pGP.GNPX_Result=value;} }
        public string       ResultLong{ set{pGP.GNPX_ResultLong=value;} }
        public TimeSpan     SdkExecTime;

		public SuperLinkMan SprLKsMan;
        public List<TagAn>  SolverLst;

        public  GNPX_AnalyzerMan( GNPZ_Engin pENGN, UProblem pGP ){
            SolverLst=new List<TagAn>();
            this.pENGN=pENGN;

			SprLKsMan=new SuperLinkMan(this);

            var SSingle=new SimpleSingleGen(this);
            SolverLst.Add( new TagAn( 1, "LastDigit",    1, SSingle.LastDigit ) );
            SolverLst.Add( new TagAn( 2, "NakedSingle",  1, SSingle.NakedSingle ) );
            SolverLst.Add( new TagAn( 3, "HiddenSingle", 1, SSingle.HiddenSingle ) );

            var LockedCand=new LockedCandidateGen(this);
            SolverLst.Add( new TagAn( 4, "LockedCandidate", 1, LockedCand.LockedCandidate ) );
                
            var LockedSet=new LockedSetGen(this);
            SolverLst.Add( new TagAn( 10, "LockedSet(2D)",        2, LockedSet.LockedSet2 ) );
            SolverLst.Add( new TagAn( 12, "LockedSet(3D)",        3, LockedSet.LockedSet3 ) );
            SolverLst.Add( new TagAn( 14, "LockedSet(4D)",        4, LockedSet.LockedSet4 ) );
            SolverLst.Add( new TagAn( 16, "LockedSet(5D)",       -5, LockedSet.LockedSet5 ) );
            SolverLst.Add( new TagAn( 18, "LockedSet(6D)",       -5, LockedSet.LockedSet6 ) );
            SolverLst.Add( new TagAn( 20, "LockedSet(7D)",       -5, LockedSet.LockedSet7 ) );           
            SolverLst.Add( new TagAn( 11, "LockedSet(2D)Hidden",  2, LockedSet.LockedSet2Hidden ) );           
            SolverLst.Add( new TagAn( 13, "LockedSet(3D)Hidden",  3, LockedSet.LockedSet3Hidden ) );          
            SolverLst.Add( new TagAn( 15, "LockedSet(4D)Hidden",  4, LockedSet.LockedSet4Hidden ) );
            SolverLst.Add( new TagAn( 17, "LockedSet(5D)Hidden", -5, LockedSet.LockedSet5Hidden ) );
            SolverLst.Add( new TagAn( 19, "LockedSet(6D)Hidden", -5, LockedSet.LockedSet6Hidden ) );            
            SolverLst.Add( new TagAn( 21, "LockedSet(7D)Hidden", -5, LockedSet.LockedSet7Hidden ) );

            var Fish=new FishGen(this);
            SolverLst.Add( new TagAn( 30, "XWing",            3, Fish.XWing ) );
            SolverLst.Add( new TagAn( 31, "SwordFish",        4, Fish.SwordFish ) );
            SolverLst.Add( new TagAn( 32, "JellyFish",        5, Fish.JellyFish ) );
            SolverLst.Add( new TagAn( 33, "Squirmbag",       -5, Fish.Squirmbag ) );
            SolverLst.Add( new TagAn( 34, "Whale",           -5, Fish.Whale ) );
            SolverLst.Add( new TagAn( 35, "Leviathan",       -5, Fish.Leviathan ) );

            SolverLst.Add( new TagAn( 40, "Finned XWing",     4, Fish.FinnedXWing ) );
            SolverLst.Add( new TagAn( 41, "Finned SwordFish", 5, Fish.FinnedSwordFish ) );
            SolverLst.Add( new TagAn( 42, "Finned JellyFish", 5, Fish.FinnedJellyFish ) );
            SolverLst.Add( new TagAn( 43, "Finned Squirmbag", 6, Fish.FinnedSquirmbag ) );
            SolverLst.Add( new TagAn( 44, "Finned Whale",     6, Fish.FinnedWhale ) );
            SolverLst.Add( new TagAn( 45, "Finned Leviathan", 6, Fish.FinnedLeviathan ) );

            SolverLst.Add( new TagAn( 90, "Franken/MutantFish",         7, Fish.FrankenMutantFish ) );
            SolverLst.Add( new TagAn( 91, "Finned Franken/Mutant Fish", 7, Fish.FinnedFrankenMutantFish ) );

            SolverLst.Add( new TagAn( 100, "EndoFinned F/M Fish",      10, Fish.EndoFinnedFMFish ) );
            SolverLst.Add( new TagAn( 101, "Cannibalistic F/M Fish",   10, Fish.CannibalisticFMFish ) );

            var nxgCellLink=new NXGCellLinkGen(this);
            SolverLst.Add( new TagAn( 50, "Skyscraper",       4, nxgCellLink.Skyscraper ) );
            SolverLst.Add( new TagAn( 51, "EmptyRectangle",   4, nxgCellLink.EmptyRectangle ) );
            SolverLst.Add( new TagAn( 52, "XY-Wing",          5, nxgCellLink.XYwing ) );
            SolverLst.Add( new TagAn( 53, "W-Wing",           6, nxgCellLink.Wwing ) );

            SolverLst.Add( new TagAn( 55, "RemotePair",       5, nxgCellLink.RemotePair ) );    
            SolverLst.Add( new TagAn( 56, "XChain",           6, nxgCellLink.XChain ) );
            SolverLst.Add( new TagAn( 57, "XYChain",          6, nxgCellLink.XYChain ) ); 
           
            SolverLst.Add( new TagAn( 60, "Color-Trap",       5, nxgCellLink.Color_Trap ) );
            SolverLst.Add( new TagAn( 61, "Color-Wrap",       5, nxgCellLink.Color_Wrap ) );
            SolverLst.Add( new TagAn( 62, "MultiColor-Type1", 6, nxgCellLink.MultiColor_Type1 ) );
            SolverLst.Add( new TagAn( 63, "MultiColor-Type2", 6, nxgCellLink.MultiColor_Type2 ) );
         
/*
            var SimpleXYZ=new SimpleUVWXYZwingGen(this);
            SolverLst.Add( new TagAn( 70, "XYZ-Wing",         5, SimpleXYZ.XYZwing ) );
            SolverLst.Add( new TagAn( 71, "WXYZ-Wing",        5, SimpleXYZ.WXYZwing ) );
            SolverLst.Add( new TagAn( 72, "VWXYZ-Wing",       6, SimpleXYZ.VWXYZwing ) );
            SolverLst.Add( new TagAn( 73, "UVWXYZ-Wing",      6, SimpleXYZ.UVWXYZwing ) );
*/
            var ALSTechP=new AALSTechGen(this);  //fakeALS(2次ALS)
            SolverLst.Add( new TagAn( 59, "SueDeCoq",         5, ALSTechP.SueDeCoq ) );

            var ALSTech=new ALSTechGen(this);
            SolverLst.Add( new TagAn( 75, "XYZ-WingALS",         7, ALSTech.XYZwingALS ) );
            SolverLst.Add( new TagAn( 80, "ALS-XZ",              7, ALSTech.ALS_XZ ) );
            SolverLst.Add( new TagAn( 81, "ALS-XY-Wing",         8, ALSTech.ALS_XY_Wing ) );
            SolverLst.Add( new TagAn( 82, "ALS-Chain",           9, ALSTech.ALS_Chain ) );
            SolverLst.Add( new TagAn( 83, "ALS-DeathBlossom",    9, ALSTech.ALS_DeathBlossom ) );
            SolverLst.Add( new TagAn( 83, "ALS-DeathBlossomExt", 9, ALSTech.ALS_DeathBlossomExt ) );

            var NLTech=new NiceLoopGen(this);
            SolverLst.Add( new TagAn( 96, "NiceLoop",          10, NLTech.NiceLoop ) );

            var GNLTech=new GroupedLinkGen(this);
			SolverLst.Add( new TagAn(  97, "GroupedNiceLoop",  11, GNLTech.GroupedNiceLoop ) );
            SolverLst.Add( new TagAn( 103, "Kraken Fish",      11, GNLTech.KrakenFish ) );
            SolverLst.Add( new TagAn( 104, "Kraken FinnedFish",11, GNLTech.KrakenFinnedFish ) );

            SolverLst.Add( new TagAn( 111, "ForceChain_Cell",  11, GNLTech.ForceChain_Cell ) );
			SolverLst.Add( new TagAn( 112, "ForceChain_House", 11, GNLTech.ForceChain_House ) );
			SolverLst.Add( new TagAn( 113, "ForceChain_Contradiction",  11, GNLTech.ForceChain_Contradiction ) );
        //  SolverLst.Add( new TagAn( 190, "ForceChainNetDevelop", 11, GNLTech.ForceChainNetDevelop ) );

            SolverLst.Sort((a,b)=>(a.ID-b.ID));
        }

        public void SolversInitialize(){
			SprLKsMan.Initialize();
        }

//==== 解析ルーティンからの要求、解析ルーティン制御
        public bool SnapSaveGP( bool saveAll=false ){
            if( !SDK_Ctrl.MltAnsSearch )  return false;

            if( SDK_Ctrl.UGPMan.MltUProbLst==null )  SDK_Ctrl.UGPMan.MltUProbLst=new List<UProblem>();
            else if( SDK_Ctrl.UGPMan.MltUProbLst.Count>=(int)SDK_Ctrl.MltAnsOption["複数解上限"] ){
                SDK_Ctrl.MltAnsOption["打切り結果"] = "複数解上限で打切り";
            //    SDK_Ctrl.UGPMan.pGPsel=SDK_Ctrl.UGPMan.MltUProbLst[0];
                return false;
            }

            if( saveAll || SDK_Ctrl.UGPMan.MltUProbLst.All(P=>(P.GNPX_ResultLong!=pGP.GNPX_ResultLong)) ){
                int IDm=SDK_Ctrl.UGPMan.MltUProbLst.Count;
                UProblem GPX=pGP.Copy(pGP.stageNo+1,IDm);
                SDK_Ctrl.UGPMan.MltUProbLst.Add(GPX);
            }

            pBDL.ForEach(p=>p.ResetAnalysisResult());
            pGP.SolCode=-1;

            return true;
        }
        public bool CheckTimeOut(){ //true:TimeOut //時間のかかる処理のみに組込む
            if( !SDK_Ctrl.MltAnsSearch )  return false;
            TimeSpan ts = DateTime.Now - (DateTime)SDK_Ctrl.MltAnsOption["打切り時間"];
            bool tmout = ts.TotalSeconds >= (int)SDK_Ctrl.MltAnsOption["探索時間上限"];
            if(tmout)  SDK_Ctrl.MltAnsOption["打切り結果"] = "探索時間上限で打切り";
            return tmout;
        }

//==== 解析結果の集計、分析
        public bool AggregateCellsPZM( ref int nP, ref int nZ,ref int nM ){
            int P=0, Z=0, M=0;
            if( pBDL==null )  return false;
            pBDL.ForEach( q =>{
                if(q.No>0)      P++;
                else if(q.No<0) M++;
                else            Z++;
            } );
            nP=P; nZ=Z; nM=M;
            return pBDL.Any(q=>q.FreeB>0);
        }

        private int[] NChk=new int[27];
        public bool FixOrEliminate_SuDoKu( ref int[] eNChk ){// 確定処理
            eNChk=null;
            if( pBDL.Any(p=>p.FixedNo>0) ){
                foreach( var P in pBDL.Where(p=>p.No==0) ){
                    int No=P.FixedNo;
                    if(No<1 && No>9) continue;
                    P.FixedNo=0; P.No=-No;
                    P.CellBgCr=Colors.Black;
                }
                
                Set_CellFreeB(false);
                foreach( var P in pBDL.Where(p=>(p.No==0 && p.FreeBC==0)) )  P.ErrorState=9;

                for( int h=0; h<27; h++ ) NChk[h]=0;
                foreach( var P in pBDL ){
                    int no=(P.No<0)? -P.No: P.No;
                    int H=(no>0)? (1<<(no-1)): P.FreeB;
                    NChk[P.r]|=H; NChk[P.c+9]|=H; NChk[P.b+18]|=H;
                }
                for( int h=0; h<27; h++ ){
                    if(NChk[h]!=0x1FF){ eNChk=NChk; SolCode=-9119; return false; }
                }
            }
            else if( pBDL.Any(p=>p.CancelB>0) ){
                foreach( var P in pBDL.Where(p=>p.CancelB>0) ){
                    int CancelB=P.CancelB^0x1FF;
                    P.FreeB &= CancelB; P.CancelB=0;       
                    P.CellBgCr=Colors.Black;
                }
            }
            else{ return false; }  //解なし
            pBDL.ForEach(P=>P.ECrLst=null);

//          foreach( var P in pBDL.Where(p=>p.FreeBC==1) ){ //LastDigitで確定
//              P.No=-(P.FreeB.BitToNum()+1); P.FreeB=0;
//          }
            SolCode=-1;
            return true;
        }
        public void SetBG_OnError(int h){
            foreach( var P in pBDL.IEGetCellInHouse(h) ) P.SetCellBgColor(Colors.Violet);
        }
        public void Set_CellFreeB( bool allFlag=true ){
            Insoluble=false;
            foreach( var P in pBDL ){
                P.Reset_StepInfo();
                int freeB=0;
                if( P.No==0 ){
                    foreach( var Q in pBDL.IEGetFixed_Pivot27(P.rc) ) freeB |= (1<<Abs(Q.No));
                    freeB=(freeB>>=1)^0x1FF; //右１ビットシフトで内部表現に変換
                    if( !allFlag ) freeB &= P.FreeB;
                    if( freeB==0 ){ Insoluble=true; P.ErrorState=1; }//解なし
                }
                P.FreeB=freeB;
            }
        }
        public bool VerifyRoule_SuDoKu( ){//*** 規則のチェック（House内で同じ数字が使われていたら"false"を返す
            bool    ret=true;

            if( Insoluble==true ){ SolCode=9; return false; }

            for( int tfx=0; tfx<27; tfx++ ){
                int usedB=0, errB=0;
                foreach( var P in pBDL.IEGetCellInHouse(tfx).Where(Q=>Q.No!=0) ){
                    int no=Abs(P.No);
                    if( (usedB&(1<<no))!=0 ) errB |= (1<<no); //すでに使われている
                    usedB |= (1<<no);
                }

                if( errB==0 ) continue;
                foreach( var P in pBDL.IEGetCellInHouse(tfx).Where(Q=>Q.No!=0) ){
                    int no=Abs(P.No);
                    if( (errB&(1<<no))!=0 ){ P.ErrorState=8; ret=false; }//警告フラッグを設定
                }
            }
            SolCode = ret? 0: 9; //99:ルール違反
            return ret;
        }
        public void ResetAnalysisResult( bool clear0 ){
            if(clear0){   //T:問題の初期状態に戻す
                foreach( var P in pBDL.Where(Q=>Q.No<=0) ){
                    P.Reset_StepInfo(); P.FreeB=0x1FF; P.No=0;
                }
            }
            else{       //F:問題以外を初期化
                foreach( var P in pBDL.Where(Q=>Q.No==0) ){
                    P.Reset_StepInfo(); P.FreeB=0x1FF;
                }
            }
            Set_CellFreeB();
			pGP.extRes="";
        }
    }
}