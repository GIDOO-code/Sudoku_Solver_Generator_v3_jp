using System;
using System.Collections.Generic;
using static System.Console;
using static System.Math;
using System.Threading;

namespace GNPZ_sdk{
    public partial class SDK_Ctrl{    
        static public event   SDKEventHandler Send_Progress; 
        static public Random  GRandom= new Random();
        static public int     TLoopCC = 0;
        static public int     lvlLow;
        static public int     lvlHgh;
        static public bool    FilePut_GenPrb;
        static public GNPZ_Engin   pGNPX_Eng;

      //===== 複数解解析拡張 20140730- ====================
        static public bool   MltAnsSearch;
        static public int    MltProblem;
        static public Dictionary<string,object> MltAnsOption;
        static public UPrbMltMan UGPMan=null;
      //---------------------------------------------------

        public NuPz_Win     pGNP00win;
        private GNPXpzl     pGNP00;

        public int          retNZ; 
        
        public int          CellNumMax;
        public int          LoopCC=0;
        public int          PatternCC=0;

        public int          ProgressPer;
        public bool         CanceledFlag;
        public bool         CbxDspNumRandmize;  //数字の乱数化 
        public int          GenLStyp;
        public bool         CbxNextLSpattern;   //問題成功時にLSパターンを変更
    
        public patternGenerator PatGen; //露出パターン

        public int          randumSeedVal=0;
        public bool         threadRet;

        private bool        _DEBUGmode_= false; //false;

        static SDK_Ctrl(){
            MltAnsOption=new Dictionary<string,object>();
            MltAnsOption["レベル"]       = 10; //レベル
            MltAnsOption["着目同一上限"] = 5;  //着目セル同一上限
            MltAnsOption["複数解上限"]   = 50; //複数解上限
            MltAnsOption["探索時間上限"] = 15; //探索時間上限
            MltAnsOption["打切り時間"]   = DateTime.Now;
            MltAnsOption["打切り結果"]   = "";
        }

        public SDK_Ctrl( GNPXpzl pGNP00, int FirstCellNum ){
            this.pGNP00 = pGNP00;
            this.pGNP00win = pGNP00.pGNP00win;
            Send_Progress += new SDKEventHandler(pGNP00win.BWGenPrb_ProgressChanged);     
            
            CellNumMax = FirstCellNum; 

            PatGen = new patternGenerator( this );
            LSP    = new LatinSqureGen( ); 　//▼仮の設定
        }

        static public void MovePre(){
            if(UGPMan==null)  return;
            UGPMan = UGPMan.GPMpre;
            if(UGPMan==null)  return;
            pGNPX_Eng.pGP = UGPMan.pGPsel;
        }
         
        static  public void MoveNxt(){
            if(UGPMan==null)  return;
            UGPMan = UGPMan.GPMnxt;
            if(UGPMan==null)  return;
            pGNPX_Eng.pGP = UGPMan.pGPsel;
        }

        private void _ApplyPattern( int[] X ){
            for( int rc=0; rc<81; rc++ ){
                if(PatGen.GPat[rc/9,rc%9]==0) X[rc]=0;
            }
            if( _DEBUGmode_ )  __DBUGprint2(X, "    ");
       }
        
        public void SetRandumSeed( int rs ){
#if DEBUG
            randumSeedVal = rs;
#else
            if(rs==0){
                int nn=Environment.TickCount&Int32.MaxValue;
                randumSeedVal=nn;
            }
#endif
            GRandom=null; 
            GRandom=new Random(randumSeedVal);
        }

    #region 問題候補生成
        //パターン生成コントロール　
        internal int[,] ASDKsol = new int[9,9];
        private int[] prKeeper = new int[9];
        private Random rnd = new Random();

        public List<UCell> GenerateSolCandidate( ){ //問題候補生成
            int[] P=GenSolPatternsList(CbxDspNumRandmize,GenLStyp);
            if(P==null)  return null;

            List<UCell> BDLa = new List<UCell>();
            for( int rc=0; rc<81; rc++ )  BDLa.Add(new UCell(rc,P[rc]));
            if( _DEBUGmode_ ) __DBUGprint(BDLa);
            return BDLa;
        }    
        private void __DBUGprint( List<UCell> BDL ){
            string po;
            WriteLine();
            for( int r=0; r<9; r++ ){
                po = r.ToString("            ##0:");
                for( int c=0; c<9; c++ ){
                    int No = BDL[r*9+c].No;
                    if( No==0 ) po += " .";
                    else po += No.ToString(" #");
                }
                WriteLine(po);
            }
        }
    #endregion 問題候補生成

