func getFileContents(path:String) -> String? {
    do {
		return try NSString.stringWithContentsAtPath(path, encoding:NSUTF8StringEncoding)
	} catch {
		return nil
	}
}

if let contents = getFileContents("/path/to/file") {
	print(contents)
} else {
	print("no contents")
}