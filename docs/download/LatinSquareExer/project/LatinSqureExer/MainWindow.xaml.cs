using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Globalization;

using GNPZ_sdk;

namespace LatinSquareExer{
    public partial class MainWindow : Window {
        private int[,] LS=new int[9,9];

        public MainWindow() {
            InitializeComponent();
         
//            LSG.GeneratePara( ref LS, TopParaX, LftParaX );
            SetInfo();   
        }
        
        private void btnEnd_Click( object sender, RoutedEventArgs e ) {
            this.Close();
        }
        private CultureInfo CulInfoJpn = CultureInfo.GetCultureInfo("ja-JP");
        private void ReDraw(){
            PB_GBoard.Source = null;
            PB_GBoard.Source = DrawBmp(LS);
        }
        private RenderTargetBitmap DrawBmp( int[,] LS ){
            int   lineWidth = 2;
            int   cellSize  = 36;
            int   cellSizeP = cellSize+1;
            Rect  rct;
            Point pt = new Point();
            Rect Rrct = new Rect(0,0, cellSize,cellSize);
            Brush brBack=Brushes.LightGray;
            
            var drawVisual = new DrawingVisual();
            using( DrawingContext DC=drawVisual.RenderOpen() ){
                #region Draw numbers on board
                for( int rc=0; rc<81; rc++ ){   
                    int r=rc/9, c=rc%9;
                    pt.X = c*cellSizeP + lineWidth/2;  
                    pt.Y = r*cellSizeP + lineWidth/2;
                    Rrct.X=pt.X;　Rrct.Y=pt.Y;
                    rct = new Rect( pt.X+1, pt.Y+1, cellSizeP, cellSizeP);
                    DC.DrawRectangle( brBack, null, rct );

                    if( LS[r,c]>0 ){
                        string st=LS[r,c].ToString().PadLeft(2);
                        FormattedText FT = new FormattedText(st, CulInfoJpn, 
                            FlowDirection.LeftToRight, new Typeface("Verdana"), 28, Brushes.Blue );                
                        DC.DrawText( FT, pt );
                    }
                }
                #endregion Draw numbers on board
           
                #region Draw a frame on the board
                Brush brBL = new SolidColorBrush(Colors.Navy);
                Pen pen1 = new Pen(brBL,1);
                Pen pen2 = new Pen(brBL,2);
                Pen pen;

                int hh=1;
                for( int k=0; k<10; k++ ){
                    Point ptS = new Point(0,hh);
                    Point ptE = new Point(cellSize*10-2,hh);
                    pen = ((k%3)==0)? pen2: pen1;
                    DC.DrawLine( pen, ptS, ptE);
                    hh += cellSizeP + (k%3)/2;
                }

                hh=1;
                for( int k=0; k<10; k++ ){
                    Point ptS = new Point(hh,0);
                    Point ptE = new Point(hh,cellSize*10-2);
                    pen = ((k%3)==0)? pen2: pen1;
                    DC.DrawLine( pen, ptS, ptE);
                    hh += cellSizeP + (k%3)/2;
                }
                #endregion Draw a frame on the board
            }
            var bmp = new RenderTargetBitmap(332,332, 96,96, PixelFormats.Default);
            bmp.Render(drawVisual);

            return bmp;
        }

        private LatinSquareGen LSG=new LatinSquareGen();
        private int[] TopPara = new int[8];
        private int[] LftPara = new int[8];
        private int   TopParaX=0;
        private int   LftParaX=0;
        private int   nPattern;

