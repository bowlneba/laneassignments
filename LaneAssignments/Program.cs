using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using LaneAssignments.Functionality;

int _teams;
int _games;
int _teamsPerPair;
bool _allowDuplicateLane;
bool _checkPossibilityOnly;


List<Guid> guids = [];

Console.Write("Enter Number of Teams: ");
_teams = Convert.ToInt32(Console.ReadLine());

Console.Write("Enter Number of Games: ");
_games = Convert.ToInt32(Console.ReadLine());

Console.Write("Enter Number of Teams per Pair: ");
_teamsPerPair = Convert.ToInt32(Console.ReadLine());

Console.Write("Allow Duplicate Lane Assignments (Y): ");
_allowDuplicateLane = string.Equals(Console.ReadLine(), "Y", StringComparison.OrdinalIgnoreCase);

Console.Write("Check Possibility Only (Y): ");
_checkPossibilityOnly = string.Equals(Console.ReadLine(), "Y", StringComparison.OrdinalIgnoreCase);

var bruteForce = false;
if (!_checkPossibilityOnly)
{
    Console.Write("Brute Force (Y): ");
    bruteForce = string.Equals(Console.ReadLine(), "Y", StringComparison.OrdinalIgnoreCase);
}

System.IO.Directory.CreateDirectory("results");

var diagnosticResult = CheckCombinations(_teams, _games, _teamsPerPair, _allowDuplicateLane);

if (!diagnosticResult.IsPossible)
{
    Console.WriteLine("Schedule is not possible:");
    Console.WriteLine(diagnosticResult);
}
else if (!_checkPossibilityOnly)
{
    Console.WriteLine("Schedule is possible:");
    Console.WriteLine(diagnosticResult);

    if (bruteForce)
    {
        var stopWatch = Stopwatch.StartNew();
        var result = _allowDuplicateLane
            ? Scheduler.BuildScheduleWithDuplicatesAllowed(_teams, _games, _teamsPerPair)
            : Scheduler.BuildScheduleWithNoDuplicates(_teams, _games, _teamsPerPair, true);
        stopWatch.Stop();
    
        Console.WriteLine("Schedule generated successfully");
        Console.WriteLine($"Total Time Elapsed: {stopWatch.Elapsed}");
        
        var x = new System.Text.StringBuilder();
        foreach (var game in result)
        {
            x.AppendLine(DumpGameBrute(game));
        }
        System.IO.Directory.CreateDirectory("results/complete");
        await System.IO.File.WriteAllTextAsync($"results/complete/teams{_teams}_games{_games}_perPair{_teamsPerPair}_duplicateLane-{_allowDuplicateLane}_{Guid.NewGuid()}.txt", x.ToString());
        
    }
    else
    {
        await DynamicGames(
            _teams,
            _games,
            _teamsPerPair,
            _allowDuplicateLane);
    }

    
}
else
{
    Console.WriteLine("Possibility check completed:");
    Console.WriteLine(diagnosticResult);
}

return;

ScheduleDiagnosticResult CheckCombinations(int teams, int games, int teamsPerPair, bool allowDuplicateLanes)
    => allowDuplicateLanes
        ? DuplicateLanesCheck(teams, games, teamsPerPair)
        : NoDuplicateLanesCheck(teams, games, teamsPerPair);

ScheduleDiagnosticResult NoDuplicateLanesCheck(int teams, int games, int teamsPerLane)
{
    var result = new ScheduleDiagnosticResult
    {
        TotalTeams = teams,
        Games = games,
        TeamsPerPair = teamsPerLane
    };

    // Step 1: Lanes per game should be max of (teams/teamsPerLane) and games
    var baseLanesPerGame = teams / teamsPerLane;
    var lanesPerGame = Math.Max(baseLanesPerGame, games);
    result.LanesPerGame = lanesPerGame;

    // Step 2: Total matches (one per lane per game)
    var totalMatches = games * lanesPerGame;
    result.TotalMatches = totalMatches;

    // Step 3: Pairs per match
    var pairsPerMatch = teamsPerLane * (teamsPerLane - 1) / 2;
    result.PairsPerMatch = pairsPerMatch;

    // Step 4: Total opponent pairs covered
    var totalPairsCovered = totalMatches * pairsPerMatch;
    result.TotalPairsCovered = totalPairsCovered;

    // Step 5: Unique opponent pairs needed per team
    var opponentsPerTeam = games * (teamsPerLane - 1);
    result.OpponentsNeededPerTeam = opponentsPerTeam;

    // Step 6: Total unique team-opponent pairings needed
    var totalPairsNeeded = (teams * opponentsPerTeam) / 2;
    result.TotalPairsNeeded = totalPairsNeeded;

    // Step 7: Total unique possible opponent pairs
    var totalUniquePairs = teams * (teams - 1) / 2;
    result.TotalUniquePairs = totalUniquePairs;

    // Step 8: Metrics
    result.AverageUniqueOpponentsPerTeam = (double)(totalPairsCovered * 2) / teams;
    result.MaxUniqueOpponentsPerTeam = teams - 1;
    result.MaximumUniquePairsPossible = Math.Min(totalPairsCovered, totalUniquePairs);
    result.PercentUniquePairsAchievable = 100.0 * result.MaximumUniquePairsPossible / totalUniquePairs;
    result.RepeatsRequired = totalPairsCovered - result.MaximumUniquePairsPossible;

    // Step 9: Final feasibility check
    if (totalPairsNeeded > totalUniquePairs)
    {
        result.IsPossible = false;
        result.Reason = $"Only {totalUniquePairs} unique opponent pairs exist, but {totalPairsNeeded} are needed to avoid repeats.";
    }
    else if (totalPairsNeeded > totalPairsCovered)
    {
        result.IsPossible = false;
        result.Reason = $"Only {totalPairsCovered} unique opponent pairs can be scheduled, but {totalPairsNeeded} are needed to avoid repeats.";
    }
    else
    {
        result.IsPossible = true;
        result.Reason = $"Schedule is possible: {totalPairsNeeded} unique pairs needed, {totalPairsCovered} pairs scheduled, {totalUniquePairs} unique pairs available.";
    }

    return result;
}

