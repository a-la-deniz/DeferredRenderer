using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;


namespace DeferredLighting
{
    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    /// 

    public class Light
    {
        public Vector3 lightPosition;
        public Color lightColor;
        public float lightRadius;
        public float lightIntensity;
    }

    public class SpotLight : Light
    {
        public Vector3 lightDirection;
        public float decay;
        public float spotAngle;
    }


    public class DeferredRenderer : DrawableGameComponent
    {
        public static DeferredRenderer drinstance;
        PAB.HiPerfTimer hpt;
        double timeSinceLastUpdate = 0;
        const int layerNum = 2;
        private Camera camera;
        private QuadRenderComponent quadRenderer;
        private Scene scene;

        private RenderTarget2D colorRT; //color and specular intensity
        private RenderTarget2D normalRT; //normals + specular power
        private RenderTarget2D depthRT; //depth
        private RenderTarget2D transRT; //refractive info / alpha
        private RenderTarget2D lightRT; //lighting
        private RenderTarget2D shadowRTColor; //shadow map
        private RenderTarget2D shadowRTDepth; //shadow map
        private RenderTarget2D previewRT;
        private RenderTarget2D graphRT;

        private RenderTarget2D lastRT;

        private Effect clearBufferEffect;
        private Effect directionalLightEffect;
        
        private Effect pointLightEffect;
        private Model sphereModel; //point ligt volume

        private Effect spotLightEffect;
        private Model spotModel;

        private Effect finalCombineEffect;

        public List<SpotLight> spotLights;

        private SpriteBatch spriteBatch;

        private Vector2 halfPixel;
        private Vector2 shadowHalfPixel;

        private float vScale = -0.02f;

        public double framesTime = 0;

        public double frameCount = 0;

        private int currentModel = 4;

        private int currentRes = 3;

        private bool showStuff = false;

        //int n = 4;

        private KeyboardState previousState;


        //graph vars

        Matrix worldMatrix;
        Matrix viewMatrix;
        Matrix projectionMatrix;
        BasicEffect basicEffect;

        VertexPositionColor[] pointList;
        short[] lineListIndices;
        private bool doShadows = true;
        private int shadowRes = 1024;

        //graph vars end

        Color[] colors = new Color[10] { 
            Color.Red, Color.Blue, 
            Color.IndianRed, Color.CornflowerBlue, 
            Color.Gold, Color.Green,
            Color.Crimson, Color.SkyBlue,
            Color.BlanchedAlmond, Color.ForestGreen};


        public DeferredRenderer(Game game)
            : base(game)
        {
            scene = new Scene(Game);
            drinstance = this;
            hpt = new PAB.HiPerfTimer();
        }

        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public override void Initialize()
        {
            // TODO: Add your initialization code here
            
            base.Initialize();
            hpt.Start();
            hpt.Stop();
            camera = new Camera(Game);
            camera.CameraArc = -30;
            camera.CameraDistance = 50;
            quadRenderer = new QuadRenderComponent(Game);
            Game.Components.Add(camera);
            Game.Components.Add(quadRenderer);
            previousState = Keyboard.GetState();

            spotLights = new List<SpotLight>();

            { // graph code
                int n = 300;
                //GeneratePoints generates a random graph, implementation irrelevant
                pointList = new VertexPositionColor[n];
                int height = 300;
                int minY = 0;
                for (int i = 0; i < n; i++)
                    pointList[i] = new VertexPositionColor() { Position = new Vector3(i * 10, 0, 0), Color = Color.Red };

                //links the points into a list
                lineListIndices = new short[(n * 2) - 2];
                for (int i = 0; i < n - 1; i++)
                {
                    lineListIndices[i * 2] = (short)(i);
                    lineListIndices[(i * 2) + 1] = (short)(i + 1);
                }

                worldMatrix = Matrix.Identity;
                viewMatrix = Matrix.CreateLookAt(new Vector3(0.0f, 0.0f, 1.0f), Vector3.Zero, Vector3.Up);
                projectionMatrix = Matrix.CreateOrthographicOffCenter(0, (float)GraphicsDevice.Viewport.Width, 0, (float)GraphicsDevice.Viewport.Height, 1.0f, 1000.0f);

                basicEffect = new BasicEffect(GraphicsDevice);
                basicEffect.World = worldMatrix;
                basicEffect.View = viewMatrix;
                basicEffect.Projection = projectionMatrix;

                basicEffect.VertexColorEnabled = true; //important for color
            }

            SpotLight spot = new SpotLight()
            {
                lightPosition = new Vector3(371, 290, -120),
                lightColor = Color.White,
                lightRadius = 500,
                lightIntensity = 2,
                lightDirection = new Vector3(-371, -290, 120),
                spotAngle = 15,
                decay = 1
            };
            spotLights.Add(spot);
            spot = new SpotLight()
            {
                lightPosition = new Vector3(-120, 290, 371),
                lightColor = Color.White,
                lightRadius = 500,
                lightIntensity = 2,
                lightDirection = new Vector3(120, -260, -371),
                spotAngle = 15,
                decay = 10
            };
            spotLights.Add(spot);
        }

