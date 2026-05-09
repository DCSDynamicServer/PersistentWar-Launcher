using DcsWarLauncher.Domain;

namespace DcsWarLauncher.Campaign;

public static class ObjectiveEngine
{
    public static ObjectiveState AdvanceObjective(
        ObjectiveState objective,
        IReadOnlyCollection<GroundUnitState> groundUnits,
        int netPressure)
    {
        var localBlueGround = GroundWarEngine.GroundPressureAt(objective.Name, groundUnits, "blue");
        var localRedGround = GroundWarEngine.GroundPressureAt(objective.Name, groundUnits, "red");
        var groundPressure = (localBlueGround - localRedGround) / 8;
        var ownerModifier = objective.Owner switch
        {
            "blue" => 2,
            "red" => -2,
            _ => 0
        };
        var nextStrength = Math.Clamp(objective.Strength + netPressure + groundPressure + ownerModifier, 0, 100);
        var owner = nextStrength switch
        {
            >= 70 => "blue",
            <= 30 => "red",
            _ => "contested"
        };

        return objective with { Owner = owner, Strength = nextStrength };
    }
}
