using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using static System.Console;

using GIDOO_space;

namespace GNPZ_sdk{
    public partial class GroupedLinkGen: AnalyzerBaseV2{
        private const int     S=1, W=2;
		private int GStageMemo;
        public  int           NiceLoopMax{ get{ return GNPXpzl.GMthdOption["NiceLoopMax"].ToInt(); } }
        private int           SolLimBrk=0;

        public GroupedLinkGen( GNPX_AnalyzerMan pAnMan ): base(pAnMan){ }

		private void Prepare(){
			if(pAnMan.GStage!=GStageMemo) {
				GStageMemo=pAnMan.GStage;
				pSprLKsMan.Initialize();
				pSprLKsMan.PrepareSuperLinkMan( AllF:true );
			}      
		}

        public bool GroupedNiceLoop( ){
            try{
                Prepare();

                for( int szCtrl=4; szCtrl<NiceLoopMax; szCtrl++ ){  //サイズの小さいNiceLoopから探す

                    foreach( var P0 in pBDL.Where(p=>(p.No==0)) ){      //起点セル
                        SolLimBrk=0;
                        foreach( var no in P0.FreeB.IEGet_BtoNo() ){    //始めの数字   

                            foreach( var GLKH in pSprLKsMan.IEGet_SuperLinkFirst(P0,no) ){   //最初のリンク
                               if( pAnMan.CheckTimeOut() ) return false;

                                var SolStack=new Stack<GroupedLink>();
                                SolStack.Push(GLKH);//▼Push
                                    // ___Debug_Print_GNLChain(SolStack);

                                Bit81 UsedCs = new Bit81();//使用済みセルのビット表現
                                SetUsed( ref UsedCs, GLKH);
                                    // ___Debug_Print_Bit819s(UsedCs);
                                GLKH.UsedCs=UsedCs;
                                _GroupedNL_Search(GLKH,GLKH,SolStack,szCtrl-1);
                                if( SolLimBrk==int.MaxValue ) goto LblSolLimBrk;
                                if( SolCode>0 ){ return true; }
                            }
                        }
                LblSolLimBrk:
                    continue;
                    }
                }
            }
            catch( Exception ex ){
                WriteLine(ex.Message);
                WriteLine(ex.StackTrace);
            }
            return false;
        }

        private bool _GroupedNL_Search( GroupedLink GLK0, GroupedLink GLKpre, Stack<GroupedLink> SolStack, int szCtrl ){
            if( szCtrl<0 ) return false;

            //連結条件を満たすリンクコレクションの反復処理 
            Bit81 UsedCs=GLKpre.UsedCs;
            foreach( var GLKnxt in pSprLKsMan.IEGet_SuperLink(GLKpre) ){     //218           
                UGrCells GCsNxt = GLKnxt.UGCellsB;
                int no=GLKnxt.no;

                { //===== Chain Search =====
                    SolStack.Push(GLKnxt);//▼Push(試行:リンク延伸）
                    ___Debug_Print_GNLChain(SolStack);
                
                    //ループが形成された(次セルは起点セルと一致）
                    if( _GroupedNL_LoopCheck(GLK0,GLKnxt) ){
                        if( szCtrl==0 && SolStack.Count>3 ){
                            int SolType=_GroupedNL_CheckSolution(GLK0,GLKnxt,SolStack,UsedCs);//解チェック
                            if( SolType>0 ){          
                                if( SolInfoDsp ) _GroupedNL_SolResult(GLK0,GLKnxt,SolStack,SolType); 
                                ___Debug_Print_GNLChain(SolStack,"★★");
                                if( !pAnMan.SnapSaveGP(false) )  return true;////return true;
                                if( (++SolLimBrk)>(int)SDK_Ctrl.MltAnsOption["着目同一上限"] ){
                                    SolLimBrk=int.MaxValue;
                                    return false;
                                }

                            }
                        }
                    }
                    else if( !CheckUsed( (UsedCs|GLK0.UsedCs), GLKnxt) ){
                        Bit81 UsedCsNxt = new Bit81(UsedCs);   //新しい"使用済みセルのビット表現"を作成
                        SetUsed( ref UsedCsNxt, GLKnxt);

                        GLKnxt.UsedCs=UsedCsNxt;
                        _GroupedNL_Search(GLK0,GLKnxt,SolStack,szCtrl-1); //次段階の探索(recursive call)
                        if( SolLimBrk==int.MaxValue ) return false;
                        if( SolCode>0 ) return true;
                    }
                    SolStack.Pop();     //▲Pop(不成立:リンク延伸取消し）
                } //-----------------------------
            }  
            return false;
        }
        private void SetUsed( ref Bit81 UsedCs, GroupedLink GLKnxt){ 
            UsedCs |= GLKnxt.UGCellsB.B81;   //▼手法完成時には展開組み込み
        }
        private bool CheckUsed( Bit81 UsedPre, GroupedLink GLKnxt){
            Bit81 BP=GLKnxt.UGCellsB.B81;
            if( GLKnxt is ALSLink ) BP -= (GLKnxt.UGCellsA.B81);
            return UsedPre.IsHit(BP); //重なりチェック
        }

