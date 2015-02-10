using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Revsoft.Wabbitcode.Extensions;
using Revsoft.Wabbitcode.Properties;
using Revsoft.Wabbitcode.Services.Interfaces;
using Revsoft.Wabbitcode.Utils;

namespace Revsoft.Wabbitcode.GUI.DocumentWindows
{
    public sealed class ImageViewer : AbstractFileEditor
    {
        private const double ConstZoomFactor = 1.4;
        private const double MaxZoom = 16.0;
        private const double MinZoom = .125;

        private Bitmap _originalImage;
        private double _currentZoom = 1.0;

        private readonly PictureBox _pictureBox = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.AutoSize,
        };

        internal static ImageViewer OpenImage(FilePath fileName)
        {
            IDockingService dockingService = DependencyFactory.Resolve<IDockingService>();
            var child = dockingService.Documents.OfType<ImageViewer>()
                .SingleOrDefault(e => e.FileName == fileName);
            if (child != null)
            {
                child.Show();
                return child;
            }

            ImageViewer doc = new ImageViewer
            {
                Text = Path.GetFileName(fileName),
                TabText = Path.GetFileName(fileName),
                ToolTipText = fileName
            };

            doc.OpenFile(fileName);

            if (!Settings.Default.RecentFiles.Contains(fileName))
            {
                Settings.Default.RecentFiles.Add(fileName);
            }

            dockingService.ShowDockPanel(doc);
            return doc;
        }

        private ImageViewer()
        {
            AutoScroll = true;
            Controls.Add(_pictureBox);
            MouseWheel += ImageViewer_MouseWheel;
        }

        private void ImageViewer_MouseWheel(object sender, MouseEventArgs e)
        {
            if ((ModifierKeys & Keys.Control) == 0)
            {
                return;
            }

            double zoomFactor = e.Delta < 0 ? 1.0 / ConstZoomFactor : ConstZoomFactor;
            _currentZoom *= zoomFactor;
            if (_currentZoom > MaxZoom)
            {
                _currentZoom = MaxZoom;
            }
            else if (_currentZoom < MinZoom)
            {
                _currentZoom = MinZoom;
            }

            ShowCurrentImage();
        }

        private void ShowCurrentImage()
        {
            if (_pictureBox.Image != null)
            {
                _pictureBox.Image.Dispose();
            }

            int height = (int) (_originalImage.Height * _currentZoom);
            int width = (int) (_originalImage.Width * _currentZoom);
            _pictureBox.Image = _originalImage.ResizeImage(width, height);
        }

        protected override bool DocumentChanged { get; set; }

        public override void OpenFile(FilePath fileName)
        {
            base.OpenFile(fileName);

            using (Image image = new Bitmap(fileName))
            {
                _originalImage = new Bitmap(image);
            }

            if ((_originalImage.Size.Width < Size.Width / 4) ||
                (_originalImage.Size.Height < Size.Height / 4))
            {
                _currentZoom = 4.0;
            }

            ShowCurrentImage();
        }

        protected override void SaveFileInner()
        {
            // Cannot save currently
        }

        public override void Copy()
        {
            Clipboard.SetImage(_pictureBox.Image);
        }

        public override void Cut()
        {
            Clipboard.SetImage(_pictureBox.Image);
        }

        public override void Paste()
        {
            _pictureBox.Image = Clipboard.GetImage();
        }

        public override void Undo()
        {
        }

        public override void Redo()
        {
        }

        public override void SelectAll()
        {
        }

        protected override void ReloadFile()
        {
            double zoom = _currentZoom;
            OpenFile(FileName);
            _currentZoom = zoom;
            ShowCurrentImage();
        }

        protected override string GetPersistString()
        {
            return base.GetPersistString() + ";" + _currentZoom;
        }

        public override void PersistStringLoad(params string[] persistStrings)
        {
            base.PersistStringLoad(persistStrings);

            double zoom;
            if (!double.TryParse(persistStrings[2], out zoom))
            {
                return;
            }

            _currentZoom = zoom;
            ShowCurrentImage();
        }
    }
}