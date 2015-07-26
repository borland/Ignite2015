// SafeHandleCpp.cpp : Defines the exported functions for the DLL application.
//

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