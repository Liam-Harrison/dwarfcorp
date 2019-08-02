using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class FXAA
    {

        // This effects sub-pixel AA quality and inversely sharpness.
        //   Where N ranges between,
        //     N = 0.50 (default)
        //     N = 0.33 (sharper)
        private float N = 0.40f;

        // Choose the amount of sub-pixel aliasing removal.
        // This can effect sharpness.
        //   1.00 - upper limit (softer)
        //   0.75 - default amount of filtering
        //   0.50 - lower limit (sharper, less sub-pixel aliasing removal)
        //   0.25 - almost off
        //   0.00 - completely off
        private float subPixelAliasingRemoval = 0.75f;

        // The minimum amount of local contrast required to apply algorithm.
        //   0.333 - too little (faster)
        //   0.250 - low quality
        //   0.166 - default
        //   0.125 - high quality 
        //   0.063 - overkill (slower)
        private float edgeTheshold = 0.166f;

        // Trims the algorithm from processing darks.
        //   0.0833 - upper limit (default, the start of visible unfiltered edges)
        //   0.0625 - high quality (faster)
        //   0.0312 - visible limit (slower)
        // Special notes when using FXAA_GREEN_AS_LUMA,
        //   Likely want to set this to zero.
        //   As colors that are mostly not-green
        //   will appear very dark in the green channel!
        //   Tune by looking at mostly non-green content,
        //   then start at zero and increase until aliasing is a problem.
        private float edgeThesholdMin = 0f;

        // This does not effect PS3, as this needs to be compiled in.
        //   Use FXAA_CONSOLE__PS3_EDGE_SHARPNESS for PS3.
        //   Due to the PS3 being ALU bound,
        //   there are only three safe values here: 2 and 4 and 8.
        //   These options use the shaders ability to a free *|/ by 2|4|8.
        // For all other platforms can be a non-power of two.
        //   8.0 is sharper (default!!!)
        //   4.0 is softer
        //   2.0 is really soft (good only for vector graphics inputs)
        private float consoleEdgeSharpness = 8.0f;

        // This does not effect PS3, as this needs to be compiled in.
        //   Use FXAA_CONSOLE__PS3_EDGE_THRESHOLD for PS3.
        //   Due to the PS3 being ALU bound,
        //   there are only two safe values here: 1/4 and 1/8.
        //   These options use the shaders ability to a free *|/ by 2|4|8.
        // The console setting has a different mapping than the quality setting.
        // Other platforms can use other values.
        //   0.125 leaves less aliasing, but is softer (default!!!)
        //   0.25 leaves more aliasing, and is sharper
        private float consoleEdgeThreshold = 0.125f;

        // Trims the algorithm from processing darks.
        // The console setting has a different mapping than the quality setting.
        // This only applies when FXAA_EARLY_EXIT is 1.
        // This does not apply to PS3, 
        // PS3 was simplified to avoid more shader instructions.
        //   0.06 - faster but more aliasing in darks
        //   0.05 - default
        //   0.04 - slower and less aliasing in darks
        // Special notes when using FXAA_GREEN_AS_LUMA,
        //   Likely want to set this to zero.
        //   As colors that are mostly not-green
        //   will appear very dark in the green channel!
        //   Tune by looking at mostly non-green content,
        //   then start at zero and increase until aliasing is a problem.
        private float consoleEdgeThresholdMin = 0f;

        public void ValidateBuffer()
        {
            PresentationParameters pp = GameState.Game.GraphicsDevice.PresentationParameters;

            int width = Math.Min(pp.BackBufferWidth, 4096);
            int height = Math.Min(pp.BackBufferHeight, 4096);

            if (RenderTarget == null || RenderTarget.Width != width ||
                RenderTarget.Height != height || RenderTarget.IsDisposed || RenderTarget.IsContentLost)
            {
                if (pp.BackBufferWidth > 4096 || pp.BackBufferHeight > 4096)
                {
                    Console.Error.WriteLine("Uh oh. Back buffer is HUGE {0} x {1}. Will need to downscale the image to apply FXAA.",
                        pp.BackBufferWidth, pp.BackBufferHeight);
                }
                if (RenderTarget != null)
                {
                    RenderTarget.Dispose();
                }

                RenderTarget = new RenderTarget2D(GameState.Game.GraphicsDevice, width, height, false, pp.BackBufferFormat, DepthFormat.None);
                Viewport viewport = new Viewport(0, 0, RenderTarget.Width, RenderTarget.Height);
                Matrix projection = Matrix.CreateOrthographicOffCenter(0, viewport.Width, viewport.Height, 0, 0, 1);
                Matrix halfPixelOffset = Matrix.CreateTranslation(-0.5f, -0.5f, 0);
                Shader.Parameters["World"].SetValue(Matrix.Identity);
                Shader.Parameters["View"].SetValue(Matrix.Identity);
                Shader.Parameters["Projection"].SetValue(halfPixelOffset * projection);
                Shader.Parameters["InverseViewportSize"].SetValue(new Vector2(1f / viewport.Width, 1f / viewport.Height));
                Shader.Parameters["ConsoleSharpness"].SetValue(new Vector4(
                    -N / RenderTarget.Width,
                    -N / RenderTarget.Height,
                    N / RenderTarget.Width,
                    N / RenderTarget.Height
                    ));
                Shader.Parameters["ConsoleOpt1"].SetValue(new Vector4(
                    -2.0f / RenderTarget.Width,
                    -2.0f / RenderTarget.Height,
                    2.0f / RenderTarget.Width,
                    2.0f / RenderTarget.Height
                    ));
                Shader.Parameters["ConsoleOpt2"].SetValue(new Vector4(
                    8.0f / RenderTarget.Width,
                    8.0f / RenderTarget.Height,
                    -4.0f / RenderTarget.Width,
                    -4.0f / RenderTarget.Height
                    ));
                Shader.Parameters["SubPixelAliasingRemoval"].SetValue(subPixelAliasingRemoval);
                Shader.Parameters["EdgeThreshold"].SetValue(edgeTheshold);
                Shader.Parameters["EdgeThresholdMin"].SetValue(edgeThesholdMin);
                Shader.Parameters["ConsoleEdgeSharpness"].SetValue(consoleEdgeSharpness);
                Shader.Parameters["ConsoleEdgeThreshold"].SetValue(consoleEdgeThreshold);
                Shader.Parameters["ConsoleEdgeThresholdMin"].SetValue(consoleEdgeThresholdMin);
            }
        }

        public void Begin(DwarfTime lastTime)
        {
               ValidateBuffer();
               GameState.Game.GraphicsDevice.SetRenderTarget(RenderTarget);
        }

        public void End(DwarfTime lastTime)
        {
            ValidateBuffer();
            GameState.Game.GraphicsDevice.SetRenderTarget(null);
            try
            {
                DwarfGame.SafeSpriteBatchBegin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, Shader, Matrix.Identity);
                DwarfGame.SpriteBatch.Draw(RenderTarget, GameState.Game.GraphicsDevice.Viewport.Bounds, Color.White);
            }
            finally
            {
                DwarfGame.SpriteBatch.End();
            }
        }

        public void Initialize()
        {
            PresentationParameters pp = GameState.Game.GraphicsDevice.PresentationParameters;
            int width = Math.Min(pp.BackBufferWidth, 4096);
            int height = Math.Min(pp.BackBufferHeight, 4096);

            if (pp.BackBufferWidth > 4096 || pp.BackBufferHeight > 4096)
            {
                Console.Error.WriteLine("Uh oh. Back buffer is HUGE {0} x {1}. Will need to downscale the image to apply FXAA.",
                    pp.BackBufferWidth, pp.BackBufferHeight);
            }
            Shader = GameStates.GameState.Game.Content.Load<Effect>(ContentPaths.Shaders.FXAA);
            RenderTarget = new RenderTarget2D(GameState.Game.GraphicsDevice, width, height, false, pp.BackBufferFormat, DepthFormat.None);

            Viewport viewport = GameState.Game.GraphicsDevice.Viewport;
            Matrix projection = Matrix.CreateOrthographicOffCenter(0, RenderTarget.Width, RenderTarget.Height, 0, 0, 1);
            Matrix halfPixelOffset = Matrix.CreateTranslation(-0.5f, -0.5f, 0);
            Shader.Parameters["World"].SetValue(Matrix.Identity);
            Shader.Parameters["View"].SetValue(Matrix.Identity);
            Shader.Parameters["Projection"].SetValue(halfPixelOffset * projection);
            Shader.Parameters["InverseViewportSize"].SetValue(new Vector2(1f / RenderTarget.Width, 1f / RenderTarget.Height));
            Shader.Parameters["ConsoleSharpness"].SetValue(new Vector4(
                -N / RenderTarget.Width,
                -N / RenderTarget.Height,
                N / RenderTarget.Width,
                N / RenderTarget.Height
                ));
            Shader.Parameters["ConsoleOpt1"].SetValue(new Vector4(
                -2.0f / RenderTarget.Width,
                -2.0f / RenderTarget.Height,
                2.0f / RenderTarget.Width,
                2.0f / RenderTarget.Height
                ));
            Shader.Parameters["ConsoleOpt2"].SetValue(new Vector4(
                8.0f / RenderTarget.Width,
                8.0f / RenderTarget.Height,
                -4.0f / RenderTarget.Width,
                -4.0f / RenderTarget.Height
                ));
            Shader.Parameters["SubPixelAliasingRemoval"].SetValue(subPixelAliasingRemoval);
            Shader.Parameters["EdgeThreshold"].SetValue(edgeTheshold);
            Shader.Parameters["EdgeThresholdMin"].SetValue(edgeThesholdMin);
            Shader.Parameters["ConsoleEdgeSharpness"].SetValue(consoleEdgeSharpness);
            Shader.Parameters["ConsoleEdgeThreshold"].SetValue(consoleEdgeThreshold);
            Shader.Parameters["ConsoleEdgeThresholdMin"].SetValue(consoleEdgeThresholdMin);

            Shader.CurrentTechnique = Shader.Techniques["FXAA"];

        }

        public RenderTarget2D RenderTarget { get; set; }
        public Effect Shader { get; set; }
    }
}
