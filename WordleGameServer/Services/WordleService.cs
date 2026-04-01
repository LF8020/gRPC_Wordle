using Grpc.Core;
using WordleGameServer.Protos;

namespace WordleGameServer.Services
{
    public class WordleService : Wordle.WordleBase
    {
        int numUsers = 0;
        int percentWon = 0;
        int[] guesses = [0, 0, 0, 0, 0, 0];

        //GetStats will return stored values which are updated each time a user finishes a game.
        public override Task<UserStats> GetStats(StatRequest request, ServerCallContext context)
        {
            return Task.FromResult(new UserStats
            {
                Guesses = guesses,//TODO: Don't know how to assign array to repeated int32
                PercentWon = percentWon,
                NumUsers = numUsers
            });
        }
    }
}
