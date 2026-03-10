# Sealed Secrets Setup

This directory contains SealedSecret manifests for UMS.
Real secrets are encrypted with the cluster's Sealed Secrets public key.

## Setup (first time)

```bash
# 1. Install Sealed Secrets controller
kubectl apply -f https://github.com/bitnami-labs/sealed-secrets/releases/latest/download/controller.yaml

# 2. Install kubeseal CLI (Windows via Chocolatey)
choco install kubeseal

# 3. Fetch cluster public key
kubeseal --fetch-cert \
  --controller-name=sealed-secrets-controller \
  --controller-namespace=kube-system \
  > pub-sealed-secrets.pem

# 4. Seal your secrets (run from repo root)
.\k8s\base\sealed-secrets\Seal-Secrets.ps1
```

## Creating a SealedSecret

```bash
kubectl create secret generic ums-secrets \
  --namespace ums \
  --from-literal=IdentityDb="Host=postgres;Port=5432;Database=identity_db;Username=ums_user;Password=NEWPASSWORD" \
  --dry-run=client -o yaml \
| kubeseal --cert pub-sealed-secrets.pem -o yaml \
> k8s/base/sealed-secrets/ums-secrets-sealed.yaml
```

Then commit `ums-secrets-sealed.yaml` — it is safe to store in git.
