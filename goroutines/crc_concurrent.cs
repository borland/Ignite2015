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
	static async Task CalcCrc32(string filePath, Channel<CrcResult> results, Channel<Exception> errors, WaitGroup wg) {
        try
        {
            var buffer = File.ReadAllBytes(filePath);
            if (buffer.Length < 1) // simulate an exception
            {
                await errors.Send(new Exception($"0 byte file at {filePath}"));
                return;
            }

            await results.Send(new CrcResult {
                Value = DamienG.Security.Cryptography.Crc32.Compute(buffer),
                Path = filePath
            });
        }
        catch(Exception e)
        {
            await errors.Send(e);
        }
        finally
        {
            wg.Done();
        }
	}

	static void ScanDir(string dir, Channel<CrcResult> results, Channel<Exception> errors, WaitGroup wg) {
		foreach(var f in Directory.GetFiles(dir)) {
			var absPath = Path.Combine(dir, f);
            wg.Add(1);
			Go.Run(CalcCrc32, absPath, results, errors, wg);
		}
		foreach(var d in Directory.GetDirectories(dir)) {
			var absPath = Path.Combine(dir, d);
            wg.Add(1);
            Go.Run(ScanDir, absPath, results, errors, wg);
		}
        wg.Done();
    }

    public static async Task Run()
    {
        var results = new Channel<CrcResult>();
        var errors = new Channel<Exception>();
        var wg = new WaitGroup();
        wg.Add(1);

        Go.Run(async () => { // close the channels when the waitGroup signals
            await wg.Wait();
            results.Close();
            errors.Close();
        });
        Go.Run(ScanDir, "/Users/orion/OneDrive/Ignite2015/dev/goroutines", results, errors, wg);

        int totalFiles = 0;
        while(results.IsOpen || errors.IsOpen) {
            await Go.Select(
                Go.Case(results, r => {
                    Console.WriteLine($"Got {r.Value} for {r.Path}");
                    totalFiles++;
                }),
                Go.Case(errors, exception => {
                    Console.WriteLine($"EXCEPTION: {exception}");
                    totalFiles++;
                }));
        }

        Console.WriteLine($"{totalFiles} total files");
    }
        
}