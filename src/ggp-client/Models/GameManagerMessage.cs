namespace ggp_client.Models
{
    public abstract class GameManagerMessage
    {
    }

    public class InfoMessage : GameManagerMessage
    {
    }

    public class StartMessage : GameManagerMessage
    {
        public string Id { get; set; }
        public string Role { get; set; }
        public string Rules { get; set; }
        public int StartClock { get; set; }
        public int PlayClock { get; set; }
    }

    public class PlayMessage : GameManagerMessage
    {
        public string Id { get; set; }
        public string Actions { get; set; }
    }

    public class StopMessage : GameManagerMessage
    {
        public string Id { get; set; }
        public string Actions { get; set; }
    }

    public class AbortMessage : GameManagerMessage
    {
        public string Id { get; set; }
    }
}