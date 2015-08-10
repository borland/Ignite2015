// This is free and unencumbered software released into the public domain.
// Anyone is free to copy, modify, publish, use, compile, sell, or
// distribute this software, either in source code form or as a compiled
// binary, for any purpose, commercial or non-commercial, and by any
// means.
package main
import "fmt"
import "time"

func multiply(a int, b int, result chan int) {
	result <- a * b
}
 
func main() {
    result := make(chan int)
    go multiply(10, 20, result)
    time.Sleep(1000 * time.Millisecond)
    fmt.Println("result was", <-result)
}
 
