using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Leadtools.Windows.Annotations;

namespace Leadtools_Issue
{
	public class AnnotatedCanvas : Canvas
	{
		private AnnContainer _container;
		private SolidColorBrush _brush;
		private AnnRectangleDrawDesigner _rectangleDesigner;

		public void Annotate(SolidColorBrush brush)
		{
			_brush = brush;
			if (_container != null) return;
			_container = new AnnContainer
			{
				Width = ActualWidth,
				Height = ActualHeight
			};
			RenderOptions.SetBitmapScalingMode(_container, BitmapScalingMode.HighQuality);
			Children.Add(_container);
		}

		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			if (_rectangleDesigner == null && !e.Handled)
			{
				if (CaptureMouse())
				{
					var rectangleObject = new AnnRectangleObject
					{
						Fill = _brush.Color,
						StrokeThickness = 0d
					};
					_rectangleDesigner = new AnnRectangleDrawDesigner(_container)
					{
						ObjectTemplate = rectangleObject
					};
					_rectangleDesigner.Container.ClipToBounds = true;
					_rectangleDesigner.OnMouseLeftButtonDown(_container, e);
				}
			}
			base.OnMouseDown(e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			SetMouseCursor(e);
			if (_rectangleDesigner != null && e.LeftButton == MouseButtonState.Pressed && !e.Handled && _rectangleDesigner != null && IsMouseCaptured)
			{
				_rectangleDesigner.OnMouseMove(_rectangleDesigner.Container, e);
			}
			base.OnMouseMove(e);
		}

		protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
		{
			if (_rectangleDesigner != null && !e.Handled && _rectangleDesigner != null && IsMouseCaptured)
			{
				ReleaseMouseCapture();

				((AnnRectangleObject)_rectangleDesigner.DrawObject).Rect = GetIntersectedRectangle(_rectangleDesigner.DrawObject, _rectangleDesigner.Container);

				_rectangleDesigner.OnMouseLeftButtonUp(_rectangleDesigner.Container, e);
				_rectangleDesigner = null;
			}
			base.OnMouseLeftButtonUp(e);
		}

		private Rect GetIntersectedRectangle(AnnObject annObject, AnnContainer annContainer)
		{
			return Rect.Intersect(annObject.BoundingRectangle, new Rect(0, 0, annContainer.ActualWidth, annContainer.ActualHeight));
		}

		private void SetMouseCursor(MouseEventArgs e)
		{
			Cursor = Cursors.Arrow;
			if (_container != null)
			{
				Cursor = Cursors.Cross;
			}
		}
	}
}