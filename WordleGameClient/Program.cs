using Grpc.Net.Client;
using WordServer.Protos;//TODO: currently a test
namespace WordleGameClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Connect to the service
            var channel = GrpcChannel.ForAddress("https://localhost:7139");
            var word = new Word.WordClient(channel);
            // Create a parameter of the type required by the service method
            WordRequest request = new();
            request.Date = DateTime.Now.Ticks;
            WordResult result = word.GetWord(request);
            Console.WriteLine(result.Word);

            InputWord request2 = new();
            request2.Word = "jokes";
            ValidationResult result2 = word.ValidateWord(request2);
            Console.WriteLine(result2.IsValid);
            Console.ReadLine();
        }
    }
}
