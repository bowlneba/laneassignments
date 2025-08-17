using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LaneAssignments;

class Program
{
    private static int _teams;
    private static int _games;
    private static int _teamsPerPair;
    private static bool _allowDuplicateLane;

    private static List<Guid> _guids = [];

    static void Main(string[] args)
    {
        Console.Write("Enter Number of Teams: ");
        var teams = Console.ReadLine();

        Console.Write("Enter Number of Games: ");
        var games = Console.ReadLine();

        Console.Write("Enter Number of Teams per Pair: ");
        var teamsPerPair = Console.ReadLine();

        Console.Write("Allow Duplicate Lane Assignments (Y): ");
        var allowDuplicateLane = Console.ReadLine();

        System.IO.Directory.CreateDirectory("results");

        DynamicGames(Convert.ToInt32(teams),Convert.ToInt32(games),Convert.ToInt32(teamsPerPair), allowDuplicateLane.ToUpper() == "Y");
    }

    private static void DynamicGames(int teams, int games, int teamsPerPair, bool allowDuplicateLanes)
    {
        var success = false;
        var resetCount = 0;
            
        // shell of a do while not success
        do
        {
            _teams = teams;
            _games = games;
            _teamsPerPair = teamsPerPair;
            _allowDuplicateLane = allowDuplicateLanes;

            var generator = new Functionality.GameGeneratorDynamic(PushTries, teams, games,teamsPerPair,allowDuplicateLanes);
            var generated = generator.Generate(WriteGame);

            if (generated)
            {
                success = true;
                Console.WriteLine("All games generated successfully");
                    
                var x = new System.Text.StringBuilder();

                foreach (var game in generator.Games)
                {
                    x.AppendLine(DumpGame(game));
                }

                System.IO.Directory.CreateDirectory("results//complete");

                System.IO.File.WriteAllText($"results//complete//teams{_teams}_games{_games}_perPair{_teamsPerPair}_duplicateLane-{_allowDuplicateLane}_{Guid.NewGuid()}.txt", x.ToString());
            }
            else
            {
                resetCount++;
                Console.WriteLine($"Resetting all games and starting over - {resetCount}");
                    
                foreach (var guid in _guids)
                {
                    var file = System.IO.Directory.GetFiles("results", $"*{guid}*.txt");
                    foreach (var f in file)
                    {
                        System.IO.File.Delete(f);
                    }
                }
            }
        } while (!success);

            
    }

    private static void WriteGame(Game game)
    {
        var gameTxt = DumpGame(game);
        Console.WriteLine(gameTxt);
            
        var guid = Guid.NewGuid();
        _guids.Add(guid);
            
        System.IO.File.WriteAllText($"results//game{game.Number}_teams{_teams}_games{_games}_perPair{_teamsPerPair}_duplicateLane-{_allowDuplicateLane}_{guid}.txt", gameTxt);
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

    private static void PushTries(int gameNumber, ulong tries, long elapsedMilliseconds)
        => Console.WriteLine($"Game {gameNumber} Attempts: {tries:n0} Elapsed: {elapsedMilliseconds}ms");
}