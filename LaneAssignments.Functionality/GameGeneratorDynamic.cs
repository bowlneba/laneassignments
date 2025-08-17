using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;

namespace LaneAssignments.Functionality;

public class GameGeneratorDynamic
{
    private readonly Action<int, int, TimeSpan> _pushTries;
    private readonly int _teams;
    private readonly int _gameCount;
    private readonly int _teamsPerPair;
    private readonly bool _allowDuplicateLane;

    private readonly IList<Game> _games;
    public IEnumerable<Game> Games => _games;

    public int Tries { get; private set; }

    public GameGeneratorDynamic(Action<int,int, TimeSpan> pushTries, int teams, int games, int teamsPerPair, bool allowDuplicateLanes)
    {
        _pushTries = pushTries;
        _teams = teams;
        _gameCount = games;
        _teamsPerPair = teamsPerPair;
        _allowDuplicateLane = allowDuplicateLanes;

        if (_gameCount > _teams / (allowDuplicateLanes ? 1 :_teamsPerPair))
            throw new InvalidOperationException("More games than pairs available");

        _games = new List<Game>();
    }

    public bool Generate(Action<Game> dumpGame)
    {
        for (var i = 1; i <= _gameCount; i++)
        {
            var game = GenerateGame(i, _games.ToArray());

            if (game == null) // failed to generate a game
            {
                _games.Clear();
                
                return false;
            }

            _games.Add(game);
            dumpGame(game);
        }
        
        return true;
    }

    private Game GenerateGame(int gameNumber, params Game[] previousGames)
    {
        bool differentLanesAndPairings;

        var lanes = new List<Lane>();
        
        var timing = Stopwatch.StartNew();

        do
        {
            if (Tries++ == 1_000_000)
            {
                Console.WriteLine("Resetting after 1,000,000 tries");

                return null;
            }

            if (Tries % 10_000 == 0)
            {
                _pushTries(gameNumber, Tries, timing.Elapsed);
                timing.Restart();
            }

            differentLanesAndPairings = true;

            var teams = RandomizeTeamOrder();

            for (var i = 1; i <= _teams / _teamsPerPair; i++)
            {
                lanes.Add(new Lane(i));
            }
            
            var laneAssignments = new List<Lane>();

            foreach (var team in teams)
            {
                var previousPairs = previousGames.SelectMany(item => item.LaneAssignments).Where(item => item.Teams.Contains(team)).Select(item=>item.Number);

                laneAssignments.Clear();

                laneAssignments.AddRange(lanes.Where(item => item.Teams.Count != _teamsPerPair).Where(item=> _allowDuplicateLane || !previousPairs.Contains(item.Number))); //eligible lanes for assignment

                if (laneAssignments.Count == 0)
                    break;

                do
                {
                    var tryLane = laneAssignments.GetRandom();

                    var previousTeamsOnLane = _allowDuplicateLane ? [] : previousGames.SelectMany(item => item.LaneAssignments)
                        .Where(item => item.Number == tryLane.Number).SelectMany(item => item.Teams);

                    var previousOpponents = previousGames.SelectMany(item => item.LaneAssignments)
                        .Where(item => item.Teams.Contains(team)).SelectMany(item => item.Teams)
                        .Where(item => item != team);

                    if (previousTeamsOnLane.Contains(team) || previousOpponents.Intersect(tryLane.Teams).Any())
                    {
                        laneAssignments.Remove(tryLane);
                    }
                    else
                    {
                        tryLane.AddTeam(team);
                        laneAssignments.Clear();
                    }

                } while (laneAssignments.Count != 0);
            }

            if (lanes.All(item => item.Teams.Count == _teamsPerPair)) continue;

            differentLanesAndPairings = false;
            lanes.Clear();

        } while (!differentLanesAndPairings);

        return new Game(gameNumber,Tries, lanes.ToArray());
    }

    private List<int> RandomizeTeamOrder()
    {
        var teams = new List<int>();

        for (var i = 1; i <= _teams; i++)
        {
            teams.Add(i);
        }

        teams.Shuffle();

        return teams;
    }
}

internal static class GameGeneratorExtensions
{
    internal static Lane GetRandom(this IList<Lane> lanes)
    {
        if (lanes.Count == 1)
            return lanes[0];
        
        var randomPosition = RandomNumberGenerator.GetInt32(0, lanes.Count - 1);

        return lanes[randomPosition];
    }

    internal static void Shuffle<T>(this IList<T> list)
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
