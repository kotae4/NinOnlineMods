using NinMods;
using NinMods.Application.FarmBotBloc;
using NinMods.Bot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public abstract class Bloc<BlocStateType, BlocEventType> : BaseBloc<BlocStateType, BlocEventType>
{
    
    private BlocStateType _currentState;
    private BlocStateType _fallbackState;
    private IBotBlocCommand<BlocEventType> _currentCommand = null;
   
    public bool HasFailedCatastrophically;

    public Bloc(BlocStateType startState, BlocStateType fallbackState)
    {
        _currentState = startState;
        _fallbackState = fallbackState;
    }

    public BlocStateType currentState { get => _currentState; set { _currentState = value; } }
    public BlocStateType fallbackState { get => _fallbackState; set { _fallbackState = value; } }
    public IBotBlocCommand<BlocEventType> currentCommand { get => _currentCommand; set { _currentCommand = value; } }



    abstract public BlocStateType mapEventToState(BlocEventType e);
    public void addEvent(BlocEventType e)
    {
        // get the new state
        _currentState = mapEventToState(e);
        // trigger command
        changeCurrentCommandBasedOnCurrentState();


    }

    abstract public void changeCurrentCommandBasedOnCurrentState();

    public void Run(BlocEventType fallbackEvent)
    {
        if (HasFailedCatastrophically)
        {
            Logger.Log.WriteError("FarmBot", "Update", "Bot failed catastrophically, cannot do anything.");
            return;
        }

        if (_currentCommand != null)
        {
            BlocEventType nextEvent = currentCommand.Perform();
            addEvent(nextEvent);

        }

        if (currentCommand == null)
        {
            addEvent(fallbackEvent);
        }
    }

}

