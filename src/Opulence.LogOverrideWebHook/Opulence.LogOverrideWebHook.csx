#r "/Users/ryan/github.com/rynowak/opulence/src/Opulence/bin/Debug/netstandard2.0/Opulence.dll"

using Opulence;

public void Package(Application application)
{
    application.Steps.Get<HelmChartStep>().ChartName = "opulence-logoverride-webhook";
}