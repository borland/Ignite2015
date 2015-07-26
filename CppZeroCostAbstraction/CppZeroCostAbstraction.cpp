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

