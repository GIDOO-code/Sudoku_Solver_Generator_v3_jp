using System;
using System.Collections.Generic;
using System.Linq;
using static System.Math;
using GIDOO_space;

namespace GNPZ_sdk{
    public class PrbTrans{
        private int[,]  prmX={ {0,1,2}, {1,2,0}, {2,0,1}, {2,1,0}, {0,2,1}, {1,0,2} };
        private int[,]  trsX={ {2,0,1,4,5,3}, {2,0,1,4,5,3}, {5,3,4,1,2,0},
                               {1,2,0,5,3,4}, {1,2,0,5,3,4}, {4,5,3,2,0,1} };
        private int[]   prmR={ 0,2,1, 3,5,4 };
        private string[] prmXst={"", "(123)->(312)", "(123)->(231)", "1-3交換", "2-3交換", "1-2交換",
                                 "", "(456)->(645)", "(456)->(564)", "4-6交換", "5-6交換", "4-5交換",
                                 "", "(789)->(978)", "(789)->(897)", "7-9交換", "8-9交換", "7-8交換"};

        private GNPXpzl pGNP;
        public UProblem pGP{ get{ return pGNP.GNPX_Eng.pGP; } }//現問題の実体は GNPX_Eng にある
        public int      ID{ get{ return pGP.ID; } }

        //===== 変換用パラメータ =====
            private UProblem UPorg; //オリジナル問題をここに保管
            private UProblem UPbas;
            public int[]  TrPara;
            private int[,] RCX;
            public  int[] ID_code;
        //----------------------------

        public PrbTrans( GNPXpzl pGNP ){
            this.pGNP=pGNP;  
        }

        private void Initialize( bool StartF=true ){          
            pGNP.GNPX_Eng.Set_MethodLst_Run(true);       //true:全手法を用いる  
            pGNP.SetSolution( pGP, false, SolAll:true ); //数独を解く
            pGP.AnsNum = pGP.BDL.ConvertAll(P=>P.No).ToArray();

            TrPara=new int[18];       
            RCX=new int[4,12];
            for( int k=0; k<9; k++ ) RCX[0,k]=RCX[1,k]=k; 
            for( int k=0; k<3; k++ ) RCX[0,k+9]=RCX[1,k+9]=k*3;
            
            if(StartF) UPorg = pGP.Copy(0,0);
            UPbas = pGP.Copy(0,0);
        }

        public void btnTransEst(){
            if(pGP.AnsNum==null) return;
            Initialize(StartF:false);
        }
        public void btnTransRes(){
            if(pGP.AnsNum==null) return;
            UPbas = UPorg.Copy(0,0);
            UProblem tGP=UPorg.Copy(0,0);
            pGNP.SDKProbLst[ID] = tGP;
            pGNP.GNPX_Eng.pGP = tGP;
           
            TrPara=new int[18];           
            RCX=new int[4,12];
            for( int k=0; k<9; k++ ) RCX[0,k]=RCX[1,k]=k; 
            for( int k=0; k<3; k++ ) RCX[0,k+9]=RCX[1,k+9]=k*3;
        }
            