ScheduleDiagnosticResult DuplicateLanesCheck(int teams, int games, int teamsPerPair)
{
    var result = new ScheduleDiagnosticResult
    {
        TotalTeams = teams,
        Games = games,
        TeamsPerPair = teamsPerPair
    };

    if ((teams * games) % teamsPerPair != 0)
    {
        result.IsPossible = false;
        result.Reason = "Total number of team appearances is not divisible by teams per match. Cannot evenly distribute matches.";
        return result;
    }

    result.TotalUniquePairs = teams * (teams - 1) / 2;
    result.TotalMatches = (teams * games) / teamsPerPair;
    result.PairsPerMatch = teamsPerPair * (teamsPerPair - 1) / 2;
    result.TotalPairsCovered = result.TotalMatches * result.PairsPerMatch;

    result.MaximumUniquePairsPossible = Math.Min(result.TotalPairsCovered, result.TotalUniquePairs);
    result.PercentUniquePairsAchievable = 100.0 * result.MaximumUniquePairsPossible / result.TotalUniquePairs;
    result.RepeatsRequired = result.TotalPairsCovered - result.MaximumUniquePairsPossible;

    // Each unique pair involves 2 teams
    result.AverageUniqueOpponentsPerTeam = Math.Min(
        teams - 1,
        (double)(result.MaximumUniquePairsPossible * 2) / teams
    );

    result.IsPossible = (result.MaximumUniquePairsPossible >= result.TotalUniquePairs);
    result.Reason = result.IsPossible
        ? "Schedule is possible without opponent repeats (lane pair reuse allowed)."
        : $"Only {result.MaximumUniquePairsPossible} unique opponent pairs can be scheduled, " +
          $"but {result.TotalUniquePairs} are needed to avoid opponent repeats.";

    return result;
}

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

        var generator = new GameGeneratorDynamic2(PushTries, teams, games, teamsPerPair, allowDuplicateLanes);
        var generated = generator.Generate(WriteGame);

        if (generated)
        {
            var result = ScheduleVerifier.VerifySchedule(generator.Games.ToList(), teams, teamsPerPair);
            result.PrintDiagnostics();

            if (result.IsValid)
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
                Console.WriteLine($"Total Attempts: {resetCount * 10_000_000 + generator.Tries:n0}"); 
            }
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

void WriteGame(Game game)
{
    var gameTxt = DumpGame(game);
    Console.WriteLine(gameTxt);
    var guid = Guid.NewGuid();
    guids.Add(guid);
    System.IO.File.WriteAllText($"results/game{game.Number}_teams{_teams}_games{_games}_perPair{_teamsPerPair}_duplicateLane-{_allowDuplicateLane}_{guid}.txt", gameTxt);
}

string DumpGame(Game game)
{
    var x = new System.Text.StringBuilder();
    x.AppendLine();
    x.AppendLine($"Game {game.Number} : Tries {game.Tries:n0}");
    foreach (var lane in game.LaneAssignments)
    {
        x.Append($"Lane {lane.Number}: ");
        if (lane.Teams.Count == 0)
        {
            x.Append("(empty)");
        }
        else
        {
            for (var i = 0; i < lane.Teams.Count; i++)
            {
                x.Append(lane.Teams[i]);
                if (i < lane.Teams.Count - 1)
                {
                    x.Append(" / ");
                }
            }
        }
        x.AppendLine();
    }
    return x.ToString();
}

string DumpGameBrute(KeyValuePair<int, List<Match>> game)
{
    var x = new System.Text.StringBuilder();
    
    x.AppendLine($"Game {game.Key}");
    foreach (var lane in game.Value)
    {
        x.Append($"Lane {lane.LaneNumber}: ");
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
