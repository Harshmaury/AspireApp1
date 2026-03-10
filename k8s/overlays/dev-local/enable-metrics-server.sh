#!/bin/bash
# k8s/overlays/dev-local/enable-metrics-server.sh
# Enables the Minikube metrics-server addon required for HPA on identity-api.
#
# Run once after `minikube start`:
#   bash k8s/overlays/dev-local/enable-metrics-server.sh

set -e

echo "Enabling metrics-server addon..."
minikube addons enable metrics-server

echo "Waiting for metrics-server to be ready..."
kubectl wait --for=condition=available deployment/metrics-server \
  -n kube-system --timeout=90s

echo "metrics-server ready. HPA on identity-api will now have CPU metrics."
echo "Verify with: kubectl top pods -n ums"
