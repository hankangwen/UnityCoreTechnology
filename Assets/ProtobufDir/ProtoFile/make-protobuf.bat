@echo off
set tool= ..\ProtoGen
set out=..\ProtoCS
  
rem ===============================================
rem  Support
set proto=common.proto
%tool%\protogen.exe -i:%proto% -o:%out%\%proto%.cs -q

set proto=Person.proto
%tool%\protogen.exe -i:%proto% -o:%out%\%proto%.cs -q
  
set proto=C2SNameRepetition.proto
%tool%\protogen.exe -i:%proto% -o:%out%\%proto%.cs -q

set proto=LoginMsg.proto
%tool%\protogen.exe -i:%proto% -o:%out%\%proto%.cs -q
pause