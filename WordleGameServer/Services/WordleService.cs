using Grpc.Core;
using System.Numerics;
using WordleGameServer.Protos;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WordleGameServer.Services
{
    public class WordleService : Wordle.WordleBase
    {
        int numUsers = 0;
        int winners = 0;//TODO: Move these stats to a file, using a mutex to prevent deadlock
        int[] userGuesses = [0, 0, 0, 0, 0, 0];

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
            //TODO: Read from file operation
            return Task.FromResult(new UserStats
            {
                //Guesses = userGuesses,//TODO: Don't know how to assign array to repeated int32
                PercentWon = (float)(Math.Round(((decimal)winners / numUsers) * 100) / 100),
                NumUsers = numUsers
            });
        }

        //TODO: Make Play bidirectional.
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
                    if (request.Word == wordOfDay)
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
                            char letter = request.Word[i];
                            if (letter == wordOfDay[i])
                            {
                                resultChars[i] = '*';
                                matches[letter]++;
                            }
                        }

                        //Search request word for letters that aren't in correct position
                        for (int i = 0; i < wordOfDay.Length; i++)
                        {
                            char letter = request.Word[i];
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
                        ResultString = resultChars.ToString() ?? ""
                    };
                    turns++;//TODO: Update the user's turns. Not sure how to get this to persist across calls, haven't looked at bidirectional yet.
                }
                else
                {
                    //TODO: Return something to communicate that the word was not valid. Do not increment turns.
                    response = new()
                    {
                        ResultString = ""
                    };
                }
                await responseStream.WriteAsync(response);
            }
        }
    }
}
