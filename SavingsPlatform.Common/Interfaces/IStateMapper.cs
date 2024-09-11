using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SavingsPlatform.Common.Interfaces
{
    public interface IStateMapper<TFrom, TTo> 
    {
        TTo Map(TFrom state);

        TFrom ReverseMap(TTo dto);
    }
}
