pushd %~dp0

set zipfile="%~dp0/Cedev-VoidSacrifice.zip"
set zip="c:\program files\7-zip\7z.exe"

del %zipfile%
%zip% a %zipfile% manifest.json
%zip% a %zipfile% README.md
%zip% a %zipfile% icon.png
pushd "bin/Release/netstandard2.0"
%zip% a %zipfile% VoidSacrifice.dll
popd

popd
