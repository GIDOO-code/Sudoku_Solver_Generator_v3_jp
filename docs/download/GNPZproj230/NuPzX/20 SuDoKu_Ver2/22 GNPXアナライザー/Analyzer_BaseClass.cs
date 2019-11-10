using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using GIDOO_space;

namespace GNPZ_sdk {
    public partial class AnalyzerBaseV2{
        private const int     S=1, W=2;

        public GNPX_AnalyzerMan pAnMan;
        public List<UCell>   pBDL{ get{ return pAnMan.pGP.BDL; } }
        public bool          SolInfoDsp{ get{return pAnMan.SolInfoDsp;} }
        public bool          MltSolOn{   get{return pAnMan.MltSolOn;} }
        public int           SolCode{    set{pAnMan.pGP.SolCode=value;} get{return pAnMan.pGP.SolCode;} }  
        public string        Result{     set{pAnMan.Result=value;} }
        public string        ResultLong{ set{pAnMan.ResultLong=value;} }
		public string		 extRes{     set{pAnMan.pGP.extRes=value; } }

		public SuperLinkMan	 pSprLKsMan{ get { return pAnMan.SprLKsMan; } }
        public CellLinkMan   CeLKMan{ get { return pAnMan.SprLKsMan.CeLKMan; } }
        public ALSLinkMan    ALSMan{  get { return pAnMan.SprLKsMan.ALSMan; } }

        public Bit81[]       Qtrue;
        public Bit81[]       Qfalse;
        public object[,]     chainDesLK;

        static  AnalyzerBaseV2( ){
            SetConnectedCells(); //関連性フィルター作成
        }
        public  AnalyzerBaseV2( ){}
        public  AnalyzerBaseV2( GNPX_AnalyzerMan pAnMan ){ 
            this.pAnMan=pAnMan;
        }

    #region 表示制御
        public string[]    rcbStr=new string[]{ "r", "c", "b" };
        public int[,,]     TTbitCheck=new int[10, 10, 9];
//        public methodCouter[] mthdCCList=new methodCouter[100];

        //===== Result ====================
        // カラーネーム一覧表 http://www.coara.or.jp/~ynakamra/iro/colorname.html //▼削除

        public Color[] _ColorsLst=new Color[]{
            Colors.LightGreen, Colors.Yellow,
            Colors.Aqua, Colors.MediumSpringGreen, Colors.Moccasin, Colors.YellowGreen, 
            Colors.Pink, Colors.ForestGreen, Colors.Aquamarine, Colors.Beige,
            Colors.Lavender, Colors.Magenta, Colors.Olive, Colors.SlateBlue };

        public Color       AttCr    = Colors.Red;
        public Color       AttCr2   = Colors.Blue;
        public Color       AttCr3   = Colors.Green;
        public Color       SolBkCr  = Colors.Yellow;
        public Color       SolBkCr2 = Colors.LightGreen;//Aqua;//SpringGreen//Colors.CornflowerBlue;  //FIn
        public Color       SolBkCr3 = Colors.Aqua;　　//Colors.CornflowerBlue;
        public Color       SolBkCr4 = Colors.CornflowerBlue;
        public int         BDCode;
        public int         SA_sq;
    #endregion 標示制御   

    #region 関連性フィルター
        static public Bit81[] ConnectedCells;    //軸セル(rc)に関連するセル
      //static public Bit81[] ConnectedCellsRev; //軸セル(rc)に関連しないセル
        static public Bit81[] HouseCells;        //行列ブロック(0-26)の関連セル
 
