using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using System.Text;
using UglyToad.PdfPig;
using DocumentFormat.OpenXml.Packaging;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Inicializar BD
using (var connection = new SqliteConnection("Data Source=chatbot.db"))
{
    connection.Open();
    var tableCmd = connection.CreateCommand();
    tableCmd.CommandText =
    @"CREATE TABLE IF NOT EXISTS Documents (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        Content TEXT
    );";
    tableCmd.ExecuteNonQuery();
}

// Endpoint: Subida de documentos (PDF, DOCX o texto plano)
app.MapPost("/upload", async (HttpRequest request) =>
{
    // Autenticación básica con "clave secreta"
    if (!request.Headers.ContainsKey("X-Admin-Key") ||
        request.Headers["X-Admin-Key"] != "clave_super_secreta")
    {
        return Results.Unauthorized();
    }

    if (!request.HasFormContentType) return Results.BadRequest("Debe ser multipart/form-data");

    var form = await request.ReadFormAsync();
    var file = form.Files["file"];
    var textContent = new StringBuilder();

    if (file != null && file.Length > 0)
    {
        using var stream = file.OpenReadStream();

        if (file.FileName.EndsWith(".pdf"))
        {
            using var pdf = PdfDocument.Open(stream);
            foreach (var page in pdf.GetPages())
            {
                textContent.AppendLine(page.Text);
            }
        }
        else if (file.FileName.EndsWith(".docx"))
        {
            using var doc = WordprocessingDocument.Open(stream, false);
            var body = doc.MainDocumentPart?.Document.Body;
            if (body != null)
                textContent.AppendLine(body.InnerText);
        }
        else if (file.FileName.EndsWith(".txt"))
        {
            using var reader = new StreamReader(stream);
            textContent.AppendLine(await reader.ReadToEndAsync());
        }
        else
        {
            return Results.BadRequest("Formato no soportado (usa PDF, DOCX o TXT)");
        }
    }
    else
    {
        return Results.BadRequest("Debe subir un archivo válido");
    }

    using (var connection = new SqliteConnection("Data Source=chatbot.db"))
    {
        connection.Open();
        var insert = connection.CreateCommand();
        insert.CommandText = "INSERT INTO Documents (Content) VALUES ($content)";
        insert.Parameters.AddWithValue("$content", textContent.ToString());
        insert.ExecuteNonQuery();
    }

    return Results.Ok("Documento almacenado con éxito");
});


// Endpoint: Preguntar
app.MapGet("/ask", (string question) =>
{
    string bestAnswer = "No encontré información relacionada.";

    using (var connection = new SqliteConnection("Data Source=chatbot.db"))
    {
        connection.Open();
        var select = connection.CreateCommand();
        select.CommandText = "SELECT Content FROM Documents";
        using var reader = select.ExecuteReader();

        while (reader.Read())
        {
            var content = reader.GetString(0);
            if (content.Contains(question, StringComparison.OrdinalIgnoreCase))
            {
                bestAnswer = content;
                break;
            }
        }
    }

    return Results.Ok(new { answer = bestAnswer });
});

app.Run();
