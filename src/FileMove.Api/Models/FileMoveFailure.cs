namespace FileMove.Api.Models;

public record FileMoveFailure(string Source, string Destination, string Error);
