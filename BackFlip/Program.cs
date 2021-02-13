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
using System.Diagnostics;
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

        Instruments instruments = new Instruments();

        // beeper beepy beep
        VarioBeeping varioBeeper = new VarioBeeping()
        {
            GetVerticalVelocityMPS = ()=> (float)Instruments.totalEnergyVV,
            IsRunning = () => !SharpDX.Samples.DemoApp.IsFormClosed,
            Mute = true
        };

        protected override void Initialize(DemoConfiguration demoConfiguration)
        {
            base.Initialize(demoConfiguration);

            var config = File.ReadAllLines("config.txt").Select(l => l.Split('=')).ToDictionary(k => k[0], v => string.Join("=", v.Skip(1)));
            Instruments.localBaro = int.Parse(config["localBaro"]);
            comPort = config["comPort"];
            baudRate = int.Parse(config["baudRate"]);
            Instruments.mbOffset = float.Parse(config["mbOffset"]);

            // Zero, Zero
            _form.Top = _form.Left = 0;

            instruments.UpdateBaro();

            adhrs = new ADHRSXPlane(comPort, baudRate);

            // Initialize a TextFormat
            Stock.TextFormatCenter = new TextFormat(FactoryDWrite, "Calibri", 64)
            {
                TextAlignment = TextAlignment.Center,
                ParagraphAlignment = ParagraphAlignment.Center
            };
            Stock.TextFormatLeft = new TextFormat(FactoryDWrite, "Calibri", 64)
            {
                TextAlignment = TextAlignment.Leading,
                ParagraphAlignment = ParagraphAlignment.Center
            };
            Stock.TextFormatRight = new TextFormat(FactoryDWrite, "Calibri", 64)
            {
                TextAlignment = TextAlignment.Trailing,
                ParagraphAlignment = ParagraphAlignment.Center
            };
            Stock.TextFormatRightSmall = new TextFormat(FactoryDWrite, "Calibri", 48)
            {
                TextAlignment = TextAlignment.Trailing,
                ParagraphAlignment = ParagraphAlignment.Center
            };

            RenderTarget2D.TextAntialiasMode = TextAntialiasMode.Cleartype;

            Stock.ClientRectangle = new RectangleF(0, 0, demoConfiguration.Width, demoConfiguration.Height);

            baroColorBrush = new SolidColorBrush(RenderTarget2D, Color.DarkGray);
            instrumentColorBrush = new SolidColorBrush(RenderTarget2D, Color.LightGray);
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
                alphaTarget = 10f,
                alphaActual = 10f,
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

            varioBeeper.Start();
        }

        static ADHRSXPlane adhrs;
        SolidColorBrush baroColorBrush;
        SolidColorBrush instrumentColorBrush;
        SolidColorBrush errorBrush;
        DateTime lastRead = DateTime.Now;
        DateTime lastFrame = DateTime.Now;

        const int msPerFrame = (1000 / 40);

        protected override void Draw(DemoTime time)
        {
            var now = DateTime.Now;

            // Don't over-render, it costs CPU and that's energy
            var delta = (int)(now - lastFrame).TotalMilliseconds;
            if (delta < msPerFrame)
                System.Threading.Thread.Sleep(msPerFrame - delta);
            lastFrame = now;

            base.Draw(time);

            // Update WorldViewProj Matrix
            var viewProj = Matrix.Multiply(view, proj);
            var worldViewProj = Matrix.RotationZ(instruments.roll) * viewProj;

            // Matrix.RotationX(time) * Matrix.RotationY(time * 2) * Matrix.RotationZ(time * .7f)
            worldViewProj.Transpose();
            context.UpdateSubresource(ref worldViewProj, contantBuffer);

            var attitude = adhrs.RawRead();
            if (attitude.Count() > 0)
            {
                lastRead = now;
                instruments.SetFromAhrs(attitude);
            }

            if ((now - lastRead).TotalSeconds > 1.2)
            {
                RenderTarget2D.DrawLine(new Vector2(0, 0), new Vector2(Stock.ClientRectangle.Width, Stock.ClientRectangle.Height), errorBrush, 3.0f);
                RenderTarget2D.DrawLine(new Vector2(Stock.ClientRectangle.Width, 0), new Vector2(0, Stock.ClientRectangle.Height), errorBrush, 3.0f);
            }

            context.Draw(36, 0);

            instruments.Draw(RenderTarget2D, instrumentColorBrush);
        }

        /// <summary>
        /// Given pitot and static pressures, compute airspeed, altitude and vertical velocity
        /// </summary>
        /// <param name="pitotPress"></param>
        /// <param name="staticPress"></param>
        /// <param name="dT"></param>
        protected override void MouseClick(MouseEventArgs e)
        {
            base.MouseClick(e);

            if (e.X > Stock.ClientRectangle.Width*3/4)
            {
                if (e.Y > Stock.ClientRectangle.Height - 120)
                    instruments.UpdateBaro(-1);
                else if (e.Y > Stock.ClientRectangle.Height - 240)
                    instruments.UpdateBaro(+1);
                else if (e.Y < 120)
                    varioBeeper.Mute = !varioBeeper.Mute;

                
                SaveConfig();
            }

            if (e.X < 50 && e.Y < 50)
            {
                _form.Top = _form.Left = 0;

                _form.FormBorderStyle = _form.FormBorderStyle == System.Windows.Forms.FormBorderStyle.FixedSingle ?
                    System.Windows.Forms.FormBorderStyle.None : System.Windows.Forms.FormBorderStyle.FixedSingle;
            }
        }

        const string preziSetApp = @"C:\Windows\WinSxS\amd64_microsoft-windows-m..resentationsettings_31bf3856ad364e35_10.0.17763.1_none_5ded448f5b93896b\PresentationSettings.exe";

        protected override void EndRun()
        {
            base.EndRun();
            PresentationMode(false);
        }


        static string sideCarApp;
        static bool embeddedWindow;
        static string comPort;
        static int baudRate;
        static bool enablePresentationMode;

        //  This attempts to execute "PresenationSettings" to turn presentation mode on/off
        //    In presentation mode the machine won't go to sleep after some period of inactivity.  This is really important!  
        //    You dont want the machine to just go blank in 5 minutes.
        // - WEIRDnESS NOTE: I trie refactoring this and it stopped workign (identical code running in a static method)
        //  "Windows SxS" is a very very very odd little beast

        private static void PresentationMode(bool turnOn)
        {
            if (!enablePresentationMode)
                return;

            var startStop = turnOn ? "start" : "stop";

            var proc = new Process();
            proc.StartInfo.FileName = "cmd.exe"; ;
            proc.StartInfo.Arguments = $"/C \"{preziSetApp} /{startStop}\"";
            proc.StartInfo.UseShellExecute = true;
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            proc.StartInfo.Verb = "runas";
            proc.Start();
        }

        private void SaveConfig()
        {
            File.WriteAllLines("config.txt",
                new[]{
                    $"localBaro={Instruments.localBaro}",
                    $"comPort={comPort}",
                    $"baudRate={baudRate}",
                    $"alphaCal={Instruments.alphaCal}",
                    $"mbOffset={Instruments.mbOffset}",
                    $"sideCarApp={sideCarApp}",
                    $"embeddedWindow={embeddedWindow}",
                    $"enablePresentationMode={enablePresentationMode}",
                });
        }
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var config = File.ReadAllLines("config.txt").Select(l => l.Split('=').Select(v=>v.Trim())).ToDictionary(k => k.First(), v => string.Join("=", v.Skip(1)));
            comPort = config["comPort"];
            baudRate = int.Parse(config["baudRate"]);
            sideCarApp = config["sideCarApp"];
            embeddedWindow = config.ContainsKey("embeddedWindow") && bool.Parse(config["embeddedWindow"]);
            enablePresentationMode = config.ContainsKey("enablePresentationMode") && bool.Parse(config["enablePresentationMode"]);
            Instruments.Configure(config);

            Program program = new Program();

            var screen = Screen.PrimaryScreen.Bounds;

            var isLandscape = screen.Width > screen.Height;

            var midWidth = isLandscape ? 2 * screen.Width / 5 : screen.Width;
            var midHeight = isLandscape ? screen.Height : 2 * screen.Height / 5;

            if (!String.IsNullOrEmpty(sideCarApp) && !sideCarApp.StartsWith("#"))
            {
                var sideCarAppName = Path.GetFileNameWithoutExtension(sideCarApp.Split('/').Last());

                var xcSoar = Process.GetProcessesByName(sideCarAppName);
                var parts = sideCarApp.Split(' ');
                var appName = parts.First(); 

                if (xcSoar.Length == 0 && File.Exists(appName))
                {
                    Process.Start(appName, string.Join(" ", parts.Skip(1)));
                    System.Threading.Thread.Sleep(2000);
                }

                PositionWindow.SendRequest(sideCarAppName,
                    new System.Drawing.Rectangle(isLandscape ? (midWidth - 6) : 0, isLandscape ? 0 : (midHeight - 6),
                                                 isLandscape ? (screen.Width - midWidth + 12) : screen.Width, isLandscape ? screen.Height : (screen.Height - midHeight + 12)));
            }

            PresentationMode(true);

            program.Run(new DemoConfiguration("BackFlip - PFD", midWidth, midHeight) { HideWindowFrames = embeddedWindow });
        }
    }
}
