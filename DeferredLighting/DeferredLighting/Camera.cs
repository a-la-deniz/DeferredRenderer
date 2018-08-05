#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
#endregion

namespace DeferredLighting
{
    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    public class Camera : Microsoft.Xna.Framework.GameComponent
    {
        PAB.HiPerfTimer hpt;
        private float cameraArc = -30;
        Vector3 cameraPos = new Vector3(179.8928f, 202.3825f, -135.3956f);
        float xDifference = -44.78f;
        float yDifference = 104.37f;
        Vector3 camCenter = Vector3.UnitZ;
        Vector3 camUp = Vector3.UnitY;
        Vector3 camForward = Vector3.UnitZ;
        //float xDifference = 90;
        //float yDifference = 90;

        public Matrix cameraView = Matrix.Identity;
        double lastPlay = 0;
        double lastRecord = 0;
        bool recording = false;
        public bool playing = false;
        int currentEntry = 0;

        public float CameraArc
        {
            get { return cameraArc; }
            set { cameraArc = value; }
        }

        private float cameraRotation = 0;

        public float CameraRotation
        {
            get { return cameraRotation; }
            set { cameraRotation = value; }
        }

        private float cameraDistance = 1000;

        public float CameraDistance
        {
            get { return cameraDistance; }
            set { cameraDistance = value; }
        }
        private Matrix view;
        private Matrix projection;

        public Vector3 Position
        {
            get { return cameraPos; }
        }

        public Vector3 Forward
        {
            get { return camForward; }
        }

        public Vector3 Up
        {
            get { return camUp; }
        }

        private float nearPlaneDistance = 1;
        public float NearPlaneDistance
        {
            get { return nearPlaneDistance; }
            set { nearPlaneDistance = value; }
        }

        private float farPlaneDistance = 3000;
        public float FarPlaneDistance
        {
            get { return farPlaneDistance; }
            set { farPlaneDistance = value; }
        }


        public Matrix View
        {
            get
            {

                return view;
            }
        }

        public Matrix Projection
        {
            get
            {


                return projection;
            }
        }

        KeyboardState currentKeyboardState = new KeyboardState();
        KeyboardState previousKeyboardState = new KeyboardState();
        MouseState currentMouseState = new MouseState();
        MouseState previousMouseState = new MouseState();
        GamePadState currentGamePadState = new GamePadState();

        public Camera(Game game)
            : base(game)
        {
            // TODO: Construct any child components here
            hpt = new PAB.HiPerfTimer();
        }

        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public override void Initialize()
        {
            // TODO: Add your initialization code here
            previousMouseState = Mouse.GetState();
            previousKeyboardState = Keyboard.GetState();
            hpt.Start();
            base.Initialize();
        }


        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {

            currentKeyboardState = Keyboard.GetState();
            currentMouseState = Mouse.GetState();
            currentGamePadState = GamePad.GetState(PlayerIndex.One);

            // TODO: Add your update code here
            hpt.Stop();
            double milisec = hpt.Duration * 1000;

            if (playing)
            {
                float lerpammount = (float)(milisec - lastPlay);
                currentEntry = (int)Math.Floor(lerpammount * 0.01);
                lerpammount = lerpammount - (currentEntry * 100);
                if ((currentEntry + 1) < camcorder.entries.Count)
                {
                    Vector2 xy = lerp(camcorder.entries[currentEntry].Item1, camcorder.entries[currentEntry + 1].Item1, lerpammount * 0.01f);
                    xDifference = xy.X;
                    yDifference = xy.Y;

                    cameraPos = Vector3.Lerp(camcorder.entries[currentEntry].Item2, camcorder.entries[currentEntry + 1].Item2, lerpammount * 0.01f);

                }
                else
                {
                    playing = false;
                }
            }

            float time = (float)gameTime.ElapsedGameTime.TotalMilliseconds * 0.1f;

            if (yDifference > 360)
            {
                yDifference -= 360;
            }
            if (yDifference < 0)
            {
                yDifference += 360;
            }
            if (xDifference > 360)
            {
                xDifference -= 360;
            }
            if (xDifference < 0)
            {
                xDifference += 360;
            }
            float xMult = 1;
            if (yDifference > 180f)
            {
                xMult = -1;
            }
            if (yDifference < 0f)
            {
                xMult = -1;
            }

            if (recording && (milisec - lastRecord) >= 100)
            {
                lastRecord = milisec;
                camcorder.entries.Add(new Tuple<Vector2, Vector3>(
                    new Vector2(xDifference, yDifference), cameraPos));
            }




            cameraView = Matrix.CreateFromYawPitchRoll(MathHelper.ToRadians(90 - xDifference), MathHelper.ToRadians(90 - yDifference), 0);
            camForward = cameraView.Forward;
            camCenter = cameraPos + camForward;
            camUp = cameraView.Up;
            Matrix.CreateLookAt(ref cameraPos, ref camCenter, ref camUp, out view);

            if (!playing)
            {
                if (currentMouseState != previousMouseState)
                {
                    if (Game.IsActive)
                    {
                        if (currentMouseState.RightButton == ButtonState.Pressed)
                        {
                            xDifference += xMult * ((currentMouseState.X - previousMouseState.X) * time);
                            yDifference += ((currentMouseState.Y - previousMouseState.Y) * time);
                            Mouse.SetPosition(previousMouseState.X, previousMouseState.Y);
                        }
                    }
                    if (currentMouseState.RightButton == ButtonState.Released)
                    {
                        previousMouseState = currentMouseState;
                    }
                }



                //if (currentKeyboardState.IsKeyDown(Keys.Up))
                //    yDifference -= time;
                //if (currentKeyboardState.IsKeyDown(Keys.Down))
                //    yDifference += time;
                //if (currentKeyboardState.IsKeyDown(Keys.Right))
                //    xDifference += time;
                //if (currentKeyboardState.IsKeyDown(Keys.Left))
                //    xDifference -= time;
                if (!currentKeyboardState.IsKeyDown(Keys.LeftControl))
                {
                    if (currentKeyboardState.IsKeyDown(Keys.W))
                        cameraPos += cameraView.Forward * time;
                    if (currentKeyboardState.IsKeyDown(Keys.S))
                        cameraPos -= cameraView.Forward * time;
                    if (currentKeyboardState.IsKeyDown(Keys.D))
                        cameraPos += cameraView.Right * time;
                    if (currentKeyboardState.IsKeyDown(Keys.A))
                        cameraPos -= cameraView.Right * time;

                    if (currentKeyboardState.IsKeyDown(Keys.E))
                        cameraPos.Y += time;
                    if (currentKeyboardState.IsKeyDown(Keys.Q))
                        cameraPos.Y -= time;

                    if (currentKeyboardState.IsKeyDown(Keys.R))
                    {
                        cameraPos = new Vector3(179.8928f, 202.3825f, -135.3956f);
                        xDifference = -44.78f;
                        yDifference = 104.37f;
                    }
                }
            }

            if (currentKeyboardState.IsKeyDown(Keys.LeftControl) &&
                currentKeyboardState.IsKeyDown(Keys.S) &&
                previousKeyboardState.IsKeyUp(Keys.S))
            {
                recording = !recording;
                if (!recording)
                {
                    camcorder.WritePos();
                }
                else
                {
                    lastRecord = milisec;
                    camcorder.entries.Clear();
                }
            }

            if (currentKeyboardState.IsKeyDown(Keys.LeftControl) &&
                currentKeyboardState.IsKeyDown(Keys.L) &&
                previousKeyboardState.IsKeyUp(Keys.L))
            {
                if (!playing)
                {
                    camcorder.entries.Clear();
                    camcorder.Read();
                    if (camcorder.entries.Count > 0)
                    {
                        xDifference = camcorder.entries[0].Item1.X;
                        yDifference = camcorder.entries[0].Item1.Y;
                        cameraPos = camcorder.entries[0].Item2;
                    }
                    playing = !playing;
                    currentEntry = 0;
                    lastPlay = milisec;
                    DeferredLighting.DeferredRenderer.drinstance.frameCount = 0;
                    DeferredLighting.DeferredRenderer.drinstance.framesTime = 0;
                }
                else
                {
                    playing = !playing;
                }
            }
            if (currentKeyboardState.IsKeyDown(Keys.LeftControl) &&
                currentKeyboardState.IsKeyDown(Keys.Z) &&
                previousKeyboardState.IsKeyUp(Keys.Z))
            {
                camcorder.WriteLights();
            }
            float aspectRatio = (float)Game.Window.ClientBounds.Width /
                                (float)Game.Window.ClientBounds.Height;
            projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4,
                                                                    aspectRatio,
                                                                    nearPlaneDistance,
                                                                    farPlaneDistance);
            previousKeyboardState = currentKeyboardState;
            base.Update(gameTime);
        }
        static Vector2 lerp(Vector2 from, Vector2 to, float ammount)
        {
            float x, y;
            ammount = MathHelper.Clamp(ammount, 0, 1);
            if (Math.Abs(from.X - to.X) > 180)
            {
                if (from.X > to.X)
                {
                    float dif = 360 - from.X + to.X;
                    x = from.X + dif * ammount;
                }
                else
                {
                    float dif = 360 - to.X + from.X;
                    x = from.X - dif * ammount;
                }
            }
            else
            {
                x = MathHelper.Lerp(from.X, to.X, ammount);
            }
            if (Math.Abs(from.Y - to.Y) > 180)
            {
                if (from.Y > to.Y)
                {
                    float dif = 360 - from.Y + to.Y;
                    y = from.Y + dif * ammount;
                }
                else
                {
                    float dif = 360 - to.Y + from.Y;
                    y = from.Y - dif * ammount;
                }
            }
            else
            {
                y = MathHelper.Lerp(from.Y, to.Y, ammount);
            }
            return new Vector2(x, y);
        }
    }
}


