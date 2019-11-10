using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using static System.Math;
using static System.Console;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;

using Microsoft.Win32;

using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;
using System.Threading.Tasks;

using GIDOO_space;
using VisualPrint;

//http://msdn.microsoft.com/en-us/library/ms750559.aspx

//WPF入門
//http://www.atmarkit.co.jp/fdotnet/chushin/introwpf_index/

//難読化 ▼▼▼　公開時は削除
//http://d.hatena.ne.jp/wwwcfe/20100513/obfuscator

//方法 : ルーティング イベントを処理する
//http://msdn.microsoft.com/ja-jp/library/ms742550.aspx

namespace GNPZ_sdk{
    public delegate void SDKEventHandler( object sender, SDKEventArgs args );
 
    public class SDKEventArgs: EventArgs{
	    public string eName;
	    public int    eCode;
        public int    ProgressPer;
        public bool   Cancelled;

	    public SDKEventArgs( string eName=null, int eCode=-1, int ProgressPer=-1
            , bool Cancelled=false ){
            try{
		        this.eName = eName;
		        this.eCode = eCode;
                this.ProgressPer = ProgressPer;
                this.Cancelled = Cancelled;
            }
            catch(Exception e ){
                WriteLine(e.Message);
                WriteLine(e.StackTrace);
            }
	    }
    }

    public partial class NuPz_Win: Window{
        private Point           _WinPosMemo;
        public  GNPXpzl         GNP00;
        public  GNPZ_Graphics   SDKGrp;           //盤面表示ビットマップの作成

        private int             WOpacityCC=0;
        private Stopwatch       AnalyzerLap;
        private DispatcherTimer startingTimer;
        private DispatcherTimer endingTimer;
        private DispatcherTimer displayTimer;
        private DispatcherTimer bruMoveTimer;
        private RenderTargetBitmap bmpGZero;
        private DevelopWin      devWin;
		private ExtendResultWin ExtResultWin;

        private GNP00_PrePrint  GPrePrnt;

        private UProblem        pGP{ get{ return GNP00.GNPX_Eng.pGP; } }

        //=============
        private List<RadioButton> patSelLst;
/*
        private System.Drawing.Printing.PrintDocument SDKprintDocument;
        private System.Windows.Forms.PageSetupDialog SDKpageSetupDialog;
        private System.Windows.Forms.PageSetupDialog pageSetupDialog1;
        private System.Drawing.Printing.PrintDocument printDocument1;
        private System.Windows.Forms.PrintDialog printDialog1;
        private System.Windows.Forms.PrintPreviewDialog SDKprintPreviewDialog;
*/
        public SDKAlarm SDKAlarmObj = new SDKAlarm();

        //WPF CHeckedListBox
        //http://www.codeproject.com/KB/WPF/WPFProblemSolving.
        //http://www.codeproject.com/KB/WPF/WPFProblemSolving.aspx
        //*************

        private DispatcherTimer     timerShortMessage;

    #region イベント
    //http://msdn.microsoft.com/ja-jp/library/system.eventargs.aspx

    public class SDKAlarm{
	    public delegate void FireEventHandler(object sender, SDKEventArgs fe );
	    public event FireEventHandler FireEvent;	
	    public void ActivateSDKAlarm( string eName, int eCode ){
		    SDKEventArgs SDKArgs = new SDKEventArgs(eName, eCode);
		    FireEvent( this, SDKArgs ); 
	    }
    }

    #endregion イベント

    #region G9 開始・終了
        public NuPz_Win(){
            GNP00  = new GNPXpzl(this);
            SDKGrp = new GNPZ_Graphics(GNP00);
           
            devWin = new DevelopWin(this);
			GroupedLinkGen.devWin = devWin;

            InitializeComponent();     
          //this.MouseLeftButtonDown += (sender, e) => this.DragMove(); //ここでは別の方法を採用

            this.Opacity = 0;

            GNPXGNPX.Content = "GNPX "+DateTime.Now.Year;

            ListBox_SuDoKuTec.ItemsSource=ListBox_SuDoKuTecLst;
           
            //パターン形式の RadioButton Controls Collection
            patSelLst = GNPZExtender.GetControlsCollection<RadioButton>(this);
            patSelLst = patSelLst.FindAll(p=>p.Name.Contains("patSel"));

          #region Timer
            AnalyzerLap = new Stopwatch();

            timerShortMessage = new DispatcherTimer(DispatcherPriority.Normal);
            timerShortMessage.Interval = TimeSpan.FromMilliseconds(50);
            timerShortMessage.Tick += new EventHandler(timerShortMessage_Tick);

            startingTimer = new DispatcherTimer( DispatcherPriority.Normal, this.Dispatcher );
            startingTimer.Interval = TimeSpan.FromMilliseconds(70);
            startingTimer.Tick += new EventHandler(startingTimer_Tick);
            this.Opacity=0.0;
            startingTimer.Start();

            endingTimer = new DispatcherTimer( DispatcherPriority.Normal, this.Dispatcher );
            endingTimer.Interval = TimeSpan.FromMilliseconds(70);
            endingTimer.Tick += new EventHandler(endingTimer_Tick);

            displayTimer = new DispatcherTimer( DispatcherPriority.Normal, this.Dispatcher );
            displayTimer.Interval = TimeSpan.FromMilliseconds(50);//50
            displayTimer.Tick += new EventHandler(displayTimer_Tick);

            bruMoveTimer = new DispatcherTimer( DispatcherPriority.Normal, this.Dispatcher );
            bruMoveTimer.Interval = TimeSpan.FromMilliseconds(20);
            bruMoveTimer.Tick += new EventHandler(bruMoveTimer_Tick);
          #endregion Timer

// /* ▼▼▼▼▼▼▼▼▼▼　公開版では削除　▼▼▼▼▼▼▼▼▼▼
            //===== 有効期限チェック =====
            DateTime ExpireDate = new DateTime(2016,9,1);
            if( !GNPZExtender.fileYMDCheckNew(ExpireDate) ){
                ToGNPXHP ToHP=new ToGNPXHP(ExpireDate);
                ToHP.Show();
            }
            bmpGZero = new RenderTargetBitmap((int)PB_GBoard.Width,(int)PB_GBoard.Height, 96,96, PixelFormats.Default);
            SDKGrp.GBoardPaint( bmpGZero, (new UProblem()).BDL, /*GNP00.crList,*/ "問題作成" );
            PB_GBoard.Source = bmpGZero;

// ▼▼▼▼▼▼▼▼▼▼　公開版では印刷機能は削除　▼▼▼▼▼▼▼▼▼▼
//#if false //印刷
            GPrePrnt = new GNP00_PrePrint();            
            //bmpGImageSource = bmpGZero.CreateBitmapSourceFromHBitmap();
/*
            SDKprintDocument = new System.Drawing.Printing.PrintDocument();
            SDKpageSetupDialog = new System.Windows.Forms.PageSetupDialog();
            pageSetupDialog1 = new System.Windows.Forms.PageSetupDialog();
            printDocument1 = new System.Drawing.Printing.PrintDocument();
            printDialog1 = new System.Windows.Forms.PrintDialog();
            SDKprintPreviewDialog = new System.Windows.Forms.PrintPreviewDialog();

            SDKprintDocument.PrintPage += new System.Drawing.Printing.PrintPageEventHandler(this.SDKprintDocument_PrintPage);
            SDKpageSetupDialog.Document = this.SDKprintDocument;
            pageSetupDialog1.Document = this.printDocument1;
            printDialog1.Document = this.SDKprintDocument;
            printDialog1.UseEXDialog = true;
            SDKprintPreviewDialog.AutoScrollMargin = new System.Drawing.Size(0, 0);
            SDKprintPreviewDialog.AutoScrollMinSize = new System.Drawing.Size(0, 0);
            SDKprintPreviewDialog.ClientSize = new System.Drawing.Size(400, 300);
            SDKprintPreviewDialog.Document = this.SDKprintDocument;
            SDKprintPreviewDialog.Enabled = true;
            SDKprintPreviewDialog.Name = "SDKprintPreviewDialog";
            SDKprintPreviewDialog.Visible = false;
*/
//#endif //印刷

            string endl = "\r";
            string st  = "===== 著作権・免責 =====" + endl;
            st += "【著作権】" + endl;
            st += "本ソフトウエアと付属文書に関する著作権は、作者GNPX に帰属します。" + endl;
            st += "本ソフトウエアは著作権法及び国際著作権条約により保護されています。" + endl;
            st += "使用ユーザは本ソフトウエアに付された権利表示を除去、改変してはいけません" + endl + endl;

            st += "【配布】" + endl;
            st += "インターネット上での二次配布、紹介等は事前の承諾なしで行ってかまいません。";
            st += "バージョンアップした場合等には、情報の更新をお願いします。" + endl;
            st += "雑誌・書籍等に収録・頒布する場合には、事前に作者の承諾が必要です。" + endl + endl;
                   
            st += "【禁止事項】" + endl;
            st += "以下のことは禁止します。" + endl;
            st += "・オリジナル以外の形で、他の人に配布すること" + endl;
            st += "・第三者に対して本ソフトウエアを販売すること、" + endl;
            st += "  販売を目的とした宣伝・営業・複製を行うこと" + endl;
            st += "・第三者に対して本ソフトウエアの使用権を譲渡・再承諾すること。" + endl;
            st += "・本ソフトウエアに対してリバースエンジニアリングを行うこと" + endl;
            st += "・本承諾書、付属文書、本ソフトウエアの一部または全部を改変・除去すること" + endl + endl;

            st += "【免責事項】" + endl;
            st += "作者は、本ソフトウエアの使用または使用不能から生じるコンピュータの故障、情報の喪失、";
            st += "その他あらゆる直接的及び間接的被害に対して一切の責任を負いません。" + endl;
            txt著作権免責.Text = st;

            tabCtrlMode.Focus();
            PB_GBoard.Focus();
        }
               
