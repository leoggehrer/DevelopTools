using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DevelopCommon.Extensions;

namespace MdTranslator.ConApp
{
    class MdTranslator
    {
        private static string MdCommentBegin => "```csharp (";
        private static string MdCommentEnd => "```";
        private static string MdEmbeddedBegin => $"{MdCommentBegin}";
        private static string MdEmbeddedEnd => $"{MdCommentEnd}";
        private static string[] EmbeddedTags => new[]
        {
            MdEmbeddedBegin,
            MdEmbeddedEnd
        };
        private class TagParameters
        {
            public string Type { get; set; }
            public string Begin { get; set; }
            public string File { get; set; }
            public string StartTag { get; set; }
            public string EndTag { get; set; }
            public string End { get; set; }
        }
        public static void ReplaceDocumenTags(string filePath, string refFilePath)
        {
            if (File.Exists(filePath))
            {
                string sourceContent = File.ReadAllText(filePath, Encoding.Default);
                string replaceContent = ReplaceDocumentTags(sourceContent, refFilePath);

                if (sourceContent.Equals(replaceContent) == false)
                {
                    File.WriteAllText(filePath, replaceContent, Encoding.Default);
                }
            }
        }
        private static string ReplaceDocumentTags(string text, string refFilePath)
        {
            int textStartPos = 0;
            StringBuilder result = new StringBuilder();

            foreach (var tag in text.GetAllTags(EmbeddedTags))
            {
                var tagLines = tag.GetFullLines();
                var startTagParams = tagLines.First().Partialstring("(", ")");
                var startParams = Newtonsoft.Json.JsonConvert.DeserializeObject<TagParameters>(startTagParams);

                result.Append(text.Partialstring(textStartPos, tag.StartTagIndex - 1));
                if (tagLines.Any())
                {
                    result.Append(tagLines.First());
                }
                if (startParams.Begin.HasContent())
                {
                    result.Append(startParams.Begin);
                }

                try
                {
                    if (startParams.Type.HasContent()
                        && startParams.Type.Equals("FileRef", StringComparison.CurrentCultureIgnoreCase)
                        && startParams.File.HasContent())
                    {
                        var files = Directory.GetFiles(refFilePath, "*.*", SearchOption.AllDirectories)
                                             .Select(f => f.ToLower().Replace(@"\", "/"));
                        var file = files.FirstOrDefault(f => f.EndsWith(startParams.File.ToLower()));

                        if (file != null)
                        {
                            string fileContent = File.ReadAllText(file, Encoding.Default);
                            var embeddedTag = fileContent.GetAllTags(startParams.StartTag, startParams.EndTag).FirstOrDefault();

                            if (embeddedTag != null)
                            {
                                result.Append(embeddedTag.GetInnerText());
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{MethodBase.GetCurrentMethod().Name}: {ex.Message}");
                }
                textStartPos = tag.EndTagIndex;

                if (tagLines.Any())
                {
                    result.Append(tagLines.Last());
                    textStartPos += tagLines.Last().Length;
                }
            }
            if (textStartPos < text.Length - 1)
            {
                result.Append(text.Partialstring(textStartPos, text.Length - 1));
            }
            return result.ToString();
        }
    }
}
