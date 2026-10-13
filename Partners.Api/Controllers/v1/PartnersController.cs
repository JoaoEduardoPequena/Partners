using Partners.Application.DTOs;
using Partners.Application.UseCases.Partners.Commands.CreatePartnerCommand;
using Partners.Application.UseCases.Partners.Commands.UpdatePartnerCommand;
using Partners.Application.UseCases.Partners.Queries.GetPartnerByIdQuery;
using Partners.Application.UseCases.Partners.Queries.GetPartnersQuery;
using Partners.Application.Wrapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Partners.Api.Controllers.v1
{
    /// <summary>
    /// Clientes e fornecedores da empresa (um registo por NIF).
    /// </summary>
    [Tags("Partners")]
    [Route("api/partners")]
    public class PartnersController : BaseApiController
    {
        private readonly ISender _sender;

        public PartnersController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>Cria um cliente, fornecedor ou entidade com ambos os papéis.</summary>
        /// <response code="201">Criado. O cabeçalho Location aponta para GET /api/partners/{id}.</response>
        /// <response code="400">Dados inválidos.</response>
        /// <response code="409">Já existe uma entidade com o mesmo NIF.</response>
        [HttpPost]
        [ProducesResponseType(typeof(PartnerDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromBody] CreatePartnerCommand command, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        /// <summary>Devolve uma entidade pelo Id, incluindo entidades inactivas.</summary>
        /// <response code="200">Encontrada.</response>
        /// <response code="400">Id malformado (não é um GUID).</response>
        /// <response code="404">Id inexistente.</response>
        // Sem restrição ":guid" na rota: um Id malformado chega ao model binding e devolve 400
        // (ValidationProblemDetails), em vez de um 404 sem corpo.
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(PartnerDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetPartnerByIdQuery(id), cancellationToken);
            return Ok(result);
        }

        /// <summary>Lista paginada, com pesquisa por nome/NIF, filtro por tipo e inactivos opcionais.</summary>
        /// <response code="200">Página de resultados.</response>
        /// <response code="400">Parâmetros inválidos (Page, PageSize, Type, ...).</response>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<PartnerDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAll([FromQuery] GetPartnersQuery query, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>Actualiza uma entidade, exigindo a Version obtida na última leitura.</summary>
        /// <response code="200">Actualizada; a resposta inclui a nova Version.</response>
        /// <response code="400">Id, campos ou Version malformados.</response>
        /// <response code="404">Id inexistente.</response>
        /// <response code="409">NIF pertencente a outra entidade ou Version desactualizada.</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(PartnerDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePartnerRequest request, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(UpdatePartnerCommand.From(id, request), cancellationToken);
            return Ok(result);
        }
    }
}
