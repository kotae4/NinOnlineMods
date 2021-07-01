using NinMods.Bot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


abstract class Bloc<BlocStateType, BlocEventType> : BaseBloc<BlocStateType>
{
    
    private BlocStateType _currentState;
    private BlocStateType _oldState;
    private IBotCommand _currentCommand = null;

    public Bloc(BlocStateType initialState)
    {
        _currentState = initialState;
    }

    public BlocStateType currentState { get => _currentState; set { _currentState = value; } }
    public BlocStateType oldState { get => _oldState; set { _oldState = value; } }
    public IBotCommand currentCommand { get => _currentCommand; set { _currentCommand = value; } }



    abstract public BlocStateType mapEventToState(BlocEventType e);
    public void addEvent(BlocEventType e)
    {
        // remember old state
        _oldState = _currentState;
        // get the new state
        _currentState = mapEventToState(e);
        // trigger command
        triggerCommandBasedOnCurrentState();


    }

    abstract public void triggerCommandBasedOnCurrentState();

    private void Update()
    {
        // to be done...
    }

}

