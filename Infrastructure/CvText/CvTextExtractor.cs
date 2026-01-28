using DocumentFormat.OpenXml.Packaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UglyToad.PdfPig;

namespace Infrastructure.CvText
{
    public sealed class CvTextExtractor
    {
        public async Task<string> ExtractAsync(Stream fileStream, string fileName, CancellationToken ct)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();

            if (ext == ".pdf")
                return ExtractPdf(fileStream);

            if (ext == ".docx")
                return ExtractDocx(fileStream);

            if (ext == ".txt")
            {
                using var reader = new StreamReader(fileStream);
                return await reader.ReadToEndAsync(ct);
            }

            throw new InvalidOperationException($"Unsupported file type: {ext}. Use PDF/DOCX/TXT.");
        }

        private static string ExtractPdf(Stream s)
        {
            using var doc = PdfDocument.Open(s);
            var parts = new List<string>(doc.NumberOfPages);
            foreach (var page in doc.GetPages())
                parts.Add(page.Text);
            return string.Join("\n\n", parts);
        }

        private static string ExtractDocx(Stream s)
        {
            using var doc = WordprocessingDocument.Open(s, false);
            var text = doc.MainDocumentPart?.Document?.Body?.InnerText ?? "";
            return text;
        }
    }
}
