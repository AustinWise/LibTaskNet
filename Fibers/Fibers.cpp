// This is the main DLL file.

#include "stdafx.h"
#define _WIN32_WINNT 0x400

#using <mscorlib.dll>
#include <tchar.h>
#include <windows.h>
#include <mscoree.h>
#include <comdef.h>

#if defined(Yield)
#undef Yield
#endif

#define CORHOST

namespace Fibers {

VOID CALLBACK unmanaged_fiberproc(PVOID pvoid);

private ref struct StopFiber {};

enum FiberStateEnum {
	FiberCreated, FiberRunning, FiberStopPending, FiberStopped
};

#pragma unmanaged

void PrintIfError(TCHAR *area, HRESULT hr)
{
	if (SUCCEEDED(hr))
		return;

	int messageSize = 64 * 1024;
	TCHAR *messageBuffer= (TCHAR*)malloc(sizeof(TCHAR) * messageSize);
	if (messageBuffer == NULL)
	{
		_tprintf(_T("Failed to allocate for printError for %s"), area);
		abort();
	}

	DWORD lastError = GetLastError();
	FormatMessage(FORMAT_MESSAGE_FROM_SYSTEM,
				  NULL,
				  lastError,
				  0,
				  messageBuffer,
				  messageSize,
				  0);

	const TCHAR *hrStr = _com_error(hr).ErrorMessage();

	_tprintf(_T("Failed %s\n\t%s\n\t%s"), area, hrStr, messageBuffer);

	abort();
}

#if defined(CORHOST)
ICorRuntimeHost *corhost;

void initialize_corhost() {
	PrintIfError(_T("CorBindToCurrentRuntime"), CorBindToCurrentRuntime(0, CLSID_CorRuntimeHost, IID_ICorRuntimeHost, (void**) &corhost));
	PrintIfError(_T("Start"), corhost->Start()); //Without SwitchOutLogicalThreadState fails on .net 4 (but not .net 3.5).
}

#endif

void CorSwitchToFiber(void *fiber) {
#if defined(CORHOST)
	DWORD *cookie;
	PrintIfError(_T("SwitchOutLogicalThreadState"), corhost->SwitchOutLogicalThreadState(&cookie));
#endif
	SwitchToFiber(fiber);
#if defined(CORHOST)
	PrintIfError(_T("SwitchInLogicalThreadState"), corhost->SwitchInLogicalThreadState(cookie));
#endif
}

#pragma managed

public delegate System::Object^ Coroutine();

public ref class Fiber abstract {
public:
#if defined(CORHOST)
	static Fiber() { initialize_corhost(); }
#endif

	Fiber() : retval(nullptr), state(FiberCreated) {
		System::IntPtr^ iptr = static_cast<System::IntPtr>(System::Runtime::InteropServices::GCHandle::Alloc(this));
		void *objptr = iptr->ToPointer();
		fiber = CreateFiber(0, unmanaged_fiberproc, objptr);
	}

	property bool IsRunning {
		bool get()
		{
		return state != FiberStopped;
		}
	}

	static operator Coroutine^(Fiber^ obj) {
		return gcnew Coroutine(obj, &Fiber::Resume);
	}

	System::Object^ Resume() {
		if(!fiber || state == FiberStopped)
			return nullptr;
		initialize_thread();
		void *current = GetCurrentFiber();
		if(fiber == current)
			return nullptr;
		previousfiber = current;
		CorSwitchToFiber(fiber);
		return retval;
	}

	~Fiber() {
		if(fiber) {
			if(state  == FiberRunning) {
				initialize_thread();
				void *current = GetCurrentFiber();
				if(fiber == current)
					return;
				previousfiber = current;
				state = FiberStopPending;
				CorSwitchToFiber(fiber);
			} else if(state == FiberCreated) {
				state = FiberStopped;
			}
			DeleteFiber(fiber);
			fiber = 0;
		}
	}

	System::Object^ GetException() {
		return exception;
	}
protected:
	virtual void Run() = 0;
	void Yield(System::Object^ obj) {
		retval = obj;
		CorSwitchToFiber(previousfiber);
		if(state == FiberStopPending)
			throw gcnew StopFiber;
	}
private:
	[System::ThreadStatic] static bool thread_is_fiber;

	void *fiber, *previousfiber;
	FiberStateEnum state;
	System::Object^ retval;
	System::Object^ exception;

	static void initialize_thread() {
		if(!thread_is_fiber) {
			ConvertThreadToFiber(0);
			thread_is_fiber = true;
		}
	}
internal:
	void* main() {
		state = FiberRunning;
		try {
			Run();
		} catch(System::Object^ x) {
			//System::Console::Error->WriteLine(
			//	S"\nFIBERS.DLL: main Caught {0}", x);
			exception = x;
		}
		state = FiberStopped;
		retval = nullptr;
		return previousfiber;
	}
};

void* fibermain(void* objptr) {
	System::IntPtr ptr = System::IntPtr(objptr);
	System::Runtime::InteropServices::GCHandle g = static_cast<System::Runtime::InteropServices::GCHandle>(ptr);
	Fiber^ fiber = static_cast<Fiber^>(g.Target);
	g.Free();
	return fiber->main();
}

#pragma unmanaged

VOID CALLBACK unmanaged_fiberproc(PVOID objptr) {
#if defined(CORHOST)
	corhost->CreateLogicalThreadState();
#endif
	void *previousfiber = fibermain(objptr);
#if defined(CORHOST)
	corhost->DeleteLogicalThreadState();
#endif
	SwitchToFiber(previousfiber);
}

} // namespace fibers
