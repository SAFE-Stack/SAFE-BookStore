#!/bin/bash

fake_path=".fake/install/fake.exe"
if [ "$OS" != "Windows_NT" ]; then
  fake_path=".fake/install/fake"
fi

if [ ! -f "./$fake_path" ]; then
  mkdir .fake
  dotnet tool install fake-cli --tool-path .fake/install
fi


./$fake_path build $@