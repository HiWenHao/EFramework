#include <codegen/il2cpp-codegen-metadata.h>
#include "vm/ClassInlines.h"
#include "vm/Object.h"
#include "vm/Class.h"

#include "../metadata/MetadataModule.h"
#include "../metadata/MetadataUtil.h"

#include "../interpreter/MethodBridge.h"
#include "../interpreter/Interpreter.h"
#include "../interpreter/MemoryUtil.h"
#include "../interpreter/InstrinctDef.h"

using namespace hybridclr::interpreter;

#if HYBRIDCLR_ABI_UNIVERSAL_32
//!!!{{CODE

Managed2NativeMethodInfo hybridclr::interpreter::g_managed2nativeStub[] = 
{

	{nullptr, nullptr},
};

Native2ManagedMethodInfo hybridclr::interpreter::g_native2managedStub[] = 
{

	{nullptr, nullptr},
};

NativeAdjustThunkMethodInfo hybridclr::interpreter::g_adjustThunkStub[] = 
{

	{nullptr, nullptr},
};

//!!!}}CODE
#endif
