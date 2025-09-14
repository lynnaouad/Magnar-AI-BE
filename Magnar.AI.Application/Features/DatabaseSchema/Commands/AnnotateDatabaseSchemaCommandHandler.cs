using Magnar.AI.Application.Dto.Schema;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Application.Interfaces.Managers;

namespace Magnar.AI.Application.Features.DatabaseSchema.Commands
{
    public sealed record AnnotateDatabaseSchemaCommand(TableAnnotationRequest TableAnnotation) : IRequest<Result>;

    public class AnnotateDatabaseSchemaCommandHandler : IRequestHandler<AnnotateDatabaseSchemaCommand, Result>
    {
        #region Members
        private readonly IAnnotationFileManager annotationFileManager;
         private readonly IUnitOfWork unitOfWork;
        #endregion

        #region Constructor
        public AnnotateDatabaseSchemaCommandHandler(IAnnotationFileManager annotationFileManager, IUnitOfWork unitOfWork)
        {
            this.annotationFileManager = annotationFileManager;
            this.unitOfWork = unitOfWork;
        }
        #endregion

        public async Task<Result> Handle(AnnotateDatabaseSchemaCommand request, CancellationToken cancellationToken)
        {
            var defaultConnection = await unitOfWork.ConnectionRepository.FirstOrDefaultAsync(x => x.IsDefault, false, cancellationToken);
            if(defaultConnection is null || defaultConnection.Provider != ProviderTypes.SqlServer)
            {
                return Result.CreateFailure([new(Constants.Errors.NoDefaultConnectionConfigured)]);
            }

            await annotationFileManager.AppendOrReplaceBlockAsync(request.TableAnnotation, defaultConnection.Id);

            return Result.CreateSuccess();
        }
    }
}
