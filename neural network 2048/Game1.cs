using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace neural_network_2048
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Texture2D WhitePixel;
        SpriteFont font1;
        KeyboardState oldstate;
        Random r = new Random();
        
        public Puzzle ActivePuzzle;

        int drawsize = 50;
        int drawsperation = 2;

        List<Brain> Brains;
        int BrainsPerGen = 1000;
        int PuzzlesPerBrain = 1;

        int brainNumber = 0;
        int puzzleNumber = 0;
        int generation = 0;
        bool nextgen = false;
        double MaxFit;
        double AvgFit;
        double MinFit;

        double BrainTickSpeed = 1;
        double tickCounter=0;
        bool Paused = false;
        bool WholeGeneration = true;
        bool Do1Gen = false;

        StreamWriter output;


        void DoBrainTick()
        {
            Brain B = Brains[brainNumber];

            B.CalculateMove(ActivePuzzle);
            B.MakeMove(ActivePuzzle);

            if (ActivePuzzle.hasWon)
            {
                // have perfect brain
            }
            if (ActivePuzzle.hasLost)
            {
                B.Fitness += ActivePuzzle.Score;
                ActivePuzzle = new Puzzle();

                puzzleNumber++;
                if (puzzleNumber>=PuzzlesPerBrain)
                {
                    puzzleNumber = 0;

                    brainNumber++;
                    if (brainNumber >= Brains.Count)
                    {
                        brainNumber = 0;

                        CalculateGenFitness();
                        GetNextGenCrossover();
                        generation++;
                        nextgen = true;
                    }
                }
            }
        }

        void CalculateGenFitness()
        {
            List<Brain> byfitness = Brains.OrderByDescending(x => x.Fitness).ToList();

            MaxFit = byfitness[0].Fitness;
            MinFit = byfitness[byfitness.Count - 1].Fitness;
            AvgFit = 0;
            //average fitness
            for (int n = 0; n < byfitness.Count; n++)
            {
                AvgFit += byfitness[n].Fitness;
            }
            AvgFit /= byfitness.Count;
            MaxFit /= PuzzlesPerBrain;
            AvgFit /= PuzzlesPerBrain;
            MinFit /= PuzzlesPerBrain;
            output.WriteLine("{0},{1},{2},{3}", generation, MaxFit, AvgFit, MinFit);
        }

        void GetNextGen()
        {
            List<Brain> byfitness = Brains.OrderByDescending(x => x.Fitness).ToList();

            Brains = new List<Brain>();
            int skiped = 0;
            for (int n=0;n<byfitness.Count/2;n++)
            {
                // chance to skip
                // n/count max chance of skiping (0% for first brain 50% for last brain
                // if chance is > random then skip
                bool skip = (n / byfitness.Count) > r.NextDouble();

                if (skip)
                {
                    skiped++;
                }
                else
                {
                    Brains.Add(byfitness[n + skiped].Evolve());
                    Brains.Add(byfitness[n + skiped].Evolve());
                }
                
            }
            
        }

        void GetNextGenCrossover()
        {
            List<Brain> byfitness = Brains.OrderByDescending(x => x.Fitness).ToList();

            Brains = new List<Brain>();
            //10% elite continue to next generation
            for (int n=0;n<byfitness.Count/10;n++)
            {
                Brains.Add(byfitness[n]);
            }

            //create other 90% by crossover
            for (int n = 0; n < 9*byfitness.Count / 20; n++)
            {
                Brain P = byfitness[n];
                Brain Q = byfitness[(int)Math.Floor(r.NextDouble()* 4 * byfitness.Count / 5)];
                Brain C1,C2;
                Brain.Mate(P, Q, out C1, out C2);
                Brains.Add(C1);
                Brains.Add(C2);
            }


            // makes sure all fitness are reset
            for (int n = 0; n < Brains.Count; n++)
            {
                Brains[n].Fitness = 0;
            }
        }

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            IsFixedTimeStep = false;
            //TargetElapsedTime = TimeSpan.FromSeconds(1.0/50.0);
            MaxElapsedTime = TimeSpan.FromSeconds(10);

            IsMouseVisible = true;
            
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            Brain.r = new Random();

            Brains = new List<Brain>();
            //create first gen
            //100 brains
            for (int n=0;n< BrainsPerGen; n++)
            {
                Brains.Add(new Brain());
                Brains[n].Initnew();
            }
            ActivePuzzle = new Puzzle();

            

            oldstate = Keyboard.GetState();
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            WhitePixel = new Texture2D(GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            WhitePixel.SetData(new Color[] { Color.White });
            font1 = Content.Load<SpriteFont>("Arial16");

            output = new StreamWriter("output.csv");
            output.WriteLine("{0},{1},{2},{3}", "Generation", "MaxFit", "AvgFit", "MinFit");

            // TODO: use this.Content to load your game content here
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
            output.Close();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            KeyboardState state = Keyboard.GetState();

            #region human player 
            //if (state.IsKeyDown(Keys.W) && oldstate.IsKeyUp(Keys.W))
            //{
            //    ActivePuzzle.Move(Puzzle.MoveDirection.up);
            //}
            //else if (state.IsKeyDown(Keys.S) && oldstate.IsKeyUp(Keys.S))
            //{
            //    ActivePuzzle.Move(Puzzle.MoveDirection.down);
            //}
            //else if (state.IsKeyDown(Keys.A) && oldstate.IsKeyUp(Keys.A))
            //{
            //    ActivePuzzle.Move(Puzzle.MoveDirection.left);
            //}
            //else if (state.IsKeyDown(Keys.D) && oldstate.IsKeyUp(Keys.D))
            //{
            //    ActivePuzzle.Move(Puzzle.MoveDirection.right);
            //}
            #endregion

            if (state.IsKeyDown(Keys.Q) && oldstate.IsKeyUp(Keys.Q))
            {
                BrainTickSpeed *= 1.2;
            }
            if (state.IsKeyDown(Keys.E) && oldstate.IsKeyUp(Keys.E))
            {
                BrainTickSpeed /= 1.2;
            }
            if (state.IsKeyDown(Keys.Space) && oldstate.IsKeyUp(Keys.Space))
            {
                Paused = !Paused;
            }
            if (state.IsKeyDown(Keys.Enter) && oldstate.IsKeyUp(Keys.Enter))
            {
                WholeGeneration = !WholeGeneration;
            }
            if (state.IsKeyDown(Keys.LeftShift) && oldstate.IsKeyUp(Keys.LeftShift))
            {
                Do1Gen = true;
                WholeGeneration = true;
                Paused = false;
            }



            if (!Paused)
            {
                if (WholeGeneration)
                {
                    nextgen = false;
                    while (!nextgen)
                    {
                        DoBrainTick();
                    }

                    if (Do1Gen)
                    {
                        WholeGeneration = false;
                        Do1Gen = false;
                        Paused = true;
                    }
                }
                else
                {
                    tickCounter += BrainTickSpeed;
                    while (tickCounter > 1)
                    {
                        tickCounter--;
                        DoBrainTick();
                    }
                }

            }
           
            
            


            oldstate = state;
            base.Update(gameTime);
        }

         
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            spriteBatch.Begin();

            Puzzle P;
            if ((P = ActivePuzzle)!=null)
            { 
                for (int x = 0; x<P.Width;x++)
                {
                    for (int y = 0; y < P.Height; y++)
                    {
                        double v = P.Grid[x, y]==0?0:Math.Log(P.Grid[x, y], 2);
                        double m = Math.Log(P.HighestOnGrid(), 2);//Math.Log(P.Max, 2);
                        int a = Convert.ToInt32(255 *  v / m);
                        if (a!=0)
                        {
                            spriteBatch.Draw(WhitePixel, new Rectangle(x * (drawsize + drawsperation), y * (drawsize + drawsperation), drawsize, drawsize), new Color(a, a, a));
                            int b = a < 120 ? 255 : 0;
                            spriteBatch.DrawString(font1, P.Grid[x, y].ToString(), new Vector2(x * (drawsize + drawsperation), y * (drawsize + drawsperation)), new Color(b, b, b));
                        }
                    }
                }
                spriteBatch.DrawString(font1, "Score: " + P.Score.ToString(), new Vector2((P.Width + 1) * (drawsize + drawsperation), 0), Color.Black);
                spriteBatch.DrawString(font1, "brain number: " + (brainNumber + 1), new Vector2((P.Width + 1) * (drawsize + drawsperation), 50), Color.Black);
                spriteBatch.DrawString(font1, "Generation: " + (generation), new Vector2((P.Width + 1) * (drawsize + drawsperation), 100), Color.Black);
                spriteBatch.DrawString(font1, "Max Fitness: " + (MaxFit), new Vector2((P.Width + 1) * (drawsize + drawsperation), 150), Color.Black);
                spriteBatch.DrawString(font1, "Avg Fitness: " + (AvgFit), new Vector2((P.Width + 1) * (drawsize + drawsperation), 200), Color.Black);
                spriteBatch.DrawString(font1, "Min Fitness: " + (MinFit), new Vector2((P.Width + 1) * (drawsize + drawsperation), 250), Color.Black);
                //spriteBatch.DrawString(font1, "Last move: " + P.LastMove.ToString(), new Vector2((P.Width + 1) * (drawsize + drawsperation), 200), Color.Black);

                //spriteBatch.DrawString(font1, Brains[brainNumber].Mutability.ToString("0.00"), new Vector2((P.Width * 2 +3) * (drawsize + drawsperation), 250), Color.Black);


                spriteBatch.DrawString(font1, "Speed: " + BrainTickSpeed.ToString("0.00"), new Vector2(0, (P.Height + 1) * (drawsize + drawsperation)), Color.Black);


                spriteBatch.Draw(WhitePixel, new Rectangle(500, 50, 50, 50), gameTime.IsRunningSlowly?Color.Red:Color.Green);
            }

            spriteBatch.End();
            base.Draw(gameTime);
        }
        
    }
}
