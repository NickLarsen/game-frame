using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using ggp_client.Models;

namespace ggp_client.Helpers
{
    // classes that inherit from this should have a RoutePrefix set to the name of the bot
    public abstract class GameManagerController : Controller
    {
        [Route("")]
        public ActionResult Index()
        {
            string content = GetRequestContent();
            dynamic message = ParseMessage(content);
            string result = HandleRequest(message);
            return Content(result);
        }

        private string GetRequestContent()
        {
            return new StreamReader(Request.InputStream).ReadToEnd();
        }

        private GameManagerMessage ParseMessage(string content)
        {
            var sentence = ParseSentence(content);
            switch (sentence[0])
            {
                case "info":
                    return new InfoMessage();
                case "start":
                    return new StartMessage()
                    {
                        Id = sentence[1],
                        Role = sentence[2],
                        Rules = sentence[3],
                        StartClock = int.Parse(sentence[4]),
                        PlayClock = int.Parse(sentence[5]),
                    };
                case "play":
                    return new PlayMessage()
                    {
                        Id = sentence[1],
                        Actions = sentence[2],
                    };
                case "stop":
                    return new StopMessage()
                    {
                        Id = sentence[1],
                        Actions = sentence[2],
                    };
                case "abort":
                    return new AbortMessage()
                    {
                        Id = sentence[1],
                    };
                default:
                    throw new Exception("Unrecognized message: " + content);
            }
        }

        static readonly Regex[] messageParsers = new Regex[]
        {
            new Regex(@"^\((info)\)$", RegexOptions.Compiled),
            new Regex(@"^\((start) ([a-z][a-z0-9]*) ([^ ]+) (\(.*\)) (\d+) (\d+)\)$", RegexOptions.Compiled),
            new Regex(@"^\((play) ([a-z][a-z0-9]*) (nil|\(.*\))\)$", RegexOptions.Compiled),
            new Regex(@"^\((stop) ([a-z][a-z0-9]*) (nil|\(.*\))\)$", RegexOptions.Compiled),
            new Regex(@"^\((abort) ([a-z][a-z0-9]*)\)$", RegexOptions.Compiled),
        };
        private string[] ParseSentence(string value)
        {
            foreach (var messageParser in messageParsers)
            {
                var parsed = messageParser.Match(value);
                if (!parsed.Success) continue;
                var result = new string[parsed.Groups.Count - 1];
                for (int i = 1; i < parsed.Groups.Count; i++)
                {
                    result[i - 1] = parsed.Groups[i].Value;
                }
                return result;
            }
            throw new Exception("Failed to parse message: " + value);
        }

        protected abstract string HandleRequest(InfoMessage message);
        protected abstract string HandleRequest(StartMessage message);
        protected abstract string HandleRequest(PlayMessage message);
        protected abstract string HandleRequest(StopMessage message);
        protected abstract string HandleRequest(AbortMessage message);
    }
}