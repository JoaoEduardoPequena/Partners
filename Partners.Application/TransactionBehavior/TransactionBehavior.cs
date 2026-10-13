using MediatR;
using Partners.Persistence.DBContexts;

namespace Partners.Application.TransactionBehavior
{
    // Garante que um Command que grava via IGenericRepositoryAsync<T> (cada chamada com o seu próprio
    // SaveChangesAsync) fica todo dentro de uma única transação: se algo falhar a meio — validação,
    // NIF duplicado ou versão desactualizada — nada é gravado.
    public class TransactionBehavior<TRequest, TResponse>(AppDbContext context)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : class
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(next);

            // Só Commands escrevem dados — Queries seguem o padrão de nomenclatura do projeto e não
            // precisam de transação.
            if (!typeof(TRequest).Name.EndsWith("Command", StringComparison.Ordinal))
                return await next().ConfigureAwait(false);

            // Evita transação aninhada, caso um handler alguma vez chame outro Command por dentro.
            if (context.Database.CurrentTransaction is not null)
                return await next().ConfigureAwait(false);

            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var response = await next().ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return response;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }
    }
}
