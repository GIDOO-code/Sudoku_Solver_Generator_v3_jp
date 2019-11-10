using System;
using System.Linq;
using System.Collections.Generic;
using static System.Console;

using GIDOO_space;

namespace GNPZ_sdk{
    public partial class GeneralLogicGen: AnalyzerBaseV2{
        static public int ChkBas1=0, ChkBas2=0;
        static public int ChkCov1=0, ChkCov2=0;
        private int GStageMemo;
        private UGLinkMan UGLMan;
        private int GLMaxSize;
        private int GLMaxRank;

        public GeneralLogicGen( GNPX_AnalyzerMan pAnMan ): base(pAnMan){ }
        //1..4....8.9.1...5.....63.....13.5.79..3...8..76.2.94.....75.....1...6.4.8....4..2

        public bool GeneralLogicExnm( ){
            if(pAnMan.GStage!=GStageMemo){
				GStageMemo=pAnMan.GStage;
                GLMaxSize = GNPXApp000.GMthdOption["GenLogMaxSize"].ToInt();
                GLMaxRank = GNPXApp000.GMthdOption["GenLogMaxRank"].ToInt();
                UGLMan=new UGLinkMan(this);
                UGLMan.PrepareUGLinkMan();
			}
                     
            for( int sz=1; sz<=GLMaxSize; sz++ ){
                for( int rnk=0; rnk<=GLMaxRank; rnk++ ){ 
                    if(rnk>=sz) continue;
                    ChkBas1=0; ChkBas2=0; ChkCov1=0; ChkCov2=0;
                    if(GeneralLogicExB(sz,rnk))  return true;
                }
            }
            return false;
        }

        private bool GeneralLogicExB( int sz, int rnk ){
            if(sz>GLMaxSize || rnk>GLMaxRank)  return false;

            foreach( var UBC in UGLMan.IEGet_BaseSet(sz) ){            //BaseSet generation
                if( pAnMan.CheckTimeOut() ) return false;

                var bas=UBC.HB819;
                foreach( var UBCc in UGLMan.IEGet_CoverSet(UBC,rnk) ){ //CoverSet generation

                    for( int no=0; no<9; no++ ){
                        if( !(UBCc.HC819[no]-UBCc.HB819[no]).IsZero() )  goto SolFond;
                    }
                    continue;

                  SolFond:
                    if(rnk==0){
                        for( int no=0; no<9; no++ ){
                            int noB=(1<<no);
                            Bit81 PF=UBCc.HC819[no]-UBCc.HB819[no];       
                            foreach( var P in PF.IEGet_rc().Select(p=>pBDL[p]) ) P.CancelB |= noB;
                        }
                    }
                    else{
                        int rc=UBCc.rcCan, no=UBCc.noCan;
                        pBDL[rc].CancelB=(1<<no);
                    }

                    if(SolInfoB)  _generalLogicResult(UBCc);
                    if( !pAnMan.SnapSaveGP(false) ) return true;                 
                }
            }
            return false;
        }
        private void _generalLogicResult( UBasCov UBCc ){
            try{
                for( int no=0; no<9; no++ ){
                    int noB=(1<<no);
                    Bit81 PB=UBCc.HB819[no];             
                    foreach( var P in PB.IEGet_rc().Select(p=>pBDL[p]) ) P.SetNoBBgColor(noB,AttCr,SolBkCr);
                }

                if(UBCc.rnk==0){
                    for( int no=0; no<9; no++ ){
                        int noB=(1<<no);
                        Bit81 Pcan=UBCc.HC819[no]-UBCc.HB819[no];
                        foreach( var P in Pcan.IEGet_rc().Select(p=>pBDL[p]) ) P.SetNoBBgColor(noB,AttCr,SolBkCr2);
                    }
                }
                else{
                    int rc=UBCc.rcCan, no=UBCc.noCan;
                    UCell P=pBDL[rc];
                    P.SetNoBBgColor(1<<no,AttCr,SolBkCr2);
                }

                string msg = "\r     BaseSet: ";
                string msgB="";
                foreach( var P in UBCc.basUGLs ){
                    if(P.rcB is Bit81) msgB += P.tfx.tfxToString()+("#"+(P.no+1))+" ";
                    else msgB += P.uc.rc.ToRCString()+" ";
                }
                msg += ToString_SameHouseComp1(msgB);
               
                msg += "\r    CoverSet: ";
                string msgC="";
                foreach( var P in UBCc.covUGLs ){
                    if(P.rcB is Bit81) msgC += P.tfx.tfxToString()+("#"+(P.no+1))+" ";
                    else msgC +=P.uc.rc.ToRCString()+" ";
                }
                msg += ToString_SameHouseComp1(msgC);

                string st="GeneralLogic N:"+UBCc.sz +" rank:"+UBCc.rnk;
                Result = st;
                msg += "\r\r ChkBas:"+ChkBas2+"/"+ChkBas1 +" ChkCov:"+ChkCov2+"/"+ChkCov1;
                ResultLong = st+msg;  
            }
            catch( Exception ex ){
                WriteLine(ex.Message);
                WriteLine(ex.StackTrace);
            }
        }

