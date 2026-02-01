@echo off
chcp 65001 >nul
echo ============================================
echo            宏录制器
echo ============================================
echo.

REM 检查 Python 是否安装
python --version >nul 2>&1
if errorlevel 1 (
    echo [错误] 未检测到 Python，请先安装 Python 3.8+
    echo 下载地址: https://www.python.org/downloads/
    pause
    exit /b 1
)

REM 安装依赖
echo [1/2] 检查 pynput...
pip show pynput >nul 2>&1
if errorlevel 1 (
    echo        正在安装 pynput...
    pip install pynput >nul 2>&1
)

REM 运行应用
echo [2/2] 启动宏录制器...
echo.
echo 如果程序无响应，请确保以管理员身份运行
echo.
cd /d "%~dp0"
python macro_recorder.py