    #region ShortMessage
        public void shortMessage(string st, Point pt, Color cr, int tm ){
            LbShortMes.Content = st;
            LbShortMes.Foreground = new SolidColorBrush(cr);

            if( tm==9999 ) timerShortMessage.Interval = TimeSpan.FromSeconds(5);
            else           timerShortMessage.Interval = TimeSpan.FromMilliseconds(tm);            
            timerShortMessage.Start();
            LbShortMes.Visibility = Visibility.Visible;
        }
        private void timerShortMessage_Tick( object sender, EventArgs e ){
            LbShortMes.Visibility = Visibility.Hidden;
            timerShortMessage.Stop();
        }
    #endregion ShortMessage

        private void Window_Loaded( object sender, RoutedEventArgs e ){
            _Display_GB_GBoard( );       //メインボード設定
            _SetBitmap_PB_pattern();     //パターンスペース設定

            Lbl_onAnalyzer.Content   = "";
            Lbl_onAnalyzerM.Content  = "";
            Lbl_onAnalyzerTS.Content = "";
            Lbl_onAnalyzerTSM.Content = "";
            
            //===== 解法リスト設定 =====           
            GMethod00A.ItemsSource = GNP00.GetMethodListFromFile();
            NiceLoopMax.Value = GNPXpzl.GMthdOption["NiceLoopMax"].ToInt();
            ALSSizeMax.Value  = GNPXpzl.GMthdOption["ALSSizeMax"].ToInt();
            method_NLCell.IsChecked   = (GNPXpzl.GMthdOption["Cell"]!="0");
            method_NLGCells.IsChecked = (GNPXpzl.GMthdOption["GroupedCells"]!="0");
            method_NLALS.IsChecked    = (GNPXpzl.GMthdOption["ALS"]!="0");

			string po=(string)GNPXpzl.GMthdOption["ForceLx"];
			switch(po){
				case "ForceL0": ForceL0.IsChecked=true; break;
				case "ForceL1": ForceL1.IsChecked=true; break;
				case "ForceL2": ForceL2.IsChecked=true; break;
			}

            WOpacityCC=0;
            startingTimer.Start();

            _WinPosMemo = new Point(this.Left,this.Top+this.Height);
        }
        private void appExit_Click( object sender, RoutedEventArgs e ){
            _Get_GNPXOptionPara();
            GNP00.MethodListOutPut();

            WOpacityCC=0;
            endingTimer.IsEnabled = true;
            endingTimer.Start();
        }
        private void GNPXGNPX_MouseDoubleClick( object sender, MouseButtonEventArgs e ){
            if(devWin==null) devWin=new DevelopWin(this);
            devWin.Show();
            devWin.Set_dev_GBoard(GNP00.pGP.BDL);
        }
        private void Window_MouseDown( object sender, MouseButtonEventArgs e ){

/* ▼▼▼▼▼▼▼▼▼▼　公開版では削除　▼▼▼▼▼▼▼▼▼▼
            //===== 有効期限チェック =====
            if( !GNPZExtender.fileYMDCheckNew(new DateTime(2015,1,1)) ){
                WOpacityCC = 0;
                endingTimer.Start();
            }
*/

            if( e.Inner(PB_GBoard) )    return; 
            if( e.Inner(tabCtrlMode) )  return;
            this.DragMove();
        }
        private void startingTimer_Tick( object sender, EventArgs e){
            WOpacityCC++;
            if( WOpacityCC >= 25 ){ this.Opacity=1.0; startingTimer.Stop(); }
            else this.Opacity=WOpacityCC/25.0;
        }
        private void endingTimer_Tick( object sender, EventArgs e){
            if( (++WOpacityCC)>10 )  Environment.Exit(0);   //Application.Exit();
            double dt = 1.0-WOpacityCC/12.0;
            this.Opacity = dt*dt;
        }
        
        private void bruMoveTimer_Tick( object sender, EventArgs e){
            Thickness X=PB_GBoard.Margin;
            PB_GBoard.Margin=new Thickness(X.Left-2,X.Top-2,X.Right,X.Bottom);
            bruMoveTimer.Stop();
        }

		private void GNPXwin_LocationChanged( object sender,EventArgs e ){			//開いているWindowを同期移動
            foreach( Window w in Application.Current.Windows ){
              //if( w.Owner!=this && w!=this && w.Visibility==Visibility.Visible ){
                if( w.Owner!=this && w!=this ){
                    w.Topmost = true;
                    __GNPXwin_LocationChanged(w);
                }
            }
            _WinPosMemo = new Point(this.Left,this.Top);
		}
		private void __GNPXwin_LocationChanged( Window _win ){
            if( _win==null )  return;
            _win.Left = this.Left-_WinPosMemo.X+_win.Left;
            _win.Top  = this.Top -_WinPosMemo.Y+_win.Top ;
        }	
    #endregion 開始・終了

    #region G9 手法選択
        private void GMethod02_Click( object sender, RoutedEventArgs e ){
            GMethod00A.ItemsSource = null;
            GMethod00A.ItemsSource = GNP00.ResetMethodList();
            GNP00.MethodListOutPut();
            GNP00.GNPX_Eng.Set_MethodLst_Run(false);
        }
        private void GMethod01U_Click( object sender, RoutedEventArgs e ){
            int nx = GMethod00A.SelectedIndex;
            if( nx< 0 || nx==0 )  return;
            GMethod00A.ItemsSource = null;
            GMethod00A.ItemsSource = GNP00.ChangeMethodList(nx,-1);
            GMethod00A.SelectedIndex = nx-1;
            GNP00.MethodListOutPut();
            GNP00.GNPX_Eng.Set_MethodLst_Run(false);
        }
        private void GMethod01D_Click( object sender, RoutedEventArgs e ){
            int nx = GMethod00A.SelectedIndex;
            if( nx<0 || nx==GMethod00A.Items.Count-1 )  return;
            GMethod00A.ItemsSource = null;
            GMethod00A.ItemsSource = GNP00.ChangeMethodList(nx,1);
            GMethod00A.SelectedIndex = nx+1;
            GNP00.MethodListOutPut();
            GNP00.GNPX_Eng.Set_MethodLst_Run(false);
        }
    #endregion 手法選択
 
    #region ファイルIO
        private string    fNameSDK;
        private void btnFileInputQ_Click( object sender, RoutedEventArgs e ){
            var OpenFDlog = new OpenFileDialog();
            OpenFDlog.Multiselect = false;
            OpenFDlog.Title  = "問題ファイル";
            OpenFDlog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            if( (bool)OpenFDlog.ShowDialog() ){
                fNameSDK = OpenFDlog.FileName;
                GNP00.SDK_FileInput( fNameSDK, (bool)cbxProbInitialSet.IsChecked );
                txtFileName.Text = fNameSDK;

                _SetScreenProblem();
                GNP00._SDK_Ctrl_Initialize();

                btnProbPre.IsEnabled = (GNP00.CurrentPrbNo>=1);
                btnProbNxt.IsEnabled = (GNP00.CurrentPrbNo<GNP00.SDKProbLst.Count-1);
            }
        }
        private void btnFileOutputQ_Click( object sender, RoutedEventArgs e ){
            var SaveFDlog = new SaveFileDialog();
            SaveFDlog.Title = "問題ファイルのセーブ";
            SaveFDlog.FileName = fNameSDK;
            SaveFDlog.Filter = "テキストファイル(*.txt)|*.txt|全てのファイル(*.*)|*.*";
            
            GNPXpzl.SlvMtdCList[0] = true;
            if( !(bool)SaveFDlog.ShowDialog() ) return;
            fNameSDK = SaveFDlog.FileName;
            bool append  = (bool)cbxAppend.IsChecked;
            bool fType81 = (bool)cbxFile81Nocsv.IsChecked;
            bool SolSort = (bool)cbxSolutionSort.IsChecked;
            bool SolSet  = (bool)cbxProbSolSetOutput.IsChecked;
            bool SolSet2 = (bool)cbxProbSolSetOutput2.IsChecked;

            if( GNP00.SDKProbLst.Count==0 ){
                if( pGP.BDL.All(p=>p.No==0) ) return;
                pGP.ID = GNP00.SDKProbLst.Count;
                GNP00.SDKProbLst.Add(pGP);
                GNP00.CurrentPrbNo=0;
                _SetScreenProblem();
            }
            GNP00.GNPX_Eng.Set_MethodLst_Run(true);  //true:全手法を用いる  
            GNP00.SDK_FileOutput( fNameSDK, append, fType81, SolSort, SolSet, SolSet2 );
        }
        private void btnFavfileOutputQ_Click( object sender, RoutedEventArgs e ){
            GNP00.btnFavfileOutput(true,SolSet:true,SolSet2:true);
        }

        private void cbxProbSolSetOutput_Checked( object sender, RoutedEventArgs e ){
            cbxProbSolSetOutput2.IsEnabled = (bool)cbxProbSolSetOutput.IsChecked;
            Color cr = cbxProbSolSetOutput2.IsEnabled? Colors.White: Colors.Gray;
            cbxProbSolSetOutput2.Foreground = new SolidColorBrush(cr); 
        }

