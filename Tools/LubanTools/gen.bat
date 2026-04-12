set CONF_ROOT=.
set UNITY_DATA_PATH=%1
set UNITY_CODE_PATH=%2
set LUBAN_DLL=%CONF_ROOT%\Luban\Luban.dll
set LUBAN_CONFIG=%CONF_ROOT%\DataTables\luban.conf

dotnet %LUBAN_DLL% ^
    -t all ^
    -c cs-bin ^
    -d bin ^
    --conf %LUBAN_CONFIG% ^
    -x outputCodeDir=%UNITY_CODE_PATH% ^
    -x outputDataDir=%UNITY_DATA_PATH%  ^
    -x lineEnding=CRLF

pause
