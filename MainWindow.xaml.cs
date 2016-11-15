using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using Leadtools;
using Leadtools.Codecs;
using Leadtools.Forms.DocumentWriters;
using Leadtools.Forms.Ocr;

namespace Leadtools_Issue
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			using (var licenseStream = typeof(MainWindow).Assembly.GetManifestResourceStream("Leadtools_Issue.License.LEADTOOLS.LIC"))
			using (var licenseKeyStream = typeof(MainWindow).Assembly.GetManifestResourceStream("Leadtools_Issue.License.LEADTOOLS.LIC.KEY"))
			{
				if (licenseStream != null && licenseKeyStream != null)
				{
					RasterSupport.SetLicense(licenseStream, new StreamReader(licenseKeyStream).ReadToEnd());
				}
				else
				{
					throw new ApplicationException("LeadTools license not found");
				}
			}
			InitializeComponent();
		}

		private static RasterImage LoadImage(string filename)
		{
			if (string.IsNullOrWhiteSpace(filename)) return null;
			using (var codecs = new RasterCodecs())
			{
				ConfigureCodecs(codecs, filename);
				return codecs.Load(filename);
			}
		}

		private static void ConfigureCodecs(RasterCodecs codecs, string filename)
		{
			codecs.Options.Save.RetrieveDataFromImage = true;
			codecs.Options.Jpeg.Save.QualityFactor = 10;
			codecs.Options.Png.Save.QualityFactor = 9;
			codecs.Options.Pdf.Save.SavePdfv14 = true;
			codecs.Options.Pdf.Save.SavePdfv15 = false;
			codecs.Options.Pdf.Save.SavePdfv16 = false;
			codecs.Options.Pdf.Save.ExtractText = true;
			codecs.Options.Pdf.Save.ExtractTextGraphics = true;
			codecs.Options.Pdf.Save.UseImageResolution = true;
			codecs.Options.Pdf.Save.PrintDocument = true;
			codecs.Options.Pdf.Save.PrintFaithful = true;
			codecs.Options.Pdf.Save.AssembleDocument = true;
			codecs.Options.Load.AllPages = true;
			codecs.Options.Load.Rotated = true;
			codecs.Options.Load.Compressed = true;
			codecs.Options.Load.LoadCorrupted = true;
			using (var info = codecs.GetInformation(filename, false))
			{
				if (info.HasResolution)
				{
					codecs.Options.Load.XResolution = info.XResolution;
					codecs.Options.Load.YResolution = info.YResolution;
				}
				else
				{
					codecs.Options.Load.XResolution = 96;
					codecs.Options.Load.YResolution = 96;
				}

				if (info.Format == RasterImageFormat.RasPdf)
				{
					codecs.Options.Pdf.Load.DisplayDepth = info.BitsPerPixel;
					codecs.Options.Load.DiskMemory = true;
					int xRes, yRes;
					var rasInfo = codecs.GetRasterPdfInfo(filename, 1);
					if (info.TotalPages > 1)
					{
						var rasInfo2 = codecs.GetRasterPdfInfo(filename, 2);
						if (rasInfo2.Width != rasInfo.Width) rasInfo.Width = -1;
						if (rasInfo2.Height != rasInfo.Height) rasInfo.Height = -1;
					}
					GetRasterResolution(info.Width, info.Height, info.XResolution, info.YResolution, rasInfo, out xRes, out yRes);
					codecs.Options.Load.XResolution = xRes;
					codecs.Options.Load.YResolution = yRes;
					codecs.Options.RasterizeDocument.Load.XResolution = xRes;
					codecs.Options.RasterizeDocument.Load.YResolution = yRes;
				}

				if (codecs.Options.Pdf.IsEngineInstalled)
				{
					codecs.Options.Pdf.Load.UseLibFonts = true;
				}
				codecs.Options.Pdf.Load.GraphicsAlpha = 1;
				codecs.Options.Pdf.Load.TextAlpha = 1;
			}
		}

		private static void GetRasterResolution(int imgWidth, int imgHeight, int imgXRes, int imgYRes, CodecsRasterPdfInfo rasInfo, out int xRes, out int yRes)
		{
			var fillsPageX = rasInfo.XResolution > 0 && rasInfo.Width > 0
							 && rasInfo.Width <= imgWidth * rasInfo.XResolution / imgXRes;
			var fillsPageY = rasInfo.YResolution > 0 && rasInfo.Height > 0
							 && rasInfo.Height <= imgHeight * rasInfo.YResolution / imgYRes;

			if (rasInfo.IsLeadPdf || (fillsPageX && fillsPageY))
			{
				xRes = rasInfo.XResolution; yRes = rasInfo.YResolution;
			}
			else
			{
				xRes = 0; yRes = 0;
			}

			int maxDimension = imgHeight;
			int res = imgYRes;
			if (imgWidth > imgHeight)
			{
				maxDimension = imgWidth;
				res = imgXRes;
			}

			int computedResolution = Math.Min(300, 5000 / (maxDimension / res));
			xRes = Math.Max(xRes, computedResolution);
			yRes = Math.Max(yRes, computedResolution);
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			var writer = new DocumentWriter();
			var pdfOptions = (PdfDocumentOptions)writer.GetOptions(DocumentFormat.Pdf);
			SetPdfOptions(pdfOptions);
			writer.SetOptions(DocumentFormat.Pdf, pdfOptions);

			IOcrEngine engine = OcrEngineManager.CreateEngine(OcrEngineType.Advantage, IntPtr.Size > 4);
			engine.Startup(null, writer, null, "OCREngine");
			IOcrDocument doc = engine.DocumentManager.CreateDocument();
			var outputFile = $".\\{Guid.NewGuid()}.pdf";
			try
			{
				using (var image = LoadImage("test.tif"))
				{
					doc.Pages.AddPages(image, 1, image.PageCount, null);
				}

				doc.Pages.Recognize(null);
				doc.Save(outputFile, DocumentFormat.Pdf, null);
			}
			catch (OcrException)
			{
				MessageBox.Show(this, "OCR Engine failure", "OCR Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				doc.Dispose();
				engine.Shutdown();
				engine.Dispose();
				Process.Start(outputFile);
			}
		}

		private static void SetPdfOptions(PdfDocumentOptions pdfOptions)
		{
			pdfOptions.DocumentType = PdfDocumentType.Pdf;
			pdfOptions.FontEmbedMode = DocumentFontEmbedMode.Auto;
			pdfOptions.ImageOverText = true;
			pdfOptions.HighQualityPrintEnabled = true;
			pdfOptions.AutoBookmarksEnabled = true;
			pdfOptions.QualityFactor = 10;
			pdfOptions.AnnotationsEnabled = true;
			pdfOptions.AssemblyEnabled = true;
			pdfOptions.OneBitImageCompression = OneBitImageCompressionType.FaxG4;
			pdfOptions.ColoredImageCompression = ColoredImageCompressionType.Lzw;
			pdfOptions.CopyEnabled = true;
			pdfOptions.EditEnabled = true;
			pdfOptions.PrintEnabled = true;
		}

		private void MainWindow_OnClosed(object sender, EventArgs e)
		{
			DirectoryInfo di = new DirectoryInfo(".");
			FileInfo[] files = di.GetFiles("*.pdf")
													 .Where(p => p.Extension == ".pdf").ToArray();
			foreach (FileInfo file in files)
				try
				{
					file.Attributes = FileAttributes.Normal;
					File.Delete(file.FullName);
				}
				catch { }
		}
	}
}
