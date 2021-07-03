using NinMods;
using NinMods.Application.FarmBotBloc;
using NinMods.Bot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public abstract class BaseBlocMachine<TBlocStateType, TBlocEventType> : IBlocInterface<TBlocStateType, TBlocEventType>
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

    public BaseBlocMachine(TBlocStateType startState, TBlocStateType fallbackState)
    {
        _currentState = startState;
        _fallbackState = fallbackState;
    }

    public void handleEvent(TBlocEventType e)
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
            Logger.Log.WriteError("BaseBlocMachine", "Run", "Bot failed catastrophically, cannot do anything.");
            return;
        }

        if (_currentCommand != null)
        {
            Logger.Log.Write("BaseBlocMachine", "Run", $"Performing command '{_currentCommand}'");
            TBlocEventType nextEvent = currentCommand.Perform();
            handleEvent(nextEvent);
        }

        if (currentCommand == null)
        {
            Logger.Log.Write("BaseBlocMachine", "Run", $"No active command, falling back via arg '{fallbackEvent}'");
            handleEvent(fallbackEvent);
        }
    }
}