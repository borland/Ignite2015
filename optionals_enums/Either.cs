// This is free and unencumbered software released into the public domain.
// Anyone is free to copy, modify, publish, use, compile, sell, or
// distribute this software, either in source code form or as a compiled
// binary, for any purpose, commercial or non-commercial, and by any
// means.
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