       //問題のcopy/paste (board<-->clipboard)
        private void Grid_PreviewKeyDown( object sender, KeyEventArgs e ){
            bool KeySft  = (Keyboard.Modifiers&ModifierKeys.Shift)>0;
            bool KeyCtrl = (Keyboard.Modifiers&ModifierKeys.Control)>0;

            if( e.Key==Key.C && KeyCtrl ){
                string st=pGP.CopyToBuffer();
                Clipboard.Clear();
                Clipboard.SetData(DataFormats.Text, st);
            }
            else if( e.Key==Key.F && KeyCtrl ){
                string st=pGP.ToGridString(KeySft);   
                Clipboard.Clear();
                Clipboard.SetData(DataFormats.Text, st);
            }
            else if( e.Key==Key.V && KeyCtrl ){
                string st=(string)Clipboard.GetData(DataFormats.Text);
                if( st==null || st.Length<81 ) return ;
                var UP=GNP00.SDK_ToUProblem(st,saveF:true); 
                if( UP==null) return;
                GNP00.CurrentPrbNo=999999999;//GNP00.SDKProbLst.Count-1
//xx            GNP00.ResetBuffer();
                _SetScreenProblem();
                _ResetAnalizer(false); //解析結果クリア

            }
/*  ▼検討作業▼
            else if( e.Key==Key.S && KeyCtrl ){
                string st=pGP.ToGridString(KeySft);   
                using( StreamWriter sf=new StreamWriter("検討用.csv",true,Encoding.Default) ){
                    sf.WriteLine(st);
                }
            }
*/
        }
    #endregion ファイルIO
        
    #region  動作モード 
        private void tabCtrlMode_SelectionChanged( object sender, SelectionChangedEventArgs e ){
            if( (TabControl)sender!=tabCtrlMode ) return;
            TabItem tb=tabCtrlMode.SelectedItem as TabItem;
            if( tb==null || tb.Name.Substring(0,4)!="tabA" )  return;
            GNP00.GSmode = (string)tb.Header;    //Tab名 -> 動作モード

            switch(GNP00.GSmode){
                case "解析": sNoAssist = (bool)chbAnalyze00.IsChecked; break;
                case "問題作成":
                    TabItem tb2=tabAutoManual.SelectedItem as TabItem;
                    if( tb2==null )  return ;
                    if( (string)tb2.Header=="自動" )  sNoAssist=false;
                    else sNoAssist = (bool)chbAssist01.IsChecked;
                    gridExhaustiveSearch.Visibility=
                        (int.Parse(GenLStyp.Text)==2)? Visibility.Visible: Visibility.Hidden;
                    break;

                case "オプション":
                    bool sAssist=true;
                    chbOpsAssist.IsChecked=sAssist;
                    sNoAssist=sAssist;
                    break;

                default: sNoAssist=false; break;
            }

            _Display_GB_GBoard();   
            tabSolver_SelectionChanged(sender,e);
        }
        
        private string _PreSelTabName="";
        private void tabSolver_SelectionChanged( object sender, SelectionChangedEventArgs e ){
            TabItem tabItm = tabSolver.SelectedItem as TabItem;
            _Get_GNPXOptionPara();
            if( tabItm!=null ){
                if( tabItm.Name!="" &&  _PreSelTabName=="tabSelMethodSel" ){     
                    GNP00.MethodListOutPut();
                }
                _PreSelTabName = tabItm.Name;  
                SDK_Ctrl.MltAnsSearch = (tabItm.Name=="TC_SDK_MltiAnalyzer"); //複数解解析"
            }    
        }     
        
    #endregion  動作モード Tab

    #region 問題の選択、メインボード設定
        private void btnProbPre_Click( object sender, RoutedEventArgs e ){ _Get_PreNxtPrg(-1); }
        private void btnProbNxt_Click( object sender, RoutedEventArgs e ){ _Get_PreNxtPrg(+1); }
        private void _Get_PreNxtPrg( int pm ){
            int nn=GNP00.CurrentPrbNo +pm;
            if( nn<0 || nn>GNP00.SDKProbLst.Count-1 ) return;
//zz        GNP00.ResetBuffer();

            GNP00.CurrentPrbNo = nn;
            GNP00.GNPX_Eng.AnMan.ResetAnalysisResult(); //解析結果のみクリア
            GNP00.GNPX_Eng.AnMan.Set_CellFreeB();
            GNP00.GNPX_Eng.AnalyzerCounterReset();
            SDK_Ctrl.UGPMan=null;                       //複数解探索初期化
            _DGViewMethodCounterSet();
            _SetScreenProblem();
        
            AnalyzerCC = 0;
            btnAnalyzerCC.Content = "";
            lblAnalyzerResult.Text = "";

            Lbl_onAnalyzerTS.Content = "";
            Lbl_onAnalyzerTSM.Content = "";

            btnAnalyzerCCM.Content = "";
            lblAnalyzerResultM.Text = "";
            ListBox_SuDoKuTecLst.Clear();
            ListBox_SuDoKuTec.ItemsSource=null;
        }

        private void _SetScreenProblem( ){
            UProblem P = GNP00.GetCurrentProble( );
            _Display_GB_GBoard();
            if( P!=null ){
                txtProbNo.Text = (P.ID+1).ToString();
                txtProbName.Text = P.Name;
                nUDDifficultyLevel.Text = P.DifLevel.ToString();
            
                int nP=0, nZ=0, nM=0, nn=P.ID;
                __Set_CellsPZMCount( ref nP, ref nZ, ref nM );

                btnProbPre.IsEnabled = (nn>0);
                btnProbNxt.IsEnabled = (nn<GNP00.SDKProbLst.Count-1);
                       
                _DGViewMethodCounterSet();
            }
        }     
    #endregion 問題の選択

    #region パターンの選択、パターン設定
        private void btnPatternAutoGen_Click( object sender, RoutedEventArgs e){
            GNP00.SDKCntrl.CellNumMax = (int)CellNumMax.Value;
            _GeneratePatternl(true);
            SDK_Ctrl.rxCTRL = 0;           //問題候補生成の初期化
        }
        private void btnPatClear_Click( object sender, RoutedEventArgs e ){
            GNP00.SDKCntrl.PatGen.GPat = new int[9,9];
            _SetBitmap_PB_pattern();  
        }     
        private void PB_pattern_MouseDown( object sender, MouseButtonEventArgs e ){
            _GeneratePatternl(false);
        }
        private void btnPatternImport_Click( object sender, RoutedEventArgs e ){
            int nn=GNP00.SDKCntrl.PatGen.patternImport( pGP );
            labelPattern.Content = "問題セル数：" + nn.ToString();
            _SetBitmap_PB_pattern();
            SDK_Ctrl.rxCTRL = 0;           //問題候補生成の初期化
        }

        private void _GeneratePatternl( bool ModeAuto ){  /*G9*/        
            int patSel = patSelLst.Find(p=>(bool)p.IsChecked).Name.Substring(6,2).ToInt(); //パターン形
            int nn=0;
            if( ModeAuto ) nn=GNP00.SDKCntrl.PatGen.patternAutoMaker(patSel);
            else{
                Point pt=Mouse.GetPosition(PB_pattern);
                int row=0, col=0;
                if( __GetRCPositionFromPattern( pt,ref row,ref col) ){
                    nn=GNP00.SDKCntrl.PatGen.symmetricPattern(patSel,row,col,false);
                }
            }
            SDK_Ctrl.rxCTRL = 0;           //問題候補生成の初期化
            _SetBitmap_PB_pattern();
            labelPattern.Content = "問題セル数："+nn;
        }    
        private bool __GetRCPositionFromPattern( Point pt, ref int row, ref int col ){
            int selSizeHf = GNP00.cellSizeP/2 + 1;

            row=col=-1;
            int rn = (int)(pt.Y-GNP00.lineWidth);
            rn = rn-rn/(selSizeHf*3)*2;
            row = (rn/selSizeHf);

            int cn = (int)(pt.X-GNP00.lineWidth);
            cn = cn-cn/(selSizeHf*3)*2;
            col = cn/selSizeHf;

            if( row<0 || row>=9 || col<0 || col>=9 ) return false;
            return true;
        }
        private void _SetBitmap_PB_pattern( ){  //パターンスペース設定
            SDKGrp.GBPatternPaint( PB_pattern, GNP00.SDKCntrl.PatGen.GPat );
        }
    #endregion パターンの選択

    #region 表示
        private bool sNoAssist=false;
        private void _Display_GB_GBoard( bool DevelopB=false ){  //メインボード設定
            UProblem P = pGP;

            Lbl_onAnalyzer.Visibility = (GNP00.GSmode=="解析")? Visibility.Visible: Visibility.Hidden; 
            Lbl_onAnalyzerM.Visibility = Visibility.Visible; 
      
            SDKGrp.GBoardPaint( bmpGZero, P.BDL, GNP00.GSmode, sNoAssist );
            PB_GBoard.Source = bmpGZero;

            int nP=0, nZ=0, nM=0;
            __Set_CellsPZMCount( ref nP, ref nZ, ref nM );
            txtProbNo.Text = (P.ID+1).ToString();
            txtProbName.Text = P.Name;
            nUDDifficultyLevel.Text = P.DifLevel.ToString();

            if(DevelopB) _Display_Develop();
			if(GNP00.GSmode=="解析")  _Display_ExtResultWin();		
        }
        private void _Display_Develop(){
            int[] TrPara=pGP.PTrans.TrPara;
            LblRg.Content    = TrPara[0].ToString();      
            LblR123g.Content = TrPara[1].ToString();
            LblR456g.Content = TrPara[2].ToString();
            LblR789g.Content = TrPara[3].ToString();

            LblCg.Content    = TrPara[4].ToString();
            LblC123g.Content = TrPara[5].ToString();
            LblC456g.Content = TrPara[6].ToString();
            LblC789g.Content = TrPara[7].ToString(); 
            LblRC7g.Content  = TrPara[8].ToString();
        }

