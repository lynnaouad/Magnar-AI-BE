using Magnar.AI.Application.Dto.Schema;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Application.Interfaces.Managers;

namespace Magnar.AI.Application.Features.DatabaseSchema.Queries
{
    public sealed record GetTableInfoQuery(string Schema, string TableName) : IRequest<Result<TableInfoDto>>;

    public class GetTableInfoQueryHandler : IRequestHandler<GetTableInfoQuery, Result<TableInfoDto>>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        private readonly ISchemaManager schemaManager;
        #endregion

        #region Constructor
        public GetTableInfoQueryHandler(ISchemaManager schemaManager, IUnitOfWork unitOfWork)
        {
            this.schemaManager = schemaManager;
            this.unitOfWork = unitOfWork;
        }
        #endregion

        public async Task<Result<TableInfoDto>> Handle(GetTableInfoQuery request, CancellationToken cancellationToken)
        {
            var defaultConnection = await unitOfWork.ConnectionRepository.FirstOrDefaultAsync(x => x.IsDefault, false, cancellationToken);
            if (defaultConnection is null || defaultConnection.Provider != ProviderTypes.SqlServer)
            {
                return Result<TableInfoDto>.CreateFailure([new(Constants.Errors.NoDefaultConnectionConfigured)]);
            }

            return await schemaManager.GetTableInfoAsync(request.Schema, request.TableName, cancellationToken);
        }
    }
}
