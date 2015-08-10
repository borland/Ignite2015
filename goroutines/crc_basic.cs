// This is free and unencumbered software released into the public domain.
// Anyone is free to copy, modify, publish, use, compile, sell, or
// distribute this software, either in source code form or as a compiled
// binary, for any purpose, commercial or non-commercial, and by any
// means.
using System;
using System.IO;

class crc_basic
{
    static int totalFiles;

	static uint CalcCrc32(string filePath) {
		var buffer = File.ReadAllBytes(filePath);

		return DamienG.Security.Cryptography.Crc32.Compute(buffer);
	}

	static void ScanDir(string dir) {
		foreach(var f in Directory.GetFiles(dir)) {
			var absPath = Path.Combine(dir, f);
			var val = CalcCrc32(absPath);
            Console.WriteLine($"File #{++totalFiles}");
            Console.WriteLine($"Got crc {val} for {absPath}");
		}
		foreach(var d in Directory.GetDirectories(dir)) {
			ScanDir(Path.Combine(dir, d));
		}
	}

	public static void Run()
	{
		ScanDir("/Users/orion/OneDrive/Ignite2015/dev/goroutines");				
	}
}