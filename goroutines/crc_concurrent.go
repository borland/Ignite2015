// This is free and unencumbered software released into the public domain.
// Anyone is free to copy, modify, publish, use, compile, sell, or
// distribute this software, either in source code form or as a compiled
// binary, for any purpose, commercial or non-commercial, and by any
// means.
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