using System.Collections.Generic;
using System.Threading.Tasks;
using System;

public class Channel<T>{
	readonly int m_bufferCapacity;
	readonly Queue<T> m_buffer;
	readonly Queue<TaskCompletionSource<T>> m_receivers = new Queue<TaskCompletionSource<T>>();

	TaskCompletionSource<TaskCompletionSource<T>> m_receiverAvailableTcs = new TaskCompletionSource<TaskCompletionSource<T>>();
	Task<TaskCompletionSource<T>> m_receiverAvailable = m_receiverAvailableTcs.Task;

	public Channel(int bufferSize = 0) {
		m_bufferCapacity = bufferSize;
		m_buffer = new Queue<T>(bufferSize);
	}

	public async Task Send(T value) {
		if(m_buffer.Count < m_bufferCapacity) {
			//if we have buffer space, buffer it
			m_buffer.Enqueue(value);
		} else {
			var tcs = await dequeueReceiever();
			tcs.SetResult(value);
		}
	}

	public Task<T> Receive() {
		// if we have a buffered value, return it
		if(m_buffer.Count > 0) {
			return Task.FromResult(m_buffer.Dequeue());
		} else {
			// set a new receiever which will wait for a sender
			return enqueueReceiver();
		}
	}

	Task<TaskCompletionSource<T>> dequeueReceiever() {
		return null;
	}

	Task<T> enqueueReceiver() {
	}
}