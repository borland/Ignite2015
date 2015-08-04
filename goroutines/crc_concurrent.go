// Copyright(c) 2015 Orion Edwards
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
package main

import "fmt"
import "io/ioutil"
import "path/filepath"
import "os"
import "hash/crc32"

type crcResult struct {
    value uint32
    path string
}

func calcCrc32(filePath string, info os.FileInfo, c chan crcResult, refCount chan int) {
    f, _ := os.Open(filePath)
    defer f.Close()
    
    buffer, _ := ioutil.ReadAll(f)
    c <- crcResult{ crc32.ChecksumIEEE(buffer), filePath }
    refCount <- -1
}

func scanDir(dir string, c chan crcResult, refCount chan int) {
    files, _ := ioutil.ReadDir(dir)
    for _, f := range files {
        absPath := filepath.Join(dir, f.Name())
        if f.IsDir() {
            refCount <- 1
            go scanDir(absPath, c, refCount)
        } else {
            refCount <- 1
            go calcCrc32(absPath, f, c, refCount)
        }
    }
    refCount <- -1
}

func main() {
    results := make(chan crcResult)
    refCount := make(chan int, 2) // need a buffered channel due to next line
    refCount <- 1
    go scanDir("/Users/orion/OneDrive/Ignite2015/dev/goroutines", results, refCount)
    
    rc := 0
    
    for {
        select {
        case result := <- results:
            fmt.Printf("Got crc %v for %v\n", result.value, result.path)
        case delta := <- refCount:
            rc += delta
            if rc == 0 {
                fmt.Println("all done")
                return
            }
        }
    }
    
    // fmt.Printf("%v cpus\n", cpus)
}