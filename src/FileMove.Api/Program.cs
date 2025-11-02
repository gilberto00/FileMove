using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapPost("/move-files", (MoveFilesRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.SearchDirectory))
    {
        return Results.BadRequest(new { message = "A pasta de busca é obrigatória." });
    }

    if (string.IsNullOrWhiteSpace(request.DestinationDirectory))
    {
        return Results.BadRequest(new { message = "A pasta de destino é obrigatória." });
    }

    if (!Directory.Exists(request.SearchDirectory))
    {
        return Results.BadRequest(new { message = $"A pasta de busca '{request.SearchDirectory}' não existe." });
    }

    try
    {
        Directory.CreateDirectory(request.DestinationDirectory);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { message = $"Não foi possível preparar a pasta de destino: {ex.Message}" });
    }

    string[] files;

    try
    {
        files = Directory.GetFiles(request.SearchDirectory, "*", SearchOption.AllDirectories);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { message = $"Não foi possível listar os arquivos: {ex.Message}" });
    }

    var moves = new List<FileMoveResult>();

    foreach (var sourcePath in files)
    {
        var relativePath = Path.GetRelativePath(request.SearchDirectory, sourcePath);
        var destinationPath = Path.Combine(request.DestinationDirectory, relativePath);

        try
        {
            var destinationFolder = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }

            File.Move(sourcePath, destinationPath, overwrite: true);
            moves.Add(new FileMoveResult(sourcePath, destinationPath, null));
        }
        catch (Exception ex)
        {
            moves.Add(new FileMoveResult(sourcePath, destinationPath, ex.Message));
        }
    }

    var movedCount = moves.Count(m => m.Error is null);
    var failed = moves.Where(m => m.Error is not null).ToArray();

    return Results.Ok(new
    {
        totalFiles = files.Length,
        movedFiles = movedCount,
        failedFiles = failed.Length,
        failures = failed
    });
});

app.Run();

record MoveFilesRequest(
    [property: Required] string SearchDirectory,
    [property: Required] string DestinationDirectory
);

record FileMoveResult(string Source, string Destination, string? Error);
