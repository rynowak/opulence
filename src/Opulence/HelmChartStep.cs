namespace Opulence
{
    public sealed class HelmChartStep : Step
    {
        public override string DisplayName => "helm chart";

        public string ChartName { get; set; } = default!;
    }
}