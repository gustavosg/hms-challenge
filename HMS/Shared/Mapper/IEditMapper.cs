namespace Shared.Mapper;

public interface IEditMapper<TDTO, TEntity>
    where TEntity : class
    where TDTO : class
{
    TEntity ToEntity(TDTO request, object original);
}
