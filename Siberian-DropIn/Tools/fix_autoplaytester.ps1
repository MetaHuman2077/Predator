# Задача 2: чинит известные ошибки компиляции в AutoPlaytester.cs
#   1) удаляет строку с несуществующим атрибутом [DefaultExecutionTag(...)]
#   2) Time.realtimeSinceSeconds -> Time.realtimeSinceStartup (5 вхождений)
# Запуск из корня Unity-проекта Siberian:
#   powershell -ExecutionPolicy Bypass -File Siberian-DropIn\Tools\fix_autoplaytester.ps1

$ErrorActionPreference = "Stop"

$candidates = Get-ChildItem -Path "Assets" -Recurse -Filter "AutoPlaytester.cs" -ErrorAction SilentlyContinue
if (-not $candidates) {
    Write-Error "AutoPlaytester.cs не найден под Assets/. Запускайте скрипт из корня Unity-проекта."
}

foreach ($file in $candidates) {
    $path = $file.FullName
    $original = Get-Content -Path $path -Raw -Encoding UTF8
    $patched = $original

    # 1) Атрибут [DefaultExecutionTag] не существует в Unity API — удаляем строку целиком.
    $patched = ($patched -split "`n" | Where-Object { $_ -notmatch '\[\s*DefaultExecutionTag' }) -join "`n"

    # 2) Опечатка API: realtimeSinceSeconds не существует.
    $wrongCount = ([regex]::Matches($patched, 'Time\.realtimeSinceSeconds')).Count
    $patched = $patched -replace 'Time\.realtimeSinceSeconds', 'Time.realtimeSinceStartup'

    if ($patched -ne $original) {
        Copy-Item -Path $path -Destination "$path.bak" -Force
        Set-Content -Path $path -Value $patched -Encoding UTF8 -NoNewline
        Write-Host "OK: $path — атрибут удалён, realtimeSinceSeconds заменён ($wrongCount вхожд.). Бэкап: $path.bak"
    } else {
        Write-Host "SKIP: $path — правки не требуются (уже исправлен?)"
    }
}

Write-Host "Готово. Откройте Unity и убедитесь, что консоль чистая (Clear -> нет compile errors)."
