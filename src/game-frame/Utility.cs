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

        public bool BetterThan(Utility other, int forPlayerIndex)
        {
            float myUtility = 0f;
            float myUtilityOther = 0f;
            float opponentsUtility = 0f;
            float opponentsUtilityOther = 0f;
            for (int i = 0; i < scores.Length; i++)
            {
                if (i == forPlayerIndex)
                {
                    myUtility = scores[i];
                    myUtilityOther = other.scores[i];
                }
                else
                {
                    opponentsUtility += scores[i];
                    opponentsUtilityOther += other.scores[i];
                }
            }
            if (myUtility > myUtilityOther) return true;
            if (myUtility < myUtilityOther) return false;
            return opponentsUtility > opponentsUtilityOther;
        }

        public override string ToString()
        {
            // this is server protocol for announcing results
            var all = Enumerable.Range(0, roles.Length).Select(i => roles[i] + "=" + scores[i]);
            return string.Join(" ", all);
        }

        public static Utility Min(string[] roles, int roleIndex)
        {
            var result = new Utility(roles);
            for (int i = 0; i < roles.Length; i++)
            {
                result.scores[i] = i == roleIndex ? float.MinValue : float.MaxValue;
            }
            return result;
        }
    }
}
