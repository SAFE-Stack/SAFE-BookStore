@ECHO OFF

IF NOT EXIST ".fake\install\fake.exe" (
  md .fake
  dotnet tool install fake-cli ^
    --tool-path .fake/install
)

".fake\install\fake.exe" build %*