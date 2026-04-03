using Grpc.Core;
using Grpc.Net.Client;
using System.Text.Json;
using System.Xml.Linq;
using WordleGameServer.Protos;
using WordServer.Protos;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WordleGameServer.Services
{
    //WordleService, Programmed by Lucas Frada and Ryan Dekoninck
    public class UserData//Declare user object to better store 
    {
        public int NumUsers { get; set; }
        public int Winners { get; set; }
        public string WordOfDay { get; set; }
        public int[] UserGuesses { get; set; }

        public UserData()
        {
            WordOfDay = "";
            NumUsers = 0;
            Winners = 0;
            UserGuesses = [0, 0, 0, 0, 0, 0];
        }
        public UserData(string wordOfDay)
        {
            WordOfDay = wordOfDay;
            NumUsers = 0;
            Winners = 0;
            UserGuesses = [0, 0, 0, 0, 0, 0];
        }
    }
    public class WordleService : Wordle.WordleBase
    {
        public int CountFrequency(string word, char letter)
        {//check frequency of letter in a word
            int count = 0;
            for (int i = 0; i < word.Length; i++)
            {
                count++;
            }
            return count;
        }

        //GetStats will return stored values from a file which are updated each time a user finishes a game.
        public override Task<UserStats> GetStats(StatRequest request, ServerCallContext context)
        {
            Mutex m = new();
            m.WaitOne();
            string json = File.ReadAllText("userData.json");
            UserData data = Newtonsoft.Json.JsonConvert.DeserializeObject<UserData>(json) ?? new();
            UserStats response = new();

            //If users read is greater than 0, create object using read values to return
            if (data.NumUsers > 0)
            {
                response = new()
                {
                    PercentWon = (float)(Math.Round(((decimal)data.Winners / data.NumUsers) * 100) / 100),
                    NumUsers = data.NumUsers
                };
            }
            else
            {
                //Else, make new object
                response = new()
                {
                    PercentWon = 0,
                    NumUsers = 0
                };
            }

            response.Guesses.AddRange(data.UserGuesses);

            m.ReleaseMutex();
            return Task.FromResult(response);
        }

        //Check if game is over
        public bool GameOver(string guess, string wordOfDay, int guessIndex)
        {
            if (GameWin(guess, wordOfDay)) return true;
            if (guessIndex == 5) return true;

            return false;
        }

        //Check to see if game was won
        public bool GameWin(string guess, string wordOfDay)
        {
            if (guess == wordOfDay)
            {
                return true;
            }
            return false;
        }

        //Check guess accuracy and return '*' if correct, '?' if correct but in wrong place, 'X' if wrong.
        public string GuessAccuracy(string guess, string wordOfDay)
        {
            char[] result = new char[5];
            Dictionary<char, int> leftOvers = new Dictionary<char, int>();

            //goes over letters once spots perfect spots and lists left overs and how many of each character there is
            for (int i = 0; i < 5; i++)
            {
                if (guess[i] == wordOfDay[i])
                {
                    result[i] = '*';
                }
                else
                {
                    if (leftOvers.ContainsKey(wordOfDay[i]))
                    {
                        leftOvers[wordOfDay[i]]++;
                    }
                    else
                    {
                        leftOvers[wordOfDay[i]] = 1;
                    }
                }
            }

            // goes over and check if the guessed word is part of the leftovers and hasnt been used yet
            for (int i = 0; i < 5; i++)
            {
                if (result[i] == '*') //if already correctly guessed position continue
                    continue;

                if (leftOvers.ContainsKey(guess[i]) && leftOvers[guess[i]] > 0) //if leftovers contains character and hasnt been accounted for
                {
                    result[i] = '?';
                    leftOvers[guess[i]]--;
                }
                else //either doesnt contain character or is already accounted for
                {
                    result[i] = 'x';
                }
            }
            string stringResult = "";
            for(int i = 0; i < 5; i++)
            {
                stringResult += result[i];
            }
            return stringResult;
        }

        //Update letters correct list 
        public List<string> UpdateCorrectLetters(string guess, string guessAccuracy, List<string> currentCorrectLetters)
        {

            for (int i = 0; i < 5; i++)
            {
                string currentLetter = guess[i].ToString();

                if ((guessAccuracy[i] == '*' || guessAccuracy[i] == '?') && !currentCorrectLetters.Contains(currentLetter))
                {
                    currentCorrectLetters.Add(currentLetter);
                }
            }
            return currentCorrectLetters;
        }

        //Update incorrect letters list
        public List<string> UpdateIncorrectLetters(string guess, string wordOfDay, List<string> currentIncorrectLetters)
        {
            for (int i = 0; i < 5; i++)
            {
                string currentLetter = guess[i].ToString();

                if (!wordOfDay.Contains(currentLetter) && !currentIncorrectLetters.Contains(currentLetter))
                {
                    currentIncorrectLetters.Add(currentLetter);
                }
            }
            return currentIncorrectLetters;
        }

        //Update available letters list
        public List<string> UpdateAvailableLetters(List<string> correctLetters, List<string> incorrectLetters, List<string> availableLetters)
        {
            for (int i = availableLetters.Count - 1; i >= 0; i--)
            {
                string currentLetter = availableLetters[i];

                if (correctLetters.Contains(currentLetter) || incorrectLetters.Contains(currentLetter))
                {
                    availableLetters.RemoveAt(i);
                }
            }
            return availableLetters;
        }

        //Play RPC
        public override async Task Play(IAsyncStreamReader<GuessedWord> requestStream, IServerStreamWriter<PlayValues> responseStream, ServerCallContext context)
        {
            Mutex m = new();
            var channel = GrpcChannel.ForAddress("https://localhost:7139");
            var word = new Word.WordClient(channel);

            //Calls WordServer to request word of day
            WordRequest request = new WordRequest()
            {
                Date = new DateTime().Ticks
            };
            string wordOfDay = word.GetWord(request).Word;//Assign word to string
            int guessIndex = 1;

            //letter lists
            List<string> correctLetters = [];
            List<string> incorrectLetters = [];
            List<string> availableLetters = 
            [
                "a","b","c","d","e","f",
                "g","h","i","j","k","l",
                "m","n","o","p","q","r",
                "s","t","u","v","w","x",
                "y","z"
            ];

            var response = new PlayValues
            {
                ValidGuess = true,
                GameOver = false,
                GameWin = false,
                GuessIndex = 1,
                GuessAccuracy = "",
                Message = "Start"
            };

            response.AvailableLetters.AddRange(availableLetters);
            await responseStream.WriteAsync(response);

            //While stream is still active and guesses are below 6
            while (await requestStream.MoveNext() && guessIndex < 6)
            {
                InputWord wordToCheck = new InputWord()
                {//Get new input word from requestStream
                    Word = requestStream.Current.Guess.ToLower()
                };

                //If word isn't valid, send back "Invalid Guess"
                if (!word.ValidateWord(wordToCheck).IsValid)
                {
                    await responseStream.WriteAsync(new PlayValues
                    {
                        ValidGuess = false,
                        GameOver = false,
                        GameWin = false,
                        GuessIndex = guessIndex,
                        GuessAccuracy = "",
                        Message = "Invalid Guess"
                    });
                    continue;
                }
                //Otherwise, continue
                else
                {
                    //Check guessAccuracy and send to a new string
                    string guessAccuracy = GuessAccuracy(wordToCheck.Word, wordOfDay);

                    //Update letter lists
                    correctLetters = UpdateCorrectLetters(wordToCheck.Word, guessAccuracy, correctLetters);
                    incorrectLetters = UpdateIncorrectLetters(wordToCheck.Word, guessAccuracy, incorrectLetters);
                    availableLetters = UpdateAvailableLetters(correctLetters, incorrectLetters, availableLetters);

                    //Check if game is won or game is over
                    bool gameWin = GameWin(wordToCheck.Word, wordOfDay);
                    bool gameOver = GameOver(wordToCheck.Word, wordOfDay, guessIndex);

                    //Assign message according to gameWin or gameOver
                    string message;
                    if (gameWin)
                        message = "You Win!";
                    else if (gameOver)
                        message = "You Lose :( the word was" + wordOfDay;
                    else
                        message = "Continue...";

                    
                    
                    //Make a response object
                    response = new PlayValues
                    {
                        ValidGuess = true,
                        GameOver = gameOver,
                        GameWin = gameWin,
                        GuessIndex = guessIndex,
                        GuessAccuracy = guessAccuracy,
                        Message = message
                    };
                    //Add lists to object in form of ranges
                    response.CorrectLetters.AddRange(correctLetters);
                    response.IncorrectLetters.AddRange(incorrectLetters);
                    response.AvailableLetters.AddRange(availableLetters);

                    //write to responseStream
                    await responseStream.WriteAsync(response);

                    //If the game is over, continue final loop
                    if (gameOver)
                    {
                        //Lock the mutex
                        m.WaitOne();
                        try
                        {
                            UserData data;
                            string json;
                            try
                            {
                                //Read file in to adjust values
                                json = File.ReadAllText("userData.json");
                                data = Newtonsoft.Json.JsonConvert.DeserializeObject<UserData>(json) ?? new(wordOfDay);
                            }
                            catch (FileNotFoundException)
                            {
                                //If file not found, make one and make an empty object with properties set to 0
                                File.WriteAllText("userData.json", "");
                                data = new(wordOfDay);
                            }
                            //Adjust values previously read
                            data.NumUsers++;
                            if (gameWin)
                            {
                                data.Winners++;
                                data.UserGuesses[guessIndex - 1]++;
                            }
                            //If word of day changed, (such as a new day) delete userData file and revert data to default values. Write those default values
                            if (data.WordOfDay != wordOfDay)
                            {
                                File.Delete("userData.json");
                                data.NumUsers = 0;
                                data.Winners = 0;
                                data.UserGuesses = [0, 0, 0, 0, 0, 0];
                            }
                            //Write to file under protection of the mutex
                            json = JsonSerializer.Serialize(data);
                            File.WriteAllText("userData.json", json);
                        }
                        finally
                        {
                            //Finally, release mutex when done.
                            m.ReleaseMutex();
                        }
                        //Break from loop to not unnecessarily increase guessIndex
                        break;
                    }
                    guessIndex++;
                }
            }
        }
    }
} 
