using System.ComponentModel.DataAnnotations;

namespace FileMove.Api.Models;

public class MoveFilesRequest
{
    [Required(ErrorMessage = "A pasta de busca é obrigatória.")]
    [Display(Name = "Pasta de busca")]
    public required string SearchDirectory { get; init; }

    [Required(ErrorMessage = "A pasta de destino é obrigatória.")]
    [Display(Name = "Pasta de destino")]
    public required string DestinationDirectory { get; init; }
}
