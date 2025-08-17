using System;
using System.Collections.Generic;
using System.Linq;

namespace LaneAssignments.Functionality;

public class ScheduleDiagnosticResult
{
    public bool IsPossible { get; set; }
    public int TotalTeams { get; set; }
    public int Games { get; set; }
    public int TeamsPerPair { get; set; }
    public int LanesPerGame { get; set; }

    public int TotalMatches { get; set; }
    public int PairsPerMatch { get; set; }
    public int TotalPairsCovered { get; set; }
    public int TotalUniquePairs { get; set; }
    public int MaximumUniquePairsPossible { get; set; }
    public double PercentUniquePairsAchievable { get; set; }
    public int RepeatsRequired { get; set; }
    public int TotalPairsNeeded { get; set; }
    public int OpponentsNeededPerTeam { get; set; }
    public int MaxUniqueOpponentsPerTeam { get; set; }
    public double AverageUniqueOpponentsPerTeam { get; set; }
    public string Reason { get; set; }

    public override string ToString()
        => $"""
            Schedule is {(IsPossible ? "possible" : "not possible")}:
            IsPossible: {IsPossible}
            Reason: {Reason}

            Total Teams: {TotalTeams}
            Games: {Games}
            Teams Per Lane: {TeamsPerPair}
            Lanes Per Game: {LanesPerGame}

            Total Matches: {TotalMatches}
            Pairs Per Match: {PairsPerMatch}
            Total Pairs Covered: {TotalPairsCovered}
            Maximum Unique Pairs Possible: {MaximumUniquePairsPossible}
            Percent Unique Pairs Achievable: {PercentUniquePairsAchievable:F2}%
            Repeats Required: {RepeatsRequired}

            Total Unique Team Pairs Available: {TotalUniquePairs}
            Total Team Pairs Needed: {TotalPairsNeeded}

            Opponents Needed Per Team: {OpponentsNeededPerTeam}
            Max Unique Opponents Per Team: {MaxUniqueOpponentsPerTeam}
            Average Unique Opponents Per Team: {AverageUniqueOpponentsPerTeam:F2}
            """;
}

public class ScheduleVerificationResult
{
    public bool IsValid { get; set; } = true;
    public List<string> Errors { get; } = new();
    public Dictionary<int, int> OpponentCounts { get; } = new();
    public Dictionary<int, Dictionary<int, int>> LaneBalance { get; } = new();

    public void PrintDiagnostics()
    {
        Console.WriteLine($"Schedule Verification Result: {(IsValid ? "VALID ✅" : "INVALID ❌")}");
        Console.WriteLine();

        if (Errors.Count > 0)
        {
            Console.WriteLine("Errors:");
            foreach (var error in Errors)
            {
                Console.WriteLine($" - {error}");
            }
            Console.WriteLine();
        }

        Console.WriteLine("Opponent Counts:");
        foreach (var kvp in OpponentCounts.OrderBy(x => x.Key))
        {
            Console.WriteLine($" Team {kvp.Key}: {kvp.Value} unique opponents");
        }
        Console.WriteLine();

        Console.WriteLine("Lane Balance:");
        foreach (var teamEntry in LaneBalance.OrderBy(x => x.Key))
        {
            Console.Write($" Team {teamEntry.Key}: ");
            foreach (var laneEntry in teamEntry.Value.OrderBy(x => x.Key))
            {
                Console.Write($"Lane {laneEntry.Key} => {laneEntry.Value} times; ");
            }
            Console.WriteLine();
        }
    }
}

public static class ScheduleVerifier
{
    public static ScheduleVerificationResult VerifySchedule(List<Game> games, int teams, int teamsPerLane)
    {
        var result = new ScheduleVerificationResult();

        // Track opponents per team
        var opponents = new Dictionary<int, HashSet<int>>();
        for (var i = 1; i <= teams; i++)
        {
            opponents[i] = new HashSet<int>();
        }

        // Track all pairs globally
        var globalPairs = new HashSet<(int, int)>();

        // Track lane balance
        for (var i = 1; i <= teams; i++)
        {
            result.LaneBalance[i] = new Dictionary<int, int>();
        }

        foreach (var game in games)
        {
            var gameNumber = game.Number;
            var lanes = game.LaneAssignments;
            var teamsInGame = new HashSet<int>();

            foreach (var lane in lanes)
            {
                if (lane.Teams.Count != teamsPerLane)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Game {gameNumber}, Lane {lane.Number}: expected {teamsPerLane} teams, found {lane.Teams.Count}");
                }

                foreach (var team in lane.Teams)
                {
                    if (!teamsInGame.Add(team))
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Game {gameNumber}: Team {team} appears more than once!");
                    }

                    // Lane balance
                    if (!result.LaneBalance[team].ContainsKey(lane.Number))
                    {
                        result.LaneBalance[team][lane.Number] = 0;
                    }
                    result.LaneBalance[team][lane.Number]++;
                }

                // Opponent pairs
                for (var i = 0; i < lane.Teams.Count; i++)
                {
                    for (var j = i + 1; j < lane.Teams.Count; j++)
                    {
                        var t1 = lane.Teams[i];
                        var t2 = lane.Teams[j];

                        opponents[t1].Add(t2);
                        opponents[t2].Add(t1);

                        var pair = (Math.Min(t1, t2), Math.Max(t1, t2));
                        if (!globalPairs.Add(pair))
                        {
                            result.IsValid = false;
                            result.Errors.Add($"Duplicate pairing detected: Team {t1} vs Team {t2}");
                        }
                    }
                }
            }

            if (teamsInGame.Count != teams)
            {
                result.IsValid = false;
                result.Errors.Add($"Game {gameNumber}: Expected {teams} teams, found {teamsInGame.Count}");
            }
        }

        // Opponent coverage check
        var expectedOpponents = games.Count * (teamsPerLane - 1);
        foreach (var kvp in opponents)
        {
            var team = kvp.Key;
            var opponentCount = kvp.Value.Count;
            result.OpponentCounts[team] = opponentCount;

            if (opponentCount != expectedOpponents)
            {
                result.IsValid = false;
                result.Errors.Add($"Team {team}: {opponentCount} unique opponents (expected {expectedOpponents})");
            }
        }

        return result;
    }
}
