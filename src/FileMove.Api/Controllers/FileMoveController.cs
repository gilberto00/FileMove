using FileMove.Api.Models;
using FileMove.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FileMove.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileMoveController : ControllerBase
{
    private readonly IFileMover _fileMover;
    private readonly ILogger<FileMoveController> _logger;

    public FileMoveController(IFileMover fileMover, ILogger<FileMoveController> logger)
    {
        _fileMover = fileMover;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileMoveSummary))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult<FileMoveSummary> MoveFiles([FromBody] MoveFilesRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var result = _fileMover.MoveFiles(request);
            return Ok(result);
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.LogWarning(ex, "Diretório de busca não encontrado: {SearchDirectory}", request.SearchDirectory);
            return BadRequest(new { message = ex.Message });
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Erro de E/S ao mover arquivos de {SearchDirectory} para {DestinationDirectory}", request.SearchDirectory, request.DestinationDirectory);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao mover arquivos de {SearchDirectory} para {DestinationDirectory}", request.SearchDirectory, request.DestinationDirectory);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Ocorreu um erro inesperado ao mover os arquivos." });
        }
    }
}