        public void SDK_TransProbG( string ctrl, bool DspSolB ){
            if(pGP.AnsNum==null) Initialize(); //問題を解く

            int ixM=0, ixR=TrPara[8], ixC=1-ixR, nx, m, n;
            switch(ctrl){   
                case "NumChange": break;
                case "Checked": break;

                case "btnPatCVRg":
                    ixM=TrPara[0]=(++TrPara[0])%6;
                    for( int k=0; k<3; k++ )  RCX[ixR,k+9]=prmX[ixM,k]*3;
                    break;
                case "btnPatCVR123g":
                    nx=RCX[ixR,9]; ixM=TrPara[1]=(++TrPara[1])%6;
                    for( int k=0; k<3; k++ )  RCX[ixR,k+nx]=prmX[ixM,k]+nx;
                    break;
                case "btnPatCVR456g":
                    nx=RCX[ixR,10]; ixM=TrPara[2]=(++TrPara[2])%6;
                    for( int k=0; k<3; k++ )  RCX[ixR,k+nx]=prmX[ixM,k]+nx;
                    break;
                case "btnPatCVR789g":
                    nx=RCX[ixR,11]; ixM=TrPara[3]=(++TrPara[3])%6;
                    for( int k=0; k<3; k++ )  RCX[ixR,k+nx]=prmX[ixM,k]+nx;
                    break;

                case "btnPatCVCg":
                    ixM=TrPara[4]=(++TrPara[4])%6;
                    for( int k=0; k<3; k++ )  RCX[ixC,k+9]=prmX[ixM,k]*3;
                    break;
                case "btnPatCVC123g":
                    nx=RCX[ixC,9]; ixM=TrPara[5]=(++TrPara[5])%6;                    
                    for( int k=0; k<3; k++ )  RCX[ixC,k+nx]=prmX[ixM,k]+nx;
                    break;
                case "btnPatCVC456g":
                    nx=RCX[ixC,10]; ixM=TrPara[6]=(++TrPara[6])%6;
                    for( int k=0; k<3; k++ )  RCX[ixC,k+nx]=prmX[ixM,k]+nx;
                    break;
                case "btnPatCVC789g":
                    nx=RCX[ixC,11]; ixM=TrPara[7]=(++TrPara[7])%6;
                    for( int k=0; k<3; k++ )  RCX[ixC,k+nx]=prmX[ixM,k]+nx;
                    break;
#if false      
                case "btnTransRtg": //右回転
                    ixR=TrPara[8]=(++TrPara[8])%2;
                    ixC=1-ixR;
                    subTransLR(ixC);
                    if(ixR==0) subTransLR(ixC);
                    break;
#endif
                case "btnPatCVRCg":
                case "btnPatCVRCg2":
                    ixR=TrPara[8]=(++TrPara[8])%2;
                    ixC=1-ixR;
                    if(ctrl=="btnTransRtg") goto case "btnTransLRg";//(右回転のとき、左右反転へ）
                    break;
                
                case "btnTransLRg": //左右反転
                    subTransLR(ixC);
                    break; 
               
                case "btnTransUDg": //上下反転
                    subTransUD(ixR);
                    break;                        

                case "btnPatCVR123987g": //123行対称変換
                    nx=RCX[ixR,9]; m=TrPara[1]; ixM=TrPara[1]=(m+1)%6;
                    for( int k=0; k<3; k++ )  RCX[ixR,k+nx]=prmX[ixM,k]+nx;
                    nx=RCX[ixR,11]; n=TrPara[3]; ixM=TrPara[3]=trsX[m,n];
                    for( int k=0; k<3; k++ )  RCX[ixR,k+nx]=prmX[ixM,k]+nx;
                    break;
                case "btnPatCVC123987g": //123列対称変換
                    nx=RCX[ixC,9]; m=TrPara[5]; ixM=TrPara[5]=(m+1)%6;
                    for( int k=0; k<3; k++ )  RCX[ixC,k+nx]=prmX[ixM,k]+nx;
                    nx=RCX[ixC,11]; n=TrPara[7]; ixM=TrPara[7]=trsX[m,n];
                    for( int k=0; k<3; k++ )  RCX[ixC,k+nx]=prmX[ixM,k]+nx;
                    break;                 
                case "btnPatCVR46g": //46行対称変換
                    nx=RCX[ixR,10]; ixM=TrPara[2]=(TrPara[2]+3)%6;
                    for( int k=0; k<3; k++ )  RCX[ixR,k+nx]=prmX[ixM,k]+nx;
                    break;
                case "btnPatCVC46g": //46列対称変換
                    nx=RCX[ixC,10]; ixM=TrPara[6]=(TrPara[6]+3)%6;
                    for( int k=0; k<3; k++ )  RCX[ixC,k+nx]=prmX[ixM,k]+nx;
                    break;     
            }

            for( int j=0; j<2; j++ ){
                for( int k=0; k<9; k++ ){
                    nx=RCX[j,k/3+9];
                    RCX[j+2,k] = RCX[j,nx+k%3];
                }
            }

            List<UCell> UCL=new List<UCell>();
            int[] AnsN2=new int[81];
            int   r, c, w;
            
            for( int rc=0; rc<81; rc++ ){
                r=RCX[ixR+2,rc/9]; c=RCX[ixC+2,rc%9];
                if(ixR==1){ w=r; r=c; c=w; }
                int rc2=r*9+c;
                UCell P=UPbas.BDL[rc2];
                UCL.Add( new UCell(rc,P.No,P.FreeB) );
                AnsN2[rc]=UPbas.AnsNum[rc2];
            }
            UProblem UP=pGP.Copy(0,0);
            UP.BDL=UCL; UP.AnsNum=AnsN2;
            pGNP.SDKProbLst[ID] = UP;
            if(!DspSolB) UP.BDL.ForEach(P=>{P.No=Max(P.No,0);});
            pGNP.CurrentPrbNo=ID;

            SetIDCode(TrPara,AnsN2);

                /*
                        string st="RCX:";
                        for( int j=0; j<4; j++ ){
                            for( int k=0; k<12; k++ ) st+=" "+RCX[j,k];
                            st+=" / ";
                        }
                        for( int k=0; k<9; k++ ) st+=" "+TrPara[k];
                        st+="//";
                        int[] BPw=new int[3];
                        for( int k=9; k<12; k++ ){BPw[k-9]=TrPara[k]; st+=" "+TrPara[k]; }
                        WriteLine(st);                 
                        
                        Bit81 BP=new Bit81(BPw);
                        WriteLine(BP);
                */
        }

