namespace Ums.Cli.Commands;

using System.CommandLine;
using Ums.Cli.Infrastructure;

public static class InfraCommands
{
    public static Command Build()
    {
        var infra = new Command("infra", "Kubernetes, deployments, port-forwards, logs");
        infra.AddCommand(BuildStart());
        infra.AddCommand(BuildStop());
        infra.AddCommand(BuildRestart());
        infra.AddCommand(BuildDeploy());
        infra.AddCommand(BuildLogs());
        infra.AddCommand(BuildStatus());
        infra.AddCommand(BuildWatch());
        infra.AddCommand(BuildRecovery());
        infra.AddCommand(BuildScan());
        infra.AddCommand(BuildOpen());
        infra.AddCommand(BuildHealth());
        infra.AddCommand(BuildScale());
        return infra;
    }

    private static Command BuildStart()
    {
        var cmd = new Command("start", "Start Minikube, port-forwards, Kafka topics, GH runner");
        cmd.SetHandler(() => BashBridge.Run("ums-engine.sh", "start"));
        return cmd;
    }

    private static Command BuildStop()
    {
        var cmd  = new Command("stop", "Stop port-forwards (optionally Minikube)");
        var hard = new Option<bool>("--hard", "Also stop Minikube");
        cmd.AddOption(hard);
        cmd.SetHandler((h) =>
            BashBridge.Run("ums-engine.sh", h ? "stop --hard" : "stop"), hard);
        return cmd;
    }

    private static Command BuildRestart()
    {
        var cmd  = new Command("restart", "Stop then start the full stack");
        var hard = new Option<bool>("--hard", "Also restart Minikube");
        cmd.AddOption(hard);
        cmd.SetHandler((h) =>
            BashBridge.Run("ums-engine.sh", h ? "restart --hard" : "restart"), hard);
        return cmd;
    }

    private static Command BuildDeploy()
    {
        var cmd = new Command("deploy", "Build Docker image and deploy a service to Minikube");
        var svc = new Argument<string>("service", "K8s deployment name, e.g. student-api");
        cmd.AddArgument(svc);
        cmd.SetHandler((s) =>
            BashBridge.Run("ums-engine.sh", $"deploy {s}"), svc);
        return cmd;
    }

    private static Command BuildLogs()
    {
        var cmd = new Command("logs", "Stream logs from a service");
        var svc = new Argument<string?>("service", () => null, "K8s deployment name");
        cmd.AddArgument(svc);
        cmd.SetHandler((s) =>
            BashBridge.Run("ums-engine.sh", s != null ? $"logs {s}" : "logs"), svc);
        return cmd;
    }

    private static Command BuildStatus()
    {
        var cmd = new Command("status", "Live dashboard");
        cmd.SetHandler(() => BashBridge.Run("ums-engine.sh", "status"));
        return cmd;
    }

    private static Command BuildWatch()
    {
        var cmd = new Command("watch", "Auto-deploy on file-save");
        cmd.SetHandler(() => BashBridge.Run("ums-engine.sh", "watch"));
        return cmd;
    }

    private static Command BuildRecovery()
    {
        var cmd = new Command("recovery", "Auto-detect and fix common failures");
        cmd.SetHandler(() => BashBridge.Run("ums-engine.sh", "recovery"));
        return cmd;
    }

    private static Command BuildScan()
    {
        var cmd = new Command("scan", "Deep project scan");
        cmd.SetHandler(() => BashBridge.Run("ums-engine.sh", "scan"));
        return cmd;
    }

    private static Command BuildOpen()
    {
        var cmd    = new Command("open", "Open an observability UI in the browser");
        var target = new Argument<string>("target",
            "grafana | seq | jaeger | gateway | bff | identity | prometheus");
        cmd.AddArgument(target);
        cmd.SetHandler((t) => BashBridge.Run("ums-engine.sh", $"open {t}"), target);
        return cmd;
    }

    private static Command BuildHealth()
    {
        var cmd = new Command("health", "Curl the API Gateway /health endpoint");
        cmd.SetHandler(() => BashBridge.Run("ums-engine.sh", "open health"));
        return cmd;
    }

    private static Command BuildScale()
    {
        var cmd      = new Command("scale", "Scale a K8s deployment to N replicas");
        var svc      = new Argument<string>("service", "K8s deployment name");
        var replicas = new Argument<int>("replicas", "Number of replicas");
        cmd.AddArgument(svc);
        cmd.AddArgument(replicas);
        cmd.SetHandler((s, r) =>
            BashBridge.RunRaw($"kubectl scale deployment/{s} --replicas={r} -n ums"),
            svc, replicas);
        return cmd;
    }
}
