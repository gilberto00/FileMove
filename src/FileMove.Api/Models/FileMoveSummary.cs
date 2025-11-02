using System.Collections.ObjectModel;

namespace FileMove.Api.Models;

public record FileMoveSummary(int TotalFiles, int MovedFiles, int FailedFiles, ReadOnlyCollection<FileMoveFailure> Failures);
