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
#include <SDKDDKVer.h>

#include <stdio.h>
#include <tchar.h>
#include <cstdint>
#include <chrono>

struct Seconds {
	const double Value;
	Seconds(double x) : Value(x) { }
};

struct MetresPerSecond {
	const double Value;
	MetresPerSecond(double x) : Value(x) { }
};

struct Metres {
	const double Value;
	Metres operator+(double x)
	{ return Value + x; }

	Metres(double x) : Value(x) { }

	MetresPerSecond operator / (Seconds s) {
		return Value / s.Value;
	}
};

void Delay(Seconds howLong) {

}

void Travel(Metres howFar) {

}

int _tmain(int argc, _TCHAR* argv[])
{
	double x = 100;
	double x2 = x + 100;

	Metres m{ 100 };
	Metres m2 = m + 100;

	Metres tenKms{ 10 * 1000 };
	Seconds oneHour{ 60 * 60 };

	auto speed = tenKms / oneHour;
	printf("%f m/s\n", speed.Value);

	printf("array of a million doubles is %d bytes\n", sizeof(double[1000000]));
	printf("array of a million Metres is %d bytes\n", sizeof(Metres[1000000]));
	printf("array of a million ints is %d bytes\n", sizeof(int[1000000]));

	//Delay(km1);

	printf("done\n");
	getchar();

	return 0;
}

