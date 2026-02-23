function ue {
    $input | Select-String -Pattern "error CS|error NU|FAILED|succeeded" |
    Where-Object { $_ -notmatch "Roslyn|LanguageServer|LanguageClient|NU1608|NativeCommandError" }
}

function ub {
    param($svc)
    $map = @{
        "academic"     = "src/Services/Academic/Academic.API/Academic.API.csproj"
        "student"      = "src/Services/Student/Student.API/Student.API.csproj"
        "identity"     = "src/Services/Identity/Identity.API/Identity.API.csproj"
        "faculty"      = "src/Services/Faculty/Faculty.API/Faculty.API.csproj"
        "attendance"   = "src/Services/Attendance/Attendance.API/Attendance.API.csproj"
        "examination"  = "src/Services/Examination/Examination.API/Examination.API.csproj"
        "fee"          = "src/Services/Fee/Fee.API/Fee.API.csproj"
        "notification" = "src/Services/Notification/Notification.API/Notification.API.csproj"
        "hostel"       = "src/Services/Hostel/Hostel.API/Hostel.API.csproj"
        "defaults"     = "src/ServiceDefaults/AspireApp1.ServiceDefaults.csproj"
        "shared"       = "src/Shared/UMS.SharedKernel/UMS.SharedKernel.csproj"
        "kafka"        = "src/Tests/Kafka.IntegrationTests/Kafka.IntegrationTests.csproj"
        "gateway"      = "src/ApiGateway/ApiGateway.csproj"
        "apphost"      = "src/Tests/AppHost.IntegrationTests/AppHost.IntegrationTests.csproj"
        "all"          = "AspireApp1.slnx"
    }
    if ($map[$svc]) { dotnet build $map[$svc] -v q 2>&1 | ue }
    else { Write-Host "Unknown: $svc. Options: $($map.Keys -join ', ')" }
}

function ut {
    param($svc)
    $map = @{
        "academic"     = "src/Services/Academic/Academic.Tests/Academic.Tests.csproj"
        "student"      = "src/Services/Student/Student.Tests/Student.Tests.csproj"
        "identity"     = "src/Services/Identity/Identity.Tests/Identity.Tests.csproj"
        "faculty"      = "src/Services/Faculty/Faculty.Tests/Faculty.Tests.csproj"
        "attendance"   = "src/Services/Attendance/Attendance.Tests/Attendance.Tests.csproj"
        "examination"  = "src/Services/Examination/Examination.Tests/Examination.Tests.csproj"
        "fee"          = "src/Services/Fee/Fee.Tests/Fee.Tests.csproj"
        "notification" = "src/Services/Notification/Notification.Tests/Notification.Tests.csproj"
        "hostel"       = "src/Services/Hostel/Hostel.Tests/Hostel.Tests.csproj"
        "kafka"        = "src/Tests/Kafka.IntegrationTests/Kafka.IntegrationTests.csproj"
        "isolation"    = "src/Tests/TenantIsolation.Tests/TenantIsolation.Tests.csproj"
        "apphost"      = "src/Tests/AppHost.IntegrationTests/AppHost.IntegrationTests.csproj"
    }
    if ($map[$svc]) {
        dotnet test $map[$svc] -v q 2>&1 |
        Select-String -Pattern "Passed|Failed|Total|error CS" |
        Where-Object { $_ -notmatch "Roslyn|LanguageServer" }
    }
    else { Write-Host "Unknown: $svc. Options: $($map.Keys -join ', ')" }
}

function ut-all {
    $map = @{
        "academic"     = "src/Services/Academic/Academic.Tests/Academic.Tests.csproj"
        "student"      = "src/Services/Student/Student.Tests/Student.Tests.csproj"
        "identity"     = "src/Services/Identity/Identity.Tests/Identity.Tests.csproj"
        "faculty"      = "src/Services/Faculty/Faculty.Tests/Faculty.Tests.csproj"
        "attendance"   = "src/Services/Attendance/Attendance.Tests/Attendance.Tests.csproj"
        "examination"  = "src/Services/Examination/Examination.Tests/Examination.Tests.csproj"
        "fee"          = "src/Services/Fee/Fee.Tests/Fee.Tests.csproj"
        "notification" = "src/Services/Notification/Notification.Tests/Notification.Tests.csproj"
        "hostel"       = "src/Services/Hostel/Hostel.Tests/Hostel.Tests.csproj"
    }
    $total = 0; $totalFailed = 0
    foreach ($svc in $map.Keys) {
        $out = dotnet test $map[$svc] -v q 2>&1 | Select-String "Passed|Failed"
        $out | ForEach-Object {
            if ($_ -match "Total:\s*(\d+)") { $total += [int]$Matches[1] }
            if ($_ -match "Failed:\s*(\d+)") { $totalFailed += [int]$Matches[1] }
        }
        Write-Host "$svc ? $out"
    }
    Write-Host "`n=== TOTAL: $total | FAILED: $totalFailed ==="
}

function ut_identity_int { dotnet test 'src/Tests/Identity.IntegrationTests/Identity.IntegrationTests.csproj' -v q }


