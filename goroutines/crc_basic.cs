using System;
using System.IO;

class crc_basic
{
	static uint CalcCrc32(string filePath) {
		var buffer = File.ReadAllBytes(filePath);

		return DamienG.Security.Cryptography.Crc32.Compute(buffer);
	}

	static void ScanDir(string dir) {
		foreach(var f in Directory.GetFiles(dir)) {
			var absPath = Path.Combine(dir, f);
			var val = CalcCrc32(absPath);
			Console.WriteLine($"Got crc {val} for {absPath}");
		}
		foreach(var d in Directory.GetDirectories(dir)) {
			ScanDir(Path.Combine(dir, d));
		}
	}

	public static void Run()
	{
		ScanDir("/Users/orione/OneDrive/Ignite2015/dev/goroutines");				
	}
}