        private void BtnB1_Click( object sender, RoutedEventArgs e ) {
            TopParaX=LftParaX=0;
            for( int rc=0; rc<9; rc++ ) LS[rc/3,rc%3]=rc+1;
            LSG.GeneratePara( TopParaX, LftParaX, ref LS, ref TopPara, ref LftPara );
            SetInfo(); 
            RX=2;
        }
        private void BtnB12_Click( object sender, RoutedEventArgs e ) {
            Button btn = sender as Button;
            int s2=TopParaX%56, nx=-1;
            switch(btn.Name){
                case "BtnB12":  s2=(++s2)%56; break;
                case "BtnB2R1": nx=2; break;
                case "BtnB3R1": nx=3; break;
                case "BtnB2R2": nx=4; break;
                case "BtnB2R3": nx=5; break; 
                case "BtnB3R2": nx=6; break;
                case "BtnB3R3": nx=7; break;
            }
            if( nx>0 ) TopPara[nx] = (TopPara[nx]+1)%6;

            TopParaX = TopPara[7];
            for( int k=6; k>=2; k-- ) TopParaX = TopParaX*6+TopPara[k];
            TopParaX = TopParaX*56 + s2;
            SetInfo();
            RX=2;
        }
        private void BtnB36_Click( object sender, RoutedEventArgs e ){
            Button btn = sender as Button;
            int s2=LftParaX%56, nx=-1;
            switch(btn.Name){
                case "BtnB36":  s2=(++s2)%56; break;
                case "BtnB4C1": nx=2; break;
                case "BtnB7C1": nx=3; break;
                case "BtnB4C2": nx=4; break;
                case "BtnB7C2": nx=5; break; 
                case "BtnB4C3": nx=6; break;
                case "BtnB7C3": nx=7; break;
            }
            if( nx>0 ) LftPara[nx] = (LftPara[nx]+1)%6;

            LftParaX = LftPara[7];
            for( int k=6; k>=2; k-- ) LftParaX = LftParaX*6+LftPara[k];
            LftParaX = LftParaX*56 + s2;
            SetInfo();
            RX=2;
        }
        private void Btn4578CLear_Click( object sender, RoutedEventArgs e ) {
            for( int r=3; r<9; r++ ){
                for( int c=3; c<9; c++ ) LS[r,c]=0;
            }
            ReDraw();
            RX=2;
        }
        private void SetInfo(){
            LSG.GeneratePara( TopParaX, LftParaX, ref LS, ref TopPara, ref LftPara );
            LblB12.Content  = (TopParaX%56).ToString();
            LblB2R1.Content = TopPara[2].ToString();
            LblB3R1.Content = TopPara[3].ToString();
            LblB2R2.Content = TopPara[4].ToString();
            LblB2R3.Content = TopPara[5].ToString();
            LblB3R2.Content = TopPara[6].ToString();
            LblB3R3.Content = TopPara[7].ToString();
            
            LblB47.Content= (LftParaX%56).ToString();
            LblB4C1.Content= LftPara[2].ToString();
            LblB7C1.Content= LftPara[3].ToString();
            LblB4C2.Content= LftPara[4].ToString();
            LblB7C2.Content= LftPara[5].ToString();
            LblB4C3.Content= LftPara[6].ToString();
            LblB7C3.Content= LftPara[7].ToString();

            ReDraw();

            nPattern=0; RX=2;
            while( GenerateLatinSquare() ) nPattern++;
            LblB4578.Content = nPattern+" cases";
        }

        private Permutation[] prmLst=new Permutation[9];
        private int   RX;
        private int   ID;
        private int[] URow;
        private int[] UCol;
        private void BtnB5689_Click( object sender, RoutedEventArgs e ) {
            if( GenerateLatinSquare() ){
                ReDraw(); 
                LblB4578.Content =  (ID++).ToString()+" / "+ nPattern; }
            else{ LblB4578.Content = "Generation end"; }
        }
        
        public bool GenerateLatinSquare( ){
            if( RX<3 ){
                URow=new int[9]; UCol=new int[9];
                for( int r=0; r<3; r++ ){
                    for( int c=3; c<9; c++ ){
                        UCol[c] |= (1<<LS[r,c]);
                        URow[c] |= (1<<LS[c,r]); //(c,r reverse use in URow)
                    }
                }
                RX=3; ID=0; prmLst[RX] = null;
            }
            do{
              LNxtLevel:
                Permutation prm=prmLst[RX];
                if( prm==null ) prmLst[RX]=prm=new Permutation(9,6);
                
                int[] UCo2 = new int[9];
                int[] UBlk = new int[9];
                for( int c=3; c<9; c++ ) UCo2[c]=UCol[c];
                for( int r=3; r<RX; r++ ){
                    for( int c=3; c<9; c++ ){
                        int no=LS[r,c];
                        UCo2[c] |= (1<<no);
                        UBlk[r/3*3+c/3] |= (1<<no);
                    }
                }
                int nxtX=9;
                while( prm.Successor(nxtX) ){
                    for( int cx=3; cx<9; cx++ ){
                        nxtX=cx-3;
                        int no=prm.Pnum[nxtX]+1;
                        int noB = 1<<(no);
                        if( (UCo2[cx]&noB)>0 ) goto LNxtPrm;
                        if( (URow[RX]&noB)>0 ) goto LNxtPrm;
                        if( (UBlk[RX/3*3+cx/3]&noB)>0 ) goto LNxtPrm;
                        LS[RX,cx] = no;
                    }
                    if( RX==8 ){
                        //__DBUGprint(LS);
                        return true;//<>
                    }
                    prmLst[++RX]=null;
                    goto LNxtLevel;

                  LNxtPrm:
                    continue;
                }
            }while((--RX)>=3);

            return false;
        }   
        private void __DBUGprint( int[,] pSol99, string st="" ){
            string po;
            Console.WriteLine();
            for( int r=0; r<9; r++ ){
                po = st+r.ToString("##0:");
                for( int c=0; c<9; c++ ){
                    int wk = pSol99[r,c];
                    if( wk==0 ) po += " .";
                    else po += wk.ToString(" #");
                }
                Console.WriteLine(po);
            }
        }
    }
}
