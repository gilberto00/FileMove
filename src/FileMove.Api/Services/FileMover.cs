using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using FileMove.Api.Models;

namespace FileMove.Api.Services;

public class FileMover : IFileMover
{
    public FileMoveSummary MoveFiles(MoveFilesRequest request)
    {
        var searchDirectory = NormalizeDirectoryPath(request.SearchDirectory);
        var destinationDirectory = NormalizeDirectoryPath(request.DestinationDirectory);

        if (!Directory.Exists(searchDirectory))
        {
            throw new DirectoryNotFoundException($"A pasta de busca '{searchDirectory}' não existe.");
        }

        try
        {
            EnsureDirectoriesAreDistinct(searchDirectory, destinationDirectory);
        }
        catch (IOException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new IOException($"Não foi possível validar as pastas informadas: {ex.Message}", ex);
        }

        try
        {
            Directory.CreateDirectory(destinationDirectory);
        }
        catch (Exception ex)
        {
            throw new IOException($"Não foi possível preparar a pasta de destino: {ex.Message}", ex);
        }

        IEnumerable<string> files;

        try
        {
            files = Directory.EnumerateFiles(searchDirectory, "*", SearchOption.AllDirectories);
        }
        catch (Exception ex)
        {
            throw new IOException($"Não foi possível listar os arquivos: {ex.Message}", ex);
        }

        var failures = new List<FileMoveFailure>();
        var movedCount = 0;

        foreach (var sourcePath in files)
        {
            var fileName = Path.GetFileName(sourcePath);
            if (string.IsNullOrEmpty(fileName))
            {
                continue;
            }

            var destinationPath = BuildUniqueDestinationPath(destinationDirectory, fileName);

            try
            {
                File.Move(sourcePath, destinationPath);
                movedCount++;
            }
            catch (Exception ex)
            {
                failures.Add(new FileMoveFailure(sourcePath, destinationPath, ex.Message));
            }
        }

        return new FileMoveSummary(
            TotalFiles: movedCount + failures.Count,
            MovedFiles: movedCount,
            FailedFiles: failures.Count,
            Failures: new ReadOnlyCollection<FileMoveFailure>(failures));
    }

    private static string BuildUniqueDestinationPath(string destinationDirectory, string fileName)
    {
        var candidate = Path.Combine(destinationDirectory, fileName);

        if (!File.Exists(candidate))
        {
            return candidate;
        }

        var name = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        var counter = 1;

        while (true)
        {
            var uniqueFileName = string.IsNullOrEmpty(extension)
                ? $"{name} ({counter})"
                : $"{name} ({counter}){extension}";

            candidate = Path.Combine(destinationDirectory, uniqueFileName);

            if (!File.Exists(candidate))
            {
                return candidate;
            }

            counter++;
        }
    }

    private static void EnsureDirectoriesAreDistinct(string searchDirectory, string destinationDirectory)
    {
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        var searchFullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(searchDirectory));
        var destinationFullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(destinationDirectory));

        if (string.Equals(searchFullPath, destinationFullPath, comparison))
        {
            throw new IOException("A pasta de destino deve ser diferente da pasta de busca.");
        }
    }

    private static string NormalizeDirectoryPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        var trimmed = path.Trim();

        if (Path.DirectorySeparatorChar == '\\')
        {
            trimmed = trimmed.Replace('/', Path.DirectorySeparatorChar);
        }
        else
        {
            trimmed = trimmed.Replace('\\', Path.DirectorySeparatorChar);
        }

        var builder = new StringBuilder(trimmed.Length);
        var previousWasSeparator = false;
        var startIndex = 0;
        var preserveUncPrefix = trimmed.StartsWith("\\\\", StringComparison.Ordinal);

        if (preserveUncPrefix)
        {
            builder.Append("\\\\");
            startIndex = 2;
        }

        for (var i = startIndex; i < trimmed.Length; i++)
        {
            var current = trimmed[i];
            if (current == Path.DirectorySeparatorChar)
            {
                if (!previousWasSeparator)
                {
                    builder.Append(current);
                    previousWasSeparator = true;
                }
            }
            else
            {
                builder.Append(current);
                previousWasSeparator = false;
            }
        }

        return builder.ToString();
    }
}
