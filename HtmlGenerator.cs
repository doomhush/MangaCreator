using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace MangaCreator {
    class Generator {
        private string savePath_;
        private string title_;
        private string imageFolderName_ = "scaled-images";
        private string htmlFolderName_ = "html";
        private string author_;
        private int deviceWith_ = 800;
        private int deviceHeight_ = 1280;

        private string opfFormat = @"<package version=""2.0"" xmlns=""http://www.idpf.org/2007/opf"" unique-identifier=""{{1bb3e6ef-17a2-4904-9afb-88269a0b608f}}"">
    <metadata xmlns:opf=""http://www.idpf.org/2007/opf"" xmlns:dc=""http://purl.org/dc/elements/1.1/"">
        <meta content=""comic"" name=""book-type""/>
        <meta content=""true"" name=""zero-gutter""/>
        <meta content=""true"" name=""zero-margin""/>
        <meta content=""true"" name=""fixed-layout""/>
        <meta content=""KindleComicGenerator-0.1"" name=""generator""/>
        <dc:title>{0}</dc:title>
        <dc:language>zh</dc:language>
        <dc:creator>{1}</dc:creator>
        <dc:publisher/>
        <meta content=""portrait"" name=""orientation-lock""/>
        <meta content=""horizontal-lr"" name=""primary-writing-mode""/>
        <meta content=""{2}x{3}"" name=""original-resolution""/>
        <meta content=""false"" name=""region-mag""/>
        <meta content=""cover-image"" name=""cover""/>
        <dc:source>KC2/1.160/cf63ffb/win</dc:source>
    </metadata>
    <manifest>
        <item href=""toc.ncx"" id=""ncx"" media-type=""application/x-dtbncx+xml""/>
        <item href=""cover-image.jpg"" id=""cover-image"" media-type=""image/jpg""/>
{4}</manifest>
    <spine toc=""ncx"">
{5}</spine>
</package>
";
        private string opfItem;
        private string opfItemref;
        public string opfItemFormat = "\t\t<item href=\"{0}/{1}.html\" id=\"item-{2}\" media-type=\"application/xhtml+xml\"/>\r\n";
        public string opfItemrefFormat = "\t\t<itemref idref=\"item-{0}\" linear=\"yes\"/>\r\n";

        private string tocFormat = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE ncx PUBLIC ""-//NISO//DTD ncx 2005-1//EN"" ""http://www.daisy.org/z3986/2005/ncx-2005-1.dtd"">
<ncx version=""2005-1"" xmlns=""http://www.daisy.org/z3986/2005/ncx/"" xml:lang=""en-US"">
    <head>
        <meta content="""" name=""dtb:uid""/>
        <meta content="""" name=""dtb:depth""/>
        <meta content=""0"" name=""dtb:totalPageCount""/>
        <meta content=""0"" name=""dtb:maxPageNumber""/>
        <meta content=""true"" name=""generated""/>
    </head>
    <docTitle>
        <text/>
    </docTitle>
    <navMap>{0}
    </navMap>
</ncx>
";
        private string navPointFormat = @"
        <navPoint playOrder=""{0}"" id=""toc-{0}"">
            <navLabel>
                <text>{0}</text>
            </navLabel>
            <content src=""{1}/{0}.html""/>
        </navPoint>";
        private string navPoint;

        public Generator(string savePath, string title, string author, int deviceWith = 800, int deviceHeight = 1280) {
            savePath_ = savePath;
            title_ = title;
            author_ = author;
            deviceWith_ = deviceWith;
            deviceHeight_ = deviceHeight;
        }

        public bool HtmlGenerator(string imagePath, int page) {
            try {
                string path = Path.Combine(savePath_, title_);
                Directory.CreateDirectory(path);
                Directory.CreateDirectory(Path.Combine(path, htmlFolderName_));
                Directory.CreateDirectory(Path.Combine(path, htmlFolderName_, imageFolderName_));

                if (!File.Exists(imagePath)) {
                    Console.WriteLine("图片{0}不存在", imagePath);
                    return false;
                }

                int margin = 0;
                using (Image pic = Image.FromFile(imagePath)) {
                
                    // 第一张图片当封面
                    if (page == 1) {
                        // 复制图片到图片文件夹
                        pic.Save(Path.Combine(path, "cover-image.jpg"));
                    }
                    // 复制图片到图片文件夹
                    string newImageName = String.Format("{0}{1}", page, Path.GetExtension(imagePath));
                    string imageCopyPath = Path.Combine(path, htmlFolderName_, imageFolderName_, newImageName);

                    // 如果图片是横的着就竖过来
                    if (pic.Width > pic.Height) {
                        pic.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    }

                    using (Bitmap resizedImage = ResizeImage(pic, deviceWith_, deviceHeight_)) {
                        resizedImage.Save(imageCopyPath, ImageFormat.Jpeg);
                    }
                    // 计算图片的缩放系数，宽度铺满，计算高度
                    margin = (deviceHeight_ - pic.Height) / 2;
                }
                
                // 创建html文件
                string htmlName = String.Format("{0}.html", page);
                string htmlPath = Path.Combine(path, htmlFolderName_, htmlName);
                using (StreamWriter output = new StreamWriter(htmlPath)) {
                    output.WriteLine("<!DOCTYPE html>");
                    output.WriteLine("<html>");
                    output.WriteLine("\t<head>");
                    output.WriteLine("\t\t<meta content=\"KC2/1.160/cf63ffb/win\" name=\"generator\"/>");
                    output.WriteLine("\t\t<title>{0}</title>", page);
                    output.WriteLine("\t</head>");
                    output.WriteLine("\t<body>");
                    output.WriteLine("\t\t<div>");
                    output.WriteLine("\t\t\t<img style=\"width:{0}px;height:{1}px;margin-left:0px;margin-top:{2}px;margin-right:0px;margin-bottom:{3}px;\" src=\"{4}/{5}.jpg\" />", deviceWith_, deviceHeight_, margin, margin, imageFolderName_, page);
                    output.WriteLine("\t\t</div>");
                    output.WriteLine("\t</body>");
                    output.WriteLine("</html>");
                }

                opfItem += String.Format(opfItemFormat, htmlFolderName_, page, page+1);
                opfItemref += String.Format(opfItemrefFormat, page+1);

                navPoint += String.Format(navPointFormat, page, htmlFolderName_);
                return true;
            }catch(Exception e) {
                Console.WriteLine(e);
                return false;
            }
        }
        public bool OpfGenerator() {
            try {
                string opf = String.Format(opfFormat, title_, author_, deviceWith_, deviceHeight_, opfItem, opfItemref);
                string opfPath = Path.Combine(savePath_, title_, "content.opf");
                File.WriteAllText(opfPath, opf);
            } catch (Exception e) {
                Console.WriteLine(e);
                return false;
            }
            return true;
        }

        public bool TocGenerator() {
            try {
                string toc = String.Format(tocFormat, navPoint);
                string tocPath = Path.Combine(savePath_, title_, "toc.ncx");
                File.WriteAllText(tocPath, toc);
            } catch (Exception e) {
                Console.WriteLine(e);
                return false;
            }
            return true;
        }

        private Bitmap ResizeImage(Image image, int width, int height) {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);
            
            using (var graphics = Graphics.FromImage(destImage)) {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                using (var wrapMode = new ImageAttributes()) {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

    }
}
