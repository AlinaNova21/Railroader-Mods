using Game.Progression;
using UI.Console;

namespace AlinasMapMod
{
    [ConsoleCommand("/echo", "Echo echo echo echo")]
    public class EchoCommand : IConsoleCommand
    {
        public string Execute(string[] components)
            => string.Join(" ", components);
    }

    [ConsoleCommand("/testing", "Testing! testing!")]
    public class TestingCommand: IConsoleCommand {
        public string Execute(string[] components) {
            Progression shared = Progression.Shared;
            // shared.mapFeatureManager.AvailableFeatures
            return "Test Test";
        }
    }

    
}
