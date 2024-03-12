using ImageMagick;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Linq;

namespace CaptchaBuster
{
    public class Captcha
    {
        public MagickImage image { get; private set; }
        private List<MagickImage> letters = new List<MagickImage>();
        private bool devmode = false;

        public Captcha(string imagePath)
        {
            this.image = new MagickImage(imagePath);
        }

        public Captcha(byte[] byteArray)
        {
            this.image = new MagickImage(byteArray);
        }

        public void Monochrome(int thresholdPercentage)
        {
            this.image.Threshold(new Percentage(thresholdPercentage));

            if (devmode)
            {
                this.image.Write("D:\\Development\\CaptchaBuster\\CaptchaBuster\\CaptchaBuster\\monochrome_captcha.jpg");
            }
        }

        /// <summary>
        /// Finds individual letters from the original captcha. If devmode is true, will save the letters as individual images.
        /// </summary>
        /// <param name="maximumLetterLength"></param>
        /// <returns>A list of MagickImages</returns>
        public List<MagickImage> FindLetters(int maximumLetterLength)
        {
            List<(int, int)> letterBoxes = FindLetterBoxes(this.image, maximumLetterLength);

            if (letterBoxes.Count <= 0) { return null; }

            int counter = 0;

            foreach (var letterBox in letterBoxes)
            {
                //Console.WriteLine(letterBox);
                MagickImage copy = (MagickImage)this.image.Clone();

                int offset = letterBox.Item1;
                int width = letterBox.Item2 - letterBox.Item1;
                MagickGeometry geometry = new MagickGeometry(offset, 0, width, copy.Height);

                copy.Crop(geometry);

                this.letters.Add(copy);

                if (devmode) { copy.Write($"D:\\Development\\CaptchaBuster\\CaptchaBuster\\CaptchaBuster\\letter_{counter}.jpg"); }
                
                counter++;
            }

            // This is where we will trim the whitespace around the letters

            //foreach(MagickImage letter in letters)
            //{
            //    var diff = CutTheWhite(letter);
            //    diff.Write($"D:\\Development\\CaptchaBuster\\CaptchaBuster\\CaptchaBuster\\diff_captcha.jpg");
            //}

            return this.letters;
        }

        public List<(int, int)> FindLetterBoxes(MagickImage img, int maxLength)
        {
            var pixels = img.GetPixels();
            var imageColumns = Enumerable.Range(0, img.Width).Select(x => Enumerable.Range(0, img.Height).Select(y => pixels.GetPixel(x, y).ToColor().R).ToList()).ToList();
            var imageCode = imageColumns.Select(column => column.Any(pixel => pixel == 0) ? 1 : 0).ToList();
            var xPoints = imageCode.Select((s, d) => new { s, d }).Where(item => item.s == 1).Select(item => item.d).ToList();
            var xCoords = xPoints.Where(x => !xPoints.Contains(x - 1) || !xPoints.Contains(x + 1)).ToList();

            if (xCoords.Count % 2 != 0)
            {
                xCoords.Insert(1, xCoords[0]);
            }

            var letterBoxes = new List<(int, int)>();
            for (int i = 0; i < xCoords.Count; i += 2)
            {
                var start = xCoords[i];
                var end = Math.Min(xCoords[i + 1] + 1, img.Width - 1);

                if (end - start <= maxLength)
                {
                    letterBoxes.Add((start, end));
                }
                else
                {
                    var twoLetters = Enumerable.Range(start + 5, end - start - 10).ToDictionary(k => k, k => imageColumns[k].Count(v => v == 0));
                    var divider = twoLetters.OrderBy(item => item.Value).First().Key + 5;
                    letterBoxes.AddRange(new[] { (start, start + divider), (start + divider + 1, end) });
                }
            }

            return letterBoxes;
        }

        private MagickImage CutTheWhite(MagickImage image)
        {
            MagickImage whiteImage = new MagickImage(MagickColors.White, image.Width, image.Height);
            MagickImage diffImage = new MagickImage();

            //image.Compose = CompositeOperator.Difference;
            image.Compare(whiteImage, ErrorMetric.Absolute, diffImage);

            //MagickErrorInfo diff = (MagickErrorInfo)image.Compare(whiteImage, CompositeOperator.Difference);

            ////Console.WriteLine(diff);
            //var bbox = image.BoundingBox;

            //image.Crop(bbox);
            whiteImage.Write($"D:\\Development\\CaptchaBuster\\CaptchaBuster\\CaptchaBuster\\white_captcha.jpg");
            diffImage.Write($"D:\\Development\\CaptchaBuster\\CaptchaBuster\\CaptchaBuster\\diff2_captcha.jpg");

            var bbox = image.BoundingBox;
            image.Crop(bbox);

            return image;
        }

        /// <summary>
        /// Transforms separated letters into pseudo binary
        /// </summary>
        private void SaveLetters()
        {
            foreach (MagickImage letter in letters)
            {
                // get data from each pixel in a list
                var pixelData = letter.GetPixels().GetValues();

                // create a binary string "1 if pixel data is 0, 0 if anything else"
                var binaryString = String.Empty;

                foreach (var pixel in pixelData)
                {
                    if (pixel == 65535)
                    {
                        binaryString += "0";
                    }
                    else
                    {
                        binaryString += "1";
                    }
                }

                // encode to a byte-string
                byte[] byteArray = Utils.ConvertBinaryStringToBytes(binaryString);

                byte[] compressedByteArray = Utils.CompressBytes(byteArray);

                //string[] path = Directory.GetFiles("D:\\Development\\CaptchaBuster\\CaptchaBuster\\CaptchaBuster\\Alphabet");
               
                //foreach (var file in path)
                //{
                //    // Converts 'A' to a byte string
                //    var jsonLetter = File.ReadAllText(file);
                //    var byteString = jsonLetter.Substring(1, jsonLetter.Length - 2);
                //    var conversion = Utils.ConvertPythonByteStringToBytes(byteString);
                //    var contains = Utils.SequenceContains(conversion, compressedByteArray);
                //}

                

            }
        }

        private void Translate()
        {
            // convert letter python bytestring into c# byte string

            var jsonLetter = File.ReadAllText("D:\\Development\\CaptchaBuster\\CaptchaBuster\\CaptchaBuster\\Alphabet\\A.json");
            var byteString = jsonLetter.Substring(1, jsonLetter.Length - 2);


            var conversion = Utils.ConvertPythonByteStringToBytes(byteString);

            //foreach (var b in conversion)
            //{
            //    Console.Write(b);
            //}

            //var decompressedBytes = Utils.DecompressZlibBytes(conversion);
            //Console.WriteLine(decompressedBytes);

            //var decompressedBytestring = Encoding.UTF8.GetString(conversion);
            //Console.WriteLine(decompressedBytestring);

            //var json = JsonSerializer.Deserialize<string>(jsonLetter);
            //Console.WriteLine(json);
        }

        public void Solve(bool devmode = false, int thresholdPercentage=2, int maximumLetterLength=33)
        {
            // devmode
            if (devmode) { this.devmode = true; }

            // Make the image monochrome
            this.Monochrome(thresholdPercentage);

            // Find letters
            List<MagickImage> letters = this.FindLetters(maximumLetterLength);

            //this.CutTheWhite();

            // Save letters
            //this.SaveLetters();

            // Translate
            //this.Translate();

            // If solution is not solved, log things and save any training data here

            // return solution
            if (devmode) { this.image.Write("D:\\Development\\CaptchaBuster\\CaptchaBuster\\CaptchaBuster\\processed_captcha.jpg"); }

            return letters;
        }
    }
}
