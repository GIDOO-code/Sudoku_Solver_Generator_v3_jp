# Sudoku_Solver_Generator
![GNPX](/images/GNPX_start.PNG)

数独の問題を 解く・作る C#プログラムです。
解析アルゴリズムは、全て論理的手法で、バックトラックは使いません。
以下のアルゴリズムを実装しています。  

>Single, LockedCandidate, (hidden)LockedSet(2D-7D),
 (Finned)(Franken/Mutant/Kraken)Fish(2D-7D),
 Skyscraper, EmptyRectangle, XY-Wing, W-Wing, RemotePair, XChain, XYChain,
 SueDeCoq, (Multi)Coloring,
 ALS-Wing, ALS-XZ, ALS-Chain,
 (ALS)DeathBlossom(Ext.), (Grouped)NiceLoop, ForceChain and
 GeneralLogic.

プログラム機能として、単解析、複数解析、数独問題作成（数独のパターン、レベル指定が可能）、問題の変換・数独問題の標準化ができます。

## ダウンロード、日本語化
プログラムは、環境に応じて（または手動で）英語と日本語に切り替わります。
ソースコード（VSプロジェクト）は、英語版 WEB ページからダウンロードしてください。  
https://github.com/GIDOO-code/Sudoku_Solver_Generator
