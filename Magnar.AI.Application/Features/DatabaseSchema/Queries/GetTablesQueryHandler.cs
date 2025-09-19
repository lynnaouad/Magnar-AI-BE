using Magnar.AI.Application.Dto.Providers;
using Magnar.AI.Application.Dto.Schema;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Application.Interfaces.Managers;

namespace Magnar.AI.Application.Features.DatabaseSchema.Queries
{
    public sealed record GetTablesQuery(ProviderDto Provider) : IRequest<Result<IEnumerable<TableDto>>>;

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
            var connection = new SqlServerProviderDetailsDto()
            {
                InstanceName = request.Provider.Details.SqlServerConfiguration.InstanceName,
                DatabaseName = request.Provider.Details.SqlServerConfiguration.DatabaseName,
                Username = request.Provider.Details.SqlServerConfiguration.Username,
                Password = request.Provider.Details.SqlServerConfiguration.Password,
            };

            return await schemaManager.LoadTablesFromDatabaseAsync(connection, cancellationToken);
        }
    }
}