		private void _Display_ExtResultWin(){
			if( pGP.extRes==null || pGP.extRes.Length<5 ){
				if(ExtResultWin!=null && ExtResultWin.Visibility==Visibility.Visible ){
					ExtResultWin.Visibility=Visibility.Hidden;
				}
				return;
			}
				//WriteLine( "_Display_ExtResultWin" );
			if(ExtResultWin==null) {
				ExtResultWin=new ExtendResultWin(this);
				ExtResultWin.Width = this.Width;
				ExtResultWin.Left  = this.Left;
				ExtResultWin.Top   = this.Top+this.Height;
			}
			ExtResultWin.SetText(pGP.extRes);
			ExtResultWin.Show();
		}	

        private void chbOpsAssist_Checked( object sender, RoutedEventArgs e ){
            sNoAssist = (bool)chbOpsAssist.IsChecked;

            int nP=0, nZ=0, nM=0;
            if( __Set_CellsPZMCount(ref nP,ref nZ,ref nM) ) _Display_GB_GBoard( );
        }

        private bool __Set_CellsPZMCount( ref int nP, ref int nZ, ref int nM ){
            nP=nZ=nM=0;
            if( GNP00.GNPX_Eng==null )  return false;
            bool sAssist=GNP00.GNPX_Eng.AnMan.AggregateCellsPZM(ref nP, ref nZ, ref nM);
            if( nP+nZ+nM>0 ){
                lblStepCounter.Content = "セル数 問:" + nP.ToString() +
                    "  解:" + nM.ToString("0#") + "  残:" + nZ.ToString("0#");
            }
            return ((nP+nM>0)&sAssist);
        }
        private void chbAnalyze00_Checked( object sender, RoutedEventArgs e ){
            if( bmpGZero==null )  return;
            sNoAssist = (bool)chbAnalyze00.IsChecked;
            _SetScreenProblem();
        }
        private void chbAssist01_Checked( object sender, RoutedEventArgs e ){
            sNoAssist = (bool)chbAssist01.IsChecked;
            _Display_GB_GBoard();　//(空き数字を表示)
        }

        private int    __GCCounter__=0;
        private int    _ProgressPer;
        private string __DispMode=null;

        private void displayTimer_Tick( object sender, EventArgs e ){
            _Display_GB_GBoard();

            //WriteLine("displayTimer_Tick");
            
            switch(GNP00.GSmode){
                case "問題作成": _Display_CreateProblem(); break;

                case "複数解解析":
                case "解析":     _Display_AnalyzeProb(); break;
            }

            ResMemory.Content = "Memory:" + GC.GetTotalMemory( true ).ToString();            
            if( ((++__GCCounter__)%1000)==0 ){ GC.Collect(); __GCCounter__=0; }
        }
        private void _Display_CreateProblem( ){
            Mlttrial.Content = "試行回数：" + GNP00.SDKCntrl.LoopCC;
            MlttrialT.Content = "(累積:" + SDK_Ctrl.TLoopCC + ")";
            LSPattern.Content = "基本パターン：" + GNP00.SDKCntrl.PatternCC;
            gamGen05A.Content = "残り" + (_ProgressPer.ToString().PadLeft( 2 )) + " 問";

            UProblem pGP=GNP00.pGP;
            if( pGP!=null ){
                int nn=GNP00.SDKProbLst.Count;
                if( nn>0 ){
                    txtProbNo.Text = nn.ToString();
                    txtProbName.Text = GNP00.SDKProbLst.Last().Name;
                    nUDDifficultyLevel.Text = pGP.DifLevel.ToString();
                }
            }

            TimeSpan ts = AnalyzerLap.Elapsed;
            string st = "";
            if( ts.TotalSeconds>1.0 ) st += ts.TotalSeconds.ToString( " 0.0" ) + " sec";
            else                      st += ts.TotalMilliseconds.ToString( " 0.0" ) + " msec";

            Lbl_onAnalyzerTS.Content = st;
            Lbl_onAnalyzerTSM.Content = st;
            Lbl_onAnalyzerTS3.Content = "経過時間：" + st;

            if( __DispMode!=null && __DispMode!="" ){
                _SetScreenProblem();
                if( __DispMode=="Canceled" )  Mlttrial.Content += " キャンセル";
                displayTimer.Stop();
                AnalyzerLap.Stop();
                btnP13MltStart.Content = "問題作成";
            }
            __DispMode="";
        }
        private void _Display_AnalyzeProb(){
            bool SearcEnd=false;
            if( __DispMode=="Canceled" ){
                Lbl_onAnalyzer.Foreground = Brushes.LightCoral; 
                Lbl_onAnalyzerM.Foreground = Brushes.LightCoral; 
//              SDK_Ctrl.FilePut_GenPrb = false;
//				Thread.Sleep(10);
                displayTimer.Stop();
                __DispMode="";
            }
            
            else if( __DispMode=="Complated" ){
                Lbl_onAnalyzer.Content = "解析完了";
                    SearcEnd=true;
                    if( (string)SDK_Ctrl.MltAnsOption["打切り結果"]!="" ){
                        Lbl_onAnalyzerM.Content = SDK_Ctrl.MltAnsOption["打切り結果"];
                    }
                    else{
                        Lbl_onAnalyzerM.Content = "解析完了";
                        Lbl_onAnalyzerM.Foreground = Brushes.LightBlue;  

						if( (bool)cbxFileDifficultyLevel.IsChecked ){
							string prbMessage;
							int DifLevel = GNP00.GNPX_Eng.GetDifficultyLevel( out prbMessage );
							pGP.DifLevel = DifLevel;
							nUDDifficultyLevel.Text = DifLevel.ToString();
						}
                    }

                /*
                    if( SDK_Ctrl.MltAnsOption["打切り結果"]==1 ) Lbl_onAnalyzerM.Content = "探索数上限打切り";
                    if( SDK_Ctrl.MltAnsOption["打切り結果"]==2 ) Lbl_onAnalyzerM.Content = "探索時間上限打切り";
                    if( SDK_Ctrl.MltAnsOption["打切り結果"]>0  ) Lbl_onAnalyzerM.Foreground = Brushes.Orange;
                */ 

                    btnMltAnlzSearch.Content = "複数解探索";
                    btnMltAnlzSearch.IsEnabled = true;
                Lbl_onAnalyzer.Foreground = Brushes.LightBlue;   
 
                _DGViewMethodCounterSet();  //手法の集計
                string msgST = pGP.GNPX_ResultLong;
                if(!ErrorStopB) lblAnalyzerResult.Text = msgST;
                if( msgST.LastIndexOf("ルール違反")>=0 || msgST.LastIndexOf("解析不能")>=0 ){ }
                displayTimer.Stop();
                __DispMode="";
            }
            else{
                if(!ErrorStopB)  lblAnalyzerResult.Text = GNPZ_Engin.GNPX_AnalyzerMessage;
                Lbl_onAnalyzerM.Content = "解析中："+GNPZ_Engin.GNPX_AnalyzerMessage;
            }        

            lblAnalyzerResultM.Text="["+(pGP.IDm+1)+"] "+pGP.GNPX_ResultLong;        
            ListBox_SuDoKuTec.ItemsSource=null;

            if( SDK_Ctrl.MltAnsSearch && SDK_Ctrl.UGPMan!=null && SDK_Ctrl.UGPMan.MltUProbLst!=null ){
                List<UProblem> pMltUProbLst=SDK_Ctrl.UGPMan.MltUProbLst;
                MAnalizeBtnSet();

                try{
                    if( pMltUProbLst!=null && pMltUProbLst.Count>0 ){
                        ListBox_SuDoKuTecLst.Clear();
 
                        int sq=0;
                        pMltUProbLst.ForEach(P=> ListBox_SuDoKuTecLst.Add(new MltTec(P,++sq)) );
                        ListBox_SuDoKuTec.ItemsSource = ListBox_SuDoKuTecLst;
                        if(!SearcEnd && __DispMode=="Complated"){
                            ListBox_SuDoKuTec.ScrollIntoView(ListBox_SuDoKuTecLst.Last());
                        }
                        if(SearcEnd)  ListBox_SuDoKuTec.ScrollIntoView(ListBox_SuDoKuTecLst.First());
            /*
                        int selX=0;
                        if( SDK_Ctrl.UGPMan!=null && SDK_Ctrl.UGPMan.pGPsel!=null )  selX=SDK_Ctrl.UGPMan.pGPsel.IDm;
                        if( selX>=0 && selX<pMltUProbLst.Count ){
                            UProblem pGPM=pMltUProbLst[selX];
                            SDK_Ctrl.UGPMan.pGPsel=pGPM;
                            GNP00.GNPX_Eng.pGP=pGPM;
                            lblAnalyzerResultM.Text ="["+(pGPM.IDm+1)+"] "+pGPM.GNPX_ResultLong; //■
                        }
            */
                     }
                }
                catch( Exception e ){
                    WriteLine(e.Message);
                    WriteLine(e.StackTrace);
                }
            }

            string st="";   
            TimeSpan ts2 = GNPZ_Engin.SdkExecTime;
            TimeSpan ts = AnalyzerLap.Elapsed;
            if( ts.TotalSeconds>1.0 )  st=ts.TotalSeconds.ToString("0.000")+" sec";
            else                       st=ts.TotalMilliseconds.ToString("0.000")+" msec";

            Lbl_onAnalyzerTS.Content  = st;
            Lbl_onAnalyzerTSM.Content  = st;
            Lbl_onAnalyzerTS3.Content = "経過時間："+st;
                        
            btnSDKAnalyzer.Content     = "解　析";
            btnMltAnlzSearch.Content   = "複数解探索";
            btnSDKAnalyzerAuto.Content = "自動解析";

            if( GNPZ_Engin.GNPX_AnalyzerMessage.Contains("sys") ){
                lblAnalyzerResultM.Text = GNPZ_Engin.GNPX_AnalyzerMessage;
            }

            this.Cursor = Cursors.Arrow;

            _DGViewMethodCounterSet();
            _SetScreenProblem();
 
            OnWork = 0;
        }
    #endregion 表示