        private string ToString_SameHouseComp1( string st ){
            char[] sep=new Char[]{ ' ', ',', '\t' };
            List<string> T=st.Trim().Split(sep).ToList();
            if(T.Count<=1) return st;
            List<_ClassNSS> URCBCell=new List<_ClassNSS>();
            T.ForEach(P=> URCBCell.Add(new _ClassNSS(P)));
            return ToString_SameHouseComp2(URCBCell);
        }
        private string ToString_SameHouseComp1( UBasCov UBC ){
            List<_ClassNSS> URCBCell=new List<_ClassNSS>();
            List<string> UQ=new List<string>();
            foreach( var P in UBC.basUGLs.Where(p=>(p.rcB is Bit81))){
                _ClassNSS Q=null;
                string stNo="#"+(P.no+1);
                if( P.tfx<9 )  Q=new _ClassNSS (1,"r"+(P.tfx+1),stNo);
                if( P.tfx>=9 && P.tfx<18 ) Q=new _ClassNSS (1,"c"+(P.tfx-9+1),stNo);
                if( P.tfx>=18 )   Q=new _ClassNSS (1,"b"+(P.tfx-18+1),stNo);
                URCBCell.Add(Q);
            }
            return ToString_SameHouseComp2(URCBCell);
        }
        private string ToString_SameHouseComp2( List<_ClassNSS> URCBCell){
            string st;
            bool cmpF=true;
            do{
                cmpF=false;
                foreach( var P in URCBCell ){
                    List<_ClassNSS> Qlst=URCBCell.FindAll(Q=>(Q.stRCB==P.stRCB && Q.stNum[0]==P.stNum[0]));
                    if(Qlst.Count>=2){
                        st=Qlst[0].stNum[0].ToString();
                        Qlst.ForEach(Q=>st+=Q.stNum.Substring(1,Q.stNum.Length-1));
                        _ClassNSS R=URCBCell.Find(Q=>(Q.stRCB==P.stRCB));
                        R.stNum = st;
                        foreach(var T in Qlst.Skip(1) ) URCBCell.Remove(T);
                        cmpF=true;
                        break;
                    }

                    Qlst=URCBCell.FindAll(Q=>(Q.stNum==P.stNum && Q.stRCB[0]==P.stRCB[0]));
                    if(Qlst.Count>=2){
                        st=Qlst[0].stRCB[0].ToString();
                        Qlst.ForEach(Q=>st+=Q.stRCB.Substring(1,Q.stRCB.Length-1));
                        _ClassNSS R=URCBCell.Find(Q=>(Q.stNum==P.stNum));
                        R.stRCB = st;
                        foreach(var T in Qlst.Skip(1) ) URCBCell.Remove(T);
                        cmpF=true;
                        break;
                    }
                }

            }while(cmpF);
            st="";
            URCBCell.ForEach(P=> st+=(P.stRCB+P.stNum+" ") );
            return st;
        }
        private class _ClassNSS{
            public int sz;
            public string stRCB;
            public string stNum;
            public _ClassNSS( int sz, string stRCB, string stNum ){
                this.sz=sz; this.stRCB=stRCB; this.stNum=stNum;
            }
            public _ClassNSS( string st ){
                sz=1; stRCB=st.Substring(0,2); stNum=st.Substring(2,2);
            }
        }
    }  
}