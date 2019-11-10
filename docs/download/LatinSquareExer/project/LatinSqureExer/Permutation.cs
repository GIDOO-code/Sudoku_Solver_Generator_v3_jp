using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;

namespace GNPZ_sdk{
/*
    //permutation test
    Permutation perm = new Permutation(10,6);

    int xx = 5;
    for( ; perm!=null; perm=perm.Successor(xx) ){

        string st="";
        Array.ForEach( perm.Pwrk, x=> st+=(" "+x) );
        Console.WriteLine( st );
    }
*/
    public class Permutation{
        public int    Psz{ get; private set; } //Psz
        public int    Ssz{ get; private set; } //Ssz
        private int[] Pwrk=null;
        public  int[] Pnum=null;
        private bool  First;
 
        public Permutation( int Psz, int Ssz=-1 ){
            this.Psz  = Psz;
            this.Ssz  = Ssz;
            if( Ssz<=0 || Ssz>Psz ) this.Ssz=Psz;
            if( Psz>0 ){
                Pwrk = Enumerable.Range(0,Psz).ToArray();
                Pnum = Enumerable.Range(0,this.Ssz).ToArray();
            }
            First=true;
        }
        public bool Successor( int nxtX=-1 ){
            if( First || Pwrk==null ){ First=false; return (Pwrk!=null); }
            int r = (nxtX>=0)? nxtX: Ssz-1;
            r = Math.Min(r,Ssz-1);
            
            do{
                if( r<0 )  break;
                int A=Pwrk[r];
        
            L_1: 
                if( A>=Psz-1 ){ r--; continue; }
                A++;
                for( int nx=0; nx<r; nx++ ){ if( Pwrk[nx]==A ) goto L_1; }  
                Pwrk[r]=A;
                if( r<Psz-1 ){
                    if( Psz<=64 ){
                        ulong bp=0;
                        for( int k=0; k<=r; k++ ) bp |= (1u<<Pwrk[k]);
                        r++;
                        for( int n=0; n<Psz; n++ ){
                            if( (bp&(1u<<n))==0 ){
                                Pwrk[r++]=n;
                                if( r>=Psz ) break;
                            }
                        }
                    }
                    else{
                        int[] wx = Enumerable.Range(0,Psz).ToArray();
                        for( int k=0; k<=r; k++ ) wx[Pwrk[k]]=-1;

                        int s=0;
                        for( int k=r+1; k<Psz; k++ ){
                            for( ; s<Psz; s++ ){
                                if( wx[s]<0 ) continue;
                                Pwrk[k]=wx[s++];
                                break;
                            }
                        }
                    }
                }
                for( int k=0; k<Ssz; ++k ) Pnum[k]=Pwrk[k];
                return true;
            }while(true);
            return false;
        }
        public override string ToString(){
            string st=""; Array.ForEach( Pnum, p=> st+=(" "+p) );
            st += "  ";   Array.ForEach( Pwrk, p=> st+=(" "+p) );
            return st;
        }
    }
}