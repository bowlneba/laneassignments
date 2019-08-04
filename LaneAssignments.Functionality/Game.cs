using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaneAssignments
{
    public class Game
    {
        public int Number { get; set; }

        public List<Lane> LaneAssignments { get; set; }

        public int Tries { get; set; }

        public Game(int number, int tries, params Lane[] lanes)
        {
            Number = number;
            Tries = tries;

            LaneAssignments = new List<Lane>();
            LaneAssignments.AddRange(lanes);
        }

        public void ClearLaneAssignments()
            => LaneAssignments.Clear();

        public void AddLanes(params Lane[] lanes)
            => LaneAssignments.AddRange(lanes);

        public IEnumerable<int> TeamPairings(int team)
        {
            var lane = LaneAssignments.Single(item => item.Teams.Contains(team));

            return lane.Teams.Where(item => item != team);
        }

        public int TeamLane(int team)
            => LaneAssignments.Single(item => item.Teams.Contains(team)).Number;

        public override string ToString() => $"Game: {Number}";
    }
}
