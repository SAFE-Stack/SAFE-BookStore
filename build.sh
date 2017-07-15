#!/usr/bin/env bash

set -eu

cd "$(dirname "$0")"

PAKET_EXE=.paket/paket.exe
FAKE_EXE=packages/build/FAKE/tools/FAKE.exe

FSIARGS=""
FSIARGS2=""
OS=${OS:-"unknown"}
if [ "$OS" != "Windows_NT" ]
then
  # Can't use FSIARGS="--fsiargs -d:MONO" in zsh, so split it up
  # (Can't use arrays since dash can't handle them)
  FSIARGS="--fsiargs"
  FSIARGS2="-d:MONO"
fi

run() {
  if [ "$OS" != "Windows_NT" ]
  then
    mono "$@"
  else
    "$@"
  fi
}

run $PAKET_EXE restore


[ ! -e build.fsx ] && run $PAKET_EXE update
[ ! -e build.fsx ] && run $FAKE_EXE init.fsx
run $FAKE_EXE "$@" $FSIARGS $FSIARGS2 build.fsx

