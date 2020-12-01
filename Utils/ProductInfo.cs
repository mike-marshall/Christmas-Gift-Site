using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Resources;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PolarExpress3.Utils
{
    public class ProductImage
    {
        public string Base64Data { get; set; }
        public byte[] RawData { get; set; }
        public string MimeType { get; set; }
    }
    public class ProductInfo
    {
        private const string imageURLTemplate =
            "https://ws-na.amazon-adsystem.com/widgets/q?_encoding=UTF8&MarketPlace=US&ASIN={0}&ServiceVersion=20070822&ID=AsinImage&WS=1&Format=_SL250_";
        public static async Task<ProductImage> GetProductThumbnailBase64Async(string url)
        {

            string mimeType = string.Empty;
            string result = string.Empty;
            if (!String.IsNullOrWhiteSpace(url) && url.Contains("amazon.com", StringComparison.InvariantCultureIgnoreCase))
            {
                mimeType = "image/jpeg";

                Regex regEx = new Regex(@"dp\/(?<ID>[0-9A-Z]+)");

                Match match = regEx.Match(url);

                string asin = match.Groups["ID"].Value;

                string imageUrl = String.Format(imageURLTemplate, asin);

                HttpClient client = new HttpClient();
                HttpResponseMessage msg = await client.GetAsync(imageUrl);

                if (msg.IsSuccessStatusCode)
                {
                    byte[] bytes = await msg.Content.ReadAsByteArrayAsync();
                    result = ImgUtils.CropToBase64Circle(bytes);
                }
            }
            else
            {
                result = Resources.Strings.DefaultGiftIcon;
                mimeType = "image/png";
            }

            return new ProductImage { Base64Data = result, MimeType = mimeType };
        }

        public static async Task<ProductImage> GetProductThumbnailAsync(string url)
        {
            string mimeType = String.Empty;
            byte[] result = null;
            if (url.Contains("amazon.com", StringComparison.InvariantCultureIgnoreCase))
            {
                Regex regEx = new Regex(@"dp\/(?<ID>[0-9A-Z]+)");

                Match match = regEx.Match(url);

                string asin = match.Groups["ID"].Value;

                string imageUrl = String.Format(imageURLTemplate, asin);

                HttpClient client = new HttpClient();
                HttpResponseMessage msg = await client.GetAsync(imageUrl);

                if (msg.IsSuccessStatusCode)
                {
                    result = await msg.Content.ReadAsByteArrayAsync();                
                }
            }
            else
            {

            }

            return new ProductImage { RawData = result, MimeType = mimeType };
        }
    }
}
