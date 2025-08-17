using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using LaneAssignments.Functionality;

// Top-level statements (no Program class, no static Main)

int _teams;
int _games;
int _teamsPerPair;
bool _allowDuplicateLane;

List<Guid> guids = new();

Console.Write("Enter Number of Teams: ");
var teams = Console.ReadLine();

Console.Write("Enter Number of Games: ");
var games = Console.ReadLine();

Console.Write("Enter Number of Teams per Pair: ");
var teamsPerPair = Console.ReadLine();

Console.Write("Allow Duplicate Lane Assignments (Y): ");
var allowDuplicateLane = Console.ReadLine();

System.IO.Directory.CreateDirectory("results");

await DynamicGames(
    Convert.ToInt32(teams),
    Convert.ToInt32(games),
    Convert.ToInt32(teamsPerPair),
    string.Equals(allowDuplicateLane, "Y", StringComparison.OrdinalIgnoreCase)
);

async Task DynamicGames(int teams, int games, int teamsPerPair, bool allowDuplicateLanes)
{
    var success = false;
    var resetCount = 0;
    var stopwatch = Stopwatch.StartNew();
    do
    {
        _teams = teams;
        _games = games;
        _teamsPerPair = teamsPerPair;
        _allowDuplicateLane = allowDuplicateLanes;

        var generator = new GameGeneratorDynamic(PushTries, teams, games, teamsPerPair, allowDuplicateLanes);
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
            System.IO.Directory.CreateDirectory("results/complete");
            await System.IO.File.WriteAllTextAsync($"results/complete/teams{_teams}_games{_games}_perPair{_teamsPerPair}_duplicateLane-{_allowDuplicateLane}_{Guid.NewGuid()}.txt", x.ToString());
            stopwatch.Stop();
            Console.WriteLine($"Total Time Elapsed: {stopwatch.Elapsed}");
            Console.WriteLine($"Total Attempts: {resetCount * 1_000_000 + generator.Tries:n0}"); 
        }
        else
        {
            resetCount++;
            Console.WriteLine($"Resetting all games and starting over - {resetCount}");
        }
        foreach (var guid in guids)
        {
            var files = System.IO.Directory.GetFiles("results", $"*{guid}*.txt");
            foreach (var file in files)
            {
                System.IO.File.Delete(file);
            }
        }
    } while (!success);
}

void WriteGame(LaneAssignments.Game game)
{
    var gameTxt = DumpGame(game);
    Console.WriteLine(gameTxt);
    var guid = Guid.NewGuid();
    guids.Add(guid);
    System.IO.File.WriteAllText($"results/game{game.Number}_teams{_teams}_games{_games}_perPair{_teamsPerPair}_duplicateLane-{_allowDuplicateLane}_{guid}.txt", gameTxt);
}

string DumpGame(LaneAssignments.Game game)
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

void PushTries(int gameNumber, int tries, TimeSpan elapsed)
    => Console.WriteLine($"Game {gameNumber} Attempts: {tries:n0} Elapsed: {elapsed}");
