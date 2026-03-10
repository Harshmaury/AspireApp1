# k8s/base/sealed-secrets/Seal-Secrets.ps1
# Run this script to (re)seal all UMS secrets after credential rotation.
# Requires: kubectl, kubeseal, pub-sealed-secrets.pem in this directory
#
# Usage: .\k8s\base\sealed-secrets\Seal-Secrets.ps1

param(
    [Parameter(Mandatory=$true)]
    [string]$IdentityDbPassword,

    [Parameter(Mandatory=$true)]
    [string]$SharedDbPassword,

    [Parameter(Mandatory=$true)]
    [string]$OpenIddictSigningKey,

    [Parameter(Mandatory=$true)]
    [string]$OpenIddictEncryptionKey
)

$certPath = "$PSScriptRoot\pub-sealed-secrets.pem"
if (-not (Test-Path $certPath)) {
    Write-Error "pub-sealed-secrets.pem not found. Run: kubeseal --fetch-cert ... > pub-sealed-secrets.pem"
    exit 1
}

$host_ = "postgres-svc.ums.svc.cluster.local"

$connections = @{
    IdentityDb     = "Host=$host_;Port=5432;Database=identity_db;Username=identity_user;Password=$IdentityDbPassword"
    StudentDb      = "Host=$host_;Port=5432;Database=student_db;Username=student_user;Password=$SharedDbPassword"
    AcademicDb     = "Host=$host_;Port=5432;Database=academic_db;Username=academic_user;Password=$SharedDbPassword"
    AttendanceDb   = "Host=$host_;Port=5432;Database=attendance_db;Username=attendance_user;Password=$SharedDbPassword"
    ExaminationDb  = "Host=$host_;Port=5432;Database=examination_db;Username=examination_user;Password=$SharedDbPassword"
    FeeDb          = "Host=$host_;Port=5432;Database=fee_db;Username=fee_user;Password=$SharedDbPassword"
    FacultyDb      = "Host=$host_;Port=5432;Database=faculty_db;Username=faculty_user;Password=$SharedDbPassword"
    HostelDb       = "Host=$host_;Port=5432;Database=hostel_db;Username=hostel_user;Password=$SharedDbPassword"
    NotificationDb = "Host=$host_;Port=5432;Database=notification_db;Username=notification_user;Password=$SharedDbPassword"
    OpenIddictSigningKey    = $OpenIddictSigningKey
    OpenIddictEncryptionKey = $OpenIddictEncryptionKey
}

# Build kubectl create secret command
$literalArgs = $connections.GetEnumerator() |
    ForEach-Object { "--from-literal=$($_.Key)=$($_.Value)" }

$tmpSecretYaml = [System.IO.Path]::GetTempFileName()
$sealedOutput  = "$PSScriptRoot\ums-secrets-sealed.yaml"

try {
    $kubectlArgs = @("create","secret","generic","ums-secrets","--namespace","ums","--dry-run=client","-o","yaml") + $literalArgs
    & kubectl @kubectlArgs | & kubeseal --cert $certPath -o yaml > $sealedOutput
    Write-Host "SealedSecret written: $sealedOutput" -ForegroundColor Green
    Write-Host "Commit this file and apply: kubectl apply -f $sealedOutput" -ForegroundColor Cyan
} finally {
    Remove-Item $tmpSecretYaml -ErrorAction SilentlyContinue
}
