using System;
using System.Collections.Generic;
using System.Linq;

namespace LaneAssignments.Functionality;

public static class Scheduler
{
    public static Dictionary<int, List<Match>> BuildScheduleWithNoDuplicates(
        int teams, int games, int teamsPerLane, bool verbose = false)
    {
        var lanesPerGame = teams / teamsPerLane;

        var teamLaneHistory = new Dictionary<int, HashSet<int>>();
        var teamOpponentHistory = new Dictionary<int, HashSet<int>>();

        for (var t = 1; t <= teams; t++)
        {
            teamLaneHistory[t] = new HashSet<int>();
            teamOpponentHistory[t] = new HashSet<int>();
        }

        var schedule = new Dictionary<int, List<Match>>();

        if (TryBuildSchedule(1, games, lanesPerGame, teamsPerLane,
                             Enumerable.Range(1, teams).ToList(),
                             teamLaneHistory, teamOpponentHistory,
                             schedule, verbose))
        {
            if (verbose)
                Console.WriteLine("✅ Schedule successfully built!");
            return schedule;
        }

        throw new InvalidOperationException("❌ No valid schedule could be generated with given parameters.");
    }

    private static bool TryBuildSchedule(
        int currentGame,
        int totalGames,
        int lanesPerGame,
        int teamsPerLane,
        List<int> remainingTeams,
        Dictionary<int, HashSet<int>> teamLaneHistory,
        Dictionary<int, HashSet<int>> teamOpponentHistory,
        Dictionary<int, List<Match>> schedule,
        bool verbose)
    {
        if (currentGame > totalGames)
            return true; // All games scheduled

        if (verbose)
            Console.WriteLine($"\n📅 Starting Game {currentGame}...");

        var matchesForGame = new List<Match>();
        var usedTeamsThisGame = new HashSet<int>();

        if (AssignLanesRecursive(1, lanesPerGame, teamsPerLane,
                                 remainingTeams, usedTeamsThisGame,
                                 teamLaneHistory, teamOpponentHistory,
                                 matchesForGame, verbose))
        {
            schedule[currentGame] = matchesForGame;

            // Update global state
            foreach (var match in matchesForGame)
            {
                foreach (var t in match.Teams)
                {
                    teamLaneHistory[t].Add(match.LaneNumber);
                    foreach (var opp in match.Teams.Where(o => o != t))
                        teamOpponentHistory[t].Add(opp);
                }
            }

            if (TryBuildSchedule(currentGame + 1, totalGames, lanesPerGame, teamsPerLane,
                                 remainingTeams, teamLaneHistory, teamOpponentHistory,
                                 schedule, verbose))
                return true;

            // Rollback if failed
            foreach (var match in matchesForGame)
            {
                foreach (var t in match.Teams)
                {
                    teamLaneHistory[t].Remove(match.LaneNumber);
                    foreach (var opp in match.Teams.Where(o => o != t))
                        teamOpponentHistory[t].Remove(opp);
                }
            }

            if (verbose)
                Console.WriteLine($"↩️ Backtracking from Game {currentGame}...");
        }

        return false;
    }

    private static bool AssignLanesRecursive(
        int currentLane,
        int totalLanes,
        int teamsPerLane,
        List<int> allTeams,
        HashSet<int> usedTeamsThisGame,
        Dictionary<int, HashSet<int>> teamLaneHistory,
        Dictionary<int, HashSet<int>> teamOpponentHistory,
        List<Match> matches,
        bool verbose)
    {
        if (currentLane > totalLanes)
            return true; // All lanes filled this game

        var availableTeams = allTeams.Except(usedTeamsThisGame).ToList();

        foreach (var group in Combinations(availableTeams, teamsPerLane))
        {
            if (verbose)
                Console.WriteLine($"  ➡️ Trying lane {currentLane} with {{{string.Join(",", group)}}}");

            if (IsValidGroup(group, currentLane, teamLaneHistory, teamOpponentHistory))
            {
                var match = new Match { LaneNumber = currentLane, Teams = group.ToList() };
                matches.Add(match);

                foreach (var t in group)
                    usedTeamsThisGame.Add(t);

                if (verbose)
                    Console.WriteLine($"  ✅ Placed lane {currentLane}: {match}");

                if (AssignLanesRecursive(currentLane + 1, totalLanes, teamsPerLane,
                                         allTeams, usedTeamsThisGame,
                                         teamLaneHistory, teamOpponentHistory,
                                         matches, verbose))
                    return true;

                // Backtrack
                matches.Remove(match);
                foreach (var t in group)
                    usedTeamsThisGame.Remove(t);

                if (verbose)
                    Console.WriteLine($"  ❌ Backtracking from lane {currentLane} group {{{string.Join(",", group)}}}");
            }
        }

        return false;
    }

    private static bool IsValidGroup(
        IEnumerable<int> group,
        int lane,
        Dictionary<int, HashSet<int>> teamLaneHistory,
        Dictionary<int, HashSet<int>> teamOpponentHistory)
    {
        var groupList = group.ToList();

        foreach (var t in groupList)
        {
            if (teamLaneHistory[t].Contains(lane))
                return false;

            foreach (var opp in groupList.Where(o => o != t))
                if (teamOpponentHistory[t].Contains(opp))
                    return false;
        }

        return true;
    }

    private static IEnumerable<IEnumerable<int>> Combinations(IEnumerable<int> source, int length)
    {
        if (length == 0)
            yield return Enumerable.Empty<int>();
        else
        {
            var i = 0;
            foreach (var item in source)
            {
                var remaining = source.Skip(i + 1);
                foreach (var combo in Combinations(remaining, length - 1))
                    yield return new[] { item }.Concat(combo);
                i++;
            }
        }
    }

    public static Dictionary<int, List<Match>> BuildScheduleWithDuplicatesAllowed(int teams, int games, int teamsPerLane)
    {
       return new Dictionary<int, List<Match>>()
        {
            // This method would implement the logic for allowing duplicate lane assignments.
            // For simplicity, we can return an empty schedule or a placeholder.
            // The actual implementation would depend on the specific requirements for duplicates.
        };
    }
}

public class Match
{
    public int LaneNumber { get; set; }
    public List<int> Teams { get; init; } = [];

    public override string ToString() =>
        $"Lane {LaneNumber}: [{string.Join(" / ", Teams)}]";
}