        protected override void LoadContent()
        {


            int backBufferWidth = GraphicsDevice.PresentationParameters.BackBufferWidth * currentRes / 3;
            int backBufferHeight = GraphicsDevice.PresentationParameters.BackBufferHeight * currentRes / 3;

            halfPixel = -new Vector2()
            {
                X = 0.5f / (float)backBufferWidth,
                Y = 0.5f / (float)backBufferHeight
            };

            colorRT = new RenderTarget2D(GraphicsDevice, backBufferWidth,
                                                                     backBufferHeight, false, SurfaceFormat.Color, DepthFormat.Depth24);
            normalRT = new RenderTarget2D(GraphicsDevice, backBufferWidth,
                                                                    backBufferHeight, false, SurfaceFormat.Rgba1010102, DepthFormat.None);
            transRT = new RenderTarget2D(GraphicsDevice, backBufferWidth,
                                                                    backBufferHeight, false, SurfaceFormat.Color, DepthFormat.None);
            depthRT = new RenderTarget2D(GraphicsDevice, backBufferWidth,
                                                                    backBufferHeight, false, SurfaceFormat.Single, DepthFormat.None);
            lightRT = new RenderTarget2D(GraphicsDevice, backBufferWidth,
                                                                    backBufferHeight, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            shadowRTColor = new RenderTarget2D(GraphicsDevice, shadowRes, shadowRes, false, SurfaceFormat.Color, DepthFormat.Depth24);
            shadowRTDepth = new RenderTarget2D(GraphicsDevice, shadowRes, shadowRes, false, SurfaceFormat.Single, DepthFormat.None);
            previewRT = new RenderTarget2D(GraphicsDevice, backBufferWidth,
                                                                    backBufferHeight, false, SurfaceFormat.Color, DepthFormat.None);
            graphRT = new RenderTarget2D(GraphicsDevice, backBufferWidth / 3,
                                                                    backBufferHeight / 3, false, SurfaceFormat.Color, DepthFormat.None);

            lastRT = previewRT;
            scene.InitializeScene();

            shadowHalfPixel = -new Vector2()
            {
                X = 0.5f / (float)shadowRTColor.Width,
                Y = 0.5f / (float)shadowRTColor.Height
            };

            clearBufferEffect = Game.Content.Load<Effect>("ClearGBuffer");
            directionalLightEffect = Game.Content.Load<Effect>("DirectionalLight");
            finalCombineEffect = Game.Content.Load<Effect>("CombineTFinal");
            pointLightEffect = Game.Content.Load<Effect>("PointTLight");
            sphereModel = Game.Content.Load<Model>(@"Models\sphere");
            spotLightEffect = Game.Content.Load<Effect>("SpotTLight");
            spotModel = Game.Content.Load<Model>(@"Models\cone");
           
            spriteBatch = new SpriteBatch(GraphicsDevice);
            base.LoadContent();
        }

        private void SetGBuffer()
        {
            GraphicsDevice.SetRenderTargets(colorRT, normalRT, depthRT, transRT);
        }

        private void ResolveGBuffer()
        {
            GraphicsDevice.SetRenderTargets(null);            
        }

        private void ClearGBuffer()
        {
            clearBufferEffect.CurrentTechnique = clearBufferEffect.Techniques[0];
            clearBufferEffect.Techniques[0].Passes[0].Apply();
            quadRenderer.Render(Vector2.One * -1, Vector2.One);            
        }

        private void DrawDirectionalLight(Vector3 lightDirection, Color color)
        {
            directionalLightEffect.Parameters["colorMap"].SetValue(colorRT);
            directionalLightEffect.Parameters["normalMap"].SetValue(normalRT);
            directionalLightEffect.Parameters["depthMap"].SetValue(depthRT);

            directionalLightEffect.Parameters["lightDirection"].SetValue(lightDirection);
            directionalLightEffect.Parameters["Color"].SetValue(color.ToVector3());

            directionalLightEffect.Parameters["cameraPosition"].SetValue(camera.Position);
            directionalLightEffect.Parameters["InvertViewProjection"].SetValue(Matrix.Invert(camera.View * camera.Projection));

            directionalLightEffect.Parameters["halfPixel"].SetValue(halfPixel);

            directionalLightEffect.Techniques[0].Passes[0].Apply();
            quadRenderer.Render(Vector2.One * -1, Vector2.One);            
        }
      
        private void DrawPointLight(Vector3 lightPosition, Color color, float lightRadius, float lightIntensity)
        {            
            //set the G-Buffer parameters
            pointLightEffect.Parameters["colorMap"].SetValue(colorRT);
            pointLightEffect.Parameters["normalMap"].SetValue(normalRT);
            pointLightEffect.Parameters["depthMap"].SetValue(depthRT);
            pointLightEffect.Parameters["transMap"].SetValue(transRT);

            //compute the light world matrix
            //scale according to light radius, and translate it to light position
            Matrix sphereWorldMatrix = Matrix.CreateScale(lightRadius) * Matrix.CreateTranslation(lightPosition);
            pointLightEffect.Parameters["World"].SetValue(sphereWorldMatrix);
            pointLightEffect.Parameters["View"].SetValue(camera.View);
            pointLightEffect.Parameters["Projection"].SetValue(camera.Projection);
            //light position
            pointLightEffect.Parameters["lightPosition"].SetValue(lightPosition);

            //set the color, radius and Intensity
            pointLightEffect.Parameters["Color"].SetValue(color.ToVector3());
            pointLightEffect.Parameters["lightRadius"].SetValue(lightRadius);
            pointLightEffect.Parameters["lightIntensity"].SetValue(lightIntensity);

            //parameters for specular computations
            pointLightEffect.Parameters["cameraPosition"].SetValue(camera.Position);
            pointLightEffect.Parameters["InvertViewProjection"].SetValue(Matrix.Invert(camera.View * camera.Projection));
            //size of a halfpixel, for texture coordinates alignment
            pointLightEffect.Parameters["halfPixel"].SetValue(halfPixel);
            //calculate the distance between the camera and light center
            float cameraToCenter = Vector3.Distance(camera.Position, lightPosition);
            //if we are inside the light volume, draw the sphere's inside face
            if (cameraToCenter < lightRadius)
                GraphicsDevice.RasterizerState = RasterizerState.CullClockwise;                
            else
                GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;

//            GraphicsDevice.DepthStencilState = DepthStencilState.None;

            pointLightEffect.Techniques[0].Passes[0].Apply();
            foreach (ModelMesh mesh in sphereModel.Meshes)
            {
                foreach (ModelMeshPart meshPart in mesh.MeshParts)
                {
                    GraphicsDevice.Indices = meshPart.IndexBuffer;
                    GraphicsDevice.SetVertexBuffer(meshPart.VertexBuffer);
                    
                    GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, meshPart.NumVertices, meshPart.StartIndex, meshPart.PrimitiveCount);
                }
            }            
            
            GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
//            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        }

        private void DrawSpotLight(SpotLight sl, bool renderShadows = false)
        {



            sl.lightDirection.Normalize();

            //lightPosition = camera.Position + (Vector3.Right + Vector3.Down) * 10;
            //spotDirection = camera.Forward;



            //set the G-Buffer parameters
            spotLightEffect.Parameters["colorMap"].SetValue(colorRT);
            spotLightEffect.Parameters["normalMap"].SetValue(normalRT);
            spotLightEffect.Parameters["depthMap"].SetValue(depthRT);
            spotLightEffect.Parameters["transMap"].SetValue(transRT);



            //compute the light world matrix
            //scale according to light radius, and translate it to light position

            Matrix spotMatrix = Matrix.CreateFromQuaternion(QuaternionLookRotation(sl.lightDirection));
            spotMatrix.Translation = sl.lightPosition;
            float spotAngleRad = MathHelper.ToRadians(sl.spotAngle);
            float tan = (float)Math.Tan(spotAngleRad);
            Matrix sphereWorldMatrix = Matrix.CreateScale(sl.lightRadius * tan, sl.lightRadius * tan, sl.lightRadius) * spotMatrix;
            float cosSpotAngle = (float)Math.Cos(spotAngleRad);

            int currentTechnique = 0;

            if (renderShadows)
            {
                GraphicsDevice.SetRenderTarget(null);
                GraphicsDevice.SetRenderTargets(shadowRTColor, shadowRTDepth);
                GraphicsDevice.BlendState = BlendState.Opaque;
                clearBufferEffect.CurrentTechnique = clearBufferEffect.Techniques[1];
                clearBufferEffect.Techniques[1].Passes[0].Apply();
                quadRenderer.Render(Vector2.One * -1, Vector2.One);   

                Matrix spotProj = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(sl.spotAngle * 2), 1, 0.01f * sl.lightRadius, sl.lightRadius);
                Matrix spotView = Matrix.CreateLookAt(sl.lightPosition, sl.lightPosition + sl.lightDirection, Vector3.Up);

                //scene.DrawSceneShadow(inverseTransform, spotProj, currentModel);
                scene.DrawSceneShadow(spotView, spotProj, currentModel, 1);
                GraphicsDevice.SetRenderTarget(null);
                GraphicsDevice.SetRenderTarget(lightRT);
                GraphicsDevice.BlendState = BlendState.AlphaBlend;
                GraphicsDevice.DepthStencilState = DepthStencilState.None;
                spotLightEffect.Parameters["ShadowInvertViewProjection"].SetValue((spotView * spotProj));
                //spotLightEffect.Parameters["ShadowView"].SetValue((spotView));
                //spotLightEffect.Parameters["ShadowProjection"].SetValue((spotProj));
                spotLightEffect.Parameters["shadowMapColor"].SetValue(shadowRTColor);
                spotLightEffect.Parameters["shadowMapDepth"].SetValue(shadowRTDepth);
                spotLightEffect.Parameters["shadowHalfPixel"].SetValue(shadowHalfPixel);
                currentTechnique = 1;


            }





            
            spotLightEffect.Parameters["World"].SetValue(sphereWorldMatrix);
            spotLightEffect.Parameters["View"].SetValue(camera.View);
            spotLightEffect.Parameters["Projection"].SetValue(camera.Projection);
            //light position
            spotLightEffect.Parameters["lightPosition"].SetValue(sl.lightPosition);

            //set the color, radius and Intensity
            spotLightEffect.Parameters["Color"].SetValue(sl.lightColor.ToVector3());
            spotLightEffect.Parameters["lightRadius"].SetValue(sl.lightRadius);
            spotLightEffect.Parameters["lightIntensity"].SetValue(sl.lightIntensity);
            spotLightEffect.Parameters["spotDirection"].SetValue(sl.lightDirection);
            spotLightEffect.Parameters["spotLightAngleCosine"].SetValue(cosSpotAngle);
            spotLightEffect.Parameters["spotDecayExponent"].SetValue(10f);

            //parameters for specular computations
            spotLightEffect.Parameters["cameraPosition"].SetValue(camera.Position);
            spotLightEffect.Parameters["InvertViewProjection"].SetValue(Matrix.Invert(camera.View * camera.Projection));
            //size of a halfpixel, for texture coordinates alignment
            spotLightEffect.Parameters["halfPixel"].SetValue(halfPixel);
            //calculate the distance between the camera and light center
            float cameraToCenter = Vector3.Distance(camera.Position, sl.lightPosition);
            //if we are inside the light volume, draw the sphere's inside face
            if (cameraToCenter < sl.lightRadius)
                GraphicsDevice.RasterizerState = RasterizerState.CullClockwise;
            else
                GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;

            //            GraphicsDevice.DepthStencilState = DepthStencilState.None;
            spotLightEffect.CurrentTechnique = spotLightEffect.Techniques[currentTechnique];
            spotLightEffect.Techniques[currentTechnique].Passes[0].Apply();
            foreach (ModelMesh mesh in spotModel.Meshes)
            {
                foreach (ModelMeshPart meshPart in mesh.MeshParts)
                {
                    GraphicsDevice.Indices = meshPart.IndexBuffer;
                    GraphicsDevice.SetVertexBuffer(meshPart.VertexBuffer);

                    GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, meshPart.NumVertices, meshPart.StartIndex, meshPart.PrimitiveCount);
                }
            }

            GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            //            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        }
        
        
        public override void Draw(GameTime gameTime)
        {            
            SetGBuffer();            
            ClearGBuffer();
            //GraphicsDevice.Clear(ClearOptions.DepthBuffer, Color.Black, 1, 0);
            scene.DrawScene(camera, gameTime, currentModel);
            ResolveGBuffer();
            DrawLights(gameTime);

            int halfWidth = GraphicsDevice.Viewport.Width / 3;
            int halfHeight = GraphicsDevice.Viewport.Height / 3;

            

            // TODO: Add your drawing code here
            if (showStuff)
            {
                GraphicsDevice.SetRenderTarget(graphRT);
                GraphicsDevice.Clear(Color.Black);
                foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    GraphicsDevice.DrawUserIndexedPrimitives<VertexPositionColor>(
                        PrimitiveType.LineList,
                        pointList,
                        0,
                        pointList.Length,
                        lineListIndices,
                        0,
                        pointList.Length - 1
                    );
                }
                GraphicsDevice.SetRenderTargets(null);
            }

            GraphicsDevice.Clear(Color.Black);
            
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullCounterClockwise);
            if (showStuff)
            {
                spriteBatch.Draw(colorRT, new Rectangle(0, 0, halfWidth, halfHeight), Color.White);
                spriteBatch.Draw(lightRT, new Rectangle(0, halfHeight, halfWidth, halfHeight), Color.White);
                spriteBatch.Draw(depthRT, new Rectangle(halfWidth, 0, halfWidth, halfHeight), Color.White);
                spriteBatch.Draw(normalRT, new Rectangle(2 * halfWidth, 0, halfWidth, halfHeight), Color.White);
                spriteBatch.Draw(lastRT, new Rectangle(halfWidth, halfHeight, 2 * halfWidth, 2 * halfHeight), Color.White);
                //spriteBatch.Draw(shadowRTDepth, new Rectangle(0, 2 * halfHeight, halfWidth, halfHeight), Color.White);
                spriteBatch.Draw(shadowRTColor, new Rectangle(0, 2 * Math.Min(halfHeight, halfWidth), Math.Min(halfHeight, halfWidth), Math.Min(halfHeight, halfWidth)), Color.White);
                //spriteBatch.Draw(graphRT, new Rectangle(0, 2 * halfHeight, halfWidth, halfHeight), Color.White);
            }
            else
            {
                spriteBatch.Draw(lastRT, new Rectangle(0, 0, lastRT.Width, lastRT.Height), Color.White);
            }
            //spriteBatch.Draw(colorRT, new Rectangle(0, 0, 3 * halfWidth, 3 * halfHeight), Color.White);
            //spriteBatch.Draw(previewRT, new Rectangle(0, 0, 3 * halfWidth, 3 * halfHeight), Color.White);
            spriteBatch.End();


            
            base.Draw(gameTime);
        }

        private void DrawLights(GameTime gameTime)
        {
            GraphicsDevice.SetRenderTarget(lightRT);
            GraphicsDevice.Clear(Color.Transparent);
            GraphicsDevice.BlendState = BlendState.AlphaBlend;
            GraphicsDevice.DepthStencilState = DepthStencilState.None;

            //float angle = (float)gameTime.TotalGameTime.TotalSeconds;
            float angle = 0;

            //DrawPointLight(new Vector3(-120, 260, 371), Color.White, 500, 1);
            //DrawPointLight(new Vector3(36, 310, 24), Color.White, 500, 0.5f);

            //DrawDirectionalLight(new Vector3(0, -1, 0), Color.White);



            for (int i = 0; i < spotLights.Count; i++ )
            {
                DrawSpotLight(spotLights[i], doShadows);
            }

                //for (int i = 0; i < n; i++)
                //{
                //Vector3 pos = new Vector3((float)Math.Sin(i * MathHelper.TwoPi / n + angle), 3f, (float)Math.Cos(i * MathHelper.TwoPi / n + angle));
                //DrawPointLight(pos * 40, colors[i % 10], 500, 1);
                //pos = new Vector3((float)Math.Cos((i + 5) * MathHelper.TwoPi / n - angle), 3f, 2 + (float)Math.Sin((i + 5) * MathHelper.TwoPi / n - angle));
                //DrawPointLight(pos * 40, colors[i % 10], 500, 1);
                //pos = new Vector3((float)Math.Cos((i + 10) * MathHelper.TwoPi / n + angle), 3f, 3 + (float)Math.Sin((i + 10) * MathHelper.TwoPi / n + angle));
                //DrawPointLight(pos * 40, colors[i % 10], 500, 1);
                //pos = new Vector3((float)Math.Cos((i - 10) * MathHelper.TwoPi / n + angle), 3f, 4 + (float)Math.Sin((i - 10) * MathHelper.TwoPi / n + angle));
                //DrawPointLight(pos * 40, colors[i % 10], 500, 1);
                //Vector3 pos = new Vector3((float)Math.Sin(i * MathHelper.TwoPi / n + angle), 70f, (float)Math.Cos(i * MathHelper.TwoPi / n + angle));
                //DrawSpotLight(pos * 40, colors[i % 10], 500, 2, -Vector3.Down, 80, 1, true);
                //pos = new Vector3((float)Math.Cos((i + 5) * MathHelper.TwoPi / n - angle), 70f, 2 + (float)Math.Sin((i + 5) * MathHelper.TwoPi / n - angle));
                //DrawSpotLight(pos * 40, colors[i % 10], 500, 2, -Vector3.Down, 80, 1, true);
                //pos = new Vector3((float)Math.Cos((i + 10) * MathHelper.TwoPi / n + angle), 70f, 3 + (float)Math.Sin((i + 10) * MathHelper.TwoPi / n + angle));
                //DrawSpotLight(pos * 40, colors[i % 10], 500, 2, -Vector3.Down, 80, 1, true);
                //pos = new Vector3((float)Math.Cos((i - 10) * MathHelper.TwoPi / n + angle), 70f, 4 + (float)Math.Sin((i - 10) * MathHelper.TwoPi / n + angle));
                //DrawSpotLight(pos * 40, colors[i % 10], 500, 2, -Vector3.Down, 80, 1, true);

                //}

                //DrawPointLight(new Vector3(0, (float)Math.Sin(angle * 0.8) * 40, 0), Color.Red, 30, 5);
                //DrawPointLight(new Vector3(0, 25, 0), Color.White, 30, 1);
                //DrawPointLight(new Vector3(0, 0, 70), Color.Wheat, 55 + 10 * (float)Math.Sin(5 * angle), 3);     

            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.SetRenderTargets(null);
            GraphicsDevice.Clear(Color.Transparent);

            //Combine everything

            Matrix rot = camera.cameraView;

            finalCombineEffect.Parameters["Rotation"].SetValue(Matrix.Invert(rot));


            finalCombineEffect.Parameters["colorMap"].SetValue(colorRT);
            finalCombineEffect.Parameters["lightMap"].SetValue(lightRT);
            finalCombineEffect.Parameters["transMap"].SetValue(transRT);
            finalCombineEffect.Parameters["normalMap"].SetValue(normalRT);
            finalCombineEffect.Parameters["depthMap"].SetValue(depthRT);
            finalCombineEffect.Parameters["halfPixel"].SetValue(halfPixel);
            finalCombineEffect.Parameters["vScale"].SetValue(vScale);

            GraphicsDevice.SetRenderTarget(previewRT);
            GraphicsDevice.Clear(Color.Black);

            finalCombineEffect.Techniques[0].Passes[0].Apply();
            quadRenderer.Render(Vector2.One * -1, Vector2.One);

            GraphicsDevice.SetRenderTargets(null);

            //GraphicsDevice.SetRenderTarget(colorRT);

            ////////Matrix sphereWorldMatrix = Matrix.CreateScale(5) * Matrix.CreateTranslation(new Vector3(-120, 260, 371));



            //Matrix spotMatrix = Matrix.CreateFromYawPitchRoll(0, (float)(Math.PI / 2), 0);


            //spotMatrix = Matrix.CreateFromQuaternion(QuaternionLookRotation(new Vector3(0, 0, 0) - new Vector3(-120, 260, 371)));
            //spotMatrix.Translation = new Vector3(-120, 260, 371);



            //float tan = (float)Math.Tan(MathHelper.ToRadians(15));
            //Matrix sphereWorldMatrix = Matrix.CreateScale(500 * tan, 500 * tan, 500) * spotMatrix;

            //foreach (ModelMesh mesh in spotModel.Meshes)
            //{
            //    foreach (BasicEffect effect in mesh.Effects)
            //    {

            //        effect.World = sphereWorldMatrix;
            //        effect.View = camera.View;
            //        effect.Projection = camera.Projection;

            //        effect.EnableDefaultLighting();

            //    }
            //    mesh.Draw();

            //}

            //GraphicsDevice.SetRenderTargets(null);
            

            double elapsedTime = timeSinceLastUpdate * 1000;
            if (camera.playing)
            {
                framesTime += elapsedTime;
                frameCount++;
            }

            //Output FPS and 'credits'
            double fps = (1000 / elapsedTime);
            fps = Math.Round(fps, 0);
            pointList[0].Position.Y = (float)fps;
            Game.Window.Title = Math.Round(frameCount * 1000 / framesTime, 0).ToString() + " FPS while drawing " + scene.count + " polygons with " + (spotLights.Count) + " lights" + " with " + shadowRes + " shadow maps. Current FPS: " + fps.ToString() + " FPS";
        }


        private static Quaternion QuaternionLookRotation(Vector3 forward)
        {
            Vector3 up = Vector3.UnitY;
            forward.Normalize();
            if (forward == up || forward == -up)
                up = Vector3.UnitZ;

            Vector3 vector = Vector3.Normalize(forward);
            Vector3 vector2 = Vector3.Normalize(Vector3.Cross(up, vector));
            Vector3 vector3 = Vector3.Cross(vector, vector2);
            float m00 = vector2.X;
            float m01 = vector2.Y;
            float m02 = vector2.Z;
            float m10 = vector3.X;
            float m11 = vector3.Y;
            float m12 = vector3.Z;
            float m20 = vector.X;
            float m21 = vector.Y;
            float m22 = vector.Z;


            float num8 = (m00 + m11) + m22;
            Quaternion quaternion = new Quaternion();
            if (num8 > 0f)
            {
                float num = (float)Math.Sqrt(num8 + 1f);
                quaternion.W = num * 0.5f;
                num = 0.5f / num;
                quaternion.X = (m12 - m21) * num;
                quaternion.Y = (m20 - m02) * num;
                quaternion.Z = (m01 - m10) * num;
                return quaternion;
            }
            if ((m00 >= m11) && (m00 >= m22))
            {
                float num7 = (float)Math.Sqrt(((1f + m00) - m11) - m22);
                float num4 = 0.5f / num7;
                quaternion.X = 0.5f * num7;
                quaternion.Y = (m01 + m10) * num4;
                quaternion.Z = (m02 + m20) * num4;
                quaternion.W = (m12 - m21) * num4;
                return quaternion;
            }
            if (m11 > m22)
            {
                float num6 = (float)Math.Sqrt(((1f + m11) - m00) - m22);
                float num3 = 0.5f / num6;
                quaternion.X = (m10 + m01) * num3;
                quaternion.Y = 0.5f * num6;
                quaternion.Z = (m21 + m12) * num3;
                quaternion.W = (m20 - m02) * num3;
                return quaternion;
            }
            float num5 = (float)Math.Sqrt(((1f + m22) - m00) - m11);
            float num2 = 0.5f / num5;
            quaternion.X = (m20 + m02) * num2;
            quaternion.Y = (m21 + m12) * num2;
            quaternion.Z = 0.5f * num5;
            quaternion.W = (m01 - m10) * num2;
            return quaternion;
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            if (showStuff)
            {
                for (int i = pointList.GetLength(0) - 1; i > 0; i--)
                {
                    pointList[i].Position.Y = pointList[i - 1].Position.Y;
                }
            }
            KeyboardState hit = new KeyboardState();
            hit = Keyboard.GetState();
            int previousModel = currentModel;
            int previousRes = currentRes;
            if (hit.IsKeyDown(Keys.Multiply) && previousState.IsKeyUp(Keys.Multiply) && currentModel < 6)
                currentModel++;
            if (hit.IsKeyDown(Keys.Divide) && previousState.IsKeyUp(Keys.Divide) && currentModel > 4)
                currentModel--;
            //if (hit.IsKeyDown(Keys.Add) && previousState.IsKeyUp(Keys.Add))
            //    n++;
            //if (hit.IsKeyDown(Keys.Subtract) && previousState.IsKeyUp(Keys.Subtract) && n > 0)
            //    n--;

            if (hit.IsKeyDown(Keys.D1) && previousState.IsKeyUp(Keys.D1))
            {
                currentRes = 1;
                LoadContent();
            }
            if (hit.IsKeyDown(Keys.D2) && previousState.IsKeyUp(Keys.D2))
            {
                currentRes = 2;
                LoadContent();
            }
            if (hit.IsKeyDown(Keys.D3) && previousState.IsKeyUp(Keys.D3))
            {
                currentRes = 3;
                LoadContent();
            }
            if (hit.IsKeyDown(Keys.T) && previousState.IsKeyUp(Keys.T))
            {
                showStuff = !showStuff;
            }
            if (hit.IsKeyDown(Keys.Y) && previousState.IsKeyUp(Keys.Y))
            {
                lastRT = previewRT;
            }
            if (hit.IsKeyDown(Keys.U) && previousState.IsKeyUp(Keys.U))
            {
                lastRT = colorRT;
            }
            if (hit.IsKeyDown(Keys.I) && previousState.IsKeyUp(Keys.I))
            {
                lastRT = normalRT;
            }
            if (hit.IsKeyDown(Keys.O) && previousState.IsKeyUp(Keys.O))
            {
                lastRT = lightRT;
            }
            if (hit.IsKeyDown(Keys.P) && previousState.IsKeyUp(Keys.P))
            {
                lastRT = shadowRTColor;
            }
            //if (hit.IsKeyDown(Keys.L) && previousState.IsKeyUp(Keys.L))
            //{
            //    lastRT = shadowRTDepth;
            //}
            if (hit.IsKeyDown(Keys.M) && previousState.IsKeyUp(Keys.M))
            {
                lastRT = depthRT;
            }
            if (hit.IsKeyDown(Keys.LeftAlt) && hit.IsKeyDown(Keys.N) && previousState.IsKeyUp(Keys.N))
            {
                if (spotLights.Count > 0)
                {
                    spotLights.RemoveAt(spotLights.Count - 1);
                }
            }
            else if (hit.IsKeyDown(Keys.N) && previousState.IsKeyUp(Keys.N))
            {
                SpotLight spot = new SpotLight()
                {
                    lightPosition = camera.Position,
                    lightColor = colors[spotLights.Count % 10],
                    lightRadius = 500,
                    lightIntensity = 2,
                    lightDirection = camera.Forward,
                    spotAngle = 15,
                    decay = 10
                };
                spotLights.Add(spot);
            }
            if (hit.IsKeyDown(Keys.B) && previousState.IsKeyUp(Keys.B))
            {
                spotLights.Clear();
            }
            if (hit.IsKeyDown(Keys.V) && previousState.IsKeyUp(Keys.V))
            {
                doShadows = !doShadows;
            }
            if (hit.IsKeyDown(Keys.RightShift) && previousState.IsKeyUp(Keys.RightShift))
            {
                if (shadowRes * 2 <= 4096)
                {
                    shadowRes *= 2;
                    LoadContent();
                }
            }
            if (hit.IsKeyDown(Keys.RightControl) && previousState.IsKeyUp(Keys.RightControl))
            {
                if (shadowRes / 2 >= 128)
                {
                    shadowRes /= 2;
                    LoadContent();
                }
            }
            if (hit.IsKeyDown(Keys.K) || previousRes != currentRes || previousModel != currentModel)
            {
                framesTime = 0;
                frameCount = 0;
            }

            if (hit.IsKeyDown(Keys.H) && previousState.IsKeyUp(Keys.H))
                vScale -= 0.01f;
            if (hit.IsKeyDown(Keys.J) && previousState.IsKeyUp(Keys.J))
                vScale += 0.01f;
            if (hit.IsKeyDown(Keys.G) && previousState.IsKeyUp(Keys.G))
                vScale = 0f;
            previousState = hit;
            base.Update(gameTime);
            double prevDur = hpt.Duration;
            hpt.Stop();
            double nowDur = hpt.Duration;
            timeSinceLastUpdate = nowDur - prevDur;
        }
    }
}
