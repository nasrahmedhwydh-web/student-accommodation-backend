@echo off
chcp 65001 > nul
echo ========================================================
echo   رفع مشروع الباك إند المشترك (WebAPI + WebMVC) إلى GitHub
echo ========================================================
echo.
set /p REPO_URL=الرجاء إدخال رابط مستودع GitHub (مثال: https://github.com/USER/student-accommodation-backend.git): 

if "%REPO_URL%"=="" (
    echo [خطأ] لم تقم بإدخال الرابط!
    pause
    exit /b
)

git init
git add .
git commit -m "Initial commit: Complete Backend with Clean Architecture (WebAPI + WebMVC)"
git branch -M main
git remote remove origin 2>nul
git remote add origin %REPO_URL%
git push -u origin main

echo.
echo ========================================================
echo   تم الرفع بنجاح! يمكنك الآن نسخ الرابط وإرساله للدكتور.
echo ========================================================
pause