        private void subTransUD( int ixR ){ 
            int ixM=TrPara[0]=(TrPara[0]+3)%6;
            for( int k=0; k<3; k++) RCX[ixR,k+9]=prmX[ixM,k]*3;

            int m=TrPara[1], n=TrPara[3];
            int nx=RCX[ixR,9]; ixM=TrPara[1]=(n+3)%6;
            for( int k=0; k<3; k++) RCX[ixR,k+nx]=prmX[ixM,k]+nx;

            nx=RCX[ixR,11]; ixM=TrPara[3]=(m+3)%6;
            for( int k=0; k<3; k++) RCX[ixR,k+nx]=prmX[ixM,k]+nx;

            nx=RCX[ixR,10]; ixM=TrPara[2]=(TrPara[2]+3)%6;
            for( int k=0; k<3; k++) RCX[ixR,k+nx]=prmX[ixM,k]+nx;
            return;
        }
        private void subTransLR( int ixC ){
            int ixM=TrPara[4]=(TrPara[4]+3)%6;
            for( int k=0; k<3; k++) RCX[ixC,k+9]=prmX[ixM,k]*3;

            int m=TrPara[5], n=TrPara[7];
            int nx=RCX[ixC,9]; ixM=TrPara[5]=(n+3)%6;
            for( int k=0; k<3; k++) RCX[ixC,k+nx]=prmX[ixM,k]+nx;

            nx=RCX[ixC,11]; ixM=TrPara[7]=(m+3)%6;
            for( int k=0; k<3; k++) RCX[ixC,k+nx]=prmX[ixM,k]+nx;

            nx=RCX[ixC,10]; ixM=TrPara[6]=(TrPara[6]+3)%6;
            for( int k=0; k<3; k++) RCX[ixC,k+nx]=prmX[ixM,k]+nx;
            return;
        }

