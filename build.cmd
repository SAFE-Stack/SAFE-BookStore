@echo off
cls

.paket\paket.bootstrapper.exe 4.0.0-alpha045
if errorlevel 1 (
  exit /b %errorlevel%
)

.paket\paket.exe restore
if errorlevel 1 (
  exit /b %errorlevel%
)

IF NOT EXIST build.fsx (
  .paket\paket.exe update
  packages\build\FAKE\tools\FAKE.exe init.fsx
)
packages\build\FAKE\tools\FAKE.exe build.fsx %*
