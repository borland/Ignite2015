package main

import "fmt"
import "io/ioutil"
import "path/filepath"
import "os"
import "hash/crc32"
import "log"
// import "runtime"

func calcCrc32(filePath string, info os.FileInfo) (uint32, error) {
    f, err := os.Open(filePath)
    if err != nil {
        return 0, err
    }
    defer f.Close()
    
    buffer, err := ioutil.ReadAll(f)
    if err != nil {
        return 0, err
    }

    fmt.Printf("%d bytes read\n", len(buffer))    
    return crc32.ChecksumIEEE(buffer), nil
}

func scanDir(dir string) {
    // fmt.Println("Scanning %v", dir)
    files, _ := ioutil.ReadDir(dir)
    for _, f := range files {
        absPath := filepath.Join(dir, f.Name())
        fmt.Println(absPath)
        if f.IsDir() {
            scanDir(absPath)
        } else {
            val, err := calcCrc32(absPath, f)
            if err == nil {
                fmt.Printf("crc of %d\n", val)
            } else {
                log.Printf(err.Error())
            }
        }
    }
}

func main() {
    // cpus := runtime.NumCPU()
    // runtime.GOMAXPROCS(cpus)
    scanDir("/Users/orione/OneDrive/Ignite2015")
    
    // fmt.Printf("%v cpus\n", cpus)
}