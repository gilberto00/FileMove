using FileMove.Api.Models;

namespace FileMove.Api.Services;

public interface IFileMover
{
    FileMoveSummary MoveFiles(MoveFilesRequest request);
}
