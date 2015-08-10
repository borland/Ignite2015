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