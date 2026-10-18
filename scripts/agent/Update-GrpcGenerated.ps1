# v33g: regenerate the checked-in gRPC C# (src/UI/Server/Generated/Protos) from src/UI/Server/Protos/*.proto with Grpc.Tools'
# protoc. Round 19 s08 hand-edited the 1,000+ line generated file (77 edits, 1.71M input tokens). Output is byte-identical to the
# checked-in files for an unchanged proto (Grpc.Tools 2.70.0 and 2.76.0 both verified). Submit-Change runs this when a .proto changed.
$ErrorActionPreference = 'Stop'
Set-Location (git rev-parse --show-toplevel)
$ver = '2.70.0'
$pkgs = Join-Path $HOME '.nuget/packages/grpc.tools'
if (-not (Test-Path (Join-Path $pkgs $ver))) {
  $tmp = Join-Path ([IO.Path]::GetTempPath()) "grpctools-restore-$PID"; New-Item -ItemType Directory -Force $tmp | Out-Null
  Set-Content (Join-Path $tmp 'r.csproj') "<Project Sdk=`"Microsoft.NET.Sdk`"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup><ItemGroup><PackageReference Include=`"Grpc.Tools`" Version=`"$ver`" /></ItemGroup></Project>"
  dotnet restore (Join-Path $tmp 'r.csproj') -v q | Out-Null
  Remove-Item -Recurse -Force $tmp }
$root = Join-Path $pkgs $ver
$arch = if ((uname -m) -match 'aarch64|arm64') { 'linux_arm64' } else { 'linux_x64' }
$bin = Join-Path $root "tools/$arch"
chmod +x (Join-Path $bin 'protoc') (Join-Path $bin 'grpc_csharp_plugin')
$out = 'src/UI/Server/Generated/Protos'
foreach ($p in Get-ChildItem src/UI/Server/Protos -Filter *.proto) {
  & (Join-Path $bin 'protoc') -I src/UI/Server/Protos -I (Join-Path $root 'build/native/include') --csharp_out=$out --grpc_out=$out "--plugin=protoc-gen-grpc=$(Join-Path $bin 'grpc_csharp_plugin')" $p.Name
  if ($LASTEXITCODE) { "GRPC_GEN=FAIL $($p.Name)"; exit 0 } }
"GRPC_GEN=OK ($out regenerated; never edit those files by hand)"
