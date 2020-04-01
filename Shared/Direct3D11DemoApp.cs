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
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace SharpDX.Samples
{
    /// <summary>
    /// Root class for Direct3D10(.1) Demo App
    /// </summary>
    public class Direct3D11DemoApp : DemoApp
    {
        Direct3D11.Device _device;
        SwapChain _swapChain;
        Texture2D _backBuffer;
        RenderTargetView _backBufferView;

        /// <summary>
        /// Returns the device
        /// </summary>
        public Direct3D11.Device Device
        {
            get
            {
                return _device;
            }
        }

        /// <summary>
        /// Returns the backbuffer used by the SwapChain
        /// </summary>
        public Texture2D BackBuffer
        {
            get
            {
                return _backBuffer;
            }
        }

        /// <summary>
        /// Returns the render target view on the backbuffer used by the SwapChain.
        /// </summary>
        public RenderTargetView BackBufferView
        {
            get
            {
                return _backBufferView;
            }
        }

        DepthStencilView depthView;

        protected override void Initialize(DemoConfiguration demoConfiguration)
        {
            // SwapChain description
            var desc = new SwapChainDescription()
            {
                BufferCount = 1,
                ModeDescription = 
                    new ModeDescription(demoConfiguration.Width, demoConfiguration.Height,
                                        new Rational(60, 1), Format.R8G8B8A8_UNorm),
                IsWindowed = true,
                OutputHandle = DisplayHandle,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput
            };

            // Create Device and SwapChain
            Direct3D11.Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.BgraSupport, new [] { FeatureLevel.Level_10_0 }, desc, out _device, out _swapChain);

            // Ignore all windows events
            Factory factory = _swapChain.GetParent<Factory>();
            factory.MakeWindowAssociation(DisplayHandle, WindowAssociationFlags.IgnoreAll);

            // New RenderTargetView from the backbuffer
            _backBuffer = Texture2D.FromSwapChain<Texture2D>(_swapChain, 0);

            _backBufferView = new RenderTargetView(_device, _backBuffer);

            // Create the depth buffer
            var depthBuffer = new Texture2D(Device, new Texture2DDescription()
            {
                Format = Format.D32_Float_S8X24_UInt,
                ArraySize = 1,
                MipLevels = 1,
                Width = Config.Width, 
                Height = Config.Height,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            });

            // Create the depth buffer view
            depthView = new DepthStencilView(Device, depthBuffer);
        }

        //public void SwapFullScreen()
        //{
        //    Output target;
        //    Mathematics.Interop.RawBool isFullScreen;

        //    _swapChain.GetFullscreenState(out isFullScreen, out target);
        //    var fullScreen = (bool)isFullScreen;

        //    var _screen = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Size;

        //    _swapChain.ResizeBuffers(1, fullScreen?_screen.Width:Config.Width, fullScreen ? _screen.Height : Config.Height, Format.Unknown, SwapChainFlags.AllowModeSwitch);
        //    _swapChain.SetFullscreenState(!fullScreen, target);
        //}

        protected override void BeginDraw()
        {
            base.BeginDraw();
            //Device.ImmediateContext.Rasterizer.SetViewport(new Viewport(0, 0, Config.Width, Config.Height));
            //Device.ImmediateContext.OutputMerger.SetTargets(_backBufferView);

            var context = Device.ImmediateContext;
            // Setup targets and viewport for rendering
            context.Rasterizer.SetViewport(new Viewport(0, 0, Config.Width, Config.Height, 0.0f, 1.0f));
            context.OutputMerger.SetTargets(depthView, BackBufferView);

            context.ClearDepthStencilView(depthView, DepthStencilClearFlags.Depth, 1.0f, 0);
            context.ClearRenderTargetView(BackBufferView, Color.Black);

        }


        protected override void EndDraw()
        {
            _swapChain.Present(Config.WaitVerticalBlanking?1:0, PresentFlags.None);
        }
    }
}