using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;

namespace DownloadMultipleFileFromJSON
{
    public class BukuAudio
    {
        public string SKU { get; set; }

        public string Judul { get; set; }

        public string Deskripsi { get; set; }

        public string Tipe { get; set; }

        public string NamaTipe { get; set; }

        public string Harga { get; set; }

        public string Cover { get; set; }

        public string File { get; set; }

        public string Gratis { get; set; }

        public string Tanggal { get; set; }

        public JsonArray Bundle { get; set; }

        public List<string> BundleName { get; set; }

        public List<string> BundlePath { get; set; }
    }
}