    #region マウスＩＦ
        //***** 制御変数
        private int     noPChg = -1;
        private int[]   noPChgList = new int[9];
        private int     rowMemo; 
        private int     colMemo;
        private int     noPMemo;
        private bool    mouseFlag = false;

        private void PB_GBoard_MouseLeftButtonDown( object sender, MouseButtonEventArgs e ){  
            if( mouseFlag ) return;
            if( GNP00.GSmode!="問題作成" && GNP00.GSmode!="数字変更" )  return;

            int r, c;
            int noP = _Get_PB_GBoardRCNum( out r, out c );
            if( noP<=0 ){
                GnumericPad.Visibility = Visibility.Hidden;
                rowMemo=-1; colMemo=-1;
                return;
            }
            rowMemo=r; colMemo=c;       
            mouseFlag = true;
            if( GNP00.GSmode=="数字変更" ) return;

            if( GNP00.GSmode!="問題作成" ){
                if( pGP.BDL[r*9+c].No > 0 ) return;
            }

            rowMemo=r; colMemo=c; noPMemo=noP;
            _GNumericPadManager( r, c, noP );  
        }
        private void PB_GBoard_MouseLeftButtonUp( object sender, MouseButtonEventArgs e ){
            if( !mouseFlag ) return;
            mouseFlag = false;

            int noP=0;
            if( GNP00.GSmode=="数字変更" ){ _Change_PB_GBoardNum( ref noP ); return; }
        }

        private void _GNumericPadManager( int r, int c, int noP ){
            noPMemo = noP;
            int FreeB=0x1FF;
            if( GNP00.GSmode=="問題作成"  ){
                FreeB = pGP.BDL[r*9+c].FreeB;   //1:選択可能な数字
            }

            GnumericPad.Source = SDKGrp.CreateCellImageLight( pGP.BDL[r*9+c], noP );
 
            int PosX = (int)PB_GBoard.Margin.Left + 2 + 37*c + (int)c/3;
            int PosY = (int)PB_GBoard.Margin.Top  + 2 + 37*r + (int)r/3;        
            GnumericPad.Margin = new Thickness(PosX, PosY, 0,0 );        
            GnumericPad.Visibility = Visibility.Visible;
            
        }       
        private void GnumericPad_MouseMove( object sender, MouseEventArgs e ){
            if( !mouseFlag ) return;
            int r, c;
             
            if( GNP00.GSmode=="数字変更" ) return;
            int noP  = _Get_PB_GBoardRCNum( out r, out c );
            if( noP<=0 || r!=rowMemo || c!=colMemo ){
                GnumericPad.Visibility = Visibility.Hidden;
                rowMemo=-1; colMemo=-1;
                return;
            }

            if( GNP00.GSmode!="問題作成" ){
                if( pGP.BDL[r*9+c].No > 0) return;
            }

            if( r!=rowMemo || c!=colMemo ){
                GnumericPad.Visibility = Visibility.Hidden;
                rowMemo=-1; colMemo=-1;
                return;
            }

            if( noP!=noPMemo ){
                rowMemo = r;
                colMemo = c;
                noPMemo = noP;
                _GNumericPadManager( r, c, noP );
            }
        }        
        private void GnumericPad_MouseUp( object sender, MouseButtonEventArgs e ){
            if( !mouseFlag ) return;
            mouseFlag = false;
            int r, c;
            int noP = _Get_PB_GBoardRCNum( out r, out c );

            if( r!=rowMemo || c!=colMemo ) return;
            /*
            if( GNP00.GSmode=="数字変更" ){ _Change_PB_GBoardNum( ref noP ); return; }
            //if( noP <= 0 ){ justNum = -1;  return; }
            */
            UCell BDX = pGP.BDL[rowMemo*9+colMemo];

            int numAbs = Abs(BDX.No);
            if( numAbs==noP ){ BDX.No=0; goto MouseUpFinary; }

            int FreeB = BDX.FreeB;   //1:選択可能な数字
            if( GNP00.GSmode=="問題作成" ){
                BDX.No=0;
                GNP00.GNPX_Eng.AnMan.Set_CellFreeB();
                FreeB = BDX.FreeB;
                if( ((FreeB>>(noP-1))&1)==0 ) goto MouseUpFinary;
                BDX.No=noP;
            }
          
          MouseUpFinary:
            GNP00.GNPX_Eng.AnMan.Set_CellFreeB();
            _SetScreenProblem();
            GnumericPad.Visibility = Visibility.Hidden;
            rowMemo=-1; colMemo=-1;

            int nP=0, nZ=0, nM=0;
            __Set_CellsPZMCount( ref nP, ref nZ, ref nM );
        }
        private void cellPZMCounterForm( ref int nP, ref int nZ, ref int nM ){
            GNP00.GNPX_Eng.AnMan.AggregateCellsPZM( ref nP, ref nZ, ref nM);
            if( nP+nZ+nM > 0 ){
                lblStepCounter.Content = "セル数 問:" + nP.ToString() +
                    "  解:" + nM.ToString("0#") + "  残:" + nZ.ToString("0#");
            }
        }
        private int  _Get_PB_GBoardRCNum( out int boadRow, out int boadCol ){
            int cellSizeP = GNP00.cellSizeP;
            int cellSizeP3 = cellSizeP*3;
            int cellSizeP32 = cellSizeP3+2;
            int LWid = GNP00.lineWidth;

            boadRow = boadCol =-1;
            Point pt = Mouse.GetPosition(PB_GBoard);
            int rn = (int)pt.Y-2;
            if( rn/cellSizeP32 >= 9 )  return -1;
            rn = rn - rn/cellSizeP32*LWid;
            if( rn/cellSizeP >= 9 )  return -1;
            boadRow = rn / cellSizeP;
            rn = (rn%cellSizeP)/12;
            if( rn >= 3 )  return -1;

            int cn = (int)pt.X-2;
            if( cn/cellSizeP32 >= 9 )  return -1;
            cn = cn - cn/cellSizeP32*LWid;
            if( cn/cellSizeP >= 9 )  return -1;
            boadCol = cn / cellSizeP;
            cn = (cn%cellSizeP)/12;
            if( cn >= 3 )  return -1;


            if( boadRow<0 || boadRow>=9 || boadCol<0 || boadCol>=9) return -1;
            int noP = cn+rn*3+1;
            return noP;
        }
    #endregion マウスＩＦ 

    #region 問題作成
      #region 問題作成【マニュアル】
        private void btnBoardClear_Click( object sender, RoutedEventArgs e ){
            for( int rc=0; rc<81; rc++ ){ pGP.BDL[rc] = new UCell(rc); }
            _SetScreenProblem();　//(空き数字を表示)
        }
        private void btnNewProblem_Click( object sender, RoutedEventArgs e ){
            if( pGP.BDL.All(P=>P.No==0) ) return;
            GNP00.SDK_Save_ifNotContain();
            GNP00.CreateNewPrb();//新しい問題用の領域を確保する
            _SetScreenProblem();　//(空き数字を表示)
        }
        private void btnDeleteProblem_Click( object sender, RoutedEventArgs e ){
            GNP00.SDK_Remove();
            _SetScreenProblem();　//(空き数字を表示)
        }
        
        private void btnCopyProblem_Click( object sender, RoutedEventArgs e ){
            UProblem UPcpy= pGP.Copy(0,0);
            UPcpy.Name="コピー";
            GNP00.CreateNewPrb(UPcpy);//新しい問題用の領域を確保する
            _SetScreenProblem();　//(空き数字を表示)
        }

        #region 数字変更
        private void btnNumChange_Click( object sender, RoutedEventArgs e ){
            if( GNP00.GSmode!="数字変更" ){
                GNP00.GSmode = "数字変更";
                TransSolverA("NumChange",true); //常に解表示

                txNumChange.Text = "1";
                txNumChange.Visibility = Visibility.Visible;
                btnNumChangeFix.Visibility = Visibility.Visible;
                noPChg = 1;
                for( int k=0; k<9; k++ ) noPChgList[k] = k+1;
                mouseFlag = false;
                PB_GBoard.IsEnabled = true;
                _SetScreenProblem();　//(空き数字を表示)
                _Display_GB_GBoard();
            }
        }
        private void btnNumChangeFix_Click( object sender, RoutedEventArgs e ){
            GNP00.GSmode = "問題作成";
            txNumChange.Visibility = Visibility.Hidden;
            btnNumChangeFix.Visibility = Visibility.Hidden;
            noPChg = -1;
            TransSolverA("Checked",(bool)chbDispAns.IsChecked);
            _Display_GB_GBoard();
        }
        private void _Change_PB_GBoardNum( ref int noP ){
            int nm, nmAbs;
            if( rowMemo<0 || rowMemo>8 || colMemo<0 || colMemo>8) return;

            noP = Abs( pGP.BDL[rowMemo*9+colMemo].No );
            if( noP==0 )  return;
            if( noP!=noPChg ){

                foreach( var q in pGP.BDL ){
                    nm = q.No;
                    if( nm==0 )  continue;
                    nmAbs = Abs( nm );
                    if( noP>=noPChg ){
                        if( nmAbs < noPChg)  continue;
                        else if( nmAbs==noP ) q.No = (nm>0)? noPChg: -noPChg;
                        else if( nmAbs<noP )  q.No = (nm>0)? nmAbs+1:   -(nmAbs+1);
                    }
                    else{
                        if( nmAbs < noP ) continue;
                        else if( nmAbs==noP)        q.No = (nm>0)? noPChg: -noPChg;
                        else if( nmAbs<=noPChg ) q.No = (nm>0)? nmAbs-1:   -(nmAbs-1);
                    }
                }
            }

            _SetScreenProblem();
            noPChg++;
            txNumChange.Text = noPChg.ToString();
            if( noPChg>9 ){
                GNP00.GSmode = "問題作成";
                txNumChange.Visibility = Visibility.Hidden;
                btnNumChangeFix.Visibility = Visibility.Hidden;
                noPChg = -1;
            }
            else GNP00.GSmode = "数字変更";
            mouseFlag = false;
            return;
        }
        private void _SetGSBoad_rc_num( int r, int c, int noP ){
            if( r<0 || r>=9 ) return;
            if( c<0 || c>=9 ) return;
            int numAbs = Abs(noP);
            if( numAbs==0 || numAbs>=10) return;
            pGP.BDL[r*9+c].No=noP;
        }
        #endregion 数字変更  
 
