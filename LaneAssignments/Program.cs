using System;
using System.Threading.Tasks;

namespace LaneAssignments
{
    class Program
    {
        private static int _teams;
        private static int _games;
        private static int _teamsPerPair;

        static void Main(string[] args)
        {
            Console.Write("Enter Number of Teams: ");
            var teams = Console.ReadLine();

            Console.Write("Enter Number of Games: ");
            var games = Console.ReadLine();

            Console.Write("Enter Number of Teams per Pair: ");
            var teamsPerPair = Console.ReadLine();

            System.IO.Directory.CreateDirectory("results");

            DynamicGames(Convert.ToInt32(teams),Convert.ToInt32(games),Convert.ToInt32(teamsPerPair));
        }

        private static void DynamicGames(int teams, int games, int teamsPerPair)
        {
            _teams = teams;
            _games = games;
            _teamsPerPair = teamsPerPair;

            var generator = new Functionality.GameGeneratorDynamic(PushTries, teams, games,teamsPerPair);
            generator.Generate(WriteGame);

            var x = new System.Text.StringBuilder();

            foreach (var game in generator.Games)
                x.AppendLine(DumpGame(game));

            System.IO.Directory.CreateDirectory("results//complete");

            System.IO.File.WriteAllText($"results//complete//teams{_teams}_games{_games}_perPair{_teamsPerPair}_{Guid.NewGuid()}.txt", x.ToString());
        }

        private static void WriteGame(Game game)
        {
            var gameTxt = DumpGame(game);
            Console.WriteLine(gameTxt);
            System.IO.File.WriteAllText($"results//game{game.Number}_teams{_teams}_games{_games}_perPair{_teamsPerPair}_{Guid.NewGuid()}.txt", gameTxt);
        }

        private static string DumpGame(Game game)
        {
            var x = new System.Text.StringBuilder();

            x.AppendLine();
            x.AppendLine($"Game {game.Number} : Tries {game.Tries:n0}");

            foreach (var lane in game.LaneAssignments)
            {
                x.Append($"Lane {lane.Number}: ");

                for (var i = 0; i < _teamsPerPair; i++)
                {
                    x.Append(lane.Teams[i]);
                    x.Append(" / ");
                }

                x.Remove(x.Length - 2, 2);
                x.AppendLine();
            }

            return x.ToString();
        }

        private static void PushTries(int gameNumber, int tries)
            => Console.WriteLine($"Game {gameNumber} Attempts: {tries:n0}");
    }
}
