// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <windows.h>
#include <stdio.h>

typedef void* (__cdecl *P_GATEWAY_CREATE_FROM_JSON)(char*);
typedef void (__cdecl *P_GATEWAY_DESTROY)(void*);

int main(int argc, char** argv)
{
	if (argc != 2)
	{
		printf("usage: dotnet_binding_sample configFile\n");
		printf("where configFile is the name of the file that contains the Gateway configuration\n");
		return 1;
	}

	HINSTANCE hDLL = LoadLibrary(TEXT("gateway.dll"));
	if (!hDLL)
	{
		printf("failed to load gateway.dll, error: %d\n", GetLastError());
		return 1;
	}

	P_GATEWAY_CREATE_FROM_JSON Gateway_CreateFromJson = (P_GATEWAY_CREATE_FROM_JSON) GetProcAddress(hDLL, "Gateway_CreateFromJson");
	if (!Gateway_CreateFromJson)
	{
		printf("failed to load function Gateway_CreateFromJson, error: %d\n", GetLastError());
		return 1;
	}
		
	void* pGateway = Gateway_CreateFromJson(argv[1]);
	if (!pGateway)
    {
        printf("failed to create the gateway from JSON\n");
		return 1;
    }
    
	printf("gateway successfully created from JSON\n");
    printf("gateway will run until ENTER is pressed\n");
    getchar();

	P_GATEWAY_CREATE_FROM_JSON Gateway_Destroy = (P_GATEWAY_CREATE_FROM_JSON)GetProcAddress(hDLL, "Gateway_Destroy");
	if (Gateway_Destroy)
	{
		Gateway_Destroy(pGateway);
	}

	FreeLibrary(hDLL);
	
	return 0;
}
