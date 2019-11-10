using System;
using System.Collections.Generic;
using System.Linq;

using GIDOO_space;

namespace GNPZ_sdk {
    public class FishMan{
        private List<UCell>   pBDL;             
        private Bit81[]       pHouseCells;

        private int           sz;
        private int           no;
        private List<Bit81>   HBL=new List<Bit81>();
        private List<UNoHouseSet> NoHsLst=null;

        public FishMan( AnalyzerBaseV2 AnB, int FMSize, int no, int sz ){
            this.pBDL = AnB.pBDL;
            this.pHouseCells = AnalyzerBaseV2.HouseCells;
                
            this.no = no;
            this.sz = sz;
            int noB=(1<<no);
            Bit81 BPnoB=new Bit81(pBDL,noB);

            Bit81 Q;
            for( int tfx=0; tfx<FMSize; tfx++ ){ 
                Q = pHouseCells[tfx]&BPnoB;
                if( !Q.IsZero() && !HBL.Contains(Q) ){ Q.ID=tfx; HBL.Add(Q); }
            }
            if( HBL.Count<sz*2 ) return;

            //Fish系解析の高速化(2015.03.26) 
            NoHsLst=new List<UNoHouseSet>();
            Combination cmbBas=new Combination(HBL.Count,sz);
            UNoHouseSet.ID0=0;
            while( cmbBas.Successor() ){
                int   HX=0;
                bool  OvrF=false;
                Bit81 HB81=new Bit81();
                Bit81 OHB81=new Bit81();
                for( int k=0; k<sz; k++ ){
                    int nx=cmbBas.Cmb[k];
                    if( !(Q=HB81&HBL[nx]).IsZero() ){
                        OvrF=true;      //重なりあり
                        OHB81 |= Q;
                    }
                    HX   |= 1<<HBL[nx].ID;  //house番号
                    HB81 |= HBL[nx];        //Bit81
                }
                NoHsLst.Add( new UNoHouseSet(HX,HB81,OvrF,OHB81) );
            }
        }

        public IEnumerable<UFish> IEGet_BaseSet( int BaseSel, bool EndoFlg=false ){ //EndoF==true:EndoFin許容
            if(NoHsLst==null)  yield break;
            foreach( var R in NoHsLst ){
                if( R.HouseX.DifSet(BaseSel)>0 ) continue;
                if( !EndoFlg&R.OverLapF )        continue;
                UFish UF = new UFish(no,sz,R.HouseX,R.HouseB81,R.OverHB81);
                UF.ID=R.ID;
                yield return UF;
            }
            yield break;
        }
        
        public IEnumerable<UFish> IEGet_CoverSet( UFish BSet, int CoverSel, bool Finned, bool CannFlg=false ){
            if(NoHsLst==null)  yield break;
            foreach( var R in NoHsLst ){
                if( R.HouseX.DifSet(CoverSel)>0 ) continue;
                if( (BSet.HouseB&R.HouseX)>0 )    continue;//BaseSetで使用済み
                if( (BSet.BaseB81&R.HouseB81).IsZero() )   continue;//BaseSetをカバーしない
                if( !CannFlg&R.OverLapF )         continue;

				//CoverSetの各HouseはBaseSetと少なくとも１セル共通部分をもつ【この条件は外しても成り立つ】
				foreach( var hs in R.HouseX.IEGet_BtoNo(27) ){
					if( (BSet.BaseB81&pHouseCells[hs]).IsZero() ) goto nextCoverSet;
				}
                
                Bit81 FinB81=BSet.BaseB81-R.HouseB81;
                if( Finned!=(FinB81.Count>0) ) continue;
                UFish UF = new UFish(BSet,R.HouseX,R.HouseB81,FinB81,R.OverHB81);
                UF.ID=R.ID;
                yield return UF;

			nextCoverSet:
				continue;
            }
            yield break;
        }

        public class UNoHouseSet{
            static public int ID0;
            public int      ID;
            public int      HouseX;
            public Bit81    HouseB81;
            public bool     OverLapF;
            public Bit81    OverHB81;

            public UNoHouseSet( int HouseX, Bit81 HouseB81, bool OverLapF, Bit81 OverHB81 ){
                this.HouseX=HouseX;
                this.HouseB81=HouseB81;
                this.OverLapF=OverLapF;
                this.OverHB81=OverHB81;
                this.ID=ID0++;
            }
        }
    }

    public class UFish{
        public int      ID;
        public int      no;
        public int      sz;
        public Bit81    BaseB81=null;
        public Bit81    EndoFin=null;
        public int      HouseB=0;

        public UFish    BaseSet=null;
        public int      HouseC=0;
        public Bit81    CoverB81=null;
        public Bit81    FinB81=null;
        public Bit81    CannFin=null;

        public UFish( int no, int sz, int HouseB, Bit81 BaseB81, Bit81 EndoFin ){
            this.no=no;
            this.sz=sz;
            this.HouseB =HouseB;
            this.BaseB81=BaseB81;
            this.EndoFin=EndoFin;
        }
          
        public UFish( UFish BaseSet, int HouseC, Bit81 CoverB81, Bit81 FinB81, Bit81 CannFin ){
            this.BaseSet =BaseSet;
            this.HouseB  =BaseSet.HouseB;
            this.HouseC  =HouseC;
            this.CoverB81=CoverB81;
            this.FinB81  =FinB81;
            this.CannFin =CannFin;
        }
        public string ToString( string ttl ){
            string st = ttl + HouseB.HouseToString();
            return st;
        }
    }
}
