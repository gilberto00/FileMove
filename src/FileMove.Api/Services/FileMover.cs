using System.Collections.ObjectModel;
using FileMove.Api.Models;

namespace FileMove.Api.Services;

public class FileMover : IFileMover
{
    public FileMoveSummary MoveFiles(MoveFilesRequest request)
    {
        if (!Directory.Exists(request.SearchDirectory))
        {
            throw new DirectoryNotFoundException($"A pasta de busca '{request.SearchDirectory}' não existe.");
        }

        try
        {
            Directory.CreateDirectory(request.DestinationDirectory);
        }
        catch (Exception ex)
        {
            throw new IOException($"Não foi possível preparar a pasta de destino: {ex.Message}", ex);
        }

        string[] files;

        try
        {
            files = Directory.GetFiles(request.SearchDirectory, "*", SearchOption.AllDirectories);
        }
        catch (Exception ex)
        {
            throw new IOException($"Não foi possível listar os arquivos: {ex.Message}", ex);
        }

        var failures = new List<FileMoveFailure>();
        var movedCount = 0;

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
                movedCount++;
            }
            catch (Exception ex)
            {
                failures.Add(new FileMoveFailure(sourcePath, destinationPath, ex.Message));
            }
        }

        return new FileMoveSummary(
            TotalFiles: files.Length,
            MovedFiles: movedCount,
            FailedFiles: failures.Count,
            Failures: new ReadOnlyCollection<FileMoveFailure>(failures));
    }
}
