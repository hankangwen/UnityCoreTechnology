@echo off
set tool= ..\ProtoGen
  
rem ===============================================
rem  Support
set proto=common.proto
%tool%\protogen.exe -i:%proto% -o:%proto%.cs -q
  
set proto=Person.proto
%tool%\protogen.exe -i:%proto% -o:%proto%.cs -q
  
set proto=C2SNameRepetition.proto
%tool%\protogen.exe -i:%proto% -o:%proto%.cs -q
pause