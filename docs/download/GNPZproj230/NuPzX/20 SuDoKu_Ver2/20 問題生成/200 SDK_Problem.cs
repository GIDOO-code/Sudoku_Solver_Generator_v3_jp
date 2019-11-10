using System;
using System.Collections.Generic;
using static System.Math;

using GIDOO_space;

namespace GNPZ_sdk{
    
    public class UPrbMltMan{
        static public GNPZ_Engin pGNPX_Eng;     
        public List<UProblem> MltUProbLst=null;
        public int        stageNo;
        public UProblem   pGPsel=null;
        public UPrbMltMan GPMpre=null;
        public UPrbMltMan GPMnxt=null;

        public UPrbMltMan( int stageNo ){
            if( SDK_Ctrl.UGPMan!=null )  SDK_Ctrl.UGPMan.GPMnxt=this;
            this.stageNo=stageNo;
            this.GPMpre=SDK_Ctrl.UGPMan;
        }

        public void　Create( ){ //新規作成
            SDK_Ctrl.UGPMan = new UPrbMltMan(stageNo+1);
            if(pGNPX_Eng.pGP==null && pGPsel!=null ){
                pGNPX_Eng.pGP=pGPsel=MltUProbLst[0];
                pGNPX_Eng.pGP=pGNPX_Eng.pGP.Copy(0,0);
            }
        }
    }

    public class UProblem{
        public int     IDm;
        public int     ID;
        public int     stageNo;
        public long    HTicks;
        public string  Name; 
        public string  TimeStamp;
        public string  solMessage;

        public int     DifLevel;    //-1:初期状態　0:手動作成
        public bool    Insoluble;    //解なし

        public List<UCell> BDL;
        public int[]   AnsNum;

        public string  GNPX_Result;           //可能解探索用に追加 20140730
        public string  GNPX_ResultLong;       //可能解探索用に追加 20140730
        public string  GNPX_AnalyzerMessage;  //可能解探索用に追加 20140730
		public string  extRes{ get; set; }
        public int     SolCode;

        public PrbTrans PTrans;
   
        public UProblem( ){
            ID=-1;
            BDL = new List<UCell>();
            for( int rc=0; rc<81; rc++ ) BDL.Add(new UCell(rc));
            this.DifLevel = 0;
            HTicks=DateTime.Now.Ticks;
        }
        public UProblem( string Name ): this(){ this.Name=Name; }

        public UProblem( List<UCell> BDL ){
            this.BDL      = BDL;
            this.DifLevel = 0;
            HTicks=DateTime.Now.Ticks;
        }
        public UProblem( int ID, List<UCell> BDL, string Name="", int DifLvl=0 ){
            this.ID       = ID;
            this.BDL      = BDL;
            this.Name     = Name;
            this.DifLevel = DifLvl;
            HTicks=DateTime.Now.Ticks;
        }
        public void PInitialize( GNPXpzl pGNP ){
            PTrans = new PrbTrans(pGNP);
        }

        public UProblem Copy( int stageNo, int IDm ){
            UProblem P = (UProblem)this.MemberwiseClone();
            P.BDL = new List<UCell>();
            foreach( var q in BDL ) P.BDL.Add(q.Copy());
//          if(AnsNum!=null) AnsNum.CopyTo(P.AnsNum,0);
            P.HTicks=DateTime.Now.Ticks;;
            P.stageNo=this.stageNo+1;
            P.IDm=IDm;
            return P;
        }

        public string ToLineString(){
            string st = BDL.ConvertAll(q=>Max(q.No,0)).Connect("").Replace("0",".");
            st += ", " + (ID+1) + "  ,\"" + Name + "\"";
            st += ", " + DifLevel.ToString();
            st += ", \"" + TimeStamp +  "\"";
            return st;
        }
        public string CopyToBuffer(){
            string st = BDL.ConvertAll(q=>Max(q.No,0)).Connect("").Replace("0",".");
            return st;
        }
        public string ToGridString( bool SolSet ){
            string st="";
            BDL.ForEach( P =>{
                st+=(SolSet? P.No: Max(P.No,0)); //+Shift：全数字(+-)
                if( P.c==8 ) st+="\r";
                else if( P.rc!=80 ) st+=",";
            } );
/*
            st += "\r"+(ID+1).ToString() + ", \"" + Name + "\"";
            st += ", " + DifLevel.ToString();
            st += ","+ solMessage;
*/
            return st;
        }
    }

}