echo off
main.py

for %%i in (..\*.zip) do @call :unpack %%i
for %%i in (..\d*.xml) do @call :do_import1 %%i
for %%i in (..\*.rar) do @call :unpack2 %%i
for %%i in (..\*.xml) do @call :do_import2 "%%i"

for %%i in (..\com\*.xml) do @call :do_import4 %%i
for %%i in (..\*.xls) do @call :do_importXLS "%%i"

exit


:unpack
echo %~nx1
rem ������� ������ ����� �६������ ��⠫���
call newGUID.bat
set root=%~dp1
set tmp=%root%%guid%
mkdir %tmp%
rem ��ᯠ����� � ���� ��⠫�� ��窨
7za -y -r0 -o"%tmp%" e "%~1" >nul

rem ������஢���� ������
for %%j in (%tmp%\*.xml) do call :do_import %%j

rem ���⪠ �६����� 䠩���
erase /f /q %tmp%\*.*
rmdir %tmp%

move "%~1" %root%imported\

exit /b


:do_import
echo   %~nx1
import.exe %1
exit /b


:do_import1
echo %~nx1
import.exe %1
if errorlevel 1 exit /b
move "%~1" %root%���������\
exit /b


:do_import2
set root=%~dp1
echo %~nx1
import.exe %1
if errorlevel 1 exit /b
move "%~1" %root%imported\
exit /b


:unpack2
echo %~nx1
set root=%~dp1
rem ��ᯠ����� � ���� ��⠫�� ��窨
unrar e "%~1" *.* %root%
if errorlevel 1 exit /b
move "%~1" %root%imported\
exit /b


:do_import4
echo %~nx1
import.exe %1
if errorlevel 1 exit /b
move "%~1" ..\imported\com\
exit /b


:do_importXLS
echo %~nx1
import.exe "%~1"
if errorlevel 1 exit /b
move "%~1" ..\Excel\
exit /b