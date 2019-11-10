using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using GIDOO_space;
using static System.Console;

namespace GNPZ_sdk{
    public partial class NiceLoopGen: AnalyzerBaseV2{
		private int GStageMemo;
        private int S=1;
        public  int NiceLoopMax{ get{ return GNPXpzl.GMthdOption["NiceLoopMax"].ToInt(); } }

        public NiceLoopGen( GNPX_AnalyzerMan pAnMan ): base(pAnMan){ }

		private void Prepare(){
			if(pAnMan.GStage!=GStageMemo) {
				GStageMemo=pAnMan.GStage;
				CeLKMan.Initialize();
				CeLKMan.PrepareCellLink(1+2);
			}      
		}

        public bool NiceLoop( ){  //深さ優先探索
			Prepare();
            CeLKMan.PrepareCellLink(1+2);    //strongLink,weakLink生成

            for( int szCtrl=4; szCtrl<NiceLoopMax; szCtrl++ ){   //サイズの小さいNiceLoopから探す
                foreach( var P0 in pBDL.Where(p=>(p.No==0)) ){   //起点セル

                    foreach( var no in P0.FreeB.IEGet_BtoNo() ){ //始めの数字
                        foreach( var LKH in CeLKMan.IEGetRcNoType(P0.rc,no,3) ){   //最初のリンク
                            if( pAnMan.CheckTimeOut() ) return false;
                            var SolStack=new Stack<UCellLink>();
                            SolStack.Push(LKH); //▼Push                     
                            Bit81 UsedCells=new Bit81(LKH.rc2);  //使用済みセルのビット表現
                            _NL_Search(LKH,LKH,SolStack,UsedCells,szCtrl-1);
                            if(SolCode>0) return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool _NL_Search( UCellLink LK0, UCellLink LKpre, Stack<UCellLink> SolStack, Bit81 UsedCells, int szCtrl ){
            if( szCtrl<=0 ) return false;

            foreach( var LKnxt in CeLKMan.IEGet_CeCeSeq(LKpre) ){  //連結条件を満たすリンクコレクションの反復処理
                int rc2Nxt = LKnxt.rc2;
                if( UsedCells.IsHit(rc2Nxt) ) continue;  //(次セルは既に使用)(UsedCellsは起点セルを含まない)

                { //===== Chain Search =====
                    SolStack.Push(LKnxt);//▼Push(試行:リンク延伸）  
                    //___Debug_Print_NLChain(SolStack); //▼▼▼
                    if( rc2Nxt==LK0.rc1 && szCtrl==1 ){
                        if( SolStack.Count>2 ){  //ループが形成された(次セルは起点セルと一致）
                            int SolType=_NL_CheckSolution(LK0,LKnxt,SolStack,UsedCells);//解チェック
                            if( SolType>0 ){          
                                if( SolInfoDsp ) _NL_SolResult(LK0,LKnxt,SolStack,SolType);
                                if( !pAnMan.SnapSaveGP(true) )  return true;
                            }
                        }
                    }
                    else{
                        Bit81 UsedCellsNxt = UsedCells|(new Bit81(rc2Nxt));   //新しい"使用済みセルのビット表現"を作成
                        _NL_Search(LK0,LKnxt,SolStack,UsedCellsNxt,szCtrl-1); //次段階の探索(recursive call)
                        if(SolCode>0 ) return true;
                    }
                    SolStack.Pop();     //▲Pop(不成立:リンク延伸取消し）
                } //-----------------------------
            }  
            return false;
        }

        private int _NL_CheckSolution( UCellLink LK0, UCellLink LKnxt, Stack<UCellLink> SolStack, Bit81 UsedCells ){ 
            bool SolFond=false;
            int SolType = CeLKMan.Check_CellCellSequence(LKnxt,LK0)? 1: 2; //1:Continuous 2:DisContinuous

            if(SolType==1){ //◆continuous
                //=== 弱いリンクを強いリンクに変えるための除外
                List<UCellLink> SolLst=SolStack.ToList();
                Bit81 UsedCellsT = UsedCells|(new Bit81(LK0.rc1));
                foreach( var L in SolLst ){
                    int noB=1<<L.no;
                    foreach( var P in pBDL.IEGetCellInHouse(L.tfx,noB) ){
                        if( UsedCellsT.IsHit(P.rc) ) continue;
                        P.CancelB |= noB;　　//除外
                        SolFond=true;
                    }
                }

                //=== SSセルでは、他の数字は入らない
                SolLst.Reverse();
                SolLst.Add(LK0);                           
                var LKpre=SolLst[0];
                foreach( var LK in SolLst.Skip(1) ){
                    if( LKpre.type==1 && LK.type==1 ){ //SSセル
                        UCell P=pBDL[LK.rc1];
                        int noB = P.FreeB.DifSet((1<<LKpre.no)|(1<<LK.no));
                        if( noB>0 ){ P.CancelB=noB; SolFond=true; }
                    }
                    LKpre=LK;
                }
                if(SolFond) SolCode=2;
            }
            else if(SolType==2){           　//◆discontinuous
                int dcTyp= LK0.type*10+LKnxt.type;
                UCell P=LK0.UCe1;
                switch(dcTyp){
                    case 11: 
                        P.FixedNo=LK0.no+1; //セル数字確定
                        P.CancelB=P.FreeB.DifSet(1<<(LK0.no));
                        SolCode=1; SolFond=true; //(1:確定）
                        break;
                    case 12: P.CancelB=1<<LKnxt.no; SolCode=2; SolFond=true; break;//(2:候補から除外）
                    case 21: P.CancelB=1<<LK0.no; SolCode=2; SolFond=true; break;
                    case 22: 
                        if( LK0.no==LKnxt.no ){ P.CancelB=1<<LK0.no; SolFond=true; SolCode=2; }
                        break;
                }
            }
            if(SolFond){ return SolType; }
            return -1;
        }

        private void _NL_SolResult( UCellLink LK0, UCellLink LKnxt, Stack<UCellLink> SolStack, int SolType ){          
            string st = "";

            List<UCellLink> SolLst=SolStack.ToList();
            SolLst.Reverse();
            SolLst.Add(LK0);

            foreach( var LK in SolLst ){
                int noB=(1<<LK.no);
                UCell P1=pBDL[LK.rc1], P2=pBDL[LK.rc2];
                P2.SetCellBgColor(SolBkCr);
                if( LK.type==S ){ P1.SetNoBColor(noB,AttCr); P2.SetNoBColor(noB,AttCr3); }
                else{             P2.SetNoBColor(noB,AttCr); P1.SetNoBColor(noB,AttCr3); }
            }

            if( SolType==1 ) st = "Nice Loop(Continuous)";  //◆continuous
            else{                                           //◆discontinuous
                st = "Nice Loop(Discontinuous) r"+(LK0.rc1/9+1)+"c"+(LK0.rc1%9+1);
                int dcTyp= LK0.type*10+LKnxt.type;
                switch(dcTyp){
                    case 11: st+=" is "+(LK0.no+1); break;
                    case 12: st+=" is not "+(LKnxt.no+1); break;
                    case 21: st+=" is not "+(LK0.no+1); break;
                    case 22: st+=" is not "+(LK0.no+1); break;
                }
                LK0.UCe1.SetCellBgColor(SolBkCr2);
            }

            Result = st;
            ResultLong = st+"\r"+_ToRCSequenceString(SolStack);
        }
        private string _ToRCSequenceString( Stack<UCellLink> SolStack ){    
            if( SolStack.Count==0 ) return ("[rc]:-");
            List<UCellLink> SolLst=SolStack.ToList();
            SolLst.Reverse();

            UCellLink LK0=SolLst[0];
            UCell     P0 =pBDL[LK0.rc1];
            string po = "[rc]:["+(P0.rc/9*10+(P0.rc%9)+11)+ "]";
            foreach( var LK in SolLst ){
                UCell  P1 = pBDL[LK.rc2];
                string mk = (LK.type==1)? "=": "-";
                po += mk+(LK.no+1)+mk+"["+(P1.rc/9*10+(P1.rc%9)+11)+ "]";
            }
            return po;
        }

        private int ___NLCC=0;
        private void ___Debug_Print_NLChain( Stack<UCellLink> SolStack ){
            WriteLine( $"<{___NLCC++}> {_ToRCSequenceString(SolStack)}" );
        }
    }
}