        static private void SetConnectedCells(){
            if( ConnectedCells!=null )  return;
            ConnectedCells    = new Bit81[81];
//          ConnectedCellsRev = new Bit81[81];

            for( int rc=0; rc<81; rc++ ){
                Bit81 BS = new Bit81();
                foreach( var q in __IEGetCellsConnectedRC(rc) ) BS.BPSet(q);
                BS.BPReset(rc);
                ConnectedCells[rc]    = BS;
//              ConnectedCellsRev[rc] = BS ^ 0x7FFFFFF;
            }

            HouseCells = new Bit81[27];
            for( int tfx=0; tfx<27; tfx++ ){
                Bit81 tmp=new Bit81();
                foreach( var q in __IEGetCellInHouse(tfx) ) tmp.BPSet(q);
                HouseCells[tfx] = tmp;
            }
        }
        static private IEnumerable<int> __IEGetCellsConnectedRC( int rc ){ 
            int r=0, c=0;
            for( int kx=0; kx<27; kx++ ){
                switch(kx/9){
                    case 0: r=rc/9; c=kx%9; break; //行   
                    case 1: r=kx%9; c=rc%9; break; //列
                    case 2: int b=rc/27*3+(rc%9)/3; r=(b/3)*3+(kx%9)/3; c=(b%3)*3+kx%3; break;//ブロック
                }
                yield return r*9+c;
            }
        }
        static private IEnumerable<int> __IEGetCellInHouse( int tfx ){ //nx=0...8
            int r=0, c=0, tp=tfx/9, fx=tfx%9;
            for( int nx=0; nx<9; nx++ ){
                switch(tp){
                    case 0: r=fx; c=nx; break;//行
                    case 1: r=nx; c=fx; break;//列
                    case 2: r=(fx/3)*3+nx/3; c=(fx%3)*3+nx%3; break;//ブロック
                }
                yield return (r*9+c);
            }
        }
    #endregion 関連性フィルター

#if false
//    #region superlinkChain
        private const bool DevelopB=false;  //true;
        public USuperLink GetSuperLinks( int rc0, int no0 ){          

            USuperLink USLK=new USuperLink(rc0,no0);
            var Qtrue =USLK.Qtrue;
            var Qfalse=USLK.Qfalse;
            var chainDesLK=USLK.chainDesLK;

            //======== 開始セル ========
            UCell P0=pBDL[rc0];
            int no0B=1<<no0;
            Qtrue[no0].BPSet(rc0);  //開始セルの着目数字
            foreach( var nox in P0.FreeB.IEGet_BtoNo().Where(p=>p!=no0) ) Qfalse[nox].BPSet(rc0);   //その他の数字

            //======== 最初のリンク ========
            var rcQue=new Queue<GroupedLink>();
            foreach( var GLKH in NxgSprLKsMan.IEGet_SuperLinkFirst(P0,no0) ){
                    if(DevelopB) WriteLine("*1st:"+GLKH.GrLKToString());
                GLKH.UsedCs |= GLKH.UGCellsB.B81;
                GLKH.preGrpedLink=P0; rcQue.Enqueue(GLKH);  //最初のリンクの埋め込み
                int no1=GLKH.no, no2=GLKH.no2;

                //着目数字に接続するリンク
                if(GLKH.type==W){
                    foreach( var P in GLKH.UGCellsB ){
                        if( (P.FreeB&no0B)>0 ){
                            Qfalse[no2].BPSet(P.rc);
                            chainDesLK[P.rc,no2]=GLKH;
                            if(P.FreeBC==2){
                                int nox=P.FreeB.DifSet(1<<no2).BitToNum();
                                Qtrue[nox].BPSet(P.rc);
                            }
                        }
                    }

                }

                //その他の数字に接続するリンク
                if(GLKH.type==S && GLKH.UGCellsB.Count==1 ){ 
                    var P=GLKH.UGCellsB[0]; 
                    Qtrue[no2].BPSet(P.rc);
                    foreach( var nox in P.FreeB.IEGet_BtoNo().Where(q=>(q!=no2)) ){
                        Qfalse[nox].BPSet(P.rc);
                        chainDesLK[P.rc,nox]=GLKH;
                    }
                }
            }

            int kkx=0;
            while(rcQue.Count>0){
                GroupedLink R = rcQue.Dequeue();
                    if(DevelopB) WriteLine("---Queue:"+R.GrLKToString());
                foreach( var GLKH in NxgSprLKsMan.IEGet_SuperLink(R) ){
                        if(DevelopB) WriteLine("   --GLKH:"+GLKH.GrLKToString());
                    if(R.type!=GLKH.type && R.UGCellsA.Equals(GLKH.UGCellsB) )  continue;
                        
                    GLKH.preGrpedLink=R;;
                    int no2=GLKH.no2;
                    if(GLKH.type==S){
                        if(!(GLKH.UGCellsB.B81&Qtrue[GLKH.no2]).IsZero() )  continue;
                            if(DevelopB) WriteLine("       *2nd S:"+GLKH.GrLKToString());
                        if(GLKH.UGCellsB.Count==1){
                            var P=GLKH.UGCellsB[0];
                            Qtrue[no2].BPSet(P.rc);
                            foreach( var nox in P.FreeB.IEGet_BtoNo().Where(q=>(q!=no2)) ){
                                Qfalse[nox].BPSet(P.rc);
                                chainDesLK[P.rc,nox]=GLKH;
                            }
                        }
                    }
                    if(GLKH.type==W){
                        if(!(GLKH.UGCellsB.B81&Qfalse[GLKH.no2]).IsZero() )  continue;
                            if(DevelopB) WriteLine("       *2nd W:"+GLKH.GrLKToString());
                            
                        foreach( var P in GLKH.UGCellsB ){
                            Qfalse[no2].BPSet(P.rc);
                            chainDesLK[P.rc,no2]=GLKH;
                        }

                        if(R.type==W && GLKH.UGCellsA.Count==1 ){
                            var P=GLKH.UGCellsA[0];
                            int nox=GLKH.no;
                            Qtrue[nox].BPSet(P.rc);
                        }
                    }
                    rcQue.Enqueue(GLKH);
                }
            }
             
            //     if(DevelopB) 
                 developDisp( rc0, no0, Qtrue, Qfalse );
        

            return USLK;
        }

