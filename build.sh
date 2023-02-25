#!/usr/bin/env bash

set -eu

dotnet tool restore
dotnet paket restore
dotnet run --project build.fsproj -- "$@"

