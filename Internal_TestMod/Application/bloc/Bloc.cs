using NinMods;
using NinMods.Application.FarmBotBloc;
using NinMods.Bot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public abstract class Bloc<TBlocStateType, TBlocEventType> : BaseBloc<TBlocStateType, TBlocEventType>
{
    
    protected TBlocStateType _currentState;
    protected TBlocStateType _fallbackState;
    protected IBotBlocCommand<TBlocEventType> _currentCommand = null;

    public TBlocStateType currentState { get => _currentState; set { _currentState = value; } }
    public TBlocStateType fallbackState { get => _fallbackState; set { _fallbackState = value; } }
    public IBotBlocCommand<TBlocEventType> currentCommand { get => _currentCommand; set { _currentCommand = value; } }

    public bool HasFailedCatastrophically;

    abstract public TBlocStateType mapEventToState(TBlocEventType e);
    abstract public IBotBlocCommand<TBlocEventType> mapStateToCommand(TBlocStateType state);

    public Bloc(TBlocStateType startState, TBlocStateType fallbackState)
    {
        _currentState = startState;
        _fallbackState = fallbackState;
    }

    public void addEvent(TBlocEventType e)
    {
        // get the new state
        _currentState = mapEventToState(e);
        // trigger command
        _currentCommand = mapStateToCommand(_currentState);
    }

    public void Run(TBlocEventType fallbackEvent)
    {
        if (HasFailedCatastrophically)
        {
            Logger.Log.WriteError("Bloc", "Run", "Bot failed catastrophically, cannot do anything.");
            return;
        }

        if (_currentCommand != null)
        {
            TBlocEventType nextEvent = currentCommand.Perform();
            addEvent(nextEvent);
        }

        if (currentCommand == null)
        {
            addEvent(fallbackEvent);
        }
    }
}