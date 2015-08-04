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

public static class OptionalExtensions
{
    public static void Unwrap<T>(this Optional<T> optional, Action<T> some, Action none = null) where T : class
    {
        if (optional.HasValue)
            some(optional.Value);
        else if (none != null)
            none();
    }
    
    public static void Unwrap<T>(this T optional, Action<T> some, Action none = null) where T : class
    {
        if (optional != null)
            some(optional);
        else if (none != null)
            none();
    }
}

public struct Optional<T> where T : class
{
    readonly T m_value;

	public Optional(T value)
	{
        m_value = value;
	}

    public static implicit operator Optional<T>(T value) => new Optional<T>(value);

    public bool HasValue => m_value != null;

    public T Value
    {
        get
        {
            if (m_value == null)
                throw new InvalidOperationException($"No value for Optional<{typeof(T)}>");
            return m_value;
        }
    }

    public T UnsafeValue => m_value;
}


