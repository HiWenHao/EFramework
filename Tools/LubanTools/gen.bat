set WORKSPACE=../..
set CONF_ROOT=.
set UNITY_ASSETS_PATH=%1
set LUBAN_DLL=%CONF_ROOT%\Luban\Luban.dll
set LUBAN_CONFIG=%CONF_ROOT%\DataTables\luban.conf

dotnet %LUBAN_DLL% ^
    -t all ^
    -c cs-bin ^
    -d bin ^
    --conf %LUBAN_CONFIG% ^
    -x outputCodeDir=%UNITY_ASSETS_PATH%\Code ^
    -x outputDataDir=%UNITY_ASSETS_PATH%\Json  ^
    -x lineEnding=CRLF

pause
