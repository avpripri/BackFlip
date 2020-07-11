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
using System.Threading.Tasks;
using System.Collections.Generic;

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

        float verticalVelocity = 0;
        bool done = false;
        bool beepZero = false;

        async void VariometerBeeper()
        {
            await Task.Run(() => BeepVario());
        }

        private void BeepVario()
        {
            while (!done)
            {
                var duration = (int)Math.Max(100, Math.Min(500, 1000f / (verticalVelocity + 2f)));
                var tone = 780 + (int)Math.Max(0, 100 * verticalVelocity);

                // only beep above zero, by config
                if (beepZero || verticalVelocity > 0)
                    Console.Beep(tone, duration);

                System.Threading.Thread.Sleep(Math.Max(250, duration));
            }
        }

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
        string vsi = "0";
        string vsi30 = "30s 0";
        string heading = "352";
        int localBaro = 3004;
        float seaLevelMp; // stdPres = 1013.25f
        float roll = 0f;
        float pitch = 0f;
        float dp_Coef = 4.91744f; // <-- calibrate this, currently for m/s
        float AIS_Baseline = 2178;
        // House altitude 892.7 '
        SolidColorBrush baroColorBrush;
        SolidColorBrush errorBrush;
        DateTime lastRead = DateTime.Now;
        DateTime lastFrame = DateTime.Now;
        float mbOffset = 0f;
        double speedLast;
        double baro2XLast;
        const double coefOfPressChange = 0.19029495718363463368220742150333d;  /* 1 / 5.25 */

        Queue<float> baroHist = new Queue<float>();     // Used to calculate baro average
        float runningMeanVertVelocity;
        float meanVerticalVelocity;
        int sampleTick = 0;

        const int msPerFrame = (1000/40);

        // Common unit conversion factors
        // NOTE: All base calculations are SI units (m, m/s, mb).  When displayed, they are converted to pilot prefered units

        const float mph2mps = 0.44704f;
        const float mps2kts = 1.943844f;
        const float mps2fpm = 196.85039370078738f;
        private const int averagingSampleCount = (30*30); // typically we're around 30fps, and I want 30 second average.
        


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
                var nowT = DateTime.Now;
                var dT = (float)(nowT - lastRead).TotalSeconds;

                roll = attitude[ADHRS.Roll];
                heading = (5 * ((int)attitude[ADHRS.Heading] / 5)).ToString();
                //pitch = -10 * ahrsLine[ADHRS.Pitch]; 

                CalculatePressureInstruments(attitude[ADHRS.IAS], attitude[ADHRS.Baro], dT);

                lastRead = nowT;
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

            RenderTarget2D.DrawText(vsi, TextFormatRight,        new RectangleF(0, 10, ClientRectangle.Width - 15, 150), SceneColorBrush);
            RenderTarget2D.DrawText(vsi30, TextFormatRightSmall, new RectangleF(0, 10, ClientRectangle.Width, 50), SceneColorBrush);

        }

        /// <summary>
        /// Given pitot and static pressures, compute airspeed, altitude and vertical velocity
        /// </summary>
        /// <param name="pitotPress"></param>
        /// <param name="staticPress"></param>
        /// <param name="dT"></param>

        private void CalculatePressureInstruments(float pitotPress, float staticPress, float dT)
        {
            var speedMps = Math.Sqrt(dp_Coef * Math.Max(0, pitotPress - AIS_Baseline));
            var speedKts = speedMps * mps2kts;

            // Compute the airspeed
            airspeed = ((int)(speedMps < 30d ? 0 : speedMps)).ToString();

            var dV = (speedLast - speedMps) / dT;
            var kinetticFactor = Math.Sign(dV) * dV * dV / 19.8d; // must be signed to work... lossing velocity needs to drop total energy

            var baro2X = Math.Pow((staticPress - mbOffset) / seaLevelMp, coefOfPressChange);

            // Altituded change from pressure difference derivation
            // --- Given
            // k = 44330
            // x = 1 / 5.25
            // p0 = 1013.25
            // --- stubtracting two pressures equations yields;
            // [k * (1 - (p2 / p0) ^ x)] - [k * (1 - (p1 / p0) ^ x)]
            // -- Then
            // k * [(1 - (p2 / p0) ^ x) - (1 - (p1 / p0) ^ x)]
            // k * (1 - (p2 / p0) ^ x - 1 + (p1 / p0) ^ x)
            // k * ((p1 / p0) ^ x - (p2 / p0) ^ x)  => p1^x / p0^x
            // k * (p1 ^ x - p2 ^ x) / p0 ^ x
            // --- QED
            // k / p0 ^ x * (p1 ^ x - p2 ^ x), or
            // 11862.610784520926279471081940874 * (p1 ^ x - p2 ^ x)
            const double k_over_p02x = 11862.610784520926279471081940874;

            // Convert the baro pressure then add the kinnetic energy factor to generate a total energy
            verticalVelocity = (float)((k_over_p02x * (baro2X - baro2XLast) / dT) + kinetticFactor);

            baroHist.Enqueue(verticalVelocity);
            runningMeanVertVelocity += verticalVelocity;

            if (baroHist.Count() > averagingSampleCount)
            {
                meanVerticalVelocity = runningMeanVertVelocity / 256;
                runningMeanVertVelocity -= baroHist.Dequeue();

                if (sampleTick++ > 1000)  // reset the runningMean periodically or float rounding errors will creap in
                {
                    runningMeanVertVelocity = baroHist.Sum(v => v);
                    sampleTick = 0;
                }
            }

            altitude = (int)(/* meters=> 44330*/145439.6d * (1.0 - baro2X));

            speedLast = speedMps;
            baro2XLast = baro2X;

            vsi = (10 * (int)(verticalVelocity * mps2fpm / 10f)).ToString();
            vsi30 = " 30s" + (10 * (int)(meanVerticalVelocity * mps2fpm / 10f)).ToString();
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
            return (int)(/* meters=> 44330*/ 145439.6f * (1.0 - Math.Pow(pressure / seaLevelMp, coefOfPressChange)));
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
