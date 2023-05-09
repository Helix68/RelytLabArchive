using System;
using System.Windows.Forms;
using System.Drawing;

using Vortice.DCommon;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.DXGI;

using VorticeMath = Vortice.Mathematics;
using System.Diagnostics;
using System.Numerics;
using System.Diagnostics.CodeAnalysis;

namespace RelytLab;

internal static class Program
{
	[STAThread]
	private static void Main()
	{
		ApplicationConfiguration.Initialize();

		var app = new RelytApp();
		app.Run();
		app.Cleanup();
	}
}

public class RelytApp
{
	private const bool _shouldCapFps = false;
	private readonly PointF _fpsDisplayLocation = new PointF(20, 20);

	private readonly Form _form;
	private bool _appNeedsExit;

	// Rendering resources
	private readonly ID2D1Factory _factoryD2D1;
	private readonly IDWriteFactory _factoryDWrite;
	private readonly ID2D1HwndRenderTarget _renderTarget;

	private readonly ID2D1SolidColorBrush _solidColorBrush;

	private readonly IDWriteTextFormat _textFormat_Small;
	private readonly IDWriteTextFormat _textFormat_Normal;
	private readonly IDWriteTextFormat _textFormat_Large;

	private IDWriteTextLayout _fpsTextLayout;

	// Misc
	private Point _mouseLocation;
	private readonly Stopwatch _stopwatch;
	private int _frameCount;

	// Initialize window, rendering and event handlers
	public RelytApp()
	{
		_stopwatch = new Stopwatch();

		// Create form
		_form = new Form()
		{
			Text = "RelytLab",

			// Initial size and position
			Width = 1200,
			Height = 800,
			StartPosition = FormStartPosition.CenterScreen,

			// Fullscreen
			FormBorderStyle = FormBorderStyle.None,
			WindowState = FormWindowState.Maximized,
		};

		// Create factories
		{
			_factoryD2D1 = D2D1.D2D1CreateFactory<ID2D1Factory>();
			_factoryDWrite = DWrite.DWriteCreateFactory<IDWriteFactory>();
		}

		// Create render target
		{
			RenderTargetProperties renderTargetProps = new RenderTargetProperties(
				RenderTargetType.Hardware,
				new PixelFormat(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Ignore),
				96.0f, 96.0f,
				RenderTargetUsage.None,
				FeatureLevel.Default);

			HwndRenderTargetProperties hwndRenderTargetProps = new HwndRenderTargetProperties()
			{
				Hwnd = _form.Handle,
				PixelSize = _form.ClientSize,
				PresentOptions = _shouldCapFps ? PresentOptions.None : PresentOptions.Immediately
			};

			_renderTarget = _factoryD2D1.CreateHwndRenderTarget(renderTargetProps, hwndRenderTargetProps);
		}

		// Create resources
		{
			_solidColorBrush = _renderTarget.CreateSolidColorBrush(new VorticeMath.Color(0.0f, 0.0f, 1.0f));
			_textFormat_Small = _factoryDWrite.CreateTextFormat("Roboto", FontWeight.SemiLight, Vortice.DirectWrite.FontStyle.Normal, FontStretch.Normal, 30.0f);
			_textFormat_Normal = _factoryDWrite.CreateTextFormat("Roboto", FontWeight.SemiLight, Vortice.DirectWrite.FontStyle.Normal, FontStretch.Normal, 60.0f);
			_textFormat_Large = _factoryDWrite.CreateTextFormat("Roboto", FontWeight.SemiLight, Vortice.DirectWrite.FontStyle.Normal, FontStretch.Normal, 100.0f);
		}

		// Bind event handlers
		{
			Application.ApplicationExit += Application_ApplicationExit;

			_form.ClientSizeChanged += _form_ClientSizeChanged;
			_form.FormClosed += _form_FormClosed;
			_form.MouseMove += _form_MouseMove;
			_form.KeyDown += _form_KeyDown;
		}

		UpdateFpsTextLayout();
	}

	// Run main loop
	public void Run()
	{
		_stopwatch.Start();

		_form.Show();
		// Cursor.Hide();

		while (!_appNeedsExit)
		{
			Application.DoEvents();

			Render();
		}
	}

	// Release rendering resources
	public void Cleanup()
	{

	}

	// Draw stuff onto the screen
	private void Render()
	{
		_renderTarget.BeginDraw();
		_renderTarget.Clear(new VorticeMath.Color(255, 255, 255));

		_solidColorBrush.Color = new VorticeMath.Color4(0, 0, 255);
		_renderTarget.FillRectangle(new RectangleF(0, _mouseLocation.Y, _form.ClientSize.Width, 1), _solidColorBrush);
		_renderTarget.FillRectangle(new RectangleF(_mouseLocation.X, 0, 1, _form.ClientSize.Height), _solidColorBrush);

		// Update _fpsTextLayout to current _frameCount
		if (_stopwatch.ElapsedMilliseconds > 1000)
		{
			UpdateFpsTextLayout();
			_frameCount = 0;
			_stopwatch.Restart();
		}

		// Fill rectangle behind _fpsTextLayout
		TextMetrics textMetrics = _fpsTextLayout.Metrics;
		_solidColorBrush.Color = new VorticeMath.Color4(230, 230, 230);
		_renderTarget.FillRectangle(new RectangleF(_fpsDisplayLocation.X, _fpsDisplayLocation.Y, textMetrics.Width, textMetrics.Height), _solidColorBrush);

		// Draw _fpsTextLayout
		_solidColorBrush.Color = new VorticeMath.Color4(0, 0, 0);
		_renderTarget.DrawTextLayout(new Vector2(_fpsDisplayLocation.X, _fpsDisplayLocation.Y), _fpsTextLayout, _solidColorBrush);

		_frameCount++;

		_renderTarget.EndDraw();
	}

	[MemberNotNull(nameof(_fpsTextLayout))]
	private void UpdateFpsTextLayout()
	{
		_fpsTextLayout = _factoryDWrite.CreateTextLayout("FPS " + _frameCount.ToString(), _textFormat_Small, float.MaxValue, float.MaxValue);
	}

	private void Application_ApplicationExit(object? sender, EventArgs e)
	{
		_appNeedsExit = true;
	}

	private void _form_ClientSizeChanged(object? sender, EventArgs e)
	{
		_renderTarget.Resize(_form.ClientSize);
	}

	private void _form_FormClosed(object? sender, FormClosedEventArgs e)
	{
		_appNeedsExit = true;
		Application.Exit();
	}

	private void _form_MouseMove(object? sender, MouseEventArgs e)
	{
		_mouseLocation = e.Location;
	}

	private void _form_KeyDown(object? sender, KeyEventArgs e)
	{
		if (e.KeyCode == Keys.Escape)
		{
			_appNeedsExit = true;
			Application.Exit();
		}
	}
}