using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;

namespace LaneAssignments.Functionality;

public class GameGeneratorDynamic2
{
    private readonly Action<int, int, TimeSpan> _pushTries;
    private readonly int _teams;
    private readonly int _gameCount;
    private readonly int _teamsPerLane;
    private readonly bool _allowDuplicateLane;

    private readonly IList<Game> _games;
    public IEnumerable<Game> Games => _games;

    public int Tries { get; private set; }

    // Histories for fast lookups
    private readonly Dictionary<int, HashSet<int>> _opponentHistory;
    private readonly Dictionary<int, HashSet<int>> _laneHistory;

    public GameGeneratorDynamic2(Action<int, int, TimeSpan> pushTries, int teams, int games, int teamsPerLane, bool allowDuplicateLanes)
    {
        _pushTries = pushTries;
        _teams = teams;
        _gameCount = games;
        _teamsPerLane = teamsPerLane;
        _allowDuplicateLane = allowDuplicateLanes;

        _games = new List<Game>();
        _opponentHistory = new Dictionary<int, HashSet<int>>();
        _laneHistory = new Dictionary<int, HashSet<int>>();

        for (var i = 1; i <= _teams; i++)
        {
            _opponentHistory[i] = [];
            _laneHistory[i] = [];
        }
    }

    public bool Generate(Action<Game> dumpGame)
    {
        for (var i = 1; i <= _gameCount; i++)
        {
            var game = GenerateGame(i);

            if (game == null) // failed to generate a game
            {
                _games.Clear();
                ResetHistories();
                return false;
            }

            _games.Add(game);
            UpdateHistories(game);
            dumpGame(game);
        }

        return true;
    }

    private Game GenerateGame(int gameNumber)
    {
        var lanesThisGame = Math.Max(_teams / _teamsPerLane, _gameCount);

        // Build all lanes
        var lanes = new List<Lane>();
        for (var i = 1; i <= lanesThisGame; i++)
        {
            lanes.Add(new Lane(i));
        }

        // total lanes available
        var totalLanes = lanesThisGame;

        // how many lanes are actually needed for this game
        var lanesNeeded = (int)Math.Ceiling((double)_teams / _teamsPerLane);

        // how many empty lanes to assign this game
        var emptyCount = totalLanes - lanesNeeded;

        // pick empty lanes in a rotating pattern across games
        var emptyLaneNumbers = Enumerable.Range(0, emptyCount)
            .Select(offset => ((gameNumber - 1 + offset) % totalLanes) + 1)
            .ToList();

        var emptyLanes = lanes.Where(l => emptyLaneNumbers.Contains(l.Number)).ToList();

        var timing = Stopwatch.StartNew();
        var availableTeams = RandomizeTeamOrder();

        do
        {
            if (Tries++ == 10_000_000)
            {
                Console.WriteLine("Resetting after 10,000,000 tries");
                return null;
            }

            if (Tries % 50_000 == 0)
            {
                _pushTries(gameNumber, Tries, timing.Elapsed);
                timing.Restart();
            }

            var success = AssignTeamsToLanes(availableTeams, lanes, emptyLanes);
            if (success)
                return new Game(gameNumber, Tries, lanes.ToArray());

            // reset lanes except the permanent empty one
            foreach (var lane in lanes)
            {
                lane.Teams.Clear();
            }

            availableTeams.Shuffle2();

        } while (true);
    }

    private bool AssignTeamsToLanes(List<int> teams, List<Lane> lanes, IEnumerable<Lane> emptyLanes)
    {
        foreach (var team in teams)
        {
            // find candidate lanes, excluding the empty lane
            var candidateLanes = lanes
                .Where(l => !emptyLanes.Contains(l))
                .Where(l => l.Teams.Count < _teamsPerLane)
                .Where(l => _allowDuplicateLane || !_laneHistory[team].Contains(l.Number))
                .Where(l => !l.Teams.Any(other => _opponentHistory[team].Contains(other)))
                .ToList();

            if (candidateLanes.Count == 0)
                return false;

            // MRV heuristic: prefer lanes with fewer available slots
            var chosenLane = candidateLanes
                .OrderBy(l => l.Teams.Count)
                .ThenBy(_ => RandomNumberGenerator.GetInt32(0, 1000))
                .First();

            chosenLane.AddTeam(team);
        }

        // ✅ check only "real" lanes are full
        return lanes.Where(l => !emptyLanes.Contains(l)).All(l => l.Teams.Count == _teamsPerLane);
    }

    private void UpdateHistories(Game game)
    {
        foreach (var lane in game.LaneAssignments)
        {
            foreach (var team in lane.Teams)
            {
                _laneHistory[team].Add(lane.Number);
                foreach (var opponent in lane.Teams.Where(o => o != team))
                {
                    _opponentHistory[team].Add(opponent);
                }
            }
        }
    }

    private void ResetHistories()
    {
        foreach (var kvp in _opponentHistory)
        {
            kvp.Value.Clear();
        }

        foreach (var kvp in _laneHistory)
        {
            kvp.Value.Clear();
        }
    }

    private List<int> RandomizeTeamOrder()
    {
        var teams = Enumerable.Range(1, _teams).ToList();
        teams.Shuffle2();
        return teams;
    }
}

internal static class GameGeneratorExtensions2
{
    internal static void Shuffle2<T>(this IList<T> list)
    {
        var rng = new Random(DateTime.Now.GetHashCode());
        var n = list.Count;
        while (n > 1)
        {
            n--;
            var k = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}
