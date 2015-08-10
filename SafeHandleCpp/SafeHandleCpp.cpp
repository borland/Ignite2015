// This is free and unencumbered software released into the public domain.
// Anyone is free to copy, modify, publish, use, compile, sell, or
// distribute this software, either in source code form or as a compiled
// binary, for any purpose, commercial or non-commercial, and by any
// means.
#include <SDKDDKVer.h>
#include <string>

struct Resource {
	int Id;
	std::string Name;
};

extern "C" __declspec(dllexport) Resource* __stdcall OpenResource(int id, const char* name)
{
	return new Resource{ id, name };
}

extern "C" __declspec(dllexport) void __stdcall CloseResource(Resource* resource)
{
	delete resource;
}

extern "C" __declspec(dllexport) void __stdcall UseResource(Resource* resource)
{
	printf("using resource %d [%s]\n", resource->Id, resource->Name.c_str());
}