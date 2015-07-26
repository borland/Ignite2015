import Cocoa
let str = try! NSString(contentsOfFile: "/Users/orione/OneDrive/Ignite2015/dev/demo.swift", encoding: NSUTF8StringEncoding) as String
print(str.lengthOfBytesUsingEncoding(NSUTF8StringEncoding))