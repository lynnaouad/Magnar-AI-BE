using Magnar.AI.Application.Dto.Schema;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Application.Interfaces.Managers;

namespace Magnar.AI.Application.Features.DatabaseSchema.Queries
{
    public sealed record GetTablesQuery() : IRequest<Result<IEnumerable<TableDto>>>;

    public class GetTablesQueryHandler : IRequestHandler<GetTablesQuery, Result<IEnumerable<TableDto>>>
    {
        #region Members
        private readonly ISchemaManager schemaManager;
        private readonly IUnitOfWork unitOfWork;
        #endregion

        #region Constructor
        public GetTablesQueryHandler(ISchemaManager schemaManager, IUnitOfWork unitOfWork)
        {
            this.schemaManager = schemaManager;
            this.unitOfWork = unitOfWork;
        }
        #endregion

        public async Task<Result<IEnumerable<TableDto>>> Handle(GetTablesQuery request, CancellationToken cancellationToken)
        {
            var defaultConnection = await unitOfWork.ConnectionRepository.FirstOrDefaultAsync(x => x.IsDefault, false, cancellationToken);
            if (defaultConnection is null || defaultConnection.Provider != ProviderTypes.SqlServer)
            {
                return Result<IEnumerable<TableDto>>.CreateFailure([new(Constants.Errors.NoDefaultConnectionConfigured)]);
            }

            return await schemaManager.GetTablesAsync(cancellationToken);
        }
    }
}
