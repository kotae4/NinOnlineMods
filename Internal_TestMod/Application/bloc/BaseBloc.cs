using NinMods.Bot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public interface BaseBloc<BlocStateType>
{
    BlocStateType currentState { get;set; }  

    BlocStateType oldState { get;set; }
    IBotCommand currentCommand { get; set; }


}

