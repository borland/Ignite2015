using System;
using System.Runtime.InteropServices;

namespace optionals_enums
{
	[StructLayout(LayoutKind.Explicit)]
	public struct Either<TA, TB>
	{
		[FieldOffset(0)]
		private readonly int m_which;

		[FieldOffset(4)]
		private readonly TA m_a;

		[FieldOffset(4)]
		private readonly TB m_b;

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

		public int Which {
			get{ return m_which; }
		}
		public TA A { get { return m_a; } }
		public TB B { get { return m_b; } }
	}
}

