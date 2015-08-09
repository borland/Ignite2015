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