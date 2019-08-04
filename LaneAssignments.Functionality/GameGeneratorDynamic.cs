using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaneAssignments.Functionality
{
    public class GameGeneratorDynamic
    {
        private readonly Action<int,int> _pushTries;
        private readonly int _teams;
        private readonly int _gameCount;
        private readonly int _teamsPerPair;

        private readonly IList<Game> _games;
        public IEnumerable<Game> Games => _games;

        public GameGeneratorDynamic(Action<int,int> pushTries, int teams, int games, int teamsPerPair)
        {
            _pushTries = pushTries;
            _teams = teams;
            _gameCount = games;
            _teamsPerPair = teamsPerPair;

            if (_gameCount > _teams / _teamsPerPair)
                throw new InvalidOperationException("More games than pairs available");

            _games = new List<Game>();
        }

        public void Generate(Action<Game> dumpGame)
        {
            for (var i = 1; i <= _gameCount; i++)
            {
                var game = GenerateGame(i, _games.ToArray());
                _games.Add(game);
                dumpGame(game);
            }
        }

        private Game GenerateGame(int gameNumber, params Game[] previousGames)
        {
            bool differentLanesAndPairings;

            var lanes = new List<Lane>();

            var tries = 0;

            do
            {
                tries++;

                if (tries % 10000 == 0)
                    _pushTries(gameNumber,tries);

                differentLanesAndPairings = true;

                var teams = RandomizeTeamOrder().ToList();

                for (var i = 1; i <= _teams / _teamsPerPair; i++)
                    lanes.Add(new Lane(i));
                

                foreach (var team in teams)
                {
                    var previousPairs = previousGames.SelectMany(item => item.LaneAssignments).Where(item => item.Teams.Contains(team)).Select(item=>item.Number);

                    var laneAssignments = new List<Lane>();
                    laneAssignments.AddRange(lanes.Where(item=>item.Teams.Count != _teamsPerPair && !previousPairs.Contains(item.Number))); //eligible lanes for assignment

                    if (!laneAssignments.Any())
                        break;

                    do
                    {
                        var tryLane = laneAssignments.GetRandom();

                        var previousTeamsOnLane = previousGames.SelectMany(item => item.LaneAssignments)
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

                    } while (laneAssignments.Any());
                }

                if (lanes.All(item => item.Teams.Count == _teamsPerPair)) continue;

                differentLanesAndPairings = false;
                lanes.Clear();

            } while (!differentLanesAndPairings);

            return new Game(gameNumber,tries, lanes.ToArray());
        }

        private IEnumerable<int> RandomizeTeamOrder()
        {
            var teams = new List<int>();

            for (var i = 1; i <= _teams; i++)
                teams.Add(i);

            teams.Shuffle();

            return teams;
        }
    }
}
