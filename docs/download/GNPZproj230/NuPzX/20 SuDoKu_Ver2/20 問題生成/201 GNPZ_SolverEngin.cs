using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Threading;
using System.Diagnostics;
using static System.Console;

using static System.Math;

namespace GNPZ_sdk{
    //与えられた数独問題を解く
    public class GNPZ_Engin{
        //===== 解法の条件・パラメータ(共通) =====GNPXpzl.MltSolOn
        static public int       cCode;
        static public int       retCode;
        static public bool      SolInfoDsp;

        static public string    GNPX_AnalyzerMessage="";
        static public List<string> SDK_AnlzST = new List<string>();
        static public TimeSpan  SdkExecTime;

        private GNPXpzl         pGNP00;
        public GNPX_AnalyzerMan AnMan;
        private List<UItemCheck> pGMthdLst{ get{ return pGNP00.GMthdLst; } }
        public List<TagAn>      MethodLst_Run=new List<TagAn>();

        //===================================
        public UProblem         pGP=null;    //解読データ
            
        public int              cCodePre = 0;
        public int              SolvingLoopCC;     

        static public bool      ChkPrint=false;

        public GNPZ_Engin( GNPXpzl pGNP00, UProblem pGP ){
            this.pGNP00=pGNP00;
            this.pGP  = pGP;
            AnMan=new GNPX_AnalyzerMan(this,pGP);
            SolvingLoopCC = 0;
        }

        public void SetGP( UProblem pGP ){
            this.pGP=pGP;
            AnMan.Set_CellFreeB();    //問題の初期化
            GNPX_AnalyzerMessage="";
        }

