using Microsoft.VisualStudio.Text.Editor;
using ScrollShared;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace SmoothScroll
{
	internal sealed class SmoothScrollProcessor : MouseProcessorBase, IParameters
	{
		public ScrollingSpeeds SpeedLever => SmoothScrollPackage.OptionsPage?.DurationRatio ?? ScrollingSpeeds.Normal;
		public ScrollingFPS FPS => SmoothScrollPackage.OptionsPage?.FPS ?? ScrollingFPS.High;

		private const int WM_MOUSEHWHEEL = 0x020E;

		private readonly IWpfTextView wpfTextView;

		private bool ExtEnable => SmoothScrollPackage.OptionsPage?.ExtEnable ?? true;
		private bool HookEnable => SmoothScrollPackage.OptionsPage?.HookEnable ?? true;
		private bool ShiftEnable => SmoothScrollPackage.OptionsPage?.ShiftEnable ?? true;
		private bool AltEnable => SmoothScrollPackage.OptionsPage?.AltEnable ?? true;
		private bool SmoothEnable => SmoothScrollPackage.OptionsPage?.SmoothEnable ?? true;
		private double DistanceRatio => SmoothScrollPackage.OptionsPage?.DistanceRatio ?? 1.1;

		private readonly ScrollController verticalController, horizontalController;

		internal SmoothScrollProcessor(IWpfTextView _wpfTextView)
		{
			this.wpfTextView = _wpfTextView;
			var pageScroller = new PageScroller(wpfTextView);
			verticalController = new ScrollController(pageScroller, this, ScrollingDirection.Vertical);
			horizontalController = new ScrollController(pageScroller, this, ScrollingDirection.Horizontal);

			if (HookEnable) {
				wpfTextView.VisualElement.Loaded += (_, __) => {
					var source = PresentationSource.FromVisual(wpfTextView.VisualElement) as HwndSource;
					source?.AddHook(MessageHook);
				};
			}
		}

		public override void PreprocessMouseWheel(MouseWheelEventArgs e)
		{
			if (!ExtEnable || Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
			{
				return;
			}

			if (AltEnable && (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)))
			{
				wpfTextView.ViewScroller.ScrollViewportVerticallyByPage(e.Delta < 0 ? ScrollDirection.Down : ScrollDirection.Up);
				wpfTextView.VisualElement.Focus();
				using (var writer = new StreamWriter(@"C:\Users\Serhan\Desktop\fuck.txt", true, Encoding.UTF8)) {
					writer.WriteLine($"wpfTextView.ViewportTop: {wpfTextView.ViewportTop}");
					writer.WriteLine($"wpfTextView.ViewportRight: {wpfTextView.ViewportRight}");
					writer.WriteLine($"wpfTextView.ViewportBottom: {wpfTextView.ViewportBottom}");
					writer.WriteLine($"wpfTextView.ViewportLeft: {wpfTextView.ViewportLeft}");
					writer.WriteLine($"wpfTextView.ViewportWidth: {wpfTextView.ViewportWidth}");
					writer.WriteLine($"wpfTextView.ViewportHeight: {wpfTextView.ViewportHeight}");
					writer.WriteLine($"wpfTextView.LineHeight: {wpfTextView.LineHeight}");
					writer.WriteLine($"wpfTextView.TextViewLines.Count: {wpfTextView.TextViewLines.Count}");
					writer.WriteLine($"wpfTextView.TextViewLines.FirstVisibleLine.Top: {wpfTextView.TextViewLines.FirstVisibleLine.Top}");
					writer.WriteLine($"wpfTextView.TextViewLines.FirstVisibleLine.Bottom: {wpfTextView.TextViewLines.FirstVisibleLine.Bottom}");
					writer.WriteLine($"wpfTextView.TextViewLines.LastVisibleLine.Top: {wpfTextView.TextViewLines.LastVisibleLine.Top}");
					writer.WriteLine($"wpfTextView.TextViewLines.LastVisibleLine.Bottom: {wpfTextView.TextViewLines.LastVisibleLine.Bottom}");
					writer.WriteLine();
				}

				e.Handled = true;
				return;
			}

			if (ShiftEnable && (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
			{
				PostScrollRequest(-e.Delta, ScrollingDirection.Horizontal);
				e.Handled = true;
				return;
			}

			PostScrollRequest(e.Delta, ScrollingDirection.Vertical);
			e.Handled = true;
		}

		public override void PostprocessMouseDown(MouseButtonEventArgs e)
		{
			verticalController.StopScroll();
			horizontalController.StopScroll();
		}

		private void PostScrollRequest(double distance, ScrollingDirection direction)
		{
			if (direction == ScrollingDirection.Vertical)
			{
				if (SmoothEnable && NativeMethods.IsMouseEvent())
				{
					verticalController.ScrollView(distance * DistanceRatio);
				}
				else
				{
					wpfTextView.ViewScroller.ScrollViewportVerticallyByPixels(distance);
				}
			}
			else
			{
				if (SmoothEnable && NativeMethods.IsMouseEvent())
				{
					horizontalController.ScrollView(distance * DistanceRatio);
				}
				else
				{
					wpfTextView.ViewScroller.ScrollViewportHorizontallyByPixels(distance);
				}
			}
		}

		private IntPtr MessageHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			switch (msg)
			{
				case WM_MOUSEHWHEEL:
					int delta = (short)NativeMethods.HIWORD(wParam);

					delta = Math.Sign(delta) * -20;
					PostScrollRequest(-delta, ScrollingDirection.Horizontal);

					handled = true;
					return (IntPtr)1;
				default:
					break;
			}

			return IntPtr.Zero;
		}
	}
}
