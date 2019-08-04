using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaneAssignments.Functionality
{
    public class GameGenerator
    {
        private readonly Action<int,int> _pushTries;

        public GameGenerator(Action<int,int> pushTries)
        {
            _pushTries = pushTries;
        }

        public Game Game1 { get; private set; }
        public Game Game2 { get; private set; }
        public Game Game3 { get; private set; }
        public Game Game4 { get; private set; }
        public Game Game5 { get; private set; }
        public Game Game6 { get; private set; }
        public Game Game7 { get; private set; }

        public void GenerateGame1()
        {
            var game = new Game(1,0);

            var lane1 = new Lane(1, 1, 2, 3, 4);
            var lane2 = new Lane(2, 5, 6, 7, 8);
            var lane3 = new Lane(3, 9, 10, 11, 12);
            var lane4 = new Lane(4, 13, 14, 15, 16);
            var lane5 = new Lane(5, 17, 18, 19, 20);
            var lane6 = new Lane(6, 21, 22, 23, 24);
            var lane7 = new Lane(7, 25, 26, 27, 28);
            var lane8 = new Lane(8, 29, 30, 31, 32);

            game.AddLanes(lane1, lane2, lane3, lane4, lane5, lane6, lane7, lane8);

            Game1 = game;
        }

        public void GenerateGame2()
        {
            Game2 = GenerateGame(2, Game1);
        }

        public void GenerateGame3()
        {
            Game3 = GenerateGame(3, Game1, Game2);
        }

        public void GenerateGame4()
        {
            Game4 = GenerateGame(4, Game1, Game2, Game3);
        }

        public void GenerateGame5()
        {
            Game5 = GenerateGame(5, Game1, Game2, Game3, Game4);
        }

        public void GenerateGame6()
        {
            Game6 = GenerateGame(6, Game1, Game2, Game3, Game4,Game5);
        }

        public void GenerateGame7()
        {
            Game7 = GenerateGame(7, Game1, Game2, Game3, Game4,Game5,Game6);
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

                var lane1 = new Lane(1);
                var lane2 = new Lane(2);
                var lane3 = new Lane(3);
                var lane4 = new Lane(4);
                var lane5 = new Lane(5);
                var lane6 = new Lane(6);
                var lane7 = new Lane(7);
                var lane8 = new Lane(8);

                

                foreach (var team in teams)
                {
                    lanes.AddRange(new[] { lane1, lane2, lane3, lane4, lane5, lane6, lane7, lane8 }.Where(item=>item.Teams.Count != 4)); //eligible lanes for assignment

                    do
                    {
                        var tryLane = lanes.GetRandom();

                        var previousTeamsOnLane = previousGames.SelectMany(item => item.LaneAssignments)
                            .Where(item => item.Number == tryLane.Number).SelectMany(item => item.Teams);

                        if (previousTeamsOnLane.Contains(team))
                        {
                            lanes.Remove(tryLane);
                        }
                        else
                        {
                            var previousOpponents = previousGames.SelectMany(item => item.LaneAssignments)
                                .Where(item => item.Teams.Contains(team)).SelectMany(item => item.Teams)
                                .Where(item => item != team);

                            if (previousOpponents.Intersect(tryLane.Teams).Any())
                            {
                                lanes.Remove(tryLane);
                            }
                            else
                            {
                                tryLane.AddTeam(team);
                                lanes.Clear();
                            }
                        }
                    } while (lanes.Any());
                }

                lanes = new[] {lane1, lane2, lane3, lane4, lane5, lane6, lane7, lane8}.ToList();

                if (lanes.Any(item => item.Teams.Count != 4))
                    differentLanesAndPairings = false;

            } while (!differentLanesAndPairings);

            return new Game(gameNumber,tries, lanes.ToArray());
        }

        private IEnumerable<int> RandomizeTeamOrder()
        {
            var teams = new List<int>
            {
                1, 2, 3, 4,
                5, 6, 7, 8,
                9, 10, 11, 12,
                13, 14, 15, 16,
                17, 18, 19, 20,
                21, 22, 23, 24,
                25, 26, 27, 28,
                29, 30, 31, 32
            };


            teams.Shuffle();

            return teams;
        }
    }

    internal static class ExtensionMethods
    {
        internal static Lane GetRandom(this IList<Lane> lanes)
        {
            var randomPosition = new Random(DateTime.Now.GetHashCode()).Next(0, lanes.Count - 1);

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
                var value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        internal static IEnumerable<int> TeamPairings(this IEnumerable<Lane> lanes, int team)
        {
            var game = new Game(0,0);
            game.AddLanes(lanes.ToArray());

            return game.TeamPairings(team);
        }

        internal static int TeamLane(this IEnumerable<Lane> lanes, int team)
        {
            var game = new Game(0,0);
            game.AddLanes(lanes.ToArray());

            return game.TeamLane(team);
        }
    }
}
