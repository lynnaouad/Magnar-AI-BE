namespace Magnar.AI.Application.Dto.Workspaces
{
    public class WorkspaceDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? LastModifiedAt { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public string? LastModifiedBy { get; set; }
    }

    public class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<WorkspaceDto, Workspace>().ReverseMap();
        }
    }
}
