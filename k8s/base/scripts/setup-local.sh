#!/bin/bash
# Run this once in WSL2 to set up ingress and local DNS
# Usage: bash k8s/scripts/setup-local.sh

set -e

MINIKUBE_IP=$(minikube ip)
echo "Minikube IP: $MINIKUBE_IP"

# Apply ingress
echo "Applying ingress..."
kubectl apply -f k8s/base/infra/ingress.yaml

# Wait for ingress to be ready
echo "Waiting for ingress controller..."
kubectl wait --namespace ingress-nginx \
  --for=condition=ready pod \
  --selector=app.kubernetes.io/component=controller \
  --timeout=120s

# Add hosts to /etc/hosts (WSL2)
HOSTS="$MINIKUBE_IP api.ums.local bff.ums.local seq.ums.local jaeger.ums.local"
if grep -q "api.ums.local" /etc/hosts; then
  echo "Hosts already in /etc/hosts, updating..."
  sudo sed -i "/api.ums.local/c\\$HOSTS" /etc/hosts
else
  echo "$HOSTS" | sudo tee -a /etc/hosts
fi

echo ""
echo "✅ Setup complete! Access your services at:"
echo "   http://api.ums.local    → API Gateway"
echo "   http://bff.ums.local    → BFF"
echo "   http://seq.ums.local    → Seq Logs"
echo "   http://jaeger.ums.local → Jaeger Tracing"
echo ""
echo "⚠️  Add this to your Windows hosts file (PowerShell as Admin):"
echo "   Add-Content C:\Windows\System32\drivers\etc\hosts '$HOSTS'"