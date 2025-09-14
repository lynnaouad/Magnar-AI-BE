using Magnar.AI.Application.Dto.Schema;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Application.Interfaces.Managers;

namespace Magnar.AI.Application.Features.DatabaseSchema.Queries
{
    public sealed record GetSelectedTablesQuery() : IRequest<Result<IEnumerable<SelectedTableBlock>>>;

    public class GetSelectedTablesQueryHandler : IRequestHandler<GetSelectedTablesQuery, Result<IEnumerable<SelectedTableBlock>>>
    {
        #region Members
        private readonly IAnnotationFileManager annotationFileManager;
        private readonly IUnitOfWork unitOfWork;
        #endregion

        #region Constructor
        public GetSelectedTablesQueryHandler(IAnnotationFileManager annotationFileManager, IUnitOfWork unitOfWork)
        {
            this.annotationFileManager = annotationFileManager;
            this.unitOfWork = unitOfWork;
        }
        #endregion

        public async Task<Result<IEnumerable<SelectedTableBlock>>> Handle(GetSelectedTablesQuery request, CancellationToken cancellationToken)
        {
            var defaultConnection = await unitOfWork.ConnectionRepository.FirstOrDefaultAsync(x => x.IsDefault, false, cancellationToken);
            if (defaultConnection is null || defaultConnection.Provider != ProviderTypes.SqlServer)
            {
                return Result<IEnumerable<SelectedTableBlock>>.CreateFailure([new(Constants.Errors.NoDefaultConnectionConfigured)]);
            }

            var result = await annotationFileManager.ReadAllBlocksAsync(defaultConnection.Id);

            return Result<IEnumerable<SelectedTableBlock>>.CreateSuccess(result);
        }
    }
}
