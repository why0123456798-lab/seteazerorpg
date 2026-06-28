using RPGBattleMaker.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace RPGBattleMaker.Infrastructure.Interface
{
    public interface IEventService
    {
        EventResult GetEventResult(Event events, int selectedValue, List<Agent> teamAgents);
        Task<Event?> RandomEvent();
    }
}
