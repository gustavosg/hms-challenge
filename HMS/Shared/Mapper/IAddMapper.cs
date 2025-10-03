using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Mapper;

public interface IAddMapper<TDTO, TEntity>
    where TEntity : class
    where TDTO : class
{
    TEntity ToEntity(TDTO request);
}
