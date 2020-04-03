// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Linq;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.DirectWrite;
using SharpDX.DXGI;
using SharpDX.Samples;
using TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode;
using System.Windows.Forms;
using System.IO;

namespace BackFlip
{

    /// <summary>
    /// Shows how to use DirectWrite to render simple text.
    /// Port of DirectWrite sample SimpleHelloWorld from Windows 7 SDK samples
    /// http://msdn.microsoft.com/en-us/library/dd742738%28v=VS.85%29.aspx
    /// </summary>
    public class Program :  Direct2D1DemoApp
    {
        private Matrix view, proj;
        SharpDX.Direct3D11.Buffer contantBuffer;
        SharpDX.Direct3D11.DeviceContext context;


        TextFormat TextFormatCenter, TextFormatLeft, TextFormatRight, TextFormatRightSmall;

        public RectangleF ClientRectangle { get; private set; }
        protected override void Initialize(DemoConfiguration demoConfiguration)
        {
            base.Initialize(demoConfiguration);

            var config = File.ReadAllLines("config.txt").Select(l => l.Split('=')).ToDictionary(k => k[0], v => string.Join("=", v.Skip(1)));
            localBaro = int.Parse(config["localBaro"]);
            comPort = config["comPort"];
            baudRate = int.Parse(config["baudRate"]);
            mbOffset = float.Parse(config["mbOffset"]);

            UpdateBaro();

            adhrs = new ADHRS(comPort, baudRate);

            // Initialize a TextFormat
            TextFormatCenter = new TextFormat(FactoryDWrite, "Calibri", 64)
            {
                TextAlignment = TextAlignment.Center,
                ParagraphAlignment = ParagraphAlignment.Center
            };
            TextFormatLeft = new TextFormat(FactoryDWrite, "Calibri", 64)
            {
                TextAlignment = TextAlignment.Leading,
                ParagraphAlignment = ParagraphAlignment.Center
            };
            TextFormatRight = new TextFormat(FactoryDWrite, "Calibri", 64)
            {
                TextAlignment = TextAlignment.Trailing,
                ParagraphAlignment = ParagraphAlignment.Center
            };
            TextFormatRightSmall = new TextFormat(FactoryDWrite, "Calibri", 48)
            {
                TextAlignment = TextAlignment.Trailing,
                ParagraphAlignment = ParagraphAlignment.Center
            };

            RenderTarget2D.TextAntialiasMode = TextAntialiasMode.Cleartype;

            ClientRectangle = new RectangleF(0, 0, demoConfiguration.Width, demoConfiguration.Height);

            SceneColorBrush.Color = Color.LightGray;
            baroColorBrush = new SolidColorBrush(RenderTarget2D, Color.DarkGray);
            errorBrush = new SolidColorBrush(RenderTarget2D, Color.Red);


            // Initialize Chevrons

            // Compile Vertex and Pixel shaders
            var vertexShaderByteCode = ShaderBytecode.CompileFromFile("BackFlip.fx", "VS", "vs_4_0");
            var vertexShader = new VertexShader(Device, vertexShaderByteCode);

            var pixelShaderByteCode = ShaderBytecode.CompileFromFile("BackFlip.fx", "PS", "ps_4_0");
            var pixelShader = new PixelShader(Device, pixelShaderByteCode);

            var signature = ShaderSignature.GetInputSignature(vertexShaderByteCode);
            // Layout from VertexShader input signature
            var layout = new InputLayout(Device, signature, new[]
                    {
                        new SharpDX.Direct3D11.InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
                        new SharpDX.Direct3D11.InputElement("COLOR", 0, Format.R32G32B32A32_Float, 16, 0)
                    });

            // Instantiate Vertex buiffer from vertex data
            var vertices = SharpDX.Direct3D11.Buffer.Create(Device, BindFlags.VertexBuffer, new BackFlip.Chevrons3()
            {
                Size = new Size2F(1f, 2f),
                alphaMax = 15,
                alphaTarget = 10
            }.Make());

            // Create Constant Buffer
            contantBuffer = new SharpDX.Direct3D11.Buffer(Device, Utilities.SizeOf<Matrix>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);

            context = Device.ImmediateContext;

            // Prepare All the stages
            context.InputAssembler.InputLayout = layout;
            context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertices, Utilities.SizeOf<Vector4>() * 2, 0));
            context.VertexShader.SetConstantBuffer(0, contantBuffer);
            context.VertexShader.Set(vertexShader);
            context.PixelShader.Set(pixelShader);

            // Prepare matrices
            view = Matrix.LookAtLH(new Vector3(0, 0, -5), new Vector3(0, 0, 0), Vector3.UnitY);

