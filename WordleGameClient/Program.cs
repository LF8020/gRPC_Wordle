using Grpc.Core;
using Grpc.Net.Client;
using WordleGameServer.Protos;
namespace WordleGameClient
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            //Tests for WordServer
            
            // Connect to the service
            var channel = GrpcChannel.ForAddress("https://localhost:7289");
            var word = new Wordle.WordleClient(channel);
            using (AsyncDuplexStreamingCall<GuessedWord, PlayValues> call = word.Play())
            {
                PlayValues wordleGame;
                GuessedWord input = new();
                do
                {
                    Console.WriteLine("Please guess a 5 letter word");
                    input.Guess = Console.ReadLine() ?? "";

                    //await write to bidirectional RPC
                    await call.RequestStream.WriteAsync(input);
                    //await read from bidirectional RPC
                    await call.ResponseStream.MoveNext();
                    //assign values returned from RPC to PlayValues object
                    wordleGame = call.ResponseStream.Current;

                    Console.WriteLine(wordleGame.GuessAccuracy);
                    Console.WriteLine(wordleGame.ValidGuess);
                    Console.WriteLine(wordleGame.Message);
                } while (!wordleGame.GameOver);
            }
            // Create a parameter of the type required by the service method
            //WordRequest request = new();
            //request.Date = DateTime.Now.Ticks;
            //WordResult result = word.GetWord(request);
            //Console.WriteLine(result.Word);
            
            //InputWord request2 = new();
            //Console.WriteLine("Enter a word to see if it matches a word in wordle.json");
            //request2.Word = Console.ReadLine();
            //ValidationResult result2 = word.ValidateWord(request2);
            //Console.WriteLine(result2.IsValid);
            //Console.ReadLine();
        }
    }
}