    #region 問題作成
        //====================================================================================
        public void SDK_ProblemMakerReal( CancellationToken ct ){ //問題作成【自動】L1067
            try{
                int mlt = MltProblem;
                pGNPX_Eng.Set_MethodLst_Run();

                do{
                    if( ct.IsCancellationRequested ){ ct.ThrowIfCancellationRequested(); return; }

                    LoopCC++; TLoopCC++;
                    List<UCell>   BDL = GenerateSolCandidate( );  //候補問題生成
                    UProblem P = new UProblem(BDL);
                    //if(BDL==null) P.GNPX_ResultLong="全数探索：この問題の階はない";
                    pGNPX_Eng.SetGP(P);

                    pGNPX_Eng.AnalyzerCounterReset();
                    pGNPX_Eng.sudokAnalyzerAuto(ct);
            
                    if( GNPZ_Engin.retCode==0 ){
                        string prbMessage;
                        int DifLevel = pGNPX_Eng.GetDifficultyLevel(out prbMessage);
                        if( DifLevel<lvlLow || lvlHgh<DifLevel ) continue; //難易度チェック
           
                        P.DifLevel = DifLevel;
                        P.Name = prbMessage;
                        P.TimeStamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                        P.solMessage = pGNPX_Eng.DGViewMethodCounterToString();
                        pGNP00.SDK_ProblemListSet(P);
                    
                        SDKEventArgs se = new SDKEventArgs(ProgressPer:(--mlt));
                        Send_Progress(this,se);  //LoopCCと同じ方法でも情報を送れる。
                        if( CbxNextLSpattern ) rxCTRL=0; //次の問題生成時にLSパターンを変更
                    }
                }while(mlt>0);
            }
            catch( Exception ex ){
                WriteLine(ex.Message);
                WriteLine(ex.StackTrace);
            }
        }
    #endregion 問題作成

    #region 解析
        public void AnalyzerReal( CancellationToken ct ){      //解析【ステップ】
            int ret2=0;
            retNZ=-1; LoopCC++; TLoopCC++;
            pGNPX_Eng.Set_MethodLst_Run(false); 
            pGNPX_Eng.AnalyzerControl( ct, ref ret2, true );
            SDKEventArgs se = new SDKEventArgs(ProgressPer:(retNZ));
            Send_Progress(this,se);  //LoopCCと同じ方法でも情報を送れる。
        }
        public void AnalyzerRealAuto( CancellationToken ct ){   //解析【全て】
            LoopCC++; TLoopCC++;
            bool MltSolOn = GNPXpzl.MltSolOn;
            pGNPX_Eng.Set_MethodLst_Run(false);
            pGNPX_Eng.sudokAnalyzerAuto(ct);
            SDKEventArgs se = new SDKEventArgs(ProgressPer:(GNPZ_Engin.retCode));
            Send_Progress(this,se);  //LoopCCと同じ方法でも情報を送れる。
        }
    #endregion 解析
 
       private void __DBUGprint2( int[,] pSol99, string st="" ){
            string po;
            WriteLine();
            for( int r=0; r<9; r++ ){
                po = st+r.ToString("##0:");
                for( int c=0; c<9; c++ ){
                    int wk=pSol99[r,c];
                    if(wk==0) po += " .";
                    else po += wk.ToString(" #");
                }
                WriteLine(po);
            }
       }
       private void __DBUGprint2( int[] X, string st="" ){
            string po;
            WriteLine();
            for( int r=0; r<9; r++ ){
                po = st+r.ToString("##0:");
                for( int c=0; c<9; c++ ){
                    int wk=Abs(X[r*9+c]);
                    if(wk==0) po += " .";
                    else po += wk.ToString(" #");
                }
                WriteLine(po);
            }
        }
    }
}