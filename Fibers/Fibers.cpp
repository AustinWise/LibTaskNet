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

typedef System::Runtime::InteropServices::GCHandle GCHandle;

VOID CALLBACK unmanaged_fiberproc(PVOID pvoid);

__gc private struct StopFiber {};

enum FiberStateEnum {
	FiberCreated, FiberRunning, FiberStopPending, FiberStopped
};

#pragma unmanaged

#if defined(CORHOST)
ICorRuntimeHost *corhost;

void initialize_corhost() {
	CorBindToCurrentRuntime(0, CLSID_CorRuntimeHost,
		IID_ICorRuntimeHost, (void**) &corhost);
}

#endif

void CorSwitchToFiber(void *fiber) {
#if defined(CORHOST)
	DWORD *cookie;
	corhost->SwitchOutLogicalThreadState(&cookie);
#endif
	SwitchToFiber(fiber);
#if defined(CORHOST)
	corhost->SwitchInLogicalThreadState(cookie);
#endif
}

#pragma managed

public __delegate System::Object* Coroutine();

__gc __abstract public class Fiber : public System::IDisposable {
public:
#if defined(CORHOST)
	static Fiber() { initialize_corhost(); }
#endif

	Fiber() : retval(0), state(FiberCreated) {
		void *objptr = (void*) GCHandle::op_Explicit(GCHandle::Alloc(this));
		fiber = CreateFiber(0, unmanaged_fiberproc, objptr);
	}

	__property bool get_IsRunning() {
		return state != FiberStopped;
	}

	static Coroutine* op_Implicit(Fiber *obj) {
		return new Coroutine(obj, &Fiber::Resume);
	}

	System::Object* Resume() {
		if(!fiber || state == FiberStopped)
			return 0;
		initialize_thread();
		void *current = GetCurrentFiber();
		if(fiber == current)
			return 0;
		previousfiber = current;
		CorSwitchToFiber(fiber);
		return retval;
	}

	void Dispose() {
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

	System::Object *GetException() {
		return exception;
	}
protected:
	virtual void Run() = 0;
	void Yield(System::Object *obj) {
		retval = obj;
		CorSwitchToFiber(previousfiber);
		if(state == FiberStopPending)
			throw new StopFiber;
	}
private:
	[System::ThreadStatic] static bool thread_is_fiber;

	void *fiber, *previousfiber;
	FiberStateEnum state;
	System::Object *retval;
	System::Object *exception;

	static void initialize_thread() {
		if(!thread_is_fiber) {
			ConvertThreadToFiber(0);
			thread_is_fiber = true;
		}
	}
private public:
	void* main() {
		state = FiberRunning;
		try {
			Run();
		} catch(System::Object *x) {
			//System::Console::Error->WriteLine(
			//	S"\nFIBERS.DLL: main Caught {0}", x);
			exception = x;
		}
		state = FiberStopped;
		retval = 0;
		return previousfiber;
	}
};

void* fibermain(void* objptr) {
	System::IntPtr ptr = (System::IntPtr) objptr;
	GCHandle g = GCHandle::op_Explicit(ptr);
	Fiber *fiber = static_cast<Fiber*>(g.Target);
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
