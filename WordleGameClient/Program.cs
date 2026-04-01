using Grpc.Net.Client;
using WordServer.Protos;//TODO: currently a test
namespace WordleGameClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ////Tests for WordServer
            //
            //// Connect to the service
            //var channel = GrpcChannel.ForAddress("https://localhost:7139");
            //var word = new Word.WordClient(channel);
            //// Create a parameter of the type required by the service method
            //WordRequest request = new();
            //request.Date = DateTime.Now.Ticks;
            //WordResult result = word.GetWord(request);
            //Console.WriteLine(result.Word);
            //
            //InputWord request2 = new();
            //Console.WriteLine("Enter a word to see if it matches a word in wordle.json");
            //request2.Word = Console.ReadLine();
            //ValidationResult result2 = word.ValidateWord(request2);
            //Console.WriteLine(result2.IsValid);
            //Console.ReadLine();
        }
    }
}