        public void SetRCX( int mx, int[] TPw ){         
            int rx=TPw[8], cx=1-rx, nx, kx;
            switch(mx){ 
                case 0:                kx=TPw[0]; for(int k=0;k<3;k++) RCX[rx,k+9] =prmX[kx,k]*3;  break;   
                case 1: nx=RCX[rx,9];  kx=TPw[1]; for(int k=0;k<3;k++) RCX[rx,k+nx]=prmX[kx,k]+nx; break;
                case 2: nx=RCX[rx,10]; kx=TPw[2]; for(int k=0;k<3;k++) RCX[rx,k+nx]=prmX[kx,k]+nx; break;
                case 3: nx=RCX[rx,11]; kx=TPw[3]; for(int k=0;k<3;k++) RCX[rx,k+nx]=prmX[kx,k]+nx; break;
                case 4:                kx=TPw[4]; for(int k=0;k<3;k++) RCX[cx,k+9] =prmX[kx,k]*3;  break;
                case 5: nx=RCX[cx,9];  kx=TPw[5]; for(int k=0;k<3;k++) RCX[cx,k+nx]=prmX[kx,k]+nx; break;
                case 6: nx=RCX[cx,10]; kx=TPw[6]; for(int k=0;k<3;k++) RCX[cx,k+nx]=prmX[kx,k]+nx; break;
                case 7: nx=RCX[cx,11]; kx=TPw[7]; for(int k=0;k<3;k++) RCX[cx,k+nx]=prmX[kx,k]+nx; break;
                case 8: break;
            }
        }

        public void SDK_TransIX( int[] TrPara, bool TransB=false, bool DspSolB=false ){
            int rx=TrPara[8], cx=1-rx;
            for( int j=0; j<2; j++ ){
                for( int k=0; k<9; k++ ){
                    int n=RCX[j,k/3+9];
                    RCX[j+2,k] = RCX[j,n+k%3];
                }
            }

            List<UCell> UCL=null;
            if(TransB) UCL=new List<UCell>();
            int [] AnsN2=new int[81];
            int   r, c, w;           
            for( int rc=0; rc<81; rc++ ){
                r=RCX[rx+2,rc/9]; c=RCX[cx+2,rc%9];
                if(rx==1){ w=r; r=c; c=w; }
                int rc2=r*9+c;
                AnsN2[rc]=UPbas.AnsNum[rc2];
                if(TransB){
                    UCell P=UPbas.BDL[rc2];
                    UCL.Add( new UCell(rc,P.No,P.FreeB) );
                }

            }

            if(TransB){
                UProblem UP=pGP.Copy(0,0);
                UP.BDL=UCL; UP.AnsNum=AnsN2;
                pGNP.SDKProbLst[ID] = UP;
                if(!DspSolB) UP.BDL.ForEach(P=>{P.No=Max(P.No,0);});
                pGNP.CurrentPrbNo=ID;
            }

            SetIDCode(TrPara,AnsN2);
        }
        public void SetIDCode( int[] TP, int[] AnsNum ){
            TP[9]=TP[10]=TP[11]=0;
            for( int rc=0; rc<81; rc++ ) if(AnsNum[rc]>0) TP[9+rc/27] |= (1<<(26-rc%27));    

            int Q=0;
            for( int k=0; k<9; k++ ) Q = Q*10 + Abs(AnsNum[(k/3*9)+(k%3)]);
            TP[16]=Q;
        }

