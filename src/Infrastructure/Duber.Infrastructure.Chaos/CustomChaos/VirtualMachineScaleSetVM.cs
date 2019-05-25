namespace Duber.Infrastructure.Chaos.CustomChaos
{
    public class VirtualMachineScaleSetVM
    {
        public Value[] value { get; set; }
    }

    public class Value
    {
        public string instanceId { get; set; }
        public Properties properties { get; set; }
    }

    public class Properties
    {
        public Instanceview instanceView { get; set; }
        public int provisioningState { get; set; }
    }

    public class Instanceview
    {
        public Status[] statuses { get; set; }
    }

    public class Status
    {
        public string code { get; set; }
        public string level { get; set; }
        public string displayStatus { get; set; }
    }
}
