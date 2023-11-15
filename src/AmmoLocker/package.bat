pushd %~dp0

mkdir dist

set zipfile="%~dp0dist/Cedev-AmmoLocker.zip"
set unetweaver="%~dp0../NetworkWeaver/Unity.UNetWeaver.exe"
set zip="c:/program files/7-zip/7z.exe"

del "dist/AmmoLocker.dll"

%unetweaver% "%~dp0../libs/UnityEngine.CoreModule.dll" "%~dp0../libs/com.unity.multiplayer-hlapi.Runtime.dll" "%~dp0dist/" "%~dp0bin/Release/netstandard2.0/AmmoLocker.dll" "%~dp0bin/Release/netstandard2.0/"

del %zipfile%
%zip% a %zipfile% manifest.json
%zip% a %zipfile% README.md
%zip% a %zipfile% icon.png
%zip% a %zipfile% languages

pushd "../../Unity/AmmoLocker/Assets/AssetBundles/"
%zip% a %zipfile% assets
popd

pushd "dist"
%zip% a %zipfile% AmmoLocker.dll
popd

popd
