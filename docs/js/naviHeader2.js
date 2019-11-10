function writeNaviHeader(fName){

	var fReader = new FileReader();
	
	fReader.onLoad = function(evt){
		document.write(evt.target.result);
	}
	
	fReader.readAsText(fName);
}