        public string SDK_Nomalize( bool DspSolB, bool NrmlNum ){
            int[]  TPw=new int[18];
            List<int[]> TPLst=new List<int[]>();
            if(pGP.AnsNum==null) Initialize(); //問題を解く

    #region パターンの標準化
            RCX=new int[4,12];
            for( int k=0; k<9; k++ ) RCX[0,k]=RCX[1,k]=k; 
            for( int k=0; k<3; k++ ) RCX[0,k+9]=RCX[1,k+9]=k*3;

            int minV=int.MaxValue;
        //===== step1 =====
            for( int tx=0; tx<2; tx++ ){
                TPw[8]=tx;
                for( int rx0=0; rx0<6; rx0++ ){
                    TPw[0]=rx0; SetRCX(0,TPw);
                    for( int rx1=0; rx1<6; rx1++ ){
                        TPw[1]=rx1; SetRCX(1,TPw);

                        for( int cx4=0; cx4<6; cx4++ ){ //6->3とし高速化の方法もあるが、ここでは単純に
                            TPw[4]=cx4; SetRCX(4,TPw);
                            for( int cx5=0; cx5<6; cx5++ ){
                                TPw[5]=cx5; SetRCX(5,TPw);
                                SDK_TransIX(TPw);

                                if( TPw[9]>minV ) continue;
                                minV=TPw[9];
                                int[]  TPtmp=new int[18];
                                TPw.CopyTo(TPtmp,0);
                                TPLst.Add(TPtmp);
                            }
                        }
                    }  
                }
            }          
            TPLst.Sort((A,B)=>(A[9]-B[9]));

        //===== step2 =====
            minV=TPLst[0][9];
            TPLst = TPLst.FindAll(P=> P[9]==minV).ToList();
            int TPLstCount=TPLst.Count;
            for(int hx=0; hx<TPLstCount; hx++ ){
                TPw[8]=TPLst[hx][8]; SetRCX(8,TPw);
                for( int mx=0; mx<18; mx++ ){
                    TPw[mx]=TPLst[hx][mx];
                    if(mx<9) SetRCX(mx,TPw);
                }
                for( int cx6=0; cx6<6; cx6++ ){
                    TPw[6]=cx6; SetRCX(6,TPw);
                    for( int cx7=0; cx7<6; cx7++ ){
                        TPw[7]=cx7; SetRCX(7,TPw);
                        SDK_TransIX(TPw);

                        if(TPw[9]>minV) continue;
                        minV=TPw[9];
                        int[]  TPtmp=new int[18];
                        TPw.CopyTo(TPtmp,0);
                        TPLst.Add(TPtmp);
                    }  
                }
            }          
            TPLst.Sort((A,B)=>(A[9]-B[9]));

       //===== step3 =====
            minV=TPLst[0][9];
            TPLst = TPLst.FindAll(P=> P[9]==minV).ToList();
            minV=TPLst[0][10];
            TPLstCount=TPLst.Count;
            for(int hx=0; hx<TPLstCount; hx++ ){
                TPw[8]=TPLst[hx][8]; SetRCX(8,TPw);
                for( int mx=0; mx<18; mx++ ){
                    TPw[mx]=TPLst[hx][mx];
                    if(mx<9) SetRCX(mx,TPw);
                }
                for( int rx2=0; rx2<6; rx2++ ){
                    TPw[2]=rx2; SetRCX(2,TPw);
                    for( int rx3=0; rx3<6; rx3++ ){
                        TPw[3]=rx3; SetRCX(3,TPw);
                        SDK_TransIX(TPw);

                        if(TPw[10]>minV) continue;
                        minV=TPw[10];
                        int[]  TPtmp=new int[18];
                        TPw.CopyTo(TPtmp,0);
                        TPLst.Add(TPtmp);
                    }  
                }
            }        
            TPLst.Sort((A,B)=>{
                if(A[10]!=B[10]) return (A[10]-B[10]);
                return (A[11]-B[11]);
            } );
            minV=TPLst[0][10];
            int minV1=TPLst[0][11];
            TPLst = TPLst.FindAll(P=> (P[10]==minV && P[11]==minV1)).ToList();

            string[] stLst=new string[TPLst.Count];
            for( int k=0; k<TPLst.Count; k++ ){
                TPLst[k].CopyTo(TrPara,0);
                SetRCX(8,TPw); 
                for( int mx=0; mx<9; mx++ ) SetRCX(mx,TrPara);
                SDK_TransIX(TrPara,TransB:true,DspSolB:DspSolB);

                string st=pGNP.SDKCntrl.Get_SDKNumPattern(TrPara,pGP.AnsNum);//ラテン方陣
                st+=TransToString(TrPara);
                stLst[k]=st;
                TrPara[17]=k;
                for( int n=0; n<TrPara.Count(); n++ ) TPLst[k][n]=TrPara[n];
                    /*
                            string st="RCX:";
                            for( int j=0; j<4; j++ ){
                                for( int m=0;m<12; m++ ) st+=" "+RCX[j,m];
                                st+=" / ";
                            }                      
                        
                            for( int m=0; m<9; m++ ) st+=" "+TrPara[m];
                            st+="//";
                            int[] IDC=new int[4];
                            for( int m=9; m<13; m++ ){ IDC[m-9]=TrPara[m]; st+=" "+TrPara[k]; }
                            WriteLine(st); 
                         // Bit81 BP=new Bit81(IDC);
                         // WriteLine(BP); 
                */
            }

            TPLst.Sort((A,B)=>{
                for( int k=9; k<A.Count(); k++ ) if(A[k]!=B[k]) return (A[k]-B[k]);
                return 0;
            } );

            TrPara=TPLst[0];
            SetRCX(8,TrPara);
            for( int mx=0; mx<8; mx++ ) SetRCX(mx,TrPara);
    #endregion パターンの標準化

    //ラテン方陣の標準化
            SDK_TransIX(TrPara,TransB:true,DspSolB:DspSolB);
            
            if(NrmlNum){
                int[] chgNum=new int[10];
                int NN=TrPara[15];
                for( int k=0; k<=9; k++ ){ chgNum[9-k]=NN%10; NN/=10; }
                for( int rc=0; rc<81; rc++ ){
                    int No=pGP.AnsNum[rc];
                    UCell P=pGP.BDL[rc];
                    if( P.No>0 ) pGP.AnsNum[rc]=P.No=chgNum[No];
                    else         pGP.AnsNum[rc]=P.No=-chgNum[-No];
                }
            }
                /*
                            string po="◆";
                            for( int k=0; k<18; k++ ) po+=" "+TrPara[k];
                            WriteLine(po); 
                */
            return stLst[TrPara[17]];
        }

