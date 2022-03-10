pushd %~dp0

set zipfile="%~dp0/Cedev-AmmoLocker.zip"
set zip="c:\program files\7-zip\7z.exe"

del %zipfile%
%zip% a %zipfile% manifest.json
%zip% a %zipfile% README.md
%zip% a %zipfile% icon.png
%zip% a %zipfile% languages

pushd "../../Unity/AmmoLocker/Assets/AssetBundles/"
%zip% a %zipfile% assets
popd

pushd "bin/Release/netstandard2.0"
%zip% a %zipfile% AmmoLocker.dll
popd

popd
