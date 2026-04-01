using Google.Protobuf.Compiler;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using WordServer.Protos;


namespace WordServer.Services
{
    //gRPC service called DailyWord
    //with the following RPCs:
    //i. GetWord
    //1.    Returns a 5 - letter English word from the file wordle.json.
    //2.    Returns the same 5-letter word anytime it is invoked during the same
    //      calendar day.
    //3.    Returns different 5-letter words if invoked on two consecutive calendar days.
    //ii.ValidateWord
    //1.    Returns a Boolean value of true if the word argument passed-in matches a
    //      word in the file wordle.json
    public class DailyWordService : Word.WordBase
    {

        //private readonly ILogger<DailyWordService> _logger;
        //public DailyWordService(ILogger<DailyWordService> logger)
        //{
        //    _logger = logger;
        //}

        public override Task<ValidationResult> ValidateWord(InputWord request, ServerCallContext context)
        {//Validate word against wordle.json contents
            string json = File.ReadAllText("wordle.json");//have to do this twice for some reason, words will get reset if it's a class property.
            List<string> words = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(json) ?? [];
            foreach (string word in words)
            {
                if (request.Word == word) 
                    return Task.FromResult(new ValidationResult
                    {
                        IsValid = true
                    });
            }
            return Task.FromResult(new ValidationResult
            {
                IsValid = false
            });
        }

        public override Task<WordResult> GetWord(WordRequest request, ServerCallContext context)
        {
            string json = File.ReadAllText("wordle.json");
            List<string> words = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(json) ?? [];

            TimeSpan spanSinceEpoch = new DateTime(request.Date) - new DateTime(1970, 1, 1);
            int daysSinceEpoch = (int)spanSinceEpoch.TotalDays;//use days since epoch as seed for RNG
            Random random = new Random(daysSinceEpoch);
            int randomNumber = random.Next(0, 2315);//generate random number between 0 and 2314 for the array grabbed from wordle.json
            return Task.FromResult(new WordResult
            {
                Word = words[randomNumber]
            });
        }
    }
}

