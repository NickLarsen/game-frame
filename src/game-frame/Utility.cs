using System.Linq;
using GameFrame.Helpers;

namespace GameFrame
{
    public class Utility
    {
        private readonly string[] roles;
        private readonly float[] scores;

        public bool IsTerminal { get; set; }

        public Utility(string[] roles)
        {
            this.roles = roles;
            scores = new float[roles.Length];
        }

        public float this[string role]
        {
            get { return scores[roles.IndexOf(role)]; }
            set { scores[roles.IndexOf(role)] = value; }
        }

        public float this[int playerNumber]
        {
            get { return scores[playerNumber]; }
            set { scores[playerNumber] = value; }
        }

        public override string ToString()
        {
            // this is server protocol for announcing results
            var all = Enumerable.Range(0, roles.Length).Select(i => roles[i] + "=" + scores[i]);
            return string.Join(" ", all);
        }
    }
}
