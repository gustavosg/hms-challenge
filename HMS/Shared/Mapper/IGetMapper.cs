namespace Shared.Mapper;

public interface IGetMapper<TEntity, TDTO>
    where TEntity : class
    where TDTO : class
{
    TDTO ToDTO(TEntity source);
}
