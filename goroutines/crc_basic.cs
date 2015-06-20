using System;
using System.IO;

class crc_basic
{
	static uint CalcCrc32(string filePath) {
		var buffer = File.ReadAllBytes(filePath);

		return DamienG.Security.Cryptography.Crc32.Compute(buffer);
	}

	static void ScanDir(string dir) {
		var files = Directory.GetFileSystemEntries(dir, "*.*", SearchOption.AllDirectories);
		foreach(var f in files) {
			if(Directory.Exists(f))
				continue;

			var absPath = Path.Combine(dir, f);
			var val = CalcCrc32(absPath);
			Console.WriteLine("Got crc {0} for {1}", val, absPath);
		}
	}

	public static void Run()
	{
		ScanDir("/Users/orione/OneDrive/Ignite2015/dev/goroutines");				
	}
}