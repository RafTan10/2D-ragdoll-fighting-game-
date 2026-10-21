using System;

/// <summary>
/// A brain driven by a delegate. Used for tests and tuning: `script eval` can't declare classes,
/// but it can assign a lambda, e.g. <c>fighter.Brain = new ScriptBrain((self, opp) => ...)</c>.
/// </summary>
public class ScriptBrain : IFighterBrain
{
    readonly Func<Fighter, Fighter, FighterInput> think;

    public ScriptBrain(Func<Fighter, Fighter, FighterInput> think) { this.think = think; }

    public FighterInput Think(Fighter self, Fighter opponent) => think(self, opponent);
}
