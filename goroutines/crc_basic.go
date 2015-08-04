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

func calcCrc32(filePath string, info os.FileInfo) (uint32, error) {
    f, _ := os.Open(filePath)
    defer f.Close()
    
    buffer, _ := ioutil.ReadAll(f)    
    return crc32.ChecksumIEEE(buffer), nil
}

func scanDir(dir string) {
    files, _ := ioutil.ReadDir(dir)
    for _, f := range files {
        absPath := filepath.Join(dir, f.Name())
        if f.IsDir() {
            scanDir(absPath)
        } else {
            val, _ := calcCrc32(absPath, f)
            fmt.Printf("Got crc %v for %v\n", val, absPath)
        }
    }
}

func main() {
    scanDir("/Users/orion/OneDrive/Ignite2015/dev/goroutines")
}