        public  void developDisp( int rc0, int no0, Bit81[] pQtrue, Bit81[] pQfalse ){
            List<UCell> qBDL=new List<UCell>();
            pBDL.ForEach(p=>qBDL.Add(p.Copy()));

            foreach( var P in qBDL ){
                if(P.No!=0)  continue;
                int noT=0, noF=0;
                for( int k=0; k<9; k++ ){
                    if( pQtrue[k].IsHit(P.rc) )  noT|=(1<<k);
                    if( pQfalse[k].IsHit(P.rc) ) noF|=(1<<k);
                }
                if(noT>0) P.SetNoBBgColor(noT, Colors.Red, Colors.LemonChiffon  );
                if(noF>0) P.SetNoBBgColorRev(noF, Colors.Red, Colors.LemonChiffon  );
                if((noT&noF)>0){
                    P.SetNoBBgColor(noT&noF, Colors.White , Colors.PowderBlue );
                    P.SetNoBColorRev(noT&noF,Colors.Blue );
                }
            }
            qBDL[rc0].SetNoBBgColor(1<<no0, Colors.Red, Colors.Yellow);

            devWin.Set_dev_GBoard( qBDL, dispOn:false );

            if(DevelopB){
                foreach( var P in pBDL ){
                    if(P.No!=0) continue;
                    foreach( var no in P.FreeB.IEGet_BtoNo() ){
                        if( Qfalse[no].IsHit(P.rc) ){
                            GroupedLink Pdes=(GroupedLink)chainDesLK[P.rc,no];
                            string st= chainToString(P,Pdes,no);
                            if(st.Length>4) WriteLine(st);
                        }
                    }
                }
            }
        }

        public string chainToString( UCell U, GroupedLink Gdes, int noRem ){
            string st="";
            if(Gdes==null)  return st;
            var Qlst=new List<GroupedLink>();

            var X=Gdes;
            while(X!=null){
                Qlst.Add(X);
                X=X.preGrpedLink as GroupedLink;
                if(Qlst.Count>10) break; //error
            }
            Qlst.Reverse();

            st = "r"+(U.r+1)+"c"+(U.c+1)+"("+U.rc+")/"+(noRem+1)+" ";
            foreach( var R in Qlst ){ st += R.GrLKToString()+ " => "; };
            st = st.Substring(0,st.Length-4);

            if(Qlst.Count>10) st=" ▼error▼"+st;
            return st;
        }
//    #endregion superlinkChain
#endif

    }
}