            proj = Matrix.PerspectiveFovLH((float)Math.PI / 4.0f, demoConfiguration.Width / (float)demoConfiguration.Height, 0.1f, 100.0f);
        }

        private void UpdateBaro()
        {
            seaLevelMp = localBaro * 1017.25f / 2992f;
        }

        private void SaveConfig()
        {
            File.WriteAllLines("config.txt", new []{ $"localBaro={localBaro}", $"comPort={comPort}", $"baudRate={baudRate}", $"mbOffset={mbOffset}"});
        }

        string comPort;
        int baudRate;
        static ADHRS adhrs;
        string airspeed = "0";
        int altitude = 0;
        string heading = "352";
        int localBaro = 3004;
        float seaLevelMp; // stdPres = 1013.25f
        float roll = 0f;
        //[TBD] float pitch = 0f; 
        float dp_Coef = 11.0f; // <-- calibrate this
        float AIS_Baseline = 2178;
        // House altitude 892.7 '
        SolidColorBrush baroColorBrush;
        SolidColorBrush errorBrush;
        DateTime lastRead = DateTime.Now;
        DateTime lastFrame = DateTime.Now;
        float mbOffset = 0f;

        const int msPerFrame = (1000/40);

        protected override void Draw(DemoTime time)
        {
            var now = DateTime.Now;
            // Don't over-render, it costs CPU and that's energy
            var delta = (int)(now-lastFrame).TotalMilliseconds;
            if (delta < msPerFrame)
                System.Threading.Thread.Sleep(msPerFrame-delta);
            lastFrame = now;

            base.Draw(time);

            // Update WorldViewProj Matrix
            var viewProj = Matrix.Multiply(view, proj);
            var worldViewProj = Matrix.RotationZ(roll) * viewProj;

            // Matrix.RotationX(time) * Matrix.RotationY(time * 2) * Matrix.RotationZ(time * .7f)
            worldViewProj.Transpose();
            context.UpdateSubresource(ref worldViewProj, contantBuffer);

            var attitude = adhrs.RawRead();
            if (attitude.Count() > 0)
            {
                roll = attitude[ADHRS.Roll];
                altitude = (10 * (Altitude(attitude[ADHRS.Baro]-mbOffset, seaLevelMp) / 10));
                heading = (5 * ((int)attitude[ADHRS.Heading] / 5)).ToString();
                //pitch = -10 * ahrsLine[ADHRS.Pitch]; 
                var dp = dp_Coef * (attitude[ADHRS.IAS] - AIS_Baseline);
                var speed = (int)Math.Sqrt(Math.Max(0, dp));
                airspeed = (speed < 30 ? 0 : speed).ToString();
                lastRead = DateTime.Now;
            }

            if ((DateTime.Now - lastRead).TotalSeconds > 1.2)
            {
                RenderTarget2D.DrawLine(new Vector2(0, 0), new Vector2(ClientRectangle.Width, ClientRectangle.Height), errorBrush, 3.0f);
                RenderTarget2D.DrawLine(new Vector2(ClientRectangle.Width, 0), new Vector2(0, ClientRectangle.Height), errorBrush, 3.0f);
            }

            // Draw the cube
            context.Draw(36, 0);

            RenderTarget2D.DrawText(heading, TextFormatCenter, new RectangleF (0,0,ClientRectangle.Width,100), SceneColorBrush);
            RenderTarget2D.DrawText((altitude/100).ToString(), TextFormatRight, new RectangleF(0, 5, ClientRectangle.Width-50, ClientRectangle.Height), SceneColorBrush);
            RenderTarget2D.DrawText((altitude%100).ToString().PadLeft(2, '0'), TextFormatRightSmall, ClientRectangle, SceneColorBrush);

            RenderTarget2D.DrawText((localBaro/100f).ToString("0.00"), TextFormatRightSmall, new RectangleF(0, ClientRectangle.Height-200, ClientRectangle.Width, 200), baroColorBrush);

            RenderTarget2D.DrawText(airspeed, TextFormatLeft, ClientRectangle, SceneColorBrush);

        }

        protected override void MouseClick(MouseEventArgs e)
        {
            base.MouseClick(e);

            if (e.X > ClientRectangle.Width*3/4)
            {
                if (e.Y > ClientRectangle.Height - 120)
                    localBaro -= 1;
                else
                    localBaro += 1;

                UpdateBaro();
                SaveConfig();
            }
        }

        private int Altitude(float pressure, float seaLevelMp)
        {
            return (int)(/* meters=> 44330*/ 145439.6f * (1.0 - Math.Pow(pressure / seaLevelMp, 0.1902949)));
        }


        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Program program = new Program();
            program.Run(new DemoConfiguration("BackFlip - PFD"));
        }
    }
}
