using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DeferredLighting
{
    public class Scene
    {
        private Game game;
        Model[] models;
        public int count;
        public Scene(Game game)
        {
            this.game = game;
        }
        public void InitializeScene()
        {
            models = new Model[7];
            //models[0] = game.Content.Load<Model>("Models\\finalroomhecta");
            //models[1] = game.Content.Load<Model>("Models\\finalroomquarter");
            //models[2] = game.Content.Load<Model>("Models\\finalroomhalf");
            //models[3] = game.Content.Load<Model>("Models\\lizard");
            models[0] = game.Content.Load<Model>("Models\\lizard");
            models[1] = game.Content.Load<Model>("Models\\finalroom1");
            models[2] = game.Content.Load<Model>("Models\\finalroom");
            models[3] = game.Content.Load<Model>("Models\\finalroom3");
            models[4] = game.Content.Load<Model>("Models\\finalroom4");
            models[5] = game.Content.Load<Model>("Models\\finalroom5");
            models[6] = game.Content.Load<Model>("Models\\finalroom6");
            //models[4] = game.Content.Load<Model>("Models\\finalroomdouble");
            //models[5] = game.Content.Load<Model>("Models\\finalroomquadruple");
        }
        public void DrawScene(Camera camera, GameTime gameTime, int modelId = 3, int techniqueId = 0)
        {
            count = 0;
            game.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            game.GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            game.GraphicsDevice.BlendState = BlendState.Opaque;

            //DrawModel(models[0], Matrix.CreateTranslation(-30, 0, -20), camera);
            //DrawModel(models[0], Matrix.CreateTranslation(0, 100, 0), camera);
            //DrawModel(models[1], Matrix.CreateTranslation(30, 0, -20), camera);
            //DrawModel(models[2], Matrix.CreateScale(0.05f) * Matrix.CreateTranslation(0, 0, 27), camera);
            DrawModel(models[modelId], Matrix.CreateTranslation(0, 0, 0), camera, techniqueId);
            //DrawModel(models[3], Matrix.CreateRotationX(-(float)Math.PI * 0.5f) * Matrix.CreateTranslation(0, 0, 100), camera);
        }
        private void DrawModel(Model model, Matrix world, Camera camera, int techniqueId)
        {
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (ModelMeshPart meshPart in mesh.MeshParts)
                {
                    count += meshPart.PrimitiveCount;
                }
                foreach (Effect effect in mesh.Effects)
                {
                    effect.Parameters["World"].SetValue(world);
                    effect.Parameters["View"].SetValue(camera.View);
                    effect.Parameters["Projection"].SetValue(camera.Projection);
                    effect.CurrentTechnique = effect.Techniques[techniqueId];
                    effect.Techniques[techniqueId].Passes[0].Apply();
                }
                mesh.Draw();
            }
        }
        public void DrawSceneShadow(Matrix view, Matrix projection, int modelId = 3, int techniqueId = 1)
        {
            game.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            game.GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            game.GraphicsDevice.BlendState = BlendState.Opaque;

            //DrawModel(models[0], Matrix.CreateTranslation(-30, 0, -20), camera);
            //DrawModel(models[0], Matrix.CreateTranslation(0, 100, 0), camera);
            //DrawModel(models[1], Matrix.CreateTranslation(30, 0, -20), camera);
            //DrawModel(models[2], Matrix.CreateScale(0.05f) * Matrix.CreateTranslation(0, 0, 27), camera);
            ShadowDrawModel(models[modelId], Matrix.CreateTranslation(0, 0, 0), view, projection, techniqueId);
            //DrawModel(models[3], Matrix.CreateRotationX(-(float)Math.PI * 0.5f) * Matrix.CreateTranslation(0, 0, 100), camera);
        }
        private void ShadowDrawModel(Model model, Matrix world, Matrix view, Matrix projection, int techniqueId)
        {
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (Effect effect in mesh.Effects)
                {
                    effect.Parameters["World"].SetValue(world);
                    effect.Parameters["View"].SetValue(view);
                    effect.Parameters["Projection"].SetValue(projection);
                    effect.CurrentTechnique = effect.Techniques[techniqueId];
                    effect.Techniques[techniqueId].Passes[0].Apply();
                }
                mesh.Draw();
            }
        }
    }
}
