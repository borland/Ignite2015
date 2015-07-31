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
 
