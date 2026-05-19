@echo off
"C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe" ^
  /target:winexe ^
  /out:SimpleTodo.exe ^
  /optimize+ ^
  /debug- ^
  /reference:System.dll ^
  /reference:System.Core.dll ^
  /reference:System.Drawing.dll ^
  /reference:System.Windows.Forms.dll ^
  /reference:System.Web.Extensions.dll ^
  Program.cs ^
  TaskItem.cs ^
  TaskStore.cs ^
  TaskRow.cs ^
  TaskListPanel.cs ^
  MainForm.cs ^
  ModernCheckBox.cs ^
  NativeMethods.cs

if %ERRORLEVEL% EQU 0 (
  echo.
  echo [OK] SimpleTodo.exe built successfully.
  echo Size: %~z0 bytes
) else (
  echo.
  echo [FAIL] Build failed.
)
pause
