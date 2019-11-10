using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

using System.Windows.Threading;
using System.Diagnostics;

//WPF で Zune のようなウィンドウを作るAdd Star
//http://grabacr.net/archives/480

namespace GNPZ_sdk{
    public partial class ToGNPXHP: Window {
        public DispatcherTimer CountDownTimer;
        private int WarpToCC=8;

        public ToGNPXHP( DateTime ExpireDate ){
            InitializeComponent();
            this.MouseLeftButtonDown += (sender, e) => this.DragMove();

            LblExpireDate.Content = "有効期限："+ExpireDate.ToShortDateString();
            WarpToHP.Content = WarpToCC.ToString()+" 秒後に GNPX-HP にワープ";

            CountDownTimer = new DispatcherTimer(DispatcherPriority.Normal);
            CountDownTimer.Interval = TimeSpan.FromMilliseconds(1000);
            CountDownTimer.Tick += new EventHandler(CountDownTimer_Tick);
            CountDownTimer.Start();
        }

        private void CountDownTimer_Tick( object sender, EventArgs e ){
            if( (--WarpToCC)==0 ){
                Process.Start("http://csdenp.web.fc2.com");
                Environment.Exit(0);
            }
            else{ 
                WarpToHP.Content = (WarpToCC).ToString()+" 秒後に GNPX-HP にワープ";
            }
        }
    }
}