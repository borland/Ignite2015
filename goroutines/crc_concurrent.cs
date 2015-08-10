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
	}

	static void ScanDir(string dir, Channel<CrcResult> results, Channel<int> refCount) {
		foreach(var f in Directory.GetFiles(dir)) {
			var absPath = Path.Combine(dir, f);
			Task.Run(() => CalcCrc32(absPath, results, refCount));
		}
		foreach(var d in Directory.GetDirectories(dir)) {
			var absPath = Path.Combine(dir, d);
			Task.Run(() => ScanDir(absPath, results, refCount));
		}
	}

	public static async Task Run()
	{
		var results = new Channel<CrcResult>();
		var refCount = new BufferedChannel<int>(1);
		await refCount.Send(1);
        new Thread(() => { }).Start();

		await Task.Run(() => ScanDir("/Users/orion/OneDrive/Ignite2015/dev/goroutines", results, refCount));

        int totalFiles = 0;
		while(true) {
			var r = await results.Receive();
            Console.WriteLine($"File #{++totalFiles}");
			Console.WriteLine($"Got {r.Value} for {r.Path}");
        }
	}
}