using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Markdig;

namespace RubikCube
{
    public partial class GAInfoForm : Form
    {
        public GAInfoForm()
        {
            InitializeComponent();
            LoadMarkdownContent();
        }

        private void LoadMarkdownContent()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream("OPERATORY_GA_KOMPLETNY.md");

                if (stream == null)
                {
                    webBrowser.DocumentText = "<html><body><h1>Error</h1><p>Could not load documentation resource.</p></body></html>";
                    return;
                }

                using var reader = new StreamReader(stream);
                string markdown = reader.ReadToEnd();

                var pipeline = new MarkdownPipelineBuilder()
                    .UseAdvancedExtensions()
                    .Build();
                string html = Markdown.ToHtml(markdown, pipeline);

                string fullHtml = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body {{
            font-family: 'Segoe UI', sans-serif;
            padding: 20px;
            line-height: 1.6;
            max-width: 1200px;
            margin: 0 auto;
        }}
        h1, h2, h3, h4 {{
            color: #333;
            margin-top: 24px;
        }}
        h1 {{
            border-bottom: 2px solid #4a90d9;
            padding-bottom: 10px;
        }}
        h2 {{
            border-bottom: 1px solid #ddd;
            padding-bottom: 8px;
        }}
        code {{
            background: #f4f4f4;
            padding: 2px 6px;
            border-radius: 3px;
            font-family: 'Consolas', 'Courier New', monospace;
        }}
        pre {{
            background: #f4f4f4;
            padding: 15px;
            overflow-x: auto;
            border-radius: 5px;
            border: 1px solid #ddd;
        }}
        pre code {{
            background: none;
            padding: 0;
        }}
        table {{
            border-collapse: collapse;
            width: 100%;
            margin: 15px 0;
        }}
        th, td {{
            border: 1px solid #ddd;
            padding: 10px;
            text-align: left;
        }}
        th {{
            background: #f0f0f0;
            font-weight: 600;
        }}
        tr:nth-child(even) {{
            background: #fafafa;
        }}
        blockquote {{
            border-left: 4px solid #4a90d9;
            margin: 15px 0;
            padding: 10px 20px;
            background: #f9f9f9;
        }}
        a {{
            color: #4a90d9;
            text-decoration: none;
        }}
        a:hover {{
            text-decoration: underline;
        }}
        hr {{
            border: none;
            border-top: 1px solid #ddd;
            margin: 30px 0;
        }}
    </style>
</head>
<body>{html}</body>
</html>";

                webBrowser.DocumentText = fullHtml;
            }
            catch (Exception ex)
            {
                webBrowser.DocumentText = $"<html><body><h1>Error</h1><p>Failed to load documentation: {ex.Message}</p></body></html>";
            }
        }
    }
}
