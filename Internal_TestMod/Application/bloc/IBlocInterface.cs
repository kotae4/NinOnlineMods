using NinMods.Bot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public interface IBlocInterface<TBlocStateType, TBlocEventType>
{
    TBlocStateType currentState { get;set; }  

    TBlocStateType fallbackState { get;set; }

    IBotBlocCommand<TBlocEventType> currentCommand { get; set; }
}