      #endregion 問題作成【マニュアル】

      #region 問題作成【自動】
        //開始
        private Task taskSDK;
        private CancellationTokenSource tokSrc;
        private void btnP13Start_Click( object sender, RoutedEventArgs e ){
        //    int mc=GNP00.SDKCntrl.GNPX_Eng.Set_MethodLst_Run( );
        //    if( mc<=0 ) GNP00.ResetMethodList();

            if( (string)btnP13MltStart.Content=="問題作成" ){
                __DispMode=null;
                GNP00.SDKCntrl.LoopCC = 0;
                btnP13MltStart.Content  = "中  断";

                GNPZ_Engin.SolInfoDsp = false;
                if( GNP00.SDKCntrl.retNZ==0 )  GNP00.SDKCntrl.LoopCC = 0;
                GNP00.SDKCntrl.CbxDspNumRandmize = (bool)cbxDspNumRandmize.IsChecked;//数字の乱数化
                GNP00.SDKCntrl.GenLStyp = int.Parse(GenLStyp.Text);
                GNP00.SDKCntrl.CbxNextLSpattern  = (bool)ChbNextLSpattern.IsChecked;
                
                SDK_Ctrl.lvlLow = (int)gamGen01.Value;
                SDK_Ctrl.lvlHgh = (int)gamGen02.Value;
                SDK_Ctrl.FilePut_GenPrb = (bool)ChbFilePut_GenPrb.IsChecked;

                int n=gamGen05.Text.ToInt();
                n = Max(Min(n,1000),0); 
                SDK_Ctrl.MltProblem = _ProgressPer = n;
//                GNP00.MltSolSave = true;

                displayTimer.Start();
                AnalyzerLap.Start();

                tokSrc = new CancellationTokenSource();　//中断時の手続き用  
                taskSDK = new Task( ()=> GNP00.SDKCntrl.SDK_ProblemMakerReal(tokSrc.Token), tokSrc.Token );
                taskSDK.ContinueWith( t=> btnP13Start2Complated() ); //完了時の手続きを登録
                taskSDK.Start();
            }
            else{   //"中断"
                try{
                    tokSrc.Cancel();
                    taskSDK.Wait();
                    GNP00.CurrentPrbNo=999999999;//GNP00.SDKProbLst.Count;
                    _SetScreenProblem( );
                }
                catch(AggregateException){
                    __DispMode="Canceled"; 
                }
            }
            return;
        }  
        //プログレス表示
        public void BWGenPrb_ProgressChanged( object sender, SDKEventArgs e ){ _ProgressPer=e.ProgressPer; }
        //完了
        private void btnP13Start2Complated( ){ __DispMode="Complated"; }

        private void gamGen01_ValueChanged( object sender, RoutedPropertyChangedEventArgs<object> e ){
            if( gamGen02==null )  return;
            int Lval=(int)gamGen01.Value, Uval=(int)gamGen02.Value;
            if( Lval>Uval ) gamGen02.Value=Lval;
        }
        private void gamGen02_ValueChanged( object sender, RoutedPropertyChangedEventArgs<object> e ){
            if( gamGen01==null )  return;
            int Lval=(int)gamGen01.Value, Uval=(int)gamGen02.Value;
            if( Uval<Lval ) gamGen01.Value=Uval;
        }
        private void btnESnxtSucc_Click( Object sender,RoutedEventArgs e ){
            int RX=(int)UP_ESRow.Value;
            GNP00.SDKCntrl.Force_NextSuccessor(RX);
            _Display_GB_GBoard();
        }
      #endregion 問題作成【自動】

    #endregion 問題作成 

    #region 解析
      //【注意】task,ProgressChanged,Completed,CanceledのthreadSafeに注意（control操作禁止）
      #region 解析【ステップ】複数解解析
        private int OnWork = 0;

        private bool ErrorStopB;
        private void btnSDKAnalyzer_Click( object sender, RoutedEventArgs e ){
            if( OnWork==2 ) return;

            if( GNP00.AnalyzerMode==null ) GNP00.AnalyzerMode = "解析";
            if( !SDK_Ctrl.MltAnsSearch )  SDK_Ctrl.UGPMan=null;

            try{
                Lbl_onAnalyzer.Foreground = Brushes.LightGreen;
                Lbl_onAnalyzerM.Foreground = Brushes.LightGreen;
                if( (string)btnSDKAnalyzer.Content!="中　断" ){
                    int mc=GNP00.GNPX_Eng.Set_MethodLst_Run( );
                    if( mc<=0 ) GNP00.ResetMethodList();
                    Lbl_onAnalyzer.Visibility = Visibility.Visible;
                    Lbl_onAnalyzerM.Visibility = Visibility.Visible;

//                  GNPZ_Engin.SolInfoDsp = false;
                    if( GNP00.SDKCntrl.retNZ==0 )  GNP00.SDKCntrl.LoopCC=0;

                    SDK_Ctrl.MltProblem = 1;    //単独
                    SDK_Ctrl.lvlLow = 0;
                    SDK_Ctrl.lvlHgh = 999;
                    GNP00.SDKCntrl.CbxDspNumRandmize=false;
                    GNP00.SDKCntrl.GenLStyp = 1;

                    GNPXpzl.MltSolOn = (bool)MltSolOn.IsChecked;
                    GNPZ_Engin.SolInfoDsp = true;
                    AnalyzerLap.Reset();

                    if( GNP00.AnalyzerMode=="解析" || GNP00.AnalyzerMode=="複数解解析" ){
                        if(GNP00.pGP.SolCode<0)  GNP00.pGP.SolCode=0;
                        ErrorStopB = !_cellFixSub();

                        List<UCell> pBDL = pGP.BDL;
                        if( pBDL.Count(p=>p.No==0)==0 ){            //解析完了
                            _SetScreenProblem();
                            goto AnalyzerEnd;
                        }

                        OnWork = 1;    
                        btnAnalyzerCC.Content= "ステップ "+(++AnalyzerCC).ToString();
                        btnAnalyzerCCM.Content= btnAnalyzerCC.Content;
                        btnSDKAnalyzer.Content = "中　断";
                        btnMltAnlzSearch.Content = "中　断";
                        Lbl_onAnalyzer.Content = "解析中";
                        Lbl_onAnalyzerM.Content = "解析中";
                        Lbl_onAnalyzer.Foreground=Brushes.Orange;
                        Lbl_onAnalyzerM.Foreground=Brushes.Orange;
                        Lbl_onAnalyzerTS.Content = "";
                        Lbl_onAnalyzerTSM.Content = "";
                        this.Cursor = Cursors.Wait;

                        if(!ErrorStopB){
                            __DispMode="";                
                            AnalyzerLap.Start();
                            //==============================================================
                            tokSrc = new CancellationTokenSource();　//中断時の手続き用  
                            taskSDK = new Task( ()=> GNP00.SDKCntrl.AnalyzerReal(tokSrc.Token), tokSrc.Token );
                            taskSDK.ContinueWith( t=> task_SDKsolver_Completed() ); //完了時の手続きを登録
                            taskSDK.Start();
                        }
                        else{
                            __DispMode="Complated"; 
                        }
                        displayTimer.Start();    
                        //--------------------------------------------------------------         
                    }
                    else{   //"中断"
                        try{
                            tokSrc.Cancel();
                            taskSDK.Wait(); 
                        }
                        catch(AggregateException e2){
                            WriteLine(e2.Message);
                            __DispMode="Canceled";
                        }
                    }
 
                AnalyzerEnd:
                    //GNPZ_Engin.ChkPrint = false;    //*****   
                    return;
                }

            }
            catch( Exception ex ){
                WriteLine( ex.Message );
                WriteLine( ex.StackTrace );
            }

        } 
            
        public  class MltTec{
            public int    ID{ get; set; }
            public int    difL{ get; set; }
            public string tech{ get; set; }
            public bool?  IsSelected { get; set; }
            public MltTec(UProblem P, int ID ){ difL=P.DifLevel; tech=P.GNPX_Result; this.ID=ID; }
        }
        private List<MltTec> ListBox_SuDoKuTecLst=new List<MltTec>();

