set WORKSPACE=../..
set CONF_ROOT=.
set UNITY_ASSETS_PATH=%WORKSPACE%\Unity\Assets\Luban
set LUBAN_DLL=%CONF_ROOT%\LubanRelease\Luban.dll
set LUBAN_CONFIG=%CONF_ROOT%\DataTables\Lc_Luban.conf

:: -c cs-simple-json ^ 
dotnet %LUBAN_DLL% ^
    -t all ^

    -d json ^
    --conf %LUBAN_CONFIG% ^
    -x outputCodeDir=%UNITY_ASSETS_PATH%\LC\Code ^
    -x outputDataDir=%UNITY_ASSETS_PATH%\LC\Json  ^
    -x lineEnding=CRLF

pause
