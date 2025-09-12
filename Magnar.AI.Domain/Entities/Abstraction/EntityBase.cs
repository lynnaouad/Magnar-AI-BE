namespace Magnar.AI.Domain.Entities.Abstraction;

public abstract class EntityBase<TKey>
    where TKey : struct
{
    public TKey Id { get; set; }
}
