#!/bin/bash
set -e
MINIKUBE_IP=$(minikube ip)
echo "Minikube IP: $MINIKUBE_IP"

HOSTS="$MINIKUBE_IP api.ums.local bff.ums.local seq.ums.local jaeger.ums.local"
if grep -q "api.ums.local" /etc/hosts; then
  sudo sed -i "/api.ums.local/c\\$HOSTS" /etc/hosts
  echo "Updated existing hosts entry"
else
  echo "$HOSTS" | sudo tee -a /etc/hosts
  echo "Added hosts entry"
fi

echo ""
echo "✅ Done! Your services:"
echo "   http://api.ums.local"
echo "   http://bff.ums.local"
echo "   http://seq.ums.local"
echo "   http://jaeger.ums.local"