        private void task_SDKsolver_ProgressChanged( object sender, SDKEventArgs e ){ _ProgressPer=e.ProgressPer; }
        private void task_SDKsolver_Completed(){
            __DispMode = "Complated";
        }
        private void btnAnalyzerReset_Click( object sender, RoutedEventArgs e ){
            if( OnWork>0 ) return;
            AnalyzerCC = AnalyzerCCMemo;
            btnAnalyzerCC.Content= "ステップ "+(AnalyzerCC).ToString();
            btnAnalyzerCCM.Content=btnAnalyzerCC.Content;
            GNP00.GNPX_Eng.AnMan.ResetAnalysisResult( );    //解析結果のみクリア
            btnSDKAnalyzer.Content = "解  析";
            btnMltAnlzSearch.Content = "複数解探索";

            Lbl_onAnalyzer.Content = "";
            lblAnalyzerResult.Text = "";
            lblAnalyzerResultM.Text = "";
            Lbl_onAnalyzerTS.Content  = "";
            Lbl_onAnalyzerTSM.Content  = "";
            Lbl_onAnalyzerTS3.Content = "経過時間：";
            GNP00.GNPX_Eng.cCodePre = 0;
            GNP00.GNPX_Eng.AnMan.Set_CellFreeB();

            GNP00.GNPX_Eng.AnalyzerCounterReset();
            _DGViewMethodCounterSet();
            
            displayTimer.Stop();
            _SetScreenProblem();
        }

        private void btnMltAnlzSearch_Click( object sender, RoutedEventArgs e ){
            if( SDK_Ctrl.UGPMan==null ) SDK_Ctrl.UGPMan=new UPrbMltMan(1);
            else{
                UProblem pGPM=SDK_Ctrl.UGPMan.pGPsel;
                SDK_Ctrl.UGPMan.pGPsel=pGPM;
                GNP00.GNPX_Eng.pGP = pGPM;
                SDK_Ctrl.UGPMan.Create();
            }
            ListBox_SuDoKuTecLst.Clear();
            lblAnalyzerResultM.Text="";

            SDK_Ctrl.MltAnsOption["レベル"]       = (int)MltAnsOpt0.Value; //レベル
            SDK_Ctrl.MltAnsOption["着目同一上限"] = (int)MltAnsOpt1.Value; //着目セル同一上限
            SDK_Ctrl.MltAnsOption["複数解上限"]   = (int)MltAnsOpt2.Value; //複数解上限
            SDK_Ctrl.MltAnsOption["探索時間上限"] = (int)MltAnsOpt3.Value; //探索時間上限
            SDK_Ctrl.MltAnsOption["打切り時間"]   = DateTime.Now;
            SDK_Ctrl.MltAnsOption["打切り結果"]   = "";

            btnMltAnlzSearch.IsEnabled = false;
            btnSDKAnalyzer_Click(sender,e);
            MAnalizeBtnSet();
        }
        private void ListBox_SuDoKuTec_SelectionChanged( object sender, SelectionChangedEventArgs e ){
            if( SDK_Ctrl.UGPMan==null )   return;
            int selX=ListBox_SuDoKuTec.SelectedIndex;
            if(selX<0)  return;

            List<UProblem> pMltUProbLst=SDK_Ctrl.UGPMan.MltUProbLst;
            if( !GNPXpzl.MltSolOn || pMltUProbLst==null ) return;
            if( pMltUProbLst.Count<=0 || selX>=pMltUProbLst.Count )  return;

            UProblem pGPx=pMltUProbLst[selX];
            SDK_Ctrl.UGPMan.pGPsel=pGPx;
            if( pGP.IDm!=selX) SDK_Ctrl.UGPMan.GPMnxt=null;
            GNP00.GNPX_Eng.pGP = pGPx;

            _Display_AnalyzeProb();
        }
     
        private void MAnalizeBtnSet(){
            if(SDK_Ctrl.UGPMan==null){
                btnMTop.IsEnabled=false;
                btnMPre.IsEnabled=false;
                btnMNxt.IsEnabled=false;
            }
            else{
                btnMTop.IsEnabled=true;
                btnMPre.IsEnabled=(SDK_Ctrl.UGPMan.GPMpre!=null);
                btnMNxt.IsEnabled=(SDK_Ctrl.UGPMan.GPMnxt!=null);
            }
        }
        private void btnMPre_Click( object sender, RoutedEventArgs e ){
            if(SDK_Ctrl.UGPMan==null){ MAnalizeBtnSet(); return; }
             SDK_Ctrl.MovePre(); 

            if( SDK_Ctrl.UGPMan==null )  return;
            List<UProblem> pMltUProbLst=SDK_Ctrl.UGPMan.MltUProbLst;
            if( !GNPXpzl.MltSolOn || pMltUProbLst==null ) return;
            AnalyzerCC=SDK_Ctrl.UGPMan.stageNo;
            btnAnalyzerCC.Content= "ステップ "+AnalyzerCC;
            btnAnalyzerCCM.Content=btnAnalyzerCC.Content;

            int selX=SDK_Ctrl.UGPMan.pGPsel.IDm;
            if(selX<0)  return;
            ListBox_SuDoKuTec.SelectedIndex = selX;

            if( selX<ListBox_SuDoKuTecLst.Count ){
                ListBox_SuDoKuTec.ScrollIntoView(ListBox_SuDoKuTecLst[selX]);
            }
  
            _Display_AnalyzeProb();
        }


        private void btnMNxt_Click( object sender, RoutedEventArgs e ){
            if(SDK_Ctrl.UGPMan==null){ MAnalizeBtnSet(); return; }
            SDK_Ctrl.MoveNxt();

            if( SDK_Ctrl.UGPMan==null )  return;
            List<UProblem> pMltUProbLst=SDK_Ctrl.UGPMan.MltUProbLst;
            if( !GNPXpzl.MltSolOn || pMltUProbLst==null ) return;
            AnalyzerCC=SDK_Ctrl.UGPMan.stageNo;
            btnAnalyzerCC.Content= "ステップ "+AnalyzerCC;
            btnAnalyzerCCM.Content=btnAnalyzerCC.Content;

            int selX=SDK_Ctrl.UGPMan.pGPsel.IDm;
            if(selX<0)  return;
            ListBox_SuDoKuTec.SelectedIndex = selX;

            ListBox_SuDoKuTec.ScrollIntoView(ListBox_SuDoKuTecLst[selX]);
            ListBox_SuDoKuTecLst[selX].IsSelected=true;

            _Display_AnalyzeProb();
        }

     #endregion  解析【ステップ】複数解解析       
    
      #region 解析【全て】
        private void task_SDKsolverAuto_ProgressChanged( object sender, ProgressChangedEventArgs e ){
            Mlttrial.Content = "試行回数：" + GNP00.SDKCntrl.LoopCC;
            LSPattern.Content = "基本パターン：" + GNP00.SDKCntrl.PatternCC;
            btnSDKAnalyzerAuto.Content = "自動解析";
            OnWork=0;
        }
        private void task_SDKsolverAuto_Completed( ){ 
            displayTimer.Start(); __DispMode="Complated";
        }

        private void btnSDKAnalyzerAuto_Click( object sender, RoutedEventArgs e ){
            if( OnWork==1 ) return;

            if( (string)btnSDKAnalyzerAuto.Content=="中　断" ){
                tokSrc.Cancel();
                try{ taskSDK.Wait(); }
                catch(AggregateException){ __DispMode="Canceled"; }
                displayTimer.Start();
                OnWork = 0;
            }
            else{
                List<UCell> pBDL = pGP.BDL;
                if( pBDL.Count(p=>p.No==0)==0 ){             //解析完了
                    _SetScreenProblem();
                    goto AnalyzerEnd;
                }
                if( pBDL.Any(p=>(p.No==0 && p.FreeB==0)) ){ //解なし
                    lblAnalyzerResult.Text = "候補数字がなくなったセルがある";
                    goto AnalyzerEnd;
                }

                OnWork = 2;
                btnSDKAnalyzerAuto.Content = null;
                btnSDKAnalyzerAuto.Content = "中　断";          
                Lbl_onAnalyzer.Content  = "解析中";
                Lbl_onAnalyzer.Foreground = Brushes.Orange;               

                int mc=GNP00.GNPX_Eng.Set_MethodLst_Run( );
                if( mc<=0 ) GNP00.ResetMethodList(); 
                
                _ResetAnalizer(true); //解析結果クリア 
                GNP00.GNPX_Eng.AnalyzerCounterReset(); 

                GNPZ_Engin.SolInfoDsp = false;
                SDK_Ctrl.lvlLow = 0;
                SDK_Ctrl.lvlHgh = 999;
//              GNPZ_Engin.SolInfoDsp = true;

                //==============================================================
                tokSrc = new CancellationTokenSource();　//中断時の手続き用
                CancellationToken ct = tokSrc.Token;   
                taskSDK = new Task( ()=> GNP00.SDKCntrl.AnalyzerRealAuto(ct), ct );
                taskSDK.ContinueWith( t=> task_SDKsolverAuto_Completed() ); //完了時の手続きを登録
                AnalyzerLap.Reset(); 
                taskSDK.Start();
                //--------------------------------------------------------------
   
                this.Cursor = Cursors.Wait;

                AnalyzerLap.Start();
                displayTimer.Start();
          //      if( (bool)AutoSolDisp.IsChecked ) displayTimer.Start();

              AnalyzerEnd:
                //GNPZ_Engin.ChkPrint = false;    //*****
                return;

            }
        }
        private void btnAnalyzerResetAll_Click( object sender, RoutedEventArgs e ){
            Thickness X=PB_GBoard.Margin;
            PB_GBoard.Margin=new Thickness(X.Left+2,X.Top+2,X.Right,X.Bottom);
            SDK_Ctrl.UGPMan=null;                       //複数解探索初期化
            _ResetAnalizer(true);
            bruMoveTimer.Start();
            MAnalizeBtnSet();
        }
        private void _ResetAnalizer( bool AllF=true ){
            if( OnWork>0 ) return;
            AnalyzerCC = 0;
            btnAnalyzerCC.Content= "";
            if(AllF) GNP00.GNPX_Eng.AnMan.ResetAnalysisResult( );    //解析結果のみクリア
            btnSDKAnalyzerAuto.Content  = "自動解析";
            lblAnalyzerResult.Text    = "";
            Lbl_onAnalyzer.Content    = "";         
            Lbl_onAnalyzerTS.Content  = "";

            btnAnalyzerCCM.Content    = "";
            Lbl_onAnalyzerM.Content   = "";
            Lbl_onAnalyzerTSM.Content = "";
            lblAnalyzerResultM.Text   = "";

            btnMltAnlzSearch.IsEnabled=true;

            ListBox_SuDoKuTecLst.Clear();
            ListBox_SuDoKuTec.ItemsSource = null;

            Lbl_onAnalyzerTS3.Content = "経過時間：";
            GNP00.GNPX_Eng.cCodePre   = 0;
            GNP00.GNPX_Eng.AnMan.Set_CellFreeB();

            GNP00.GNPX_Eng.AnalyzerCounterReset();
            SDK_Ctrl.UGPMan=null;

            _DGViewMethodCounterSet();

            displayTimer.Stop();
            _SetScreenProblem();
        }
      #endregion 解析【全て】