        private bool _GroupedNL_LoopCheck( GroupedLink GLK0, GroupedLink GLKnxt ){
            UGrCells Qorg=GLK0.UGCellsA;
            UGrCells Qdst=GLKnxt.UGCellsB;
            if( Qdst.Count!=1 ) return false;
            return  (Qdst[0].rc==Qorg[0].rc);
        }

        private int _GroupedNL_CheckSolution( GroupedLink GLK0, GroupedLink GLKnxt, 
                                              Stack<GroupedLink> SolStack, Bit81 UsedCs ){ 
            bool SolFond=false;
            int SolType = pSprLKsMan.Check_SuperLinkSequence(GLKnxt,GLK0)? 1: 2; //1:Continuous 2:DisContinuous

            if( SolType==1 ){ //◆continuous
                List<GroupedLink> SolLst=SolStack.ToList();
//             ___Debug_Print_GNLChain(SolStack);

                SolLst.Reverse();
                SolLst.Add(GLK0); 

                Bit81 UsedCsTmp = new Bit81(UsedCs);
                SetUsed( ref UsedCsTmp, GLKnxt);

                foreach( var LK in SolLst.Where(P=>(P.type==W))){
                    int   noB=1<<LK.no;        
                    Bit81 SolBP=new Bit81();      
                    
                    LK.UGCellsA.ForEach(P=>{ if((P.FreeB&noB)>0) SolBP.BPSet(P.rc); });
                    LK.UGCellsB.ForEach(P=>{ if((P.FreeB&noB)>0) SolBP.BPSet(P.rc); });
                    if( SolBP.BitCount()<=1 ) continue;
                    foreach( var P in pBDL.Where(p=>(p.FreeB&noB)>0) ){
                        if( UsedCsTmp.IsHit(P.rc) ) continue;
                        if( !(SolBP-ConnectedCells[P.rc]).IsZero() )  continue;
                        if( (P.FreeB&noB)==0 )  continue;
                        P.CancelB |= noB;　　//除外
                        SolFond=true;
                    }
                }

                var LKpre=SolLst[0];               
                foreach( var LK in SolLst.Skip(1) ){  
                    if( LKpre.type==S && LK.type==S && LK.UGCellsA.Count==1 ){
                        var P=LK.UGCellsA[0];
                        int noB2=P.FreeB-((1<<LKpre.no2)|(1<<LK.no));                       
                        if( noB2>0 ){ P.CancelB |= noB2; SolFond=true; }
                    }
                    LKpre=LK;
                }
                if( SolFond ) SolCode=1;
            }
            else{           　//◆discontinuous
                int dcTyp= GLK0.type*10+GLKnxt.type;
                UCell P=GLK0.UGCellsA[0];
                switch(dcTyp){
                    case 11: 
                        P.FixedNo=GLK0.no+1; //セル数字確定
                        P.CancelB=P.FreeB.DifSet(1<<(GLK0.no));
                        SolCode=1; SolFond=true; //(1:確定）
                        break;
                    case 12: P.CancelB=1<<GLKnxt.no; SolCode=2; SolFond=true; break;//(2:候補から除外）
                    case 21: P.CancelB=1<<GLK0.no; SolCode=2; SolFond=true; break;
                    case 22: 
                        if( GLK0.no==GLKnxt.no ){ P.CancelB=1<<GLK0.no; SolFond=true; SolCode=2; }
                        break;
                }
            }

            if( SolFond ) return SolType;
            return -1;
        }

