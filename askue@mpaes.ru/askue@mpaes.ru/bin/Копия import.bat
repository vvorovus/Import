echo off

for %%i in (..\*.xml) do @call :do_import2 %%i

exit


:unpack
echo %~nx1
rem Генерация нового имени временного каталога
call newGUID.bat
set root=%~dp1
set tmp=%root%%guid%
mkdir %tmp%
rem Распаковка в новый каталог пачки
7za -y -r0 -o"%tmp%" e "%~1" >nul

rem Импортирование данных
set error=0
for %%j in (%tmp%\*.xml) do call :do_import %%j

rem Очистка временных файлов
erase /f /q %tmp%\*.*
rmdir %tmp%

if %error% GEQ 1 exit /b
move "%~1" %root%imported\

exit /b


:do_import
echo   %~nx1
import.exe %1 -recalc
if errorlevel 1 set error=%errorlevel%
exit /b


:do_import1
echo %~nx1
import.exe %1
if errorlevel 1 exit /b
move "%~1" %root%Температура\
exit /b


:do_import2
set root=%~dp1
echo %~nx1
import.exe %1 -recalc
if errorlevel 1 exit /b
move %~1 %root%imported\
exit /b

:do_import3
set root=%~dp1
echo %~nx1
import.exe %1
if errorlevel 1 exit /b
rem move "%~1" %root%imported\80020\
exit /b

:unpack2
echo %~nx1
set root=%~dp1
rem Распаковка в новый каталог пачки
unrar e "%~1" *.* %root%
if errorlevel 1 exit /b
move "%~1" %root%imported\
exit /b


:do_import4
echo %~nx1
import.exe %1 -recalc
if errorlevel 1 exit /b
move "%~1" ..\imported\com\
exit /b


:do_importXLS
echo %~nx1
import.exe "%~1"
if errorlevel 1 exit /b
move "%~1" ..\Excel\
exit /b

:do_importFob1
echo %~nx1
import.exe %~1
call 7zdate %1
if errorlevel 1 exit /b
erase %1
@move %~dp1*.7z ..\imported\1day\ >nul
exit /b

:do_importFob3
echo %~nx1
import.exe %~1
call 7zdate %1
if errorlevel 1 exit /b
erase %1
move %~dp1*.7z ..\imported\3days\ >nul
exit /b