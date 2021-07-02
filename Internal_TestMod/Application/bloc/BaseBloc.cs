using NinMods.Bot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public interface BaseBloc<BlocStateType, BlocEventType>
{
    BlocStateType currentState { get;set; }  

    BlocStateType fallbackState { get;set; }
    IBotBlocCommand<BlocEventType> currentCommand { get; set; }


}

