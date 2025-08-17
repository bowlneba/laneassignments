using System.Collections.Generic;

namespace LaneAssignments.Functionality ;

public class Lane
{
    public int Number { get; set; }

    public List<int> Teams { get; set; }


    public Lane(int number, params int[] teams)
    {
        Number = number;

        Teams = new List<int>();
        Teams.AddRange(teams);
    }

    public void AddTeam(int team)
        => Teams.Add(team);

    public override string ToString() => $"Lane: {Number}";
}