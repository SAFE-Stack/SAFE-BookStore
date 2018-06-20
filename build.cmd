@echo off
setlocal
cls

pushd %~dp0

.paket\paket.exe restore
if errorlevel 1 (
  exit /b %errorlevel%
)

if not exist ".fake\fake.exe" (
  dotnet tool install fake-cli --tool-path .\.fake
)

.fake\fake.exe build %*
if errorlevel 1 (
  exit /b %errorlevel%
)

popd