      #region 解析【手法集計】
        public int AnalyzerCC=0;
        private int AnalyzerCCMemo=0;
        private int AnalyzerMMemo=0;   
        private int[] eNChk;
        private bool _cellFixSub(  ){
            if( GNP00.pGP.SolCode<0) return false;
            bool retB=GNP00.GNPX_Eng.AnMan.FixOrEliminate_SuDoKu( ref eNChk );
            if( !retB && GNP00.GNPX_Eng.AnMan.SolCode==-9119 ){
                string st="";
                for( int h=0; h<27; h++ ){
                    if(eNChk[h]!=0x1FF){
                        st+=_ToHouseName(h)+"で数字#"+(eNChk[h]^0x1ff).ToBitStringNZ(9)+"がなくなった\r";
                        GNP00.GNPX_Eng.AnMan.SetBG_OnError(h);
                    }
                }

                lblAnalyzerResult.Text=st;
                GNP00.pGP.SolCode = GNP00.GNPX_Eng.AnMan.SolCode;
                return false;
            }

            if( GNP00.pGP.SolCode==-999 ){
                lblAnalyzerResult.Text = "手法制御のエラー";
                GNP00.pGP.SolCode = -1;
            }

            int nP=0, nZ=0, nM=0;
            cellPZMCounterForm( ref nP, ref nZ, ref nM );
            if( nZ==0){ GNP00.GNPX_Eng.AnMan.SolCode=0; return true; }
            if( nM!=AnalyzerMMemo ){
                AnalyzerCCMemo = AnalyzerCC;
                AnalyzerMMemo = nM;
            }

            if( nZ==0 && (bool)cbxFileDifficultyLevel.IsChecked ){
                string prbMessage;
                int DifLevel = GNP00.GNPX_Eng.GetDifficultyLevel( out prbMessage );
                pGP.DifLevel = DifLevel;
                nUDDifficultyLevel.Text = DifLevel.ToString();
            }
            return true;
        }
        private string _ToHouseName( int h ){
            string st="";
            switch(h/9){
                case 0: st="行"; break;
                case 1: st="列"; break;
                case 2: st="ブロック"; break;
            }
            st += ((h%9)+1).ToString();
            return st;
        }
        private List<MethodCounter> MCList;
        private void _DGViewMethodCounterSet(){ //手法の集計
            MCList = new List<MethodCounter>();

            foreach( var P in GNP00.GNPX_Eng.MethodLst_Run ){
                if( P.UsedCC <= 0 )  continue;
                MCList.Add( new MethodCounter( P.MethodName, P.UsedCC ) );
            }
            DGViewMethodCounter.ItemsSource = MCList;
            if( MCList.Count>0 )  DGViewMethodCounter.SelectedIndex=-1;

            if( GNP00.GSmode=="解析" && MCList.Count>0 && DGViewMethodCounter.Columns.Count>1 ){
                //http://msdn.microsoft.com/ja-jp/library/ms745683.aspx
                //http://social.msdn.microsoft.com/Forums/ja/csharpgeneralja/thread/6a3160d7-8ce0-461b-89e2-9b20e1ff31d6
                //【Att】HorizontalAlignment.Centerとすると、不要な縦線が現れる 現象・原因・対処方法不明
                Style style = new Style(typeof(DataGridCell));
                style.Setters.Add(new Setter(DataGrid.HorizontalAlignmentProperty, HorizontalAlignment.Right));
                DGViewMethodCounter.Columns[1].CellStyle = style;
            }

        }
        private class MethodCounter{
            public string methodName{ get; set; }
            public string count{ get; set; }
            public MethodCounter( string nm, int cc ){
                methodName = " "+nm;//.PadRight(30);
                count = cc.ToString()+" ";
            }
        }

      #endregion 解析【手法集計】 
    #endregion 解析

        private void Window_Unloaded( object sender, RoutedEventArgs e ){
            Environment.Exit(0);
        }
        private void randumSeed_TextChanged( object sender, TextChangedEventArgs e ){
            int rv=randumSeed.Text.ToInt();
            GNP00.SDKCntrl.randumSeedVal = rv;
            GNP00.SDKCntrl.SetRandumSeed(rv);
        }

        private void NiceLoopMax_ValueChanged( object sender, RoutedPropertyChangedEventArgs<object> e ){
            if( NiceLoopMax==null )  return;
            GNPXpzl.GMthdOption["NiceLoopMax"]  = NiceLoopMax.Value.ToString();
        }

        private void ALSSizeMax_ValueChanged( object sender, RoutedPropertyChangedEventArgs<object> e ){
            if( ALSSizeMax==null )  return;
            GNPXpzl.GMthdOption["ALSSizeMax"]  = ALSSizeMax.Value.ToString();
        }

        private void _Get_GNPXOptionPara(){
            GNPXpzl.GMthdOption["Cell"]         = ((bool)method_NLCell.IsChecked)? "1": "0";
            GNPXpzl.GMthdOption["GroupedCells"] = ((bool)method_NLGCells.IsChecked)? "1": "0";
            GNPXpzl.GMthdOption["ALS"]          = ((bool)method_NLALS.IsChecked)? "1": "0";

     ///    GNPXpzl.GMthdOption["AFish"]        = ((bool)method_NLALS.IsChecked)? "1": "0"; //次期開発

        }
        private void PutBitMap_Click( object sender, RoutedEventArgs e ){
            Clipboard.SetData(DataFormats.Bitmap,bmpGZero);
        }

        private void SaveBitMap_Click( object sender, RoutedEventArgs e ){
            Clipboard.SetData(DataFormats.Bitmap,bmpGZero);

            BitmapEncoder enc = new PngBitmapEncoder(); // JpegBitmapEncoder(); BmpBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(bmpGZero));

            if( !Directory.Exists("画像蔵") ){ Directory.CreateDirectory("画像蔵"); }
            string fName=DateTime.Now.ToString("yyyyMMdd HHmmss")+".png";
            using( Stream stream = File.Create("画像蔵/"+fName) ){
                enc.Save(stream);
            }         
        }

        private void btnHomePage_Click( object sender, RoutedEventArgs e ){
            Process.Start("http://csdenp.web.fc2.com");
        }

        private void txtProbName_KeyUp( object sender, KeyEventArgs e ){
            if( e.Key==Key.Return )  pGP.Name=txtProbName.Text;
        }


//▼▼▼公開時は削除 ???               
        private void btnPatCVRCg_Click( object sender, RoutedEventArgs e ){
            Button btn = sender as Button;
            TransSolverA(btn.Name,(bool)chbDispAns.IsChecked);

            _SetScreenProblem();
            _Display_Develop();
        }
        private void TransSolverA( string Name, bool DspSol ){
            SDK_Ctrl.MltProblem = 1;    //単独
            SDK_Ctrl.lvlLow = 0;
            SDK_Ctrl.lvlHgh = 999;
            GNP00.SDKCntrl.CbxDspNumRandmize=false;
            GNP00.SDKCntrl.GenLStyp = 1;
            GNPXpzl.MltSolOn = (bool)MltSolOn.IsChecked;
            GNPZ_Engin.SolInfoDsp = true;

            if(pGP.AnsNum==null) pGP.PInitialize(GNP00);
            pGP.PTrans.SDK_TransProbG(Name,DspSol);
        }

        private void chbDispAns_Checked( object sender, RoutedEventArgs e ){
            if(pGP.AnsNum==null){
                pGP.PInitialize(GNP00);
                TransSolverA("Checked",true);
            }
            pGP.PTrans.SDK_TransProbG("Checked",(bool)chbDispAns.IsChecked);
            _Display_GB_GBoard(DevelopB:true);
        }

        private void btnTransEst_Click( object sender, RoutedEventArgs e ){
            pGP.PTrans.btnTransEst();
            _Display_GB_GBoard(DevelopB:true);
        }

        private void btnTransRes_Click(object sender, RoutedEventArgs e ){
            pGP.PTrans.btnTransRes();
            if(!(bool)chbDispAns.IsChecked) pGP.BDL.ForEach(P=>{P.No=Max(P.No,0);});
            _Display_GB_GBoard(DevelopB:true);
        }

        private void btnNomalize_Click( object sender, RoutedEventArgs e ){
            if(pGP.AnsNum==null){
                pGP.PInitialize(GNP00);
                TransSolverA("Checked",true);
            }
            string st=pGP.PTrans.SDK_Nomalize( (bool)chbDispAns.IsChecked, (bool)chbNrmlNum.IsChecked );
            tbxTransReport.Text=st;
            _Display_GB_GBoard(DevelopB:true);
        }

		private void ForceL0_Checked( object sender,RoutedEventArgs e ){
			GNPXpzl.GMthdOption["ForceLx"] = ((RadioButton)sender).Name;
		}
    }
}