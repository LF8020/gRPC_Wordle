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
        //public override Task<UserStats> GetStats(StatRequest request, ServerCallContext context)
        //{
        //    return Task.FromResult(new UserStats
        //    {
        //        Guesses = userGuesses,//TODO: Don't know how to assign array to repeated int32
        //        PercentWon = Math.Round(((decimal)winners / numUsers) * 100) / 100,
        //        NumUsers = numUsers
        //    });
        //}

        public override Task<PlayValues> Play(GuessedWord request, ServerCallContext context)
        {
            string wordOfDay = "zonal";//TODO: Will call other service later.
            int turns = 0;
            bool gameWon = false;
            char[] resultChars = new char[5];
            while (!gameWon && turns < 6)
            {
                turns++;
                if (request.Word == wordOfDay)
                {
                    gameWon = true;
                    for (int i = 0; i < 5; i++)
                    {
                        resultChars[i] = '*';
                    }
                    numUsers++;
                    winners++;
                    userGuesses[turns - 1]++;
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
                                resultChars[i] = '?';
                                matches[letter] += 1;
                            }
                        }
                    }
                }
            }
            string resultString = resultChars.ToString() ?? "";
            return Task.FromResult(new PlayValues
            {
                ResultString = resultString
            });
        }
    }
}
