// This is the main DLL file.

#include "stdafx.h"
#define _WIN32_WINNT 0x400

#using <mscorlib.dll>
#include <windows.h>
#include <mscoree.h>

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

#if defined(CORHOST)
ICorRuntimeHost *corhost;

void initialize_corhost() {
	HRESULT hr = CorBindToCurrentRuntime(0, CLSID_CorRuntimeHost, IID_ICorRuntimeHost, (void**) &corhost);
	if (!SUCCEEDED(hr))
		abort();
}

#endif

void CorSwitchToFiber(void *fiber) {
#if defined(CORHOST)
	DWORD *cookie;
	HRESULT hr = corhost->SwitchOutLogicalThreadState(&cookie);
	if (!SUCCEEDED(hr))
		abort();
#endif
	SwitchToFiber(fiber);
#if defined(CORHOST)
	hr = corhost->SwitchInLogicalThreadState(cookie);
	if (!SUCCEEDED(hr))
		abort();
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
