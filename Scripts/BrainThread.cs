using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace NeuralNetwork2048_v2
{
    public class BrainThread : IDisposable
    {
        private Stopwatch timer = new Stopwatch();
        public int ticksCount;
        public long totalMs;
        private bool stop;

        private Puzzle puzzle;
        private List<Brain> Brains = new List<Brain>();
        private int PuzzlesPerBrain = 1;
        private readonly bool randomBoard;
        public bool Running { get; private set; }

        public List<Brain> Winners = new List<Brain>();

        public BrainThread(bool randomBoard)
        {
            this.randomBoard = randomBoard;
            var thread = new Thread(Loop);
            thread.Start();
        }

        public void RunTask(List<Brain> brains, int puzzlesPerBrain = 1)
        {
            Brains = brains;
            PuzzlesPerBrain = puzzlesPerBrain;
            Winners.Clear();
            Running = true;
        }

        public void Dispose()
        {
            stop = true;
        }
        private void Loop()
        {
            while (!stop)
            {
                if (Running)
                {
                    foreach (var brain in Brains)
                    {
                        RunBrain(brain);
                    }
                    Running = false;
                }

                // wait for next task
                Thread.Sleep(1);
            }
        }

        private void RunBrain(Brain B)
        {
            var puzzleNumber = 1;
            var playing = true;
            var puzzle = new Puzzle(randomBoard: randomBoard);
            while (playing)
            {
                BrainTick(B, ref puzzleNumber, ref playing, ref puzzle);
            }
        }

        public void BrainTick(Brain B, ref int puzzleNumber, ref bool playing, ref Puzzle puzzle)
        {
            timer.Restart();

            B.CalculateMove(puzzle);
            B.MakeMove(puzzle);

            if (puzzle.hasFinished)
                Finish(B, ref puzzleNumber, ref playing, ref puzzle);

            timer.Stop();
            ticksCount++;
            totalMs += timer.ElapsedMilliseconds;
        }

        private void Finish(Brain B, ref int puzzleNumber, ref bool playing, ref Puzzle puzzle)
        {
            var score = puzzle.Score;
            if (puzzle.hasWon)
            {
                Winners.Add(B);
                score *= 10;
            }

            B.Fitness += score;

            puzzleNumber++;
            if (puzzleNumber < PuzzlesPerBrain)
            {
                puzzle = new Puzzle(randomBoard: randomBoard);
            }
            else
            {
                puzzleNumber = 0;
                playing = false;
            }
        }
    }
}
