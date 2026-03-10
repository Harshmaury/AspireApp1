# _ROTATE-CREDENTIALS.md
# ============================================================
# URGENT: Credential Rotation After Secret Exposure
# All credentials that were in k8s/base/secret.yaml are COMPROMISED.
# Complete these steps BEFORE deploying or pushing code.
# ============================================================

## 1. Rotate PostgreSQL passwords

Connect to your Minikube postgres instance and run:

```sql
-- Identity DB
ALTER USER identity_user PASSWORD 'NEW-STRONG-PASSWORD-1';

-- Shared service users
ALTER USER student_user      PASSWORD 'NEW-STRONG-PASSWORD-2';
ALTER USER academic_user     PASSWORD 'NEW-STRONG-PASSWORD-2';
ALTER USER attendance_user   PASSWORD 'NEW-STRONG-PASSWORD-2';
ALTER USER examination_user  PASSWORD 'NEW-STRONG-PASSWORD-2';
ALTER USER fee_user          PASSWORD 'NEW-STRONG-PASSWORD-2';
ALTER USER faculty_user      PASSWORD 'NEW-STRONG-PASSWORD-2';
ALTER USER hostel_user       PASSWORD 'NEW-STRONG-PASSWORD-2';
ALTER USER notification_user PASSWORD 'NEW-STRONG-PASSWORD-2';
```

## 2. Rotate OpenIddict signing/encryption keys

```bash
# Generate new signing key (RSA 2048)
openssl genrsa -out signing.pem 2048
openssl pkcs8 -topk8 -inform PEM -outform PEM -nocrypt -in signing.pem -out signing.pkcs8.pem

# Generate new encryption key
openssl rand -base64 32
```

## 3. Re-seal secrets with new credentials

```powershell
.\k8s\base\sealed-secrets\Seal-Secrets.ps1 `
    -IdentityDbPassword "NEW-STRONG-PASSWORD-1" `
    -SharedDbPassword "NEW-STRONG-PASSWORD-2" `
    -OpenIddictSigningKey "NEW-SIGNING-KEY" `
    -OpenIddictEncryptionKey "NEW-ENCRYPTION-KEY"
```

## 4. Purge git history (run from repo root)

```bash
# Install git-filter-repo
pip install git-filter-repo

# Remove file from ALL history
git filter-repo --path k8s/base/secret.yaml --invert-paths --force

# Force push (coordinate with team first)
git push origin --force --all
git push origin --force --tags
```

## 5. Revoke access from anyone who cloned before the purge

Notify all contributors to re-clone from scratch after the force push.

## 6. Verify the secret is gone

```bash
git log --all --full-history -- k8s/base/secret.yaml
# Should return empty output
```
