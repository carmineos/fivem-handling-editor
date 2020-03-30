# Handling Editor
|Master|Development|
|:-:|:-:|
|[![Build status](https://ci.appveyor.com/api/projects/status/ar8rag9opc4169y1/branch/master?svg=true)](https://ci.appveyor.com/project/carmineos/fivem-handling-editor/branch/master)|[![Build status](https://ci.appveyor.com/api/projects/status/ar8rag9opc4169y1/branch/development?svg=true)](https://ci.appveyor.com/project/carmineos/fivem-handling-editor/branch/development)

### Description
A script which allows to edit the handling of each vehicle individually using FiveM API and [MenuAPI](https://github.com/TomGrobbe/MenuAPI) .

When a client edits a vehicle, it will be automatically synchronized with all the players using decorators.

The default key to open the menu is F7

### Features
* Edit the values of the handling's fields (scroll the list or press Enter to insert custom values)
* Customizable min and max allowed values for each field
* Load presets from a server resource file
* Save and Load presets locally for each client

### Supported Classes
* CHandlingData

### Supported Types
* Float
* Vector3

### Other Limitations
* Some fields don't seem to have any effect after being edited
* Some fields require the car to move a bit or to be damaged to update after being edited
* Some Int fields have a wrong type in the ExtraNative, so I disabled them ( nInitialDriveGears )
* Flags fields aren't supported yet
* Some of these fields above work if edited globally (but editing globally doesn't give the smoothest experience, if more clients edit a fields in different way)

### Client Commands
`handling_preset`
Prints the preset of the current vehicle

`handling_decorators`
Prints the info about decorators on the current vehicle

`handling_decorators <int>` 
Prints the info about decorators on the vehicle with the specified int as local handle

`handling_print`
Prints the list of all the vehicles with any decorator of this script

`handling_range <float>`
Sets the specified float as the maximum distance used to refresh wheels of the vehicles with decorators

`handling_debug <bool>`
Enables or disables the logs to be printed in the console

### Handling Presets
The `HandlingPresets.xml` is the file which contains the handlings you want to preload for each client. The script will load them and each client will be able to apply them to their current vehicle. I will add an option to let the client use each preset only if the model of the vehicle is the same.
**NOTE**: The presets require an attribute called `presetName` to be loaded by the script, check the included example.

### Handling Info
The `HandlingInfo.xml` is the file which controls how the script handles each handling field. You can set for each field if you want it to be editable by the clients of your server by editing the `Editable` field. You can also set custom `Min` and `Max` allowed values and edit the `Description` of each field.

### Config
`toggleMenu=168`
The Control to toggle the Menu, default is 168 which is F7 (check the [controls list](https://docs.fivem.net/game-references/controls/))

`FloatStep=0.01`
The step used to increase and decrease a value

`ScriptRange=150.0`
The max distance within which each client refreshes others clients' vehicles

`timer=1000`
The value in milliseconds used by each client to check if its preset requires to be synched again

`debug=false`
Enables the debug mode, which prints some logs in the console

[Source](https://github.com/carmineos/fivem-handling-editor)
[Download](https://github.com/carmineos/fivem-handling-editor/releases)
I am open to any kind of feedback. Report suggestions and bugs you find.

### Build
Open the `postbuild.bat` and edit the path of the resource folder. If in Debug configuration, the post build event will copy the following files to the specified path: the script, the `config.ini`, the `HandlingInfo.xml`, the `HandlingPresets.xml`, the `__resource.lua` and a copy of a built [MenuAPI](https://github.com/TomGrobbe/MenuAPI)

### Credits
* FiveM by CitizenFX: https://github.com/citizenfx/fivem
* MenuAPI by Vespura: https://github.com/TomGrobbe/MenuAPI
* GTADrifting members: https://gtad.club/
* ikt's Handling Editor: https://github.com/E66666666/GTAVHandlingEditor
* All the testers

### TODO

#### Update handling-loader-five component to use RageParser from gta:core:five
* https://github.com/citizenfx/fivem/blob/master/code/components/handling-loader-five/
* https://github.com/citizenfx/fivem/blob/master/code/components/gta-core-five/include/RageParser.h

#### Fix wrong parMemberType for nInitialDriveGears
* https://github.com/citizenfx/fivem/blob/master/code/components/handling-loader-five/src/HandlingLoader.cpp#L72

#### Add an extra native to get the handling by modelHash
references: 
* https://github.com/citizenfx/fivem/blob/871e449a8eecc3a9b4d5fc48d2cfc2e1823552fb/code/components/extra-natives-five/src/RuntimeAssetNatives.cpp#L886
* https://github.com/E66666666/GTAVHandlingEditor/blob/master/ProjectName/Memory/NativeMemory.cpp#L27
* https://github.com/E66666666/GTAVAddonLoader/blob/master/GTAVAddonLoader/NativeMemory.hpp#L163
```c
CHandlingData* GetHandlingDataByModelHash(uint32_t modelHash)
{
    CVehicleModelInfo* modelInfo = (CVehicleModelInfo*)GetModelInfo(modelHash);
    if (modelInfo && modelInfo->IsVehicle())
    {
        return GetHandlingDataByIndex(modelInfo->m_handlingIndex);
    }

    return nullptr;
}

// usage
CHandlingData* handlingData = GetHandlingDataByModelHash(joaat("adder"));
uint32_t handlingHash = handlingData->m_handlingNameHash;
```

#### Add GET_HANDLING_* extra natives (by using the GetHandlingDataByModelHash mentioned above)
#### Update SET_HANDLING_* to use GetHandlingDataByModelHash instead of iterating handlings

#### Add an extra native to get SubHandlingData
```c
__int64 __fastcall CHandlingData::GetSubHandlingDataByType(__int64 this, int type)
{
  int _type; // ebp
  __int64 _this; // rsi
  unsigned int i; // edi
  __int64 v5; // rbx

  _type = type;
  _this = this;
  i = 0;
  if ( *(_WORD *)(this + 0x160) <= 0u )
    return 0i64;
  while ( 1 )
  {
    v5 = *(_QWORD *)(*(_QWORD *)(_this + 0x158) + 8i64 * i);
    if ( v5 )
    {
      if ( _type == (*(unsigned int (__fastcall **)(__int64))(*(_QWORD *)v5 + 0x10i64))(v5) )
        break;
    }
    if ( (signed int)++i >= *(unsigned __int16 *)(_this + 0x160) )
      return 0i64;
  }
  return v5;
}

//It's at 66 44 3B B1 ? ? ? ? 73 2D - 0x24
```