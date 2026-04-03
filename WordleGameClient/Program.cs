using Grpc.Core;
using Grpc.Net.Client;
using WordleGameServer.Protos;
namespace WordleGameClient
{
    //WordleGameClient, programmed by Ryan Dekoninck
    internal class Program
    {
        public static void Format()
        { 
            Console.WriteLine("+-------------------+");
            Console.WriteLine("| W O R D L E D |");
            Console.WriteLine("+-------------------+");
            Console.WriteLine();
            Console.WriteLine("You have 6 chances to guess a 5-letter word.");
            Console.WriteLine("Each guess must be a 'playable' 5 letter word.");
            Console.WriteLine("After a guess the game will display a series of");
            Console.WriteLine("characters to show you how good your guess was.");
            Console.WriteLine("x - means the letter above is not in the word.");
            Console.WriteLine("? - means the letter should be in another spot.");
            Console.WriteLine("* - means the letter is correct in this spot.");
            Console.WriteLine();
        }

        public static void PrintLetters(List<string> letters)
        {
            for (int i = 0; i < letters.Count; i++)
            {
                Console.Write(letters[i]);
                if (!(i == letters.Count - 1))
                    Console.Write(",");
            }
            Console.WriteLine();
        }

        static async Task Main(string[] args)
        {
            Format();

            try
            {
                // Connect to the service
                var channel = GrpcChannel.ForAddress("https://localhost:7289");
                var word = new Wordle.WordleClient(channel);
                using (AsyncDuplexStreamingCall<GuessedWord, PlayValues> call = word.Play())
                {
                    //for initial values and available letters

                    await call.ResponseStream.MoveNext();
                    var response = call.ResponseStream.Current;

                    string guess = "";

                    Console.Write("Available: ");
                    PrintLetters(response.AvailableLetters.ToList());

                    while (true)
                    {

                        Console.WriteLine();
                        Console.Write($"{response.GuessIndex}: ");

                        guess = Console.ReadLine() ?? "".Trim().ToLower();
                        Console.WriteLine();

                        await call.RequestStream.WriteAsync(new GuessedWord { Guess = guess });

                        await call.ResponseStream.MoveNext();
                        response = call.ResponseStream.Current;

                        if (!response.ValidGuess)
                        {
                            Console.WriteLine($"{guess} is not a valid response please input a 5 letter word");
                            continue;
                        }

                        Console.WriteLine(response.GuessAccuracy);
                        Console.WriteLine();

                        if (response.GameOver)
                        {
                            Console.WriteLine($"{response.Message}");
                            break;
                        }

                        Console.Write("Included: ");
                        PrintLetters(response.CorrectLetters.ToList());
                        Console.Write("Available: ");
                        PrintLetters(response.AvailableLetters.ToList());
                        Console.Write("Excluded: ");
                        PrintLetters(response.IncorrectLetters.ToList());
                        Console.WriteLine();

                    } //end of game loop



                    await call.RequestStream.CompleteAsync();

                }//end of using

                UserStats userStats = await word.GetStatsAsync(new StatRequest());

                Console.WriteLine($"Players: {userStats.NumUsers}");
                Console.WriteLine($"Winners: {userStats.PercentWon}%");
                int count = 1;
                Console.WriteLine("Guess Distribution...");
                foreach(var guess in userStats.Guesses)
                {
                    Console.WriteLine($"{count}.) {guess}");
                    count++;
                }

            }//end of try
            catch (RpcException e)
            {
                Console.WriteLine("Could not connect with WordleGameServer.");
                Console.WriteLine($"gRPC error: {e.Status.Detail}");
            }
            catch (Exception e)
            {
                Console.WriteLine("uh-oh weird error");
                Console.WriteLine(e.Message);
            }
        }
    }
}