        public string TransToString( int[] TrPara ){
            string st=(TrPara[8]==0)? "": " ・行列転置\r";

            for( int k=0; k<8; k++ ){
                int n=TrPara[k], m=0;
                if( n>0 ){
                    st+=" ・";
                    switch(k){
                        case 0: st+="行ブロック"; break;
                        case 1: st+="行"; break;
                        case 2: st+="行"; m=6; break;
                        case 3: st+="行"; m=12; break;
                        case 4: st+="列ブロック"; break;
                        case 5: st+="列"; break;
                        case 6: st+="列"; m=6; break;
                        case 7: st+="列"; m=12; break;
                    }
                    st+=prmXst[n+m]+"\r";
                }
            }
            if(st!="")  st="\r\r======================\r"+st;
            return st;
        }

        public void RCNChange( string RCNchg, int RCNidx, bool DspSolB ){
            if(pGP.AnsNum==null) Initialize(); //問題を解く
            //"変更なし","RCN->RNC","RCN->CRN","RCN->CNR","RCN->NRC","RCN->NCR"

            List<UCell> UCL=new List<UCell>();
            int[] AnsN2=new int[81];
            int r1=0,c1=0,n1=0;
            foreach( var rc in Enumerable.Range(0,81) ){// for( int rc=0; rc<81; rc+ ){
                int r0=rc/9, c0=rc%9, n0=pGP.AnsNum[rc];
                int pm=(n0>0)? 1: -1;
                if(n0<0) n0=-n0;
                n0--;
                switch(RCNidx){
                    case 0: r1=r0; c1=c0; n1=n0+1; break; //変更なし
                    case 1: r1=r0; c1=n0; n1=c0+1; break; //RCN->RNC
                    case 2: r1=c0; c1=r0; n1=n0+1; break; //RCN->CRN
                    case 3: r1=c0; c1=n0; n1=r0+1; break; //RCN->CNR
                    case 4: r1=n0; c1=r0; n1=c0+1; break; //RCN->NRC
                    case 5: r1=n0; c1=c0; n1=r0+1; break; //RCN->NCR
                }
                int rc1=r1*9+c1;
                UCell P=UPbas.BDL[rc1];
                UCL.Add( new UCell(rc1,n1*pm,P.FreeB) );
                AnsN2[rc]=UPbas.AnsNum[rc1];
            }
            UCL.Sort((a,b)=>(a.rc-b.rc));

            UProblem UP=pGP.Copy(0,0);
            UP.BDL=UCL; UP.AnsNum=AnsN2;
            pGNP.SDKProbLst[ID] = UP;
            if(!DspSolB) UP.BDL.ForEach(P=>{P.No=Max(P.No,0);});
            pGNP.CurrentPrbNo=ID;

            SetIDCode(TrPara,AnsN2);
        }
    }
}
   