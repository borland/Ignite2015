//: Playground - noun: a place where people can play

import Cocoa

enum Errors : ErrorType {
    case DivideByZero
}

enum Either<TA, TB> {
    case A(TA)
    case B(TB)
}

func divide(value:Int, by:Int) -> Either<Int, ErrorType> {
    if by == 0 {
        return Either<Int, ErrorType>.B(Errors.DivideByZero)
    }
    return Either<Int, ErrorType>.A(value / by)
}

divide(6, by: 2)

switch divide(6, by: 2) {
case .A(let value):
    print(value)
case .B(let error):
    print(error)
}

func getFileContents(path:String) -> String? {
    do {
        return try NSString(contentsOfFile: path, encoding: NSUTF8StringEncoding) as String?
    } catch {
        return nil
    }
}

//getFileContents("nosuchfile")

//getFileContents("/Users/orione/OneDrive/Ignite2015/dev/optionals_enums/Either.cs")
//
let len = (try! NSString(contentsOfFile: "/Users/orione/OneDrive/Ignite2015/dev/demo.swift", encoding: NSUTF8StringEncoding) as String).lengthOfBytesUsingEncoding(NSUTF8StringEncoding)
print(len)
