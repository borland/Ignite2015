using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

class Channel<T>{
	readonly int m_bufferCapacity;
	readonly Queue<T> m_buffer;

	public Channel(int bufferSize = 0) {
		m_bufferSize = bufferSize;
		m_buffer = new Queue<T>(bufferSize);
	}

	public async void Send(T value) {
		if(m_buffer.Count >= m_bufferCapacity) { // block
			await receiver()(value);
		} else { // buffer
			m_buffer.Enqueue(value);
		}
	}

	public T Receive() {
		return default(T);
	}

	async Task<Action<T>> receiver() {
		// if we have a receiver, return it
		// if not, block until we do
	}
}

struct CrcResult {
	uint Value;
	string Path;
}

class crc_concurrent
{
	static uint CalcCrc32(string filePath, Channel<CrcResult> results, Channel<int> refCount) {
		var buffer = File.ReadAllBytes(filePath);

		return DamienG.Security.Cryptography.Crc32.Compute(buffer);
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

	public static void Run()
	{
		var results = new Channel<CrcResult>();
		var refCount = new Channel<int>(bufferSize: 1);
		refCount.Send(1);
		Task.Run(() => ScanDir("/Users/orione/OneDrive/Ignite2015/dev/goroutines", results, refCount));
	}
}