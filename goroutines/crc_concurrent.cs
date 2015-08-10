// This is free and unencumbered software released into the public domain.
// Anyone is free to copy, modify, publish, use, compile, sell, or
// distribute this software, either in source code form or as a compiled
// binary, for any purpose, commercial or non-commercial, and by any
// means.
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

struct CrcResult {
	public uint Value;
	public string Path;
}

class crc_concurrent
{
	static async void CalcCrc32(string filePath, Channel<CrcResult> results, Channel<int> refCount) {
		var buffer = File.ReadAllBytes(filePath);
		
		await results.Send(new CrcResult {
			Value = DamienG.Security.Cryptography.Crc32.Compute(buffer),
			Path = filePath
		});
        await refCount.Send(-1);
	}

	static async Task ScanDir(string dir, Channel<CrcResult> results, Channel<int> refCount) {
		foreach(var f in Directory.GetFiles(dir)) {
			var absPath = Path.Combine(dir, f);
            await refCount.Send(1);
			Go.Run(CalcCrc32, absPath, results, refCount);
		}
		foreach(var d in Directory.GetDirectories(dir)) {
			var absPath = Path.Combine(dir, d);
            await refCount.Send(1);
            Go.Run(ScanDir, absPath, results, refCount);
		}
        await refCount.Send(-1);
    }

    public static async Task Run()
    {
        var results = new Channel<CrcResult>();
        var refCount = new BufferedChannel<int>(2);
        await refCount.Send(100);
        Go.Run(ScanDir, "/Users/orion/OneDrive/Ignite2015/dev/goroutines", results, refCount);

        int rc = 0;
        int totalFiles = 0;
        for (var stop = false; !stop;) {
            await Go.Select(
                Go.Case(results, r => {
                    Console.WriteLine($"Got {r.Value} for {r.Path}");
                    totalFiles++;
                }),
                Go.Case(refCount, delta => {
                    rc += delta;
                    if (rc == 0) {
                        Console.WriteLine("all done");
                        stop = true;
                    }
                }));
        }

        Console.WriteLine($"{totalFiles} total files");
    }
        
}