        private void _GroupedNL_SolResult( GroupedLink LK0, GroupedLink LKnxt, Stack<GroupedLink> SolStack, int SolType ){          
            string st = "";

            List<GroupedLink> SolLst=SolStack.ToList();
            SolLst.Reverse();
            SolLst.Add(LK0);

            foreach( var LK in SolLst ){
                bool bALK = LK is ALSLink;
                int type = (LK is ALSLink)? S: LK.type;//ALSLinkは、ALS内ではS
                foreach( var P1 in LK.UGCellsA ){
                    int noB=(1<<LK.no);
                    if( !bALK )  P1.SetCellBgColor(SolBkCr);
                    if( type==S ){ P1.SetNoBColor(noB,AttCr);  }
                    else{          P1.SetNoBColor(noB,AttCr3); }
                }

                if( type==W ){
                    foreach( var P2 in LK.UGCellsB ){
                        int noB2=(1<<LK.no);
                        if( !bALK )  P2.SetCellBgColor(SolBkCr);
                        P2.SetNoBColor(noB2,AttCr);
                    }
                }
            }

            int cx=2;
            foreach( var LK in SolLst ){
                ALSLink ALK = LK as ALSLink;
                if( ALK==null )  continue;
                Color crG = _ColorsLst[cx++];
                foreach( var P in ALK.ALSbase.B81.IEGet_rc().Select(rc=>pBDL[rc]) ){
                    P.SetCellBgColor(crG);
                }
            }

            if( SolType==1 ) st = "Grouped Nice Loop(Cont.)";  //◆continuous
            else{                                              //◆discontinuous
                int rc=LK0.UGCellsA[0].rc;
                st = "Grouped Nice Loop(Discont.) r"+(rc/9+1)+"c"+(rc%9+1);
                int dcTyp= LK0.type*10+LKnxt.type;
                switch(dcTyp){
                    case 11: st+=" is "+(LK0.no+1); break;
                    case 12: st+=" is not "+(LKnxt.no+1); break;
                    case 21: st+=" is not "+(LK0.no+1); break;
                    case 22: st+=" is not "+(LK0.no+1); break;
                }
                LK0.UGCellsA[0].SetCellBgColor(SolBkCr2);
            }

            Result = st;
            string st2=_ToGroupedRCSequenceString(SolStack);
                // st2 = st2.Replace("<","").Replace(">","");
            ResultLong = st+"\r"+st2;
        }
        private string _ToGroupedRCSequenceString( Stack<GroupedLink> SolStack ){    
            if( SolStack.Count==0 ) return ("[rc]:-");
            List<GroupedLink> SolLst=SolStack.ToList();
            SolLst.Reverse();

            string po = "["+SolLst[0].UGCellsA.ToString()+"]";
            foreach( var LK in SolLst ){
                 string ST_LinkNo="";
                ALSLink ALK=LK as ALSLink;
                if( ALK!=null ){
                    ST_LinkNo = "-#"+(ALK.no+1)+"◆ALS:<"+ALK.ALSbase.ToStringRC()+">#"+(ALK.no2+1)+"-";
                }
                else{
                    string mk = (LK.type==1)? "=": "-";
                    ST_LinkNo = mk+(LK.no2+1)+mk;
                }
                po += ST_LinkNo+"["+LK.UGCellsB.ToString()+ "]";
            }
            return po;
        }

        private int ___GNLCC=0;
        private void ___Debug_Print_GNLChain( Stack<GroupedLink> SolStack, string msg="" ){
            //if( (___GNLCC%100)==0 || msg!="" || ___GNLCC>3500 ){ 
            if(  msg!="" ){ 
                WriteLine( $"{msg}<{___GNLCC}> {_ToGroupedRCSequenceString(SolStack)}" );
            }
        //    if( ___GNLCC==13067 ) WriteLine("Break Hit!");
            ___GNLCC++;
        }
    }
}