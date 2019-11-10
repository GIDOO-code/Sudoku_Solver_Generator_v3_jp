function writeNaviHeader(){

	var html = "";
	html += '<nav>';
	html += '<ul>';
	html += '<li><a href="index.html">ホーム</a></li>';
	html += '<li><a href="page1.html">準備</a></li>';
	html += '<li><a href="page2.html">解析アルゴリズム</a></li>';
	html += '<li><a href="page17.html">ダウンロード</a></li>';
//	html += '<li><a href="page18.html">掲示板</a></li>';
	html += '<li><a href="page19.html">このサイトについて</a></li>';
	html += '<li><a href="https://gidoo-code.github.io/Sudoku_Solver_Generator/">English</a></li>';
	
	        	
	html += '</ul>';
	html += '</nav>';

	document.write(html);
}