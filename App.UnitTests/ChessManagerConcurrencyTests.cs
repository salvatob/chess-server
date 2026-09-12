using System.Diagnostics;
using ChessBotCore;
using ChessBotCore.Game;
using Xunit;

namespace App.UnitTests;

public class ChessManagerConcurrencyTests {
    [Fact]
    public async Task RegisterPlayer_And_StartGame_Concurrent_ShouldNotLoseData() {
        var manager = new ChessManager(10);
        int iterations = 1000;
        var tasks = new List<Task>();

        for (int i = 0; i < iterations; i++) {
            int id = manager.CreateGame(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5), TimeSpan.Zero);
            
            var t1 = Task.Run(() => {
                try {
                    manager.RegisterPlayer(id, new MockPlayer(), true);
                } catch (KeyNotFoundException) { } // Might happen if StartGame already removed it
                catch (InvalidOperationException) { } // Might happen if player already set
            });

            var t2 = Task.Run(() => {
                try {
                    manager.RegisterPlayer(id, new MockPlayer(), false);
                } catch (KeyNotFoundException) { }
                catch (InvalidOperationException) { }
            });

            var t3 = Task.Run(() => {
                try {
                    manager.StartGame(id);
                } catch (KeyNotFoundException) { }
                catch (InvalidOperationException) { }
            });

            tasks.Add(t1);
            tasks.Add(t2);
            tasks.Add(t3);
        }

        await Task.WhenAll(tasks);
        
        // If there was no crash, it's a good sign, but the real issue is hard to catch with just this.
        // We need a more targeted test that reveals the race.
    }

    [Fact]
    public void RegisterPlayer_RaceCondition_Demonstration() {
        int whiteAlreadySetCount = 0;
        int successCount = 0;

        for (int i = 0; i < 1000; i++) {
            var manager = new ChessManager();
            int id = manager.CreateGame(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5), TimeSpan.Zero);

            var barrier = new Barrier(2);

            var player1 = new MockPlayer();
            var player2 = new MockPlayer();

            var t1 = Task.Run(() => {
                barrier.SignalAndWait();
                try {
                    manager.RegisterPlayer(id, player1, true);
                    Interlocked.Increment(ref successCount);
                } catch (InvalidOperationException ex) when (ex.Message == "White player already set.") {
                    Interlocked.Increment(ref whiteAlreadySetCount);
                }
            });

            var t2 = Task.Run(() => {
                barrier.SignalAndWait();
                try {
                    manager.RegisterPlayer(id, player2, true);
                    Interlocked.Increment(ref successCount);
                } catch (InvalidOperationException ex) when (ex.Message == "White player already set.") {
                    Interlocked.Increment(ref whiteAlreadySetCount);
                }
            });

            Task.WaitAll(t1, t2);
        }

        // With a race condition, it's possible that successCount > 1000 because both threads
        // might successfully register the white player if they both pass the check before either sets it.
        // We want to prove that successCount is ALWAYS 1000.
        Assert.Equal(1000, successCount);
        Assert.Equal(1000, whiteAlreadySetCount);
    }
}
