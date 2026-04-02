using Grpc.Core;
using Grpc.Net.Client;
using System.Text.Json;
using System.Xml.Linq;
using WordleGameServer.Protos;
using WordServer.Protos;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WordleGameServer.Services
{
    public class UserData
    {
        public int NumUsers { get; set; }
        public int Winners { get; set; }
        public List<int> UserGuesses { get; set; }

        public UserData()
        {
            NumUsers = 0;
            Winners = 0;
            UserGuesses = new List<int>()
            {
                0,0,0,0,0,0
            };
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
            string json = File.ReadAllText("userData.json");
            UserData data = Newtonsoft.Json.JsonConvert.DeserializeObject<UserData>(json) ?? new();

            UserStats response = new()
            {
                PercentWon = (float)(Math.Round(((decimal)data.Winners / data.NumUsers) * 100) / 100),
                NumUsers = data.NumUsers
            };
            response.Guesses.AddRange(data.UserGuesses);
            return Task.FromResult(response);
        }

        public bool GameOver(string guess, string wordOfDay, int guessIndex)
        {
            if (GameWin(guess, wordOfDay)) return true;
            if (guessIndex == 5) return true;

            return false;
        }

        public bool GameWin(string guess, string wordOfDay)
        {
            if (guess == wordOfDay)
            {
                return true;
            }
            return false;
        }

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

        public void UpdateCorrectLetters(string guess, string guessAccuracy, List<string> currentCorrectLetters)
        {

            for (int i = 0; i < 5; i++)
            {
                string currentLetter = guess[i].ToString();

                if ((guessAccuracy[i] == '*' || guessAccuracy[i] == '?') && !currentCorrectLetters.Contains(currentLetter))
                {
                    currentCorrectLetters.Add(currentLetter);
                }
            }
        }

        public void UpdateIncorrectLetters(string guess, string wordOfDay, List<string> currentIncorrectLetters)
        {
            for (int i = 0; i < 5; i++)
            {
                string currentLetter = guess[i].ToString();

                if (!wordOfDay.Contains(currentLetter) && !currentIncorrectLetters.Contains(currentLetter))
                {
                    currentIncorrectLetters.Add(currentLetter);
                }
            }
        }

        public void UpdateAvailableLetters(List<string> correctLetters, List<string> incorrectLetters, List<string> availableLetters)
        {
            for (int i = availableLetters.Count - 1; i >= 0; i--)
            {
                string currentLetter = availableLetters[i];

                if (correctLetters.Contains(currentLetter) || incorrectLetters.Contains(currentLetter))
                {
                    availableLetters.RemoveAt(i);
                }
            }
        }

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
            int guessIndex = 0;


            List<string> correctLetters = new List<string>();
            List<string> incorrectLetters = new List<string>();

            List<string> availableLetters = new List<string>()
            {
                "a","b","c","d","e","f",
                "g","h","i","j","k","l",
                "m","n","o","p","q","r",
                "s","t","u","v","w","x",
                "y","z"
            };

            while (await requestStream.MoveNext() && guessIndex < 6)
            {
                InputWord wordToCheck = new InputWord()
                {
                    Word = requestStream.Current.Guess.ToLower()
                };

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
                }
                else
                {

                    string guessAccuracy = GuessAccuracy(wordToCheck.Word, wordOfDay);

                    UpdateCorrectLetters(wordToCheck.Word, guessAccuracy, correctLetters);
                    UpdateIncorrectLetters(wordToCheck.Word, guessAccuracy, incorrectLetters);
                    UpdateAvailableLetters(correctLetters, incorrectLetters, availableLetters);

                    bool gameWin = GameWin(wordToCheck.Word, wordOfDay);
                    bool gameOver = GameOver(wordToCheck.Word, wordOfDay, guessIndex);
                    string message = "Countinue...";

                    if (gameWin)
                        message = "You Win!";
                    if (gameOver)
                        message = "You Lose :( the word was" + wordOfDay;

                    PlayValues response = new PlayValues
                    {
                        ValidGuess = true,
                        GameOver = gameOver,
                        GameWin = gameWin,
                        GuessIndex = guessIndex,
                        GuessAccuracy = guessAccuracy,
                        Message = message
                    };

                    response.CorrectLetters.AddRange(correctLetters);
                    response.IncorrectLetters.AddRange(incorrectLetters);
                    response.AvailableLetters.AddRange(availableLetters);


                    await responseStream.WriteAsync(response);

                    if (gameOver)
                    {
                        m.WaitOne();
                        try
                        {
                            UserData data;
                            string json;
                            try
                            {
                                json = File.ReadAllText("userData.json");
                                data = Newtonsoft.Json.JsonConvert.DeserializeObject<UserData>(json) ?? new();
                            }
                            catch (FileNotFoundException)
                            {
                                File.WriteAllText("userData.json", "");
                                data = new();
                            }
                            

                            data.NumUsers++;
                            if (gameWin)
                            {
                                data.Winners++;
                                data.UserGuesses[guessIndex]++;
                            }
                            json = JsonSerializer.Serialize(data);
                            File.WriteAllText("userData.json", json);
                        }
                        finally
                        {
                            m.ReleaseMutex();
                        }
                        break;
                    }
                    guessIndex++;
                }
            }
        }

                /*
                public override async Task Play(IAsyncStreamReader<GuessedWord> requestStream, IServerStreamWriter<PlayValues> responseStream, ServerCallContext context)
                {
                    string wordOfDay = "zonal";//TODO: Will call other service later to get word of day. zonal is just a test assignment

                    int turns = 0;
                    bool gameWon = false;
                    bool validWord = true;//TODO: Replace with call to other service's validateWord to check guessed word against it
                    char[] resultChars = new char[5];//Result string sent back, the *, ?, X

                    while (await requestStream.MoveNext() && !gameWon && turns < 6)//TODO: need so somehow check if another word is played according to project file, not sure what they mean.
                    {
                        GuessedWord request = requestStream.Current;
                        PlayValues response;

                        if (validWord)
                        {
                            if (request.Guess == wordOfDay)
                            {
                                gameWon = true;
                                for (int i = 0; i < 5; i++)
                                {
                                    resultChars[i] = '*';
                                }
                                numUsers++;
                                winners++;//TODO: update these to read file operation, then adjust value, then write back
                                userGuesses[turns]++;
                            }
                            else
                            {
                                Dictionary<char, int> matches = new Dictionary<char, int>()
                                {
                                    ['A'] = 0,
                                    ['B'] = 0,
                                    ['C'] = 0,
                                    ['D'] = 0,
                                    ['E'] = 0,
                                    ['F'] = 0,
                                    ['G'] = 0,
                                    ['H'] = 0,
                                    ['I'] = 0,
                                    ['J'] = 0,
                                    ['K'] = 0,
                                    ['L'] = 0,
                                    ['M'] = 0,
                                    ['N'] = 0,
                                    ['O'] = 0,
                                    ['P'] = 0,
                                    ['Q'] = 0,
                                    ['R'] = 0,
                                    ['S'] = 0,
                                    ['T'] = 0,
                                    ['U'] = 0,
                                    ['V'] = 0,
                                    ['W'] = 0,
                                    ['X'] = 0,
                                    ['Y'] = 0,
                                    ['Z'] = 0,
                                };

                                //Check to see if letter is in corect position
                                for (int i = 0; i < wordOfDay.Length; i++)
                                {
                                    char letter = request.Guess[i];
                                    if (letter == wordOfDay[i])
                                    {
                                        resultChars[i] = '*';
                                        matches[letter]++;
                                    }
                                }

                                //Search request word for letters that aren't in correct position
                                for (int i = 0; i < wordOfDay.Length; i++)
                                {
                                    char letter = request.Guess[i];
                                    if (CountFrequency(wordOfDay, letter) == 0)
                                    {
                                        resultChars[i] = 'X';

                                    }
                                    else if (letter != wordOfDay[i])
                                    {
                                        if (matches[letter] < CountFrequency(wordOfDay, letter))
                                        {
                                            resultChars[i] = '?';//Looking at this if statement we're given, it seems wrong.
                                            matches[letter] += 1;//I know it's meant to check if there's more than one letter of the same letter but I feel like it's updated wrong, unless matches resets between iterations.
                                        }
                                        else
                                        {
                                            resultChars[i] = 'X';//Should theoretically be called when there's more letters than matches.
                                            //Example: aabbb
                                            //Input word: aaabb
                                            // **X**

                                            //Example: aacbd
                                            //Input word: bbaaa
                                            // ?X??X

                                            //In a nutshell, if there's too many of a letter, subsequent iterations of that letter are 'wrong'
                                        }
                                    }
                                }
                            }
                            response = new()
                            {
                                Message = resultChars.ToString() ?? ""
                            };
                            turns++;//TODO: Update the user's turns. Not sure how to get this to persist across calls, haven't looked at bidirectional yet.
                        }
                        else
                        {
                            //TODO: Return something to communicate that the word was not valid. Do not increment turns.
                            response = new()
                            {
                                Message = ""
                            };
                        }
                        await responseStream.WriteAsync(response);
                    }
                }
                */
            }
        } 
