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
using System.Runtime.InteropServices;

namespace optionals_enums
{
    public static class EitherExtensions
    {
        public static void Switch<TA, TB>(
            this Either<TA, TB> either,
            Action<TA> ifA, Action<TB> ifB)
        {
            if (either.Which == 0)
                ifA(either.A);
            else
                ifB(either.B);
        }
    }

    public struct Either<TA, TB>
    {
        private readonly TA m_a;
        private readonly TB m_b;
        private readonly int m_which;

        public static implicit operator Either<TA, TB>(TA a) => new Either<TA, TB>(a);

        public static implicit operator Either<TA, TB>(TB b) => new Either<TA, TB>(b);

        public Either(TA a)
        {
            m_which = 0;
            m_b = default(TB); // will get overwritten
            m_a = a;
        }

        public Either(TB b)
        {
            m_which = 1;
            m_a = default(TA); // will get overwritten
            m_b = b;
        }

        public int Which
        {
            get { return m_which; }
        }

        public TA A
        {
            get
            {
                if (m_which != 0)
                    throw new ArgumentException("Cannot access field A");
                return m_a;
            }
        }

        public TB B
        {
            get
            {
                if (m_which != 1)
                    throw new ArgumentException("Cannot access field A");
                return m_b;
            }
        }
    }
}

