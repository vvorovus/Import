echo off
for %%i in (..\*.xml) do @call :do_import %%i
for %%i in (..\*.xls) do @call :do_import %%i
exit


:do_import
echo   %~nx1
import.exe %1
exit /b
