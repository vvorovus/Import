echo off

for %%i in (..\Forecast\1 day\*.txt) do @call :do_importFob1 "%%i"


exit


:do_importFob1
echo %~nx1
import.exe "%~1"
if errorlevel 1 exit /b
exit /b