        public int Set_MethodLst_Run( bool AllMthd=false ){
            MethodLst_Run.Clear(); 
            foreach( var S in pGMthdLst ){ //AllMthd=trueの場合は、指定順序のみ用いる
                if( !AllMthd && !S.IsChecked )  continue;
                var Sobj = AnMan.SolverLst.Find(P=>P.MethodName==S.Name );
                MethodLst_Run.Add(Sobj);
            }
            return MethodLst_Run.Count;
        }
        public void AnalyzerCounterReset(){ MethodLst_Run.ForEach(P=>P.UsedCC=0); }
        private Random rnd = new Random();
        private void DspNumRandmize( UProblem P ){
            List<int> ranNum = new List<int>();
            for( int r=0; r<9; r++ )  ranNum.Add( rnd.Next(0,9)*10+r );
            ranNum.Sort( (x,y) => (x-y) );
            for( int r=0; r<9; r++) ranNum[r] %= 10;
            int n;
            P.BDL.ForEach(q =>{
                if( (n=q.No)>0 ) q.No = ranNum[n-1]+1;
            } );
        }
        private int[] eNChk;
        public void sudokAnalyzerAuto( CancellationToken ct ){
            retCode=0;
            AnMan.Set_CellFreeB();
            Stopwatch AnalyzerLap = new Stopwatch();
            AnalyzerLap.Start();
            while(true){
                if( ct.IsCancellationRequested ){ return; }
                int ret2=0;     
                bool ret = AnalyzerControl(ct,ref ret2,false );
                if( pGP.Insoluble==true ) retCode=-999;   //候補数字なしのセルを発見
                if( ret2==-999888777 )   retCode=ret2;
                if( ret==false )  break;
                if( !AnMan.FixOrEliminate_SuDoKu( ref eNChk) )  retCode=-998;
                SdkExecTime = AnalyzerLap.Elapsed;
                if( retCode<0 )  return;
            }
            AnalyzerLap.Stop();        
            int  nP=0, nZ=0, nM=0;
            AnMan.AggregateCellsPZM(ref nP,ref nZ,ref nM);
            retCode=nZ;
        }
        public bool AnalyzerControl( CancellationToken ct, ref int ret2, bool SolInfoDsp ){
            List<UCell> XYchainList = new List<UCell>();
            Stopwatch AnalyzerLap = new Stopwatch();
			AnMan.GStage++;

            bool ret=false;
            try{
                pGP.GNPX_ResultLong = "";
                int   lvlLow = SDK_Ctrl.lvlLow;
                int   lvlHgh = SDK_Ctrl.lvlHgh;
                AnMan.SolversInitialize(); //■Solver初期化

   #region 解法適用
                int  mCC=0;            
                do{    //ダミーのブロック
                    ret = AnMan.VerifyRoule_SuDoKu();
                    //if( pGP.SolCode==99999 ) break; //
                    if( ret==false ){
                        if( SolInfoDsp ) pGP.GNPX_ResultLong = "00 解なし";
                        ret2 = -999888777;
                        return false;
                    }
                    ret=false;
                    //-------------------------------------------
                LblRestart:    
                AnalyzerLap.Start();

                    DateTime MltAnsTimer=DateTime.Now;
                    UProblem GPpre=null;
                    if( SDK_Ctrl.MltAnsSearch ) GPpre=pGP.Copy(0,0);
                    try{
						if( AnMan.pBDL.All(p=>(p.FreeB==0)) ) break;

                        //SDA.AnalyzerLog = "";
                        pGP.SolCode=-1;
                        //MethodLst_Run.ForEach( P=>WriteLine(P) );

                        bool L1SolFond=false;

                        foreach( var P in MethodLst_Run ){
                            //if( !P.Enabled )  continue;
                            int lvl=P.DifLevel; 
                            int lvlAbs = Abs(lvl);

                            if( SDK_Ctrl.MltAnsSearch ){
                                if( L1SolFond && lvlAbs>=2 )  break;
                                if( lvlAbs>(int)SDK_Ctrl.MltAnsOption["レベル"] ) continue;
                                if( (string)SDK_Ctrl.MltAnsOption["打切り結果"]!="" ){
                                    GNPX_AnalyzerMessage = (string)SDK_Ctrl.MltAnsOption["打切り結果"];
                                    break;
                                }
                            }
                            else{
                                if( lvl<0 ) continue; //負難易度手法は複数解解析のみ
                            }
                            
                            if( !SDK_Ctrl.MltAnsSearch && lvl<0 ) continue; //負難易度手法は複数解解析のみ
                            if( SDK_Ctrl.MltAnsSearch && L1SolFond && Abs(lvl)>=2 )  continue;
                            if( lvl>lvlHgh )  continue;
                            if( ChkPrint ) WriteLine( $"---> method{(mCC++)} :{P.MethodName}");
                            GNPX_AnalyzerMessage = P.MethodName;
                            if( ct!=null && ct.IsCancellationRequested ){ /*ct.ThrowIfCancellationRequested();*/ return false; }
                            
							//if(pGP.DifLevel<P.DifLevel)  pGP.DifLevel=P.DifLevel;
							pGP.DifLevel=P.DifLevel;

                            if( (ret=P.Method()) ){
                                if( SDK_Ctrl.UGPMan!=null &&  SDK_Ctrl.UGPMan.MltUProbLst!=null &&
                                    SDK_Ctrl.UGPMan.MltUProbLst.Any(q=>q.SolCode==1) ) L1SolFond=true;
                                P.UsedCC++;
                                if( ChkPrint ) WriteLine( $"========================> solved {P.MethodName}" );
                                if( !SDK_Ctrl.MltAnsSearch )  goto succeedBreak;
                            }
                        }

                        if( SDK_Ctrl.MltAnsSearch && SDK_Ctrl.UGPMan.MltUProbLst!=null ){
                            pGP=SDK_Ctrl.UGPMan.MltUProbLst.First();
                            ret=true;
                            goto succeedBreak;
                        }

                        if( ChkPrint ) WriteLine( "========================> 解けない");
                        if( SolInfoDsp ) pGP.GNPX_ResultLong = "解けない";
                        ret2 = -999888777;
                        return false;
                    }
                catch( Exception e ){
                        WriteLine( e.Message );
                        WriteLine( e.StackTrace );
                        goto LblRestart;
                    }
                    finally{ AnalyzerLap.Stop(); }
                }while(false);
            }
            catch( ThreadAbortException ex ){
                WriteLine( ex.Message );
                WriteLine( ex.StackTrace );
            }
          succeedBreak:  //Fond
            SdkExecTime = AnalyzerLap.Elapsed;
            
#endregion 解法適用
            return ret;  
        }

        public int solutionConditionCode( ){
            int rc=0, n, cCode=0;
            pGP.BDL.ForEach( q =>{
                if( (n=q.No)!=0 )  cCode ^= ((rc++)*7 + n*97);
                else               cCode ^= q.FreeB*317 + (rc*13);
            } );
            return cCode;
        }
        public int  GetDifficultyLevel( out string prbMessage ){
            int DifL=0;
            prbMessage="";
            if(MethodLst_Run.Any(P=>(P.UsedCC>0))){
                DifL =MethodLst_Run.Where(P=>P.UsedCC>0).Max(P=>P.DifLevel);
                var R =MethodLst_Run.FindLast(Q=>(Q.UsedCC>0)&&(Q.DifLevel==DifL));
                prbMessage =(R!=null)? R.MethodName: "";
            }
            return DifL;
        }

        public string DGViewMethodCounterToString(){ //手法の集計 ▼【TBD】
            string solMessage="";
            foreach( var q in MethodLst_Run.Where(p=>p.UsedCC>0) ){
                solMessage += " "+q.MethodName+"["+q.UsedCC+"]";
            }
            return solMessage;
        }
    }
}