@echo off
chcp 65001 > nul
echo ===================================================
echo Packaging MoeLiveTranslator (Minimal Release)
echo ===================================================

:: Set paths to keep root directory clean
:: Output build files to bin\Publish\Minimal
set OUT_DIR=bin\Publish\Minimal
:: Output final ZIP to Releases folder in root
set RELEASES_DIR=bin\Publish\zip
set ZIP_NAME=%RELEASES_DIR%\MoeLiveTranslator.zip

echo [1/5] Preparing directories / 准备目录...
if exist "%OUT_DIR%" rd /s /q "%OUT_DIR%"
if not exist "%RELEASES_DIR%" mkdir "%RELEASES_DIR%"
if exist "%ZIP_NAME%" del "%ZIP_NAME%"

echo [2/5] Building Release / 正在编译...
:: -p:ExcludeWhisperModels=true: Skip copying large .bin files
:: -p:DebugType=None: Do not generate .pdb files (Debug symbols from Step 102 request: not needed)
dotnet publish LiveTranslator.csproj ^
  -c Release ^
  -r win-x64 ^
  --self-contained false ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:ExcludeWhisperModels=true ^
  -p:DebugType=None ^
  -p:DebugSymbols=false ^
  -o "%OUT_DIR%"

if %errorlevel% neq 0 (
    echo [ERROR] Build failed! 编译失败
    pause
    exit /b %errorlevel%
)

echo [3/5] Post-processing / 后处理...
:: 1. Create empty WhisperModels folder
if not exist "%OUT_DIR%\WhisperModels" mkdir "%OUT_DIR%\WhisperModels"
echo Place your downloaded model files (e.g. ggml-small.bin) in this folder. > "%OUT_DIR%\WhisperModels\PUT_MODELS_HERE.txt"

:: 2. Copy documentation
copy /Y README.md "%OUT_DIR%\" > nul
copy /Y PROMPTS.md "%OUT_DIR%\" > nul

:: 3. Clean up any PDBs if they exist
del /Q "%OUT_DIR%\*.pdb" 2>nul
:: Remove the created PDB for the app itself if dotnet publish made it despite flags
del /Q "%OUT_DIR%\MoeLiveTranslator.pdb" 2>nul

echo [4/5] Zipping to %ZIP_NAME% / 正在压缩...
powershell -Command "Compress-Archive -Path '%OUT_DIR%\*' -DestinationPath '%ZIP_NAME%' -Force"

echo ===================================================
echo Success! 
echo Location: %ZIP_NAME%
echo ===================================================
pause
